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

using UnityEngine;

namespace Spine.Unity
{
    /// <summary>Sets a GameObject's transform to match a bone on a Spine skeleton.</summary>
#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [AddComponentMenu("Spine/SkeletonUtilityBone")]
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonUtilityBone")]
    public class SkeletonUtilityBone : MonoBehaviour
    {
        public enum Mode
        {
            Follow,
            Override
        }

        public enum UpdatePhase
        {
            Local,
            World,
            Complete
        }

        #region Inspector
        /// <summary>If a bone isn't set, boneName is used to find the bone.</summary>
        public string boneName;
        public Transform parentReference;
        public Mode mode;
        public bool position, rotation, scale, zPosition = true;
        [Range(0f, 1f)]
        public float overrideAlpha = 1;
        #endregion

        public SkeletonUtility hierarchy;
        [System.NonSerialized] public Bone bone;
        [System.NonSerialized] public bool transformLerpComplete;
        [System.NonSerialized] public bool valid;
        private Transform cachedTransform;
        private Transform skeletonTransform;
        private bool incompatibleTransformMode;
        public bool IncompatibleTransformMode { get { return this.incompatibleTransformMode; } }

        public void Reset()
        {
            this.bone = null;
            this.cachedTransform = this.transform;
            this.valid = this.hierarchy != null && this.hierarchy.IsValid;
            if (!this.valid)
                return;
            this.skeletonTransform = this.hierarchy.transform;
            this.hierarchy.OnReset -= this.HandleOnReset;
            this.hierarchy.OnReset += this.HandleOnReset;
            this.DoUpdate(UpdatePhase.Local);
        }

        private void OnEnable()
        {
            if (this.hierarchy == null) this.hierarchy = this.transform.GetComponentInParent<SkeletonUtility>();
            if (this.hierarchy == null) return;

            this.hierarchy.RegisterBone(this);
            this.hierarchy.OnReset += this.HandleOnReset;
        }

        private void HandleOnReset()
        {
            this.Reset();
        }

        private void OnDisable()
        {
            if (this.hierarchy != null)
            {
                this.hierarchy.OnReset -= this.HandleOnReset;
                this.hierarchy.UnregisterBone(this);
            }
        }

