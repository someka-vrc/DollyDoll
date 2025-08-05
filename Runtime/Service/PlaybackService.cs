using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Somekasu.DollyDoll;
using UniRx;
using System.Linq;
using System;
using System.Diagnostics;

namespace Somekasu.DollyDoll
{
    internal class PlaybackService : IDisposable
    {
        private readonly DollyDoll _dollyDoll;
        private readonly CompositeDisposable _disposables;

        ///<summary> スプライン総時間 </summary>
        internal readonly Dictionary<int, float> Duration;
        ///<summary> スプライン総伸長 </summary>
        internal readonly Dictionary<int, float> Length;
        ///<summary> スプライン有効範囲 </summary>
        internal readonly Dictionary<int, Vector2> Range;
        ///<summary> スプラインの進行状況 </summary>
        internal readonly ReactiveProperty<float> CurrentProgress;
        ///<summary> 現在の線形進行状況(イージング計算に使用) </summary>
        private float _currentLinearProgress;
        ///<summary> 区間内T </summary>
        private float _fixedTInPeriod = 0f;
        ///<summary> 現在の区間の始点 </summary>
        private DollyDollCameraNode _periodStart = null;
        ///<summary> 現在の区間の終点 </summary>
        private DollyDollCameraNode _periodEnd = null;
        ///<summary> 選択中のパスインデックス </summary>
        internal readonly ReactiveProperty<int> SelectedPathIndex;
        ///<summary> プレイバック中 </summary>
        internal readonly ReactiveProperty<bool> IsPlaying;
        ///<summary> 進行方向 </summary>
        private bool _isForward;
        ///<summary> キャンセル用のSubject </summary>
        private Subject<Unit> _playbackCancel;

        ///<summary> イージングメソッド </summary>
        private Func<float, float> _easingFunction;
        private static readonly float ALMOST_ZERO = 0.001f;
        private static readonly float ALMOST_ONE = 0.999f;

