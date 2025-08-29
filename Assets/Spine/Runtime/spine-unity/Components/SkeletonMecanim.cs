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

using System.Collections.Generic;
using UnityEngine;

namespace Spine.Unity
{
    [RequireComponent(typeof(Animator))]
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonMecanim-Component")]
    public class SkeletonMecanim : SkeletonRenderer, ISkeletonAnimation
    {

        [SerializeField] protected MecanimTranslator translator;
        public MecanimTranslator Translator { get { return this.translator; } }
        private bool wasUpdatedAfterInit = true;

        #region Bone Callbacks (ISkeletonAnimation)
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
        /// Use this callback when you want to set bone local values.</summary>
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

        public override void Initialize(bool overwrite, bool quiet = false)
        {
            if (this.valid && !overwrite)
                return;

            base.Initialize(overwrite, quiet);

            if (!this.valid)
                return;

            if (this.translator == null) this.translator = new MecanimTranslator();
            this.translator.Initialize(this.GetComponent<Animator>(), this.skeletonDataAsset);
            this.wasUpdatedAfterInit = false;
        }

        public void Update()
        {
            if (!this.valid) return;

            this.wasUpdatedAfterInit = true;
            // animation status is kept by Mecanim Animator component
            if (this.updateMode <= UpdateMode.OnlyAnimationStatus)
                return;
            this.ApplyAnimation();
        }

        protected void ApplyAnimation()
        {
            if (_BeforeApply != null)
                _BeforeApply(this);

#if UNITY_EDITOR
            var translatorAnimator = this.translator.Animator;
            if (translatorAnimator != null && !translatorAnimator.isInitialized)
                translatorAnimator.Rebind();

            if (Application.isPlaying)
            {
                this.translator.Apply(this.skeleton);
            }
            else
            {
                if (translatorAnimator != null && translatorAnimator.isInitialized &&
                    translatorAnimator.isActiveAndEnabled && translatorAnimator.runtimeAnimatorController != null)
                {
                    // Note: Rebind is required to prevent warning "Animator is not playing an AnimatorController" with prefabs
                    translatorAnimator.Rebind();
                    this.translator.Apply(this.skeleton);
                }
            }
#else
			translator.Apply(skeleton);
#endif

            // UpdateWorldTransform and Bone Callbacks
            {
                if (_UpdateLocal != null)
                    _UpdateLocal(this);

                this.skeleton.UpdateWorldTransform();

                if (_UpdateWorld != null)
                {
                    _UpdateWorld(this);
                    this.skeleton.UpdateWorldTransform();
                }

                if (_UpdateComplete != null)
                    _UpdateComplete(this);
            }
        }

        public override void LateUpdate()
        {
            // instantiation can happen from Update() after this component, leading to a missing Update() call.
            if (!this.wasUpdatedAfterInit) this.Update();
            base.LateUpdate();
        }

        [System.Serializable]
        public class MecanimTranslator
        {

            private const float WeightEpsilon = 0.0001f;

            #region Inspector
            public bool autoReset = true;
            public bool useCustomMixMode = true;
            public MixMode[] layerMixModes = new MixMode[0];
            public MixBlend[] layerBlendModes = new MixBlend[0];
            #endregion

            public delegate void OnClipAppliedDelegate(Spine.Animation clip, int layerIndex, float weight,
                float time, float lastTime, bool playsBackward);
            protected event OnClipAppliedDelegate _OnClipApplied;

            public event OnClipAppliedDelegate OnClipApplied { add { _OnClipApplied += value; } remove { _OnClipApplied -= value; } }

            public enum MixMode { AlwaysMix, MixNext, Hard }

            private readonly Dictionary<int, Spine.Animation> animationTable = new Dictionary<int, Spine.Animation>(IntEqualityComparer.Instance);
            private readonly Dictionary<AnimationClip, int> clipNameHashCodeTable = new Dictionary<AnimationClip, int>(AnimationClipEqualityComparer.Instance);
            private readonly List<Animation> previousAnimations = new List<Animation>();

            protected class ClipInfos
            {
                public bool isInterruptionActive = false;
                public bool isLastFrameOfInterruption = false;

                public int clipInfoCount = 0;
                public int nextClipInfoCount = 0;
                public int interruptingClipInfoCount = 0;
                public readonly List<AnimatorClipInfo> clipInfos = new List<AnimatorClipInfo>();
                public readonly List<AnimatorClipInfo> nextClipInfos = new List<AnimatorClipInfo>();
                public readonly List<AnimatorClipInfo> interruptingClipInfos = new List<AnimatorClipInfo>();

