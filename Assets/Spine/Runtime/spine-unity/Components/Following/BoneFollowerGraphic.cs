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
    using AxisOrientation = BoneFollower.AxisOrientation;

#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [RequireComponent(typeof(RectTransform)), DisallowMultipleComponent]
    [AddComponentMenu("Spine/UI/BoneFollowerGraphic")]
    [HelpURL("http://esotericsoftware.com/spine-unity#BoneFollowerGraphic")]
    public class BoneFollowerGraphic : MonoBehaviour
    {
        public SkeletonGraphic skeletonGraphic;
        public SkeletonGraphic SkeletonGraphic
        {
            get { return this.skeletonGraphic; }
            set
            {
                this.skeletonGraphic = value;
                this.Initialize();
            }
        }

        public bool initializeOnAwake = true;

        /// <summary>If a bone isn't set in code, boneName is used to find the bone at the beginning. For runtime switching by name, use SetBoneByName. You can also set the BoneFollower.bone field directly.</summary>
        [SpineBone(dataField: "skeletonGraphic")]
        public string boneName;

        public bool followBoneRotation = true;
        [Tooltip("Follows the skeleton's flip state by controlling this Transform's local scale.")]
        public bool followSkeletonFlip = true;
        [Tooltip("Follows the target bone's local scale. BoneFollower cannot inherit world/skewed scale because of UnityEngine.Transform property limitations.")]
        public bool followLocalScale = false;
        public bool followXYPosition = true;
        public bool followZPosition = true;
        [Tooltip("Applies when 'Follow Skeleton Flip' is disabled but 'Follow Bone Rotation' is enabled."
            + " When flipping the skeleton by scaling its Transform, this follower's rotation is adjusted"
            + " instead of its scale to follow the bone orientation. When one of the axes is flipped, "
            + " only one axis can be followed, either the X or the Y axis, which is selected here.")]
        public AxisOrientation maintainedAxisOrientation = AxisOrientation.XAxis;

        [System.NonSerialized] public Bone bone;

        private Transform skeletonTransform;
        private bool skeletonTransformIsParent;

        [System.NonSerialized] public bool valid;

        /// <summary>
        /// Sets the target bone by its bone name. Returns false if no bone was found.</summary>
        public bool SetBone(string name)
        {
            this.bone = this.skeletonGraphic.Skeleton.FindBone(name);
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

        public void Initialize()
        {
            this.bone = null;
            this.valid = this.skeletonGraphic != null && this.skeletonGraphic.IsValid;
            if (!this.valid) return;

            this.skeletonTransform = this.skeletonGraphic.transform;
            //			skeletonGraphic.OnRebuild -= HandleRebuildRenderer;
            //			skeletonGraphic.OnRebuild += HandleRebuildRenderer;
            this.skeletonTransformIsParent = Transform.ReferenceEquals(this.skeletonTransform, this.transform.parent);

            if (!string.IsNullOrEmpty(this.boneName))
                this.bone = this.skeletonGraphic.Skeleton.FindBone(this.boneName);

#if UNITY_EDITOR
            if (Application.isEditor)
            {
                this.LateUpdate();
            }
#endif
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
                this.bone = this.skeletonGraphic.Skeleton.FindBone(this.boneName);
                if (!this.SetBone(this.boneName)) return;
            }

            var thisTransform = this.transform as RectTransform;
            if (thisTransform == null) return;

            var canvas = this.skeletonGraphic.canvas;
            if (canvas == null) canvas = this.skeletonGraphic.GetComponentInParent<Canvas>();
            var scale = canvas != null ? canvas.referencePixelsPerUnit : 100.0f;

            float additionalFlipScale = 1;
            if (this.skeletonTransformIsParent)
            {
                // Recommended setup: Use local transform properties if Spine GameObject is the immediate parent
                thisTransform.localPosition = new Vector3(this.followXYPosition ? this.bone.worldX * scale : thisTransform.localPosition.x,
                                                        this.followXYPosition ? this.bone.worldY * scale : thisTransform.localPosition.y,
                                                        this.followZPosition ? 0f : thisTransform.localPosition.z);
                if (this.followBoneRotation) thisTransform.localRotation = this.bone.GetQuaternion();
            }
            else
            {
                // For special cases: Use transform world properties if transform relationship is complicated
                var targetWorldPosition = this.skeletonTransform.TransformPoint(new Vector3(this.bone.worldX * scale, this.bone.worldY * scale, 0f));
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
