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

using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{

    [CustomEditor(typeof(SkeletonAnimation))]
    [CanEditMultipleObjects]
    public class SkeletonAnimationInspector : SkeletonRendererInspector
    {
        protected SerializedProperty animationName, loop, timeScale, autoReset;
        protected bool wasAnimationParameterChanged = false;
        protected bool requireRepaint;
        private readonly GUIContent LoopLabel = new GUIContent("Loop", "Whether or not .AnimationName should loop. This only applies to the initial animation specified in the inspector, or any subsequent Animations played through .AnimationName. Animations set through state.SetAnimation are unaffected.");
        private readonly GUIContent TimeScaleLabel = new GUIContent("Time Scale", "The rate at which animations progress over time. 1 means normal speed. 0.5 means 50% speed.");

        protected override void OnEnable()
        {
            base.OnEnable();
            this.animationName = this.serializedObject.FindProperty("_animationName");
            this.loop = this.serializedObject.FindProperty("loop");
            this.timeScale = this.serializedObject.FindProperty("timeScale");
        }

        protected override void DrawInspectorGUI(bool multi)
        {
            base.DrawInspectorGUI(multi);
            if (!this.TargetIsValid) return;
            var sameData = SpineInspectorUtility.TargetsUseSameData(this.serializedObject);

            foreach (var o in this.targets)
                this.TrySetAnimation(o as SkeletonAnimation);

            EditorGUILayout.Space();
            if (!sameData)
            {
                EditorGUILayout.DelayedTextField(this.animationName);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(this.animationName);
                this.wasAnimationParameterChanged |= EditorGUI.EndChangeCheck(); // Value used in the next update.
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(this.loop, this.LoopLabel);
            this.wasAnimationParameterChanged |= EditorGUI.EndChangeCheck(); // Value used in the next update.
            EditorGUILayout.PropertyField(this.timeScale, this.TimeScaleLabel);
            foreach (var o in this.targets)
            {
                var component = o as SkeletonAnimation;
                component.timeScale = Mathf.Max(component.timeScale, 0);
            }

            EditorGUILayout.Space();
            this.SkeletonRootMotionParameter();

            this.serializedObject.ApplyModifiedProperties();

            if (!this.isInspectingPrefab)
            {
                if (this.requireRepaint)
                {
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    this.requireRepaint = false;
                }
            }
        }

        protected void TrySetAnimation(SkeletonAnimation skeletonAnimation)
        {
            if (skeletonAnimation == null) return;
            if (!skeletonAnimation.valid || skeletonAnimation.AnimationState == null)
                return;

            var current = skeletonAnimation.AnimationState.GetCurrent(0);
            if (!this.isInspectingPrefab)
            {
                var activeAnimation = (current != null) ? current.Animation.Name : "";
                var activeLoop = (current != null) ? current.Loop : false;
                var animationParameterChanged = this.wasAnimationParameterChanged &&
                    ((activeAnimation != this.animationName.stringValue) || (activeLoop != this.loop.boolValue));
                if (animationParameterChanged)
                {
                    this.wasAnimationParameterChanged = false;
                    var skeleton = skeletonAnimation.Skeleton;
                    var state = skeletonAnimation.AnimationState;

                    if (!Application.isPlaying)
                    {
                        if (state != null) state.ClearTrack(0);
                        skeleton.SetToSetupPose();
                    }

                    var animationToUse = skeleton.Data.FindAnimation(this.animationName.stringValue);

                    if (!Application.isPlaying)
                    {
                        if (animationToUse != null)
                        {
                            skeletonAnimation.AnimationState.SetAnimation(0, animationToUse, this.loop.boolValue);
                        }
                        skeletonAnimation.Update(0);
                        skeletonAnimation.LateUpdate();
                        this.requireRepaint = true;
                    }
                    else
                    {
                        if (animationToUse != null)
                            state.SetAnimation(0, animationToUse, this.loop.boolValue);
                        else
                            state.ClearTrack(0);
                    }
                }

                // Reflect animationName serialized property in the inspector even if SetAnimation API was used.
                if (Application.isPlaying)
                {
                    if (current != null && current.Animation != null)
                    {
                        if (skeletonAnimation.AnimationName != this.animationName.stringValue)
                            this.animationName.stringValue = current.Animation.Name;
                    }
                }
            }
        }
    }
}
