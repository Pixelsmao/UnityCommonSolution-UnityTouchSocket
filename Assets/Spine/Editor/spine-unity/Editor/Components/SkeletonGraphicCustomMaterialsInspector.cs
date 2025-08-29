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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{

    // This script is not intended for use with code. See spine-unity documentation page for additional information.
    [CustomEditor(typeof(SkeletonGraphicCustomMaterials))]
    public class SkeletonGraphicCustomMaterialsInspector : UnityEditor.Editor
    {
        private List<SkeletonGraphicCustomMaterials.AtlasMaterialOverride> componentCustomMaterialOverrides, _customMaterialOverridesPrev;
        private List<SkeletonGraphicCustomMaterials.AtlasTextureOverride> componentCustomTextureOverrides, _customTextureOverridesPrev;
        private SkeletonGraphicCustomMaterials component;

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private MethodInfo RemoveCustomMaterialOverrides, RemoveCustomTextureOverrides, SetCustomMaterialOverrides, SetCustomTextureOverrides;

        #region SkeletonGraphic context menu
        [MenuItem("CONTEXT/SkeletonGraphic/Add Basic Serialized Custom Materials")]
        private static void AddSkeletonGraphicCustomMaterials(MenuCommand menuCommand)
        {
            var skeletonGraphic = (SkeletonGraphic)menuCommand.context;
            var newComponent = skeletonGraphic.gameObject.AddComponent<SkeletonGraphicCustomMaterials>();
            Undo.RegisterCreatedObjectUndo(newComponent, "Add Basic Serialized Custom Materials");
        }

        [MenuItem("CONTEXT/SkeletonGraphic/Add Basic Serialized Custom Materials", true)]
        private static bool AddSkeletonGraphicCustomMaterials_Validate(MenuCommand menuCommand)
        {
            var skeletonGraphic = (SkeletonGraphic)menuCommand.context;
            return (skeletonGraphic.GetComponent<SkeletonGraphicCustomMaterials>() == null);
        }
        #endregion

        private void OnEnable()
        {
            var cm = typeof(SkeletonGraphicCustomMaterials);
            this.RemoveCustomMaterialOverrides = cm.GetMethod("RemoveCustomMaterialOverrides", PrivateInstance);
            this.RemoveCustomTextureOverrides = cm.GetMethod("RemoveCustomTextureOverrides", PrivateInstance);
            this.SetCustomMaterialOverrides = cm.GetMethod("SetCustomMaterialOverrides", PrivateInstance);
            this.SetCustomTextureOverrides = cm.GetMethod("SetCustomTextureOverrides", PrivateInstance);
        }

        public override void OnInspectorGUI()
        {
            this.component = (SkeletonGraphicCustomMaterials)this.target;
            var skeletonGraphic = this.component.skeletonGraphic;

            // Draw the default inspector
            this.DrawDefaultInspector();

            if (this.serializedObject.isEditingMultipleObjects)
                return;

            if (this.componentCustomMaterialOverrides == null)
            {
                var cm = typeof(SkeletonGraphicCustomMaterials);
                this.componentCustomMaterialOverrides = cm.GetField("customMaterialOverrides", PrivateInstance).GetValue(this.component) as List<SkeletonGraphicCustomMaterials.AtlasMaterialOverride>;
                this.componentCustomTextureOverrides = cm.GetField("customTextureOverrides", PrivateInstance).GetValue(this.component) as List<SkeletonGraphicCustomMaterials.AtlasTextureOverride>;
                if (this.componentCustomMaterialOverrides == null)
                {
                    Debug.Log("Reflection failed.");
                    return;
                }
            }

            // Fill with current values at start
            if (this._customMaterialOverridesPrev == null || this._customTextureOverridesPrev == null)
            {
                this._customMaterialOverridesPrev = CopyList(this.componentCustomMaterialOverrides);
                this._customTextureOverridesPrev = CopyList(this.componentCustomTextureOverrides);
            }

            // Compare new values with saved. If change is detected:
            // store new values, restore old values, remove overrides, restore new values, restore overrides.

            // 1. Store new values
            var customMaterialOverridesNew = CopyList(this.componentCustomMaterialOverrides);
            var customTextureOverridesNew = CopyList(this.componentCustomTextureOverrides);

            // Detect changes
            if (!this._customMaterialOverridesPrev.SequenceEqual(customMaterialOverridesNew) ||
                !this._customTextureOverridesPrev.SequenceEqual(customTextureOverridesNew))
            {
                // 2. Restore old values
                this.componentCustomMaterialOverrides.Clear();
                this.componentCustomTextureOverrides.Clear();
                this.componentCustomMaterialOverrides.AddRange(this._customMaterialOverridesPrev);
                this.componentCustomTextureOverrides.AddRange(this._customTextureOverridesPrev);

                // 3. Remove overrides
                this.RemoveCustomMaterials();

                // 4. Restore new values
                this.componentCustomMaterialOverrides.Clear();
                this.componentCustomTextureOverrides.Clear();
                this.componentCustomMaterialOverrides.AddRange(customMaterialOverridesNew);
                this.componentCustomTextureOverrides.AddRange(customTextureOverridesNew);

                // 5. Restore overrides
                this.SetCustomMaterials();

                if (skeletonGraphic != null)
                    skeletonGraphic.LateUpdate();
            }

            this._customMaterialOverridesPrev = CopyList(this.componentCustomMaterialOverrides);
            this._customTextureOverridesPrev = CopyList(this.componentCustomTextureOverrides);

            if (SpineInspectorUtility.LargeCenteredButton(SpineInspectorUtility.TempContent("Clear and Reapply Changes", tooltip: "Removes all non-serialized overrides in the SkeletonGraphic and reapplies the overrides on this component.")))
            {
                if (skeletonGraphic != null)
                {
                    skeletonGraphic.CustomMaterialOverride.Clear();
                    skeletonGraphic.CustomTextureOverride.Clear();
                    this.RemoveCustomMaterials();
                    this.SetCustomMaterials();
                    skeletonGraphic.LateUpdate();
                }
            }
        }

        private void RemoveCustomMaterials()
        {
            this.RemoveCustomMaterialOverrides.Invoke(this.component, null);
            this.RemoveCustomTextureOverrides.Invoke(this.component, null);
        }

        private void SetCustomMaterials()
        {
            this.SetCustomMaterialOverrides.Invoke(this.component, null);
            this.SetCustomTextureOverrides.Invoke(this.component, null);
        }

        private static List<T> CopyList<T>(List<T> list)
        {
            return list.GetRange(0, list.Count);
        }
    }
}
