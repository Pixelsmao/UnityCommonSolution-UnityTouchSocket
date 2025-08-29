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
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonGraphicCustomMaterials")]
    public class SkeletonGraphicCustomMaterials : MonoBehaviour
    {

        #region Inspector
        public SkeletonGraphic skeletonGraphic;
        [SerializeField] protected List<AtlasMaterialOverride> customMaterialOverrides = new List<AtlasMaterialOverride>();
        [SerializeField] protected List<AtlasTextureOverride> customTextureOverrides = new List<AtlasTextureOverride>();

#if UNITY_EDITOR
        private void Reset()
        {
            this.skeletonGraphic = this.GetComponent<SkeletonGraphic>();

            // Populate material list
            if (this.skeletonGraphic != null && this.skeletonGraphic.skeletonDataAsset != null)
            {
                var atlasAssets = this.skeletonGraphic.skeletonDataAsset.atlasAssets;

                var initialAtlasMaterialOverrides = new List<AtlasMaterialOverride>();
                foreach (var atlasAsset in atlasAssets)
                {
                    foreach (var atlasMaterial in atlasAsset.Materials)
                    {
                        var atlasMaterialOverride = new AtlasMaterialOverride
                        {
                            overrideEnabled = false,
                            originalTexture = atlasMaterial.mainTexture
                        };

                        initialAtlasMaterialOverrides.Add(atlasMaterialOverride);
                    }
                }
                this.customMaterialOverrides = initialAtlasMaterialOverrides;
            }

            // Populate texture list
            if (this.skeletonGraphic != null && this.skeletonGraphic.skeletonDataAsset != null)
            {
                var atlasAssets = this.skeletonGraphic.skeletonDataAsset.atlasAssets;

                var initialAtlasTextureOverrides = new List<AtlasTextureOverride>();
                foreach (var atlasAsset in atlasAssets)
                {
                    foreach (var atlasMaterial in atlasAsset.Materials)
                    {
                        var atlasTextureOverride = new AtlasTextureOverride
                        {
                            overrideEnabled = false,
                            originalTexture = atlasMaterial.mainTexture
                        };

                        initialAtlasTextureOverrides.Add(atlasTextureOverride);
                    }
                }
                this.customTextureOverrides = initialAtlasTextureOverrides;
            }
        }
#endif
        #endregion

        private void SetCustomMaterialOverrides()
        {
            if (this.skeletonGraphic == null)
            {
                Debug.LogError("skeletonGraphic == null");
                return;
            }

            for (var i = 0; i < this.customMaterialOverrides.Count; i++)
            {
                var atlasMaterialOverride = this.customMaterialOverrides[i];
                if (atlasMaterialOverride.overrideEnabled)
                    this.skeletonGraphic.CustomMaterialOverride[atlasMaterialOverride.originalTexture] = atlasMaterialOverride.replacementMaterial;
            }
        }

        private void RemoveCustomMaterialOverrides()
        {
            if (this.skeletonGraphic == null)
            {
                Debug.LogError("skeletonGraphic == null");
                return;
            }

            for (var i = 0; i < this.customMaterialOverrides.Count; i++)
            {
                var atlasMaterialOverride = this.customMaterialOverrides[i];

                if (!this.skeletonGraphic.CustomMaterialOverride.TryGetValue(atlasMaterialOverride.originalTexture, out var currentMaterial))
                    continue;

                // Do not revert the material if it was changed by something else
                if (currentMaterial != atlasMaterialOverride.replacementMaterial)
                    continue;

                this.skeletonGraphic.CustomMaterialOverride.Remove(atlasMaterialOverride.originalTexture);
            }
        }

        private void SetCustomTextureOverrides()
        {
            if (this.skeletonGraphic == null)
            {
                Debug.LogError("skeletonGraphic == null");
                return;
            }

            for (var i = 0; i < this.customTextureOverrides.Count; i++)
            {
                var atlasTextureOverride = this.customTextureOverrides[i];
                if (atlasTextureOverride.overrideEnabled)
                    this.skeletonGraphic.CustomTextureOverride[atlasTextureOverride.originalTexture] = atlasTextureOverride.replacementTexture;
            }
        }

        private void RemoveCustomTextureOverrides()
        {
            if (this.skeletonGraphic == null)
            {
                Debug.LogError("skeletonGraphic == null");
                return;
            }

            for (var i = 0; i < this.customTextureOverrides.Count; i++)
            {
                var atlasTextureOverride = this.customTextureOverrides[i];

                if (!this.skeletonGraphic.CustomTextureOverride.TryGetValue(atlasTextureOverride.originalTexture, out var currentTexture))
                    continue;

                // Do not revert the material if it was changed by something else
                if (currentTexture != atlasTextureOverride.replacementTexture)
                    continue;

                this.skeletonGraphic.CustomTextureOverride.Remove(atlasTextureOverride.originalTexture);
            }
        }

        // OnEnable applies the overrides at runtime, and when the editor loads.
        private void OnEnable()
        {
            if (this.skeletonGraphic == null)
                this.skeletonGraphic = this.GetComponent<SkeletonGraphic>();

            if (this.skeletonGraphic == null)
            {
                Debug.LogError("skeletonGraphic == null");
                return;
            }

            this.skeletonGraphic.Initialize(false);
            this.SetCustomMaterialOverrides();
            this.SetCustomTextureOverrides();
        }

        // OnDisable removes the overrides at runtime, and in the editor when the component is disabled or destroyed.
        private void OnDisable()
        {
            if (this.skeletonGraphic == null)
            {
                Debug.LogError("skeletonGraphic == null");
                return;
            }

            this.RemoveCustomMaterialOverrides();
            this.RemoveCustomTextureOverrides();
        }

        [Serializable]
        public struct AtlasMaterialOverride : IEquatable<AtlasMaterialOverride>
        {
            public bool overrideEnabled;
            public Texture originalTexture;
            public Material replacementMaterial;

            public bool Equals(AtlasMaterialOverride other)
            {
                return this.overrideEnabled == other.overrideEnabled && this.originalTexture == other.originalTexture && this.replacementMaterial == other.replacementMaterial;
            }
        }

        [Serializable]
        public struct AtlasTextureOverride : IEquatable<AtlasTextureOverride>
        {
            public bool overrideEnabled;
            public Texture originalTexture;
            public Texture replacementTexture;

            public bool Equals(AtlasTextureOverride other)
            {
                return this.overrideEnabled == other.overrideEnabled && this.originalTexture == other.originalTexture && this.replacementTexture == other.replacementTexture;
            }
        }
    }
}
