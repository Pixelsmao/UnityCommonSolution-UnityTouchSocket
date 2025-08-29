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
    [AddComponentMenu("Spine/SkeletonAnimation")]
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonAnimation-Component")]
    public class SkeletonAnimation : SkeletonRenderer, ISkeletonAnimation, IAnimationStateComponent
    {

        #region IAnimationStateComponent
        /// <summary>
        /// This is the Spine.AnimationState object of this SkeletonAnimation. You can control animations through it.
        /// Note that this object, like .skeleton, is not guaranteed to exist in Awake. Do all accesses and caching to it in Start</summary>
        public Spine.AnimationState state;
        /// <summary>
        /// This is the Spine.AnimationState object of this SkeletonAnimation. You can control animations through it.
        /// Note that this object, like .skeleton, is not guaranteed to exist in Awake. Do all accesses and caching to it in Start</summary>
        public Spine.AnimationState AnimationState
        {
            get
            {
                this.Initialize(false);
                return this.state;
            }
        }
        private bool wasUpdatedAfterInit = true;
        #endregion

        #region Bone Callbacks ISkeletonAnimation
        protected event UpdateBonesDelegate _BeforeApply;
        protected event UpdateBonesDelegate _UpdateLocal;
        protected event UpdateBonesDelegate _UpdateWorld;
        protected event UpdateBonesDelegate _UpdateComplete;

        /// <summary>
        /// Occurs before the animations are applied.
        /// Use this callback when you want to change the skeleton state before animations are applied on top.
        /// </summary>
        public event UpdateBonesDelegate BeforeApply { add { _BeforeApply += value; } remove { _BeforeApply -= value; } }

        /// <summary>
        /// Occurs after the animations are applied and before world space values are resolved.
        /// Use this callback when you want to set bone local values.
        /// </summary>
        public event UpdateBonesDelegate UpdateLocal { add { _UpdateLocal += value; } remove { _UpdateLocal -= value; } }

        /// <summary>
        /// Occurs after the Skeleton's bone world space values are resolved (including all constraints).
        /// Using this callback will cause the world space values to be solved an extra time.
        /// Use this callback if want to use bone world space values, and also set bone local values.</summary>
        public event UpdateBonesDelegate UpdateWorld { add { _UpdateWorld += value; } remove { _UpdateWorld -= value; } }

        /// <summary>
        /// Occurs after the Skeleton's bone world space values are resolved (including all constraints).
        /// Use this callback if you want to use bone world space values, but don't intend to modify bone local values.
        /// This callback can also be used when setting world position and the bone matrix.</summary>
        public event UpdateBonesDelegate UpdateComplete { add { _UpdateComplete += value; } remove { _UpdateComplete -= value; } }
        #endregion

        #region Serialized state and Beginner API
        [SerializeField]
        [SpineAnimation]
        private string _animationName;

        /// <summary>
        /// Setting this property sets the animation of the skeleton. If invalid, it will store the animation name for the next time the skeleton is properly initialized.
        /// Getting this property gets the name of the currently playing animation. If invalid, it will return the last stored animation name set through this property.</summary>
        public string AnimationName
        {
            get
            {
                if (!this.valid)
                {
                    return this._animationName;
                }
                else
                {
                    var entry = this.state.GetCurrent(0);
                    return entry == null ? null : entry.Animation.Name;
                }
            }
            set
            {
                this.Initialize(false);
                if (this._animationName == value)
                {
                    var entry = this.state.GetCurrent(0);
                    if (entry != null && entry.loop == this.loop)
                        return;
                }
                this._animationName = value;

                if (string.IsNullOrEmpty(value))
                {
                    this.state.ClearTrack(0);
                }
                else
                {
                    var animationObject = this.skeletonDataAsset.GetSkeletonData(false).FindAnimation(value);
                    if (animationObject != null)
                        this.state.SetAnimation(0, animationObject, this.loop);
                }
            }
        }

        /// <summary>Whether or not <see cref="AnimationName"/> should loop. This only applies to the initial animation specified in the inspector, or any subsequent Animations played through .AnimationName. Animations set through state.SetAnimation are unaffected.</summary>
        public bool loop;

        /// <summary>
        /// The rate at which animations progress over time. 1 means 100%. 0.5 means 50%.</summary>
        /// <remarks>AnimationState and TrackEntry also have their own timeScale. These are combined multiplicatively.</remarks>
        public float timeScale = 1;
        #endregion

        #region Runtime Instantiation
        /// <summary>Adds and prepares a SkeletonAnimation component to a GameObject at runtime.</summary>
        /// <returns>The newly instantiated SkeletonAnimation</returns>
        public static SkeletonAnimation AddToGameObject(GameObject gameObject, SkeletonDataAsset skeletonDataAsset,
            bool quiet = false)
        {
            return SkeletonRenderer.AddSpineComponent<SkeletonAnimation>(gameObject, skeletonDataAsset, quiet);
        }

        /// <summary>Instantiates a new UnityEngine.GameObject and adds a prepared SkeletonAnimation component to it.</summary>
        /// <returns>The newly instantiated SkeletonAnimation component.</returns>
        public static SkeletonAnimation NewSkeletonAnimationGameObject(SkeletonDataAsset skeletonDataAsset,
            bool quiet = false)
        {
            return SkeletonRenderer.NewSpineGameObject<SkeletonAnimation>(skeletonDataAsset, quiet);
        }
        #endregion

        /// <summary>
        /// Clears the previously generated mesh, resets the skeleton's pose, and clears all previously active animations.</summary>
        public override void ClearState()
        {
            base.ClearState();
            if (this.state != null) this.state.ClearTracks();
        }

        /// <summary>
        /// Initialize this component. Attempts to load the SkeletonData and creates the internal Spine objects and buffers.</summary>
        /// <param name="overwrite">If set to <c>true</c>, force overwrite an already initialized object.</param>
        public override void Initialize(bool overwrite, bool quiet = false)
        {
            if (this.valid && !overwrite)
                return;
            base.Initialize(overwrite, quiet);

            if (!this.valid)
                return;
            this.state = new Spine.AnimationState(this.skeletonDataAsset.GetAnimationStateData());
            this.wasUpdatedAfterInit = false;

            if (!string.IsNullOrEmpty(this._animationName))
            {
                var animationObject = this.skeletonDataAsset.GetSkeletonData(false).FindAnimation(this._animationName);
                if (animationObject != null)
                {
                    this.state.SetAnimation(0, animationObject, this.loop);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        this.Update(0f);
#endif
                }
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                this.Update(0f);
                return;
            }
#endif

            this.Update(Time.deltaTime);
        }

        /// <summary>Progresses the AnimationState according to the given deltaTime, and applies it to the Skeleton. Use Time.deltaTime to update manually. Use deltaTime 0 to update without progressing the time.</summary>
        public void Update(float deltaTime)
        {
            if (!this.valid || this.state == null)
                return;

            this.wasUpdatedAfterInit = true;
            if (this.updateMode < UpdateMode.OnlyAnimationStatus)
                return;
            this.UpdateAnimationStatus(deltaTime);

            if (this.updateMode == UpdateMode.OnlyAnimationStatus)
                return;
            this.ApplyAnimation();
        }

        protected void UpdateAnimationStatus(float deltaTime)
        {
            deltaTime *= this.timeScale;
            this.skeleton.Update(deltaTime);
            this.state.Update(deltaTime);
        }

        protected void ApplyAnimation()
        {
            if (_BeforeApply != null)
                _BeforeApply(this);

            if (this.updateMode != UpdateMode.OnlyEventTimelines)
                this.state.Apply(this.skeleton);
            else
                this.state.ApplyEventTimelinesOnly(this.skeleton);

            if (_UpdateLocal != null)
                _UpdateLocal(this);

            this.skeleton.UpdateWorldTransform();

            if (_UpdateWorld != null)
            {
                _UpdateWorld(this);
                this.skeleton.UpdateWorldTransform();
            }

            if (_UpdateComplete != null)
            {
                _UpdateComplete(this);
            }
        }

        public override void LateUpdate()
        {
            // instantiation can happen from Update() after this component, leading to a missing Update() call.
            if (!this.wasUpdatedAfterInit) this.Update(0);
            base.LateUpdate();
        }
    }

}
