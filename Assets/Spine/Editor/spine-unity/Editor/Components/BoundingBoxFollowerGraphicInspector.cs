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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    using Event = UnityEngine.Event;
    using Icons = SpineEditorUtilities.Icons;

    [CustomEditor(typeof(BoundingBoxFollowerGraphic))]
    public class BoundingBoxFollowerGraphicInspector : UnityEditor.Editor
    {
        private SerializedProperty skeletonGraphic, slotName, isTrigger, clearStateOnDisable;
        private BoundingBoxFollowerGraphic follower;
        private bool rebuildRequired = false;
        private bool addBoneFollower = false;
        private bool sceneRepaintRequired = false;
        private bool debugIsExpanded;

        private GUIContent addBoneFollowerLabel;
        private GUIContent AddBoneFollowerLabel
        {
            get
            {
                if (this.addBoneFollowerLabel == null) this.addBoneFollowerLabel = new GUIContent("Add Bone Follower", Icons.bone);
                return this.addBoneFollowerLabel;
            }
        }

        private void InitializeEditor()
        {
            this.skeletonGraphic = this.serializedObject.FindProperty("skeletonGraphic");
            this.slotName = this.serializedObject.FindProperty("slotName");
            this.isTrigger = this.serializedObject.FindProperty("isTrigger");
            this.clearStateOnDisable = this.serializedObject.FindProperty("clearStateOnDisable");
            this.follower = (BoundingBoxFollowerGraphic)this.target;
        }

        public override void OnInspectorGUI()
        {

#if !NEW_PREFAB_SYSTEM
			bool isInspectingPrefab = (PrefabUtility.GetPrefabType(target) == PrefabType.Prefab);
#else
            var isInspectingPrefab = false;
#endif

            // Note: when calling InitializeEditor() in OnEnable, it throws exception
            // "SerializedObjectNotCreatableException: Object at index 0 is null".
            this.InitializeEditor();

            // Try to auto-assign SkeletonGraphic field.
            if (this.skeletonGraphic.objectReferenceValue == null)
            {
                var foundSkeletonGraphic = this.follower.GetComponentInParent<SkeletonGraphic>();
                if (foundSkeletonGraphic != null)
                    Debug.Log("BoundingBoxFollowerGraphic automatically assigned: " + foundSkeletonGraphic.gameObject.name);
                else if (Event.current.type == EventType.Repaint)
                    Debug.Log("No Spine GameObject detected. Make sure to set this GameObject as a child of the Spine GameObject; or set BoundingBoxFollowerGraphic's 'Skeleton Graphic' field in the inspector.");

                this.skeletonGraphic.objectReferenceValue = foundSkeletonGraphic;
                this.serializedObject.ApplyModifiedProperties();
                this.InitializeEditor();
            }

            var skeletonGraphicValue = this.skeletonGraphic.objectReferenceValue as SkeletonGraphic;
            if (skeletonGraphicValue != null && skeletonGraphicValue.gameObject == this.follower.gameObject)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.HelpBox("It's ideal to add BoundingBoxFollowerGraphic to a separate child GameObject of the Spine GameObject.", MessageType.Warning);

                    if (GUILayout.Button(new GUIContent("Move BoundingBoxFollowerGraphic to new GameObject", Icons.boundingBox), GUILayout.Height(30f)))
                    {
                        AddBoundingBoxFollowerGraphicChild(skeletonGraphicValue, this.follower);
                        DestroyImmediate(this.follower);
                        return;
                    }
                }
                EditorGUILayout.Space();
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(this.skeletonGraphic);
            EditorGUILayout.PropertyField(this.slotName, new GUIContent("Slot"));
            if (EditorGUI.EndChangeCheck())
            {
                this.serializedObject.ApplyModifiedProperties();
                this.InitializeEditor();
#if !NEW_PREFAB_SYSTEM
				if (!isInspectingPrefab)
					rebuildRequired = true;
#endif
            }

            using (new SpineInspectorUtility.LabelWidthScope(150f))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(this.isTrigger);
                var triggerChanged = EditorGUI.EndChangeCheck();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(this.clearStateOnDisable, new GUIContent(this.clearStateOnDisable.displayName, "Enable this if you are pooling your Spine GameObject"));
                var clearStateChanged = EditorGUI.EndChangeCheck();

                if (clearStateChanged || triggerChanged)
                {
                    this.serializedObject.ApplyModifiedProperties();
                    this.InitializeEditor();
                    if (triggerChanged)
                        foreach (var col in this.follower.colliderTable.Values)
                            col.isTrigger = this.isTrigger.boolValue;
                }
            }

            if (isInspectingPrefab)
            {
                this.follower.colliderTable.Clear();
                this.follower.nameTable.Clear();
                EditorGUILayout.HelpBox("BoundingBoxAttachments cannot be previewed in prefabs.", MessageType.Info);

                // How do you prevent components from being saved into the prefab? No such HideFlag. DontSaveInEditor | DontSaveInBuild does not work. DestroyImmediate does not work.
                var collider = this.follower.GetComponent<PolygonCollider2D>();
                if (collider != null) Debug.LogWarning("Found BoundingBoxFollowerGraphic collider components in prefab. These are disposed and regenerated at runtime.");

            }
            else
            {
                using (new SpineInspectorUtility.BoxScope())
                {
                    if (this.debugIsExpanded = EditorGUILayout.Foldout(this.debugIsExpanded, "Debug Colliders"))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField(string.Format("Attachment Names ({0} PolygonCollider2D)", this.follower.colliderTable.Count));
                        EditorGUI.BeginChangeCheck();
                        foreach (var kp in this.follower.nameTable)
                        {
                            var attachmentName = kp.Value;
                            var collider = this.follower.colliderTable[kp.Key];
                            var isPlaceholder = attachmentName != kp.Key.Name;
                            collider.enabled = EditorGUILayout.ToggleLeft(new GUIContent(!isPlaceholder ? attachmentName : string.Format("{0} [{1}]", attachmentName, kp.Key.Name), isPlaceholder ? Icons.skinPlaceholder : Icons.boundingBox), collider.enabled);
                        }
                        this.sceneRepaintRequired |= EditorGUI.EndChangeCheck();
                        EditorGUI.indentLevel--;
                    }
                }

            }

            if (this.follower.Slot == null)
                this.follower.Initialize(false);
            var hasBoneFollower = this.follower.GetComponent<BoneFollowerGraphic>() != null;
            if (!hasBoneFollower)
            {
                var buttonDisabled = this.follower.Slot == null;
                using (new EditorGUI.DisabledGroupScope(buttonDisabled))
                {
                    this.addBoneFollower |= SpineInspectorUtility.LargeCenteredButton(this.AddBoneFollowerLabel, true);
                    EditorGUILayout.Space();
                }
            }


            if (Event.current.type == EventType.Repaint)
            {
                if (this.addBoneFollower)
                {
                    var boneFollower = this.follower.gameObject.AddComponent<BoneFollowerGraphic>();
                    boneFollower.skeletonGraphic = skeletonGraphicValue;
                    boneFollower.SetBone(this.follower.Slot.Data.BoneData.Name);
                    this.addBoneFollower = false;
                }

                if (this.sceneRepaintRequired)
                {
                    SceneView.RepaintAll();
                    this.sceneRepaintRequired = false;
                }

                if (this.rebuildRequired)
                {
                    this.follower.Initialize();
                    this.rebuildRequired = false;
                }
            }
        }

        #region Menus
        [MenuItem("CONTEXT/SkeletonGraphic/Add BoundingBoxFollowerGraphic GameObject")]
        private static void AddBoundingBoxFollowerGraphicChild(MenuCommand command)
        {
            var go = AddBoundingBoxFollowerGraphicChild((SkeletonGraphic)command.context);
            Undo.RegisterCreatedObjectUndo(go, "Add BoundingBoxFollowerGraphic");
        }

        [MenuItem("CONTEXT/SkeletonGraphic/Add all BoundingBoxFollowerGraphic GameObjects")]
        private static void AddAllBoundingBoxFollowerGraphicChildren(MenuCommand command)
        {
            var objects = AddAllBoundingBoxFollowerGraphicChildren((SkeletonGraphic)command.context);
            foreach (var go in objects)
                Undo.RegisterCreatedObjectUndo(go, "Add BoundingBoxFollowerGraphic");
        }
        #endregion

        public static GameObject AddBoundingBoxFollowerGraphicChild(SkeletonGraphic skeletonGraphic,
            BoundingBoxFollowerGraphic original = null, string name = "BoundingBoxFollowerGraphic",
            string slotName = null)
        {

            var go = EditorInstantiation.NewGameObject(name, true);
            go.transform.SetParent(skeletonGraphic.transform, false);
            go.AddComponent<RectTransform>();
            var newFollower = go.AddComponent<BoundingBoxFollowerGraphic>();

            if (original != null)
            {
                newFollower.slotName = original.slotName;
                newFollower.isTrigger = original.isTrigger;
                newFollower.clearStateOnDisable = original.clearStateOnDisable;
            }
            if (slotName != null)
                newFollower.slotName = slotName;

            newFollower.skeletonGraphic = skeletonGraphic;
            newFollower.Initialize();

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            return go;
        }

        public static List<GameObject> AddAllBoundingBoxFollowerGraphicChildren(
            SkeletonGraphic skeletonGraphic, BoundingBoxFollowerGraphic original = null)
        {

            var createdGameObjects = new List<GameObject>();
            foreach (var skin in skeletonGraphic.Skeleton.Data.Skins)
            {
                var attachments = skin.Attachments;
                foreach (var entry in attachments)
                {
                    var boundingBoxAttachment = entry.Value as BoundingBoxAttachment;
                    if (boundingBoxAttachment == null)
                        continue;
                    var slotIndex = entry.Key.SlotIndex;
                    var slot = skeletonGraphic.Skeleton.Slots.Items[slotIndex];
                    var slotName = slot.Data.Name;
                    var go = AddBoundingBoxFollowerGraphicChild(skeletonGraphic,
                        original, boundingBoxAttachment.Name, slotName);
                    var boneFollower = go.AddComponent<BoneFollowerGraphic>();
                    boneFollower.skeletonGraphic = skeletonGraphic;
                    boneFollower.SetBone(slot.Data.BoneData.Name);
                    createdGameObjects.Add(go);
                }
            }
            return createdGameObjects;
        }
    }

}
