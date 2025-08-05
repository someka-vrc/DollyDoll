using UnityEditor;
using UnityEngine;

namespace Somekasu.DollyDoll
{
    /// <summary>
    /// DollyDollのスプラインに付与するコンポーネント。
    /// </summary>
    public class DollyDollSpline : DollyDollObject
    {
        [SerializeField]
        internal int PathIndex = 0;
    }

    [CustomEditor(typeof(DollyDollSpline))]
    public class DollyDollSplineEditor : DollyDollObjectEditor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DrawDefaultInspector();
        }
    }
}