                public AnimatorStateInfo stateInfo;
                public AnimatorStateInfo nextStateInfo;
                public AnimatorStateInfo interruptingStateInfo;

                public float interruptingClipTimeAddition = 0;
            }
            protected ClipInfos[] layerClipInfos = new ClipInfos[0];

            private Animator animator;
            public Animator Animator { get { return this.animator; } }

            public int MecanimLayerCount
            {
                get
                {
                    if (!this.animator)
                        return 0;
                    return this.animator.layerCount;
                }
            }

            public string[] MecanimLayerNames
            {
                get
                {
                    if (!this.animator)
                        return new string[0];
                    var layerNames = new string[this.animator.layerCount];
                    for (var i = 0; i < this.animator.layerCount; ++i)
                    {
                        layerNames[i] = this.animator.GetLayerName(i);
                    }
                    return layerNames;
                }
            }

            public void Initialize(Animator animator, SkeletonDataAsset skeletonDataAsset)
            {
                this.animator = animator;

                this.previousAnimations.Clear();

                this.animationTable.Clear();
                var data = skeletonDataAsset.GetSkeletonData(true);
                foreach (var a in data.Animations)
                    this.animationTable.Add(a.Name.GetHashCode(), a);

                this.clipNameHashCodeTable.Clear();
                this.ClearClipInfosForLayers();
            }

            private bool ApplyAnimation(Skeleton skeleton, AnimatorClipInfo info, AnimatorStateInfo stateInfo,
                                        int layerIndex, float layerWeight, MixBlend layerBlendMode, bool useClipWeight1 = false)
            {
                var weight = info.weight * layerWeight;
                if (weight < WeightEpsilon)
                    return false;

                var clip = this.GetAnimation(info.clip);
                if (clip == null)
                    return false;

                var time = AnimationTime(stateInfo.normalizedTime, info.clip.length,
                                        info.clip.isLooping, stateInfo.speed < 0);
                weight = useClipWeight1 ? layerWeight : weight;
                clip.Apply(skeleton, 0, time, info.clip.isLooping, null,
                        weight, layerBlendMode, MixDirection.In);
                if (_OnClipApplied != null)
                    this.OnClipAppliedCallback(clip, stateInfo, layerIndex, time, info.clip.isLooping, weight);
                return true;
            }

            private bool ApplyInterruptionAnimation(Skeleton skeleton,
                bool interpolateWeightTo1, AnimatorClipInfo info, AnimatorStateInfo stateInfo,
                int layerIndex, float layerWeight, MixBlend layerBlendMode, float interruptingClipTimeAddition,
                bool useClipWeight1 = false)
            {

                var clipWeight = interpolateWeightTo1 ? (info.weight + 1.0f) * 0.5f : info.weight;
                var weight = clipWeight * layerWeight;
                if (weight < WeightEpsilon)
                    return false;

                var clip = this.GetAnimation(info.clip);
                if (clip == null)
                    return false;

                var time = AnimationTime(stateInfo.normalizedTime + interruptingClipTimeAddition,
                                        info.clip.length, stateInfo.speed < 0);
                weight = useClipWeight1 ? layerWeight : weight;
                clip.Apply(skeleton, 0, time, info.clip.isLooping, null,
                            weight, layerBlendMode, MixDirection.In);
                if (_OnClipApplied != null)
                {
                    this.OnClipAppliedCallback(clip, stateInfo, layerIndex, time, info.clip.isLooping, weight);
                }
                return true;
            }

            private void OnClipAppliedCallback(Spine.Animation clip, AnimatorStateInfo stateInfo,
                int layerIndex, float time, bool isLooping, float weight)
            {

                var speedFactor = stateInfo.speedMultiplier * stateInfo.speed;
                var lastTime = time - (Time.deltaTime * speedFactor);
                if (isLooping && clip.duration != 0)
                {
                    time %= clip.duration;
                    lastTime %= clip.duration;
                }
                _OnClipApplied(clip, layerIndex, weight, time, lastTime, speedFactor < 0);
            }

            public void Apply(Skeleton skeleton)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    this.GetLayerBlendModes();
                }
#endif

