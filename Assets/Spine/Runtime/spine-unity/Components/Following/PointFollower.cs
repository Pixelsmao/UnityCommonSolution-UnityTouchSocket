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

#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [AddComponentMenu("Spine/Point Follower")]
    [HelpURL("http://esotericsoftware.com/spine-unity#PointFollower")]
    public class PointFollower : MonoBehaviour, IHasSkeletonRenderer, IHasSkeletonComponent
    {

        public SkeletonRenderer skeletonRenderer;
        public SkeletonRenderer SkeletonRenderer { get { return this.skeletonRenderer; } }
        public ISkeletonComponent SkeletonComponent { get { return this.skeletonRenderer; } }

        [SpineSlot(dataField: "skeletonRenderer", includeNone: true)]
        public string slotName;

        [SpineAttachment(slotField: "slotName", dataField: "skeletonRenderer", fallbackToTextField: true, includeNone: true)]
        public string pointAttachmentName;

        public bool followRotation = true;
        public bool followSkeletonFlip = true;
        public bool followSkeletonZPosition = false;

        private Transform skeletonTransform;
        private bool skeletonTransformIsParent;
        private PointAttachment point;
        private Bone bone;
        private bool valid;
        public bool IsValid { get { return this.valid; } }

        public void Initialize()
        {
            this.valid = this.skeletonRenderer != null && this.skeletonRenderer.valid;
            if (!this.valid)
                return;

            this.UpdateReferences();

#if UNITY_EDITOR
            if (Application.isEditor) this.LateUpdate();
#endif
        }

        private void HandleRebuildRenderer(SkeletonRenderer skeletonRenderer)
        {
            this.Initialize();
        }

        private void UpdateReferences()
        {
            this.skeletonTransform = this.skeletonRenderer.transform;
            this.skeletonRenderer.OnRebuild -= this.HandleRebuildRenderer;
            this.skeletonRenderer.OnRebuild += this.HandleRebuildRenderer;
            this.skeletonTransformIsParent = Transform.ReferenceEquals(this.skeletonTransform, this.transform.parent);

            this.bone = null;
            this.point = null;
            if (!string.IsNullOrEmpty(this.pointAttachmentName))
            {
                var skeleton = this.skeletonRenderer.Skeleton;

                var slotIndex = skeleton.FindSlotIndex(this.slotName);
                if (slotIndex >= 0)
                {
                    var slot = skeleton.slots.Items[slotIndex];
                    this.bone = slot.bone;
                    this.point = skeleton.GetAttachment(slotIndex, this.pointAttachmentName) as PointAttachment;
                }
            }
        }

        private void OnDestroy()
        {
            if (this.skeletonRenderer != null)
                this.skeletonRenderer.OnRebuild -= this.HandleRebuildRenderer;
        }

        public void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) this.skeletonTransformIsParent = Transform.ReferenceEquals(this.skeletonTransform, this.transform.parent);
#endif

            if (this.point == null)
            {
                if (string.IsNullOrEmpty(this.pointAttachmentName)) return;
                this.UpdateReferences();
                if (this.point == null) return;
            }

            Vector2 worldPos;
            this.point.ComputeWorldPosition(this.bone, out worldPos.x, out worldPos.y);
            var rotation = this.point.ComputeWorldRotation(this.bone);

            var thisTransform = this.transform;
            if (this.skeletonTransformIsParent)
            {
                // Recommended setup: Use local transform properties if Spine GameObject is the immediate parent
                thisTransform.localPosition = new Vector3(worldPos.x, worldPos.y, this.followSkeletonZPosition ? 0f : thisTransform.localPosition.z);
                if (this.followRotation)
                {
                    var halfRotation = rotation * 0.5f * Mathf.Deg2Rad;

                    var q = default(Quaternion);
                    q.z = Mathf.Sin(halfRotation);
                    q.w = Mathf.Cos(halfRotation);
                    thisTransform.localRotation = q;
                }
            }
            else
            {
                // For special cases: Use transform world properties if transform relationship is complicated
                var targetWorldPosition = this.skeletonTransform.TransformPoint(new Vector3(worldPos.x, worldPos.y, 0f));
                if (!this.followSkeletonZPosition)
                    targetWorldPosition.z = thisTransform.position.z;

                var transformParent = thisTransform.parent;
                if (transformParent != null)
                {
                    var m = transformParent.localToWorldMatrix;
                    if (m.m00 * m.m11 - m.m01 * m.m10 < 0) // Determinant2D is negative
                        rotation = -rotation;
                }

                if (this.followRotation)
                {
                    var transformWorldRotation = this.skeletonTransform.rotation.eulerAngles;
                    thisTransform.SetPositionAndRotation(targetWorldPosition, Quaternion.Euler(transformWorldRotation.x, transformWorldRotation.y, transformWorldRotation.z + rotation));
                }
                else
                {
                    thisTransform.position = targetWorldPosition;
                }
            }

            if (this.followSkeletonFlip)
            {
                var localScale = thisTransform.localScale;
                localScale.y = Mathf.Abs(localScale.y) * Mathf.Sign(this.bone.skeleton.ScaleX * this.bone.skeleton.ScaleY);
                thisTransform.localScale = localScale;
            }
        }
    }
}
