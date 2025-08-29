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
#else
#define NO_PREFAB_MESH
#endif

#if UNITY_2018_1_OR_NEWER
#define PER_MATERIAL_PROPERTY_BLOCKS
#endif

#if UNITY_2017_1_OR_NEWER
#define BUILT_IN_SPRITE_MASK_COMPONENT
#endif

using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    using Event = UnityEngine.Event;
    using Icons = SpineEditorUtilities.Icons;

    [CustomEditor(typeof(SkeletonRenderer))]
    [CanEditMultipleObjects]
    public class SkeletonRendererInspector : UnityEditor.Editor
    {
        public static bool advancedFoldout;

        private const string SeparatorSlotNamesFieldName = "separatorSlotNames";

        protected SerializedProperty skeletonDataAsset, initialSkinName;
        protected SerializedProperty initialFlipX, initialFlipY;
        protected SerializedProperty updateWhenInvisible, singleSubmesh, separatorSlotNames, clearStateOnDisable, immutableTriangles, fixDrawOrder;
        protected SerializedProperty normals, tangents, zSpacing, pmaVertexColors, tintBlack; // MeshGenerator settings
        protected SerializedProperty maskInteraction;
        protected SerializedProperty maskMaterialsNone, maskMaterialsInside, maskMaterialsOutside;
        protected SpineInspectorUtility.SerializedSortingProperties sortingProperties;

        protected bool isInspectingPrefab;
        protected bool forceReloadQueued = false;
        protected bool setMaskNoneMaterialsQueued = false;
        protected bool setInsideMaskMaterialsQueued = false;
        protected bool setOutsideMaskMaterialsQueued = false;
        protected bool deleteInsideMaskMaterialsQueued = false;
        protected bool deleteOutsideMaskMaterialsQueued = false;

        protected GUIContent SkeletonDataAssetLabel, SkeletonUtilityButtonContent;
        protected GUIContent PMAVertexColorsLabel, ClearStateOnDisableLabel, ZSpacingLabel, ImmubleTrianglesLabel, TintBlackLabel, UpdateWhenInvisibleLabel, SingleSubmeshLabel, FixDrawOrderLabel;
        protected GUIContent NormalsLabel, TangentsLabel, MaskInteractionLabel;
        protected GUIContent MaskMaterialsHeadingLabel, MaskMaterialsNoneLabel, MaskMaterialsInsideLabel, MaskMaterialsOutsideLabel;
        protected GUIContent SetMaterialButtonLabel, ClearMaterialButtonLabel, DeleteMaterialButtonLabel;

        private const string ReloadButtonString = "Reload";
        private static GUILayoutOption reloadButtonWidth;
        private static GUILayoutOption ReloadButtonWidth
        { get { return reloadButtonWidth = reloadButtonWidth ?? GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent(ReloadButtonString)).x + 20); } }
        private static GUIStyle ReloadButtonStyle
        { get { return EditorStyles.miniButton; } }

        protected bool TargetIsValid
        {
            get
            {
                if (this.serializedObject.isEditingMultipleObjects)
                {
                    foreach (var o in this.targets)
                    {
                        var component = (SkeletonRenderer)o;
                        if (!component.valid)
                            return false;
                    }
                    return true;
                }
                else
                {
                    var component = (SkeletonRenderer)this.target;
                    return component.valid;
                }
            }
        }

        protected virtual void OnEnable()
        {
#if NEW_PREFAB_SYSTEM
            this.isInspectingPrefab = false;
#else
			isInspectingPrefab = (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab);
#endif
            SpineEditorUtilities.ConfirmInitialization();

            // Labels
            this.SkeletonDataAssetLabel = new GUIContent("SkeletonData Asset", Icons.spine);
            this.SkeletonUtilityButtonContent = new GUIContent("Add Skeleton Utility", Icons.skeletonUtility);
            this.ImmubleTrianglesLabel = new GUIContent("Immutable Triangles", "Enable to optimize rendering for skeletons that never change attachment visbility");
            this.PMAVertexColorsLabel = new GUIContent("PMA Vertex Colors", "Use this if you are using the default Spine/Skeleton shader or any premultiply-alpha shader.");
            this.ClearStateOnDisableLabel = new GUIContent("Clear State On Disable", "Use this if you are pooling or enabling/disabling your Spine GameObject.");
            this.ZSpacingLabel = new GUIContent("Z Spacing", "A value other than 0 adds a space between each rendered attachment to prevent Z Fighting when using shaders that read or write to the depth buffer. Large values may cause unwanted parallax and spaces depending on camera setup.");
            this.NormalsLabel = new GUIContent("Add Normals", "Use this if your shader requires vertex normals. A more efficient solution for 2D setups is to modify the shader to assume a single normal value for the whole mesh.");
            this.TangentsLabel = new GUIContent("Solve Tangents", "Calculates the tangents per frame. Use this if you are using lit shaders (usually with normal maps) that require vertex tangents.");
            this.TintBlackLabel = new GUIContent("Tint Black (!)", "Adds black tint vertex data to the mesh as UV2 and UV3. Black tinting requires that the shader interpret UV2 and UV3 as black tint colors for this effect to work. You may also use the default [Spine/Skeleton Tint Black] shader.\n\nIf you only need to tint the whole skeleton and not individual parts, the [Spine/Skeleton Tint] shader is recommended for better efficiency and changing/animating the _Black material property via MaterialPropertyBlock.");
            this.SingleSubmeshLabel = new GUIContent("Use Single Submesh", "Simplifies submesh generation by assuming you are only using one Material and need only one submesh. This is will disable multiple materials, render separation, and custom slot materials.");
            this.UpdateWhenInvisibleLabel = new GUIContent("Update When Invisible", "Update mode used when the MeshRenderer becomes invisible. Update mode is automatically reset to UpdateMode.FullUpdate when the mesh becomes visible again.");
            this.FixDrawOrderLabel = new GUIContent("Fix Draw Order", "Applies only when 3+ submeshes are used (2+ materials with alternating order, e.g. \"A B A\"). If true, GPU instancing will be disabled at all materials and MaterialPropertyBlocks are assigned at each material to prevent aggressive batching of submeshes by e.g. the LWRP renderer, leading to incorrect draw order (e.g. \"A1 B A2\" changed to \"A1A2 B\"). You can disable this parameter when everything is drawn correctly to save the additional performance cost. Note: the GPU instancing setting will remain disabled at affected material assets after exiting play mode, you have to enable it manually if you accidentally enabled this parameter.");
            this.MaskInteractionLabel = new GUIContent("Mask Interaction", "SkeletonRenderer's interaction with a Sprite Mask.");
            this.MaskMaterialsHeadingLabel = new GUIContent("Mask Interaction Materials", "Materials used for different interaction with sprite masks.");
            this.MaskMaterialsNoneLabel = new GUIContent("Normal Materials", "Normal materials used when Mask Interaction is set to None.");
            this.MaskMaterialsInsideLabel = new GUIContent("Inside Mask", "Materials used when Mask Interaction is set to Inside Mask.");
            this.MaskMaterialsOutsideLabel = new GUIContent("Outside Mask", "Materials used when Mask Interaction is set to Outside Mask.");
            this.SetMaterialButtonLabel = new GUIContent("Set", "Prepares material references for switching to the corresponding Mask Interaction mode at runtime. Creates the required materials if they do not exist.");
            this.ClearMaterialButtonLabel = new GUIContent("Clear", "Clears unused material references. Note: when switching to the corresponding Mask Interaction mode at runtime, a new material is generated on the fly.");
            this.DeleteMaterialButtonLabel = new GUIContent("Delete", "Clears unused material references and deletes the corresponding assets. Note: when switching to the corresponding Mask Interaction mode at runtime, a new material is generated on the fly.");

            var so = this.serializedObject;
            this.skeletonDataAsset = so.FindProperty("skeletonDataAsset");
            this.initialSkinName = so.FindProperty("initialSkinName");
            this.initialFlipX = so.FindProperty("initialFlipX");
            this.initialFlipY = so.FindProperty("initialFlipY");
            this.normals = so.FindProperty("addNormals");
            this.tangents = so.FindProperty("calculateTangents");
            this.immutableTriangles = so.FindProperty("immutableTriangles");
            this.pmaVertexColors = so.FindProperty("pmaVertexColors");
            this.clearStateOnDisable = so.FindProperty("clearStateOnDisable");
            this.tintBlack = so.FindProperty("tintBlack");
            this.updateWhenInvisible = so.FindProperty("updateWhenInvisible");
            this.singleSubmesh = so.FindProperty("singleSubmesh");
            this.fixDrawOrder = so.FindProperty("fixDrawOrder");
            this.maskInteraction = so.FindProperty("maskInteraction");
            this.maskMaterialsNone = so.FindProperty("maskMaterials.materialsMaskDisabled");
            this.maskMaterialsInside = so.FindProperty("maskMaterials.materialsInsideMask");
            this.maskMaterialsOutside = so.FindProperty("maskMaterials.materialsOutsideMask");

            this.separatorSlotNames = so.FindProperty("separatorSlotNames");
            this.separatorSlotNames.isExpanded = true;

            this.zSpacing = so.FindProperty("zSpacing");

            var renderersSerializedObject = SpineInspectorUtility.GetRenderersSerializedObject(this.serializedObject); // Allows proper multi-edit behavior.
            this.sortingProperties = new SpineInspectorUtility.SerializedSortingProperties(renderersSerializedObject);
        }

        public void OnSceneGUI()
        {
            var skeletonRenderer = (SkeletonRenderer)this.target;
            var skeleton = skeletonRenderer.Skeleton;
            var transform = skeletonRenderer.transform;
            if (skeleton == null) return;

            SpineHandles.DrawBones(transform, skeleton);
        }

        public override void OnInspectorGUI()
        {
            var multi = this.serializedObject.isEditingMultipleObjects;
            this.DrawInspectorGUI(multi);
            this.HandleSkinChange();
            if (this.serializedObject.ApplyModifiedProperties() || SpineInspectorUtility.UndoRedoPerformed(Event.current) ||
                this.AreAnyMaskMaterialsMissing())
            {
                if (!Application.isPlaying)
                {
                    foreach (var o in this.targets)
                        SpineEditorUtilities.ReinitializeComponent((SkeletonRenderer)o);
                    SceneView.RepaintAll();
                }
            }
        }

        protected virtual void DrawInspectorGUI(bool multi)
        {
            // Initialize.
            if (Event.current.type == EventType.Layout)
            {
                if (this.forceReloadQueued)
                {
                    this.forceReloadQueued = false;
                    foreach (var c in this.targets)
                    {
                        SpineEditorUtilities.ReloadSkeletonDataAssetAndComponent(c as SkeletonRenderer);
                    }
                }
                else
                {
                    foreach (var c in this.targets)
                    {
                        var component = c as SkeletonRenderer;
                        if (!component.valid)
                        {
                            SpineEditorUtilities.ReinitializeComponent(component);
                            if (!component.valid) continue;
                        }
                    }
                }

#if BUILT_IN_SPRITE_MASK_COMPONENT
                if (this.setMaskNoneMaterialsQueued)
                {
                    this.setMaskNoneMaterialsQueued = false;
                    foreach (var c in this.targets)
                        EditorSetMaskMaterials(c as SkeletonRenderer, SpriteMaskInteraction.None);
                }
                if (this.setInsideMaskMaterialsQueued)
                {
                    this.setInsideMaskMaterialsQueued = false;
                    foreach (var c in this.targets)
                        EditorSetMaskMaterials(c as SkeletonRenderer, SpriteMaskInteraction.VisibleInsideMask);
                }
                if (this.setOutsideMaskMaterialsQueued)
                {
                    this.setOutsideMaskMaterialsQueued = false;
                    foreach (var c in this.targets)
                        EditorSetMaskMaterials(c as SkeletonRenderer, SpriteMaskInteraction.VisibleOutsideMask);
                }

                if (this.deleteInsideMaskMaterialsQueued)
                {
                    this.deleteInsideMaskMaterialsQueued = false;
                    foreach (var c in this.targets)
                        EditorDeleteMaskMaterials(c as SkeletonRenderer, SpriteMaskInteraction.VisibleInsideMask);
                }
                if (this.deleteOutsideMaskMaterialsQueued)
                {
                    this.deleteOutsideMaskMaterialsQueued = false;
                    foreach (var c in this.targets)
                        EditorDeleteMaskMaterials(c as SkeletonRenderer, SpriteMaskInteraction.VisibleOutsideMask);
                }
#endif

#if NO_PREFAB_MESH
				if (isInspectingPrefab) {
					foreach (var c in targets) {
						var component = (SkeletonRenderer)c;
						MeshFilter meshFilter = component.GetComponent<MeshFilter>();
						if (meshFilter != null && meshFilter.sharedMesh != null)
							meshFilter.sharedMesh = null;
					}
				}
#endif
            }

            var valid = this.TargetIsValid;

            // Fields.
            if (multi)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    SpineInspectorUtility.PropertyFieldFitLabel(this.skeletonDataAsset, this.SkeletonDataAssetLabel);
                    if (GUILayout.Button(ReloadButtonString, ReloadButtonStyle, ReloadButtonWidth))
                        this.forceReloadQueued = true;
                }

                if (valid) EditorGUILayout.PropertyField(this.initialSkinName, SpineInspectorUtility.TempContent("Initial Skin"));

            }
            else
            {
                var component = (SkeletonRenderer)this.target;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    SpineInspectorUtility.PropertyFieldFitLabel(this.skeletonDataAsset, this.SkeletonDataAssetLabel);
                    if (component.valid)
                    {
                        if (GUILayout.Button(ReloadButtonString, ReloadButtonStyle, ReloadButtonWidth))
                            this.forceReloadQueued = true;
                    }
                }

                if (component.skeletonDataAsset == null)
                {
                    EditorGUILayout.HelpBox("Skeleton Data Asset required", MessageType.Warning);
                    return;
                }

                if (!SpineEditorUtilities.SkeletonDataAssetIsValid(component.skeletonDataAsset))
                {
                    EditorGUILayout.HelpBox("Skeleton Data Asset error. Please check Skeleton Data Asset.", MessageType.Error);
                    return;
                }

                if (valid)
                    EditorGUILayout.PropertyField(this.initialSkinName, SpineInspectorUtility.TempContent("Initial Skin"));

            }

            EditorGUILayout.Space();

            // Sorting Layers
            SpineInspectorUtility.SortingPropertyFields(this.sortingProperties, applyModifiedProperties: true);

            if (this.maskInteraction != null) EditorGUILayout.PropertyField(this.maskInteraction, this.MaskInteractionLabel);

            if (!valid)
                return;

            string errorMessage = null;
            if (SpineEditorUtilities.Preferences.componentMaterialWarning &&
                MaterialChecks.IsMaterialSetupProblematic((SkeletonRenderer)this.target, ref errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error, true);
            }

            // More Render Options...
            using (new SpineInspectorUtility.BoxScope())
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight + 5));
                advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced");
                if (advancedFoldout)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Debug", EditorStyles.miniButton, GUILayout.Width(65f)))
                        SkeletonDebugWindow.Init();
                }
                else
                {
                    EditorGUILayout.Space();
                }
                EditorGUILayout.EndHorizontal();

                if (advancedFoldout)
                {

                    using (new SpineInspectorUtility.IndentScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            SpineInspectorUtility.ToggleLeftLayout(this.initialFlipX);
                            SpineInspectorUtility.ToggleLeftLayout(this.initialFlipY);
                            EditorGUILayout.Space();
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Renderer Settings", EditorStyles.boldLabel);
                        using (new SpineInspectorUtility.LabelWidthScope())
                        {
                            // Optimization options
                            if (this.updateWhenInvisible != null) EditorGUILayout.PropertyField(this.updateWhenInvisible, this.UpdateWhenInvisibleLabel);

                            if (this.singleSubmesh != null) EditorGUILayout.PropertyField(this.singleSubmesh, this.SingleSubmeshLabel);
#if PER_MATERIAL_PROPERTY_BLOCKS
                            if (this.fixDrawOrder != null) EditorGUILayout.PropertyField(this.fixDrawOrder, this.FixDrawOrderLabel);
#endif
                            if (this.immutableTriangles != null) EditorGUILayout.PropertyField(this.immutableTriangles, this.ImmubleTrianglesLabel);
                            EditorGUILayout.PropertyField(this.clearStateOnDisable, this.ClearStateOnDisableLabel);
                            EditorGUILayout.Space();
                        }

                        SeparatorsField(this.separatorSlotNames);
                        EditorGUILayout.Space();

                        // Render options
                        const float MinZSpacing = -0.1f;
                        const float MaxZSpacing = 0f;
                        EditorGUILayout.Slider(this.zSpacing, MinZSpacing, MaxZSpacing, this.ZSpacingLabel);
                        EditorGUILayout.Space();

                        using (new SpineInspectorUtility.LabelWidthScope())
                        {
                            EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Vertex Data", SpineInspectorUtility.UnityIcon<MeshFilter>()), EditorStyles.boldLabel);
                            if (this.pmaVertexColors != null) EditorGUILayout.PropertyField(this.pmaVertexColors, this.PMAVertexColorsLabel);
                            EditorGUILayout.PropertyField(this.tintBlack, this.TintBlackLabel);

                            // Optional fields. May be disabled in SkeletonRenderer.
                            if (this.normals != null) EditorGUILayout.PropertyField(this.normals, this.NormalsLabel);
                            if (this.tangents != null) EditorGUILayout.PropertyField(this.tangents, this.TangentsLabel);
                        }

#if BUILT_IN_SPRITE_MASK_COMPONENT
                        EditorGUILayout.Space();
                        if (this.maskMaterialsNone.arraySize > 0 || this.maskMaterialsInside.arraySize > 0 || this.maskMaterialsOutside.arraySize > 0)
                        {
                            EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Mask Interaction Materials", SpineInspectorUtility.UnityIcon<SpriteMask>()), EditorStyles.boldLabel);
                            var differentMaskModesSelected = this.maskInteraction.hasMultipleDifferentValues;
                            var activeMaskInteractionValue = differentMaskModesSelected ? -1 : this.maskInteraction.intValue;

                            var ignoredParam = true;
                            this.MaskMaterialsEditingField(ref this.setMaskNoneMaterialsQueued, ref ignoredParam, this.maskMaterialsNone, this.MaskMaterialsNoneLabel,
                                                        differentMaskModesSelected, allowDelete: false, isActiveMaterial: activeMaskInteractionValue == (int)SpriteMaskInteraction.None);
                            this.MaskMaterialsEditingField(ref this.setInsideMaskMaterialsQueued, ref this.deleteInsideMaskMaterialsQueued, this.maskMaterialsInside, this.MaskMaterialsInsideLabel,
                                                        differentMaskModesSelected, allowDelete: true, isActiveMaterial: activeMaskInteractionValue == (int)SpriteMaskInteraction.VisibleInsideMask);
                            this.MaskMaterialsEditingField(ref this.setOutsideMaskMaterialsQueued, ref this.deleteOutsideMaskMaterialsQueued, this.maskMaterialsOutside, this.MaskMaterialsOutsideLabel,
                                                        differentMaskModesSelected, allowDelete: true, isActiveMaterial: activeMaskInteractionValue == (int)SpriteMaskInteraction.VisibleOutsideMask);
                        }
#endif

                        EditorGUILayout.Space();

                        if (valid && !this.isInspectingPrefab)
                        {
                            if (multi)
                            {
                                // Support multi-edit SkeletonUtility button.
                                //	EditorGUILayout.Space();
                                //	bool addSkeletonUtility = GUILayout.Button(buttonContent, GUILayout.Height(30));
                                //	foreach (var t in targets) {
                                //		var component = t as Component;
                                //		if (addSkeletonUtility && component.GetComponent<SkeletonUtility>() == null)
                                //			component.gameObject.AddComponent<SkeletonUtility>();
                                //	}
                            }
                            else
                            {
                                var component = (Component)this.target;
                                if (component.GetComponent<SkeletonUtility>() == null)
                                {
                                    if (SpineInspectorUtility.CenteredButton(this.SkeletonUtilityButtonContent, 21, true, 200f))
                                        component.gameObject.AddComponent<SkeletonUtility>();
                                }
                            }
                        }

                        EditorGUILayout.Space();
                    }
                }

                if (EditorGUI.EndChangeCheck())
                    SceneView.RepaintAll();
            }
        }

        protected void SkeletonRootMotionParameter()
        {
            SkeletonRootMotionParameter(this.targets);
        }

        public static void SkeletonRootMotionParameter(Object[] targets)
        {
            var rootMotionComponentCount = 0;
            foreach (var t in targets)
            {
                var component = t as Component;
                if (component.GetComponent<SkeletonRootMotion>() != null)
                {
                    ++rootMotionComponentCount;
                }
            }
            var allHaveRootMotion = rootMotionComponentCount == targets.Length;
            var anyHaveRootMotion = rootMotionComponentCount > 0;

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Root Motion");

                if (!allHaveRootMotion)
                {
                    if (GUILayout.Button(SpineInspectorUtility.TempContent("Add Component", Icons.constraintTransform), GUILayout.MaxWidth(130), GUILayout.Height(18)))
                    {
                        foreach (var t in targets)
                        {
                            var component = t as Component;
                            if (component.GetComponent<SkeletonRootMotion>() == null)
                            {
                                component.gameObject.AddComponent<SkeletonRootMotion>();
                            }
                        }
                    }
                }
                if (anyHaveRootMotion)
                {
                    if (GUILayout.Button(SpineInspectorUtility.TempContent("Remove Component", Icons.constraintTransform), GUILayout.MaxWidth(140), GUILayout.Height(18)))
                    {
                        foreach (var t in targets)
                        {
                            var component = t as Component;
                            var rootMotionComponent = component.GetComponent<SkeletonRootMotion>();
                            if (rootMotionComponent != null)
                            {
                                DestroyImmediate(rootMotionComponent);
                            }
                        }
                    }
                }
            }
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

        public static void SeparatorsField(SerializedProperty separatorSlotNames)
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

                    EditorGUILayout.Space();
                }
                else
                    EditorGUILayout.PropertyField(separatorSlotNames, new GUIContent(separatorSlotNames.displayName + string.Format("{0} [{1}]", terminalSlotWarning, separatorSlotNames.arraySize), SeparatorsDescription), true);
            }
        }

        public void MaskMaterialsEditingField(ref bool wasSetRequested, ref bool wasDeleteRequested,
                                                    SerializedProperty maskMaterials, GUIContent label,
                                                    bool differentMaskModesSelected, bool allowDelete, bool isActiveMaterial)
        {
            using (new EditorGUILayout.HorizontalScope())
            {

                EditorGUILayout.LabelField(label, isActiveMaterial ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.MinWidth(80f), GUILayout.MaxWidth(140));
                EditorGUILayout.LabelField(maskMaterials.hasMultipleDifferentValues ? "-" : maskMaterials.arraySize.ToString(), EditorStyles.miniLabel, GUILayout.Width(42f));

                var enableSetButton = differentMaskModesSelected || maskMaterials.arraySize == 0;
                var enableClearButtons = differentMaskModesSelected || (maskMaterials.arraySize != 0 && !isActiveMaterial);

                EditorGUI.BeginDisabledGroup(!enableSetButton);
                if (GUILayout.Button(this.SetMaterialButtonLabel, EditorStyles.miniButtonLeft, GUILayout.Width(46f)))
                {
                    wasSetRequested = true;
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!enableClearButtons);
                {
                    if (GUILayout.Button(this.ClearMaterialButtonLabel, allowDelete ? EditorStyles.miniButtonMid : EditorStyles.miniButtonRight, GUILayout.Width(46f)))
                    {
                        maskMaterials.ClearArray();
                    }
                    else if (allowDelete && GUILayout.Button(this.DeleteMaterialButtonLabel, EditorStyles.miniButtonRight, GUILayout.Width(46f)))
                    {
                        wasDeleteRequested = true;
                    }
                    if (!allowDelete)
                        GUILayout.Space(46f);
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void HandleSkinChange()
        {
            if (!Application.isPlaying && Event.current.type == EventType.Layout && !this.initialSkinName.hasMultipleDifferentValues)
            {
                var mismatchDetected = false;
                var newSkinName = this.initialSkinName.stringValue;
                foreach (var o in this.targets)
                {
                    mismatchDetected |= UpdateIfSkinMismatch((SkeletonRenderer)o, newSkinName);
                }

                if (mismatchDetected)
                {
                    mismatchDetected = false;
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
            }
        }

        private static bool UpdateIfSkinMismatch(SkeletonRenderer skeletonRenderer, string componentSkinName)
        {
            if (!skeletonRenderer.valid || skeletonRenderer.EditorSkipSkinSync) return false;

            var skin = skeletonRenderer.Skeleton.Skin;
            var skeletonSkinName = skin != null ? skin.Name : null;
            var defaultCase = skin == null && string.IsNullOrEmpty(componentSkinName);
            var fieldMatchesSkin = defaultCase || string.Equals(componentSkinName, skeletonSkinName, System.StringComparison.Ordinal);

            if (!fieldMatchesSkin)
            {
                var skinToSet = string.IsNullOrEmpty(componentSkinName) ? null : skeletonRenderer.Skeleton.Data.FindSkin(componentSkinName);
                skeletonRenderer.Skeleton.SetSkin(skinToSet);
                skeletonRenderer.Skeleton.SetSlotsToSetupPose();

                // Note: the UpdateIfSkinMismatch concept shall be replaced with e.g. an OnValidate based
                // solution or in a separate commit. The current solution does not repaint the Game view because
                // it is first applying values and in the next editor pass is calling this skin-changing method.
                if (skeletonRenderer is SkeletonAnimation)
                    ((SkeletonAnimation)skeletonRenderer).Update(0f);
                else if (skeletonRenderer is SkeletonMecanim)
                    ((SkeletonMecanim)skeletonRenderer).Update();

                skeletonRenderer.LateUpdate();
                return true;
            }
            return false;
        }

        private bool AreAnyMaskMaterialsMissing()
        {
#if BUILT_IN_SPRITE_MASK_COMPONENT
            foreach (var o in this.targets)
            {
                var component = (SkeletonRenderer)o;
                if (!component.valid)
                    continue;
                if (SpineMaskUtilities.AreMaskMaterialsMissing(component))
                    return true;
            }
#endif
            return false;
        }

#if BUILT_IN_SPRITE_MASK_COMPONENT
        private static void EditorSetMaskMaterials(SkeletonRenderer component, SpriteMaskInteraction maskType)
        {
            if (component == null) return;
            if (!SpineEditorUtilities.SkeletonDataAssetIsValid(component.SkeletonDataAsset)) return;
            SpineMaskUtilities.EditorInitMaskMaterials(component, component.maskMaterials, maskType);
        }

        private static void EditorDeleteMaskMaterials(SkeletonRenderer component, SpriteMaskInteraction maskType)
        {
            if (component == null) return;
            if (!SpineEditorUtilities.SkeletonDataAssetIsValid(component.SkeletonDataAsset)) return;
            SpineMaskUtilities.EditorDeleteMaskMaterials(component.maskMaterials, maskType);
        }
#endif
    }
}