                if (this.layerMixModes.Length < this.animator.layerCount)
                {
                    var oldSize = this.layerMixModes.Length;
                    System.Array.Resize<MixMode>(ref this.layerMixModes, this.animator.layerCount);
                    for (var layer = oldSize; layer < this.animator.layerCount; ++layer)
                    {
                        var isAdditiveLayer = false;
                        if (layer < this.layerBlendModes.Length)
                            isAdditiveLayer = this.layerBlendModes[layer] == MixBlend.Add;
                        this.layerMixModes[layer] = isAdditiveLayer ? MixMode.AlwaysMix : MixMode.MixNext;
                    }
                }

                this.InitClipInfosForLayers();
                for (int layer = 0, n = this.animator.layerCount; layer < n; layer++)
                {
                    this.GetStateUpdatesFromAnimator(layer);
                }

                // Clear Previous
                if (this.autoReset)
                {
                    var previousAnimations = this.previousAnimations;
                    for (int i = 0, n = previousAnimations.Count; i < n; i++)
                        previousAnimations[i].SetKeyedItemsToSetupPose(skeleton);

                    previousAnimations.Clear();
                    for (int layer = 0, n = this.animator.layerCount; layer < n; layer++)
                    {
                        var layerWeight = (layer == 0) ? 1 : this.animator.GetLayerWeight(layer); // Animator.GetLayerWeight always returns 0 on the first layer. Should be interpreted as 1.
                        if (layerWeight <= 0) continue;

                        var nextStateInfo = this.animator.GetNextAnimatorStateInfo(layer);

                        var hasNext = nextStateInfo.fullPathHash != 0;

                        this.GetAnimatorClipInfos(layer, out var isInterruptionActive, out var clipInfoCount, out var nextClipInfoCount, out var interruptingClipInfoCount,
                                            out var clipInfo, out var nextClipInfo, out var interruptingClipInfo, out var shallInterpolateWeightTo1);

                        for (var c = 0; c < clipInfoCount; c++)
                        {
                            var info = clipInfo[c];
                            var weight = info.weight * layerWeight; if (weight < WeightEpsilon) continue;
                            var clip = this.GetAnimation(info.clip);
                            if (clip != null)
                                previousAnimations.Add(clip);
                        }

                        if (hasNext)
                        {
                            for (var c = 0; c < nextClipInfoCount; c++)
                            {
                                var info = nextClipInfo[c];
                                var weight = info.weight * layerWeight; if (weight < WeightEpsilon) continue;
                                var clip = this.GetAnimation(info.clip);
                                if (clip != null)
                                    previousAnimations.Add(clip);
                            }
                        }

                        if (isInterruptionActive)
                        {
                            for (var c = 0; c < interruptingClipInfoCount; c++)
                            {
                                var info = interruptingClipInfo[c];
                                var clipWeight = shallInterpolateWeightTo1 ? (info.weight + 1.0f) * 0.5f : info.weight;
                                var weight = clipWeight * layerWeight; if (weight < WeightEpsilon) continue;
                                var clip = this.GetAnimation(info.clip);
                                if (clip != null)
                                    previousAnimations.Add(clip);
                            }
                        }
                    }
                }

