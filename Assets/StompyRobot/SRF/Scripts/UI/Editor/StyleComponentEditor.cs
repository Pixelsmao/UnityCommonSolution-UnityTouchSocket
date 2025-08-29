using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SRF.UI.Editor
{
    [CustomEditor(typeof(StyleComponent))]
    [CanEditMultipleObjects]
    public class StyleComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty _styleKeyProperty;

        protected void OnEnable()
        {
            this._styleKeyProperty = this.serializedObject.FindProperty("_styleKey");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var styleComponent = this.serializedObject.targetObject as StyleComponent;

            if (styleComponent == null)
            {
                Debug.LogWarning("Target is null, expected StyleComponent");
                return;
            }

            var styleRoot = styleComponent.GetComponentInParent<StyleRoot>();

            if (styleRoot == null)
            {
                EditorGUILayout.HelpBox("There must be a StyleRoot component above this object in the hierarchy.",
                    MessageType.Error,
                    true);

                return;
            }

            var styleSheet = styleRoot.StyleSheet;

            if (styleSheet == null)
            {
                EditorGUILayout.HelpBox("Style Root has no stylesheet set.", MessageType.Warning);

                EditorGUILayout.Popup("Key", 0,
                    new[] { string.IsNullOrEmpty(styleComponent.StyleKey) ? "--" : styleComponent.StyleKey });

                return;
            }

            var options = styleRoot.StyleSheet.GetStyleKeys(true).ToList();

            var index = this._styleKeyProperty.hasMultipleDifferentValues
                ? 0
                : options.IndexOf(this._styleKeyProperty.stringValue) + 1;

            options.Insert(0, "--");

            EditorGUILayout.Separator();

            GUI.enabled = this._styleKeyProperty.editable;
            var newIndex = EditorGUILayout.Popup("Key", index, options.ToArray());
            GUI.enabled = true;

            if (newIndex != index)
            {
                this._styleKeyProperty.stringValue = "";
                this._styleKeyProperty.stringValue = newIndex == 0 ? "" : options[newIndex];
            }

            if (this.serializedObject.ApplyModifiedProperties())
            {
                foreach (var o in this.serializedObject.targetObjects)
                {
                    var c = o as StyleComponent;
                    c.Refresh(true);
                }

                this._styleKeyProperty.serializedObject.Update();
            }

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Open StyleSheet"))
            {
                Selection.activeObject = styleRoot.StyleSheet;
            }

            EditorGUILayout.Separator();

            if (GUILayout.Button("Select StyleRoot"))
            {
                Selection.activeGameObject = styleRoot.gameObject;
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();
        }
    }
}
