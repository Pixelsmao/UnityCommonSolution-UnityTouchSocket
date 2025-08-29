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

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    using Editor = UnityEditor.Editor;

    [CustomEditor(typeof(AnimationReferenceAsset))]
    public class AnimationReferenceAssetEditor : Editor
    {

        private const string InspectorHelpText = "This is a Spine-Unity Animation Reference Asset. It serializes a reference to a SkeletonDataAsset and an animationName. It does not contain actual animation data. At runtime, it stores a reference to a Spine.Animation.\n\n" +
                "You can use this in your AnimationState calls instead of a string animation name or a Spine.Animation reference. Use its implicit conversion into Spine.Animation or its .Animation property.\n\n" +
                "Use AnimationReferenceAssets as an alternative to storing strings or finding animations and caching per component. This only does the lookup by string once, and allows you to store and manage animations via asset references.";

        private readonly SkeletonInspectorPreview preview = new SkeletonInspectorPreview();
        private readonly FieldInfo skeletonDataAssetField = typeof(AnimationReferenceAsset).GetField("skeletonDataAsset", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo nameField = typeof(AnimationReferenceAsset).GetField("animationName", BindingFlags.NonPublic | BindingFlags.Instance);

        private AnimationReferenceAsset ThisAnimationReferenceAsset
        { get { return this.target as AnimationReferenceAsset; } }
        private SkeletonDataAsset ThisSkeletonDataAsset
        { get { return this.skeletonDataAssetField.GetValue(this.ThisAnimationReferenceAsset) as SkeletonDataAsset; } }
        private string ThisAnimationName
        { get { return this.nameField.GetValue(this.ThisAnimationReferenceAsset) as string; } }

        private bool changeNextFrame = false;
        private SerializedProperty animationNameProperty;
        private SkeletonDataAsset lastSkeletonDataAsset;
        private SkeletonData lastSkeletonData;

        private void OnEnable()
        { this.HandleOnEnablePreview(); }
        private void OnDestroy()
        {
            this.HandleOnDestroyPreview();
            AppDomain.CurrentDomain.DomainUnload -= this.OnDomainUnload;
            EditorApplication.update -= this.preview.HandleEditorUpdate;
        }

        public override void OnInspectorGUI()
        {
            this.animationNameProperty = this.animationNameProperty ?? this.serializedObject.FindProperty("animationName");
            var animationName = this.animationNameProperty.stringValue;

            Animation animation = null;
            if (this.ThisSkeletonDataAsset != null)
            {
                var skeletonData = this.ThisSkeletonDataAsset.GetSkeletonData(true);
                if (skeletonData != null)
                {
                    animation = skeletonData.FindAnimation(animationName);
                }
            }
            var animationNotFound = (animation == null);

            if (this.changeNextFrame)
            {
                this.changeNextFrame = false;

                if (this.ThisSkeletonDataAsset != this.lastSkeletonDataAsset || this.ThisSkeletonDataAsset.GetSkeletonData(true) != this.lastSkeletonData)
                {
                    this.preview.Clear();
                    this.preview.Initialize(this.Repaint, this.ThisSkeletonDataAsset, this.LastSkinName);

                    if (animationNotFound)
                    {
                        this.animationNameProperty.stringValue = "";
                        this.preview.ClearAnimationSetupPose();
                    }
                }

                this.preview.ClearAnimationSetupPose();

                if (!string.IsNullOrEmpty(this.animationNameProperty.stringValue))
                    this.preview.PlayPauseAnimation(this.animationNameProperty.stringValue, true);
            }

            this.lastSkeletonDataAsset = this.ThisSkeletonDataAsset;
            this.lastSkeletonData = this.ThisSkeletonDataAsset.GetSkeletonData(true);

            //EditorGUILayout.HelpBox(AnimationReferenceAssetEditor.InspectorHelpText, MessageType.Info, true);
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            this.DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                this.changeNextFrame = true;
            }

            // Draw extra info below default inspector.
            EditorGUILayout.Space();
            if (this.ThisSkeletonDataAsset == null)
            {
                EditorGUILayout.HelpBox("SkeletonDataAsset is missing.", MessageType.Error);
            }
            else if (string.IsNullOrEmpty(animationName))
            {
                EditorGUILayout.HelpBox("No animation selected.", MessageType.Warning);
            }
            else if (animationNotFound)
            {
                EditorGUILayout.HelpBox(string.Format("Animation named {0} was not found for this Skeleton.", this.animationNameProperty.stringValue), MessageType.Warning);
            }
            else
            {
                using (new SpineInspectorUtility.BoxScope())
                {
                    if (!string.Equals(AssetUtility.GetPathSafeName(animationName), this.ThisAnimationReferenceAsset.name, System.StringComparison.OrdinalIgnoreCase))
                        EditorGUILayout.HelpBox("Animation name value does not match this asset's name. Inspectors using this asset may be misleading.", MessageType.None);

                    EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(animationName, SpineEditorUtilities.Icons.animation));
                    if (animation != null)
                    {
                        EditorGUILayout.LabelField(string.Format("Timelines: {0}", animation.Timelines.Count));
                        EditorGUILayout.LabelField(string.Format("Duration: {0} sec", animation.Duration));
                    }
                }
            }
        }

        #region Preview Handlers
        private string TargetAssetGUID
        { get { return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this.ThisSkeletonDataAsset)); } }
        private string LastSkinKey
        { get { return this.TargetAssetGUID + "_lastSkin"; } }
        private string LastSkinName
        { get { return EditorPrefs.GetString(this.LastSkinKey, ""); } }

        private void HandleOnEnablePreview()
        {
            if (this.ThisSkeletonDataAsset != null && this.ThisSkeletonDataAsset.skeletonJSON == null)
                return;
            SpineEditorUtilities.ConfirmInitialization();

            // This handles the case where the managed editor assembly is unloaded before recompilation when code changes.
            AppDomain.CurrentDomain.DomainUnload -= this.OnDomainUnload;
            AppDomain.CurrentDomain.DomainUnload += this.OnDomainUnload;

            this.preview.Initialize(this.Repaint, this.ThisSkeletonDataAsset, this.LastSkinName);
            this.preview.PlayPauseAnimation(this.ThisAnimationName, true);
            this.preview.OnSkinChanged -= this.HandleOnSkinChanged;
            this.preview.OnSkinChanged += this.HandleOnSkinChanged;
            EditorApplication.update -= this.preview.HandleEditorUpdate;
            EditorApplication.update += this.preview.HandleEditorUpdate;
        }

        private void OnDomainUnload(object sender, EventArgs e)
        {
            this.OnDestroy();
        }

        private void HandleOnSkinChanged(string skinName)
        {
            EditorPrefs.SetString(this.LastSkinKey, skinName);
            this.preview.PlayPauseAnimation(this.ThisAnimationName, true);
        }

        private void HandleOnDestroyPreview()
        {
            EditorApplication.update -= this.preview.HandleEditorUpdate;
            this.preview.OnDestroy();
        }

        public override bool HasPreviewGUI()
        {
            if (this.serializedObject.isEditingMultipleObjects) return false;
            return this.ThisSkeletonDataAsset != null && this.ThisSkeletonDataAsset.GetSkeletonData(true) != null;
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            this.preview.Initialize(this.Repaint, this.ThisSkeletonDataAsset);
            this.preview.HandleInteractivePreviewGUI(r, background);
        }

        public override GUIContent GetPreviewTitle() { return SpineInspectorUtility.TempContent("Preview"); }
        public override void OnPreviewSettings() { this.preview.HandleDrawSettings(); }
        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height) { return this.preview.GetStaticPreview(width, height); }
        #endregion
    }

}
