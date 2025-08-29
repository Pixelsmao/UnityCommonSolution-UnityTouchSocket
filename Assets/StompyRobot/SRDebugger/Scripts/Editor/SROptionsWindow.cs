

namespace SRDebugger.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SRF;
    using UnityEngine;
    using UnityEditor;
    using SRF.Helpers;
#if !DISABLE_SRDEBUGGER
    using Internal;
    using SRDebugger.Services;
    using UI.Controls.Data;
#endif

    internal class SROptionsWindow : EditorWindow
    {
        [MenuItem(SRDebugEditorPaths.SROptionsMenuItemPath)]
        public static void Open()
        {
            var window = GetWindow<SROptionsWindow>(false, "SROptions", true);
            window.minSize = new Vector2(100, 100);
            window.Show();
        }

#if DISABLE_SRDEBUGGER
        private bool _isWorking;

        void OnGUI()
        {
            SRDebugEditor.DrawDisabledWindowGui(ref _isWorking);
        }
#else
        [Serializable]
        private class CategoryState
        {
            public string Name;
            public bool IsOpen;
        }

        [SerializeField]
        private List<CategoryState> _categoryStates = new List<CategoryState>();

        private Dictionary<Type, Action<OptionDefinition>> _typeLookup;
        private Dictionary<string, List<OptionDefinition>> _options;

        private Vector2 _scrollPosition;
        private bool _queueRefresh;
        private bool _isDirty;


        [NonSerialized] private GUIStyle _divider;
        [NonSerialized] private GUIStyle _foldout;

        private IOptionsService _activeOptionsService;

        public void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying && (this._options == null || this._isDirty))
            {
                this.Populate();
                this._queueRefresh = true;
                this._isDirty = false;
            }
            else if (!EditorApplication.isPlaying && this._options != null)
            {
                this.Clear();
                this._queueRefresh = true;
            }

            if (this._queueRefresh)
            {
                this.Repaint();
            }

            this._queueRefresh = false;
        }

        private void OnDisable()
        {
            this.Clear();
        }

        private void PopulateTypeLookup()
        {
            this._typeLookup = new Dictionary<Type, Action<OptionDefinition>>()
            {
                {typeof(int), this.OnGUI_Int},
                {typeof(float), this.OnGUI_Float},
                {typeof(double), this.OnGUI_Double},
                {typeof(string), this.OnGUI_String},
                {typeof(bool), this.OnGUI_Boolean },
                {typeof(uint), this.OnGUI_AnyInteger},
                {typeof(ushort), this.OnGUI_AnyInteger},
                {typeof(short), this.OnGUI_AnyInteger},
                {typeof(sbyte), this.OnGUI_AnyInteger},
                {typeof(byte), this.OnGUI_AnyInteger},
                {typeof(long), this.OnGUI_AnyInteger},
            };
        }

        private void Clear()
        {
            this._options = null;
            this._isDirty = false;

            if (this._activeOptionsService != null)
            {
                this._activeOptionsService.OptionsUpdated -= this.OnOptionsUpdated;
            }

            this._activeOptionsService = null;
        }

        private void Populate()
        {
            if (this._typeLookup == null)
            {
                this.PopulateTypeLookup();
            }

            if (this._activeOptionsService != null)
            {
                this._activeOptionsService.OptionsUpdated -= this.OnOptionsUpdated;
            }

            if (this._options != null)
            {
                foreach (var kv in this._options)
                {
                    foreach (var option in kv.Value)
                    {
                        if (option.IsProperty)
                        {
                            option.Property.ValueChanged -= this.OnOptionPropertyValueChanged;
                        }
                    }
                }
            }

            this._options = new Dictionary<string, List<OptionDefinition>>();

            foreach (var option in Service.Options.Options)
            {

                if (!this._options.TryGetValue(option.Category, out var list))
                {
                    list = new List<OptionDefinition>();
                    this._options[option.Category] = list;
                }

                list.Add(option);

                if (option.IsProperty)
                {
                    option.Property.ValueChanged += this.OnOptionPropertyValueChanged;
                }
            }

            foreach (var kv in this._options)
            {
                kv.Value.Sort((d1, d2) => d1.SortPriority.CompareTo(d2.SortPriority));
            }

            this._activeOptionsService = Service.Options;
            this._activeOptionsService.OptionsUpdated += this.OnOptionsUpdated;
        }

        private void OnOptionPropertyValueChanged(PropertyReference property)
        {
            this._queueRefresh = true;
        }

        private void OnOptionsUpdated(object sender, EventArgs e)
        {
            this._isDirty = true;
            this._queueRefresh = true;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            if (!EditorApplication.isPlayingOrWillChangePlaymode || this._options == null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("SROptions can only be edited in play-mode.");
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (this._divider == null)
            {
                this._divider = new GUIStyle(GUI.skin.box);
                this._divider.stretchWidth = true;
                this._divider.fixedHeight = 2;
            }

            if (this._foldout == null)
            {
                this._foldout = new GUIStyle(EditorStyles.foldout);
                this._foldout.fontStyle = FontStyle.Bold;
            }

            this._scrollPosition = EditorGUILayout.BeginScrollView(this._scrollPosition);

            foreach (var kv in this._options)
            {
                var state = this._categoryStates.FirstOrDefault(p => p.Name == kv.Key);

                if (state == null)
                {
                    state = new CategoryState()
                    {
                        Name = kv.Key,
                        IsOpen = true
                    };
                    this._categoryStates.Add(state);
                }

                state.IsOpen = EditorGUILayout.Foldout(state.IsOpen, kv.Key, this._foldout);

                if (!state.IsOpen)
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
                this.OnGUI_Category(kv.Value);
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnGUI_Category(List<OptionDefinition> options)
        {
            for (var i = 0; i < options.Count; i++)
            {
                var op = options[i];

                if (op.Property != null)
                {
                    this.OnGUI_Property(op);
                }
                else if (op.Method != null)
                {
                    this.OnGUI_Method(op);
                }
            }
        }

        private void OnGUI_Method(OptionDefinition op)
        {
            if (GUILayout.Button(op.Name))
            {
                op.Method.Invoke(null);
            }
        }

        private void OnGUI_Property(OptionDefinition op)
        {
            Action<OptionDefinition> method;

            if (op.Property.PropertyType.IsEnum)
            {
                method = this.OnGUI_Enum;
            }
            else if (!this._typeLookup.TryGetValue(op.Property.PropertyType, out method))
            {
                this.OnGUI_Unsupported(op);
                return;
            }

            if (!op.Property.CanWrite)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            method(op);

            if (!op.Property.CanWrite)
            {
                EditorGUI.EndDisabledGroup();
            }
        }

        private void OnGUI_String(OptionDefinition op)
        {
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.TextField(op.Name, (string)op.Property.GetValue());

            if (EditorGUI.EndChangeCheck())
            {
                op.Property.SetValue(Convert.ChangeType(newValue, op.Property.PropertyType));
            }
        }

        private void OnGUI_Boolean(OptionDefinition op)
        {
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.Toggle(op.Name, (bool)op.Property.GetValue());

            if (EditorGUI.EndChangeCheck())
            {
                op.Property.SetValue(Convert.ChangeType(newValue, op.Property.PropertyType));
            }
        }

        private void OnGUI_Enum(OptionDefinition op)
        {
            EditorGUI.BeginChangeCheck();
            var newValue = EditorGUILayout.EnumPopup(op.Name, (Enum)op.Property.GetValue());

            if (EditorGUI.EndChangeCheck())
            {
                op.Property.SetValue(Convert.ChangeType(newValue, op.Property.PropertyType));
            }
        }

        private void OnGUI_Int(OptionDefinition op)
        {
            var range = op.Property.GetAttribute<NumberRangeAttribute>();

            int newValue;

            EditorGUI.BeginChangeCheck();

            if (range != null)
            {
                newValue = EditorGUILayout.IntSlider(op.Name, (int)op.Property.GetValue(), (int)range.Min, (int)range.Max);
            }
            else
            {
                newValue = EditorGUILayout.IntField(op.Name, (int)op.Property.GetValue());
            }

            if (EditorGUI.EndChangeCheck())
            {
                op.Property.SetValue(Convert.ChangeType(newValue, op.Property.PropertyType));
            }
        }

        private void OnGUI_Float(OptionDefinition op)
        {
            var range = op.Property.GetAttribute<NumberRangeAttribute>();

            float newValue;

            EditorGUI.BeginChangeCheck();

            if (range != null)
            {
                newValue = EditorGUILayout.Slider(op.Name, (float)op.Property.GetValue(), (float)range.Min, (float)range.Max);
            }
            else
            {
                newValue = EditorGUILayout.FloatField(op.Name, (float)op.Property.GetValue());
            }

            if (EditorGUI.EndChangeCheck())
            {
                op.Property.SetValue(Convert.ChangeType(newValue, op.Property.PropertyType));
            }
        }

        private void OnGUI_Double(OptionDefinition op)
        {
            var range = op.Property.GetAttribute<NumberRangeAttribute>();

            double newValue;

            EditorGUI.BeginChangeCheck();

            if (range != null && range.Min > float.MinValue && range.Max < float.MaxValue)
            {
                newValue = EditorGUILayout.Slider(op.Name, (float)op.Property.GetValue(), (float)range.Min, (float)range.Max);
            }
            else
            {
                newValue = EditorGUILayout.DoubleField(op.Name, (double)op.Property.GetValue());

                if (range != null)
                {
                    if (newValue > range.Max)
                    {
                        newValue = range.Max;
                    }
                    else if (newValue < range.Min)
                    {
                        newValue = range.Min;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                op.Property.SetValue(Convert.ChangeType(newValue, op.Property.PropertyType));
            }
        }


        private void OnGUI_AnyInteger(OptionDefinition op)
        {

            if (!NumberControl.ValueRanges.TryGetValue(op.Property.PropertyType, out var range))
            {
                Debug.LogError("Unknown integer type: " + op.Property.PropertyType);
                return;
            }

            var userRange = op.Property.GetAttribute<NumberRangeAttribute>();

            EditorGUI.BeginChangeCheck();

            var oldValue = (long)Convert.ChangeType(op.Property.GetValue(), typeof(long));
            var newValue = EditorGUILayout.LongField(op.Name, oldValue);

            if (newValue > range.MaxValue)
            {
                newValue = (long)range.MaxValue;
            }
            else if (newValue < range.MinValue)
            {
                newValue = (long)range.MinValue;
            }

            if (userRange != null)
            {
                if (newValue > userRange.Max)
                {
                    newValue = (long)userRange.Max;
                }
                else if (newValue < userRange.Min)
                {
                    newValue = (long)userRange.Min;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                op.Property.SetValue(Convert.ChangeType(newValue, op.Property.PropertyType));
            }
        }

        private void OnGUI_Unsupported(OptionDefinition op)
        {
            EditorGUILayout.PrefixLabel(op.Name);
            EditorGUILayout.LabelField("Unsupported Type: {0}".Fmt(op.Property.PropertyType));
        }
#endif
    }
}
