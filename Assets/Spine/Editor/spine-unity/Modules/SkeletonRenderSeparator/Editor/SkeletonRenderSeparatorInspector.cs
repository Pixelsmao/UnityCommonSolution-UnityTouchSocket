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

using Spine.Unity.Editor;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Examples
{

    [CustomEditor(typeof(SkeletonRenderSeparator))]
    public class SkeletonRenderSeparatorInspector : UnityEditor.Editor
    {
        private SkeletonRenderSeparator component;

        // Properties
        private SerializedProperty skeletonRenderer_, copyPropertyBlock_, copyMeshRendererFlags_, partsRenderers_;
        private static bool partsRenderersExpanded = false;

        // For separator field.
        private SerializedObject skeletonRendererSerializedObject;
        private SerializedProperty separatorNamesProp;
        private static bool skeletonRendererExpanded = true;
        private bool slotsReapplyRequired = false;
        private bool partsRendererInitRequired = false;

        private void OnEnable()
        {
            if (this.component == null)
                this.component = this.target as SkeletonRenderSeparator;

            this.skeletonRenderer_ = this.serializedObject.FindProperty("skeletonRenderer");
            this.copyPropertyBlock_ = this.serializedObject.FindProperty("copyPropertyBlock");
            this.copyMeshRendererFlags_ = this.serializedObject.FindProperty("copyMeshRendererFlags");

            var partsRenderers = this.component.partsRenderers;
            this.partsRenderers_ = this.serializedObject.FindProperty("partsRenderers");
            this.partsRenderers_.isExpanded = partsRenderersExpanded || // last state
                partsRenderers.Contains(null) ||    // null items found
                partsRenderers.Count < 1 ||         // no parts renderers
                (this.skeletonRenderer_.objectReferenceValue != null && this.SkeletonRendererSeparatorCount + 1 > partsRenderers.Count); // not enough parts renderers
        }

        private int SkeletonRendererSeparatorCount
        {
            get
            {
                if (Application.isPlaying)
                    return this.component.SkeletonRenderer.separatorSlots.Count;
                else
                    return this.separatorNamesProp == null ? 0 : this.separatorNamesProp.arraySize;
            }
        }

        public override void OnInspectorGUI()
        {

            // Restore mesh part for undo logic after undo of "Add Parts Renderer".
            // Triggers regeneration and assignment of the mesh filter's mesh.

            var isMeshFilterAlwaysNull = false;
#if UNITY_EDITOR && NEW_PREFAB_SYSTEM
            // Don't store mesh or material at the prefab, otherwise it will permanently reload
            var prefabType = UnityEditor.PrefabUtility.GetPrefabAssetType(this.component);
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this.component) &&
                (prefabType == UnityEditor.PrefabAssetType.Regular || prefabType == UnityEditor.PrefabAssetType.Variant))
            {
                isMeshFilterAlwaysNull = true;
            }
#endif

            if (!isMeshFilterAlwaysNull && this.component.GetComponent<MeshFilter>() && this.component.GetComponent<MeshFilter>().sharedMesh == null)
            {
                this.component.OnDisable();
                this.component.OnEnable();
            }

            var componentRenderers = this.component.partsRenderers;
            int totalParts;

            using (new SpineInspectorUtility.LabelWidthScope())
            {
                var componentEnabled = this.component.enabled;
                var checkBox = EditorGUILayout.Toggle("Enable Separator", componentEnabled);
                if (checkBox != componentEnabled)
                    this.component.enabled = checkBox;
                if (this.component.SkeletonRenderer.disableRenderingOnOverride && !this.component.enabled)
                    EditorGUILayout.HelpBox("By default, SkeletonRenderer's MeshRenderer is disabled while the SkeletonRenderSeparator takes over rendering. It is re-enabled when SkeletonRenderSeparator is disabled.", MessageType.Info);

                EditorGUILayout.PropertyField(this.copyPropertyBlock_);
                EditorGUILayout.PropertyField(this.copyMeshRendererFlags_);
            }

            // SkeletonRenderer Box
            using (new SpineInspectorUtility.BoxScope(false))
            {
                // Fancy SkeletonRenderer foldout reference field
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    var foldoutSkeletonRendererRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(foldoutSkeletonRendererRect, this.skeletonRenderer_);
                    if (EditorGUI.EndChangeCheck())
                        this.serializedObject.ApplyModifiedProperties();
                    if (this.component.SkeletonRenderer != null)
                    {
                        skeletonRendererExpanded = EditorGUI.Foldout(foldoutSkeletonRendererRect, skeletonRendererExpanded, "");
                    }
                    EditorGUI.indentLevel--;
                }

                var separatorCount = 0;
                EditorGUI.BeginChangeCheck();
                if (this.component.SkeletonRenderer != null)
                {
                    // Separators from SkeletonRenderer
                    {
                        var skeletonRendererMismatch = this.skeletonRendererSerializedObject != null && this.skeletonRendererSerializedObject.targetObject != this.component.SkeletonRenderer;
                        if (this.separatorNamesProp == null || skeletonRendererMismatch)
                        {
                            if (this.component.SkeletonRenderer != null)
                            {
                                this.skeletonRendererSerializedObject = new SerializedObject(this.component.SkeletonRenderer);
                                this.separatorNamesProp = this.skeletonRendererSerializedObject.FindProperty("separatorSlotNames");
                                this.separatorNamesProp.isExpanded = true;
                            }
                        }

                        if (this.separatorNamesProp != null)
                        {
                            if (skeletonRendererExpanded)
                            {
                                EditorGUI.indentLevel++;
                                SkeletonRendererInspector.SeparatorsField(this.separatorNamesProp);
                                EditorGUI.indentLevel--;
                            }
                            separatorCount = this.SkeletonRendererSeparatorCount;
                        }
                    }

                    if (this.SkeletonRendererSeparatorCount == 0)
                    {
                        EditorGUILayout.HelpBox("Separators are empty. Change the size to 1 and choose a slot if you want the render to be separated.", MessageType.Info);
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    this.skeletonRendererSerializedObject.ApplyModifiedProperties();

                    if (!Application.isPlaying)
                        this.slotsReapplyRequired = true;
                }


                totalParts = separatorCount + 1;
                var counterStyle = skeletonRendererExpanded ? EditorStyles.label : EditorStyles.miniLabel;
                EditorGUILayout.LabelField(string.Format("{0}: separates into {1}.", SpineInspectorUtility.Pluralize(separatorCount, "separator", "separators"), SpineInspectorUtility.Pluralize(totalParts, "part", "parts")), counterStyle);
            }

            // Parts renderers
            using (new SpineInspectorUtility.BoxScope(false))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(this.partsRenderers_, true);
                EditorGUI.indentLevel--;

                // Null items warning
                var nullItemsFound = componentRenderers.Contains(null);
                if (nullItemsFound)
                    EditorGUILayout.HelpBox("Some items in the parts renderers list are null and may cause problems.\n\nYou can right-click on that element and choose 'Delete Array Element' to remove it.", MessageType.Warning);

                // (Button) Match Separators count
                if (this.separatorNamesProp != null)
                {
                    var currentRenderers = 0;
                    foreach (var r in componentRenderers)
                    {
                        if (r != null)
                            currentRenderers++;
                    }
                    var extraRenderersNeeded = totalParts - currentRenderers;

                    if (this.component.enabled && this.component.SkeletonRenderer != null && extraRenderersNeeded > 0)
                    {
                        EditorGUILayout.HelpBox(string.Format("Insufficient parts renderers. Some parts will not be rendered."), MessageType.Warning);
                        var addMissingLabel = string.Format("Add the missing renderer{1} ({0}) ", extraRenderersNeeded, SpineInspectorUtility.PluralThenS(extraRenderersNeeded));
                        if (GUILayout.Button(addMissingLabel, GUILayout.Height(30f)))
                        {
                            this.AddPartsRenderer(extraRenderersNeeded);
                            this.DetectOrphanedPartsRenderers(this.component);
                            this.partsRendererInitRequired = true;
                        }
                    }
                }

                if (this.partsRenderers_.isExpanded != partsRenderersExpanded) partsRenderersExpanded = this.partsRenderers_.isExpanded;
                if (this.partsRenderers_.isExpanded)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // (Button) Destroy Renderers button
                        if (componentRenderers.Count > 0)
                        {
                            if (GUILayout.Button("Clear Parts Renderers"))
                            {
                                // Do you really want to destroy all?
                                Undo.RegisterCompleteObjectUndo(this.component, "Clear Parts Renderers");
                                if (EditorUtility.DisplayDialog("Destroy Renderers", "Do you really want to destroy all the Parts Renderer GameObjects in the list?", "Destroy", "Cancel"))
                                {
                                    foreach (var r in componentRenderers)
                                    {
                                        if (r != null)
                                            Undo.DestroyObjectImmediate(r.gameObject);
                                    }
                                    componentRenderers.Clear();
                                    // Do you also want to destroy orphans? (You monster.)
                                    this.DetectOrphanedPartsRenderers(this.component);
                                }
                            }
                        }

                        // (Button) Add Part Renderer button
                        if (GUILayout.Button("Add Parts Renderer"))
                        {
                            this.AddPartsRenderer(1);
                            this.partsRendererInitRequired = true;
                        }
                    }
                }
            }

            this.serializedObject.ApplyModifiedProperties();

            if (this.partsRendererInitRequired)
            {
                Undo.RegisterCompleteObjectUndo(this.component.GetComponent<MeshRenderer>(), "Add Parts Renderers");
                this.component.OnEnable();
                this.partsRendererInitRequired = false;
            }

            if (this.slotsReapplyRequired && UnityEngine.Event.current.type == EventType.Repaint)
            {
                this.component.SkeletonRenderer.ReapplySeparatorSlotNames();
                this.component.SkeletonRenderer.LateUpdate();
                SceneView.RepaintAll();
                this.slotsReapplyRequired = false;
            }
        }

        public void AddPartsRenderer(int count)
        {
            var componentRenderers = this.component.partsRenderers;
            var emptyFound = componentRenderers.Contains(null);
            if (emptyFound)
            {
                var userClearEntries = EditorUtility.DisplayDialog("Empty entries found", "Null entries found. Do you want to remove null entries before adding the new renderer? ", "Clear Empty Entries", "Don't Clear");
                if (userClearEntries) componentRenderers.RemoveAll(x => x == null);
            }

            Undo.RegisterCompleteObjectUndo(this.component, "Add Parts Renderers");
            for (var i = 0; i < count; i++)
            {
                var index = componentRenderers.Count;
                var smr = SkeletonPartsRenderer.NewPartsRendererGameObject(this.component.transform, index.ToString());
                Undo.RegisterCreatedObjectUndo(smr.gameObject, "New Parts Renderer GameObject.");
                componentRenderers.Add(smr);

                // increment renderer sorting order.
                if (index == 0) continue;
                var prev = componentRenderers[index - 1]; if (prev == null) continue;

                var prevMeshRenderer = prev.GetComponent<MeshRenderer>();
                var currentMeshRenderer = smr.GetComponent<MeshRenderer>();
                if (prevMeshRenderer == null || currentMeshRenderer == null) continue;

                var prevSortingLayer = prevMeshRenderer.sortingLayerID;
                var prevSortingOrder = prevMeshRenderer.sortingOrder;
                currentMeshRenderer.sortingLayerID = prevSortingLayer;
                currentMeshRenderer.sortingOrder = prevSortingOrder + SkeletonRenderSeparator.DefaultSortingOrderIncrement;
            }

        }

        /// <summary>Detects orphaned parts renderers and offers to delete them.</summary>
        public void DetectOrphanedPartsRenderers(SkeletonRenderSeparator component)
        {
            var children = component.GetComponentsInChildren<SkeletonPartsRenderer>();

            var orphans = new System.Collections.Generic.List<SkeletonPartsRenderer>();
            foreach (var r in children)
            {
                if (!component.partsRenderers.Contains(r))
                    orphans.Add(r);
            }

            if (orphans.Count > 0)
            {
                if (EditorUtility.DisplayDialog("Destroy Submesh Renderers", "Unassigned renderers were found. Do you want to delete them? (These may belong to another Render Separator in the same hierarchy. If you don't have another Render Separator component in the children of this GameObject, it's likely safe to delete. Warning: This operation cannot be undone.)", "Delete", "Cancel"))
                {
                    foreach (var o in orphans)
                    {
                        Undo.DestroyObjectImmediate(o.gameObject);
                    }
                }
            }
        }

        #region SkeletonRenderer Context Menu Item
        [MenuItem("CONTEXT/SkeletonRenderer/Add Skeleton Render Separator")]
        private static void AddRenderSeparatorComponent(MenuCommand cmd)
        {
            var skeletonRenderer = cmd.context as SkeletonRenderer;
            var newComponent = skeletonRenderer.gameObject.AddComponent<SkeletonRenderSeparator>();

            Undo.RegisterCreatedObjectUndo(newComponent, "Add SkeletonRenderSeparator");
        }

        // Validate
        [MenuItem("CONTEXT/SkeletonRenderer/Add Skeleton Render Separator", true)]
        private static bool ValidateAddRenderSeparatorComponent(MenuCommand cmd)
        {
            var skeletonRenderer = cmd.context as SkeletonRenderer;
            var separator = skeletonRenderer.GetComponent<SkeletonRenderSeparator>();
            var separatorNotOnObject = separator == null;
            return separatorNotOnObject;
        }
        #endregion

    }
}
