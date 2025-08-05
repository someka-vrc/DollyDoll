using UnityEngine;
using UnityEditor;
using System;
using Unity.Mathematics;
using UniRx;

namespace Somekasu.DollyDoll
{
    [ExecuteAlways]
    /// <summary>
    /// DollyDollのカメラパス上のノードを定義するコンポーネントです。
    /// </summary>
    public class DollyDollCameraNode : DollyDollObject
    {
        /// <summary>ローカル座標かどうか (FALSE=ワールド, TRUE=ローカル)</summary>
        [SerializeField]
        internal bool IsLocal = true;
        /// <summary>フォーカス距離 (Default: 1.5, Min: 0, Max: 10)</summary>
        [SerializeField]
        internal float FocalDistance = 1.5f;
        /// <summary>絞り値 (Default: 15, Min: 1.4, Max: 32)</summary>
        [SerializeField]
        internal float Aperture = 15f;
        /// <summary>グリーンスクリーン色相 (Hue) (Default: 120, Min: 0, Max: 360)</summary>
        [SerializeField]
        internal float Hue = 120f;
        /// <summary>グリーンスクリーン彩度 (Saturation) (Default: 100, Min: 0, Max: 100)</summary>
        [SerializeField]
        internal float Saturation = 100f;
        /// <summary>グリーンスクリーン輝度 (Lightness) (Default: 50, Min: 0, Max: 50)</summary>
        [SerializeField]
        internal float Lightness = 50f;
        /// <summary>Look-At-Me X方向オフセット (Default: 0, Min: -25, Max: 25)</summary>
        [SerializeField]
        internal float LookAtMeXOffset = 0f;
        /// <summary>Look-At-Me Y方向オフセット (Default: 0, Min: -25, Max: 25)</summary>
        [SerializeField]
        internal float LookAtMeYOffset = 0f;
        /// <summary>ズーム (Default: 45, Min: 20, Max: 150)</summary>
        [SerializeField]
        internal float Zoom = 45f;
        /// <summary>移動速度 (Default: 3, Min: 0.1, Max: 15)</summary>
        [SerializeField]
        internal float Speed = 3f;
        /// <summary>移動時間 (秒/m) (Default: 2, Min: 0.1, Max: 60)</summary>
        [SerializeField]
        internal float Duration = 2f;
        /// <summary>パス内のインデックス (Default: -1(append), Min: 0, Max: パス内ポイント数)</summary>
        [SerializeField]
        internal int Index = -1;
        /// <summary>パス番号 (Default: 現在のパス, Min: 0, Max: パス数)</summary>
        [SerializeField]
        internal int PathIndex = 0;
        ///<summary> T。スプライン計算用 </summary>
        [SerializeField]
        internal float T = 0f;

        internal ReadOnlyReactiveProperty<Vector3> Position => transform.ObserveEveryValueChanged(transform => transform.position)
                .ToReadOnlyReactiveProperty(transform.position);
        internal ReadOnlyReactiveProperty<Vector3> Rotation => transform.ObserveEveryValueChanged(transform => transform.eulerAngles)
                .ToReadOnlyReactiveProperty(transform.eulerAngles);

        private void OnEnable()
        {
            // 自身をDollyDollに登録
            Observable.EveryUpdate()
                .Where(_ => DollyDoll != null && DollyDoll.enabled)
                .Take(1)
                .Subscribe(_ => DollyDoll.Service.SubscribeNodeComponent(this))
                .AddTo(this);
        }

        /// <summary><see cref="CameraNode"/> からセットする</summary>
        internal void FromCameraNode(CameraNode point)
        {
            IsLocal = point.IsLocal;
            FocalDistance = point.FocalDistance;
            Aperture = point.Aperture;
            Hue = point.Hue;
            Saturation = point.Saturation;
            Lightness = point.Lightness;
            LookAtMeXOffset = point.LookAtMeXOffset;
            LookAtMeYOffset = point.LookAtMeYOffset;
            Zoom = point.Zoom;
            Speed = point.Speed;
            Duration = point.Duration;
            Index = point.Index;
            PathIndex = point.PathIndex;
            // Transformに反映
            transform.position = point.PositionVector3;
            transform.eulerAngles = point.RotationVector3;
        }

