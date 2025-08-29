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

#define AUTOINIT_SPINEREFERENCE

using UnityEngine;

namespace Spine.Unity
{
    [CreateAssetMenu(menuName = "Spine/Animation Reference Asset", order = 100)]
    public class AnimationReferenceAsset : ScriptableObject, IHasSkeletonDataAsset
    {
        private const bool QuietSkeletonData = true;

        [SerializeField] protected SkeletonDataAsset skeletonDataAsset;
        [SerializeField, SpineAnimation] protected string animationName;
        private Animation animation;

        public SkeletonDataAsset SkeletonDataAsset { get { return this.skeletonDataAsset; } }

        public Animation Animation
        {
            get
            {
#if AUTOINIT_SPINEREFERENCE
                if (this.animation == null)
                    this.Initialize();
#endif

                return this.animation;
            }
        }

        public void Initialize()
        {
            if (this.skeletonDataAsset == null) return;
            this.animation = this.skeletonDataAsset.GetSkeletonData(AnimationReferenceAsset.QuietSkeletonData).FindAnimation(this.animationName);
            if (this.animation == null) Debug.LogWarningFormat("Animation '{0}' not found in SkeletonData : {1}.", this.animationName, this.skeletonDataAsset.name);
        }

        public static implicit operator Animation(AnimationReferenceAsset asset)
        {
            return asset.Animation;
        }
    }
}
