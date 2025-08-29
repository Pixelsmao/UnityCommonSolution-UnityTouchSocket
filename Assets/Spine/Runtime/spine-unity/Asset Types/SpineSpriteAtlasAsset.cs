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

#if UNITY_2018_2_OR_NEWER
#define EXPOSES_SPRITE_ATLAS_UTILITIES
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Spine.Unity
{
    /// <summary>Loads and stores a Spine atlas and list of materials.</summary>
    [CreateAssetMenu(fileName = "New Spine SpriteAtlas Asset", menuName = "Spine/Spine SpriteAtlas Asset")]
    public class SpineSpriteAtlasAsset : AtlasAssetBase
    {
        public SpriteAtlas spriteAtlasFile;
        public Material[] materials;
        protected Atlas atlas;
        public bool updateRegionsInPlayMode;

        [System.Serializable]
        protected class SavedRegionInfo
        {
            public float x, y, width, height;
            public SpritePackingRotation packingRotation;
        }
        [SerializeField] protected SavedRegionInfo[] savedRegions;

        public override bool IsLoaded { get { return this.atlas != null; } }

        public override IEnumerable<Material> Materials { get { return this.materials; } }
        public override int MaterialCount { get { return this.materials == null ? 0 : this.materials.Length; } }
        public override Material PrimaryMaterial { get { return this.materials[0]; } }

#if UNITY_EDITOR
        private static MethodInfo GetPackedSpritesMethod, GetPreviewTexturesMethod;
#if !EXPOSES_SPRITE_ATLAS_UTILITIES
		static MethodInfo PackAtlasesMethod;
#endif
#endif

        #region Runtime Instantiation
        /// <summary>
        /// Creates a runtime AtlasAsset</summary>
        public static SpineSpriteAtlasAsset CreateRuntimeInstance(SpriteAtlas spriteAtlasFile, Material[] materials, bool initialize)
        {
            var atlasAsset = ScriptableObject.CreateInstance<SpineSpriteAtlasAsset>();
            atlasAsset.Reset();
            atlasAsset.spriteAtlasFile = spriteAtlasFile;
            atlasAsset.materials = materials;

            if (initialize)
                atlasAsset.GetAtlas();

            return atlasAsset;
        }
        #endregion

        private void Reset()
        {
            this.Clear();
        }

        public override void Clear()
        {
            this.atlas = null;
        }

        /// <returns>The atlas or null if it could not be loaded.</returns>
        public override Atlas GetAtlas()
        {
            if (this.spriteAtlasFile == null)
            {
                Debug.LogError("SpriteAtlas file not set for SpineSpriteAtlasAsset: " + this.name, this);
                this.Clear();
                return null;
            }

            if (this.materials == null || this.materials.Length == 0)
            {
                Debug.LogError("Materials not set for SpineSpriteAtlasAsset: " + this.name, this);
                this.Clear();
                return null;
            }

            if (this.atlas != null) return this.atlas;

            try
            {
                this.atlas = this.LoadAtlas(this.spriteAtlasFile);
                return this.atlas;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error analyzing SpriteAtlas for SpineSpriteAtlasAsset: " + this.name + "\n" + ex.Message + "\n" + ex.StackTrace, this);
                return null;
            }
        }

        protected void AssignRegionsFromSavedRegions(Sprite[] sprites, Atlas usedAtlas)
        {

            if (this.savedRegions == null || this.savedRegions.Length != sprites.Length)
                return;

            var i = 0;
            foreach (var region in usedAtlas)
            {
                var savedRegion = this.savedRegions[i];
                var page = region.page;

                region.degrees = savedRegion.packingRotation == SpritePackingRotation.None ? 0 : 90;
                region.rotate = region.degrees != 0;

                var x = savedRegion.x;
                var y = savedRegion.y;
                var width = savedRegion.width;
                var height = savedRegion.height;

                region.u = x / page.width;
                region.v = y / page.height;
                if (region.rotate)
                {
                    region.u2 = (x + height) / page.width;
                    region.v2 = (y + width) / page.height;
                }
                else
                {
                    region.u2 = (x + width) / page.width;
                    region.v2 = (y + height) / page.height;
                }
                region.x = (int)x;
                region.y = (int)y;
                region.width = Math.Abs((int)width);
                region.height = Math.Abs((int)height);

                // flip upside down
                var temp = region.v;
                region.v = region.v2;
                region.v2 = temp;

                region.originalWidth = (int)width;
                region.originalHeight = (int)height;

                // note: currently sprite pivot offsets are ignored.
                // var sprite = sprites[i];
                region.offsetX = 0;//sprite.pivot.x;
                region.offsetY = 0;//sprite.pivot.y;

                ++i;
            }
        }

        private Atlas LoadAtlas(UnityEngine.U2D.SpriteAtlas spriteAtlas)
        {

            var pages = new List<AtlasPage>();
            var regions = new List<AtlasRegion>();

            var sprites = new UnityEngine.Sprite[spriteAtlas.spriteCount];
            spriteAtlas.GetSprites(sprites);
            if (sprites.Length == 0)
                return new Atlas(pages, regions);

            Texture2D texture = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                texture = AccessPackedTextureEditor(spriteAtlas);
            else
#endif
                texture = AccessPackedTexture(sprites);

            var material = this.materials[0];
#if !UNITY_EDITOR
			material.mainTexture = texture;
#endif

            var page = new AtlasPage();
            page.name = spriteAtlas.name;
            page.width = texture.width;
            page.height = texture.height;
            page.format = Spine.Format.RGBA8888;

            page.minFilter = TextureFilter.Linear;
            page.magFilter = TextureFilter.Linear;
            page.uWrap = TextureWrap.ClampToEdge;
            page.vWrap = TextureWrap.ClampToEdge;
            page.rendererObject = material;
            pages.Add(page);

            sprites = AccessPackedSprites(spriteAtlas);

            var i = 0;
            for (; i < sprites.Length; ++i)
            {
                var sprite = sprites[i];
                var region = new AtlasRegion();
                region.name = sprite.name.Replace("(Clone)", "");
                region.page = page;
                region.degrees = sprite.packingRotation == SpritePackingRotation.None ? 0 : 90;
                region.rotate = region.degrees != 0;

                region.u2 = 1;
                region.v2 = 1;
                region.width = page.width;
                region.height = page.height;
                region.originalWidth = page.width;
                region.originalHeight = page.height;

                region.index = i;
                regions.Add(region);
            }

            var atlas = new Atlas(pages, regions);
            this.AssignRegionsFromSavedRegions(sprites, atlas);

            return atlas;
        }

#if UNITY_EDITOR
        public static void UpdateByStartingEditorPlayMode()
        {
            EditorApplication.isPlaying = true;
        }

        public static bool AnySpriteAtlasNeedsRegionsLoaded()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:SpineSpriteAtlasAsset");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var atlasAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<SpineSpriteAtlasAsset>(path);
                    if (atlasAsset)
                    {
                        if (atlasAsset.RegionsNeedLoading)
                            return true;
                    }
                }
            }
            return false;
        }

        public static void UpdateWhenEditorPlayModeStarted()
        {
            if (!EditorApplication.isPlaying)
                return;

            EditorApplication.update -= UpdateWhenEditorPlayModeStarted;
            var guids = UnityEditor.AssetDatabase.FindAssets("t:SpineSpriteAtlasAsset");
            if (guids.Length == 0)
                return;

            Debug.Log("Updating SpineSpriteAtlasAssets");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var atlasAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<SpineSpriteAtlasAsset>(path);
                    if (atlasAsset)
                    {
                        atlasAsset.atlas = atlasAsset.LoadAtlas(atlasAsset.spriteAtlasFile);
                        atlasAsset.LoadRegionsInEditorPlayMode();
                        Debug.Log(string.Format("Updated regions of '{0}'", atlasAsset.name), atlasAsset);
                    }
                }
            }

            EditorApplication.isPlaying = false;
        }

        public bool RegionsNeedLoading
        {
            get { return this.savedRegions == null || this.savedRegions.Length == 0 || this.updateRegionsInPlayMode; }
        }

        public void LoadRegionsInEditorPlayMode()
        {

            Sprite[] sprites = null;
            var T = Type.GetType("UnityEditor.U2D.SpriteAtlasExtensions,UnityEditor");
            var method = T.GetMethod("GetPackedSprites", BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                var retval = method.Invoke(null, new object[] { this.spriteAtlasFile });
                var spritesArray = retval as Sprite[];
                if (spritesArray != null && spritesArray.Length > 0)
                {
                    sprites = spritesArray;
                }
            }
            if (sprites == null)
            {
                sprites = new UnityEngine.Sprite[this.spriteAtlasFile.spriteCount];
                this.spriteAtlasFile.GetSprites(sprites);
            }
            if (sprites.Length == 0)
            {
                Debug.LogWarning(string.Format("SpriteAtlas '{0}' contains no sprites. Please make sure all assigned images are set to import type 'Sprite'.", this.spriteAtlasFile.name), this.spriteAtlasFile);
                return;
            }
            else if (sprites[0].packingMode == SpritePackingMode.Tight)
            {
                Debug.LogError(string.Format("SpriteAtlas '{0}': Tight packing is not supported. Please disable 'Tight Packing' in the SpriteAtlas Inspector.", this.spriteAtlasFile.name), this.spriteAtlasFile);
                return;
            }

            if (this.savedRegions == null || this.savedRegions.Length != sprites.Length)
                this.savedRegions = new SavedRegionInfo[sprites.Length];

            var i = 0;
            foreach (var region in this.atlas)
            {
                var sprite = sprites[i];
                var rect = sprite.textureRect;
                var x = rect.min.x;
                var y = rect.min.y;
                var width = rect.width;
                var height = rect.height;

                var savedRegion = new SavedRegionInfo();
                savedRegion.x = x;
                savedRegion.y = y;
                savedRegion.width = width;
                savedRegion.height = height;
                savedRegion.packingRotation = sprite.packingRotation;
                this.savedRegions[i] = savedRegion;

                ++i;
            }
            this.updateRegionsInPlayMode = false;
            this.AssignRegionsFromSavedRegions(sprites, this.atlas);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public static Texture2D AccessPackedTextureEditor(SpriteAtlas spriteAtlas)
        {
#if EXPOSES_SPRITE_ATLAS_UTILITIES
            UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget);
#else
			/*if (PackAtlasesMethod == null) {
				System.Type T = Type.GetType("UnityEditor.U2D.SpriteAtlasUtility,UnityEditor");
				PackAtlasesMethod = T.GetMethod("PackAtlases", BindingFlags.NonPublic | BindingFlags.Static);
			}
			if (PackAtlasesMethod != null) {
				PackAtlasesMethod.Invoke(null, new object[] { new SpriteAtlas[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget });
			}*/
#endif
            if (GetPreviewTexturesMethod == null)
            {
                var T = Type.GetType("UnityEditor.U2D.SpriteAtlasExtensions,UnityEditor");
                GetPreviewTexturesMethod = T.GetMethod("GetPreviewTextures", BindingFlags.NonPublic | BindingFlags.Static);
            }
            if (GetPreviewTexturesMethod != null)
            {
                var retval = GetPreviewTexturesMethod.Invoke(null, new object[] { spriteAtlas });
                var textures = retval as Texture2D[];
                if (textures.Length > 0)
                    return textures[0];
            }
            return null;
        }
#endif
        public static Texture2D AccessPackedTexture(Sprite[] sprites)
        {
            return sprites[0].texture;
        }


        public static Sprite[] AccessPackedSprites(UnityEngine.U2D.SpriteAtlas spriteAtlas)
        {
            Sprite[] sprites = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {

                if (GetPackedSpritesMethod == null)
                {
                    var T = Type.GetType("UnityEditor.U2D.SpriteAtlasExtensions,UnityEditor");
                    GetPackedSpritesMethod = T.GetMethod("GetPackedSprites", BindingFlags.NonPublic | BindingFlags.Static);
                }
                if (GetPackedSpritesMethod != null)
                {
                    var retval = GetPackedSpritesMethod.Invoke(null, new object[] { spriteAtlas });
                    var spritesArray = retval as Sprite[];
                    if (spritesArray != null && spritesArray.Length > 0)
                    {
                        sprites = spritesArray;
                    }
                }
            }
#endif
            if (sprites == null)
            {
                sprites = new UnityEngine.Sprite[spriteAtlas.spriteCount];
                spriteAtlas.GetSprites(sprites);
                if (sprites.Length == 0)
                    return null;
            }
            return sprites;
        }
    }
}
