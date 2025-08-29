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
    public class SlotData
    {
        internal int index;
        internal string name;
        internal BoneData boneData;
        internal float r = 1, g = 1, b = 1, a = 1;
        internal float r2 = 0, g2 = 0, b2 = 0;
        internal bool hasSecondColor = false;
        internal string attachmentName;
        internal BlendMode blendMode;

        /// <summary>The index of the slot in <see cref="Skeleton.Slots"/>.</summary>
        public int Index { get { return this.index; } }
        /// <summary>The name of the slot, which is unique across all slots in the skeleton.</summary>
        public string Name { get { return this.name; } }
        /// <summary>The bone this slot belongs to.</summary>
        public BoneData BoneData { get { return this.boneData; } }
        public float R { get { return this.r; } set { this.r = value; } }
        public float G { get { return this.g; } set { this.g = value; } }
        public float B { get { return this.b; } set { this.b = value; } }
        public float A { get { return this.a; } set { this.a = value; } }

        public float R2 { get { return this.r2; } set { this.r2 = value; } }
        public float G2 { get { return this.g2; } set { this.g2 = value; } }
        public float B2 { get { return this.b2; } set { this.b2 = value; } }
        public bool HasSecondColor { get { return this.hasSecondColor; } set { this.hasSecondColor = value; } }

        /// <summary>The name of the attachment that is visible for this slot in the setup pose, or null if no attachment is visible.</summary>
        public String AttachmentName { get { return this.attachmentName; } set { this.attachmentName = value; } }
        /// <summary>The blend mode for drawing the slot's attachment.</summary>
        public BlendMode BlendMode { get { return this.blendMode; } set { this.blendMode = value; } }

        public SlotData(int index, String name, BoneData boneData)
        {
            if (index < 0) throw new ArgumentException("index must be >= 0.", "index");
            if (name == null) throw new ArgumentNullException("name", "name cannot be null.");
            if (boneData == null) throw new ArgumentNullException("boneData", "boneData cannot be null.");
            this.index = index;
            this.name = name;
            this.boneData = boneData;
        }

        public override string ToString()
        {
            return this.name;
        }
    }
}
