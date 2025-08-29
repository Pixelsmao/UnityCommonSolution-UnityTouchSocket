using UnityEditor;

namespace SRF.UI.Editor
{
    [CustomEditor(typeof(CopyPreferredSizes))]
    [CanEditMultipleObjects]
    public class CopyPreferredSizesEditor : UnityEditor.Editor
    {
        private SerializedProperty _copySourcesProperty;
        private SerializedProperty _operationProperty;

        protected void OnEnable()
        {
            this._copySourcesProperty = this.serializedObject.FindProperty("CopySources");
            this._operationProperty = this.serializedObject.FindProperty("Operation");
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(this._operationProperty);
            EditorGUILayout.PropertyField(this._copySourcesProperty);
            this.serializedObject.ApplyModifiedProperties();

            this.serializedObject.Update();
        }
    }
}