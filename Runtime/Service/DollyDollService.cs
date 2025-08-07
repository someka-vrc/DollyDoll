using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using System.Linq;
using System;
using System.IO;
using Newtonsoft.Json;
using UniRx;
using Unity.Mathematics;
using UnityEngine.Animations;

namespace Somekasu.DollyDoll
{

    internal class DollyDollService : IDisposable
    {
        private DollyDoll _dollyDoll;
        private CompositeDisposable _disposables;
        ///<summary> スプライン更新要求 </summary>
        private readonly Subject<Unit> _splineUpdateRequest;
        private readonly Subject<Unit> _onSplineUpdated;
        internal readonly PlaybackService Playback;
        private readonly Mesh _ctrlMesh;
        private readonly Mesh _cameraMesh;

        public DollyDollService(DollyDoll dollyDoll)
        {
            _dollyDoll = dollyDoll;
            _disposables = new CompositeDisposable();
            _splineUpdateRequest = new Subject<Unit>();
            _onSplineUpdated = new Subject<Unit>();
            Playback = new PlaybackService(dollyDoll, _onSplineUpdated).AddTo(_disposables);

            GameObject ctrlModel = Resources.Load<GameObject>("DollyDoll/DollyDollControl");
            _ctrlMesh = ctrlModel.GetComponent<MeshFilter>().sharedMesh;
            GameObject cameraModel = Resources.Load<GameObject>("DollyDoll/DollyDollCamera");
            _cameraMesh = cameraModel.GetComponent<MeshFilter>().sharedMesh;

            // スプライン更新要求
            Observable.Merge(_splineUpdateRequest.ThrottleFirst(TimeSpan.FromSeconds(0.05)), _splineUpdateRequest.Throttle(TimeSpan.FromSeconds(0.05)))
                .Subscribe(_ => RegenerateSplines())
                .AddTo(_disposables);

            // スプライン更新誘発
            new List<IObservable<Unit>>
            {
                _dollyDoll.NodesChanged,
                _dollyDoll.PlayBackSetting.ObserveEveryValueChanged(playBack => playBack.MotionControl).Select(_ => Unit.Default),
                _dollyDoll.PlayBackSetting.ObserveEveryValueChanged(playBack => playBack.Easing).Select(_ => Unit.Default),
                _dollyDoll.PlayBackSetting.ObserveEveryValueChanged(playBack => playBack.PathType).Select(_ => Unit.Default),
                _dollyDoll.PlayBackSetting.ObserveEveryValueChanged(playBack => playBack.Looping).Select(_ => Unit.Default),
            }
            .Merge()
            .Subscribe(_ => _splineUpdateRequest.OnNext(Unit.Default))
            .AddTo(_disposables);

            // 初回構築
            _splineUpdateRequest.OnNext(Unit.Default);
        }

        internal void SubscribeNodeComponent(DollyDollCameraNode nodeComponent)
        {
            nodeComponent.Position.Subscribe(position => _splineUpdateRequest.OnNext(Unit.Default)).AddTo(nodeComponent);
            nodeComponent.Rotation.Subscribe(rotation => _splineUpdateRequest.OnNext(Unit.Default)).AddTo(nodeComponent);
            nodeComponent.ObserveEveryValueChanged(n => n.Duration)
                .Subscribe(_ => _splineUpdateRequest.OnNext(Unit.Default))
                .AddTo(nodeComponent);
            nodeComponent.ObserveEveryValueChanged(n => n.PathIndex)
                .Subscribe(_ => _splineUpdateRequest.OnNext(Unit.Default))
                .AddTo(nodeComponent);
            nodeComponent.ObserveEveryValueChanged(n => n.Index)
                .Subscribe(_ => _splineUpdateRequest.OnNext(Unit.Default))
                .AddTo(nodeComponent);
        }

