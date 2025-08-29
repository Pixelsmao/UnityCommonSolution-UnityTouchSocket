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
#define SPINE_OPTIONAL_RENDEROVERRIDE

using System.Collections.Generic;
using UnityEngine;

namespace Spine.Unity
{

#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonRenderSeparator")]
    public class SkeletonRenderSeparator : MonoBehaviour
    {
        public const int DefaultSortingOrderIncrement = 5;

        #region Inspector
        [SerializeField]
        protected SkeletonRenderer skeletonRenderer;
        public SkeletonRenderer SkeletonRenderer
        {
            get { return this.skeletonRenderer; }
            set
            {
#if SPINE_OPTIONAL_RENDEROVERRIDE
                if (this.skeletonRenderer != null)
                    this.skeletonRenderer.GenerateMeshOverride -= this.HandleRender;
#endif

                this.skeletonRenderer = value;
                if (value == null)
                    this.enabled = false;
            }
        }

        private MeshRenderer mainMeshRenderer;
        public bool copyPropertyBlock = true;
        [Tooltip("Copies MeshRenderer flags into each parts renderer")]
        public bool copyMeshRendererFlags = true;
        public List<Spine.Unity.SkeletonPartsRenderer> partsRenderers = new List<SkeletonPartsRenderer>();

#if UNITY_EDITOR
        private void Reset()
        {
            if (this.skeletonRenderer == null)
                this.skeletonRenderer = this.GetComponent<SkeletonRenderer>();
        }
#endif
        #endregion

        #region Callback Delegates
        /// <summary>OnMeshAndMaterialsUpdated is called at the end of LateUpdate after the Mesh and
        /// all materials have been updated.</summary>
        public event SkeletonRenderer.SkeletonRendererDelegate OnMeshAndMaterialsUpdated;
        #endregion

        #region Runtime Instantiation
        /// <summary>Adds a SkeletonRenderSeparator and child SkeletonPartsRenderer GameObjects to a given SkeletonRenderer.</summary>
        /// <returns>The to skeleton renderer.</returns>
        /// <param name="skeletonRenderer">The target SkeletonRenderer or SkeletonAnimation.</param>
        /// <param name="sortingLayerID">Sorting layer to be used for the parts renderers.</param>
        /// <param name="extraPartsRenderers">Number of additional SkeletonPartsRenderers on top of the ones determined by counting the number of separator slots.</param>
        /// <param name="sortingOrderIncrement">The integer to increment the sorting order per SkeletonPartsRenderer to separate them.</param>
        /// <param name="baseSortingOrder">The sorting order value of the first SkeletonPartsRenderer.</param>
        /// <param name="addMinimumPartsRenderers">If set to <c>true</c>, a minimum number of SkeletonPartsRenderer GameObjects (determined by separatorSlots.Count + 1) will be added.</param>
        public static SkeletonRenderSeparator AddToSkeletonRenderer(SkeletonRenderer skeletonRenderer, int sortingLayerID = 0, int extraPartsRenderers = 0, int sortingOrderIncrement = DefaultSortingOrderIncrement, int baseSortingOrder = 0, bool addMinimumPartsRenderers = true)
        {
            if (skeletonRenderer == null)
            {
                Debug.Log("Tried to add SkeletonRenderSeparator to a null SkeletonRenderer reference.");
                return null;
            }

            var srs = skeletonRenderer.gameObject.AddComponent<SkeletonRenderSeparator>();
            srs.skeletonRenderer = skeletonRenderer;

            skeletonRenderer.Initialize(false);
            var count = extraPartsRenderers;
            if (addMinimumPartsRenderers)
                count = extraPartsRenderers + skeletonRenderer.separatorSlots.Count + 1;

            var skeletonRendererTransform = skeletonRenderer.transform;
            var componentRenderers = srs.partsRenderers;

            for (var i = 0; i < count; i++)
            {
                var spr = SkeletonPartsRenderer.NewPartsRendererGameObject(skeletonRendererTransform, i.ToString());
                var mr = spr.MeshRenderer;
                mr.sortingLayerID = sortingLayerID;
                mr.sortingOrder = baseSortingOrder + (i * sortingOrderIncrement);
                componentRenderers.Add(spr);
            }

            srs.OnEnable();

#if UNITY_EDITOR
            // Make sure editor updates properly in edit mode.
            if (!Application.isPlaying)
            {
                skeletonRenderer.enabled = false;
                skeletonRenderer.enabled = true;
                skeletonRenderer.LateUpdate();
            }
#endif

            return srs;
        }

        /// <summary>Add a child SkeletonPartsRenderer GameObject to this SkeletonRenderSeparator.</summary>
        public SkeletonPartsRenderer AddPartsRenderer(int sortingOrderIncrement = DefaultSortingOrderIncrement, string name = null)
        {
            var sortingLayerID = 0;
            var sortingOrder = 0;
            if (this.partsRenderers.Count > 0)
            {
                var previous = this.partsRenderers[this.partsRenderers.Count - 1];
                var previousMeshRenderer = previous.MeshRenderer;
                sortingLayerID = previousMeshRenderer.sortingLayerID;
                sortingOrder = previousMeshRenderer.sortingOrder + sortingOrderIncrement;
            }

            if (string.IsNullOrEmpty(name))
                name = this.partsRenderers.Count.ToString();

            var spr = SkeletonPartsRenderer.NewPartsRendererGameObject(this.skeletonRenderer.transform, name);
            this.partsRenderers.Add(spr);

            var mr = spr.MeshRenderer;
            mr.sortingLayerID = sortingLayerID;
            mr.sortingOrder = sortingOrder;

            return spr;
        }
        #endregion

