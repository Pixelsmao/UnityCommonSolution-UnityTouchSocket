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
    /// <para>
    /// Stores the current pose for a transform constraint. A transform constraint adjusts the world transform of the constrained
    /// bones to match that of the target bone.</para>
    /// <para>
    /// See <a href="http://esotericsoftware.com/spine-transform-constraints">Transform constraints</a> in the Spine User Guide.</para>
    /// </summary>
    public class TransformConstraint : IUpdatable
    {
        internal TransformConstraintData data;
        internal ExposedList<Bone> bones;
        internal Bone target;
        internal float rotateMix, translateMix, scaleMix, shearMix;

        internal bool active;

        public TransformConstraint(TransformConstraintData data, Skeleton skeleton)
        {
            if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
            if (skeleton == null) throw new ArgumentNullException("skeleton", "skeleton cannot be null.");
            this.data = data;
            this.rotateMix = data.rotateMix;
            this.translateMix = data.translateMix;
            this.scaleMix = data.scaleMix;
            this.shearMix = data.shearMix;

            this.bones = new ExposedList<Bone>();
            foreach (var boneData in data.bones)
                this.bones.Add(skeleton.FindBone(boneData.name));

            this.target = skeleton.FindBone(data.target.name);
        }

        /// <summary>Copy constructor.</summary>
        public TransformConstraint(TransformConstraint constraint, Skeleton skeleton)
        {
            if (constraint == null) throw new ArgumentNullException("constraint cannot be null.");
            if (skeleton == null) throw new ArgumentNullException("skeleton cannot be null.");
            this.data = constraint.data;
            this.bones = new ExposedList<Bone>(constraint.Bones.Count);
            foreach (var bone in constraint.Bones)
                this.bones.Add(skeleton.Bones.Items[bone.data.index]);
            this.target = skeleton.Bones.Items[constraint.target.data.index];
            this.rotateMix = constraint.rotateMix;
            this.translateMix = constraint.translateMix;
            this.scaleMix = constraint.scaleMix;
            this.shearMix = constraint.shearMix;
        }

        /// <summary>Applies the constraint to the constrained bones.</summary>
        public void Apply()
        {
            this.Update();
        }

        public void Update()
        {
            if (this.data.local)
            {
                if (this.data.relative)
                    this.ApplyRelativeLocal();
                else
                    this.ApplyAbsoluteLocal();
            }
            else
            {
                if (this.data.relative)
                    this.ApplyRelativeWorld();
                else
                    this.ApplyAbsoluteWorld();
            }
        }

        private void ApplyAbsoluteWorld()
        {
            float rotateMix = this.rotateMix, translateMix = this.translateMix, scaleMix = this.scaleMix, shearMix = this.shearMix;
            var target = this.target;
            float ta = target.a, tb = target.b, tc = target.c, td = target.d;
            var degRadReflect = ta * td - tb * tc > 0 ? MathUtils.DegRad : -MathUtils.DegRad;
            float offsetRotation = this.data.offsetRotation * degRadReflect, offsetShearY = this.data.offsetShearY * degRadReflect;
            var bones = this.bones;
            for (int i = 0, n = bones.Count; i < n; i++)
            {
                var bone = bones.Items[i];
                var modified = false;

                if (rotateMix != 0)
                {
                    float a = bone.a, b = bone.b, c = bone.c, d = bone.d;
                    var r = MathUtils.Atan2(tc, ta) - MathUtils.Atan2(c, a) + offsetRotation;
                    if (r > MathUtils.PI)
                        r -= MathUtils.PI2;
                    else if (r < -MathUtils.PI) r += MathUtils.PI2;
                    r *= rotateMix;
                    float cos = MathUtils.Cos(r), sin = MathUtils.Sin(r);
                    bone.a = cos * a - sin * c;
                    bone.b = cos * b - sin * d;
                    bone.c = sin * a + cos * c;
                    bone.d = sin * b + cos * d;
                    modified = true;
                }

                if (translateMix != 0)
                {
                    //Vector2 temp = this.temp;
                    target.LocalToWorld(this.data.offsetX, this.data.offsetY, out var tx, out var ty); //target.localToWorld(temp.set(data.offsetX, data.offsetY));
                    bone.worldX += (tx - bone.worldX) * translateMix;
                    bone.worldY += (ty - bone.worldY) * translateMix;
                    modified = true;
                }

                if (scaleMix > 0)
                {
                    var s = (float)Math.Sqrt(bone.a * bone.a + bone.c * bone.c);
                    if (s != 0) s = (s + ((float)Math.Sqrt(ta * ta + tc * tc) - s + this.data.offsetScaleX) * scaleMix) / s;
                    bone.a *= s;
                    bone.c *= s;
                    s = (float)Math.Sqrt(bone.b * bone.b + bone.d * bone.d);
                    if (s != 0) s = (s + ((float)Math.Sqrt(tb * tb + td * td) - s + this.data.offsetScaleY) * scaleMix) / s;
                    bone.b *= s;
                    bone.d *= s;
                    modified = true;
                }

                if (shearMix > 0)
                {
                    float b = bone.b, d = bone.d;
                    var by = MathUtils.Atan2(d, b);
                    var r = MathUtils.Atan2(td, tb) - MathUtils.Atan2(tc, ta) - (by - MathUtils.Atan2(bone.c, bone.a));
                    if (r > MathUtils.PI)
                        r -= MathUtils.PI2;
                    else if (r < -MathUtils.PI) r += MathUtils.PI2;
                    r = by + (r + offsetShearY) * shearMix;
                    var s = (float)Math.Sqrt(b * b + d * d);
                    bone.b = MathUtils.Cos(r) * s;
                    bone.d = MathUtils.Sin(r) * s;
                    modified = true;
                }

                if (modified) bone.appliedValid = false;
            }
        }

        private void ApplyRelativeWorld()
        {
            float rotateMix = this.rotateMix, translateMix = this.translateMix, scaleMix = this.scaleMix, shearMix = this.shearMix;
            var target = this.target;
            float ta = target.a, tb = target.b, tc = target.c, td = target.d;
            var degRadReflect = ta * td - tb * tc > 0 ? MathUtils.DegRad : -MathUtils.DegRad;
            float offsetRotation = this.data.offsetRotation * degRadReflect, offsetShearY = this.data.offsetShearY * degRadReflect;
            var bones = this.bones;
            for (int i = 0, n = bones.Count; i < n; i++)
            {
                var bone = bones.Items[i];
                var modified = false;

                if (rotateMix != 0)
                {
                    float a = bone.a, b = bone.b, c = bone.c, d = bone.d;
                    var r = MathUtils.Atan2(tc, ta) + offsetRotation;
                    if (r > MathUtils.PI)
                        r -= MathUtils.PI2;
                    else if (r < -MathUtils.PI) r += MathUtils.PI2;
                    r *= rotateMix;
                    float cos = MathUtils.Cos(r), sin = MathUtils.Sin(r);
                    bone.a = cos * a - sin * c;
                    bone.b = cos * b - sin * d;
                    bone.c = sin * a + cos * c;
                    bone.d = sin * b + cos * d;
                    modified = true;
                }

                if (translateMix != 0)
                {
                    //Vector2 temp = this.temp;
                    target.LocalToWorld(this.data.offsetX, this.data.offsetY, out var tx, out var ty); //target.localToWorld(temp.set(data.offsetX, data.offsetY));
                    bone.worldX += tx * translateMix;
                    bone.worldY += ty * translateMix;
                    modified = true;
                }

                if (scaleMix > 0)
                {
                    var s = ((float)Math.Sqrt(ta * ta + tc * tc) - 1 + this.data.offsetScaleX) * scaleMix + 1;
                    bone.a *= s;
                    bone.c *= s;
                    s = ((float)Math.Sqrt(tb * tb + td * td) - 1 + this.data.offsetScaleY) * scaleMix + 1;
                    bone.b *= s;
                    bone.d *= s;
                    modified = true;
                }

                if (shearMix > 0)
                {
                    var r = MathUtils.Atan2(td, tb) - MathUtils.Atan2(tc, ta);
                    if (r > MathUtils.PI)
                        r -= MathUtils.PI2;
                    else if (r < -MathUtils.PI) r += MathUtils.PI2;
                    float b = bone.b, d = bone.d;
                    r = MathUtils.Atan2(d, b) + (r - MathUtils.PI / 2 + offsetShearY) * shearMix;
                    var s = (float)Math.Sqrt(b * b + d * d);
                    bone.b = MathUtils.Cos(r) * s;
                    bone.d = MathUtils.Sin(r) * s;
                    modified = true;
                }

                if (modified) bone.appliedValid = false;
            }
        }

        private void ApplyAbsoluteLocal()
        {
            float rotateMix = this.rotateMix, translateMix = this.translateMix, scaleMix = this.scaleMix, shearMix = this.shearMix;
            var target = this.target;
            if (!target.appliedValid) target.UpdateAppliedTransform();
            var bonesItems = this.bones.Items;
            for (int i = 0, n = this.bones.Count; i < n; i++)
            {
                var bone = bonesItems[i];
                if (!bone.appliedValid) bone.UpdateAppliedTransform();

                var rotation = bone.arotation;
                if (rotateMix != 0)
                {
                    var r = target.arotation - rotation + this.data.offsetRotation;
                    r -= (16384 - (int)(16384.499999999996 - r / 360)) * 360;
                    rotation += r * rotateMix;
                }

                float x = bone.ax, y = bone.ay;
                if (translateMix != 0)
                {
                    x += (target.ax - x + this.data.offsetX) * translateMix;
                    y += (target.ay - y + this.data.offsetY) * translateMix;
                }

                float scaleX = bone.ascaleX, scaleY = bone.ascaleY;
                if (scaleMix != 0)
                {
                    if (scaleX != 0) scaleX = (scaleX + (target.ascaleX - scaleX + this.data.offsetScaleX) * scaleMix) / scaleX;
                    if (scaleY != 0) scaleY = (scaleY + (target.ascaleY - scaleY + this.data.offsetScaleY) * scaleMix) / scaleY;
                }

                var shearY = bone.ashearY;
                if (shearMix != 0)
                {
                    var r = target.ashearY - shearY + this.data.offsetShearY;
                    r -= (16384 - (int)(16384.499999999996 - r / 360)) * 360;
                    shearY += r * shearMix;
                }

                bone.UpdateWorldTransform(x, y, rotation, scaleX, scaleY, bone.ashearX, shearY);
            }
        }

        private void ApplyRelativeLocal()
        {
            float rotateMix = this.rotateMix, translateMix = this.translateMix, scaleMix = this.scaleMix, shearMix = this.shearMix;
            var target = this.target;
            if (!target.appliedValid) target.UpdateAppliedTransform();
            var bonesItems = this.bones.Items;
            for (int i = 0, n = this.bones.Count; i < n; i++)
            {
                var bone = bonesItems[i];
                if (!bone.appliedValid) bone.UpdateAppliedTransform();

                var rotation = bone.arotation;
                if (rotateMix != 0) rotation += (target.arotation + this.data.offsetRotation) * rotateMix;

                float x = bone.ax, y = bone.ay;
                if (translateMix != 0)
                {
                    x += (target.ax + this.data.offsetX) * translateMix;
                    y += (target.ay + this.data.offsetY) * translateMix;
                }

                float scaleX = bone.ascaleX, scaleY = bone.ascaleY;
                if (scaleMix != 0)
                {
                    scaleX *= ((target.ascaleX - 1 + this.data.offsetScaleX) * scaleMix) + 1;
                    scaleY *= ((target.ascaleY - 1 + this.data.offsetScaleY) * scaleMix) + 1;
                }

                var shearY = bone.ashearY;
                if (shearMix != 0) shearY += (target.ashearY + this.data.offsetShearY) * shearMix;

                bone.UpdateWorldTransform(x, y, rotation, scaleX, scaleY, bone.ashearX, shearY);
            }
        }

        /// <summary>The bones that will be modified by this transform constraint.</summary>
        public ExposedList<Bone> Bones { get { return this.bones; } }
        /// <summary>The target bone whose world transform will be copied to the constrained bones.</summary>
        public Bone Target { get { return this.target; } set { this.target = value; } }
        /// <summary>A percentage (0-1) that controls the mix between the constrained and unconstrained rotations.</summary>
        public float RotateMix { get { return this.rotateMix; } set { this.rotateMix = value; } }
        /// <summary>A percentage (0-1) that controls the mix between the constrained and unconstrained translations.</summary>
        public float TranslateMix { get { return this.translateMix; } set { this.translateMix = value; } }
        /// <summary>A percentage (0-1) that controls the mix between the constrained and unconstrained scales.</summary>
        public float ScaleMix { get { return this.scaleMix; } set { this.scaleMix = value; } }
        /// <summary>A percentage (0-1) that controls the mix between the constrained and unconstrained scales.</summary>
        public float ShearMix { get { return this.shearMix; } set { this.shearMix = value; } }
        public bool Active { get { return this.active; } }
        /// <summary>The transform constraint's setup pose data.</summary>
        public TransformConstraintData Data { get { return this.data; } }

        public override string ToString()
        {
            return this.data.name;
        }
    }
}
