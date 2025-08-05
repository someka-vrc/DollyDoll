using UnityEditor;
using UnityEngine;

namespace Somekasu.DollyDoll
{
    /// <summary>
    /// DollyDollが支配するオブジェクトにつけるコンポーネント。
    /// </summary>
    public class DollyDollObject : MonoBehaviour
    {
        [SerializeField]
        internal DollyDoll DollyDoll;
    }

    [CustomEditor(typeof(DollyDollObject))]
    public class DollyDollObjectEditor : Editor
    {
        // GUIContent生成を簡略化する関数
        internal GUIContent Gc(string prop)
        {
            return new GUIContent(
                I18n.G($"DollyDollCameraNode/{prop}/label"),
                I18n.G($"DollyDollCameraNode/{prop}/tooltip")
            );
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("DollyDoll"), new GUIContent("DollyDoll"));
            serializedObject.ApplyModifiedProperties();
            var node = (DollyDollObject)target;
            var dolly = node.DollyDoll;
            if (dolly == null)
            {
                EditorGUILayout.HelpBox("No DollyDoll reference found", MessageType.Warning);
                return;
            }
        }
    }
}