        internal PlaybackService(DollyDoll dollyDoll, Subject<Unit> onSplineUpdated)
        {
            _dollyDoll = dollyDoll;
            _disposables = new CompositeDisposable();
            Duration = new Dictionary<int, float>();
            Length = new Dictionary<int, float>();
            Range = new Dictionary<int, Vector2>();
            CurrentProgress = new ReactiveProperty<float>(0f);
            SelectedPathIndex = new ReactiveProperty<int>(0);
            IsPlaying = new ReactiveProperty<bool>(false);
            _isForward = true;
            _easingFunction = Easing.Linear;

            // スプライン更新、選択パス変更、進行状況更新 -> プレビューカメラ位置回転、区間内T更新
            Observable.Merge(onSplineUpdated, SelectedPathIndex.Select(_ => Unit.Default), CurrentProgress.Select(_ => Unit.Default))
                .Subscribe(_ =>
                {
                    var splineContainer = _dollyDoll.Splines
                        .FirstOrDefault(s => s.PathIndex == SelectedPathIndex.Value)?.GetComponent<SplineContainer>();
                    if (splineContainer == null)
                    {
                        return;
                    }

                    var nodes = _dollyDoll.Nodes
                        .Where(n => n.PathIndex == SelectedPathIndex.Value)
                        .OrderBy(n => n.Index)
                        .ToList();
                    var t = CurrentProgress.Value;
                    var range = Range.TryGetValue(SelectedPathIndex.Value, out Vector2 rng) ? rng : new Vector2(0f, 1f);
                    var w = range.y - range.x;
                    // 0-1のtをrange幅に圧縮してオフセット
                    var fixedT = w > 0f ? Mathf.Clamp01(t * w + range.x) : 0f;
                    Vector3 position = splineContainer.EvaluatePosition(fixedT);
                    _dollyDoll.PreviewCamera.position = position;
                    _periodStart = null;
                    _periodEnd = null;
                    for (int i = 1; i < nodes.Count; i++)
                    {
                        if (fixedT < nodes[i].T)
                        {
                            _periodStart = nodes[i - 1];
                            _periodEnd = nodes[i];
                            break;
                        }
                    }
                    if (_periodStart == null || _periodEnd == null)
                    {
                        // 選択パスにノードがない場合は終了
                        return;
                    }
                    _fixedTInPeriod = (fixedT - _periodStart.T) / (_periodEnd.T - _periodStart.T);
                    _dollyDoll.PreviewCamera.rotation = Quaternion.Lerp(_periodStart.transform.rotation, _periodEnd.transform.rotation, _fixedTInPeriod);
                    _dollyDoll.PreviewCamera.GetComponent<Camera>().fieldOfView = Mathf.Lerp(
                        _periodStart.Zoom, _periodEnd.Zoom, _fixedTInPeriod);
                })
                .AddTo(_disposables);

            // ループ設定を監視し、Reverse以外に設定された時に進行方向を前向きにする
            _dollyDoll.ObserveEveryValueChanged(x => x.PlayBackSetting.Looping)
                .Where(looping => looping != PBLooping.Reverse)
                .Subscribe(_ => _isForward = true)
                .AddTo(_disposables);

            // イージング設定を監視し、変更時にイージング関数を更新
            _dollyDoll.ObserveEveryValueChanged(x => x.PlayBackSetting.Easing)
                .Subscribe(easing =>
                {
                    _easingFunction = easing switch
                    {
                        PBEasing.None => Easing.Linear,
                        PBEasing.InSine => Easing.EaseInSine,
                        PBEasing.OutSine => Easing.EaseOutSine,
                        PBEasing.InOutSine => Easing.EaseInOutSine,
                        PBEasing.InQuart => Easing.EaseInQuart,
                        PBEasing.OutQuart => Easing.EaseOutQuart,
                        PBEasing.InOutQuart => Easing.EaseInOutQuart,
                        PBEasing.InExpo => Easing.EaseInExpo,
                        PBEasing.OutExpo => Easing.EaseOutExpo,
                        PBEasing.InOutExpo => Easing.EaseInOutExpo,
                        PBEasing.InBounce => Easing.EaseInBounce,
                        PBEasing.OutBounce => Easing.EaseOutBounce,
                        PBEasing.InOutBounce => Easing.EaseInOutBounce,
                        _ => Easing.Linear
                    };
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// スプラインの進行状況を再生する
        /// </summary>
        internal void PlaySplineT()
        {
            if (!Duration.TryGetValue(SelectedPathIndex.Value, out float duration) || duration <= 0)
            {
                MyLog.LogError($"PathIndex {SelectedPathIndex.Value} のスプラインの総時間が取得できません。");
                return;
            }
            if (!Length.TryGetValue(SelectedPathIndex.Value, out float length) || length <= 0)
            {
                MyLog.LogError($"PathIndex {SelectedPathIndex.Value} のスプラインの総伸長が取得できません。");
                return;
            }
            var splineContainer = _dollyDoll.Splines
                .FirstOrDefault(s => s.PathIndex == SelectedPathIndex.Value)?.GetComponent<SplineContainer>();
            if (splineContainer == null)
            {
                MyLog.LogError($"PathIndex {SelectedPathIndex.Value} のスプラインが見つかりません。");
                return;
            }

            if (CurrentProgress.Value > ALMOST_ONE)
            {
                CurrentProgress.Value = 0f;
                _currentLinearProgress = 0f;
            }
            IsPlaying.Value = true;

            var timerDisposable = new CompositeDisposable();
            _disposables.Add(timerDisposable);

            // スプラインの進行状況を更新
            _playbackCancel = new();
            double prev = 0;
            Stopwatch sw = new();
            sw.Start();
            Observable.EveryUpdate()
                .TakeWhile(_ => _dollyDoll.PlayBackSetting.Looping != PBLooping.None || CurrentProgress.Value < ALMOST_ONE) // ループ系じゃないならゴールしてない間だけ
                .TakeUntil(_playbackCancel) // キャンセルが来たら即終了
                .Finally(() =>
                {
                    IsPlaying.Value = false;
                    timerDisposable.Dispose();
                })
                .Subscribe(_ =>
                {
                    double now = sw.Elapsed.TotalSeconds;
                    float deltaTime = (float)(now - prev);
                    prev = now;
                    switch (_dollyDoll.PlayBackSetting.MotionControl)
                    {
                        case PBMotionControl.TimeBased:
                            _currentLinearProgress += deltaTime / duration * (_isForward ? 1f : -1f);
                            CurrentProgress.Value = _easingFunction(_currentLinearProgress);
                            break;
                        case PBMotionControl.SpeedBased:
                            var speed = _periodStart != null && _periodEnd != null
                                ? 1 / Mathf.Lerp(_periodStart.Speed, _periodEnd.Speed, _fixedTInPeriod) : 1f / 3f;
                            CurrentProgress.Value += speed * deltaTime / length * (_isForward ? 1f : -1f);
                            break;
                    }
                    switch (_dollyDoll.PlayBackSetting.Looping)
                    {
                        case PBLooping.None:
                            break;
                        case PBLooping.Repeat:
                        case PBLooping.Revolve:
                            if (CurrentProgress.Value > ALMOST_ONE)
                            {
                                CurrentProgress.Value = 0f;
                                _currentLinearProgress = 0f;
                            }
                            break;
                        case PBLooping.Reverse:
                            if (CurrentProgress.Value < ALMOST_ZERO || CurrentProgress.Value > ALMOST_ONE)
                            {
                                _isForward = !_isForward;
                            }
                            break;
                    }
                    CurrentProgress.Value = Mathf.Clamp01(CurrentProgress.Value);
                    _currentLinearProgress = Mathf.Clamp01(_currentLinearProgress);
                })
                .AddTo(timerDisposable);
        }

        internal void StopSplineT()
        {
            if (_playbackCancel != null)
            {
                _playbackCancel.OnNext(Unit.Default);
                _playbackCancel.Dispose();
                _playbackCancel = null;
            }
            IsPlaying.Value = false;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}