        /// <summary>
        /// スプライン生成
        /// コンポーネントやメッシュが生成済みの場合はスプラインデータを差し替える
        /// </summary>
        private void RegenerateSplines()
        {
            var nodeComponents = _dollyDoll.Nodes;
            // nodeComponentsのすべての要素について、同一PathIndexの中でIndexを出現順に0から振り直す
            foreach (var group in nodeComponents.GroupBy(dcp => dcp.PathIndex))
            {
                int currentIndex = 0;
                foreach (var node in group)
                {
                    node.Index = currentIndex++;
                }
            }

            // PathIndexとIndexに応じてゲームオブジェクトをリネーム
            foreach (var node in nodeComponents)
            {
                node.name = $"cam_{node.PathIndex}_{node.Index}";
            }

            // var nodes = nodeComponents.Select(dcp => dcp.ToCameraNode()).ToList();
            var nodes = nodeComponents.ToList();
            // パスごとにスプライン生成
            foreach (var pathGrp in nodes.GroupBy(p => p.PathIndex))
            {
                int pathIndex = pathGrp.Key;
                List<DollyDollCameraNode> path = pathGrp.ToList();
                if (path.Count < 2)
                {
                    // パスにノードが2つ未満の場合はスプラインを生成しない
                    continue;
                }
                // Fitted and Loose paths require at least 4 points to be smoothed. While a path has less than 4 points, it will always be linear.
                bool isLoop = _dollyDoll.PlayBackSetting.Looping == PBLooping.Revolve;
                PBPathType pathType = path.Count > 3 ? _dollyDoll.PlayBackSetting.PathType : PBPathType.Linear;

                List<float3> points = path.Select(p => (float3)p.transform.localPosition).ToList();
                float[] fittedTs = null;
                Spline spline = pathType switch
                {
                    PBPathType.Fitted => BSplineFactory.Create(points, out fittedTs, isLoop),
                    PBPathType.Loose => SplineFactory.CreateCatmullRom(points, isLoop),
                    _ => SplineFactory.CreateLinear(points, isLoop),
                };

                Transform exist = _dollyDoll.Splines
                    .FirstOrDefault(s => s.PathIndex == pathIndex)?.transform;
                SplineExtrude splineExtrude;
                if (exist != null && exist.TryGetComponent<SplineContainer>(out var splineContainer))
                {
                    splineContainer.Spline = spline;
                    splineExtrude = exist.GetComponent<SplineExtrude>();
                    splineExtrude.Rebuild();
                }
                else
                {
                    // 新規スプラインオブジェクト作成
                    GameObject splineObj = new($"spline_{pathIndex}");
                    splineObj.transform.SetParent(_dollyDoll.SplinesObj);
                    splineObj.transform.localPosition = Vector3.zero;
                    var dds = splineObj.AddComponent<DollyDollSpline>();
                    dds.DollyDoll = _dollyDoll;
                    dds.PathIndex = pathIndex;
                    splineContainer = splineObj.AddComponent<SplineContainer>();
                    splineContainer.Spline = spline;
                    splineExtrude = splineObj.AddComponent<SplineExtrude>();
                    splineExtrude.Radius = 0.00250f;
                    splineExtrude.Sides = 8;
                    splineExtrude.SegmentsPerUnit = 64;
                    splineExtrude.RebuildOnSplineChange = true;
                    splineExtrude.Rebuild();
                }

                for (int i = 0; i < path.Count; i++)
                {
                    DollyDollCameraNode node = path[i];
                    if (pathType == PBPathType.Fitted)
                    {
                        node.T = fittedTs[i];
                    }
                    else if (pathType == PBPathType.Loose)
                    {
                        // Looseの場合はスプラインの近傍点を取得してTを設定
                        float3 p = (float3)node.transform.localPosition;
                        SplineUtility.GetNearestPoint(splineContainer.Spline, p, out float3 nearestPoint, out float percent, 16, 4);
                        node.T = percent;
                    }
                    else
                    {
                        float3 p = (float3)node.transform.position;
                        SplineUtility.GetNearestPoint(splineContainer.Spline, p, out float3 nearestPoint, out float percent, 16, 4);
                        node.T = percent;
                    }
                }

                // ループでなくリニアでない場合はsplineExtrudeのRangeから最初と最後の区間を除外する
                Vector2 range;
                if (!isLoop && pathType == PBPathType.Loose)
                {
                    // 始点・終点インデックスの区間の長さの割合を取得
                    float startRatio = GetSegmentLengthRatio(splineContainer, 0, 1);
                    float endRatio = GetSegmentLengthRatio(splineContainer, splineContainer.Spline.Count - 2, splineContainer.Spline.Count - 1);
                    range = new Vector2(startRatio, 1f - endRatio);
                }
                else if (!isLoop && pathType == PBPathType.Fitted)
                {
                    // 始点・終点を除外した区間の長さの割合を取得
                    range = new Vector2(fittedTs[1], fittedTs[fittedTs.Length - 2]);
                }
                else
                {
                    range = new Vector2(0f, 1f);
                }
                splineExtrude.Range = range;
                Playback.Range[pathIndex] = range;

                // メッシュの再指定
                for (int i = 0; i < path.Count; i++)
                {
                    // オープンでありリニアでなく端点の場合はコントローラーメッシュを使用
                    var mesh = !isLoop && pathType != PBPathType.Linear && (i == 0 || i == path.Count - 1)
                        ? _ctrlMesh
                        : _cameraMesh;
                    path[i].GetComponent<MeshFilter>().sharedMesh = mesh;
                }

                // スプライン総時間を計算
                Playback.Duration[pathIndex] = !isLoop && pathType != PBPathType.Linear
                    ? path.Skip(1).SkipLast(2).Sum(n => n.Duration)
                    : path.Sum(n => n.Duration);

                Playback.Length[pathIndex] = spline.GetLength();
            }
            _onSplineUpdated.OnNext(Unit.Default);
        }

