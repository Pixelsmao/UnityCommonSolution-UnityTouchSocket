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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    using Event = UnityEngine.Event;

    [CustomEditor(typeof(SpineSpriteAtlasAsset)), CanEditMultipleObjects]
    public class SpineSpriteAtlasAssetInspector : UnityEditor.Editor
    {
        private SerializedProperty atlasFile, materials;
        private SpineSpriteAtlasAsset atlasAsset;

        private static List<AtlasRegion> GetRegions(Atlas atlas)
        {
            var regionsField = SpineInspectorUtility.GetNonPublicField(typeof(Atlas), "regions");
            return (List<AtlasRegion>)regionsField.GetValue(atlas);
        }

        private void OnEnable()
        {
            SpineEditorUtilities.ConfirmInitialization();
            this.atlasFile = this.serializedObject.FindProperty("spriteAtlasFile");
            this.materials = this.serializedObject.FindProperty("materials");
            this.materials.isExpanded = true;
            this.atlasAsset = (SpineSpriteAtlasAsset)this.target;

            if (!SpineSpriteAtlasAsset.AnySpriteAtlasNeedsRegionsLoaded())
                return;
            EditorApplication.update -= SpineSpriteAtlasAsset.UpdateWhenEditorPlayModeStarted;
            EditorApplication.update += SpineSpriteAtlasAsset.UpdateWhenEditorPlayModeStarted;
        }

        private void OnDisable()
        {
            EditorApplication.update -= SpineSpriteAtlasAsset.UpdateWhenEditorPlayModeStarted;
        }

        public override void OnInspectorGUI()
        {
            if (this.serializedObject.isEditingMultipleObjects)
            {
                this.DrawDefaultInspector();
                return;
            }

            this.serializedObject.Update();
            this.atlasAsset = (this.atlasAsset == null) ? (SpineSpriteAtlasAsset)this.target : this.atlasAsset;

            if (this.atlasAsset.RegionsNeedLoading)
            {
                if (GUILayout.Button(SpineInspectorUtility.TempContent("Load regions by entering Play mode"), GUILayout.Height(20)))
                {
                    EditorApplication.isPlaying = true;
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(this.atlasFile);
            EditorGUILayout.PropertyField(this.materials, true);
            if (EditorGUI.EndChangeCheck())
            {
                this.serializedObject.ApplyModifiedProperties();
                this.atlasAsset.Clear();
                this.atlasAsset.GetAtlas();
                this.atlasAsset.updateRegionsInPlayMode = true;
            }

            if (this.materials.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No materials", MessageType.Error);
                return;
            }

            for (var i = 0; i < this.materials.arraySize; i++)
            {
                var prop = this.materials.GetArrayElementAtIndex(i);
                var material = (Material)prop.objectReferenceValue;
                if (material == null)
                {
                    EditorGUILayout.HelpBox("Materials cannot be null.", MessageType.Error);
                    return;
                }
            }

            if (this.atlasFile.objectReferenceValue != null)
            {
                var baseIndent = EditorGUI.indentLevel;

                var regions = SpineSpriteAtlasAssetInspector.GetRegions(this.atlasAsset.GetAtlas());
                var regionsCount = regions.Count;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Atlas Regions", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(string.Format("{0} regions total", regionsCount));
                }
                AtlasPage lastPage = null;
                for (var i = 0; i < regionsCount; i++)
                {
                    if (lastPage != regions[i].page)
                    {
                        if (lastPage != null)
                        {
                            EditorGUILayout.Separator();
                            EditorGUILayout.Separator();
                        }
                        lastPage = regions[i].page;
                        var mat = ((Material)lastPage.rendererObject);
                        if (mat != null)
                        {
                            EditorGUI.indentLevel = baseIndent;
                            using (new GUILayout.HorizontalScope())
                            using (new EditorGUI.DisabledGroupScope(true))
                                EditorGUILayout.ObjectField(mat, typeof(Material), false, GUILayout.Width(250));
                            EditorGUI.indentLevel = baseIndent + 1;
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Page missing material!", MessageType.Warning);
                        }
                    }

                    var regionName = regions[i].name;
                    var icon = SpineEditorUtilities.Icons.image;
                    if (regionName.EndsWith(" "))
                    {
                        regionName = string.Format("'{0}'", regions[i].name);
                        icon = SpineEditorUtilities.Icons.warning;
                        EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(regionName, icon, "Region name ends with whitespace. This may cause errors. Please check your source image filenames."));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(regionName, icon));
                    }
                }
                EditorGUI.indentLevel = baseIndent;
            }

            if (this.serializedObject.ApplyModifiedProperties() || SpineInspectorUtility.UndoRedoPerformed(Event.current))
                this.atlasAsset.Clear();
        }
    }

}
