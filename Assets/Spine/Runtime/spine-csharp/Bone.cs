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
    /// Stores a bone's current pose.
    /// <para>
    /// A bone has a local transform which is used to compute its world transform. A bone also has an applied transform, which is a
    /// local transform that can be applied to compute the world transform. The local transform and applied transform may differ if a
    /// constraint or application code modifies the world transform after it was computed from the local transform.
    /// </para>
    /// </summary>
    public class Bone : IUpdatable
    {
        public static bool yDown;

        internal BoneData data;
        internal Skeleton skeleton;
        internal Bone parent;
        internal ExposedList<Bone> children = new ExposedList<Bone>();
        internal float x, y, rotation, scaleX, scaleY, shearX, shearY;
        internal float ax, ay, arotation, ascaleX, ascaleY, ashearX, ashearY;
        internal bool appliedValid;

        internal float a, b, worldX;
        internal float c, d, worldY;

        internal bool sorted, active;

        public BoneData Data { get { return this.data; } }
        public Skeleton Skeleton { get { return this.skeleton; } }
        public Bone Parent { get { return this.parent; } }
        public ExposedList<Bone> Children { get { return this.children; } }
        /// <summary>Returns false when the bone has not been computed because <see cref="BoneData.SkinRequired"/> is true and the
        /// <see cref="Skeleton.Skin">active skin</see> does not <see cref="Skin.Bones">contain</see> this bone.</summary>
        public bool Active { get { return this.active; } }
        /// <summary>The local X translation.</summary>
        public float X { get { return this.x; } set { this.x = value; } }
        /// <summary>The local Y translation.</summary>
        public float Y { get { return this.y; } set { this.y = value; } }
        /// <summary>The local rotation.</summary>
        public float Rotation { get { return this.rotation; } set { this.rotation = value; } }

        /// <summary>The local scaleX.</summary>
        public float ScaleX { get { return this.scaleX; } set { this.scaleX = value; } }

        /// <summary>The local scaleY.</summary>
        public float ScaleY { get { return this.scaleY; } set { this.scaleY = value; } }

        /// <summary>The local shearX.</summary>
        public float ShearX { get { return this.shearX; } set { this.shearX = value; } }

        /// <summary>The local shearY.</summary>
        public float ShearY { get { return this.shearY; } set { this.shearY = value; } }

        /// <summary>The rotation, as calculated by any constraints.</summary>
        public float AppliedRotation { get { return this.arotation; } set { this.arotation = value; } }

        /// <summary>The applied local x translation.</summary>
        public float AX { get { return this.ax; } set { this.ax = value; } }

        /// <summary>The applied local y translation.</summary>
        public float AY { get { return this.ay; } set { this.ay = value; } }

        /// <summary>The applied local scaleX.</summary>
        public float AScaleX { get { return this.ascaleX; } set { this.ascaleX = value; } }

        /// <summary>The applied local scaleY.</summary>
        public float AScaleY { get { return this.ascaleY; } set { this.ascaleY = value; } }

        /// <summary>The applied local shearX.</summary>
        public float AShearX { get { return this.ashearX; } set { this.ashearX = value; } }

        /// <summary>The applied local shearY.</summary>
        public float AShearY { get { return this.ashearY; } set { this.ashearY = value; } }

        public float A { get { return this.a; } }
        public float B { get { return this.b; } }
        public float C { get { return this.c; } }
        public float D { get { return this.d; } }

        public float WorldX { get { return this.worldX; } }
        public float WorldY { get { return this.worldY; } }
        public float WorldRotationX { get { return MathUtils.Atan2(this.c, this.a) * MathUtils.RadDeg; } }
        public float WorldRotationY { get { return MathUtils.Atan2(this.d, this.b) * MathUtils.RadDeg; } }

        /// <summary>Returns the magnitide (always positive) of the world scale X.</summary>
        public float WorldScaleX { get { return (float)Math.Sqrt(this.a * this.a + this.c * this.c); } }
        /// <summary>Returns the magnitide (always positive) of the world scale Y.</summary>
        public float WorldScaleY { get { return (float)Math.Sqrt(this.b * this.b + this.d * this.d); } }

        /// <param name="parent">May be null.</param>
        public Bone(BoneData data, Skeleton skeleton, Bone parent)
        {
            if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
            if (skeleton == null) throw new ArgumentNullException("skeleton", "skeleton cannot be null.");
            this.data = data;
            this.skeleton = skeleton;
            this.parent = parent;
            this.SetToSetupPose();
        }

        /// <summary>Same as <see cref="UpdateWorldTransform"/>. This method exists for Bone to implement <see cref="Spine.IUpdatable"/>.</summary>
        public void Update()
        {
            this.UpdateWorldTransform(this.x, this.y, this.rotation, this.scaleX, this.scaleY, this.shearX, this.shearY);
        }

        /// <summary>Computes the world transform using the parent bone and this bone's local transform.</summary>
        public void UpdateWorldTransform()
        {
            this.UpdateWorldTransform(this.x, this.y, this.rotation, this.scaleX, this.scaleY, this.shearX, this.shearY);
        }

        /// <summary>Computes the world transform using the parent bone and the specified local transform.</summary>
        public void UpdateWorldTransform(float x, float y, float rotation, float scaleX, float scaleY, float shearX, float shearY)
        {
            this.ax = x;
            this.ay = y;
            this.arotation = rotation;
            this.ascaleX = scaleX;
            this.ascaleY = scaleY;
            this.ashearX = shearX;
            this.ashearY = shearY;
            this.appliedValid = true;
            var skeleton = this.skeleton;

            var parent = this.parent;
            if (parent == null)
            { // Root bone.
                float rotationY = rotation + 90 + shearY, sx = skeleton.ScaleX, sy = skeleton.ScaleY;
                this.a = MathUtils.CosDeg(rotation + shearX) * scaleX * sx;
                this.b = MathUtils.CosDeg(rotationY) * scaleY * sx;
                this.c = MathUtils.SinDeg(rotation + shearX) * scaleX * sy;
                this.d = MathUtils.SinDeg(rotationY) * scaleY * sy;
                this.worldX = x * sx + skeleton.x;
                this.worldY = y * sy + skeleton.y;
                return;
            }

            float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d;
            this.worldX = pa * x + pb * y + parent.worldX;
            this.worldY = pc * x + pd * y + parent.worldY;

            switch (this.data.transformMode)
            {
                case TransformMode.Normal:
                    {
                        var rotationY = rotation + 90 + shearY;
                        var la = MathUtils.CosDeg(rotation + shearX) * scaleX;
                        var lb = MathUtils.CosDeg(rotationY) * scaleY;
                        var lc = MathUtils.SinDeg(rotation + shearX) * scaleX;
                        var ld = MathUtils.SinDeg(rotationY) * scaleY;
                        this.a = pa * la + pb * lc;
                        this.b = pa * lb + pb * ld;
                        this.c = pc * la + pd * lc;
                        this.d = pc * lb + pd * ld;
                        return;
                    }
                case TransformMode.OnlyTranslation:
                    {
                        var rotationY = rotation + 90 + shearY;
                        this.a = MathUtils.CosDeg(rotation + shearX) * scaleX;
                        this.b = MathUtils.CosDeg(rotationY) * scaleY;
                        this.c = MathUtils.SinDeg(rotation + shearX) * scaleX;
                        this.d = MathUtils.SinDeg(rotationY) * scaleY;
                        break;
                    }
                case TransformMode.NoRotationOrReflection:
                    {
                        float s = pa * pa + pc * pc, prx;
                        if (s > 0.0001f)
                        {
                            s = Math.Abs(pa * pd - pb * pc) / s;
                            pa /= skeleton.ScaleX;
                            pc /= skeleton.ScaleY;
                            pb = pc * s;
                            pd = pa * s;
                            prx = MathUtils.Atan2(pc, pa) * MathUtils.RadDeg;
                        }
                        else
                        {
                            pa = 0;
                            pc = 0;
                            prx = 90 - MathUtils.Atan2(pd, pb) * MathUtils.RadDeg;
                        }
                        var rx = rotation + shearX - prx;
                        var ry = rotation + shearY - prx + 90;
                        var la = MathUtils.CosDeg(rx) * scaleX;
                        var lb = MathUtils.CosDeg(ry) * scaleY;
                        var lc = MathUtils.SinDeg(rx) * scaleX;
                        var ld = MathUtils.SinDeg(ry) * scaleY;
                        this.a = pa * la - pb * lc;
                        this.b = pa * lb - pb * ld;
                        this.c = pc * la + pd * lc;
                        this.d = pc * lb + pd * ld;
                        break;
                    }
                case TransformMode.NoScale:
                case TransformMode.NoScaleOrReflection:
                    {
                        float cos = MathUtils.CosDeg(rotation), sin = MathUtils.SinDeg(rotation);
                        var za = (pa * cos + pb * sin) / skeleton.ScaleX;
                        var zc = (pc * cos + pd * sin) / skeleton.ScaleY;
                        var s = (float)Math.Sqrt(za * za + zc * zc);
                        if (s > 0.00001f) s = 1 / s;
                        za *= s;
                        zc *= s;
                        s = (float)Math.Sqrt(za * za + zc * zc);
                        if (this.data.transformMode == TransformMode.NoScale
                            && (pa * pd - pb * pc < 0) != (skeleton.ScaleX < 0 != skeleton.ScaleY < 0)) s = -s;

                        var r = MathUtils.PI / 2 + MathUtils.Atan2(zc, za);
                        var zb = MathUtils.Cos(r) * s;
                        var zd = MathUtils.Sin(r) * s;
                        var la = MathUtils.CosDeg(shearX) * scaleX;
                        var lb = MathUtils.CosDeg(90 + shearY) * scaleY;
                        var lc = MathUtils.SinDeg(shearX) * scaleX;
                        var ld = MathUtils.SinDeg(90 + shearY) * scaleY;
                        this.a = za * la + zb * lc;
                        this.b = za * lb + zb * ld;
                        this.c = zc * la + zd * lc;
                        this.d = zc * lb + zd * ld;
                        break;
                    }
            }

            this.a *= skeleton.ScaleX;
            this.b *= skeleton.ScaleX;
            this.c *= skeleton.ScaleY;
            this.d *= skeleton.ScaleY;
        }

        public void SetToSetupPose()
        {
            var data = this.data;
            this.x = data.x;
            this.y = data.y;
            this.rotation = data.rotation;
            this.scaleX = data.scaleX;
            this.scaleY = data.scaleY;
            this.shearX = data.shearX;
            this.shearY = data.shearY;
        }

        /// <summary>
        /// Computes the individual applied transform values from the world transform. This can be useful to perform processing using
        /// the applied transform after the world transform has been modified directly (eg, by a constraint)..
        ///
        /// Some information is ambiguous in the world transform, such as -1,-1 scale versus 180 rotation.
        /// </summary>
        internal void UpdateAppliedTransform()
        {
            this.appliedValid = true;
            var parent = this.parent;
            if (parent == null)
            {
                this.ax = this.worldX;
                this.ay = this.worldY;
                this.arotation = MathUtils.Atan2(this.c, this.a) * MathUtils.RadDeg;
                this.ascaleX = (float)Math.Sqrt(this.a * this.a + this.c * this.c);
                this.ascaleY = (float)Math.Sqrt(this.b * this.b + this.d * this.d);
                this.ashearX = 0;
                this.ashearY = MathUtils.Atan2(this.a * this.b + this.c * this.d, this.a * this.d - this.b * this.c) * MathUtils.RadDeg;
                return;
            }
            float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d;
            var pid = 1 / (pa * pd - pb * pc);
            float dx = this.worldX - parent.worldX, dy = this.worldY - parent.worldY;
            this.ax = (dx * pd * pid - dy * pb * pid);
            this.ay = (dy * pa * pid - dx * pc * pid);
            var ia = pid * pd;
            var id = pid * pa;
            var ib = pid * pb;
            var ic = pid * pc;
            var ra = ia * this.a - ib * this.c;
            var rb = ia * this.b - ib * this.d;
            var rc = id * this.c - ic * this.a;
            var rd = id * this.d - ic * this.b;
            this.ashearX = 0;
            this.ascaleX = (float)Math.Sqrt(ra * ra + rc * rc);
            if (this.ascaleX > 0.0001f)
            {
                var det = ra * rd - rb * rc;
                this.ascaleY = det / this.ascaleX;
                this.ashearY = MathUtils.Atan2(ra * rb + rc * rd, det) * MathUtils.RadDeg;
                this.arotation = MathUtils.Atan2(rc, ra) * MathUtils.RadDeg;
            }
            else
            {
                this.ascaleX = 0;
                this.ascaleY = (float)Math.Sqrt(rb * rb + rd * rd);
                this.ashearY = 0;
                this.arotation = 90 - MathUtils.Atan2(rd, rb) * MathUtils.RadDeg;
            }
        }

        public void WorldToLocal(float worldX, float worldY, out float localX, out float localY)
        {
            float a = this.a, b = this.b, c = this.c, d = this.d;
            var invDet = 1 / (a * d - b * c);
            float x = worldX - this.worldX, y = worldY - this.worldY;
            localX = (x * d * invDet - y * b * invDet);
            localY = (y * a * invDet - x * c * invDet);
        }

        public void LocalToWorld(float localX, float localY, out float worldX, out float worldY)
        {
            worldX = localX * this.a + localY * this.b + this.worldX;
            worldY = localX * this.c + localY * this.d + this.worldY;
        }

        public float WorldToLocalRotationX
        {
            get
            {
                var parent = this.parent;
                if (parent == null) return this.arotation;
                float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d, a = this.a, c = this.c;
                return MathUtils.Atan2(pa * c - pc * a, pd * a - pb * c) * MathUtils.RadDeg;
            }
        }

        public float WorldToLocalRotationY
        {
            get
            {
                var parent = this.parent;
                if (parent == null) return this.arotation;
                float pa = parent.a, pb = parent.b, pc = parent.c, pd = parent.d, b = this.b, d = this.d;
                return MathUtils.Atan2(pa * d - pc * b, pd * b - pb * d) * MathUtils.RadDeg;
            }
        }

        public float WorldToLocalRotation(float worldRotation)
        {
            float sin = MathUtils.SinDeg(worldRotation), cos = MathUtils.CosDeg(worldRotation);
            return MathUtils.Atan2(this.a * sin - this.c * cos, this.d * cos - this.b * sin) * MathUtils.RadDeg + this.rotation - this.shearX;
        }

        public float LocalToWorldRotation(float localRotation)
        {
            localRotation -= this.rotation - this.shearX;
            float sin = MathUtils.SinDeg(localRotation), cos = MathUtils.CosDeg(localRotation);
            return MathUtils.Atan2(cos * this.c + sin * this.d, cos * this.a + sin * this.b) * MathUtils.RadDeg;
        }

        /// <summary>
        /// Rotates the world transform the specified amount and sets isAppliedValid to false.
        /// </summary>
        /// <param name="degrees">Degrees.</param>
        public void RotateWorld(float degrees)
        {
            float a = this.a, b = this.b, c = this.c, d = this.d;
            float cos = MathUtils.CosDeg(degrees), sin = MathUtils.SinDeg(degrees);
            this.a = cos * a - sin * c;
            this.b = cos * b - sin * d;
            this.c = sin * a + cos * c;
            this.d = sin * b + cos * d;
            this.appliedValid = false;
        }

        public override string ToString()
        {
            return this.data.name;
        }
    }
}