        // 始点・終点インデックスの区間の長さの割合（0~1）を返す関数
        private static float GetSegmentLengthRatio(SplineContainer splineContainer, int startIndex, int endIndex)
        {
            var spline = splineContainer.Spline;
            var transform = splineContainer.transform;

            using var nativeSpline = new NativeSpline(spline, transform.localToWorldMatrix);

            var knots = spline.Knots.ToList();

            Vector3 startPos = transform.TransformPoint(knots[startIndex].Position);
            Vector3 endPos = transform.TransformPoint(knots[endIndex].Position);

            SplineUtility.GetNearestPoint(nativeSpline, startPos, out _, out float startPercent, 16, 8);
            SplineUtility.GetNearestPoint(nativeSpline, endPos, out _, out float endPercent, 16, 8);

            startPercent = Mathf.Clamp01(startPercent);
            endPercent = Mathf.Clamp01(endPercent);

            return Mathf.Abs(endPercent - startPercent);
        }

        private void ClearChildren()
        {
            var children = new List<Transform>();
            foreach (Transform child in _dollyDoll.transform)
            {
                children.Add(child);
            }
            foreach (var s in _dollyDoll.Splines)
            {
                if (s.TryGetComponent<MeshFilter>(out var mf))
                {
                    if (mf.sharedMesh != null)
                    {
                        var meshPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                        if (!string.IsNullOrEmpty(meshPath))
                        {
                            AssetDatabase.DeleteAsset(meshPath);
                            mf.sharedMesh = null;
                        }
                        else
                        {
                            UnityEngine.Object.DestroyImmediate(mf.sharedMesh, true);
                        }
                    }
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            foreach (var child in children)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        internal string Save(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                docsDir = Path.Combine(docsDir, "VRChat", "CameraPaths");
                path = EditorUtility.SaveFilePanel(I18n.G("dialog/title/save"), docsDir, "CameraPath", "json");
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
            }
            List<CameraNode> nodeComponents = _dollyDoll.Nodes
                .Select(dcp => dcp.ToCameraNode()).ToList();
            string json = JsonConvert.SerializeObject(nodeComponents, Formatting.Indented);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();

            return path;
        }

        internal string Load(string path = null)
        {
            bool shouldAsk = string.IsNullOrEmpty(path);
            if (!shouldAsk && !File.Exists(path))
            {
                MyLog.LogWarning($"File not found: {path}");
                shouldAsk = true;
            }
            if (shouldAsk)
            {
                string docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                docsDir = Path.Combine(docsDir, "VRChat", "CameraPaths");
                path = EditorUtility.OpenFilePanel(I18n.G("dialog/title/load"), docsDir, "json");
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }
            }
            string v = Path.GetExtension(path);
            if (string.IsNullOrEmpty(v) || v.ToLower() != ".json")
            {
                EditorUtility.DisplayDialog(I18n.G("dialog/title/error"), I18n.G("dialog/message/invalidFileExtension"), "OK");
                return null;
            }

            // JSONファイルを読み込み
            string json = File.ReadAllText(path);
            List<CameraNode> nodes = null;
            try
            {
                nodes = JsonConvert.DeserializeObject<List<CameraNode>>(json);
            }
            catch (Exception e)
            {
                MyLog.LogError(e);
            }
            if (nodes == null)
            {
                EditorUtility.DisplayDialog(I18n.G("dialog/title/error"), I18n.G("dialog/message/jsonParseFailed"), "OK");
                return null;
            }

            // ゲームオブジェクト配下のオブジェクトを全削除
            ClearChildren();

            // データごとにノードオブジェクトを生成
            foreach (CameraNode node in nodes)
            {
                // モデルを追加
                var go = CreateNodeObject(_dollyDoll.NodesObj, $"cam_{node.PathIndex}_{node.Index}", node);
                go.transform.localPosition = node.PositionVector3;
                go.transform.localEulerAngles = node.RotationVector3;
            }

            // スプライン生成
            RegenerateSplines();

            return path;
        }

        private GameObject CreateNodeObject(Transform parent, string name, CameraNode data)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localScale = Vector3.one * 100;
            DollyDollCameraNode nodeComponent = obj.AddComponent<DollyDollCameraNode>();
            nodeComponent.FromCameraNode(data);
            nodeComponent.DollyDoll = _dollyDoll;
            obj.AddComponent<MeshFilter>(); // sharedMeshは後で設定する
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            return obj;
        }

