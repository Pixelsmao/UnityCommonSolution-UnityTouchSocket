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

namespace Spine
{
    /// <summary>Attachment that displays a texture region using a mesh.</summary>
    public class MeshAttachment : VertexAttachment, IHasRendererObject
    {
        internal float regionOffsetX, regionOffsetY, regionWidth, regionHeight, regionOriginalWidth, regionOriginalHeight;
        private MeshAttachment parentMesh;
        internal float[] uvs, regionUVs;
        internal int[] triangles;
        internal float r = 1, g = 1, b = 1, a = 1;
        internal int hulllength;

        public int HullLength { get { return this.hulllength; } set { this.hulllength = value; } }
        public float[] RegionUVs { get { return this.regionUVs; } set { this.regionUVs = value; } }
        /// <summary>The UV pair for each vertex, normalized within the entire texture. <seealso cref="MeshAttachment.UpdateUVs"/></summary>
        public float[] UVs { get { return this.uvs; } set { this.uvs = value; } }
        public int[] Triangles { get { return this.triangles; } set { this.triangles = value; } }

        public float R { get { return this.r; } set { this.r = value; } }
        public float G { get { return this.g; } set { this.g = value; } }
        public float B { get { return this.b; } set { this.b = value; } }
        public float A { get { return this.a; } set { this.a = value; } }

        public string Path { get; set; }
        public object RendererObject { get; set; }
        public float RegionU { get; set; }
        public float RegionV { get; set; }
        public float RegionU2 { get; set; }
        public float RegionV2 { get; set; }
        public bool RegionRotate { get; set; }
        public int RegionDegrees { get; set; }
        public float RegionOffsetX { get { return this.regionOffsetX; } set { this.regionOffsetX = value; } }
        public float RegionOffsetY { get { return this.regionOffsetY; } set { this.regionOffsetY = value; } } // Pixels stripped from the bottom left, unrotated.
        public float RegionWidth { get { return this.regionWidth; } set { this.regionWidth = value; } }
        public float RegionHeight { get { return this.regionHeight; } set { this.regionHeight = value; } } // Unrotated, stripped size.
        public float RegionOriginalWidth { get { return this.regionOriginalWidth; } set { this.regionOriginalWidth = value; } }
        public float RegionOriginalHeight { get { return this.regionOriginalHeight; } set { this.regionOriginalHeight = value; } } // Unrotated, unstripped size.

        public MeshAttachment ParentMesh
        {
            get { return this.parentMesh; }
            set
            {
                this.parentMesh = value;
                if (value != null)
                {
                    this.bones = value.bones;
                    this.vertices = value.vertices;
                    this.worldVerticesLength = value.worldVerticesLength;
                    this.regionUVs = value.regionUVs;
                    this.triangles = value.triangles;
                    this.HullLength = value.HullLength;
                    this.Edges = value.Edges;
                    this.Width = value.Width;
                    this.Height = value.Height;
                }
            }
        }

