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

    [CustomEditor(typeof(BoneFollower)), CanEditMultipleObjects]
    public class BoneFollowerInspector : Editor
    {
        private SerializedProperty boneName, skeletonRenderer, followXYPosition, followZPosition, followBoneRotation,
            followLocalScale, followSkeletonFlip, maintainedAxisOrientation;
        private BoneFollower targetBoneFollower;
        private bool needsReset;

        #region Context Menu Item
        [MenuItem("CONTEXT/SkeletonRenderer/Add BoneFollower GameObject")]
        private static void AddBoneFollowerGameObject(MenuCommand cmd)
        {
            var skeletonRenderer = cmd.context as SkeletonRenderer;
            var go = EditorInstantiation.NewGameObject("New BoneFollower", true);
            var t = go.transform;
            t.SetParent(skeletonRenderer.transform);
            t.localPosition = Vector3.zero;

            var f = go.AddComponent<BoneFollower>();
            f.skeletonRenderer = skeletonRenderer;

            EditorGUIUtility.PingObject(t);

            Undo.RegisterCreatedObjectUndo(go, "Add BoneFollower");
        }

        // Validate
        [MenuItem("CONTEXT/SkeletonRenderer/Add BoneFollower GameObject", true)]
        private static bool ValidateAddBoneFollowerGameObject(MenuCommand cmd)
        {
            var skeletonRenderer = cmd.context as SkeletonRenderer;
            return skeletonRenderer.valid;
        }

        [MenuItem("CONTEXT/BoneFollower/Rename BoneFollower GameObject")]
        private static void RenameGameObject(MenuCommand cmd)
        {
            AutonameGameObject(cmd.context as BoneFollower);
        }
        #endregion

        private static void AutonameGameObject(BoneFollower boneFollower)
        {
            if (boneFollower == null) return;

            var boneName = boneFollower.boneName;
            boneFollower.gameObject.name = string.IsNullOrEmpty(boneName) ? "BoneFollower" : string.Format("{0} (BoneFollower)", boneName);
        }

        private void OnEnable()
        {
            this.skeletonRenderer = this.serializedObject.FindProperty("skeletonRenderer");
            this.boneName = this.serializedObject.FindProperty("boneName");
            this.followBoneRotation = this.serializedObject.FindProperty("followBoneRotation");
            this.followXYPosition = this.serializedObject.FindProperty("followXYPosition");
            this.followZPosition = this.serializedObject.FindProperty("followZPosition");
            this.followLocalScale = this.serializedObject.FindProperty("followLocalScale");
            this.followSkeletonFlip = this.serializedObject.FindProperty("followSkeletonFlip");
            this.maintainedAxisOrientation = this.serializedObject.FindProperty("maintainedAxisOrientation");

            this.targetBoneFollower = (BoneFollower)this.target;
            if (this.targetBoneFollower.SkeletonRenderer != null)
                this.targetBoneFollower.SkeletonRenderer.Initialize(false);

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
            var tbf = this.target as BoneFollower;
            var skeletonRendererComponent = tbf.skeletonRenderer;
            if (skeletonRendererComponent == null) return;

            var transform = skeletonRendererComponent.transform;
            var skeleton = skeletonRendererComponent.skeleton;

            if (string.IsNullOrEmpty(this.boneName.stringValue))
            {
                SpineHandles.DrawBones(transform, skeleton);
                SpineHandles.DrawBoneNames(transform, skeleton);
                Handles.Label(tbf.transform.position, "No bone selected", EditorStyles.helpBox);
            }
            else
            {
                var targetBone = tbf.bone;
                if (targetBone == null) return;
                SpineHandles.DrawBoneWireframe(transform, targetBone, SpineHandles.TransformContraintColor);
                Handles.Label(targetBone.GetWorldPosition(transform), targetBone.Data.Name, SpineHandles.BoneNameStyle);
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
            if (this.skeletonRenderer.objectReferenceValue == null)
            {
                var parentRenderer = this.targetBoneFollower.GetComponentInParent<SkeletonRenderer>();
                if (parentRenderer != null && parentRenderer.gameObject != this.targetBoneFollower.gameObject)
                {
                    this.skeletonRenderer.objectReferenceValue = parentRenderer;
                    Debug.Log("Inspector automatically assigned BoneFollower.SkeletonRenderer");
                }
            }

            EditorGUILayout.PropertyField(this.skeletonRenderer);
            var skeletonRendererReference = this.skeletonRenderer.objectReferenceValue as SkeletonRenderer;
            if (skeletonRendererReference != null)
            {
                if (skeletonRendererReference.gameObject == this.targetBoneFollower.gameObject)
                {
                    this.skeletonRenderer.objectReferenceValue = null;
                    EditorUtility.DisplayDialog("Invalid assignment.", "BoneFollower can only follow a skeleton on a separate GameObject.\n\nCreate a new GameObject for your BoneFollower, or choose a SkeletonRenderer from a different GameObject.", "Ok");
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

                BoneFollowerInspector.RecommendRigidbodyButton(this.targetBoneFollower);
            }
            else
            {
                var boneFollowerSkeletonRenderer = this.targetBoneFollower.skeletonRenderer;
                if (boneFollowerSkeletonRenderer == null)
                {
                    EditorGUILayout.HelpBox("SkeletonRenderer is unassigned. Please assign a SkeletonRenderer (SkeletonAnimation or SkeletonMecanim).", MessageType.Warning);
                }
                else
                {
                    boneFollowerSkeletonRenderer.Initialize(false);

                    if (boneFollowerSkeletonRenderer.skeletonDataAsset == null)
                        EditorGUILayout.HelpBox("Assigned SkeletonRenderer does not have SkeletonData assigned to it.", MessageType.Warning);

                    if (!boneFollowerSkeletonRenderer.valid)
                        EditorGUILayout.HelpBox("Assigned SkeletonRenderer is invalid. Check target SkeletonRenderer, its SkeletonDataAsset or the console for other errors.", MessageType.Warning);
                }
            }

            var current = Event.current;
            var wasUndo = (current.type == EventType.ValidateCommand && current.commandName == "UndoRedoPerformed");
            if (wasUndo)
                this.targetBoneFollower.Initialize();

            this.serializedObject.ApplyModifiedProperties();
        }

        internal static void RecommendRigidbodyButton(Component component)
        {
            var hasCollider2D = component.GetComponent<Collider2D>() != null || component.GetComponent<BoundingBoxFollower>() != null;
            var hasCollider3D = !hasCollider2D && component.GetComponent<Collider>();
            var missingRigidBody = (hasCollider2D && component.GetComponent<Rigidbody2D>() == null) || (hasCollider3D && component.GetComponent<Rigidbody>() == null);
            if (missingRigidBody)
            {
                using (new SpineInspectorUtility.BoxScope())
                {
                    EditorGUILayout.HelpBox("Collider detected. Unity recommends adding a Rigidbody to the Transforms of any colliders that are intended to be dynamically repositioned and rotated.", MessageType.Warning);
                    var rbType = hasCollider2D ? typeof(Rigidbody2D) : typeof(Rigidbody);
                    var rbLabel = string.Format("Add {0}", rbType.Name);
                    var rbContent = SpineInspectorUtility.TempContent(rbLabel, SpineInspectorUtility.UnityIcon(rbType), "Add a rigidbody to this GameObject to be the Physics body parent of the attached collider.");
                    if (SpineInspectorUtility.CenteredButton(rbContent)) component.gameObject.AddComponent(rbType);
                }
            }
        }
    }

}
