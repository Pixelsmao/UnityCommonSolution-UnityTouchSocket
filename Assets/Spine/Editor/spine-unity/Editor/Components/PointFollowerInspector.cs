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

    [CustomEditor(typeof(PointFollower)), CanEditMultipleObjects]
    public class PointFollowerInspector : Editor
    {
        private SerializedProperty slotName;
        private SerializedProperty pointAttachmentName;
        private SerializedProperty skeletonRenderer;
        private readonly SerializedProperty followZPosition;
        private readonly SerializedProperty followBoneRotation;
        private readonly SerializedProperty followSkeletonFlip;
        private PointFollower targetPointFollower;
        private bool needsReset;

        #region Context Menu Item
        [MenuItem("CONTEXT/SkeletonRenderer/Add PointFollower GameObject")]
        private static void AddBoneFollowerGameObject(MenuCommand cmd)
        {
            var skeletonRenderer = cmd.context as SkeletonRenderer;
            var go = EditorInstantiation.NewGameObject("PointFollower", true);
            var t = go.transform;
            t.SetParent(skeletonRenderer.transform);
            t.localPosition = Vector3.zero;

            var f = go.AddComponent<PointFollower>();
            f.skeletonRenderer = skeletonRenderer;

            EditorGUIUtility.PingObject(t);

            Undo.RegisterCreatedObjectUndo(go, "Add PointFollower");
        }

        // Validate
        [MenuItem("CONTEXT/SkeletonRenderer/Add PointFollower GameObject", true)]
        private static bool ValidateAddBoneFollowerGameObject(MenuCommand cmd)
        {
            var skeletonRenderer = cmd.context as SkeletonRenderer;
            return skeletonRenderer.valid;
        }
        #endregion

        private void OnEnable()
        {
            this.skeletonRenderer = this.serializedObject.FindProperty("skeletonRenderer");
            this.slotName = this.serializedObject.FindProperty("slotName");
            this.pointAttachmentName = this.serializedObject.FindProperty("pointAttachmentName");

            this.targetPointFollower = (PointFollower)this.target;
            if (this.targetPointFollower.skeletonRenderer != null)
                this.targetPointFollower.skeletonRenderer.Initialize(false);

            if (!this.targetPointFollower.IsValid || this.needsReset)
            {
                this.targetPointFollower.Initialize();
                this.targetPointFollower.LateUpdate();
                this.needsReset = false;
                SceneView.RepaintAll();
            }
        }

        public void OnSceneGUI()
        {
            var tbf = this.target as PointFollower;
            var skeletonRendererComponent = tbf.skeletonRenderer;
            if (skeletonRendererComponent == null)
                return;

            var skeleton = skeletonRendererComponent.skeleton;
            var skeletonTransform = skeletonRendererComponent.transform;

            if (string.IsNullOrEmpty(this.pointAttachmentName.stringValue))
            {
                // Draw all active PointAttachments in the current skin
                var currentSkin = skeleton.Skin;
                if (currentSkin != skeleton.Data.DefaultSkin) DrawPointsInSkin(skeleton.Data.DefaultSkin, skeleton, skeletonTransform);
                if (currentSkin != null) DrawPointsInSkin(currentSkin, skeleton, skeletonTransform);
            }
            else
            {
                var slotIndex = skeleton.FindSlotIndex(this.slotName.stringValue);
                if (slotIndex >= 0)
                {
                    var slot = skeleton.Slots.Items[slotIndex];
                    var point = skeleton.GetAttachment(slotIndex, this.pointAttachmentName.stringValue) as PointAttachment;
                    if (point != null)
                    {
                        DrawPointAttachmentWithLabel(point, slot.Bone, skeletonTransform);
                    }
                }
            }
        }

        private static void DrawPointsInSkin(Skin skin, Skeleton skeleton, Transform transform)
        {
            foreach (var skinEntry in skin.Attachments)
            {
                var attachment = skinEntry.Value as PointAttachment;
                if (attachment != null)
                {
                    var skinKey = skinEntry.Key;
                    var slot = skeleton.Slots.Items[skinKey.SlotIndex];
                    DrawPointAttachmentWithLabel(attachment, slot.Bone, transform);
                }
            }
        }

        private static void DrawPointAttachmentWithLabel(PointAttachment point, Bone bone, Transform transform)
        {
            var labelOffset = new Vector3(0f, -0.2f, 0f);
            SpineHandles.DrawPointAttachment(bone, point, transform);
            Handles.Label(labelOffset + point.GetWorldPosition(bone, transform), point.Name, SpineHandles.PointNameStyle);
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
                this.targetPointFollower.Initialize();
                this.targetPointFollower.LateUpdate();
                this.needsReset = false;
                SceneView.RepaintAll();
            }
            this.serializedObject.Update();

            this.DrawDefaultInspector();

            // Find Renderer
            if (this.skeletonRenderer.objectReferenceValue == null)
            {
                var parentRenderer = this.targetPointFollower.GetComponentInParent<SkeletonRenderer>();
                if (parentRenderer != null && parentRenderer.gameObject != this.targetPointFollower.gameObject)
                {
                    this.skeletonRenderer.objectReferenceValue = parentRenderer;
                    Debug.Log("Inspector automatically assigned PointFollower.SkeletonRenderer");
                }
            }

            var skeletonRendererReference = this.skeletonRenderer.objectReferenceValue as SkeletonRenderer;
            if (skeletonRendererReference != null)
            {
                if (skeletonRendererReference.gameObject == this.targetPointFollower.gameObject)
                {
                    this.skeletonRenderer.objectReferenceValue = null;
                    EditorUtility.DisplayDialog("Invalid assignment.", "PointFollower can only follow a skeleton on a separate GameObject.\n\nCreate a new GameObject for your PointFollower, or choose a SkeletonRenderer from a different GameObject.", "Ok");
                }
            }

            if (!this.targetPointFollower.IsValid)
            {
                this.needsReset = true;
            }

            var current = Event.current;
            var wasUndo = (current.type == EventType.ValidateCommand && current.commandName == "UndoRedoPerformed");
            if (wasUndo)
                this.targetPointFollower.Initialize();

            this.serializedObject.ApplyModifiedProperties();
        }
    }

}
