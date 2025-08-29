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

#define SPINE_SKELETON_MECANIM

#if (UNITY_2017_4 || UNITY_2018_1_OR_NEWER)
#define SPINE_UNITY_2018_PREVIEW_API
#endif


using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CompatibilityProblemInfo = Spine.Unity.SkeletonDataCompatibility.CompatibilityProblemInfo;

namespace Spine.Unity.Editor
{
    using Event = UnityEngine.Event;
    using Icons = SpineEditorUtilities.Icons;

    [CustomEditor(typeof(SkeletonDataAsset)), CanEditMultipleObjects]
    public class SkeletonDataAssetInspector : UnityEditor.Editor
    {
        internal static bool showAnimationStateData = true;
        internal static bool showAnimationList = true;
        internal static bool showSlotList = false;
        internal static bool showAttachments = false;

        private SerializedProperty atlasAssets, skeletonJSON, scale, fromAnimation, toAnimation, duration, defaultMix;
        private SerializedProperty skeletonDataModifiers;
        private SerializedProperty blendModeMaterials;
#if SPINE_TK2D
		SerializedProperty spriteCollection;
#endif

#if SPINE_SKELETON_MECANIM
        private static bool isMecanimExpanded = false;
        private SerializedProperty controller;
#endif

        private SkeletonDataAsset targetSkeletonDataAsset;
        private SkeletonData targetSkeletonData;

        private readonly List<string> warnings = new List<string>();
        private CompatibilityProblemInfo compatibilityProblemInfo = null;
        private readonly SkeletonInspectorPreview preview = new SkeletonInspectorPreview();

        private GUIStyle activePlayButtonStyle, idlePlayButtonStyle;
        private readonly GUIContent DefaultMixLabel = new GUIContent("Default Mix Duration", "Sets 'SkeletonDataAsset.defaultMix' in the asset and 'AnimationState.data.defaultMix' at runtime load time.");

