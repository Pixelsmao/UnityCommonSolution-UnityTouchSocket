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

    using Editor = UnityEditor.Editor;
    using Event = UnityEngine.Event;

    [CustomEditor(typeof(BoneFollowerGraphic)), CanEditMultipleObjects]
    public class BoneFollowerGraphicInspector : Editor
    {

        private SerializedProperty boneName, skeletonGraphic, followXYPosition, followZPosition, followBoneRotation,
            followLocalScale, followSkeletonFlip, maintainedAxisOrientation;
        private BoneFollowerGraphic targetBoneFollower;
        private bool needsReset;

        #region Context Menu Item
        [MenuItem("CONTEXT/SkeletonGraphic/Add BoneFollower GameObject")]
        private static void AddBoneFollowerGameObject(MenuCommand cmd)
        {
            var skeletonGraphic = cmd.context as SkeletonGraphic;
            var go = EditorInstantiation.NewGameObject("BoneFollower", true, typeof(RectTransform));
            var t = go.transform;
            t.SetParent(skeletonGraphic.transform);
            t.localPosition = Vector3.zero;

            var f = go.AddComponent<BoneFollowerGraphic>();
            f.skeletonGraphic = skeletonGraphic;
            f.SetBone(skeletonGraphic.Skeleton.RootBone.Data.Name);

            EditorGUIUtility.PingObject(t);

            Undo.RegisterCreatedObjectUndo(go, "Add BoneFollowerGraphic");
        }

        // Validate
        [MenuItem("CONTEXT/SkeletonGraphic/Add BoneFollower GameObject", true)]
        private static bool ValidateAddBoneFollowerGameObject(MenuCommand cmd)
        {
            var skeletonGraphic = cmd.context as SkeletonGraphic;
            return skeletonGraphic.IsValid;
        }
        #endregion

        private void OnEnable()
        {
            this.skeletonGraphic = this.serializedObject.FindProperty("skeletonGraphic");
            this.boneName = this.serializedObject.FindProperty("boneName");
            this.followBoneRotation = this.serializedObject.FindProperty("followBoneRotation");
            this.followXYPosition = this.serializedObject.FindProperty("followXYPosition");
            this.followZPosition = this.serializedObject.FindProperty("followZPosition");
            this.followLocalScale = this.serializedObject.FindProperty("followLocalScale");
            this.followSkeletonFlip = this.serializedObject.FindProperty("followSkeletonFlip");
            this.maintainedAxisOrientation = this.serializedObject.FindProperty("maintainedAxisOrientation");

            this.targetBoneFollower = (BoneFollowerGraphic)this.target;
            if (this.targetBoneFollower.SkeletonGraphic != null)
                this.targetBoneFollower.SkeletonGraphic.Initialize(false);

            if (!this.targetBoneFollower.valid || this.needsReset)
            {
                this.targetBoneFollower.Initialize();
                this.targetBoneFollower.LateUpdate();
                this.needsReset = false;
                SceneView.RepaintAll();
            }
        }

        public void OnSceneGUI()
        {
            var tbf = this.target as BoneFollowerGraphic;
            var skeletonGraphicComponent = tbf.SkeletonGraphic;
            if (skeletonGraphicComponent == null) return;

            var transform = skeletonGraphicComponent.transform;
            var skeleton = skeletonGraphicComponent.Skeleton;
            var canvas = skeletonGraphicComponent.canvas;
            var positionScale = canvas == null ? 1f : skeletonGraphicComponent.canvas.referencePixelsPerUnit;

            if (string.IsNullOrEmpty(this.boneName.stringValue))
            {
                SpineHandles.DrawBones(transform, skeleton, positionScale);
                SpineHandles.DrawBoneNames(transform, skeleton, positionScale);
                Handles.Label(tbf.transform.position, "No bone selected", EditorStyles.helpBox);
            }
            else
            {
                var targetBone = tbf.bone;
                if (targetBone == null) return;

                SpineHandles.DrawBoneWireframe(transform, targetBone, SpineHandles.TransformContraintColor, positionScale);
                Handles.Label(targetBone.GetWorldPosition(transform, positionScale), targetBone.Data.Name, SpineHandles.BoneNameStyle);
            }
        }

        public override void OnInspectorGUI()
        {
            if (this.serializedObject.isEditingMultipleObjects)
            {
                if (this.needsReset)
                {
                    this.needsReset = false;
                    foreach (var o in this.targets)
                    {
                        var bf = (BoneFollower)o;
                        bf.Initialize();
                        bf.LateUpdate();
                    }
                    SceneView.RepaintAll();
                }

                EditorGUI.BeginChangeCheck();
                this.DrawDefaultInspector();
                this.needsReset |= EditorGUI.EndChangeCheck();
                return;
            }

            if (this.needsReset && Event.current.type == EventType.Layout)
            {
                this.targetBoneFollower.Initialize();
                this.targetBoneFollower.LateUpdate();
                this.needsReset = false;
                SceneView.RepaintAll();
            }
            this.serializedObject.Update();

            // Find Renderer
            if (this.skeletonGraphic.objectReferenceValue == null)
            {
                var parentRenderer = this.targetBoneFollower.GetComponentInParent<SkeletonGraphic>();
                if (parentRenderer != null && parentRenderer.gameObject != this.targetBoneFollower.gameObject)
                {
                    this.skeletonGraphic.objectReferenceValue = parentRenderer;
                    Debug.Log("Inspector automatically assigned BoneFollowerGraphic.SkeletonGraphic");
                }
            }

            EditorGUILayout.PropertyField(this.skeletonGraphic);
            var skeletonGraphicComponent = this.skeletonGraphic.objectReferenceValue as SkeletonGraphic;
            if (skeletonGraphicComponent != null)
            {
                if (skeletonGraphicComponent.gameObject == this.targetBoneFollower.gameObject)
                {
                    this.skeletonGraphic.objectReferenceValue = null;
                    EditorUtility.DisplayDialog("Invalid assignment.", "BoneFollowerGraphic can only follow a skeleton on a separate GameObject.\n\nCreate a new GameObject for your BoneFollower, or choose a SkeletonGraphic from a different GameObject.", "Ok");
                }
            }

            if (!this.targetBoneFollower.valid)
            {
                this.needsReset = true;
            }

            if (this.targetBoneFollower.valid)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(this.boneName);
                this.needsReset |= EditorGUI.EndChangeCheck();

                EditorGUILayout.PropertyField(this.followBoneRotation);
                EditorGUILayout.PropertyField(this.followXYPosition);
                EditorGUILayout.PropertyField(this.followZPosition);
                EditorGUILayout.PropertyField(this.followLocalScale);
                EditorGUILayout.PropertyField(this.followSkeletonFlip);
                if ((this.followSkeletonFlip.hasMultipleDifferentValues || this.followSkeletonFlip.boolValue == false) &&
                    (this.followBoneRotation.hasMultipleDifferentValues || this.followBoneRotation.boolValue == true))
                {
                    using (new SpineInspectorUtility.IndentScope())
                        EditorGUILayout.PropertyField(this.maintainedAxisOrientation);
                }

                //BoneFollowerInspector.RecommendRigidbodyButton(targetBoneFollower);
            }
            else
            {
                var boneFollowerSkeletonGraphic = this.targetBoneFollower.skeletonGraphic;
                if (boneFollowerSkeletonGraphic == null)
                {
                    EditorGUILayout.HelpBox("SkeletonGraphic is unassigned. Please assign a SkeletonRenderer (SkeletonAnimation or SkeletonMecanim).", MessageType.Warning);
                }
                else
                {
                    boneFollowerSkeletonGraphic.Initialize(false);

                    if (boneFollowerSkeletonGraphic.skeletonDataAsset == null)
                        EditorGUILayout.HelpBox("Assigned SkeletonGraphic does not have SkeletonData assigned to it.", MessageType.Warning);

                    if (!boneFollowerSkeletonGraphic.IsValid)
                        EditorGUILayout.HelpBox("Assigned SkeletonGraphic is invalid. Check target SkeletonGraphic, its SkeletonDataAsset or the console for other errors.", MessageType.Warning);
                }
            }

            var current = Event.current;
            var wasUndo = (current.type == EventType.ValidateCommand && current.commandName == "UndoRedoPerformed");
            if (wasUndo)
                this.targetBoneFollower.Initialize();

            this.serializedObject.ApplyModifiedProperties();
        }

    }
}
