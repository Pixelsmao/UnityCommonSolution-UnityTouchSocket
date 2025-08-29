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

    /// <summary>
    /// Stores a slot's current pose. Slots organize attachments for {@link Skeleton#drawOrder} purposes and provide a place to store
    /// state for an attachment.State cannot be stored in an attachment itself because attachments are stateless and may be shared
    /// across multiple skeletons.
    /// </summary>
    public class Slot
    {
        internal SlotData data;
        internal Bone bone;
        internal float r, g, b, a;
        internal float r2, g2, b2;
        internal bool hasSecondColor;
        internal Attachment attachment;
        internal float attachmentTime;
        internal ExposedList<float> deform = new ExposedList<float>();
        internal int attachmentState;

        public Slot(SlotData data, Bone bone)
        {
            if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
            if (bone == null) throw new ArgumentNullException("bone", "bone cannot be null.");
            this.data = data;
            this.bone = bone;

            // darkColor = data.darkColor == null ? null : new Color();
            if (data.hasSecondColor)
            {
                this.r2 = this.g2 = this.b2 = 0;
            }

            this.SetToSetupPose();
        }

        /// <summary>Copy constructor.</summary>
        public Slot(Slot slot, Bone bone)
        {
            if (slot == null) throw new ArgumentNullException("slot", "slot cannot be null.");
            if (bone == null) throw new ArgumentNullException("bone", "bone cannot be null.");
            this.data = slot.data;
            this.bone = bone;
            this.r = slot.r;
            this.g = slot.g;
            this.b = slot.b;
            this.a = slot.a;

            // darkColor = slot.darkColor == null ? null : new Color(slot.darkColor);
            if (slot.hasSecondColor)
            {
                this.r2 = slot.r2;
                this.g2 = slot.g2;
                this.b2 = slot.b2;
            }
            else
            {
                this.r2 = this.g2 = this.b2 = 0;
            }
            this.hasSecondColor = slot.hasSecondColor;

            this.attachment = slot.attachment;
            this.attachmentTime = slot.attachmentTime;
            this.deform.AddRange(slot.deform);
        }

        /// <summary>The slot's setup pose data.</summary>
        public SlotData Data { get { return this.data; } }
        /// <summary>The bone this slot belongs to.</summary>
        public Bone Bone { get { return this.bone; } }
        /// <summary>The skeleton this slot belongs to.</summary>
        public Skeleton Skeleton { get { return this.bone.skeleton; } }
        /// <summary>The color used to tint the slot's attachment. If <see cref="HasSecondColor"/> is set, this is used as the light color for two
        /// color tinting.</summary>
        public float R { get { return this.r; } set { this.r = value; } }
        /// <summary>The color used to tint the slot's attachment. If <see cref="HasSecondColor"/> is set, this is used as the light color for two
        /// color tinting.</summary>
        public float G { get { return this.g; } set { this.g = value; } }
        /// <summary>The color used to tint the slot's attachment. If <see cref="HasSecondColor"/> is set, this is used as the light color for two
        /// color tinting.</summary>
        public float B { get { return this.b; } set { this.b = value; } }
        /// <summary>The color used to tint the slot's attachment. If <see cref="HasSecondColor"/> is set, this is used as the light color for two
        /// color tinting.</summary>
        public float A { get { return this.a; } set { this.a = value; } }

        public void ClampColor()
        {
            this.r = MathUtils.Clamp(this.r, 0, 1);
            this.g = MathUtils.Clamp(this.g, 0, 1);
            this.b = MathUtils.Clamp(this.b, 0, 1);
            this.a = MathUtils.Clamp(this.a, 0, 1);
        }

        /// <summary>The dark color used to tint the slot's attachment for two color tinting, ignored if two color tinting is not used.</summary>
        /// <seealso cref="HasSecondColor"/>
        public float R2 { get { return this.r2; } set { this.r2 = value; } }
        /// <summary>The dark color used to tint the slot's attachment for two color tinting, ignored if two color tinting is not used.</summary>
        /// <seealso cref="HasSecondColor"/>
        public float G2 { get { return this.g2; } set { this.g2 = value; } }
        /// <summary>The dark color used to tint the slot's attachment for two color tinting, ignored if two color tinting is not used.</summary>
        /// <seealso cref="HasSecondColor"/>
        public float B2 { get { return this.b2; } set { this.b2 = value; } }
        /// <summary>Whether R2 G2 B2 are used to tint the slot's attachment for two color tinting. False if two color tinting is not used.</summary>
        public bool HasSecondColor { get { return this.data.hasSecondColor; } set { this.data.hasSecondColor = value; } }

        public void ClampSecondColor()
        {
            this.r2 = MathUtils.Clamp(this.r2, 0, 1);
            this.g2 = MathUtils.Clamp(this.g2, 0, 1);
            this.b2 = MathUtils.Clamp(this.b2, 0, 1);
        }

        public Attachment Attachment
        {
            /// <summary>The current attachment for the slot, or null if the slot has no attachment.</summary>
            get { return this.attachment; }
            /// <summary>
            /// Sets the slot's attachment and, if the attachment changed, resets <see cref="AttachmentTime"/> and clears
            /// <see cref="Deform">.</summary>
            /// <param name="value">May be null.</param>
            set
            {
                if (this.attachment == value) return;
                this.attachment = value;
                this.attachmentTime = this.bone.skeleton.time;
                this.deform.Clear(false);
            }
        }

        /// <summary> The time that has elapsed since the last time the attachment was set or cleared. Relies on Skeleton
        /// <see cref="Skeleton.Time"/></summary>
        public float AttachmentTime
        {
            get { return this.bone.skeleton.time - this.attachmentTime; }
            set { this.attachmentTime = this.bone.skeleton.time - value; }
        }

        /// <summary> Vertices to deform the slot's attachment. For an unweighted mesh, the entries are local positions for each vertex. For a
        /// weighted mesh, the entries are an offset for each vertex which will be added to the mesh's local vertex positions.
        /// <para />
        /// See <see cref="VertexAttachment.ComputeWorldVertices(Slot, int, int, float[], int, int)"/> and <see cref="DeformTimeline"/>.</summary>
        public ExposedList<float> Deform
        {
            get
            {
                return this.deform;
            }
            set
            {
                if (this.deform == null) throw new ArgumentNullException("deform", "deform cannot be null.");
                this.deform = value;
            }
        }

        /// <summary>Sets this slot to the setup pose.</summary>
        public void SetToSetupPose()
        {
            this.r = this.data.r;
            this.g = this.data.g;
            this.b = this.data.b;
            this.a = this.data.a;

            // if (darkColor != null) darkColor.set(data.darkColor);
            if (this.HasSecondColor)
            {
                this.r2 = this.data.r2;
                this.g2 = this.data.g2;
                this.b2 = this.data.b2;
            }

            if (this.data.attachmentName == null)
                this.Attachment = null;
            else
            {
                this.attachment = null;
                this.Attachment = this.bone.skeleton.GetAttachment(this.data.index, this.data.attachmentName);
            }
        }

        public override string ToString()
        {
            return this.data.name;
        }
    }
}