        public void DoUpdate(UpdatePhase phase)
        {
            if (!this.valid)
            {
                this.Reset();
                return;
            }

            var skeleton = this.hierarchy.Skeleton;

            if (this.bone == null)
            {
                if (string.IsNullOrEmpty(this.boneName)) return;
                this.bone = skeleton.FindBone(this.boneName);
                if (this.bone == null)
                {
                    Debug.LogError("Bone not found: " + this.boneName, this);
                    return;
                }
            }
            if (!this.bone.Active) return;

            var positionScale = this.hierarchy.PositionScale;

            var thisTransform = this.cachedTransform;
            var skeletonFlipRotation = Mathf.Sign(skeleton.ScaleX * skeleton.ScaleY);
            if (this.mode == Mode.Follow)
            {
                switch (phase)
                {
                    case UpdatePhase.Local:
                        if (this.position)
                            thisTransform.localPosition = new Vector3(this.bone.x * positionScale, this.bone.y * positionScale, 0);

                        if (this.rotation)
                        {
                            if (this.bone.data.transformMode.InheritsRotation())
                            {
                                thisTransform.localRotation = Quaternion.Euler(0, 0, this.bone.rotation);
                            }
                            else
                            {
                                var euler = this.skeletonTransform.rotation.eulerAngles;
                                thisTransform.rotation = Quaternion.Euler(euler.x, euler.y, euler.z + (this.bone.WorldRotationX * skeletonFlipRotation));
                            }
                        }

                        if (this.scale)
                        {
                            thisTransform.localScale = new Vector3(this.bone.scaleX, this.bone.scaleY, 1f);
                            this.incompatibleTransformMode = BoneTransformModeIncompatible(this.bone);
                        }
                        break;
                    case UpdatePhase.World:
                    case UpdatePhase.Complete:
                        // Use Applied transform values (ax, ay, AppliedRotation, ascale) if world values were modified by constraints.
                        if (!this.bone.appliedValid)
                        {
                            this.bone.UpdateAppliedTransform();
                        }

                        if (this.position)
                            thisTransform.localPosition = new Vector3(this.bone.ax * positionScale, this.bone.ay * positionScale, 0);

                        if (this.rotation)
                        {
                            if (this.bone.data.transformMode.InheritsRotation())
                            {
                                thisTransform.localRotation = Quaternion.Euler(0, 0, this.bone.AppliedRotation);
                            }
                            else
                            {
                                var euler = this.skeletonTransform.rotation.eulerAngles;
                                thisTransform.rotation = Quaternion.Euler(euler.x, euler.y, euler.z + (this.bone.WorldRotationX * skeletonFlipRotation));
                            }
                        }

                        if (this.scale)
                        {
                            thisTransform.localScale = new Vector3(this.bone.ascaleX, this.bone.ascaleY, 1f);
                            this.incompatibleTransformMode = BoneTransformModeIncompatible(this.bone);
                        }
                        break;
                }

            }
            else if (this.mode == Mode.Override)
            {
                if (this.transformLerpComplete)
                    return;

                if (this.parentReference == null)
                {
                    if (this.position)
                    {
                        var clp = thisTransform.localPosition / positionScale;
                        this.bone.x = Mathf.Lerp(this.bone.x, clp.x, this.overrideAlpha);
                        this.bone.y = Mathf.Lerp(this.bone.y, clp.y, this.overrideAlpha);
                    }

                    if (this.rotation)
                    {
                        var angle = Mathf.LerpAngle(this.bone.Rotation, thisTransform.localRotation.eulerAngles.z, this.overrideAlpha);
                        this.bone.Rotation = angle;
                        this.bone.AppliedRotation = angle;
                    }

                    if (this.scale)
                    {
                        var cls = thisTransform.localScale;
                        this.bone.scaleX = Mathf.Lerp(this.bone.scaleX, cls.x, this.overrideAlpha);
                        this.bone.scaleY = Mathf.Lerp(this.bone.scaleY, cls.y, this.overrideAlpha);
                    }

                }
                else
                {
                    if (this.transformLerpComplete)
                        return;

                    if (this.position)
                    {
                        var pos = this.parentReference.InverseTransformPoint(thisTransform.position) / positionScale;
                        this.bone.x = Mathf.Lerp(this.bone.x, pos.x, this.overrideAlpha);
                        this.bone.y = Mathf.Lerp(this.bone.y, pos.y, this.overrideAlpha);
                    }

                    if (this.rotation)
                    {
                        var angle = Mathf.LerpAngle(this.bone.Rotation, Quaternion.LookRotation(Vector3.forward, this.parentReference.InverseTransformDirection(thisTransform.up)).eulerAngles.z, this.overrideAlpha);
                        this.bone.Rotation = angle;
                        this.bone.AppliedRotation = angle;
                    }

                    if (this.scale)
                    {
                        var cls = thisTransform.localScale;
                        this.bone.scaleX = Mathf.Lerp(this.bone.scaleX, cls.x, this.overrideAlpha);
                        this.bone.scaleY = Mathf.Lerp(this.bone.scaleY, cls.y, this.overrideAlpha);
                    }

                    this.incompatibleTransformMode = BoneTransformModeIncompatible(this.bone);
                }

                this.transformLerpComplete = true;
            }
        }

        public static bool BoneTransformModeIncompatible(Bone bone)
        {
            return !bone.data.transformMode.InheritsScale();
        }

        public void AddBoundingBox(string skinName, string slotName, string attachmentName)
        {
            SkeletonUtility.AddBoneRigidbody2D(this.transform.gameObject);
            SkeletonUtility.AddBoundingBoxGameObject(this.bone.skeleton, skinName, slotName, attachmentName, this.transform);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (this.IncompatibleTransformMode)
                Gizmos.DrawIcon(this.transform.position + new Vector3(0, 0.128f, 0), "icon-warning");
        }
#endif
    }
}