        private string TargetAssetGUID
        { get { return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this.targetSkeletonDataAsset)); } }
        private string LastSkinKey
        { get { return this.TargetAssetGUID + "_lastSkin"; } }
        private string LastSkinName
        { get { return EditorPrefs.GetString(this.LastSkinKey, ""); } }

        private void OnEnable()
        {
            this.InitializeEditor();
        }

        private void OnDestroy()
        {
            this.HandleOnDestroyPreview();
            AppDomain.CurrentDomain.DomainUnload -= this.OnDomainUnload;
            EditorApplication.update -= this.preview.HandleEditorUpdate;
        }

        private void OnDomainUnload(object sender, EventArgs e)
        {
            this.OnDestroy();
        }

        public void UpdateSkeletonData()
        {
            this.preview.Clear();
            this.InitializeEditor();
            if (this.targetSkeletonDataAsset)
                EditorUtility.SetDirty(this.targetSkeletonDataAsset);
        }

        private void InitializeEditor()
        {
            SpineEditorUtilities.ConfirmInitialization();
            this.targetSkeletonDataAsset = (SkeletonDataAsset)this.target;

            var newAtlasAssets = this.atlasAssets == null;
            if (newAtlasAssets) this.atlasAssets = this.serializedObject.FindProperty("atlasAssets");
            this.skeletonJSON = this.serializedObject.FindProperty("skeletonJSON");
            this.scale = this.serializedObject.FindProperty("scale");
            this.fromAnimation = this.serializedObject.FindProperty("fromAnimation");
            this.toAnimation = this.serializedObject.FindProperty("toAnimation");
            this.duration = this.serializedObject.FindProperty("duration");
            this.defaultMix = this.serializedObject.FindProperty("defaultMix");

            this.skeletonDataModifiers = this.serializedObject.FindProperty("skeletonDataModifiers");
            this.blendModeMaterials = this.serializedObject.FindProperty("blendModeMaterials");

#if SPINE_SKELETON_MECANIM
            this.controller = this.serializedObject.FindProperty("controller");
#endif

#if SPINE_TK2D
			if (newAtlasAssets) atlasAssets.isExpanded = false;
			spriteCollection = serializedObject.FindProperty("spriteCollection");
#else
            // Analysis disable once ConvertIfToOrExpression
            if (newAtlasAssets) this.atlasAssets.isExpanded = true;
#endif

            // This handles the case where the managed editor assembly is unloaded before recompilation when code changes.
            AppDomain.CurrentDomain.DomainUnload -= this.OnDomainUnload;
            AppDomain.CurrentDomain.DomainUnload += this.OnDomainUnload;

            EditorApplication.update -= this.preview.HandleEditorUpdate;
            EditorApplication.update += this.preview.HandleEditorUpdate;
            this.preview.OnSkinChanged -= this.HandlePreviewSkinChanged;
            this.preview.OnSkinChanged += this.HandlePreviewSkinChanged;

            this.PopulateWarnings();
            if (this.targetSkeletonDataAsset.skeletonJSON == null)
            {
                this.targetSkeletonData = null;
                return;
            }

            this.targetSkeletonData = this.NoProblems() ? this.targetSkeletonDataAsset.GetSkeletonData(false) : null;

            if (this.targetSkeletonData != null && this.NoProblems())
            {
                this.preview.Initialize(this.Repaint, this.targetSkeletonDataAsset, this.LastSkinName);
            }

        }

        private void Clear()
        {
            this.preview.Clear();
            this.targetSkeletonDataAsset.Clear();
            this.targetSkeletonData = null;
        }

        public override void OnInspectorGUI()
        {
            // Multi-Editing
            if (this.serializedObject.isEditingMultipleObjects)
            {
                this.OnInspectorGUIMulti();
                return;
            }

            { // Lazy initialization because accessing EditorStyles values in OnEnable during a recompile causes UnityEditor to throw null exceptions. (Unity 5.3.5)
                this.idlePlayButtonStyle = this.idlePlayButtonStyle ?? new GUIStyle(EditorStyles.miniButton);
                if (this.activePlayButtonStyle == null)
                {
                    this.activePlayButtonStyle = new GUIStyle(this.idlePlayButtonStyle);
                    this.activePlayButtonStyle.normal.textColor = Color.red;
                }
            }

            this.serializedObject.Update();

            // Header
            EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(this.target.name + " (SkeletonDataAsset)", Icons.spine), EditorStyles.whiteLargeLabel);
            if (this.targetSkeletonData != null) EditorGUILayout.LabelField("(Drag and Drop to instantiate.)", EditorStyles.miniLabel);

            // Main Serialized Fields
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                using (new SpineInspectorUtility.BoxScope())
                    this.DrawSkeletonDataFields();

                if (this.compatibilityProblemInfo != null)
                    return;

                using (new SpineInspectorUtility.BoxScope())
                {
                    this.DrawAtlasAssetsFields();
                    this.HandleAtlasAssetsNulls();
                }

                if (changeCheck.changed)
                {
                    if (this.serializedObject.ApplyModifiedProperties())
                    {
                        this.Clear();
                        this.InitializeEditor();

                        if (SpineEditorUtilities.Preferences.autoReloadSceneSkeletons)
                            SpineEditorUtilities.DataReloadHandler.ReloadSceneSkeletonComponents(this.targetSkeletonDataAsset);

                        return;
                    }
                }
            }

            // Unity Quirk: Some code depends on valid preview. If preview is initialized elsewhere, this can cause contents to change between Layout and Repaint events, causing GUILayout control count errors.
            if (this.NoProblems())
                this.preview.Initialize(this.Repaint, this.targetSkeletonDataAsset, this.LastSkinName);

            if (this.targetSkeletonData != null)
            {
                GUILayout.Space(20f);

                using (new SpineInspectorUtility.BoxScope(false))
                {
                    EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Mix Settings", Icons.animationRoot), EditorStyles.boldLabel);
                    this.DrawAnimationStateInfo();
                    EditorGUILayout.Space();
                }

                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                this.DrawAnimationList();
                if (this.targetSkeletonData.Animations.Count > 0)
                {
                    const string AnimationReferenceButtonText = "Create Animation Reference Assets";
                    const string AnimationReferenceTooltipText = "AnimationReferenceAsset acts as Unity asset for a reference to a Spine.Animation. This can be used in inspectors.\n\nIt serializes a reference to a SkeletonDataAsset and an animationName.\n\nAt runtime, a reference to its Spine.Animation is loaded and cached into the object to be used as needed. This skips the need to find and cache animation references in individual MonoBehaviours.";
                    if (GUILayout.Button(SpineInspectorUtility.TempContent(AnimationReferenceButtonText, Icons.animationRoot, AnimationReferenceTooltipText), GUILayout.Width(250), GUILayout.Height(26)))
                    {
                        this.CreateAnimationReferenceAssets();
                    }
                }
                EditorGUILayout.Space();
                this.DrawSlotList();
                EditorGUILayout.Space();

                this.DrawUnityTools();

            }
            else
            {
#if !SPINE_TK2D
                // Draw Reimport Button
                using (new EditorGUI.DisabledGroupScope(this.skeletonJSON.objectReferenceValue == null))
                {
                    if (GUILayout.Button(SpineInspectorUtility.TempContent("Attempt Reimport", Icons.warning)))
                        this.DoReimport();
                }
#else
				EditorGUILayout.HelpBox("Couldn't load SkeletonData.", MessageType.Error);
#endif

                this.DrawWarningList();
            }

            if (!Application.isPlaying)
                this.serializedObject.ApplyModifiedProperties();
        }

        private void CreateAnimationReferenceAssets()
        {
            const string AssetFolderName = "ReferenceAssets";
            var parentFolder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(this.targetSkeletonDataAsset));
            var dataPath = parentFolder + "/" + AssetFolderName;
            if (!AssetDatabase.IsValidFolder(dataPath))
            {
                AssetDatabase.CreateFolder(parentFolder, AssetFolderName);
            }

            var nameField = typeof(AnimationReferenceAsset).GetField("animationName", BindingFlags.NonPublic | BindingFlags.Instance);
            var skeletonDataAssetField = typeof(AnimationReferenceAsset).GetField("skeletonDataAsset", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var animation in this.targetSkeletonData.Animations)
            {
                var assetPath = string.Format("{0}/{1}.asset", dataPath, AssetUtility.GetPathSafeName(animation.Name));
                var existingAsset = AssetDatabase.LoadAssetAtPath<AnimationReferenceAsset>(assetPath);
                if (existingAsset == null)
                {
                    var newAsset = ScriptableObject.CreateInstance<AnimationReferenceAsset>();
                    skeletonDataAssetField.SetValue(newAsset, this.targetSkeletonDataAsset);
                    nameField.SetValue(newAsset, animation.Name);
                    AssetDatabase.CreateAsset(newAsset, assetPath);
                }
            }

            var folderObject = AssetDatabase.LoadAssetAtPath(dataPath, typeof(UnityEngine.Object));
            if (folderObject != null)
            {
                Selection.activeObject = folderObject;
                EditorGUIUtility.PingObject(folderObject);
            }
        }

        private void OnInspectorGUIMulti()
        {

            // Skeleton data file field.
            using (new SpineInspectorUtility.BoxScope())
            {
                EditorGUILayout.LabelField("SkeletonData", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(this.skeletonJSON, SpineInspectorUtility.TempContent(this.skeletonJSON.displayName, Icons.spine));
                EditorGUILayout.DelayedFloatField(this.scale); //EditorGUILayout.PropertyField(scale);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(this.skeletonDataModifiers, true);

                this.DrawBlendModeMaterialProperties();
            }

            // Texture source field.
            using (new SpineInspectorUtility.BoxScope())
            {
                EditorGUILayout.LabelField("Atlas", EditorStyles.boldLabel);
#if !SPINE_TK2D
                EditorGUILayout.PropertyField(this.atlasAssets, true);
#else
				using (new EditorGUI.DisabledGroupScope(spriteCollection.objectReferenceValue != null)) {
					EditorGUILayout.PropertyField(atlasAssets, true);
				}
				EditorGUILayout.LabelField("spine-tk2d", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(spriteCollection, true);
#endif
            }

            // Mix settings.
            using (new SpineInspectorUtility.BoxScope())
            {
                EditorGUILayout.LabelField("Mix Settings", EditorStyles.boldLabel);
                SpineInspectorUtility.PropertyFieldWideLabel(this.defaultMix, this.DefaultMixLabel, 160);
                EditorGUILayout.Space();
            }

        }

        private void DrawBlendModeMaterialProperties()
        {
            if (this.skeletonDataModifiers.arraySize > 0)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight + 5));
                EditorGUILayout.PrefixLabel("Blend Modes");
                if (GUILayout.Button(new GUIContent("Upgrade", "Upgrade BlendModeMaterialAsset to built-in BlendModeMaterials."), EditorStyles.miniButton, GUILayout.Width(65f)))
                {
                    foreach (SkeletonDataAsset skeletonData in this.targets)
                    {
                        BlendModeMaterialsUtility.UpgradeBlendModeMaterials(skeletonData);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(this.blendModeMaterials, true);
            if (EditorGUI.EndChangeCheck())
            {
                this.serializedObject.ApplyModifiedProperties();
                foreach (SkeletonDataAsset skeletonData in this.targets)
                {
                    BlendModeMaterialsUtility.UpdateBlendModeMaterials(skeletonData);
                }
            }
        }

        private void DrawSkeletonDataFields()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("SkeletonData", EditorStyles.boldLabel);
                if (this.targetSkeletonData != null)
                {
                    var sd = this.targetSkeletonData;
                    var m = string.Format("{8} - {0} {1}\nBones: {2}\nConstraints: \n {5} IK \n {6} Path \n {7} Transform\n\nSlots: {3}\nSkins: {4}\n\nAnimations: {9}",
                        sd.Version, string.IsNullOrEmpty(sd.Version) ? "" : "export          ", sd.Bones.Count, sd.Slots.Count, sd.Skins.Count, sd.IkConstraints.Count, sd.PathConstraints.Count, sd.TransformConstraints.Count, this.skeletonJSON.objectReferenceValue.name, sd.Animations.Count);
                    EditorGUILayout.LabelField(GUIContent.none, new GUIContent(Icons.info, m), GUILayout.Width(30f));
                }
            }
            EditorGUILayout.PropertyField(this.skeletonJSON, SpineInspectorUtility.TempContent(this.skeletonJSON.displayName, Icons.spine));

            if (this.compatibilityProblemInfo != null)
            {
                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(this.compatibilityProblemInfo.DescriptionString(), Icons.warning), GUILayout.Height(52));
                return;
            }

            EditorGUILayout.DelayedFloatField(this.scale); //EditorGUILayout.PropertyField(scale);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(this.skeletonDataModifiers, true);

            this.DrawBlendModeMaterialProperties();
        }

        private void DrawAtlasAssetsFields()
        {
            EditorGUILayout.LabelField("Atlas", EditorStyles.boldLabel);
#if !SPINE_TK2D
            EditorGUILayout.PropertyField(this.atlasAssets, true);
#else
			using (new EditorGUI.DisabledGroupScope(spriteCollection.objectReferenceValue != null)) {
				EditorGUILayout.PropertyField(atlasAssets, true);
			}
			EditorGUILayout.LabelField("spine-tk2d", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(spriteCollection, true);
#endif

            if (this.atlasAssets.arraySize == 0)
                EditorGUILayout.HelpBox("AtlasAssets array is empty. Skeleton's attachments will load without being mapped to images.", MessageType.Info);
        }

        private void HandleAtlasAssetsNulls()
        {
            var hasNulls = false;
            foreach (var a in this.targetSkeletonDataAsset.atlasAssets)
            {
                if (a == null)
                {
                    hasNulls = true;
                    break;
                }
            }
            if (hasNulls)
            {
                if (this.targetSkeletonDataAsset.atlasAssets.Length == 1)
                {
                    EditorGUILayout.HelpBox("Atlas array cannot have null entries!", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("Atlas array should not have null entries!", MessageType.Error);
                    if (SpineInspectorUtility.CenteredButton(SpineInspectorUtility.TempContent("Remove null entries")))
                    {
                        var trimmedAtlasAssets = new List<AtlasAssetBase>();
                        foreach (var a in this.targetSkeletonDataAsset.atlasAssets)
                        {
                            if (a != null)
                                trimmedAtlasAssets.Add(a);
                        }
                        this.targetSkeletonDataAsset.atlasAssets = trimmedAtlasAssets.ToArray();
                        this.serializedObject.Update();
                    }
                }
            }
        }

        private void DrawAnimationStateInfo()
        {
            using (new SpineInspectorUtility.IndentScope())
                showAnimationStateData = EditorGUILayout.Foldout(showAnimationStateData, "Animation State Data");

            if (!showAnimationStateData)
                return;

            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                using (new SpineInspectorUtility.IndentScope())
                    SpineInspectorUtility.PropertyFieldWideLabel(this.defaultMix, this.DefaultMixLabel, 160);


                if (this.fromAnimation.arraySize > 0)
                {
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        EditorGUILayout.LabelField("Custom Mix Durations", EditorStyles.boldLabel);
                    }

                    for (var i = 0; i < this.fromAnimation.arraySize; i++)
                    {
                        var from = this.fromAnimation.GetArrayElementAtIndex(i);
                        var to = this.toAnimation.GetArrayElementAtIndex(i);
                        var durationProp = this.duration.GetArrayElementAtIndex(i);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(16f); // Space instead of EditorGUIUtility.indentLevel. indentLevel will add the space on every field.
                            EditorGUILayout.PropertyField(from, GUIContent.none);
                            //EditorGUILayout.LabelField(">", EditorStyles.miniLabel, GUILayout.Width(9f));
                            EditorGUILayout.PropertyField(to, GUIContent.none);
                            //GUILayout.Space(5f);
                            durationProp.floatValue = EditorGUILayout.FloatField(durationProp.floatValue, GUILayout.MinWidth(25f), GUILayout.MaxWidth(60f));
                            if (GUILayout.Button("Delete", EditorStyles.miniButton))
                            {
                                this.duration.DeleteArrayElementAtIndex(i);
                                this.toAnimation.DeleteArrayElementAtIndex(i);
                                this.fromAnimation.DeleteArrayElementAtIndex(i);
                            }
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Add Custom Mix"))
                    {
                        this.duration.arraySize++;
                        this.toAnimation.arraySize++;
                        this.fromAnimation.arraySize++;
                    }
                    EditorGUILayout.Space();
                }

                if (cc.changed)
                {
                    this.targetSkeletonDataAsset.FillStateData();
                    EditorUtility.SetDirty(this.targetSkeletonDataAsset);
                    this.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawAnimationList()
        {
            showAnimationList = EditorGUILayout.Foldout(showAnimationList, SpineInspectorUtility.TempContent(string.Format("Animations [{0}]", this.targetSkeletonData.Animations.Count), Icons.animationRoot));
            if (!showAnimationList)
                return;

            var isPreviewWindowOpen = this.preview.IsValid;

            if (isPreviewWindowOpen)
            {
                if (GUILayout.Button(SpineInspectorUtility.TempContent("Setup Pose", Icons.skeleton), GUILayout.Width(105), GUILayout.Height(18)))
                {
                    this.preview.ClearAnimationSetupPose();
                    this.preview.RefreshOnNextUpdate();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Animations can be previewed if you expand the Preview window below.", MessageType.Info);
            }

            EditorGUILayout.LabelField("Name", "      Duration");
            //bool nonessential = targetSkeletonData.ImagesPath != null; // Currently the only way to determine if skeleton data has nonessential data. (Spine 3.6)
            //float fps = targetSkeletonData.Fps;
            //if (nonessential && fps == 0) fps = 30;

            var activeTrack = this.preview.ActiveTrack;
            foreach (var animation in this.targetSkeletonData.Animations)
            {
                using (new GUILayout.HorizontalScope())
                {
                    if (isPreviewWindowOpen)
                    {
                        var active = activeTrack != null && activeTrack.Animation == animation;
                        //bool sameAndPlaying = active && activeTrack.TimeScale > 0f;
                        if (GUILayout.Button("\u25BA", active ? this.activePlayButtonStyle : this.idlePlayButtonStyle, GUILayout.Width(24)))
                        {
                            this.preview.PlayPauseAnimation(animation.Name, true);
                            activeTrack = this.preview.ActiveTrack;
                        }
                    }
                    else
                    {
                        GUILayout.Label("-", GUILayout.Width(24));
                    }
                    //string frameCountString = (fps > 0) ? ("(" + (Mathf.RoundToInt(animation.Duration * fps)) + ")").PadLeft(12, ' ') : string.Empty;
                    //EditorGUILayout.LabelField(new GUIContent(animation.Name, Icons.animation), SpineInspectorUtility.TempContent(animation.Duration.ToString("f3") + "s" + frameCountString));
                    var durationString = animation.Duration.ToString("f3");
                    EditorGUILayout.LabelField(new GUIContent(animation.Name, Icons.animation), SpineInspectorUtility.TempContent(durationString + "s", tooltip: string.Format("{0} seconds\n{1} timelines", durationString, animation.Timelines.Count)));
                }
            }
        }

        private void DrawSlotList()
        {
            showSlotList = EditorGUILayout.Foldout(showSlotList, SpineInspectorUtility.TempContent("Slots", Icons.slotRoot));

            if (!showSlotList) return;
            if (!this.preview.IsValid) return;

            var defaultSkin = this.targetSkeletonData.DefaultSkin;
            var skin = this.preview.Skeleton.Skin ?? defaultSkin;

            using (new SpineInspectorUtility.IndentScope())
            {

                using (new EditorGUILayout.HorizontalScope())
                {
                    showAttachments = EditorGUILayout.ToggleLeft("Show Attachments", showAttachments, GUILayout.MaxWidth(150f));
                    if (showAttachments)
                    {
                        if (skin != null)
                        {
                            var attachmentCount = skin.Attachments.Count;
                            EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(string.Format("{0} ({1} attachment{2})", skin.Name, attachmentCount, SpineInspectorUtility.PluralThenS(attachmentCount)), Icons.skin));
                        }

                    }
                }

                var slotAttachments = new List<Skin.SkinEntry>();
                var defaultSkinAttachments = new List<Skin.SkinEntry>();
                var slotsItems = this.preview.Skeleton.Slots.Items;
                for (var i = this.preview.Skeleton.Slots.Count - 1; i >= 0; i--)
                {
                    var slot = slotsItems[i];
                    EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(slot.Data.Name, Icons.slot));
                    if (showAttachments)
                    {
                        slotAttachments.Clear();
                        defaultSkinAttachments.Clear();

                        using (new SpineInspectorUtility.IndentScope())
                        {
                            {
                                skin.GetAttachments(i, slotAttachments);
                                if (defaultSkin != null)
                                {
                                    if (skin != defaultSkin)
                                    {
                                        defaultSkin.GetAttachments(i, slotAttachments);
                                        defaultSkin.GetAttachments(i, defaultSkinAttachments);
                                    }
                                    else
                                    {
                                        defaultSkin.GetAttachments(i, defaultSkinAttachments);
                                    }
                                }
                            }

                            for (var a = 0; a < slotAttachments.Count; a++)
                            {
                                var skinEntry = slotAttachments[a];
                                var attachment = skinEntry.Attachment;
                                var attachmentName = skinEntry.Name;
                                var attachmentIsFromSkin = !defaultSkinAttachments.Contains(skinEntry);

                                var attachmentTypeIcon = Icons.GetAttachmentIcon(attachment);
                                var initialState = slot.Attachment == attachment;

                                var iconToUse = attachmentIsFromSkin ? Icons.skinPlaceholder : attachmentTypeIcon;
                                var toggled = EditorGUILayout.ToggleLeft(SpineInspectorUtility.TempContent(attachmentName, iconToUse), slot.Attachment == attachment, GUILayout.MinWidth(150f));

                                if (attachmentIsFromSkin)
                                {
                                    var extraIconRect = GUILayoutUtility.GetLastRect();
                                    extraIconRect.x += extraIconRect.width - (attachmentTypeIcon.width * 2f);
                                    extraIconRect.width = attachmentTypeIcon.width;
                                    extraIconRect.height = attachmentTypeIcon.height;
                                    GUI.DrawTexture(extraIconRect, attachmentTypeIcon);
                                }

                                if (toggled != initialState)
                                {
                                    slot.Attachment = toggled ? attachment : null;
                                    this.preview.RefreshOnNextUpdate();
                                }
                            }
                        }

                    }
                }
            }

        }

        private void DrawUnityTools()
        {
#if SPINE_SKELETON_MECANIM
            using (new SpineInspectorUtility.BoxScope())
            {
                isMecanimExpanded = EditorGUILayout.Foldout(isMecanimExpanded, SpineInspectorUtility.TempContent("SkeletonMecanim", SpineInspectorUtility.UnityIcon<SceneAsset>()));
                if (isMecanimExpanded)
                {
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        EditorGUILayout.PropertyField(this.controller, SpineInspectorUtility.TempContent("Controller", SpineInspectorUtility.UnityIcon<Animator>()));
                        if (this.controller.objectReferenceValue == null)
                        {

                            // Generate Mecanim Controller Button
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(EditorGUIUtility.labelWidth);
                                if (GUILayout.Button(SpineInspectorUtility.TempContent("Generate Mecanim Controller"), GUILayout.Height(20)))
                                    SkeletonBaker.GenerateMecanimAnimationClips(this.targetSkeletonDataAsset);
                            }
                            EditorGUILayout.HelpBox("SkeletonMecanim is the Mecanim alternative to SkeletonAnimation.\nIt is not required.", MessageType.Info);

                        }
                        else
                        {

                            // Update AnimationClips button.
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(EditorGUIUtility.labelWidth);
                                if (GUILayout.Button(SpineInspectorUtility.TempContent("Force Update AnimationClips"), GUILayout.Height(20)))
                                    SkeletonBaker.GenerateMecanimAnimationClips(this.targetSkeletonDataAsset);
                            }

                        }
                    }
                }
            }
#endif
        }

        private void DrawWarningList()
        {
            foreach (var line in this.warnings)
                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(line, Icons.warning));
        }

        private void PopulateWarnings()
        {
            this.warnings.Clear();
            this.compatibilityProblemInfo = null;

            if (this.skeletonJSON.objectReferenceValue == null)
            {
                this.warnings.Add("Missing Skeleton JSON");
            }
            else
            {
                var fieldValue = (TextAsset)this.skeletonJSON.objectReferenceValue;
                string problemDescription = null;
                if (!AssetUtility.IsSpineData(fieldValue, out this.compatibilityProblemInfo, ref problemDescription))
                {
                    if (problemDescription != null)
                        this.warnings.Add(problemDescription);
                    else
                        this.warnings.Add("Skeleton data file is not a valid Spine JSON or binary file.");
                }
                else
                {
#if SPINE_TK2D
					bool searchForSpineAtlasAssets = true;
					bool isSpriteCollectionNull = spriteCollection.objectReferenceValue == null;
					if (!isSpriteCollectionNull) searchForSpineAtlasAssets = false;
#else
                    // Analysis disable once ConvertToConstant.Local
                    var searchForSpineAtlasAssets = true;
#endif

                    if (searchForSpineAtlasAssets)
                    {
                        var detectedNullAtlasEntry = false;
                        var atlasList = new List<Atlas>();
                        var actualAtlasAssets = this.targetSkeletonDataAsset.atlasAssets;

                        for (var i = 0; i < actualAtlasAssets.Length; i++)
                        {
                            if (actualAtlasAssets[i] == null)
                            {
                                detectedNullAtlasEntry = true;
                                break;
                            }
                            else
                            {
                                if (actualAtlasAssets[i].MaterialCount > 0)
                                    atlasList.Add(actualAtlasAssets[i].GetAtlas());
                            }
                        }

                        if (detectedNullAtlasEntry)
                        {
                            this.warnings.Add("AtlasAsset elements should not be null.");
                        }
                        else
                        {
                            List<string> missingPaths = null;
                            if (this.atlasAssets.arraySize > 0)
                            {
                                missingPaths = AssetUtility.GetRequiredAtlasRegions(AssetDatabase.GetAssetPath(this.skeletonJSON.objectReferenceValue));
                                foreach (var atlas in atlasList)
                                {
                                    if (atlas == null)
                                        continue;
                                    for (var i = 0; i < missingPaths.Count; i++)
                                    {
                                        if (atlas.FindRegion(missingPaths[i]) != null)
                                        {
                                            missingPaths.RemoveAt(i);
                                            i--;
                                        }
                                    }
                                }

#if SPINE_TK2D
								if (missingPaths.Count > 0)
									warnings.Add("Missing regions. SkeletonDataAsset requires tk2DSpriteCollectionData or Spine AtlasAssets.");
#endif
                            }

                            if (missingPaths != null)
                            {
                                foreach (var missingRegion in missingPaths)
                                    this.warnings.Add(string.Format("Missing Region: '{0}'", missingRegion));
                            }

                        }
                    }

                }
            }
        }

        private void DoReimport()
        {
            AssetUtility.ImportSpineContent(new[] { AssetDatabase.GetAssetPath(this.skeletonJSON.objectReferenceValue) }, null, true);
            this.preview.Clear();
            this.InitializeEditor();
            EditorUtility.SetDirty(this.targetSkeletonDataAsset);
        }

        private void HandlePreviewSkinChanged(string skinName)
        {
            EditorPrefs.SetString(this.LastSkinKey, skinName);
        }

        private bool NoProblems()
        {
            return this.warnings.Count == 0 && this.compatibilityProblemInfo == null;
        }

        #region Preview Handlers
        private void HandleOnDestroyPreview()
        {
            EditorApplication.update -= this.preview.HandleEditorUpdate;
            this.preview.OnDestroy();
        }

        public override bool HasPreviewGUI()
        {
            if (this.serializedObject.isEditingMultipleObjects)
                return false;

            for (var i = 0; i < this.atlasAssets.arraySize; i++)
            {
                var prop = this.atlasAssets.GetArrayElementAtIndex(i);
                if (prop.objectReferenceValue == null)
                    return false;
            }

            return this.skeletonJSON.objectReferenceValue != null;
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (this.NoProblems())
            {
                this.preview.Initialize(this.Repaint, this.targetSkeletonDataAsset, this.LastSkinName);
                this.preview.HandleInteractivePreviewGUI(r, background);
            }
        }

        public override GUIContent GetPreviewTitle() { return SpineInspectorUtility.TempContent("Preview"); }
        public override void OnPreviewSettings() { this.preview.HandleDrawSettings(); }
        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height) { return this.preview.GetStaticPreview(width, height); }
        #endregion
    }

    internal class SkeletonInspectorPreview
    {
        private Color OriginColor = new Color(0.3f, 0.3f, 0.3f, 1);
        private static readonly int SliderHash = "Slider".GetHashCode();

        private SkeletonDataAsset skeletonDataAsset;
        private SkeletonData skeletonData;

        private SkeletonAnimation skeletonAnimation;
        private GameObject previewGameObject;
        internal bool requiresRefresh;
        private float animationLastTime;

        private static float CurrentTime
        { get { return (float)EditorApplication.timeSinceStartup; } }

        private Action Repaint;
        public event Action<string> OnSkinChanged;

        private Texture previewTexture;
        private PreviewRenderUtility previewRenderUtility;
        private Camera PreviewUtilityCamera
        {
            get
            {
                if (this.previewRenderUtility == null) return null;
#if UNITY_2017_1_OR_NEWER
                return this.previewRenderUtility.camera;
#else
				return previewRenderUtility.m_Camera;
#endif
            }
        }

        private static Vector3 lastCameraPositionGoal;
        private static float lastCameraOrthoGoal;
        private float cameraOrthoGoal = 1;
        private Vector3 cameraPositionGoal = new Vector3(0, 0, -10);
        private double cameraAdjustEndFrame = 0;

        private readonly List<Spine.Event> currentAnimationEvents = new List<Spine.Event>();
        private readonly List<float> currentAnimationEventTimes = new List<float>();
        private List<SpineEventTooltip> currentAnimationEventTooltips = new List<SpineEventTooltip>();

        public bool IsValid { get { return this.skeletonAnimation != null && this.skeletonAnimation.valid; } }

        public Skeleton Skeleton { get { return this.IsValid ? this.skeletonAnimation.Skeleton : null; } }

        public float TimeScale
        {
            get { return this.IsValid ? this.skeletonAnimation.timeScale : 1f; }
            set { if (this.IsValid) this.skeletonAnimation.timeScale = value; }
        }

        public bool IsPlayingAnimation
        {
            get
            {
                if (!this.IsValid) return false;
                var currentTrack = this.skeletonAnimation.AnimationState.GetCurrent(0);
                return currentTrack != null && currentTrack.TimeScale > 0;
            }
        }

        public TrackEntry ActiveTrack { get { return this.IsValid ? this.skeletonAnimation.AnimationState.GetCurrent(0) : null; } }

        public Vector3 PreviewCameraPosition
        {
            get { return this.PreviewUtilityCamera.transform.position; }
            set { this.PreviewUtilityCamera.transform.position = value; }
        }

        public void HandleDrawSettings()
        {
            const float SliderWidth = 150;
            const float SliderSnap = 0.25f;
            const float SliderMin = 0f;
            const float SliderMax = 2f;

            if (this.IsValid)
            {
                var timeScale = GUILayout.HorizontalSlider(this.TimeScale, SliderMin, SliderMax, GUILayout.MaxWidth(SliderWidth));
                timeScale = Mathf.RoundToInt(timeScale / SliderSnap) * SliderSnap;
                this.TimeScale = timeScale;
            }
        }

        public void HandleEditorUpdate()
        {
            this.AdjustCamera();
            if (this.IsPlayingAnimation)
            {
                this.RefreshOnNextUpdate();
                this.Repaint();
            }
            else if (this.requiresRefresh)
            {
                this.Repaint();
            }
        }

        public void Initialize(Action repaintCallback, SkeletonDataAsset skeletonDataAsset, string skinName = "")
        {
            if (skeletonDataAsset == null) return;
            if (skeletonDataAsset.GetSkeletonData(false) == null)
            {
                this.DestroyPreviewGameObject();
                return;
            }

            this.Repaint = repaintCallback;
            this.skeletonDataAsset = skeletonDataAsset;
            this.skeletonData = skeletonDataAsset.GetSkeletonData(false);

            if (this.skeletonData == null)
            {
                this.DestroyPreviewGameObject();
                return;
            }

            const int PreviewLayer = 30;
            const int PreviewCameraCullingMask = 1 << PreviewLayer;

            if (this.previewRenderUtility == null)
            {
                this.previewRenderUtility = new PreviewRenderUtility(true);
                this.animationLastTime = CurrentTime;

                {
                    var c = this.PreviewUtilityCamera;
                    c.orthographic = true;
                    c.cullingMask = PreviewCameraCullingMask;
                    c.nearClipPlane = 0.01f;
                    c.farClipPlane = 1000f;
                    c.orthographicSize = lastCameraOrthoGoal;
                    c.transform.position = lastCameraPositionGoal;
                }

                this.DestroyPreviewGameObject();
            }

            if (this.previewGameObject == null)
            {
                try
                {
                    this.previewGameObject = EditorInstantiation.InstantiateSkeletonAnimation(skeletonDataAsset, skinName, useObjectFactory: false).gameObject;

                    if (this.previewGameObject != null)
                    {
                        this.previewGameObject.hideFlags = HideFlags.HideAndDontSave;
                        this.previewGameObject.layer = PreviewLayer;
                        this.skeletonAnimation = this.previewGameObject.GetComponent<SkeletonAnimation>();
                        this.skeletonAnimation.initialSkinName = skinName;
                        this.skeletonAnimation.LateUpdate();
                        this.previewGameObject.GetComponent<Renderer>().enabled = false;

#if SPINE_UNITY_2018_PREVIEW_API
                        this.previewRenderUtility.AddSingleGO(this.previewGameObject);
#endif
                    }

                    if (this.ActiveTrack != null) this.cameraAdjustEndFrame = EditorApplication.timeSinceStartup + this.skeletonAnimation.AnimationState.GetCurrent(0).Alpha;
                    this.AdjustCameraGoals();
                }
                catch
                {
                    this.DestroyPreviewGameObject();
                }

                this.RefreshOnNextUpdate();
            }
        }

        public void HandleInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (this.requiresRefresh)
                {
                    this.previewRenderUtility.BeginPreview(r, background);
                    this.DoRenderPreview(true);
                    this.previewTexture = this.previewRenderUtility.EndPreview();
                    this.requiresRefresh = false;
                }
                if (this.previewTexture != null)
                    GUI.DrawTexture(r, this.previewTexture, ScaleMode.StretchToFill, false);
            }

            this.DrawSkinToolbar(r);
            //DrawSetupPoseButton(r);
            this.DrawTimeBar(r);
            this.HandleMouseScroll(r);
        }

        public Texture2D GetStaticPreview(int width, int height)
        {
            var c = this.PreviewUtilityCamera;
            if (c == null)
                return null;

            this.RefreshOnNextUpdate();
            this.AdjustCameraGoals();
            c.orthographicSize = this.cameraOrthoGoal / 2;
            c.transform.position = this.cameraPositionGoal;
            this.previewRenderUtility.BeginStaticPreview(new Rect(0, 0, width, height));
            this.DoRenderPreview(false);
            var tex = this.previewRenderUtility.EndStaticPreview();

            return tex;
        }

        public void DoRenderPreview(bool drawHandles)
        {
            if (this.PreviewUtilityCamera.activeTexture == null || this.PreviewUtilityCamera.targetTexture == null)
                return;

            var go = this.previewGameObject;
            if (this.requiresRefresh && go != null)
            {
                var renderer = go.GetComponent<Renderer>();
                renderer.enabled = true;


                if (!EditorApplication.isPlaying)
                {
                    var current = CurrentTime;
                    var deltaTime = (current - this.animationLastTime);
                    this.skeletonAnimation.Update(deltaTime);
                    this.animationLastTime = current;
                    this.skeletonAnimation.LateUpdate();
                }

                var thisPreviewUtilityCamera = this.PreviewUtilityCamera;

                if (drawHandles)
                {
                    Handles.SetCamera(thisPreviewUtilityCamera);
                    Handles.color = this.OriginColor;

                    // Draw Cross
                    var scale = this.skeletonDataAsset.scale;
                    var cl = 1000 * scale;
                    Handles.DrawLine(new Vector3(-cl, 0), new Vector3(cl, 0));
                    Handles.DrawLine(new Vector3(0, cl), new Vector3(0, -cl));
                }

                thisPreviewUtilityCamera.Render();

                if (drawHandles)
                {
                    Handles.SetCamera(thisPreviewUtilityCamera);
                    SpineHandles.DrawBoundingBoxes(this.skeletonAnimation.transform, this.skeletonAnimation.skeleton);
                    if (SkeletonDataAssetInspector.showAttachments)
                        SpineHandles.DrawPaths(this.skeletonAnimation.transform, this.skeletonAnimation.skeleton);
                }

                renderer.enabled = false;
            }
        }

        public void AdjustCamera()
        {
            if (this.previewRenderUtility == null)
                return;

            if (CurrentTime < this.cameraAdjustEndFrame)
                this.AdjustCameraGoals();

            lastCameraPositionGoal = this.cameraPositionGoal;
            lastCameraOrthoGoal = this.cameraOrthoGoal;

            var c = this.PreviewUtilityCamera;
            var orthoSet = Mathf.Lerp(c.orthographicSize, this.cameraOrthoGoal, 0.1f);

            c.orthographicSize = orthoSet;

            var dist = Vector3.Distance(c.transform.position, this.cameraPositionGoal);
            if (dist > 0f)
            {
                var pos = Vector3.Lerp(c.transform.position, this.cameraPositionGoal, 0.1f);
                pos.x = 0;
                c.transform.position = pos;
                c.transform.rotation = Quaternion.identity;
                this.RefreshOnNextUpdate();
            }
        }

        private void AdjustCameraGoals()
        {
            if (this.previewGameObject == null) return;

            var bounds = this.previewGameObject.GetComponent<Renderer>().bounds;
            this.cameraOrthoGoal = bounds.size.y;
            this.cameraPositionGoal = bounds.center + new Vector3(0, 0, -10f);
        }

        private void HandleMouseScroll(Rect position)
        {
            var current = Event.current;
            var controlID = GUIUtility.GetControlID(SliderHash, FocusType.Passive);
            switch (current.GetTypeForControl(controlID))
            {
                case EventType.ScrollWheel:
                    if (position.Contains(current.mousePosition))
                    {
                        this.cameraOrthoGoal += current.delta.y * 0.06f;
                        this.cameraOrthoGoal = Mathf.Max(0.01f, this.cameraOrthoGoal);
                        GUIUtility.hotControl = controlID;
                        current.Use();
                    }
                    break;
            }
        }

        public void RefreshOnNextUpdate()
        {
            this.requiresRefresh = true;
        }

        public void ClearAnimationSetupPose()
        {
            if (this.skeletonAnimation == null)
            {
                Debug.LogWarning("Animation was stopped but preview doesn't exist. It's possible that the Preview Panel is closed.");
            }

            this.skeletonAnimation.AnimationState.ClearTracks();
            this.skeletonAnimation.Skeleton.SetToSetupPose();
        }

        public void PlayPauseAnimation(string animationName, bool loop)
        {
            if (this.skeletonData == null) return;

            if (this.skeletonAnimation == null)
            {
                //Debug.LogWarning("Animation was stopped but preview doesn't exist. It's possible that the Preview Panel is closed.");
                return;
            }

            if (!this.skeletonAnimation.valid) return;

            if (string.IsNullOrEmpty(animationName))
            {
                this.skeletonAnimation.Skeleton.SetToSetupPose();
                this.skeletonAnimation.AnimationState.ClearTracks();
                return;
            }

            var targetAnimation = this.skeletonData.FindAnimation(animationName);
            if (targetAnimation != null)
            {
                var currentTrack = this.ActiveTrack;
                var isEmpty = (currentTrack == null);
                var isNewAnimation = isEmpty || currentTrack.Animation != targetAnimation;

                var skeleton = this.skeletonAnimation.Skeleton;
                var animationState = this.skeletonAnimation.AnimationState;

                if (isEmpty)
                {
                    skeleton.SetToSetupPose();
                    animationState.SetAnimation(0, targetAnimation, loop);
                }
                else
                {
                    var sameAnimation = (currentTrack.Animation == targetAnimation);
                    if (sameAnimation)
                    {
                        currentTrack.TimeScale = (currentTrack.TimeScale == 0) ? 1f : 0f; // pause/play
                    }
                    else
                    {
                        currentTrack.TimeScale = 1f;
                        animationState.SetAnimation(0, targetAnimation, loop);
                    }
                }

                if (isNewAnimation)
                {
                    this.currentAnimationEvents.Clear();
                    this.currentAnimationEventTimes.Clear();
                    foreach (var timeline in targetAnimation.Timelines)
                    {
                        var eventTimeline = timeline as EventTimeline;
                        if (eventTimeline != null)
                        {
                            for (var i = 0; i < eventTimeline.Events.Length; i++)
                            {
                                this.currentAnimationEvents.Add(eventTimeline.Events[i]);
                                this.currentAnimationEventTimes.Add(eventTimeline.Frames[i]);
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogFormat("The Spine.Animation named '{0}' was not found for this Skeleton.", animationName);
            }

        }

        private void DrawSkinToolbar(Rect r)
        {
            if (!this.IsValid) return;

            var skeleton = this.Skeleton;
            var label = (skeleton.Skin != null) ? skeleton.Skin.Name : "default";

            var popRect = new Rect(r);
            popRect.y += 32;
            popRect.x += 4;
            popRect.height = 24;
            popRect.width = 40;
            EditorGUI.DropShadowLabel(popRect, SpineInspectorUtility.TempContent("Skin"));

            popRect.y += 11;
            popRect.width = 150;
            popRect.x += 44;

            if (GUI.Button(popRect, SpineInspectorUtility.TempContent(label, Icons.skin), EditorStyles.popup))
            {
                this.DrawSkinDropdown();
            }
        }

        private void DrawSetupPoseButton(Rect r)
        {
            if (!this.IsValid)
                return;

            var skeleton = this.Skeleton;

            var popRect = new Rect(r);
            popRect.y += 64;
            popRect.x += 4;
            popRect.height = 24;
            popRect.width = 40;

            //popRect.y += 11;
            popRect.width = 150;
            //popRect.x += 44;

            if (GUI.Button(popRect, SpineInspectorUtility.TempContent("Reset to SetupPose", Icons.skeleton)))
            {
                this.ClearAnimationSetupPose();
                this.RefreshOnNextUpdate();
            }
        }

        private void DrawSkinDropdown()
        {
            var menu = new GenericMenu();
            foreach (var s in this.skeletonData.Skins)
                menu.AddItem(new GUIContent(s.Name, Icons.skin), this.skeletonAnimation.skeleton.Skin == s, this.HandleSkinDropdownSelection, s);

            menu.ShowAsContext();
        }

        private void HandleSkinDropdownSelection(object o)
        {
            var skin = (Skin)o;
            this.skeletonAnimation.initialSkinName = skin.Name;
            this.skeletonAnimation.Initialize(true);
            this.RefreshOnNextUpdate();
            if (OnSkinChanged != null) OnSkinChanged(skin.Name);
        }

        private void DrawTimeBar(Rect r)
        {
            if (this.skeletonAnimation == null)
                return;

            var barRect = new Rect(r);
            barRect.height = 32;
            barRect.x += 4;
            barRect.width -= 4;

            GUI.Box(barRect, "");

            var lineRect = new Rect(barRect);
            var lineRectWidth = lineRect.width;
            var t = this.skeletonAnimation.AnimationState.GetCurrent(0);

            if (t != null && Icons.userEvent != null)
            { // when changing to play mode, Icons.userEvent  will not be reset
                var loopCount = (int)(t.TrackTime / t.TrackEnd);
                var currentTime = t.TrackTime - (t.TrackEnd * loopCount);
                var normalizedTime = currentTime / t.Animation.Duration;
                var wrappedTime = normalizedTime % 1f;

                lineRect.x = barRect.x + (lineRectWidth * wrappedTime) - 0.5f;
                lineRect.width = 2;

                GUI.color = Color.red;
                GUI.DrawTexture(lineRect, EditorGUIUtility.whiteTexture);
                GUI.color = Color.white;

                this.currentAnimationEventTooltips = this.currentAnimationEventTooltips ?? new List<SpineEventTooltip>();
                this.currentAnimationEventTooltips.Clear();
                for (var i = 0; i < this.currentAnimationEvents.Count; i++)
                {
                    var eventTime = this.currentAnimationEventTimes[i];
                    var userEventIcon = Icons.userEvent;
                    var iconX = Mathf.Max(((eventTime / t.Animation.Duration) * lineRectWidth) - (userEventIcon.width / 2), barRect.x);
                    var iconY = barRect.y + userEventIcon.height;
                    var evRect = new Rect(barRect)
                    {
                        x = iconX,
                        y = iconY,
                        width = userEventIcon.width,
                        height = userEventIcon.height
                    };
                    GUI.DrawTexture(evRect, userEventIcon);
                    var ev = Event.current;
                    if (ev.type == EventType.Repaint)
                    {
                        if (evRect.Contains(ev.mousePosition))
                        {
                            var eventName = this.currentAnimationEvents[i].Data.Name;
                            var tooltipRect = new Rect(evRect)
                            {
                                width = EditorStyles.helpBox.CalcSize(new GUIContent(eventName)).x
                            };
                            tooltipRect.y -= 4;
                            tooltipRect.y -= tooltipRect.height * this.currentAnimationEventTooltips.Count; // Avoid several overlapping tooltips.
                            tooltipRect.x += 4;

                            // Handle tooltip overflowing to the right.
                            var rightEdgeOverflow = (tooltipRect.x + tooltipRect.width) - (barRect.x + barRect.width);
                            if (rightEdgeOverflow > 0)
                                tooltipRect.x -= rightEdgeOverflow;

                            this.currentAnimationEventTooltips.Add(new SpineEventTooltip { rect = tooltipRect, text = eventName });
                        }
                    }
                }

                // Draw tooltips.
                for (var i = 0; i < this.currentAnimationEventTooltips.Count; i++)
                {
                    GUI.Label(this.currentAnimationEventTooltips[i].rect, this.currentAnimationEventTooltips[i].text, EditorStyles.helpBox);
                    GUI.tooltip = this.currentAnimationEventTooltips[i].text;
                }
            }
        }

        public void OnDestroy()
        {
            this.DisposePreviewRenderUtility();
            this.DestroyPreviewGameObject();
        }

        public void Clear()
        {
            this.DisposePreviewRenderUtility();
            this.DestroyPreviewGameObject();
        }

        private void DisposePreviewRenderUtility()
        {
            if (this.previewRenderUtility != null)
            {
                this.previewRenderUtility.Cleanup();
                this.previewRenderUtility = null;
            }
        }

        private void DestroyPreviewGameObject()
        {
            if (this.previewGameObject != null)
            {
                GameObject.DestroyImmediate(this.previewGameObject);
                this.previewGameObject = null;
            }
        }

        internal struct SpineEventTooltip
        {
            public Rect rect;
            public string text;
        }
    }

}