                // Apply
                for (int layer = 0, n = this.animator.layerCount; layer < n; layer++)
                {
                    var layerWeight = (layer == 0) ? 1 : this.animator.GetLayerWeight(layer); // Animator.GetLayerWeight always returns 0 on the first layer. Should be interpreted as 1.

                    this.GetAnimatorStateInfos(layer, out var isInterruptionActive, out var stateInfo, out var nextStateInfo, out var interruptingStateInfo, out var interruptingClipTimeAddition);

                    var hasNext = nextStateInfo.fullPathHash != 0;

                    this.GetAnimatorClipInfos(layer, out isInterruptionActive, out var clipInfoCount, out var nextClipInfoCount, out var interruptingClipInfoCount,
                                        out var clipInfo, out var nextClipInfo, out var interruptingClipInfo, out var interpolateWeightTo1);

                    var layerBlendMode = (layer < this.layerBlendModes.Length) ? this.layerBlendModes[layer] : MixBlend.Replace;
                    var mode = this.GetMixMode(layer, layerBlendMode);
                    if (mode == MixMode.AlwaysMix)
                    {
                        // Always use Mix instead of Applying the first non-zero weighted clip.
                        for (var c = 0; c < clipInfoCount; c++)
                        {
                            this.ApplyAnimation(skeleton, clipInfo[c], stateInfo, layer, layerWeight, layerBlendMode);
                        }
                        if (hasNext)
                        {
                            for (var c = 0; c < nextClipInfoCount; c++)
                            {
                                this.ApplyAnimation(skeleton, nextClipInfo[c], nextStateInfo, layer, layerWeight, layerBlendMode);
                            }
                        }
                        if (isInterruptionActive)
                        {
                            for (var c = 0; c < interruptingClipInfoCount; c++)
                            {
                                this.ApplyInterruptionAnimation(skeleton, interpolateWeightTo1,
                                    interruptingClipInfo[c], interruptingStateInfo,
                                    layer, layerWeight, layerBlendMode, interruptingClipTimeAddition);
                            }
                        }
                    }
                    else
                    { // case MixNext || Hard
                      // Apply first non-zero weighted clip
                        var c = 0;
                        for (; c < clipInfoCount; c++)
                        {
                            if (!this.ApplyAnimation(skeleton, clipInfo[c], stateInfo, layer, layerWeight, layerBlendMode, useClipWeight1: true))
                                continue;
                            ++c; break;
                        }
                        // Mix the rest
                        for (; c < clipInfoCount; c++)
                        {
                            this.ApplyAnimation(skeleton, clipInfo[c], stateInfo, layer, layerWeight, layerBlendMode);
                        }

                        c = 0;
                        if (hasNext)
                        {
                            // Apply next clip directly instead of mixing (ie: no crossfade, ignores mecanim transition weights)
                            if (mode == MixMode.Hard)
                            {
                                for (; c < nextClipInfoCount; c++)
                                {
                                    if (!this.ApplyAnimation(skeleton, nextClipInfo[c], nextStateInfo, layer, layerWeight, layerBlendMode, useClipWeight1: true))
                                        continue;
                                    ++c; break;
                                }
                            }
                            // Mix the rest
                            for (; c < nextClipInfoCount; c++)
                            {
                                if (!this.ApplyAnimation(skeleton, nextClipInfo[c], nextStateInfo, layer, layerWeight, layerBlendMode))
                                    continue;
                            }
                        }

                        c = 0;
                        if (isInterruptionActive)
                        {
                            // Apply next clip directly instead of mixing (ie: no crossfade, ignores mecanim transition weights)
                            if (mode == MixMode.Hard)
                            {
                                for (; c < interruptingClipInfoCount; c++)
                                {
                                    if (this.ApplyInterruptionAnimation(skeleton, interpolateWeightTo1,
                                        interruptingClipInfo[c], interruptingStateInfo,
                                        layer, layerWeight, layerBlendMode, interruptingClipTimeAddition, useClipWeight1: true))
                                    {

                                        ++c; break;
                                    }
                                }
                            }
                            // Mix the rest
                            for (; c < interruptingClipInfoCount; c++)
                            {
                                this.ApplyInterruptionAnimation(skeleton, interpolateWeightTo1,
                                    interruptingClipInfo[c], interruptingStateInfo,
                                    layer, layerWeight, layerBlendMode, interruptingClipTimeAddition);
                            }
                        }
                    }
                }
            }

            public KeyValuePair<Spine.Animation, float> GetActiveAnimationAndTime(int layer)
            {
                if (layer >= this.layerClipInfos.Length)
                    return new KeyValuePair<Spine.Animation, float>(null, 0);

                var layerInfos = this.layerClipInfos[layer];
                var isInterruptionActive = layerInfos.isInterruptionActive;
                AnimationClip clip = null;
                Spine.Animation animation = null;
                AnimatorStateInfo stateInfo;
                if (isInterruptionActive && layerInfos.interruptingClipInfoCount > 0)
                {
                    clip = layerInfos.interruptingClipInfos[0].clip;
                    stateInfo = layerInfos.interruptingStateInfo;
                }
                else
                {
                    clip = layerInfos.clipInfos[0].clip;
                    stateInfo = layerInfos.stateInfo;
                }
                animation = this.GetAnimation(clip);
                var time = AnimationTime(stateInfo.normalizedTime, clip.length,
                                        clip.isLooping, stateInfo.speed < 0);
                return new KeyValuePair<Animation, float>(animation, time);
            }

            private static float AnimationTime(float normalizedTime, float clipLength, bool loop, bool reversed)
            {
                var time = AnimationTime(normalizedTime, clipLength, reversed);
                if (loop) return time;
                const float EndSnapEpsilon = 1f / 30f; // Workaround for end-duration keys not being applied.
                return (clipLength - time < EndSnapEpsilon) ? clipLength : time; // return a time snapped to clipLength;
            }

