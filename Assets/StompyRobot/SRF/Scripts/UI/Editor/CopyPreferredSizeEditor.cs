using UnityEditor;

namespace SRF.UI.Editor
{
    [CustomEditor(typeof(CopyPreferredSize))]
    [CanEditMultipleObjects]
    public class CopyPreferredSizeEditor : UnityEditor.Editor
    {
        private SerializedProperty _copySourceProperty;
        private SerializedProperty _paddingHeightProperty;
        private SerializedProperty _paddingWidthProperty;

        protected void OnEnable()
        {
            this._paddingWidthProperty = this.serializedObject.FindProperty("PaddingWidth");
            this._paddingHeightProperty = this.serializedObject.FindProperty("PaddingHeight");
            this._copySourceProperty = this.serializedObject.FindProperty("CopySource");
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(this._copySourceProperty);
            EditorGUILayout.PropertyField(this._paddingWidthProperty);
            EditorGUILayout.PropertyField(this._paddingHeightProperty);
            this.serializedObject.ApplyModifiedProperties();

            this.serializedObject.Update();
        }
    }
}
