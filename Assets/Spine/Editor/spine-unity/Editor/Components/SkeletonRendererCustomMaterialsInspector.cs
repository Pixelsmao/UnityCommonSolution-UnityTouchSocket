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

#define SPINE_OPTIONAL_MATERIALOVERRIDE

// Contributed by: Lost Polygon

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{

    // This script is not intended for use with code. See the readme.txt file in SkeletonRendererCustomMaterials folder to learn more.
    [CustomEditor(typeof(SkeletonRendererCustomMaterials))]
    public class SkeletonRendererCustomMaterialsInspector : UnityEditor.Editor
    {
        private List<SkeletonRendererCustomMaterials.AtlasMaterialOverride> componentCustomMaterialOverrides, _customMaterialOverridesPrev;
        private List<SkeletonRendererCustomMaterials.SlotMaterialOverride> componentCustomSlotMaterials, _customSlotMaterialsPrev;
        private SkeletonRendererCustomMaterials component;

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private MethodInfo RemoveCustomMaterialOverrides, RemoveCustomSlotMaterials, SetCustomMaterialOverrides, SetCustomSlotMaterials;

        #region SkeletonRenderer context menu
        [MenuItem("CONTEXT/SkeletonRenderer/Add Basic Serialized Custom Materials")]
        private static void AddSkeletonRendererCustomMaterials(MenuCommand menuCommand)
        {
            var skeletonRenderer = (SkeletonRenderer)menuCommand.context;
            var newComponent = skeletonRenderer.gameObject.AddComponent<SkeletonRendererCustomMaterials>();
            Undo.RegisterCreatedObjectUndo(newComponent, "Add Basic Serialized Custom Materials");
        }

        [MenuItem("CONTEXT/SkeletonRenderer/Add Basic Serialized Custom Materials", true)]
        private static bool AddSkeletonRendererCustomMaterials_Validate(MenuCommand menuCommand)
        {
            var skeletonRenderer = (SkeletonRenderer)menuCommand.context;
            return (skeletonRenderer.GetComponent<SkeletonRendererCustomMaterials>() == null);
        }
        #endregion

        private void OnEnable()
        {
            var cm = typeof(SkeletonRendererCustomMaterials);
            this.RemoveCustomMaterialOverrides = cm.GetMethod("RemoveCustomMaterialOverrides", PrivateInstance);
            this.RemoveCustomSlotMaterials = cm.GetMethod("RemoveCustomSlotMaterials", PrivateInstance);
            this.SetCustomMaterialOverrides = cm.GetMethod("SetCustomMaterialOverrides", PrivateInstance);
            this.SetCustomSlotMaterials = cm.GetMethod("SetCustomSlotMaterials", PrivateInstance);
        }

        public override void OnInspectorGUI()
        {
            this.component = (SkeletonRendererCustomMaterials)this.target;
            var skeletonRenderer = this.component.skeletonRenderer;

            // Draw the default inspector
            this.DrawDefaultInspector();

            if (this.serializedObject.isEditingMultipleObjects)
                return;

            if (this.componentCustomMaterialOverrides == null)
            {
                var cm = typeof(SkeletonRendererCustomMaterials);
                this.componentCustomMaterialOverrides = cm.GetField("customMaterialOverrides", PrivateInstance).GetValue(this.component) as List<SkeletonRendererCustomMaterials.AtlasMaterialOverride>;
                this.componentCustomSlotMaterials = cm.GetField("customSlotMaterials", PrivateInstance).GetValue(this.component) as List<SkeletonRendererCustomMaterials.SlotMaterialOverride>;
                if (this.componentCustomMaterialOverrides == null)
                {
                    Debug.Log("Reflection failed.");
                    return;
                }
            }

            // Fill with current values at start
            if (this._customMaterialOverridesPrev == null || this._customSlotMaterialsPrev == null)
            {
                this._customMaterialOverridesPrev = CopyList(this.componentCustomMaterialOverrides);
                this._customSlotMaterialsPrev = CopyList(this.componentCustomSlotMaterials);
            }

            // Compare new values with saved. If change is detected:
            // store new values, restore old values, remove overrides, restore new values, restore overrides.

            // 1. Store new values
            var customMaterialOverridesNew = CopyList(this.componentCustomMaterialOverrides);
            var customSlotMaterialsNew = CopyList(this.componentCustomSlotMaterials);

            // Detect changes
            if (!this._customMaterialOverridesPrev.SequenceEqual(customMaterialOverridesNew) ||
                !this._customSlotMaterialsPrev.SequenceEqual(customSlotMaterialsNew))
            {
                // 2. Restore old values
                this.componentCustomMaterialOverrides.Clear();
                this.componentCustomSlotMaterials.Clear();
                this.componentCustomMaterialOverrides.AddRange(this._customMaterialOverridesPrev);
                this.componentCustomSlotMaterials.AddRange(this._customSlotMaterialsPrev);

                // 3. Remove overrides
                this.RemoveCustomMaterials();

                // 4. Restore new values
                this.componentCustomMaterialOverrides.Clear();
                this.componentCustomSlotMaterials.Clear();
                this.componentCustomMaterialOverrides.AddRange(customMaterialOverridesNew);
                this.componentCustomSlotMaterials.AddRange(customSlotMaterialsNew);

                // 5. Restore overrides
                this.SetCustomMaterials();

                if (skeletonRenderer != null)
                    skeletonRenderer.LateUpdate();
            }

            this._customMaterialOverridesPrev = CopyList(this.componentCustomMaterialOverrides);
            this._customSlotMaterialsPrev = CopyList(this.componentCustomSlotMaterials);

            if (SpineInspectorUtility.LargeCenteredButton(SpineInspectorUtility.TempContent("Clear and Reapply Changes", tooltip: "Removes all non-serialized overrides in the SkeletonRenderer and reapplies the overrides on this component.")))
            {
                if (skeletonRenderer != null)
                {
#if SPINE_OPTIONAL_MATERIALOVERRIDE
                    skeletonRenderer.CustomMaterialOverride.Clear();
#endif
                    skeletonRenderer.CustomSlotMaterials.Clear();
                    this.RemoveCustomMaterials();
                    this.SetCustomMaterials();
                    skeletonRenderer.LateUpdate();
                }
            }
        }

        private void RemoveCustomMaterials()
        {
            this.RemoveCustomMaterialOverrides.Invoke(this.component, null);
            this.RemoveCustomSlotMaterials.Invoke(this.component, null);
        }

        private void SetCustomMaterials()
        {
            this.SetCustomMaterialOverrides.Invoke(this.component, null);
            this.SetCustomSlotMaterials.Invoke(this.component, null);
        }

        private static List<T> CopyList<T>(List<T> list)
        {
            return list.GetRange(0, list.Count);
        }
    }
}
