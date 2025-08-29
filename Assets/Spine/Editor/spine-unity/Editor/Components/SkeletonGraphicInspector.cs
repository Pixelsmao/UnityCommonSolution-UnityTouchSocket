/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated January 1, 2020. Replaces all prior versions.
 *
 * Copyright (c) 2013-2020, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES,
 * BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

#if UNITY_2018_3 || UNITY_2019 || UNITY_2018_3_OR_NEWER
#define NEW_PREFAB_SYSTEM
#endif

using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    using Icons = SpineEditorUtilities.Icons;

    [CustomEditor(typeof(SkeletonGraphic))]
    [CanEditMultipleObjects]
    public class SkeletonGraphicInspector : UnityEditor.Editor
    {

        private const string SeparatorSlotNamesFieldName = "separatorSlotNames";
        private const string ReloadButtonString = "Reload";
        protected GUIContent SkeletonDataAssetLabel;
        private static GUILayoutOption reloadButtonWidth;
        private static GUILayoutOption ReloadButtonWidth
        { get { return reloadButtonWidth = reloadButtonWidth ?? GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent(ReloadButtonString)).x + 20); } }
        private static GUIStyle ReloadButtonStyle
        { get { return EditorStyles.miniButton; } }

        private SerializedProperty material, color;
        private SerializedProperty skeletonDataAsset, initialSkinName;
        private SerializedProperty startingAnimation;
        private SerializedProperty startingLoop;
        private SerializedProperty timeScale;
        private SerializedProperty freeze;
        private SerializedProperty updateWhenInvisible;
        private SerializedProperty unscaledTime;
        private readonly SerializedProperty tintBlack;
        private SerializedProperty initialFlipX, initialFlipY;
        private SerializedProperty meshGeneratorSettings;
        private SerializedProperty allowMultipleCanvasRenderers, separatorSlotNames, enableSeparatorSlots, updateSeparatorPartLocation;
        private SerializedProperty raycastTarget;

        private SkeletonGraphic thisSkeletonGraphic;
        protected bool isInspectingPrefab;
        protected bool slotsReapplyRequired = false;
        protected bool forceReloadQueued = false;

        protected bool TargetIsValid
        {
            get
            {
                if (this.serializedObject.isEditingMultipleObjects)
                {
                    foreach (var o in this.targets)
                    {
                        var component = (SkeletonGraphic)o;
                        if (!component.IsValid)
                            return false;
                    }
                    return true;
                }
                else
                {
                    var component = (SkeletonGraphic)this.target;
                    return component.IsValid;
                }
            }
        }

        private void OnEnable()
        {
#if NEW_PREFAB_SYSTEM
            this.isInspectingPrefab = false;
#else
			isInspectingPrefab = (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab);
#endif
            SpineEditorUtilities.ConfirmInitialization();

            // Labels
            this.SkeletonDataAssetLabel = new GUIContent("SkeletonData Asset", Icons.spine);

            var so = this.serializedObject;
            this.thisSkeletonGraphic = this.target as SkeletonGraphic;

            // MaskableGraphic
            this.material = so.FindProperty("m_Material");
            this.color = so.FindProperty("m_Color");
            this.raycastTarget = so.FindProperty("m_RaycastTarget");

            // SkeletonRenderer
            this.skeletonDataAsset = so.FindProperty("skeletonDataAsset");
            this.initialSkinName = so.FindProperty("initialSkinName");

            this.initialFlipX = so.FindProperty("initialFlipX");
            this.initialFlipY = so.FindProperty("initialFlipY");

            // SkeletonAnimation
            this.startingAnimation = so.FindProperty("startingAnimation");
            this.startingLoop = so.FindProperty("startingLoop");
            this.timeScale = so.FindProperty("timeScale");
            this.unscaledTime = so.FindProperty("unscaledTime");
            this.freeze = so.FindProperty("freeze");
            this.updateWhenInvisible = so.FindProperty("updateWhenInvisible");

            this.meshGeneratorSettings = so.FindProperty("meshGenerator").FindPropertyRelative("settings");
            this.meshGeneratorSettings.isExpanded = SkeletonRendererInspector.advancedFoldout;

            this.allowMultipleCanvasRenderers = so.FindProperty("allowMultipleCanvasRenderers");
            this.updateSeparatorPartLocation = so.FindProperty("updateSeparatorPartLocation");
            this.enableSeparatorSlots = so.FindProperty("enableSeparatorSlots");

            this.separatorSlotNames = so.FindProperty("separatorSlotNames");
            this.separatorSlotNames.isExpanded = true;
        }

        public override void OnInspectorGUI()
        {

            if (UnityEngine.Event.current.type == EventType.Layout)
            {
                if (this.forceReloadQueued)
                {
                    this.forceReloadQueued = false;
                    foreach (var c in this.targets)
                    {
                        SpineEditorUtilities.ReloadSkeletonDataAssetAndComponent(c as SkeletonGraphic);
                    }
                }
                else
                {
                    foreach (var c in this.targets)
                    {
                        var component = c as SkeletonGraphic;
                        if (!component.IsValid)
                        {
                            SpineEditorUtilities.ReinitializeComponent(component);
                            if (!component.IsValid) continue;
                        }
                    }
                }
            }

            var wasChanged = false;
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                SpineInspectorUtility.PropertyFieldFitLabel(this.skeletonDataAsset, this.SkeletonDataAssetLabel);
                if (GUILayout.Button(ReloadButtonString, ReloadButtonStyle, ReloadButtonWidth))
                    this.forceReloadQueued = true;
            }

            EditorGUILayout.PropertyField(this.material);
            EditorGUILayout.PropertyField(this.color);

            if (this.thisSkeletonGraphic.skeletonDataAsset == null)
            {
                EditorGUILayout.HelpBox("You need to assign a SkeletonDataAsset first.", MessageType.Info);
                this.serializedObject.ApplyModifiedProperties();
                this.serializedObject.Update();
                return;
            }

            string errorMessage = null;
            if (SpineEditorUtilities.Preferences.componentMaterialWarning &&
                MaterialChecks.IsMaterialSetupProblematic(this.thisSkeletonGraphic, ref errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error, true);
            }

            var isSingleRendererOnly = (!this.allowMultipleCanvasRenderers.hasMultipleDifferentValues && this.allowMultipleCanvasRenderers.boolValue == false);
            var isSeparationEnabledButNotMultipleRenderers =
                 isSingleRendererOnly && (!this.enableSeparatorSlots.hasMultipleDifferentValues && this.enableSeparatorSlots.boolValue == true);
            var meshRendersIncorrectlyWithSingleRenderer =
                isSingleRendererOnly && this.SkeletonHasMultipleSubmeshes();

            if (isSeparationEnabledButNotMultipleRenderers || meshRendersIncorrectlyWithSingleRenderer)
                this.meshGeneratorSettings.isExpanded = true;

            using (new SpineInspectorUtility.BoxScope())
            {

                EditorGUILayout.PropertyField(this.meshGeneratorSettings, SpineInspectorUtility.TempContent("Advanced..."), includeChildren: true);
                SkeletonRendererInspector.advancedFoldout = this.meshGeneratorSettings.isExpanded;

                if (this.meshGeneratorSettings.isExpanded)
                {
                    EditorGUILayout.Space();
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(this.allowMultipleCanvasRenderers, SpineInspectorUtility.TempContent("Multiple CanvasRenderers"));

                        if (GUILayout.Button(new GUIContent("Trim Renderers", "Remove currently unused CanvasRenderer GameObjects. These will be regenerated whenever needed."),
                            EditorStyles.miniButton, GUILayout.Width(100f)))
                        {

                            foreach (var skeletonGraphic in this.targets)
                            {
                                ((SkeletonGraphic)skeletonGraphic).TrimRenderers();
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.PropertyField(this.updateWhenInvisible);

                        // warning box
                        if (isSeparationEnabledButNotMultipleRenderers)
                        {
                            using (new SpineInspectorUtility.BoxScope())
                            {
                                this.meshGeneratorSettings.isExpanded = true;
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("'Multiple Canvas Renderers' must be enabled\nwhen 'Enable Separation' is enabled.", Icons.warning), GUILayout.Height(42), GUILayout.Width(340));
                            }
                        }
                        else if (meshRendersIncorrectlyWithSingleRenderer)
                        {
                            using (new SpineInspectorUtility.BoxScope())
                            {
                                this.meshGeneratorSettings.isExpanded = true;
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("This mesh uses multiple atlas pages. You\n" +
                                                                                            "need to enable 'Multiple Canvas Renderers'\n" +
                                                                                            "for correct rendering. Consider packing\n" +
                                                                                            "attachments to a single atlas page if possible.", Icons.warning), GUILayout.Height(60), GUILayout.Width(340));
                            }
                        }
                    }

                    EditorGUILayout.Space();
                    SeparatorsField(this.separatorSlotNames, this.enableSeparatorSlots, this.updateSeparatorPartLocation);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.initialSkinName);
            {
                var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PrefixLabel(rect, SpineInspectorUtility.TempContent("Initial Flip"));
                rect.x += EditorGUIUtility.labelWidth;
                rect.width = 30f;
                SpineInspectorUtility.ToggleLeft(rect, this.initialFlipX, SpineInspectorUtility.TempContent("X", tooltip: "initialFlipX"));
                rect.x += 35f;
                SpineInspectorUtility.ToggleLeft(rect, this.initialFlipY, SpineInspectorUtility.TempContent("Y", tooltip: "initialFlipY"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.startingAnimation);
            EditorGUILayout.PropertyField(this.startingLoop);
            EditorGUILayout.PropertyField(this.timeScale);
            EditorGUILayout.PropertyField(this.unscaledTime, SpineInspectorUtility.TempContent(this.unscaledTime.displayName, tooltip: "If checked, this will use Time.unscaledDeltaTime to make this update independent of game Time.timeScale. Instance SkeletonGraphic.timeScale will still be applied."));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.freeze);
            EditorGUILayout.Space();
            SkeletonRendererInspector.SkeletonRootMotionParameter(this.targets);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(this.raycastTarget);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight + 5));
            EditorGUILayout.PrefixLabel("Match RectTransform with Mesh");
            if (GUILayout.Button("Match", EditorStyles.miniButton, GUILayout.Width(65f)))
            {
                foreach (var skeletonGraphic in this.targets)
                {
                    MatchRectTransformWithBounds((SkeletonGraphic)skeletonGraphic);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (this.TargetIsValid && !this.isInspectingPrefab)
            {
                EditorGUILayout.Space();
                if (SpineInspectorUtility.CenteredButton(new GUIContent("Add Skeleton Utility", Icons.skeletonUtility), 21, true, 200f))
                    foreach (var t in this.targets)
                    {
                        var component = t as Component;
                        if (component.GetComponent<SkeletonUtility>() == null)
                        {
                            component.gameObject.AddComponent<SkeletonUtility>();
                        }
                    }
            }

            wasChanged |= EditorGUI.EndChangeCheck();

            if (wasChanged)
            {
                this.serializedObject.ApplyModifiedProperties();
                this.slotsReapplyRequired = true;
            }

            if (this.slotsReapplyRequired && UnityEngine.Event.current.type == EventType.Repaint)
            {
                foreach (var target in this.targets)
                {
                    var skeletonGraphic = (SkeletonGraphic)target;
                    skeletonGraphic.ReapplySeparatorSlotNames();
                    skeletonGraphic.LateUpdate();
                    SceneView.RepaintAll();
                }
                this.slotsReapplyRequired = false;
            }
        }

        protected bool SkeletonHasMultipleSubmeshes()
        {
            foreach (var target in this.targets)
            {
                var skeletonGraphic = (SkeletonGraphic)target;
                if (skeletonGraphic.HasMultipleSubmeshInstructions())
                    return true;
            }
            return false;
        }

        public static void SetSeparatorSlotNames(SkeletonRenderer skeletonRenderer, string[] newSlotNames)
        {
            var field = SpineInspectorUtility.GetNonPublicField(typeof(SkeletonRenderer), SeparatorSlotNamesFieldName);
            field.SetValue(skeletonRenderer, newSlotNames);
        }

        public static string[] GetSeparatorSlotNames(SkeletonRenderer skeletonRenderer)
        {
            var field = SpineInspectorUtility.GetNonPublicField(typeof(SkeletonRenderer), SeparatorSlotNamesFieldName);
            return field.GetValue(skeletonRenderer) as string[];
        }

        public static void SeparatorsField(SerializedProperty separatorSlotNames, SerializedProperty enableSeparatorSlots,
            SerializedProperty updateSeparatorPartLocation)
        {

            var multi = separatorSlotNames.serializedObject.isEditingMultipleObjects;
            var hasTerminalSlot = false;
            if (!multi)
            {
                var sr = separatorSlotNames.serializedObject.targetObject as ISkeletonComponent;
                var skeleton = sr.Skeleton;
                var lastSlot = skeleton.Slots.Count - 1;
                if (skeleton != null)
                {
                    for (int i = 0, n = separatorSlotNames.arraySize; i < n; i++)
                    {
                        var index = skeleton.FindSlotIndex(separatorSlotNames.GetArrayElementAtIndex(i).stringValue);
                        if (index == 0 || index == lastSlot)
                        {
                            hasTerminalSlot = true;
                            break;
                        }
                    }
                }
            }

            var terminalSlotWarning = hasTerminalSlot ? " (!)" : "";

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                const string SeparatorsDescription = "Stored names of slots where the Skeleton's render will be split into different batches. This is used by separate components that split the render into different MeshRenderers or GameObjects.";
                if (separatorSlotNames.isExpanded)
                {
                    EditorGUILayout.PropertyField(separatorSlotNames, SpineInspectorUtility.TempContent(separatorSlotNames.displayName + terminalSlotWarning, Icons.slotRoot, SeparatorsDescription), true);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+", GUILayout.MaxWidth(28f), GUILayout.MaxHeight(15f)))
                    {
                        separatorSlotNames.arraySize++;
                    }
                    GUILayout.EndHorizontal();
                }
                else
                    EditorGUILayout.PropertyField(separatorSlotNames, new GUIContent(separatorSlotNames.displayName + string.Format("{0} [{1}]", terminalSlotWarning, separatorSlotNames.arraySize), SeparatorsDescription), true);

                EditorGUILayout.PropertyField(enableSeparatorSlots, SpineInspectorUtility.TempContent("Enable Separation", tooltip: "Whether to enable separation at the above separator slots."));
                EditorGUILayout.PropertyField(updateSeparatorPartLocation, SpineInspectorUtility.TempContent("Update Part Location", tooltip: "Update separator part GameObject location to match the position of the SkeletonGraphic. This can be helpful when re-parenting parts to a different GameObject."));
            }
        }

        #region Menus
        [MenuItem("CONTEXT/SkeletonGraphic/Match RectTransform with Mesh Bounds")]
        private static void MatchRectTransformWithBounds(MenuCommand command)
        {
            var skeletonGraphic = (SkeletonGraphic)command.context;
            MatchRectTransformWithBounds(skeletonGraphic);
        }

        private static void MatchRectTransformWithBounds(SkeletonGraphic skeletonGraphic)
        {
            if (!skeletonGraphic.MatchRectTransformWithBounds())
                Debug.Log("Mesh was not previously generated.");
        }

        [MenuItem("GameObject/Spine/SkeletonGraphic (UnityUI)", false, 15)]
        public static void SkeletonGraphicCreateMenuItem()
        {
            var parentGameObject = Selection.activeObject as GameObject;
            var parentTransform = parentGameObject == null ? null : parentGameObject.GetComponent<RectTransform>();

            if (parentTransform == null)
                Debug.LogWarning("Your new SkeletonGraphic will not be visible until it is placed under a Canvas");

            var gameObject = NewSkeletonGraphicGameObject("New SkeletonGraphic");
            gameObject.transform.SetParent(parentTransform, false);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = gameObject;
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        // SpineEditorUtilities.InstantiateDelegate. Used by drag and drop.
        public static Component SpawnSkeletonGraphicFromDrop(SkeletonDataAsset data)
        {
            return InstantiateSkeletonGraphic(data);
        }

        public static SkeletonGraphic InstantiateSkeletonGraphic(SkeletonDataAsset skeletonDataAsset, string skinName)
        {
            return InstantiateSkeletonGraphic(skeletonDataAsset, skeletonDataAsset.GetSkeletonData(true).FindSkin(skinName));
        }

        public static SkeletonGraphic InstantiateSkeletonGraphic(SkeletonDataAsset skeletonDataAsset, Skin skin = null)
        {
            var spineGameObjectName = string.Format("SkeletonGraphic ({0})", skeletonDataAsset.name.Replace("_SkeletonData", ""));
            var go = NewSkeletonGraphicGameObject(spineGameObjectName);
            var graphic = go.GetComponent<SkeletonGraphic>();
            graphic.skeletonDataAsset = skeletonDataAsset;

            var data = skeletonDataAsset.GetSkeletonData(true);

            if (data == null)
            {
                for (var i = 0; i < skeletonDataAsset.atlasAssets.Length; i++)
                {
                    var reloadAtlasPath = AssetDatabase.GetAssetPath(skeletonDataAsset.atlasAssets[i]);
                    skeletonDataAsset.atlasAssets[i] = (AtlasAssetBase)AssetDatabase.LoadAssetAtPath(reloadAtlasPath, typeof(AtlasAssetBase));
                }

                data = skeletonDataAsset.GetSkeletonData(true);
            }

            skin = skin ?? data.DefaultSkin ?? data.Skins.Items[0];
            graphic.MeshGenerator.settings.zSpacing = SpineEditorUtilities.Preferences.defaultZSpacing;

            graphic.startingLoop = SpineEditorUtilities.Preferences.defaultInstantiateLoop;
            graphic.Initialize(false);
            if (skin != null) graphic.Skeleton.SetSkin(skin);
            graphic.initialSkinName = skin.Name;
            graphic.Skeleton.UpdateWorldTransform();
            graphic.UpdateMesh();
            return graphic;
        }

        private static GameObject NewSkeletonGraphicGameObject(string gameObjectName)
        {
            var go = EditorInstantiation.NewGameObject(gameObjectName, true, typeof(RectTransform), typeof(CanvasRenderer), typeof(SkeletonGraphic));
            var graphic = go.GetComponent<SkeletonGraphic>();
            graphic.material = SkeletonGraphicInspector.DefaultSkeletonGraphicMaterial;
            return go;
        }

        public static Material DefaultSkeletonGraphicMaterial
        {
            get
            {
                var guids = AssetDatabase.FindAssets("SkeletonGraphicDefault t:material");
                if (guids.Length <= 0) return null;

                var firstAssetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                if (string.IsNullOrEmpty(firstAssetPath)) return null;

                var firstMaterial = AssetDatabase.LoadAssetAtPath<Material>(firstAssetPath);
                return firstMaterial;
            }
        }

        #endregion
    }
}