        // Nonessential.
        public int[] Edges { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public MeshAttachment(string name)
            : base(name)
        {
        }

        public void UpdateUVs()
        {
            var regionUVs = this.regionUVs;
            if (this.uvs == null || this.uvs.Length != regionUVs.Length) this.uvs = new float[regionUVs.Length];
            var uvs = this.uvs;
            float u = this.RegionU, v = this.RegionV, width = 0, height = 0;

            if (this.RegionDegrees == 90)
            {
                var textureHeight = this.regionWidth / (this.RegionV2 - this.RegionV);
                var textureWidth = this.regionHeight / (this.RegionU2 - this.RegionU);
                u -= (this.RegionOriginalHeight - this.RegionOffsetY - this.RegionHeight) / textureWidth;
                v -= (this.RegionOriginalWidth - this.RegionOffsetX - this.RegionWidth) / textureHeight;
                width = this.RegionOriginalHeight / textureWidth;
                height = this.RegionOriginalWidth / textureHeight;

                for (int i = 0, n = uvs.Length; i < n; i += 2)
                {
                    uvs[i] = u + regionUVs[i + 1] * width;
                    uvs[i + 1] = v + (1 - regionUVs[i]) * height;
                }
            }
            else if (this.RegionDegrees == 180)
            {
                var textureWidth = this.regionWidth / (this.RegionU2 - this.RegionU);
                var textureHeight = this.regionHeight / (this.RegionV2 - this.RegionV);
                u -= (this.RegionOriginalWidth - this.RegionOffsetX - this.RegionWidth) / textureWidth;
                v -= this.RegionOffsetY / textureHeight;
                width = this.RegionOriginalWidth / textureWidth;
                height = this.RegionOriginalHeight / textureHeight;

                for (int i = 0, n = uvs.Length; i < n; i += 2)
                {
                    uvs[i] = u + (1 - regionUVs[i]) * width;
                    uvs[i + 1] = v + (1 - regionUVs[i + 1]) * height;
                }
            }
            else if (this.RegionDegrees == 270)
            {
                var textureWidth = this.regionWidth / (this.RegionU2 - this.RegionU);
                var textureHeight = this.regionHeight / (this.RegionV2 - this.RegionV);
                u -= this.RegionOffsetY / textureWidth;
                v -= this.RegionOffsetX / textureHeight;
                width = this.RegionOriginalHeight / textureWidth;
                height = this.RegionOriginalWidth / textureHeight;

                for (int i = 0, n = uvs.Length; i < n; i += 2)
                {
                    uvs[i] = u + (1 - regionUVs[i + 1]) * width;
                    uvs[i + 1] = v + regionUVs[i] * height;
                }
            }
            else
            {
                var textureWidth = this.regionWidth / (this.RegionU2 - this.RegionU);
                var textureHeight = this.regionHeight / (this.RegionV2 - this.RegionV);
                u -= this.RegionOffsetX / textureWidth;
                v -= (this.RegionOriginalHeight - this.RegionOffsetY - this.RegionHeight) / textureHeight;
                width = this.RegionOriginalWidth / textureWidth;
                height = this.RegionOriginalHeight / textureHeight;

                for (int i = 0, n = uvs.Length; i < n; i += 2)
                {
                    uvs[i] = u + regionUVs[i] * width;
                    uvs[i + 1] = v + regionUVs[i + 1] * height;
                }
            }
        }

        public override Attachment Copy()
        {
            if (this.parentMesh != null) return this.NewLinkedMesh();

            var copy = new MeshAttachment(this.Name);
            copy.RendererObject = this.RendererObject;
            copy.regionOffsetX = this.regionOffsetX;
            copy.regionOffsetY = this.regionOffsetY;
            copy.regionWidth = this.regionWidth;
            copy.regionHeight = this.regionHeight;
            copy.regionOriginalWidth = this.regionOriginalWidth;
            copy.regionOriginalHeight = this.regionOriginalHeight;
            copy.RegionRotate = this.RegionRotate;
            copy.RegionDegrees = this.RegionDegrees;
            copy.RegionU = this.RegionU;
            copy.RegionV = this.RegionV;
            copy.RegionU2 = this.RegionU2;
            copy.RegionV2 = this.RegionV2;

            copy.Path = this.Path;
            copy.r = this.r;
            copy.g = this.g;
            copy.b = this.b;
            copy.a = this.a;

            this.CopyTo(copy);
            copy.regionUVs = new float[this.regionUVs.Length];
            Array.Copy(this.regionUVs, 0, copy.regionUVs, 0, this.regionUVs.Length);
            copy.uvs = new float[this.uvs.Length];
            Array.Copy(this.uvs, 0, copy.uvs, 0, this.uvs.Length);
            copy.triangles = new int[this.triangles.Length];
            Array.Copy(this.triangles, 0, copy.triangles, 0, this.triangles.Length);
            copy.HullLength = this.HullLength;

            // Nonessential.
            if (this.Edges != null)
            {
                copy.Edges = new int[this.Edges.Length];
                Array.Copy(this.Edges, 0, copy.Edges, 0, this.Edges.Length);
            }
            copy.Width = this.Width;
            copy.Height = this.Height;
            return copy;
        }

        ///<summary>Returns a new mesh with this mesh set as the <see cref="ParentMesh"/>.
        public MeshAttachment NewLinkedMesh()
        {
            var mesh = new MeshAttachment(this.Name);
            mesh.RendererObject = this.RendererObject;
            mesh.regionOffsetX = this.regionOffsetX;
            mesh.regionOffsetY = this.regionOffsetY;
            mesh.regionWidth = this.regionWidth;
            mesh.regionHeight = this.regionHeight;
            mesh.regionOriginalWidth = this.regionOriginalWidth;
            mesh.regionOriginalHeight = this.regionOriginalHeight;
            mesh.RegionDegrees = this.RegionDegrees;
            mesh.RegionRotate = this.RegionRotate;
            mesh.RegionU = this.RegionU;
            mesh.RegionV = this.RegionV;
            mesh.RegionU2 = this.RegionU2;
            mesh.RegionV2 = this.RegionV2;

            mesh.Path = this.Path;
            mesh.r = this.r;
            mesh.g = this.g;
            mesh.b = this.b;
            mesh.a = this.a;

            mesh.deformAttachment = this.deformAttachment;
            mesh.ParentMesh = this.parentMesh != null ? this.parentMesh : this;
            mesh.UpdateUVs();
            return mesh;
        }
    }
}