            private static float AnimationTime(float normalizedTime, float clipLength, bool reversed)
            {
                if (reversed)
                    normalizedTime = (1 - normalizedTime);
                if (normalizedTime < 0.0f)
                    normalizedTime = (normalizedTime % 1.0f) + 1.0f;
                return normalizedTime * clipLength;
            }

            private void InitClipInfosForLayers()
            {
                if (this.layerClipInfos.Length < this.animator.layerCount)
                {
                    System.Array.Resize<ClipInfos>(ref this.layerClipInfos, this.animator.layerCount);
                    for (int layer = 0, n = this.animator.layerCount; layer < n; ++layer)
                    {
                        if (this.layerClipInfos[layer] == null)
                            this.layerClipInfos[layer] = new ClipInfos();
                    }
                }
            }

            private void ClearClipInfosForLayers()
            {
                for (int layer = 0, n = this.layerClipInfos.Length; layer < n; ++layer)
                {
                    if (this.layerClipInfos[layer] == null)
                        this.layerClipInfos[layer] = new ClipInfos();
                    else
                    {
                        this.layerClipInfos[layer].isInterruptionActive = false;
                        this.layerClipInfos[layer].isLastFrameOfInterruption = false;
                        this.layerClipInfos[layer].clipInfos.Clear();
                        this.layerClipInfos[layer].nextClipInfos.Clear();
                        this.layerClipInfos[layer].interruptingClipInfos.Clear();
                    }
                }
            }

            private MixMode GetMixMode(int layer, MixBlend layerBlendMode)
            {
                if (this.useCustomMixMode)
                {
                    var mode = this.layerMixModes[layer];
                    // Note: at additive blending it makes no sense to use constant weight 1 at a fadeout anim add1 as
                    // with override layers, so we use AlwaysMix instead to use the proper weights.
                    // AlwaysMix leads to the expected result = lower_layer + lerp(add1, add2, transition_weight).
                    if (layerBlendMode == MixBlend.Add && mode == MixMode.MixNext)
                    {
                        mode = MixMode.AlwaysMix;
                        this.layerMixModes[layer] = mode;
                    }
                    return mode;
                }
                else
                {
                    return layerBlendMode == MixBlend.Add ? MixMode.AlwaysMix : MixMode.MixNext;
                }
            }

#if UNITY_EDITOR
            private void GetLayerBlendModes()
            {
                if (this.layerBlendModes.Length < this.animator.layerCount)
                {
                    System.Array.Resize<MixBlend>(ref this.layerBlendModes, this.animator.layerCount);
                }
                for (int layer = 0, n = this.animator.layerCount; layer < n; ++layer)
                {
                    var controller = this.animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    if (controller != null)
                    {
                        this.layerBlendModes[layer] = MixBlend.First;
                        if (layer > 0)
                        {
                            this.layerBlendModes[layer] = controller.layers[layer].blendingMode == UnityEditor.Animations.AnimatorLayerBlendingMode.Additive ?
                                MixBlend.Add : MixBlend.Replace;
                        }
                    }
                }
            }
#endif

