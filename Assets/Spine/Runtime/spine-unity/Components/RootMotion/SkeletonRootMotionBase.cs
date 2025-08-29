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

using Spine.Unity.AnimationTools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spine.Unity
{

    /// <summary>
    /// Base class for skeleton root motion components.
    /// </summary>
    public abstract class SkeletonRootMotionBase : MonoBehaviour
    {

        #region Inspector
        [SpineBone]
        [SerializeField]
        protected string rootMotionBoneName = "root";
        public bool transformPositionX = true;
        public bool transformPositionY = true;

        public float rootMotionScaleX = 1;
        public float rootMotionScaleY = 1;
        /// <summary>Skeleton space X translation per skeleton space Y translation root motion.</summary>
        public float rootMotionTranslateXPerY = 0;
        /// <summary>Skeleton space Y translation per skeleton space X translation root motion.</summary>
        public float rootMotionTranslateYPerX = 0;

        [Header("Optional")]
        public Rigidbody2D rigidBody2D;
        public Rigidbody rigidBody;

        public bool UsesRigidbody
        {
            get { return this.rigidBody != null || this.rigidBody2D != null; }
        }
        #endregion

        protected ISkeletonComponent skeletonComponent;
        protected Bone rootMotionBone;
        protected int rootMotionBoneIndex;
        protected List<Bone> topLevelBones = new List<Bone>();
        protected Vector2 initialOffset = Vector2.zero;
        protected Vector2 tempSkeletonDisplacement;
        protected Vector2 rigidbodyDisplacement;

        protected virtual void Reset()
        {
            this.FindRigidbodyComponent();
        }

        protected virtual void Start()
        {
            this.skeletonComponent = this.GetComponent<ISkeletonComponent>();
            this.GatherTopLevelBones();
            this.SetRootMotionBone(this.rootMotionBoneName);
            if (this.rootMotionBone != null)
                this.initialOffset = new Vector2(this.rootMotionBone.x, this.rootMotionBone.y);

            var skeletonAnimation = this.skeletonComponent as ISkeletonAnimation;
            if (skeletonAnimation != null)
            {
                skeletonAnimation.UpdateLocal -= this.HandleUpdateLocal;
                skeletonAnimation.UpdateLocal += this.HandleUpdateLocal;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!this.isActiveAndEnabled)
                return; // Root motion is only applied when component is enabled.

            if (this.rigidBody2D != null)
            {
                this.rigidBody2D.MovePosition(new Vector2(this.transform.position.x, this.transform.position.y)
                    + this.rigidbodyDisplacement);
            }
            if (this.rigidBody != null)
            {
                this.rigidBody.MovePosition(this.transform.position
                    + new Vector3(this.rigidbodyDisplacement.x, this.rigidbodyDisplacement.y, 0));
            }
            this.GetScaleAffectingRootMotion(out var parentBoneScale);
            this.ClearEffectiveBoneOffsets(parentBoneScale);
            this.rigidbodyDisplacement = Vector2.zero;
            this.tempSkeletonDisplacement = Vector2.zero;
        }

        protected virtual void OnDisable()
        {
            this.rigidbodyDisplacement = Vector2.zero;
            this.tempSkeletonDisplacement = Vector2.zero;
        }

        protected void FindRigidbodyComponent()
        {
            this.rigidBody2D = this.GetComponent<Rigidbody2D>();
            if (!this.rigidBody2D)
                this.rigidBody = this.GetComponent<Rigidbody>();

            if (!this.rigidBody2D && !this.rigidBody)
            {
                this.rigidBody2D = this.GetComponentInParent<Rigidbody2D>();
                if (!this.rigidBody2D)
                    this.rigidBody = this.GetComponentInParent<Rigidbody>();
            }
        }

        protected virtual float AdditionalScale { get { return 1.0f; } }
        protected abstract Vector2 CalculateAnimationsMovementDelta();
        public abstract Vector2 GetRemainingRootMotion(int trackIndex = 0);

        public struct RootMotionInfo
        {
            public Vector2 start;
            public Vector2 current;
            public Vector2 mid;
            public Vector2 end;
            public bool timeIsPastMid;
        };
        public abstract RootMotionInfo GetRootMotionInfo(int trackIndex = 0);

        public void SetRootMotionBone(string name)
        {
            var skeleton = this.skeletonComponent.Skeleton;
            var index = skeleton.FindBoneIndex(name);
            if (index >= 0)
            {
                this.rootMotionBoneIndex = index;
                this.rootMotionBone = skeleton.bones.Items[index];
            }
            else
            {
                Debug.Log("Bone named \"" + name + "\" could not be found.");
                this.rootMotionBoneIndex = 0;
                this.rootMotionBone = skeleton.RootBone;
            }
        }

        public void AdjustRootMotionToDistance(Vector2 distanceToTarget, int trackIndex = 0, bool adjustX = true, bool adjustY = true,
            float minX = 0, float maxX = float.MaxValue, float minY = 0, float maxY = float.MaxValue,
            bool allowXTranslation = false, bool allowYTranslation = false)
        {

            var distanceToTargetSkeletonSpace = (Vector2)this.transform.InverseTransformVector(distanceToTarget);
            var scaleAffectingRootMotion = this.GetScaleAffectingRootMotion();
            if (this.UsesRigidbody)
                distanceToTargetSkeletonSpace -= this.tempSkeletonDisplacement;

            var remainingRootMotionSkeletonSpace = this.GetRemainingRootMotion(trackIndex);
            remainingRootMotionSkeletonSpace.Scale(scaleAffectingRootMotion);
            if (remainingRootMotionSkeletonSpace.x == 0)
                remainingRootMotionSkeletonSpace.x = 0.0001f;
            if (remainingRootMotionSkeletonSpace.y == 0)
                remainingRootMotionSkeletonSpace.y = 0.0001f;

            if (adjustX)
                this.rootMotionScaleX = Math.Min(maxX, Math.Max(minX, distanceToTargetSkeletonSpace.x / remainingRootMotionSkeletonSpace.x));
            if (adjustY)
                this.rootMotionScaleY = Math.Min(maxY, Math.Max(minY, distanceToTargetSkeletonSpace.y / remainingRootMotionSkeletonSpace.y));

            if (allowXTranslation)
                this.rootMotionTranslateXPerY = (distanceToTargetSkeletonSpace.x - remainingRootMotionSkeletonSpace.x * this.rootMotionScaleX) / remainingRootMotionSkeletonSpace.y;
            if (allowYTranslation)
                this.rootMotionTranslateYPerX = (distanceToTargetSkeletonSpace.y - remainingRootMotionSkeletonSpace.y * this.rootMotionScaleY) / remainingRootMotionSkeletonSpace.x;
        }

        public Vector2 GetAnimationRootMotion(Animation animation)
        {
            return this.GetAnimationRootMotion(0, animation.duration, animation);
        }

        public Vector2 GetAnimationRootMotion(float startTime, float endTime,
            Animation animation)
        {

            var timeline = animation.FindTranslateTimelineForBone(this.rootMotionBoneIndex);
            if (timeline != null)
            {
                return this.GetTimelineMovementDelta(startTime, endTime, timeline, animation);
            }
            return Vector2.zero;
        }

        public RootMotionInfo GetAnimationRootMotionInfo(Animation animation, float currentTime)
        {
            var rootMotion = new RootMotionInfo();
            var timeline = animation.FindTranslateTimelineForBone(this.rootMotionBoneIndex);
            if (timeline != null)
            {
                var duration = animation.duration;
                var mid = duration * 0.5f;
                rootMotion.start = timeline.Evaluate(0);
                rootMotion.current = timeline.Evaluate(currentTime);
                rootMotion.mid = timeline.Evaluate(mid);
                rootMotion.end = timeline.Evaluate(duration);
                rootMotion.timeIsPastMid = currentTime > mid;
            }
            return rootMotion;
        }

        private Vector2 GetTimelineMovementDelta(float startTime, float endTime,
            TranslateTimeline timeline, Animation animation)
        {

            Vector2 currentDelta;
            if (startTime > endTime) // Looped
                currentDelta = (timeline.Evaluate(animation.duration) - timeline.Evaluate(startTime))
                    + (timeline.Evaluate(endTime) - timeline.Evaluate(0));
            else if (startTime != endTime) // Non-looped
                currentDelta = timeline.Evaluate(endTime) - timeline.Evaluate(startTime);
            else
                currentDelta = Vector2.zero;
            return currentDelta;
        }

        private void GatherTopLevelBones()
        {
            this.topLevelBones.Clear();
            var skeleton = this.skeletonComponent.Skeleton;
            foreach (var bone in skeleton.Bones)
            {
                if (bone.Parent == null)
                    this.topLevelBones.Add(bone);
            }
        }

        private void HandleUpdateLocal(ISkeletonAnimation animatedSkeletonComponent)
        {
            if (!this.isActiveAndEnabled)
                return; // Root motion is only applied when component is enabled.

            var boneLocalDelta = this.CalculateAnimationsMovementDelta();
            var skeletonDelta = this.GetSkeletonSpaceMovementDelta(boneLocalDelta, out var parentBoneScale);
            this.ApplyRootMotion(skeletonDelta, parentBoneScale);
        }

        private void ApplyRootMotion(Vector2 skeletonDelta, Vector2 parentBoneScale)
        {
            // Apply root motion to Transform or RigidBody;
            if (this.UsesRigidbody)
            {
                this.rigidbodyDisplacement += (Vector2)this.transform.TransformVector(skeletonDelta);

                // Accumulated displacement is applied on the next Physics update in FixedUpdate.
                // Until the next Physics update, tempBoneDisplacement is offsetting bone locations
                // to prevent stutter which would otherwise occur if we don't move every Update.
                this.tempSkeletonDisplacement += skeletonDelta;
                this.SetEffectiveBoneOffsetsTo(this.tempSkeletonDisplacement, parentBoneScale);
            }
            else
            {
                this.transform.position += this.transform.TransformVector(skeletonDelta);
                this.ClearEffectiveBoneOffsets(parentBoneScale);
            }
        }

        private Vector2 GetScaleAffectingRootMotion()
        {
            return this.GetScaleAffectingRootMotion(out var parentBoneScale);
        }

        private Vector2 GetScaleAffectingRootMotion(out Vector2 parentBoneScale)
        {
            var skeleton = this.skeletonComponent.Skeleton;
            var totalScale = Vector2.one;
            totalScale.x *= skeleton.ScaleX;
            totalScale.y *= skeleton.ScaleY;

            parentBoneScale = Vector2.one;
            var scaleBone = this.rootMotionBone;
            while ((scaleBone = scaleBone.parent) != null)
            {
                parentBoneScale.x *= scaleBone.ScaleX;
                parentBoneScale.y *= scaleBone.ScaleY;
            }
            totalScale = Vector2.Scale(totalScale, parentBoneScale);
            totalScale *= this.AdditionalScale;
            return totalScale;
        }

        private Vector2 GetSkeletonSpaceMovementDelta(Vector2 boneLocalDelta, out Vector2 parentBoneScale)
        {
            var skeletonDelta = boneLocalDelta;
            var totalScale = this.GetScaleAffectingRootMotion(out parentBoneScale);
            skeletonDelta.Scale(totalScale);

            var rootMotionTranslation = new Vector2(
                this.rootMotionTranslateXPerY * skeletonDelta.y,
                this.rootMotionTranslateYPerX * skeletonDelta.x);

            skeletonDelta.x *= this.rootMotionScaleX;
            skeletonDelta.y *= this.rootMotionScaleY;
            skeletonDelta.x += rootMotionTranslation.x;
            skeletonDelta.y += rootMotionTranslation.y;

            if (!this.transformPositionX) skeletonDelta.x = 0f;
            if (!this.transformPositionY) skeletonDelta.y = 0f;
            return skeletonDelta;
        }

        private void SetEffectiveBoneOffsetsTo(Vector2 displacementSkeletonSpace, Vector2 parentBoneScale)
        {
            // Move top level bones in opposite direction of the root motion bone
            var skeleton = this.skeletonComponent.Skeleton;
            foreach (var topLevelBone in this.topLevelBones)
            {
                if (topLevelBone == this.rootMotionBone)
                {
                    if (this.transformPositionX) topLevelBone.x = displacementSkeletonSpace.x / skeleton.ScaleX;
                    if (this.transformPositionY) topLevelBone.y = displacementSkeletonSpace.y / skeleton.ScaleY;
                }
                else
                {
                    var offsetX = (this.initialOffset.x - this.rootMotionBone.x) * parentBoneScale.x;
                    var offsetY = (this.initialOffset.y - this.rootMotionBone.y) * parentBoneScale.y;
                    if (this.transformPositionX) topLevelBone.x = (displacementSkeletonSpace.x / skeleton.ScaleX) + offsetX;
                    if (this.transformPositionY) topLevelBone.y = (displacementSkeletonSpace.y / skeleton.ScaleY) + offsetY;
                }
            }
        }

        private void ClearEffectiveBoneOffsets(Vector2 parentBoneScale)
        {
            this.SetEffectiveBoneOffsetsTo(Vector2.zero, parentBoneScale);
        }
    }
}