        public void OnEnable()
        {
            if (this.skeletonRenderer == null) return;
            if (this.copiedBlock == null) this.copiedBlock = new MaterialPropertyBlock();
            this.mainMeshRenderer = this.skeletonRenderer.GetComponent<MeshRenderer>();

#if SPINE_OPTIONAL_RENDEROVERRIDE
            this.skeletonRenderer.GenerateMeshOverride -= this.HandleRender;
            this.skeletonRenderer.GenerateMeshOverride += this.HandleRender;
#endif

            if (this.copyMeshRendererFlags)
            {
                var lightProbeUsage = this.mainMeshRenderer.lightProbeUsage;
                var receiveShadows = this.mainMeshRenderer.receiveShadows;
                var reflectionProbeUsage = this.mainMeshRenderer.reflectionProbeUsage;
                var shadowCastingMode = this.mainMeshRenderer.shadowCastingMode;
                var motionVectorGenerationMode = this.mainMeshRenderer.motionVectorGenerationMode;
                var probeAnchor = this.mainMeshRenderer.probeAnchor;

                for (var i = 0; i < this.partsRenderers.Count; i++)
                {
                    var currentRenderer = this.partsRenderers[i];
                    if (currentRenderer == null) continue; // skip null items.

                    var mr = currentRenderer.MeshRenderer;
                    mr.lightProbeUsage = lightProbeUsage;
                    mr.receiveShadows = receiveShadows;
                    mr.reflectionProbeUsage = reflectionProbeUsage;
                    mr.shadowCastingMode = shadowCastingMode;
                    mr.motionVectorGenerationMode = motionVectorGenerationMode;
                    mr.probeAnchor = probeAnchor;
                }
            }
        }

        public void OnDisable()
        {
            if (this.skeletonRenderer == null) return;
#if SPINE_OPTIONAL_RENDEROVERRIDE
            this.skeletonRenderer.GenerateMeshOverride -= this.HandleRender;
#endif

            this.skeletonRenderer.LateUpdate();

            foreach (var partsRenderer in this.partsRenderers)
            {
                if (partsRenderer != null)
                    partsRenderer.ClearMesh();
            }
        }

        private MaterialPropertyBlock copiedBlock;

        private void HandleRender(SkeletonRendererInstruction instruction)
        {
            var rendererCount = this.partsRenderers.Count;
            if (rendererCount <= 0) return;

            if (this.copyPropertyBlock)
                this.mainMeshRenderer.GetPropertyBlock(this.copiedBlock);

            var settings = new MeshGenerator.Settings
            {
                addNormals = this.skeletonRenderer.addNormals,
                calculateTangents = this.skeletonRenderer.calculateTangents,
                immutableTriangles = false, // parts cannot do immutable triangles.
                pmaVertexColors = this.skeletonRenderer.pmaVertexColors,
                tintBlack = this.skeletonRenderer.tintBlack,
                useClipping = true,
                zSpacing = this.skeletonRenderer.zSpacing
            };

            var submeshInstructions = instruction.submeshInstructions;
            var submeshInstructionsItems = submeshInstructions.Items;
            var lastSubmeshInstruction = submeshInstructions.Count - 1;

            var rendererIndex = 0;
            var currentRenderer = this.partsRenderers[rendererIndex];
            for (int si = 0, start = 0; si <= lastSubmeshInstruction; si++)
            {
                if (currentRenderer == null)
                    continue;
                if (submeshInstructionsItems[si].forceSeparate || si == lastSubmeshInstruction)
                {
                    // Apply properties
                    var meshGenerator = currentRenderer.MeshGenerator;
                    meshGenerator.settings = settings;

                    if (this.copyPropertyBlock)
                        currentRenderer.SetPropertyBlock(this.copiedBlock);

                    // Render
                    currentRenderer.RenderParts(instruction.submeshInstructions, start, si + 1);

                    start = si + 1;
                    rendererIndex++;
                    if (rendererIndex < rendererCount)
                    {
                        currentRenderer = this.partsRenderers[rendererIndex];
                    }
                    else
                    {
                        // Not enough renderers. Skip the rest of the instructions.
                        break;
                    }
                }
            }

            if (OnMeshAndMaterialsUpdated != null)
                OnMeshAndMaterialsUpdated(this.skeletonRenderer);

            // Clear extra renderers if they exist.
            for (; rendererIndex < rendererCount; rendererIndex++)
            {
                currentRenderer = this.partsRenderers[rendererIndex];
                if (currentRenderer != null)
                    this.partsRenderers[rendererIndex].ClearMesh();
            }

        }

    }
}
