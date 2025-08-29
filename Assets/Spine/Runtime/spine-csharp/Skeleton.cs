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
    public class Skeleton
    {
        internal SkeletonData data;
        internal ExposedList<Bone> bones;
        internal ExposedList<Slot> slots;
        internal ExposedList<Slot> drawOrder;
        internal ExposedList<IkConstraint> ikConstraints;
        internal ExposedList<TransformConstraint> transformConstraints;
        internal ExposedList<PathConstraint> pathConstraints;
        internal ExposedList<IUpdatable> updateCache = new ExposedList<IUpdatable>();
        internal ExposedList<Bone> updateCacheReset = new ExposedList<Bone>();
        internal Skin skin;
        internal float r = 1, g = 1, b = 1, a = 1;
        internal float time;
        private float scaleX = 1, scaleY = 1;
        internal float x, y;

        public SkeletonData Data { get { return this.data; } }
        public ExposedList<Bone> Bones { get { return this.bones; } }
        public ExposedList<IUpdatable> UpdateCacheList { get { return this.updateCache; } }
        public ExposedList<Slot> Slots { get { return this.slots; } }
        public ExposedList<Slot> DrawOrder { get { return this.drawOrder; } }
        public ExposedList<IkConstraint> IkConstraints { get { return this.ikConstraints; } }
        public ExposedList<PathConstraint> PathConstraints { get { return this.pathConstraints; } }
        public ExposedList<TransformConstraint> TransformConstraints { get { return this.transformConstraints; } }
        public Skin Skin { get { return this.skin; } set { this.SetSkin(value); } }
        public float R { get { return this.r; } set { this.r = value; } }
        public float G { get { return this.g; } set { this.g = value; } }
        public float B { get { return this.b; } set { this.b = value; } }
        public float A { get { return this.a; } set { this.a = value; } }
        public float Time { get { return this.time; } set { this.time = value; } }
        public float X { get { return this.x; } set { this.x = value; } }
        public float Y { get { return this.y; } set { this.y = value; } }
        public float ScaleX { get { return this.scaleX; } set { this.scaleX = value; } }
        public float ScaleY { get { return this.scaleY * (Bone.yDown ? -1 : 1); } set { this.scaleY = value; } }

        [Obsolete("Use ScaleX instead. FlipX is when ScaleX is negative.")]
        public bool FlipX { get { return this.scaleX < 0; } set { this.scaleX = value ? -1f : 1f; } }

        [Obsolete("Use ScaleY instead. FlipY is when ScaleY is negative.")]
        public bool FlipY { get { return this.scaleY < 0; } set { this.scaleY = value ? -1f : 1f; } }

        public Bone RootBone
        {
            get { return this.bones.Count == 0 ? null : this.bones.Items[0]; }
        }

        public Skeleton(SkeletonData data)
        {
            if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
            this.data = data;

            this.bones = new ExposedList<Bone>(data.bones.Count);
            foreach (var boneData in data.bones)
            {
                Bone bone;
                if (boneData.parent == null)
                {
                    bone = new Bone(boneData, this, null);
                }
                else
                {
                    var parent = this.bones.Items[boneData.parent.index];
                    bone = new Bone(boneData, this, parent);
                    parent.children.Add(bone);
                }
                this.bones.Add(bone);
            }

            this.slots = new ExposedList<Slot>(data.slots.Count);
            this.drawOrder = new ExposedList<Slot>(data.slots.Count);
            foreach (var slotData in data.slots)
            {
                var bone = this.bones.Items[slotData.boneData.index];
                var slot = new Slot(slotData, bone);
                this.slots.Add(slot);
                this.drawOrder.Add(slot);
            }

            this.ikConstraints = new ExposedList<IkConstraint>(data.ikConstraints.Count);
            foreach (var ikConstraintData in data.ikConstraints)
                this.ikConstraints.Add(new IkConstraint(ikConstraintData, this));

            this.transformConstraints = new ExposedList<TransformConstraint>(data.transformConstraints.Count);
            foreach (var transformConstraintData in data.transformConstraints)
                this.transformConstraints.Add(new TransformConstraint(transformConstraintData, this));

            this.pathConstraints = new ExposedList<PathConstraint>(data.pathConstraints.Count);
            foreach (var pathConstraintData in data.pathConstraints)
                this.pathConstraints.Add(new PathConstraint(pathConstraintData, this));

            this.UpdateCache();
            this.UpdateWorldTransform();
        }

        /// <summary>Caches information about bones and constraints. Must be called if the <see cref="Skin"/> is modified or if bones, constraints, or
        /// constraints, or weighted path attachments are added or removed.</summary>
        public void UpdateCache()
        {
            var updateCache = this.updateCache;
            updateCache.Clear();
            this.updateCacheReset.Clear();

            var boneCount = this.bones.Items.Length;
            var bones = this.bones;
            for (var i = 0; i < boneCount; i++)
            {
                var bone = bones.Items[i];
                bone.sorted = bone.data.skinRequired;
                bone.active = !bone.sorted;
            }
            if (this.skin != null)
            {
                Object[] skinBones = this.skin.bones.Items;
                for (int i = 0, n = this.skin.bones.Count; i < n; i++)
                {
                    var bone = bones.Items[((BoneData)skinBones[i]).index];
                    do
                    {
                        bone.sorted = false;
                        bone.active = true;
                        bone = bone.parent;
                    } while (bone != null);
                }
            }

            int ikCount = this.ikConstraints.Count, transformCount = this.transformConstraints.Count, pathCount = this.pathConstraints.Count;
            var ikConstraints = this.ikConstraints;
            var transformConstraints = this.transformConstraints;
            var pathConstraints = this.pathConstraints;
            var constraintCount = ikCount + transformCount + pathCount;
            //outer:
            for (var i = 0; i < constraintCount; i++)
            {
                for (var ii = 0; ii < ikCount; ii++)
                {
                    var constraint = ikConstraints.Items[ii];
                    if (constraint.data.order == i)
                    {
                        this.SortIkConstraint(constraint);
                        goto continue_outer; //continue outer;
                    }
                }
                for (var ii = 0; ii < transformCount; ii++)
                {
                    var constraint = transformConstraints.Items[ii];
                    if (constraint.data.order == i)
                    {
                        this.SortTransformConstraint(constraint);
                        goto continue_outer; //continue outer;
                    }
                }
                for (var ii = 0; ii < pathCount; ii++)
                {
                    var constraint = pathConstraints.Items[ii];
                    if (constraint.data.order == i)
                    {
                        this.SortPathConstraint(constraint);
                        goto continue_outer; //continue outer;
                    }
                }
                continue_outer: { }
            }

            for (var i = 0; i < boneCount; i++)
                this.SortBone(bones.Items[i]);
        }

        private void SortIkConstraint(IkConstraint constraint)
        {
            constraint.active = constraint.target.active
                && (!constraint.data.skinRequired || (this.skin != null && this.skin.constraints.Contains(constraint.data)));
            if (!constraint.active) return;

            var target = constraint.target;
            this.SortBone(target);

            var constrained = constraint.bones;
            var parent = constrained.Items[0];
            this.SortBone(parent);

            if (constrained.Count > 1)
            {
                var child = constrained.Items[constrained.Count - 1];
                if (!this.updateCache.Contains(child))
                    this.updateCacheReset.Add(child);
            }

            this.updateCache.Add(constraint);

            SortReset(parent.children);
            constrained.Items[constrained.Count - 1].sorted = true;
        }

        private void SortPathConstraint(PathConstraint constraint)
        {
            constraint.active = constraint.target.bone.active
                && (!constraint.data.skinRequired || (this.skin != null && this.skin.constraints.Contains(constraint.data)));
            if (!constraint.active) return;

            var slot = constraint.target;
            var slotIndex = slot.data.index;
            var slotBone = slot.bone;
            if (this.skin != null) this.SortPathConstraintAttachment(this.skin, slotIndex, slotBone);
            if (this.data.defaultSkin != null && this.data.defaultSkin != this.skin)
                this.SortPathConstraintAttachment(this.data.defaultSkin, slotIndex, slotBone);

            var attachment = slot.attachment;
            if (attachment is PathAttachment) this.SortPathConstraintAttachment(attachment, slotBone);

            var constrained = constraint.bones;
            var boneCount = constrained.Count;
            for (var i = 0; i < boneCount; i++)
                this.SortBone(constrained.Items[i]);

            this.updateCache.Add(constraint);

            for (var i = 0; i < boneCount; i++)
                SortReset(constrained.Items[i].children);
            for (var i = 0; i < boneCount; i++)
                constrained.Items[i].sorted = true;
        }

        private void SortTransformConstraint(TransformConstraint constraint)
        {
            constraint.active = constraint.target.active
                && (!constraint.data.skinRequired || (this.skin != null && this.skin.constraints.Contains(constraint.data)));
            if (!constraint.active) return;

            this.SortBone(constraint.target);

            var constrained = constraint.bones;
            var boneCount = constrained.Count;
            if (constraint.data.local)
            {
                for (var i = 0; i < boneCount; i++)
                {
                    var child = constrained.Items[i];
                    this.SortBone(child.parent);
                    if (!this.updateCache.Contains(child)) this.updateCacheReset.Add(child);
                }
            }
            else
            {
                for (var i = 0; i < boneCount; i++)
                    this.SortBone(constrained.Items[i]);
            }

            this.updateCache.Add(constraint);

            for (var i = 0; i < boneCount; i++)
                SortReset(constrained.Items[i].children);
            for (var i = 0; i < boneCount; i++)
                constrained.Items[i].sorted = true;
        }

        private void SortPathConstraintAttachment(Skin skin, int slotIndex, Bone slotBone)
        {
            foreach (var entryObj in skin.Attachments.Keys)
            {
                var entry = entryObj;
                if (entry.SlotIndex == slotIndex) this.SortPathConstraintAttachment(entry.Attachment, slotBone);
            }
        }

        private void SortPathConstraintAttachment(Attachment attachment, Bone slotBone)
        {
            if (!(attachment is PathAttachment)) return;
            var pathBones = ((PathAttachment)attachment).bones;
            if (pathBones == null)
                this.SortBone(slotBone);
            else
            {
                var bones = this.bones;
                for (int i = 0, n = pathBones.Length; i < n;)
                {
                    var nn = pathBones[i++];
                    nn += i;
                    while (i < nn)
                        this.SortBone(bones.Items[pathBones[i++]]);
                }
            }
        }

        private void SortBone(Bone bone)
        {
            if (bone.sorted) return;
            var parent = bone.parent;
            if (parent != null) this.SortBone(parent);
            bone.sorted = true;
            this.updateCache.Add(bone);
        }

        private static void SortReset(ExposedList<Bone> bones)
        {
            var bonesItems = bones.Items;
            for (int i = 0, n = bones.Count; i < n; i++)
            {
                var bone = bonesItems[i];
                if (!bone.active) continue;
                if (bone.sorted) SortReset(bone.children);
                bone.sorted = false;
            }
        }

        /// <summary>Updates the world transform for each bone and applies constraints.</summary>
        public void UpdateWorldTransform()
        {
            var updateCacheReset = this.updateCacheReset;
            var updateCacheResetItems = updateCacheReset.Items;
            for (int i = 0, n = updateCacheReset.Count; i < n; i++)
            {
                var bone = updateCacheResetItems[i];
                bone.ax = bone.x;
                bone.ay = bone.y;
                bone.arotation = bone.rotation;
                bone.ascaleX = bone.scaleX;
                bone.ascaleY = bone.scaleY;
                bone.ashearX = bone.shearX;
                bone.ashearY = bone.shearY;
                bone.appliedValid = true;
            }
            var updateItems = this.updateCache.Items;
            for (int i = 0, n = this.updateCache.Count; i < n; i++)
                updateItems[i].Update();
        }

        /// <summary>
        /// Temporarily sets the root bone as a child of the specified bone, then updates the world transform for each bone and applies
        /// all constraints.
        /// </summary>
        public void UpdateWorldTransform(Bone parent)
        {
            // This partial update avoids computing the world transform for constrained bones when 1) the bone is not updated
            // before the constraint, 2) the constraint only needs to access the applied local transform, and 3) the constraint calls
            // updateWorldTransform.
            var updateCacheReset = this.updateCacheReset;
            var updateCacheResetItems = updateCacheReset.Items;
            for (int i = 0, n = updateCacheReset.Count; i < n; i++)
            {
                var bone = updateCacheResetItems[i];
                bone.ax = bone.x;
                bone.ay = bone.y;
                bone.arotation = bone.rotation;
                bone.ascaleX = bone.scaleX;
                bone.ascaleY = bone.scaleY;
                bone.ashearX = bone.shearX;
                bone.ashearY = bone.shearY;
                bone.appliedValid = true;
            }

            // Apply the parent bone transform to the root bone. The root bone always inherits scale, rotation and reflection.
            var rootBone = this.RootBone;
            float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d;
            rootBone.worldX = pa * this.x + pb * this.y + parent.worldX;
            rootBone.worldY = pc * this.x + pd * this.y + parent.worldY;

            var rotationY = rootBone.rotation + 90 + rootBone.shearY;
            var la = MathUtils.CosDeg(rootBone.rotation + rootBone.shearX) * rootBone.scaleX;
            var lb = MathUtils.CosDeg(rotationY) * rootBone.scaleY;
            var lc = MathUtils.SinDeg(rootBone.rotation + rootBone.shearX) * rootBone.scaleX;
            var ld = MathUtils.SinDeg(rotationY) * rootBone.scaleY;
            rootBone.a = (pa * la + pb * lc) * this.scaleX;
            rootBone.b = (pa * lb + pb * ld) * this.scaleX;
            rootBone.c = (pc * la + pd * lc) * this.scaleY;
            rootBone.d = (pc * lb + pd * ld) * this.scaleY;

            // Update everything except root bone.
            var updateCache = this.updateCache;
            var updateCacheItems = updateCache.Items;
            for (int i = 0, n = updateCache.Count; i < n; i++)
            {
                var updatable = updateCacheItems[i];
                if (updatable != rootBone)
                    updatable.Update();
            }
        }

        /// <summary>Sets the bones, constraints, and slots to their setup pose values.</summary>
        public void SetToSetupPose()
        {
            this.SetBonesToSetupPose();
            this.SetSlotsToSetupPose();
        }

        /// <summary>Sets the bones and constraints to their setup pose values.</summary>
        public void SetBonesToSetupPose()
        {
            var bonesItems = this.bones.Items;
            for (int i = 0, n = this.bones.Count; i < n; i++)
                bonesItems[i].SetToSetupPose();

            var ikConstraintsItems = this.ikConstraints.Items;
            for (int i = 0, n = this.ikConstraints.Count; i < n; i++)
            {
                var constraint = ikConstraintsItems[i];
                constraint.mix = constraint.data.mix;
                constraint.softness = constraint.data.softness;
                constraint.bendDirection = constraint.data.bendDirection;
                constraint.compress = constraint.data.compress;
                constraint.stretch = constraint.data.stretch;
            }

            var transformConstraintsItems = this.transformConstraints.Items;
            for (int i = 0, n = this.transformConstraints.Count; i < n; i++)
            {
                var constraint = transformConstraintsItems[i];
                var constraintData = constraint.data;
                constraint.rotateMix = constraintData.rotateMix;
                constraint.translateMix = constraintData.translateMix;
                constraint.scaleMix = constraintData.scaleMix;
                constraint.shearMix = constraintData.shearMix;
            }

            var pathConstraintItems = this.pathConstraints.Items;
            for (int i = 0, n = this.pathConstraints.Count; i < n; i++)
            {
                var constraint = pathConstraintItems[i];
                var constraintData = constraint.data;
                constraint.position = constraintData.position;
                constraint.spacing = constraintData.spacing;
                constraint.rotateMix = constraintData.rotateMix;
                constraint.translateMix = constraintData.translateMix;
            }
        }

        public void SetSlotsToSetupPose()
        {
            var slots = this.slots;
            var slotsItems = slots.Items;
            this.drawOrder.Clear();
            for (int i = 0, n = slots.Count; i < n; i++)
                this.drawOrder.Add(slotsItems[i]);

            for (int i = 0, n = slots.Count; i < n; i++)
                slotsItems[i].SetToSetupPose();
        }

        /// <returns>May be null.</returns>
        public Bone FindBone(string boneName)
        {
            if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
            var bones = this.bones;
            var bonesItems = bones.Items;
            for (int i = 0, n = bones.Count; i < n; i++)
            {
                var bone = bonesItems[i];
                if (bone.data.name == boneName) return bone;
            }
            return null;
        }

        /// <returns>-1 if the bone was not found.</returns>
        public int FindBoneIndex(string boneName)
        {
            if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
            var bones = this.bones;
            var bonesItems = bones.Items;
            for (int i = 0, n = bones.Count; i < n; i++)
                if (bonesItems[i].data.name == boneName) return i;
            return -1;
        }

        /// <returns>May be null.</returns>
        public Slot FindSlot(string slotName)
        {
            if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
            var slots = this.slots;
            var slotsItems = slots.Items;
            for (int i = 0, n = slots.Count; i < n; i++)
            {
                var slot = slotsItems[i];
                if (slot.data.name == slotName) return slot;
            }
            return null;
        }

        /// <returns>-1 if the bone was not found.</returns>
        public int FindSlotIndex(string slotName)
        {
            if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
            var slots = this.slots;
            var slotsItems = slots.Items;
            for (int i = 0, n = slots.Count; i < n; i++)
                if (slotsItems[i].data.name.Equals(slotName)) return i;
            return -1;
        }

        /// <summary>Sets a skin by name (see SetSkin).</summary>
        public void SetSkin(string skinName)
        {
            var foundSkin = this.data.FindSkin(skinName);
            if (foundSkin == null) throw new ArgumentException("Skin not found: " + skinName, "skinName");
            this.SetSkin(foundSkin);
        }

        /// <summary>
        /// <para>Sets the skin used to look up attachments before looking in the <see cref="SkeletonData.DefaultSkin"/>. If the
        /// skin is changed, <see cref="UpdateCache()"/> is called.
        /// </para>
        /// <para>Attachments from the new skin are attached if the corresponding attachment from the old skin was attached.
        /// If there was no old skin, each slot's setup mode attachment is attached from the new skin.
        /// </para>
        /// <para>After changing the skin, the visible attachments can be reset to those attached in the setup pose by calling
        /// <see cref="Skeleton.SetSlotsToSetupPose()"/>.
        /// Also, often <see cref="AnimationState.Apply(Skeleton)"/> is called before the next time the
        /// skeleton is rendered to allow any attachment keys in the current animation(s) to hide or show attachments from the new skin.</para>
        /// </summary>
        /// <param name="newSkin">May be null.</param>
        public void SetSkin(Skin newSkin)
        {
            if (newSkin == this.skin) return;
            if (newSkin != null)
            {
                if (this.skin != null)
                    newSkin.AttachAll(this, this.skin);
                else
                {
                    var slots = this.slots;
                    for (int i = 0, n = slots.Count; i < n; i++)
                    {
                        var slot = slots.Items[i];
                        var name = slot.data.attachmentName;
                        if (name != null)
                        {
                            var attachment = newSkin.GetAttachment(i, name);
                            if (attachment != null) slot.Attachment = attachment;
                        }
                    }
                }
            }
            this.skin = newSkin;
            this.UpdateCache();
        }

        /// <summary>Finds an attachment by looking in the {@link #skin} and {@link SkeletonData#defaultSkin} using the slot name and attachment name.</summary>
        /// <returns>May be null.</returns>
        public Attachment GetAttachment(string slotName, string attachmentName)
        {
            return this.GetAttachment(this.data.FindSlotIndex(slotName), attachmentName);
        }

        /// <summary>Finds an attachment by looking in the skin and skeletonData.defaultSkin using the slot index and attachment name.First the skin is checked and if the attachment was not found, the default skin is checked.</summary>
        /// <returns>May be null.</returns>
        public Attachment GetAttachment(int slotIndex, string attachmentName)
        {
            if (attachmentName == null) throw new ArgumentNullException("attachmentName", "attachmentName cannot be null.");
            if (this.skin != null)
            {
                var attachment = this.skin.GetAttachment(slotIndex, attachmentName);
                if (attachment != null) return attachment;
            }
            return this.data.defaultSkin != null ? this.data.defaultSkin.GetAttachment(slotIndex, attachmentName) : null;
        }

        /// <summary>A convenience method to set an attachment by finding the slot with FindSlot, finding the attachment with GetAttachment, then setting the slot's slot.Attachment.</summary>
        /// <param name="attachmentName">May be null to clear the slot's attachment.</param>
        public void SetAttachment(string slotName, string attachmentName)
        {
            if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
            var slots = this.slots;
            for (int i = 0, n = slots.Count; i < n; i++)
            {
                var slot = slots.Items[i];
                if (slot.data.name == slotName)
                {
                    Attachment attachment = null;
                    if (attachmentName != null)
                    {
                        attachment = this.GetAttachment(i, attachmentName);
                        if (attachment == null) throw new Exception("Attachment not found: " + attachmentName + ", for slot: " + slotName);
                    }
                    slot.Attachment = attachment;
                    return;
                }
            }
            throw new Exception("Slot not found: " + slotName);
        }

        /// <returns>May be null.</returns>
        public IkConstraint FindIkConstraint(string constraintName)
        {
            if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
            var ikConstraints = this.ikConstraints;
            for (int i = 0, n = ikConstraints.Count; i < n; i++)
            {
                var ikConstraint = ikConstraints.Items[i];
                if (ikConstraint.data.name == constraintName) return ikConstraint;
            }
            return null;
        }

        /// <returns>May be null.</returns>
        public TransformConstraint FindTransformConstraint(string constraintName)
        {
            if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
            var transformConstraints = this.transformConstraints;
            for (int i = 0, n = transformConstraints.Count; i < n; i++)
            {
                var transformConstraint = transformConstraints.Items[i];
                if (transformConstraint.data.Name == constraintName) return transformConstraint;
            }
            return null;
        }

        /// <returns>May be null.</returns>
        public PathConstraint FindPathConstraint(string constraintName)
        {
            if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
            var pathConstraints = this.pathConstraints;
            for (int i = 0, n = pathConstraints.Count; i < n; i++)
            {
                var constraint = pathConstraints.Items[i];
                if (constraint.data.Name.Equals(constraintName)) return constraint;
            }
            return null;
        }

        public void Update(float delta)
        {
            this.time += delta;
        }

        /// <summary>Returns the axis aligned bounding box (AABB) of the region and mesh attachments for the current pose.</summary>
        /// <param name="x">The horizontal distance between the skeleton origin and the left side of the AABB.</param>
        /// <param name="y">The vertical distance between the skeleton origin and the bottom side of the AABB.</param>
        /// <param name="width">The width of the AABB</param>
        /// <param name="height">The height of the AABB.</param>
        /// <param name="vertexBuffer">Reference to hold a float[]. May be a null reference. This method will assign it a new float[] with the appropriate size as needed.</param>
        public void GetBounds(out float x, out float y, out float width, out float height, ref float[] vertexBuffer)
        {
            var temp = vertexBuffer;
            temp = temp ?? new float[8];
            var drawOrderItems = this.drawOrder.Items;
            float minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int i = 0, n = drawOrderItems.Length; i < n; i++)
            {
                var slot = drawOrderItems[i];
                if (!slot.bone.active) continue;
                var verticesLength = 0;
                float[] vertices = null;
                var attachment = slot.attachment;
                var regionAttachment = attachment as RegionAttachment;
                if (regionAttachment != null)
                {
                    verticesLength = 8;
                    vertices = temp;
                    if (vertices.Length < 8) vertices = temp = new float[8];
                    regionAttachment.ComputeWorldVertices(slot.bone, temp, 0);
                }
                else
                {
                    var meshAttachment = attachment as MeshAttachment;
                    if (meshAttachment != null)
                    {
                        var mesh = meshAttachment;
                        verticesLength = mesh.WorldVerticesLength;
                        vertices = temp;
                        if (vertices.Length < verticesLength) vertices = temp = new float[verticesLength];
                        mesh.ComputeWorldVertices(slot, 0, verticesLength, temp, 0);
                    }
                }

                if (vertices != null)
                {
                    for (var ii = 0; ii < verticesLength; ii += 2)
                    {
                        float vx = vertices[ii], vy = vertices[ii + 1];
                        minX = Math.Min(minX, vx);
                        minY = Math.Min(minY, vy);
                        maxX = Math.Max(maxX, vx);
                        maxY = Math.Max(maxY, vy);
                    }
                }
            }
            x = minX;
            y = minY;
            width = maxX - minX;
            height = maxY - minY;
            vertexBuffer = temp;
        }
    }
}
