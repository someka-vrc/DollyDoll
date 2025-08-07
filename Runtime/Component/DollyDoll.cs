using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Somekasu.DollyDoll;
using UniRx;
using System.Linq;
using System;
using extOSC;

namespace Somekasu.DollyDoll
{

    /// <summary>
    /// VRChat Camera Dolly のカメラパス定義ファイルエディタ。
    /// JSONファイルを読み込み、ノードの位置回転とパスをシーン上に可視化する。
    /// </summary>
    [ExecuteAlways]
    public class DollyDoll : MonoBehaviour
    {
        [SerializeField]
        internal string JSONPath = "";
        [SerializeField]
        internal string LocaleName = "日本語";
        [SerializeField]
        internal Osc Osc;
        [SerializeField]
        internal PlayBackSetting PlayBackSetting;
        [SerializeField]
        internal CircleGen CircleGen;

        /// <summary>
        /// Editor Foldoutの開閉状態を保持するDictionary。キーはFoldout名。
        /// </summary>
        [NonSerialized]
        public Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>();

        internal Transform NodesObj
        {
            get
            {
                if (transform == null)
                {
                    return new GameObject("empty").transform;
                }
                var container = transform.Find("Nodes");
                if (container == null)
                {
                    container = new GameObject("Nodes").transform;
                    container.SetParent(transform);
                    container.localPosition = Vector3.zero;
                    var ddObj = container.gameObject.AddComponent<DollyDollObject>();
                    ddObj.DollyDoll = this;
                }
                return container;
            }
        }

        internal Transform SplinesObj
        {
            get
            {
                if (transform == null)
                {
                    return new GameObject("empty").transform;
                }
                var container = transform.Find("Splines");
                if (container == null)
                {
                    container = new GameObject("Splines").transform;
                    container.SetParent(transform);
                    container.localPosition = Vector3.zero;
                    var ddObj = container.gameObject.AddComponent<DollyDollObject>();
                    ddObj.DollyDoll = this;
                }
                return container;
            }
        }

        internal Transform PreviewCamera
        {
            get
            {
                if (transform == null)
                {
                    return new GameObject("empty").transform;
                }
                var cameraTransform = transform.Find("Camera");
                if (cameraTransform == null)
                {
                    cameraTransform = new GameObject("Camera").transform;
                    cameraTransform.SetParent(transform);
                    cameraTransform.localPosition = Vector3.zero;
                }
                if (!cameraTransform.TryGetComponent<DollyDollObject>(out _))
                {
                    var ddo = cameraTransform.gameObject.AddComponent<DollyDollObject>();
                    ddo.DollyDoll = this;
                }
                if (!cameraTransform.TryGetComponent<Camera>(out _))
                {
                    var camera = cameraTransform.gameObject.AddComponent<Camera>();
                    camera.fieldOfView = 26.99147f;
                    camera.nearClipPlane = 0.01f;
                    camera.farClipPlane = 1000f;
                    camera.depth = -2f;
                }
                return cameraTransform;
            }
        }

        private CompositeDisposable _disposables;

        ///<summary> ノードリスト変更検知。増減・並び変更監視のためインスタンスID連結・ハッシュ化して監視（要素内容は個別で監視） </summary>
        internal IObservable<Unit> NodesChanged { get; private set; }

        internal DollyDollService Service { get; private set; }

        internal IEnumerable<DollyDollCameraNode> Nodes => NodesObj.GetComponentsInChildren<DollyDollCameraNode>();
        internal IEnumerable<DollyDollSpline> Splines => SplinesObj.GetComponentsInChildren<DollyDollSpline>();

        private void OnEnable()
        {
            Osc ??= new Osc();
            Osc.Initialize(this);
            PlayBackSetting ??= new PlayBackSetting();
            CircleGen ??= new CircleGen();
            _ = NodesObj;
            _ = SplinesObj;
            _ = PreviewCamera;
            _disposables = new CompositeDisposable();

            I18n.Load();
            I18n.SetLocaleByDisplayName(LocaleName);
            I18n.Subscribe().AddTo(_disposables);

            var comparer = new FastListEqualityComparer();
            NodesChanged = Observable.Interval(TimeSpan.FromSeconds(0.5))
                    .Where(_ => this != null && gameObject.activeInHierarchy)
                    .Select(_ => Nodes.Select(t => t.GetInstanceID()).ToList())
                    .DistinctUntilChanged(comparer)
                    .Select(_ => Unit.Default);
            this.ObserveEveryValueChanged(x => x.LocaleName)
                .Subscribe(lang =>
                {
                    I18n.SetLocaleByDisplayName(lang);
                })
                .AddTo(_disposables);
            Service = new DollyDollService(this).AddTo(_disposables);

        }

        void OnDisable()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        internal void Load() => JSONPath = Service.Load();
        internal void Reload() => JSONPath = Service.Load(JSONPath);
        internal void SaveAsNew()
        {
            JSONPath = Service.Save();
            Osc.SendImportRequest();
        }

        internal void Save()
        {
            JSONPath = Service.Save(JSONPath);
            Osc.SendImportRequest();
        }

        internal void AddNode() => Service.AddNode();
        internal void SetLookAtToAllNodes() => Service.SetLookAtToAllNodes();
        internal void SetLookAtToSelectionNodes() => Service.SetLookAtToSelectionNodes();
        internal void GenerateCircleNodes() => CircleGen.GenerateCircleNodes(Service.CreateCameraNode, transform);
    }
}