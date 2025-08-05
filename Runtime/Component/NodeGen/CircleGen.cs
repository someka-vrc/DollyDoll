using UnityEngine;
using System;
using UnityEngine.Animations;
using UnityEditor;

namespace Somekasu.DollyDoll
{
    ///<summary> 円周ノード生成 </summary>
    [System.Serializable]
    public class CircleGen
    {
        ///<summary>半径</summary>
        [SerializeField]
        internal float Radius = 1f;
        ///<summary>ノードごとに半径が増加する量（渦巻き状配置用）</summary>
        [SerializeField]
        internal float RadiusDelta = 0f;
        ///<summary>Y方向の増分 (0で水平円)</summary>
        [SerializeField]
        internal float YDelta = 0f;
        ///<summary>ノード間の角度（度数, 正なら反時計回り、負なら時計回り）</summary>
        [SerializeField]
        internal float ThetaDelta = 45f;
        ///<summary>ノード数（1以上）</summary>
        [SerializeField]
        internal int Count = 8;
        ///<summary>ノードが向くTransform。未指定なら水平に中心を向く</summary>
        [SerializeField]
        internal Transform LookAt = null;


        internal void GenerateCircleNodes(Func<DollyDollCameraNode> nodeCompFactory, Transform rootTransform)
        {

            for (int i = 0; i < Count; i++)
            {
                float angle = i * ThetaDelta;
                float radius = Radius + RadiusDelta * i;
                float y = YDelta * i;
                Vector3 position = rootTransform.position + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    y,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                var comp = nodeCompFactory();
                comp.transform.position = position;
                if (LookAt != null && LookAt)
                {
                    var lookAtComp = comp.gameObject.AddComponent<LookAtConstraint>();
                    lookAtComp.AddSource(new ConstraintSource
                    {
                        sourceTransform = LookAt,
                        weight = 1f
                    });
                    lookAtComp.constraintActive = true;
                }
                else
                {
                    Vector3 target = rootTransform.position;
                    target.y = comp.transform.position.y; // 水平に保つ
                    Vector3 direction = target - comp.transform.position;
                    comp.transform.rotation = (direction == Vector3.zero)
                        ? Quaternion.identity
                        : Quaternion.LookRotation(direction, Vector3.up);
                }
            }
        }
    }
}