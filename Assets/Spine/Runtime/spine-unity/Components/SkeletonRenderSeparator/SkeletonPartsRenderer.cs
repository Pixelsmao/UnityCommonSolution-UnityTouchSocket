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

using UnityEngine;

namespace Spine.Unity
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonRenderSeparator")]
    public class SkeletonPartsRenderer : MonoBehaviour
    {

        #region Properties
        private MeshGenerator meshGenerator;
        public MeshGenerator MeshGenerator
        {
            get
            {
                this.LazyIntialize();
                return this.meshGenerator;
            }
        }

        private MeshRenderer meshRenderer;
        public MeshRenderer MeshRenderer
        {
            get
            {
                this.LazyIntialize();
                return this.meshRenderer;
            }
        }

        private MeshFilter meshFilter;
        public MeshFilter MeshFilter
        {
            get
            {
                this.LazyIntialize();
                return this.meshFilter;
            }
        }
        #endregion

        #region Callback Delegates
        public delegate void SkeletonPartsRendererDelegate(SkeletonPartsRenderer skeletonPartsRenderer);

        /// <summary>OnMeshAndMaterialsUpdated is called at the end of LateUpdate after the Mesh and
        /// all materials have been updated.</summary>
        public event SkeletonPartsRendererDelegate OnMeshAndMaterialsUpdated;
        #endregion

        private MeshRendererBuffers buffers;
        private readonly SkeletonRendererInstruction currentInstructions = new SkeletonRendererInstruction();


        private void LazyIntialize()
        {
            if (this.buffers == null)
            {
                this.buffers = new MeshRendererBuffers();
                this.buffers.Initialize();

                if (this.meshGenerator != null) return;
                this.meshGenerator = new MeshGenerator();
                this.meshFilter = this.GetComponent<MeshFilter>();
                this.meshRenderer = this.GetComponent<MeshRenderer>();
                this.currentInstructions.Clear();
            }
        }

        public void ClearMesh()
        {
            this.LazyIntialize();
            this.meshFilter.sharedMesh = null;
        }

        public void RenderParts(ExposedList<SubmeshInstruction> instructions, int startSubmesh, int endSubmesh)
        {
            this.LazyIntialize();

            // STEP 1: Create instruction
            var smartMesh = this.buffers.GetNextMesh();
            this.currentInstructions.SetWithSubset(instructions, startSubmesh, endSubmesh);
            var updateTriangles = SkeletonRendererInstruction.GeometryNotEqual(this.currentInstructions, smartMesh.instructionUsed);

            // STEP 2: Generate mesh buffers.
            var currentInstructionsSubmeshesItems = this.currentInstructions.submeshInstructions.Items;
            this.meshGenerator.Begin();
            if (this.currentInstructions.hasActiveClipping)
            {
                for (var i = 0; i < this.currentInstructions.submeshInstructions.Count; i++)
                    this.meshGenerator.AddSubmesh(currentInstructionsSubmeshesItems[i], updateTriangles);
            }
            else
            {
                this.meshGenerator.BuildMeshWithArrays(this.currentInstructions, updateTriangles);
            }

            this.buffers.UpdateSharedMaterials(this.currentInstructions.submeshInstructions);

            // STEP 3: modify mesh.
            var mesh = smartMesh.mesh;

            if (this.meshGenerator.VertexCount <= 0)
            { // Clear an empty mesh
                updateTriangles = false;
                mesh.Clear();
            }
            else
            {
                this.meshGenerator.FillVertexData(mesh);
                if (updateTriangles)
                {
                    this.meshGenerator.FillTriangles(mesh);
                    this.meshRenderer.sharedMaterials = this.buffers.GetUpdatedSharedMaterialsArray();
                }
                else if (this.buffers.MaterialsChangedInLastUpdate())
                {
                    this.meshRenderer.sharedMaterials = this.buffers.GetUpdatedSharedMaterialsArray();
                }
                this.meshGenerator.FillLateVertexData(mesh);
            }

            this.meshFilter.sharedMesh = mesh;
            smartMesh.instructionUsed.Set(this.currentInstructions);

            if (OnMeshAndMaterialsUpdated != null)
                OnMeshAndMaterialsUpdated(this);
        }

        public void SetPropertyBlock(MaterialPropertyBlock block)
        {
            this.LazyIntialize();
            this.meshRenderer.SetPropertyBlock(block);
        }

        public static SkeletonPartsRenderer NewPartsRendererGameObject(Transform parent, string name, int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var returnComponent = go.AddComponent<SkeletonPartsRenderer>();
            returnComponent.MeshRenderer.sortingOrder = sortingOrder;

            return returnComponent;
        }
    }
}