        /// <summary><see cref="CameraNode"/> に変換する</summary>
        internal CameraNode ToCameraNode()
        {
            // TransformからPosition/Rotationを取得
            var pos = transform.position;
            var rot = transform.eulerAngles;
            // -180～180度に正規化
            var normX = NormalizeAngle(rot.x);
            var normY = NormalizeAngle(rot.y);
            var normZ = NormalizeAngle(rot.z);

            return new CameraNode
            {
                IsLocal = IsLocal,
                Position = new SerializableVector3 { X = pos.x, Y = pos.y, Z = pos.z },
                Rotation = new SerializableVector3 { X = normX, Y = normY, Z = normZ },
                FocalDistance = FocalDistance,
                Aperture = Aperture,
                Hue = Hue,
                Saturation = Saturation,
                Lightness = Lightness,
                LookAtMeXOffset = LookAtMeXOffset,
                LookAtMeYOffset = LookAtMeYOffset,
                Zoom = Zoom,
                Speed = Speed,
                Duration = Duration,
                Index = Index,
                PathIndex = PathIndex
            };
        }

        /// <summary>
        /// 角度を-180～180度に正規化する
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f)
            {
                angle -= 360f;
            }

            if (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }
    }

    [CustomEditor(typeof(DollyDollCameraNode))]
    [CanEditMultipleObjects]
    public class DollyDollCameraNodeEditor : DollyDollObjectEditor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var node = (DollyDollCameraNode)target;
            var dolly = node.DollyDoll;
            if (dolly == null)
            {
                EditorGUILayout.HelpBox("No DollyDoll reference found", MessageType.Warning);
                return;
            }

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IsLocal"), Gc("IsLocal"));
            EditorGUILayout.Slider(serializedObject.FindProperty("FocalDistance"), 0f, 10f, Gc("FocalDistance"));
            EditorGUILayout.Slider(serializedObject.FindProperty("Aperture"), 1.4f, 32f, Gc("Aperture"));
            EditorGUILayout.Slider(serializedObject.FindProperty("Hue"), 0f, 360f, Gc("Hue"));
            EditorGUILayout.Slider(serializedObject.FindProperty("Saturation"), 0f, 100f, Gc("Saturation"));
            EditorGUILayout.Slider(serializedObject.FindProperty("Lightness"), 0f, 50f, Gc("Lightness"));
            EditorGUILayout.Slider(serializedObject.FindProperty("LookAtMeXOffset"), -25f, 25f, Gc("LookAtMeXOffset"));
            EditorGUILayout.Slider(serializedObject.FindProperty("LookAtMeYOffset"), -25f, 25f, Gc("LookAtMeYOffset"));
            EditorGUILayout.Slider(serializedObject.FindProperty("Zoom"), 20f, 150f, Gc("Zoom"));

            // MotionControlに応じてSpeed/Durationの活性制御
            EditorGUI.BeginDisabledGroup(dolly.PlayBackSetting.MotionControl != PBMotionControl.SpeedBased);
            EditorGUILayout.Slider(serializedObject.FindProperty("Speed"), 0.1f, 15f, Gc("Speed"));
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(dolly.PlayBackSetting.MotionControl != PBMotionControl.TimeBased);
            EditorGUILayout.Slider(serializedObject.FindProperty("Duration"), 0.1f, 60f, Gc("Duration"));
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(true);
            // インデックスは自動計算されるため、エディタでは編集しない
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Index"), Gc("Index"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PathIndex"), Gc("PathIndex"));

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("T"), Gc("T"));
            EditorGUI.EndDisabledGroup();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
