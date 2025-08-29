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

// Contributed by: Mitch Thompson

#if UNITY_2019_2_OR_NEWER
#define HINGE_JOINT_NEW_BEHAVIOUR
#endif

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    using Icons = SpineEditorUtilities.Icons;

    [CustomEditor(typeof(SkeletonUtilityBone)), CanEditMultipleObjects]
    public class SkeletonUtilityBoneInspector : UnityEditor.Editor
    {
        private SerializedProperty mode, boneName, zPosition, position, rotation, scale, overrideAlpha, hierarchy, parentReference;
        private GUIContent hierarchyLabel;

        //multi selected flags
        private bool containsFollows, containsOverrides, multiObject;

        //single selected helpers
        private SkeletonUtilityBone utilityBone;
        private SkeletonUtility skeletonUtility;
        private bool canCreateHingeChain = false;

        private readonly Dictionary<Slot, List<BoundingBoxAttachment>> boundingBoxTable = new Dictionary<Slot, List<BoundingBoxAttachment>>();

        private void OnEnable()
        {
            this.mode = this.serializedObject.FindProperty("mode");
            this.boneName = this.serializedObject.FindProperty("boneName");
            this.zPosition = this.serializedObject.FindProperty("zPosition");
            this.position = this.serializedObject.FindProperty("position");
            this.rotation = this.serializedObject.FindProperty("rotation");
            this.scale = this.serializedObject.FindProperty("scale");
            this.overrideAlpha = this.serializedObject.FindProperty("overrideAlpha");
            this.hierarchy = this.serializedObject.FindProperty("hierarchy");
            this.hierarchyLabel = new GUIContent("Skeleton Utility Parent");
            this.parentReference = this.serializedObject.FindProperty("parentReference");

            this.utilityBone = (SkeletonUtilityBone)this.target;
            this.skeletonUtility = this.utilityBone.hierarchy;
            this.EvaluateFlags();

            if (!this.utilityBone.valid && this.skeletonUtility != null)
            {
                if (this.skeletonUtility.skeletonRenderer != null)
                    this.skeletonUtility.skeletonRenderer.Initialize(false);
                if (this.skeletonUtility.skeletonGraphic != null)
                    this.skeletonUtility.skeletonGraphic.Initialize(false);
            }

            this.canCreateHingeChain = this.CanCreateHingeChain();
            this.boundingBoxTable.Clear();

            if (this.multiObject) return;
            if (this.utilityBone.bone == null) return;

            var skeleton = this.utilityBone.bone.Skeleton;
            var slotCount = skeleton.Slots.Count;
            var skin = skeleton.Skin;
            if (skeleton.Skin == null)
                skin = skeleton.Data.DefaultSkin;

            for (var i = 0; i < slotCount; i++)
            {
                var slot = this.skeletonUtility.Skeleton.Slots.Items[i];
                if (slot.Bone == this.utilityBone.bone)
                {
                    var slotAttachments = new List<Skin.SkinEntry>();
                    var slotIndex = skeleton.FindSlotIndex(slot.Data.Name);
                    skin.GetAttachments(slotIndex, slotAttachments);

                    var boundingBoxes = new List<BoundingBoxAttachment>();
                    foreach (var att in slotAttachments)
                    {
                        var boundingBoxAttachment = att.Attachment as BoundingBoxAttachment;
                        if (boundingBoxAttachment != null)
                            boundingBoxes.Add(boundingBoxAttachment);
                    }

                    if (boundingBoxes.Count > 0)
                        this.boundingBoxTable.Add(slot, boundingBoxes);
                }
            }
        }

        private void EvaluateFlags()
        {
            if (Selection.objects.Length == 1)
            {
                this.containsFollows = this.utilityBone.mode == SkeletonUtilityBone.Mode.Follow;
                this.containsOverrides = this.utilityBone.mode == SkeletonUtilityBone.Mode.Override;
            }
            else
            {
                var boneCount = 0;
                foreach (var o in Selection.objects)
                {
                    var go = o as GameObject;
                    if (go != null)
                    {
                        var sub = go.GetComponent<SkeletonUtilityBone>();
                        if (sub != null)
                        {
                            boneCount++;
                            this.containsFollows |= (sub.mode == SkeletonUtilityBone.Mode.Follow);
                            this.containsOverrides |= (sub.mode == SkeletonUtilityBone.Mode.Override);
                        }
                    }
                }

                this.multiObject |= (boneCount > 1);
            }
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(this.mode);
            if (EditorGUI.EndChangeCheck())
            {
                this.containsOverrides = this.mode.enumValueIndex == 1;
                this.containsFollows = this.mode.enumValueIndex == 0;
            }

            using (new EditorGUI.DisabledGroupScope(this.multiObject))
            {
                var str = this.boneName.stringValue;
                if (str == "")
                    str = "<None>";
                if (this.multiObject)
                    str = "<Multiple>";

                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Bone");
                    if (GUILayout.Button(str, EditorStyles.popup))
                    {
                        BoneSelectorContextMenu(str, ((SkeletonUtilityBone)this.target).hierarchy.Skeleton.Bones, "<None>", this.TargetBoneSelected);
                    }
                }
            }

            EditorGUILayout.PropertyField(this.zPosition);
            EditorGUILayout.PropertyField(this.position);
            EditorGUILayout.PropertyField(this.rotation);
            EditorGUILayout.PropertyField(this.scale);

            using (new EditorGUI.DisabledGroupScope(this.containsFollows))
            {
                EditorGUILayout.PropertyField(this.overrideAlpha);
                EditorGUILayout.PropertyField(this.parentReference);
                EditorGUILayout.PropertyField(this.hierarchy, this.hierarchyLabel);
            }

            EditorGUILayout.Space();

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();
                using (new EditorGUI.DisabledGroupScope(this.multiObject || !this.utilityBone.valid || this.utilityBone.bone == null || this.utilityBone.bone.Children.Count == 0))
                {
                    if (GUILayout.Button(SpineInspectorUtility.TempContent("Add Child Bone", Icons.bone), GUILayout.MinWidth(120), GUILayout.Height(24)))
                        BoneSelectorContextMenu("", this.utilityBone.bone.Children, "<Recursively>", this.SpawnChildBoneSelected);
                }
                using (new EditorGUI.DisabledGroupScope(this.multiObject || !this.utilityBone.valid || this.utilityBone.bone == null || this.containsOverrides))
                {
                    if (GUILayout.Button(SpineInspectorUtility.TempContent("Add Override", Icons.poseBones), GUILayout.MinWidth(120), GUILayout.Height(24)))
                        this.SpawnOverride();
                }
                EditorGUILayout.Space();
            }
            EditorGUILayout.Space();
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();
                using (new EditorGUI.DisabledGroupScope(this.multiObject || !this.utilityBone.valid || !this.canCreateHingeChain))
                {
                    if (GUILayout.Button(SpineInspectorUtility.TempContent("Create 3D Hinge Chain", Icons.hingeChain), GUILayout.MinWidth(120), GUILayout.Height(24)))
                        this.CreateHingeChain();
                    if (GUILayout.Button(SpineInspectorUtility.TempContent("Create 2D Hinge Chain", Icons.hingeChain), GUILayout.MinWidth(120), GUILayout.Height(24)))
                        this.CreateHingeChain2D();
                }
                EditorGUILayout.Space();
            }

            using (new EditorGUI.DisabledGroupScope(this.multiObject || this.boundingBoxTable.Count == 0))
            {
                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Bounding Boxes", Icons.boundingBox), EditorStyles.boldLabel);

                foreach (var entry in this.boundingBoxTable)
                {
                    var slot = entry.Key;
                    var boundingBoxes = entry.Value;

                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(slot.Data.Name);
                    EditorGUI.indentLevel++;
                    {
                        foreach (var box in boundingBoxes)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(30);
                                var buttonLabel = box.IsWeighted() ? box.Name + " (!)" : box.Name;
                                if (GUILayout.Button(buttonLabel, GUILayout.Width(200)))
                                {
                                    this.utilityBone.bone.Skeleton.UpdateWorldTransform();
                                    var bbTransform = this.utilityBone.transform.Find("[BoundingBox]" + box.Name); // Use FindChild in older versions of Unity.
                                    if (bbTransform != null)
                                    {
                                        var originalCollider = bbTransform.GetComponent<PolygonCollider2D>();
                                        if (originalCollider != null)
                                            SkeletonUtility.SetColliderPointsLocal(originalCollider, slot, box);
                                        else
                                            SkeletonUtility.AddBoundingBoxAsComponent(box, slot, bbTransform.gameObject);
                                    }
                                    else
                                    {
                                        var newPolygonCollider = SkeletonUtility.AddBoundingBoxGameObject(null, box, slot, this.utilityBone.transform);
                                        bbTransform = newPolygonCollider.transform;
                                    }
                                    EditorGUIUtility.PingObject(bbTransform);
                                }
                            }

                        }
                    }
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                }
            }

            BoneFollowerInspector.RecommendRigidbodyButton(this.utilityBone);

            this.serializedObject.ApplyModifiedProperties();
        }

        private static void BoneSelectorContextMenu(string current, ExposedList<Bone> bones, string topValue, GenericMenu.MenuFunction2 callback)
        {
            var menu = new GenericMenu();

            if (topValue != "")
                menu.AddItem(new GUIContent(topValue), current == topValue, callback, null);

            for (var i = 0; i < bones.Count; i++)
                menu.AddItem(new GUIContent(bones.Items[i].Data.Name), bones.Items[i].Data.Name == current, callback, bones.Items[i]);

            menu.ShowAsContext();
        }

        private void TargetBoneSelected(object obj)
        {
            if (obj == null)
            {
                this.boneName.stringValue = "";
                this.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                var bone = (Bone)obj;
                this.boneName.stringValue = bone.Data.Name;
                this.serializedObject.ApplyModifiedProperties();
                this.utilityBone.Reset();
            }
        }

        private void SpawnChildBoneSelected(object obj)
        {
            if (obj == null)
            {
                // Add recursively
                foreach (var bone in this.utilityBone.bone.Children)
                {
                    var go = this.skeletonUtility.SpawnBoneRecursively(bone, this.utilityBone.transform, this.utilityBone.mode, this.utilityBone.position, this.utilityBone.rotation, this.utilityBone.scale);
                    var newUtilityBones = go.GetComponentsInChildren<SkeletonUtilityBone>();
                    foreach (var utilBone in newUtilityBones)
                        SkeletonUtilityInspector.AttachIcon(utilBone);
                }
            }
            else
            {
                var bone = (Bone)obj;
                var go = this.skeletonUtility.SpawnBone(bone, this.utilityBone.transform, this.utilityBone.mode, this.utilityBone.position, this.utilityBone.rotation, this.utilityBone.scale);
                SkeletonUtilityInspector.AttachIcon(go.GetComponent<SkeletonUtilityBone>());
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
        }

        private void SpawnOverride()
        {
            var go = this.skeletonUtility.SpawnBone(this.utilityBone.bone, this.utilityBone.transform.parent, SkeletonUtilityBone.Mode.Override, this.utilityBone.position, this.utilityBone.rotation, this.utilityBone.scale);
            go.name = go.name + " [Override]";
            SkeletonUtilityInspector.AttachIcon(go.GetComponent<SkeletonUtilityBone>());
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        private bool CanCreateHingeChain()
        {
            if (this.utilityBone == null)
                return false;
            if (this.utilityBone.GetComponent<Rigidbody>() != null || this.utilityBone.GetComponent<Rigidbody2D>() != null)
                return false;
            if (this.utilityBone.bone != null && this.utilityBone.bone.Children.Count == 0)
                return false;

            var rigidbodies = this.utilityBone.GetComponentsInChildren<Rigidbody>();
            var rigidbodies2D = this.utilityBone.GetComponentsInChildren<Rigidbody2D>();
            return rigidbodies.Length <= 0 && rigidbodies2D.Length <= 0;
        }

        private void CreateHingeChain2D()
        {
            var kinematicParentUtilityBone = this.utilityBone.transform.parent.GetComponent<SkeletonUtilityBone>();
            if (kinematicParentUtilityBone == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("No parent SkeletonUtilityBone found!", "Please select the first physically moving chain node, having a parent GameObject with a SkeletonUtilityBone component attached.", "OK");
                return;
            }

            float mass = 10;
            const float rotationLimit = 20.0f;

            this.SetSkeletonUtilityToFlipByRotation();

            kinematicParentUtilityBone.mode = SkeletonUtilityBone.Mode.Follow;
            kinematicParentUtilityBone.position = kinematicParentUtilityBone.rotation = kinematicParentUtilityBone.scale = kinematicParentUtilityBone.zPosition = true;

            var commonParentObject = new GameObject(this.skeletonUtility.name + " HingeChain Parent " + this.utilityBone.name);
            var commonParentActivateOnFlip = commonParentObject.AddComponent<ActivateBasedOnFlipDirection>();
            commonParentActivateOnFlip.skeletonRenderer = this.skeletonUtility.skeletonRenderer;
            commonParentActivateOnFlip.skeletonGraphic = this.skeletonUtility.skeletonGraphic;

            // HingeChain Parent
            // Needs to be on top hierarchy level (not attached to the moving skeleton at least) for physics to apply proper momentum.
            var normalChainParentObject = new GameObject("HingeChain");
            normalChainParentObject.transform.SetParent(commonParentObject.transform);
            commonParentActivateOnFlip.activeOnNormalX = normalChainParentObject;

            //var followRotationComponent = normalChainParentObject.AddComponent<FollowSkeletonUtilityRootRotation>();
            //followRotationComponent.reference = skeletonUtility.boneRoot;

            // Follower Kinematic Rigidbody
            var followerKinematicObject = new GameObject(kinematicParentUtilityBone.name + " Follower");
            followerKinematicObject.transform.parent = normalChainParentObject.transform;
            var followerRigidbody = followerKinematicObject.AddComponent<Rigidbody2D>();
            followerRigidbody.mass = mass;
            followerRigidbody.isKinematic = true;
            followerKinematicObject.AddComponent<FollowLocationRigidbody2D>().reference = kinematicParentUtilityBone.transform;
            followerKinematicObject.transform.position = kinematicParentUtilityBone.transform.position;
            followerKinematicObject.transform.rotation = kinematicParentUtilityBone.transform.rotation;

            // Child Bones
            var utilityBones = this.utilityBone.GetComponentsInChildren<SkeletonUtilityBone>();
            var childBoneParentReference = followerKinematicObject.transform;
            for (var i = 0; i < utilityBones.Length; ++i)
            {
                var childBone = utilityBones[i];
                mass *= 0.75f;
                childBone.parentReference = (i == 0) ? kinematicParentUtilityBone.transform : childBoneParentReference;
                childBone.transform.SetParent(normalChainParentObject.transform, true); // we need a flat hierarchy of all Joint objects in Unity.
                AttachRigidbodyAndCollider2D(childBone);
                childBone.mode = SkeletonUtilityBone.Mode.Override;
                childBone.scale = childBone.position = childBone.zPosition = false;

                var joint = childBone.gameObject.AddComponent<HingeJoint2D>();
                joint.connectedBody = childBoneParentReference.GetComponent<Rigidbody2D>();
                joint.useLimits = true;
                this.ApplyJoint2DAngleLimits(joint, rotationLimit, childBoneParentReference, childBone.transform);

                childBone.GetComponent<Rigidbody2D>().mass = mass;
                childBoneParentReference = childBone.transform;
            }

            this.Duplicate2DHierarchyForFlippedChains(normalChainParentObject, commonParentActivateOnFlip, this.skeletonUtility.transform, rotationLimit);
            UnityEditor.Selection.activeGameObject = commonParentObject;
        }

        private void ApplyJoint2DAngleLimits(HingeJoint2D joint, float rotationLimit, Transform parentBone, Transform bone)
        {
#if HINGE_JOINT_NEW_BEHAVIOUR
            var referenceAngle = (parentBone.eulerAngles.z - bone.eulerAngles.z + 360f) % 360f;
            var minAngle = referenceAngle - rotationLimit;
            var maxAngle = referenceAngle + rotationLimit;
            if (maxAngle > 270f)
            {
                minAngle -= 360f;
                maxAngle -= 360f;
            }
            if (minAngle < -90f)
            {
                minAngle += 360f;
                maxAngle += 360f;
            }
#else
			float minAngle = - rotationLimit;
			float maxAngle = rotationLimit;
#endif
            joint.limits = new JointAngleLimits2D
            {
                min = minAngle,
                max = maxAngle
            };
        }

        private void Duplicate2DHierarchyForFlippedChains(GameObject normalChainParentObject, ActivateBasedOnFlipDirection commonParentActivateOnFlip,
                                                    Transform skeletonUtilityRoot, float rotationLimit)
        {

            var mirroredChain = GameObject.Instantiate(normalChainParentObject, normalChainParentObject.transform.position,
                normalChainParentObject.transform.rotation, commonParentActivateOnFlip.transform);
            mirroredChain.name = normalChainParentObject.name + " FlippedX";

            commonParentActivateOnFlip.activeOnFlippedX = mirroredChain;

            var followerKinematicObject = mirroredChain.GetComponentInChildren<FollowLocationRigidbody2D>();
            followerKinematicObject.followFlippedX = true;
            this.FlipBone2DHorizontal(followerKinematicObject.transform, skeletonUtilityRoot);

            var childBoneJoints = mirroredChain.GetComponentsInChildren<HingeJoint2D>();
            Transform prevRotatedChild = null;
            var parentTransformForAngles = followerKinematicObject.transform;
            for (var i = 0; i < childBoneJoints.Length; ++i)
            {
                var joint = childBoneJoints[i];
                this.FlipBone2DHorizontal(joint.transform, skeletonUtilityRoot);
                this.ApplyJoint2DAngleLimits(joint, rotationLimit, parentTransformForAngles, joint.transform);

                var rotatedChild = GameObject.Instantiate(joint.gameObject, joint.transform, true);
                rotatedChild.name = joint.name + " rotated";
                var rotationEulerAngles = rotatedChild.transform.localEulerAngles;
                rotationEulerAngles.x = 180;
                rotatedChild.transform.localEulerAngles = rotationEulerAngles;
                DestroyImmediate(rotatedChild.GetComponent<HingeJoint2D>());
                DestroyImmediate(rotatedChild.GetComponent<BoxCollider2D>());
                DestroyImmediate(rotatedChild.GetComponent<Rigidbody2D>());

                DestroyImmediate(joint.gameObject.GetComponent<SkeletonUtilityBone>());

                if (i > 0)
                {
                    var utilityBone = rotatedChild.GetComponent<SkeletonUtilityBone>();
                    utilityBone.parentReference = prevRotatedChild;
                }
                prevRotatedChild = rotatedChild.transform;
                parentTransformForAngles = joint.transform;
            }

            mirroredChain.SetActive(false);
        }

        private void FlipBone2DHorizontal(Transform bone, Transform mirrorPosition)
        {
            var position = bone.position;
            position.x = 2 * mirrorPosition.position.x - position.x; // = mirrorPosition + (mirrorPosition - bone.position)
            bone.position = position;

            var boneZ = bone.forward;
            var boneX = bone.right;
            boneX.x *= -1;

            bone.rotation = Quaternion.LookRotation(boneZ, Vector3.Cross(boneZ, boneX));
        }

        private void CreateHingeChain()
        {
            var kinematicParentUtilityBone = this.utilityBone.transform.parent.GetComponent<SkeletonUtilityBone>();
            if (kinematicParentUtilityBone == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("No parent SkeletonUtilityBone found!", "Please select the first physically moving chain node, having a parent GameObject with a SkeletonUtilityBone component attached.", "OK");
                return;
            }

            this.SetSkeletonUtilityToFlipByRotation();

            kinematicParentUtilityBone.mode = SkeletonUtilityBone.Mode.Follow;
            kinematicParentUtilityBone.position = kinematicParentUtilityBone.rotation = kinematicParentUtilityBone.scale = kinematicParentUtilityBone.zPosition = true;

            // HingeChain Parent
            // Needs to be on top hierarchy level (not attached to the moving skeleton at least) for physics to apply proper momentum.
            var chainParentObject = new GameObject(this.skeletonUtility.name + " HingeChain Parent " + this.utilityBone.name);
            var followRotationComponent = chainParentObject.AddComponent<FollowSkeletonUtilityRootRotation>();
            followRotationComponent.reference = this.skeletonUtility.boneRoot;

            // Follower Kinematic Rigidbody
            var followerKinematicObject = new GameObject(kinematicParentUtilityBone.name + " Follower");
            followerKinematicObject.transform.parent = chainParentObject.transform;
            var followerRigidbody = followerKinematicObject.AddComponent<Rigidbody>();
            followerRigidbody.mass = 10;
            followerRigidbody.isKinematic = true;
            followerKinematicObject.AddComponent<FollowLocationRigidbody>().reference = kinematicParentUtilityBone.transform;
            followerKinematicObject.transform.position = kinematicParentUtilityBone.transform.position;
            followerKinematicObject.transform.rotation = kinematicParentUtilityBone.transform.rotation;

            // Child Bones
            var utilityBones = this.utilityBone.GetComponentsInChildren<SkeletonUtilityBone>();
            var childBoneParentReference = followerKinematicObject.transform;
            foreach (var childBone in utilityBones)
            {
                childBone.parentReference = childBoneParentReference;
                childBone.transform.SetParent(chainParentObject.transform, true); // we need a flat hierarchy of all Joint objects in Unity.
                AttachRigidbodyAndCollider(childBone);
                childBone.mode = SkeletonUtilityBone.Mode.Override;

                var joint = childBone.gameObject.AddComponent<HingeJoint>();
                joint.axis = Vector3.forward;
                joint.connectedBody = childBoneParentReference.GetComponent<Rigidbody>();
                joint.useLimits = true;
                joint.limits = new JointLimits
                {
                    min = -20,
                    max = 20
                };
                childBone.GetComponent<Rigidbody>().mass = childBoneParentReference.transform.GetComponent<Rigidbody>().mass * 0.75f;

                childBoneParentReference = childBone.transform;
            }
            UnityEditor.Selection.activeGameObject = chainParentObject;
        }

        private void SetSkeletonUtilityToFlipByRotation()
        {
            if (!this.skeletonUtility.flipBy180DegreeRotation)
            {
                this.skeletonUtility.flipBy180DegreeRotation = true;
                Debug.Log("Set SkeletonUtility " + this.skeletonUtility.name + " to flip by rotation instead of negative scale (required).", this.skeletonUtility);
            }
        }

        private static void AttachRigidbodyAndCollider(SkeletonUtilityBone utilBone, bool enableCollider = false)
        {
            if (utilBone.GetComponent<Collider>() == null)
            {
                if (utilBone.bone.Data.Length == 0)
                {
                    var sphere = utilBone.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = 0.1f;
                    sphere.enabled = enableCollider;
                }
                else
                {
                    var length = utilBone.bone.Data.Length;
                    var box = utilBone.gameObject.AddComponent<BoxCollider>();
                    box.size = new Vector3(length, length / 3f, 0.2f);
                    box.center = new Vector3(length / 2f, 0, 0);
                    box.enabled = enableCollider;
                }
            }
            utilBone.gameObject.AddComponent<Rigidbody>();
        }

        private static void AttachRigidbodyAndCollider2D(SkeletonUtilityBone utilBone, bool enableCollider = false)
        {
            if (utilBone.GetComponent<Collider2D>() == null)
            {
                if (utilBone.bone.Data.Length == 0)
                {
                    var sphere = utilBone.gameObject.AddComponent<CircleCollider2D>();
                    sphere.radius = 0.1f;
                    sphere.enabled = enableCollider;
                }
                else
                {
                    var length = utilBone.bone.Data.Length;
                    var box = utilBone.gameObject.AddComponent<BoxCollider2D>();
                    box.size = new Vector3(length, length / 3f, 0.2f);
                    box.offset = new Vector3(length / 2f, 0, 0);
                    box.enabled = enableCollider;
                }
            }
            utilBone.gameObject.AddComponent<Rigidbody2D>();
        }
    }
}
