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

using System;
using UnityEngine;

namespace Spine.Unity
{

    /// <summary>Sets a GameObject's transform to match a bone on a Spine skeleton.</summary>
#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [AddComponentMenu("Spine/BoneFollower")]
    [HelpURL("http://esotericsoftware.com/spine-unity#BoneFollower")]
    public class BoneFollower : MonoBehaviour
    {

        #region Inspector
        public SkeletonRenderer skeletonRenderer;
        public SkeletonRenderer SkeletonRenderer
        {
            get { return this.skeletonRenderer; }
            set
            {
                this.skeletonRenderer = value;
                this.Initialize();
            }
        }

        /// <summary>If a bone isn't set in code, boneName is used to find the bone at the beginning. For runtime switching by name, use SetBoneByName. You can also set the BoneFollower.bone field directly.</summary>
        [SpineBone(dataField: "skeletonRenderer")]
        public string boneName;

        public bool followXYPosition = true;
        public bool followZPosition = true;
        public bool followBoneRotation = true;

        [Tooltip("Follows the skeleton's flip state by controlling this Transform's local scale.")]
        public bool followSkeletonFlip = true;

        [Tooltip("Follows the target bone's local scale. BoneFollower cannot inherit world/skewed scale because of UnityEngine.Transform property limitations.")]
        public bool followLocalScale = false;

        public enum AxisOrientation
        {
            XAxis = 1,
            YAxis
        }
        [Tooltip("Applies when 'Follow Skeleton Flip' is disabled but 'Follow Bone Rotation' is enabled."
            + " When flipping the skeleton by scaling its Transform, this follower's rotation is adjusted"
            + " instead of its scale to follow the bone orientation. When one of the axes is flipped, "
            + " only one axis can be followed, either the X or the Y axis, which is selected here.")]
        public AxisOrientation maintainedAxisOrientation = AxisOrientation.XAxis;

        [UnityEngine.Serialization.FormerlySerializedAs("resetOnAwake")]
        public bool initializeOnAwake = true;
        #endregion

        [NonSerialized] public bool valid;
        [NonSerialized] public Bone bone;

        private Transform skeletonTransform;
        private bool skeletonTransformIsParent;

        /// <summary>
        /// Sets the target bone by its bone name. Returns false if no bone was found. To set the bone by reference, use BoneFollower.bone directly.</summary>
        public bool SetBone(string name)
        {
            this.bone = this.skeletonRenderer.skeleton.FindBone(name);
            if (this.bone == null)
            {
                Debug.LogError("Bone not found: " + name, this);
                return false;
            }
            this.boneName = name;
            return true;
        }

        public void Awake()
        {
            if (this.initializeOnAwake) this.Initialize();
        }

        public void HandleRebuildRenderer(SkeletonRenderer skeletonRenderer)
        {
            this.Initialize();
        }

        public void Initialize()
        {
            this.bone = null;
            this.valid = this.skeletonRenderer != null && this.skeletonRenderer.valid;
            if (!this.valid) return;

            this.skeletonTransform = this.skeletonRenderer.transform;
            this.skeletonRenderer.OnRebuild -= this.HandleRebuildRenderer;
            this.skeletonRenderer.OnRebuild += this.HandleRebuildRenderer;
            this.skeletonTransformIsParent = Transform.ReferenceEquals(this.skeletonTransform, this.transform.parent);

            if (!string.IsNullOrEmpty(this.boneName))
                this.bone = this.skeletonRenderer.skeleton.FindBone(this.boneName);

#if UNITY_EDITOR
            if (Application.isEditor)
                this.LateUpdate();
#endif
        }

        private void OnDestroy()
        {
            if (this.skeletonRenderer != null)
                this.skeletonRenderer.OnRebuild -= this.HandleRebuildRenderer;
        }

        public void LateUpdate()
        {
            if (!this.valid)
            {
                this.Initialize();
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                this.skeletonTransformIsParent = Transform.ReferenceEquals(this.skeletonTransform, this.transform.parent);
#endif

            if (this.bone == null)
            {
                if (string.IsNullOrEmpty(this.boneName)) return;
                this.bone = this.skeletonRenderer.skeleton.FindBone(this.boneName);
                if (!this.SetBone(this.boneName)) return;
            }

            var thisTransform = this.transform;
            float additionalFlipScale = 1;
            if (this.skeletonTransformIsParent)
            {
                // Recommended setup: Use local transform properties if Spine GameObject is the immediate parent
                thisTransform.localPosition = new Vector3(this.followXYPosition ? this.bone.worldX : thisTransform.localPosition.x,
                                                        this.followXYPosition ? this.bone.worldY : thisTransform.localPosition.y,
                                                        this.followZPosition ? 0f : thisTransform.localPosition.z);
                if (this.followBoneRotation)
                {
                    var halfRotation = Mathf.Atan2(this.bone.c, this.bone.a) * 0.5f;
                    if (this.followLocalScale && this.bone.scaleX < 0) // Negate rotation from negative scaleX. Don't use negative determinant. local scaleY doesn't factor into used rotation.
                        halfRotation += Mathf.PI * 0.5f;

                    var q = default(Quaternion);
                    q.z = Mathf.Sin(halfRotation);
                    q.w = Mathf.Cos(halfRotation);
                    thisTransform.localRotation = q;
                }
            }
            else
            {
                // For special cases: Use transform world properties if transform relationship is complicated
                var targetWorldPosition = this.skeletonTransform.TransformPoint(new Vector3(this.bone.worldX, this.bone.worldY, 0f));
                if (!this.followZPosition) targetWorldPosition.z = thisTransform.position.z;
                if (!this.followXYPosition)
                {
                    targetWorldPosition.x = thisTransform.position.x;
                    targetWorldPosition.y = thisTransform.position.y;
                }

                var skeletonLossyScale = this.skeletonTransform.lossyScale;
                var transformParent = thisTransform.parent;
                var parentLossyScale = transformParent != null ? transformParent.lossyScale : Vector3.one;
                if (this.followBoneRotation)
                {
                    var boneWorldRotation = this.bone.WorldRotationX;

                    if ((skeletonLossyScale.x * skeletonLossyScale.y) < 0)
                        boneWorldRotation = -boneWorldRotation;

                    if (this.followSkeletonFlip || this.maintainedAxisOrientation == AxisOrientation.XAxis)
                    {
                        if ((skeletonLossyScale.x * parentLossyScale.x < 0))
                            boneWorldRotation += 180f;
                    }
                    else
                    {
                        if ((skeletonLossyScale.y * parentLossyScale.y < 0))
                            boneWorldRotation += 180f;
                    }

                    var worldRotation = this.skeletonTransform.rotation.eulerAngles;
                    if (this.followLocalScale && this.bone.scaleX < 0) boneWorldRotation += 180f;
                    thisTransform.SetPositionAndRotation(targetWorldPosition, Quaternion.Euler(worldRotation.x, worldRotation.y, worldRotation.z + boneWorldRotation));
                }
                else
                {
                    thisTransform.position = targetWorldPosition;
                }

                additionalFlipScale = Mathf.Sign(skeletonLossyScale.x * parentLossyScale.x
                                                * skeletonLossyScale.y * parentLossyScale.y);
            }

            var localScale = this.followLocalScale ? new Vector3(this.bone.scaleX, this.bone.scaleY, 1f) : new Vector3(1f, 1f, 1f);
            if (this.followSkeletonFlip)
                localScale.y *= Mathf.Sign(this.bone.skeleton.ScaleX * this.bone.skeleton.ScaleY) * additionalFlipScale;

            thisTransform.localScale = localScale;
        }
    }

}
