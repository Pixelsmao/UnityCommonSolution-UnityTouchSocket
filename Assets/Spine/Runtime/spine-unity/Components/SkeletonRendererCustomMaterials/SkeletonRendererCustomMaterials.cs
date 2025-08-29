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
#define SPINE_OPTIONAL_MATERIALOVERRIDE

// Contributed by: Lost Polygon

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spine.Unity
{
#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonRendererCustomMaterials")]
    public class SkeletonRendererCustomMaterials : MonoBehaviour
    {

        #region Inspector
        public SkeletonRenderer skeletonRenderer;
        [SerializeField] protected List<SlotMaterialOverride> customSlotMaterials = new List<SlotMaterialOverride>();
        [SerializeField] protected List<AtlasMaterialOverride> customMaterialOverrides = new List<AtlasMaterialOverride>();

#if UNITY_EDITOR
        private void Reset()
        {
            this.skeletonRenderer = this.GetComponent<SkeletonRenderer>();

            // Populate atlas list
            if (this.skeletonRenderer != null && this.skeletonRenderer.skeletonDataAsset != null)
            {
                var atlasAssets = this.skeletonRenderer.skeletonDataAsset.atlasAssets;

                var initialAtlasMaterialOverrides = new List<AtlasMaterialOverride>();
                foreach (var atlasAsset in atlasAssets)
                {
                    foreach (var atlasMaterial in atlasAsset.Materials)
                    {
                        var atlasMaterialOverride = new AtlasMaterialOverride
                        {
                            overrideDisabled = true,
                            originalMaterial = atlasMaterial
                        };

                        initialAtlasMaterialOverrides.Add(atlasMaterialOverride);
                    }
                }

                this.customMaterialOverrides = initialAtlasMaterialOverrides;
            }
        }
#endif
        #endregion

        private void SetCustomSlotMaterials()
        {
            if (this.skeletonRenderer == null)
            {
                Debug.LogError("skeletonRenderer == null");
                return;
            }

            for (var i = 0; i < this.customSlotMaterials.Count; i++)
            {
                var slotMaterialOverride = this.customSlotMaterials[i];
                if (slotMaterialOverride.overrideDisabled || string.IsNullOrEmpty(slotMaterialOverride.slotName))
                    continue;

                var slotObject = this.skeletonRenderer.skeleton.FindSlot(slotMaterialOverride.slotName);
                this.skeletonRenderer.CustomSlotMaterials[slotObject] = slotMaterialOverride.material;
            }
        }

        private void RemoveCustomSlotMaterials()
        {
            if (this.skeletonRenderer == null)
            {
                Debug.LogError("skeletonRenderer == null");
                return;
            }

            for (var i = 0; i < this.customSlotMaterials.Count; i++)
            {
                var slotMaterialOverride = this.customSlotMaterials[i];
                if (string.IsNullOrEmpty(slotMaterialOverride.slotName))
                    continue;

                var slotObject = this.skeletonRenderer.skeleton.FindSlot(slotMaterialOverride.slotName);

                if (!this.skeletonRenderer.CustomSlotMaterials.TryGetValue(slotObject, out var currentMaterial))
                    continue;

                // Do not revert the material if it was changed by something else
                if (currentMaterial != slotMaterialOverride.material)
                    continue;

                this.skeletonRenderer.CustomSlotMaterials.Remove(slotObject);
            }
        }

        private void SetCustomMaterialOverrides()
        {
            if (this.skeletonRenderer == null)
            {
                Debug.LogError("skeletonRenderer == null");
                return;
            }

#if SPINE_OPTIONAL_MATERIALOVERRIDE
            for (var i = 0; i < this.customMaterialOverrides.Count; i++)
            {
                var atlasMaterialOverride = this.customMaterialOverrides[i];
                if (atlasMaterialOverride.overrideDisabled)
                    continue;

                this.skeletonRenderer.CustomMaterialOverride[atlasMaterialOverride.originalMaterial] = atlasMaterialOverride.replacementMaterial;
            }
#endif
        }

        private void RemoveCustomMaterialOverrides()
        {
            if (this.skeletonRenderer == null)
            {
                Debug.LogError("skeletonRenderer == null");
                return;
            }

#if SPINE_OPTIONAL_MATERIALOVERRIDE
            for (var i = 0; i < this.customMaterialOverrides.Count; i++)
            {
                var atlasMaterialOverride = this.customMaterialOverrides[i];

                if (!this.skeletonRenderer.CustomMaterialOverride.TryGetValue(atlasMaterialOverride.originalMaterial, out var currentMaterial))
                    continue;

                // Do not revert the material if it was changed by something else
                if (currentMaterial != atlasMaterialOverride.replacementMaterial)
                    continue;

                this.skeletonRenderer.CustomMaterialOverride.Remove(atlasMaterialOverride.originalMaterial);
            }
#endif
        }

        // OnEnable applies the overrides at runtime, and when the editor loads.
        private void OnEnable()
        {
            if (this.skeletonRenderer == null)
                this.skeletonRenderer = this.GetComponent<SkeletonRenderer>();

            if (this.skeletonRenderer == null)
            {
                Debug.LogError("skeletonRenderer == null");
                return;
            }

            this.skeletonRenderer.Initialize(false);
            this.SetCustomMaterialOverrides();
            this.SetCustomSlotMaterials();
        }

        // OnDisable removes the overrides at runtime, and in the editor when the component is disabled or destroyed.
        private void OnDisable()
        {
            if (this.skeletonRenderer == null)
            {
                Debug.LogError("skeletonRenderer == null");
                return;
            }

            this.RemoveCustomMaterialOverrides();
            this.RemoveCustomSlotMaterials();
        }

        [Serializable]
        public struct SlotMaterialOverride : IEquatable<SlotMaterialOverride>
        {
            public bool overrideDisabled;

            [SpineSlot]
            public string slotName;
            public Material material;

            public bool Equals(SlotMaterialOverride other)
            {
                return this.overrideDisabled == other.overrideDisabled && this.slotName == other.slotName && this.material == other.material;
            }
        }

        [Serializable]
        public struct AtlasMaterialOverride : IEquatable<AtlasMaterialOverride>
        {
            public bool overrideDisabled;
            public Material originalMaterial;
            public Material replacementMaterial;

            public bool Equals(AtlasMaterialOverride other)
            {
                return this.overrideDisabled == other.overrideDisabled && this.originalMaterial == other.originalMaterial && this.replacementMaterial == other.replacementMaterial;
            }
        }
    }
}