            private void GetStateUpdatesFromAnimator(int layer)
            {

                var layerInfos = this.layerClipInfos[layer];
                var clipInfoCount = this.animator.GetCurrentAnimatorClipInfoCount(layer);
                var nextClipInfoCount = this.animator.GetNextAnimatorClipInfoCount(layer);

                var clipInfos = layerInfos.clipInfos;
                var nextClipInfos = layerInfos.nextClipInfos;
                var interruptingClipInfos = layerInfos.interruptingClipInfos;

                layerInfos.isInterruptionActive = (clipInfoCount == 0 && clipInfos.Count != 0 &&
                                                    nextClipInfoCount == 0 && nextClipInfos.Count != 0);

                // Note: during interruption, GetCurrentAnimatorClipInfoCount and GetNextAnimatorClipInfoCount
                // are returning 0 in calls above. Therefore we keep previous clipInfos and nextClipInfos
                // until the interruption is over.
                if (layerInfos.isInterruptionActive)
                {

                    // Note: The last frame of a transition interruption
                    // will have fullPathHash set to 0, therefore we have to use previous
                    // frame's infos about interruption clips and correct some values
                    // accordingly (normalizedTime and weight).
                    var interruptingStateInfo = this.animator.GetNextAnimatorStateInfo(layer);
                    layerInfos.isLastFrameOfInterruption = interruptingStateInfo.fullPathHash == 0;
                    if (!layerInfos.isLastFrameOfInterruption)
                    {
                        this.animator.GetNextAnimatorClipInfo(layer, interruptingClipInfos);
                        layerInfos.interruptingClipInfoCount = interruptingClipInfos.Count;
                        var oldTime = layerInfos.interruptingStateInfo.normalizedTime;
                        var newTime = interruptingStateInfo.normalizedTime;
                        layerInfos.interruptingClipTimeAddition = newTime - oldTime;
                        layerInfos.interruptingStateInfo = interruptingStateInfo;
                    }
                }
                else
                {
                    layerInfos.clipInfoCount = clipInfoCount;
                    layerInfos.nextClipInfoCount = nextClipInfoCount;
                    layerInfos.interruptingClipInfoCount = 0;
                    layerInfos.isLastFrameOfInterruption = false;

                    if (clipInfos.Capacity < clipInfoCount) clipInfos.Capacity = clipInfoCount;
                    if (nextClipInfos.Capacity < nextClipInfoCount) nextClipInfos.Capacity = nextClipInfoCount;

                    this.animator.GetCurrentAnimatorClipInfo(layer, clipInfos);
                    this.animator.GetNextAnimatorClipInfo(layer, nextClipInfos);

                    layerInfos.stateInfo = this.animator.GetCurrentAnimatorStateInfo(layer);
                    layerInfos.nextStateInfo = this.animator.GetNextAnimatorStateInfo(layer);
                }
            }

            private void GetAnimatorClipInfos(
                int layer,
                out bool isInterruptionActive,
                out int clipInfoCount,
                out int nextClipInfoCount,
                out int interruptingClipInfoCount,
                out IList<AnimatorClipInfo> clipInfo,
                out IList<AnimatorClipInfo> nextClipInfo,
                out IList<AnimatorClipInfo> interruptingClipInfo,
                out bool shallInterpolateWeightTo1)
            {

                var layerInfos = this.layerClipInfos[layer];
                isInterruptionActive = layerInfos.isInterruptionActive;

                clipInfoCount = layerInfos.clipInfoCount;
                nextClipInfoCount = layerInfos.nextClipInfoCount;
                interruptingClipInfoCount = layerInfos.interruptingClipInfoCount;

                clipInfo = layerInfos.clipInfos;
                nextClipInfo = layerInfos.nextClipInfos;
                interruptingClipInfo = isInterruptionActive ? layerInfos.interruptingClipInfos : null;
                shallInterpolateWeightTo1 = layerInfos.isLastFrameOfInterruption;
            }

            private void GetAnimatorStateInfos(
                int layer,
                out bool isInterruptionActive,
                out AnimatorStateInfo stateInfo,
                out AnimatorStateInfo nextStateInfo,
                out AnimatorStateInfo interruptingStateInfo,
                out float interruptingClipTimeAddition)
            {

                var layerInfos = this.layerClipInfos[layer];
                isInterruptionActive = layerInfos.isInterruptionActive;

                stateInfo = layerInfos.stateInfo;
                nextStateInfo = layerInfos.nextStateInfo;
                interruptingStateInfo = layerInfos.interruptingStateInfo;
                interruptingClipTimeAddition = layerInfos.isLastFrameOfInterruption ? layerInfos.interruptingClipTimeAddition : 0;
            }

            private Spine.Animation GetAnimation(AnimationClip clip)
            {
                if (!this.clipNameHashCodeTable.TryGetValue(clip, out var clipNameHashCode))
                {
                    clipNameHashCode = clip.name.GetHashCode();
                    this.clipNameHashCodeTable.Add(clip, clipNameHashCode);
                }
                this.animationTable.TryGetValue(clipNameHashCode, out var animation);
                return animation;
            }

            private class AnimationClipEqualityComparer : IEqualityComparer<AnimationClip>
            {
                internal static readonly IEqualityComparer<AnimationClip> Instance = new AnimationClipEqualityComparer();
                public bool Equals(AnimationClip x, AnimationClip y) { return x.GetInstanceID() == y.GetInstanceID(); }
                public int GetHashCode(AnimationClip o) { return o.GetInstanceID(); }
            }

            private class IntEqualityComparer : IEqualityComparer<int>
            {
                internal static readonly IEqualityComparer<int> Instance = new IntEqualityComparer();
                public bool Equals(int x, int y) { return x == y; }
                public int GetHashCode(int o) { return o; }
            }
        }

    }
}
