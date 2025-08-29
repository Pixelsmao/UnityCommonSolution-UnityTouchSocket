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
    /// <summary>Attachment that displays a texture region.</summary>
    public class RegionAttachment : Attachment, IHasRendererObject
    {
        public const int BLX = 0;
        public const int BLY = 1;
        public const int ULX = 2;
        public const int ULY = 3;
        public const int URX = 4;
        public const int URY = 5;
        public const int BRX = 6;
        public const int BRY = 7;

        internal float x, y, rotation, scaleX = 1, scaleY = 1, width, height;
        internal float regionOffsetX, regionOffsetY, regionWidth, regionHeight, regionOriginalWidth, regionOriginalHeight;
        internal float[] offset = new float[8], uvs = new float[8];
        internal float r = 1, g = 1, b = 1, a = 1;

        public float X { get { return this.x; } set { this.x = value; } }
        public float Y { get { return this.y; } set { this.y = value; } }
        public float Rotation { get { return this.rotation; } set { this.rotation = value; } }
        public float ScaleX { get { return this.scaleX; } set { this.scaleX = value; } }
        public float ScaleY { get { return this.scaleY; } set { this.scaleY = value; } }
        public float Width { get { return this.width; } set { this.width = value; } }
        public float Height { get { return this.height; } set { this.height = value; } }

        public float R { get { return this.r; } set { this.r = value; } }
        public float G { get { return this.g; } set { this.g = value; } }
        public float B { get { return this.b; } set { this.b = value; } }
        public float A { get { return this.a; } set { this.a = value; } }

        public string Path { get; set; }
        public object RendererObject { get; set; }
        public float RegionOffsetX { get { return this.regionOffsetX; } set { this.regionOffsetX = value; } }
        public float RegionOffsetY { get { return this.regionOffsetY; } set { this.regionOffsetY = value; } } // Pixels stripped from the bottom left, unrotated.
        public float RegionWidth { get { return this.regionWidth; } set { this.regionWidth = value; } }
        public float RegionHeight { get { return this.regionHeight; } set { this.regionHeight = value; } } // Unrotated, stripped size.
        public float RegionOriginalWidth { get { return this.regionOriginalWidth; } set { this.regionOriginalWidth = value; } }
        public float RegionOriginalHeight { get { return this.regionOriginalHeight; } set { this.regionOriginalHeight = value; } } // Unrotated, unstripped size.

        public float[] Offset { get { return this.offset; } }
        public float[] UVs { get { return this.uvs; } }

        public RegionAttachment(string name)
            : base(name)
        {
        }

        public void UpdateOffset()
        {
            var width = this.width;
            var height = this.height;
            var localX2 = width * 0.5f;
            var localY2 = height * 0.5f;
            var localX = -localX2;
            var localY = -localY2;
            if (this.regionOriginalWidth != 0)
            { // if (region != null)
                localX += this.regionOffsetX / this.regionOriginalWidth * width;
                localY += this.regionOffsetY / this.regionOriginalHeight * height;
                localX2 -= (this.regionOriginalWidth - this.regionOffsetX - this.regionWidth) / this.regionOriginalWidth * width;
                localY2 -= (this.regionOriginalHeight - this.regionOffsetY - this.regionHeight) / this.regionOriginalHeight * height;
            }
            var scaleX = this.scaleX;
            var scaleY = this.scaleY;
            localX *= scaleX;
            localY *= scaleY;
            localX2 *= scaleX;
            localY2 *= scaleY;
            var rotation = this.rotation;
            var cos = MathUtils.CosDeg(rotation);
            var sin = MathUtils.SinDeg(rotation);
            var x = this.x;
            var y = this.y;
            var localXCos = localX * cos + x;
            var localXSin = localX * sin;
            var localYCos = localY * cos + y;
            var localYSin = localY * sin;
            var localX2Cos = localX2 * cos + x;
            var localX2Sin = localX2 * sin;
            var localY2Cos = localY2 * cos + y;
            var localY2Sin = localY2 * sin;
            var offset = this.offset;
            offset[BLX] = localXCos - localYSin;
            offset[BLY] = localYCos + localXSin;
            offset[ULX] = localXCos - localY2Sin;
            offset[ULY] = localY2Cos + localXSin;
            offset[URX] = localX2Cos - localY2Sin;
            offset[URY] = localY2Cos + localX2Sin;
            offset[BRX] = localX2Cos - localYSin;
            offset[BRY] = localYCos + localX2Sin;
        }

        public void SetUVs(float u, float v, float u2, float v2, bool rotate)
        {
            var uvs = this.uvs;
            // UV values differ from RegionAttachment.java
            if (rotate)
            {
                uvs[URX] = u;
                uvs[URY] = v2;
                uvs[BRX] = u;
                uvs[BRY] = v;
                uvs[BLX] = u2;
                uvs[BLY] = v;
                uvs[ULX] = u2;
                uvs[ULY] = v2;
            }
            else
            {
                uvs[ULX] = u;
                uvs[ULY] = v2;
                uvs[URX] = u;
                uvs[URY] = v;
                uvs[BRX] = u2;
                uvs[BRY] = v;
                uvs[BLX] = u2;
                uvs[BLY] = v2;
            }
        }

        /// <summary>Transforms the attachment's four vertices to world coordinates.</summary>
        /// <param name="bone">The parent bone.</param>
        /// <param name="worldVertices">The output world vertices. Must have a length greater than or equal to offset + 8.</param>
        /// <param name="offset">The worldVertices index to begin writing values.</param>
        /// <param name="stride">The number of worldVertices entries between the value pairs written.</param>
        public void ComputeWorldVertices(Bone bone, float[] worldVertices, int offset, int stride = 2)
        {
            var vertexOffset = this.offset;
            float bwx = bone.worldX, bwy = bone.worldY;
            float a = bone.a, b = bone.b, c = bone.c, d = bone.d;
            float offsetX, offsetY;

            // Vertex order is different from RegionAttachment.java
            offsetX = vertexOffset[BRX]; // 0
            offsetY = vertexOffset[BRY]; // 1
            worldVertices[offset] = offsetX * a + offsetY * b + bwx; // bl
            worldVertices[offset + 1] = offsetX * c + offsetY * d + bwy;
            offset += stride;

            offsetX = vertexOffset[BLX]; // 2
            offsetY = vertexOffset[BLY]; // 3
            worldVertices[offset] = offsetX * a + offsetY * b + bwx; // ul
            worldVertices[offset + 1] = offsetX * c + offsetY * d + bwy;
            offset += stride;

            offsetX = vertexOffset[ULX]; // 4
            offsetY = vertexOffset[ULY]; // 5
            worldVertices[offset] = offsetX * a + offsetY * b + bwx; // ur
            worldVertices[offset + 1] = offsetX * c + offsetY * d + bwy;
            offset += stride;

            offsetX = vertexOffset[URX]; // 6
            offsetY = vertexOffset[URY]; // 7
            worldVertices[offset] = offsetX * a + offsetY * b + bwx; // br
            worldVertices[offset + 1] = offsetX * c + offsetY * d + bwy;
            //offset += stride;
        }

        public override Attachment Copy()
        {
            var copy = new RegionAttachment(this.Name);
            copy.RendererObject = this.RendererObject;
            copy.regionOffsetX = this.regionOffsetX;
            copy.regionOffsetY = this.regionOffsetY;
            copy.regionWidth = this.regionWidth;
            copy.regionHeight = this.regionHeight;
            copy.regionOriginalWidth = this.regionOriginalWidth;
            copy.regionOriginalHeight = this.regionOriginalHeight;
            copy.Path = this.Path;
            copy.x = this.x;
            copy.y = this.y;
            copy.scaleX = this.scaleX;
            copy.scaleY = this.scaleY;
            copy.rotation = this.rotation;
            copy.width = this.width;
            copy.height = this.height;
            Array.Copy(this.uvs, 0, copy.uvs, 0, 8);
            Array.Copy(this.offset, 0, copy.offset, 0, 8);
            copy.r = this.r;
            copy.g = this.g;
            copy.b = this.b;
            copy.a = this.a;
            return copy;
        }
    }
}