        internal DollyDollCameraNode CreateCameraNode()
        {
            (CameraNode lastNode, int lastPathIndex, int lastIndex) = GetLastNode();
            int newIndex = lastIndex + 1;
            CameraNode newNode = lastNode;
            newNode.Index = newIndex;
            var newObject = CreateNodeObject(_dollyDoll.NodesObj, $"cam_{lastPathIndex}_{newIndex}", newNode);

            return newObject.GetComponent<DollyDollCameraNode>();
        }

        internal void AddNode()
        {
            GameObject added = CreateCameraNode().gameObject;
            EditorGUIUtility.PingObject(added);
        }

        internal void SetLookAtToAllNodes() => SetLookAt(_dollyDoll.Nodes);
        internal void SetLookAtToSelectionNodes() => SetLookAt(_dollyDoll.Nodes.Where(n => Selection.gameObjects.Contains(n.gameObject)));

        private void SetLookAt(IEnumerable<DollyDollCameraNode> nodes)
        {
            Undo.SetCurrentGroupName("Set LookAt Constraint");
            GameObject lookAtTarget = CreateLookAtTarget();
            foreach (var node in nodes)
            {
                Undo.RecordObject(node, "Add LookAt Constraint");
                if (!node.TryGetComponent<LookAtConstraint>(out var lookAtConstraint))
                {
                    lookAtConstraint = node.gameObject.AddComponent<LookAtConstraint>();
                }
                lookAtConstraint.AddSource(new ConstraintSource
                {
                    sourceTransform = lookAtTarget.transform,
                    weight = 1f
                });
                lookAtConstraint.constraintActive = true;
            }
        }

        private GameObject CreateLookAtTarget()
        {
            // create a new GameObject for LookAt target
            string lookAtTargetName = GameObjectUtility.GetUniqueNameForSibling(null, "LookAtTarget");
            GameObject lookAtTarget = new(lookAtTargetName);
            Undo.RegisterCreatedObjectUndo(lookAtTarget, "Create LookAt Target");
            // search object with animator component which has non-null avatar
            Animator animator = GameObject.FindObjectsOfType<Animator>().FirstOrDefault(a => a.avatar != null);
            // Set position between the avatar's eyes if they exist
            if (animator != null)
            {
                Transform leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
                Transform rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
                if (leftEye != null && rightEye != null)
                {
                    // set position to the average of left and right eye positions
                    lookAtTarget.transform.position = (leftEye.position + rightEye.position) / 2f;
                }
            }
            return lookAtTarget;
        }

        private (CameraNode lastNode, int lastPathIndex, int lastIndex) GetLastNode()
        {
            var nodeComponents = _dollyDoll.Nodes;
            DollyDollCameraNode lastNodeComponent = nodeComponents.LastOrDefault();
            CameraNode lastNode = lastNodeComponent != null ? lastNodeComponent.ToCameraNode() : new CameraNode();
            int lastPathIndex = lastNode.PathIndex;
            int lastIndex = lastNodeComponent != null ? lastNode.Index : -1;
            return (lastNode, lastPathIndex, lastIndex);
        }

        public void Dispose()
        {
            _disposables?.Dispose();
            _dollyDoll = null;
        }
    }
}