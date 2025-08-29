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

#define SPINE_SKELETONMECANIM

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{

    /// <summary>
    /// [SUPPORTS]
    /// Linear, Constant, and Bezier Curves*
    /// Inverse Kinematics*
    /// Inherit Rotation
    /// Translate Timeline
    /// Rotate Timeline
    /// Scale Timeline**
    /// Event Timeline***
    /// Attachment Timeline
    ///
    /// RegionAttachment
    /// MeshAttachment (optionally Skinned)
    ///
    /// [LIMITATIONS]
    /// *Bezier Curves are baked into the animation at 60fps and are not realtime. Use bakeIncrement constant to adjust key density if desired.
    /// *Inverse Kinematics is baked into the animation at 60fps and are not realtime. Use bakeIncrement constant to adjust key density if desired.
    /// ***Events may only fire 1 type of data per event in Unity safely so priority to String data if present in Spine key, otherwise a Float is sent whether the Spine key was Int or Float with priority given to Int.
    ///
    /// [DOES NOT SUPPORT]
    /// FFD (Unity does not provide access to BlendShapes with code)
    /// Color Keys (Maybe one day when Unity supports full FBX standard and provides access with code)
    /// Draw Order Keyframes
    /// </summary>
    public static class SkeletonBaker
    {

        #region SkeletonMecanim's Mecanim Clips
#if SPINE_SKELETONMECANIM
        public static void UpdateMecanimClips(SkeletonDataAsset skeletonDataAsset)
        {
            if (skeletonDataAsset.controller == null)
                return;

            SkeletonBaker.GenerateMecanimAnimationClips(skeletonDataAsset);
        }

        public static void GenerateMecanimAnimationClips(SkeletonDataAsset skeletonDataAsset)
        {
            var data = skeletonDataAsset.GetSkeletonData(true);
            if (data == null)
            {
                Debug.LogError("SkeletonData loading failed!", skeletonDataAsset);
                return;
            }

            var dataPath = AssetDatabase.GetAssetPath(skeletonDataAsset);
            var controllerPath = dataPath.Replace(AssetUtility.SkeletonDataSuffix, "_Controller").Replace(".asset", ".controller");
            UnityEditor.Animations.AnimatorController controller;
            if (skeletonDataAsset.controller != null)
            {
                controller = (UnityEditor.Animations.AnimatorController)skeletonDataAsset.controller;
                controllerPath = AssetDatabase.GetAssetPath(controller);
            }
            else
            {
                if (File.Exists(controllerPath))
                {
                    if (EditorUtility.DisplayDialog("Controller Overwrite Warning", "Unknown Controller already exists at: " + controllerPath, "Update", "Overwrite"))
                    {
                        controller = (UnityEditor.Animations.AnimatorController)AssetDatabase.LoadAssetAtPath(controllerPath, typeof(RuntimeAnimatorController));
                    }
                    else
                    {
                        controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    }
                }
                else
                {
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                }

            }

            skeletonDataAsset.controller = controller;
            EditorUtility.SetDirty(skeletonDataAsset);

            var objs = AssetDatabase.LoadAllAssetsAtPath(controllerPath);

            var unityAnimationClipTable = new Dictionary<string, AnimationClip>();
            var spineAnimationTable = new Dictionary<string, Spine.Animation>();

            foreach (var o in objs)
            {
                //Debug.LogFormat("({0}){1} : {3} + {2} + {4}", o.GetType(), o.name, o.hideFlags, o.GetInstanceID(), o.GetHashCode());
                // There is a bug in Unity 5.3.3 (and likely before) that creates
                // a duplicate AnimationClip when you duplicate a Mecanim Animator State.
                // These duplicates seem to be identifiable by their HideFlags, so we'll exclude them.
                if (o is AnimationClip)
                {
                    var clip = o as AnimationClip;
                    if (!clip.HasFlag(HideFlags.HideInHierarchy))
                    {
                        if (unityAnimationClipTable.ContainsKey(clip.name))
                        {
                            Debug.LogWarningFormat("Duplicate AnimationClips were found named {0}", clip.name);
                        }
                        unityAnimationClipTable.Add(clip.name, clip);
                    }
                }
            }

            foreach (var animations in data.Animations)
            {
                var animationName = animations.Name; // Review for unsafe names. Requires runtime implementation too.
                spineAnimationTable.Add(animationName, animations);

                if (unityAnimationClipTable.ContainsKey(animationName) == false)
                {
                    var newClip = new AnimationClip
                    {
                        name = animationName
                    };
                    //AssetDatabase.CreateAsset(newClip, Path.GetDirectoryName(dataPath) + "/" + animationName + ".asset");
                    AssetDatabase.AddObjectToAsset(newClip, controller);
                    unityAnimationClipTable.Add(animationName, newClip);
                }

                var clip = unityAnimationClipTable[animationName];
                clip.SetCurve("", typeof(GameObject), "dummy", AnimationCurve.Linear(0, 0, animations.Duration, 0));
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.stopTime = animations.Duration;
                SetAnimationSettings(clip, settings);

                AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
                foreach (var t in animations.Timelines)
                {
                    if (t is EventTimeline)
                        ParseEventTimeline((EventTimeline)t, clip, SendMessageOptions.DontRequireReceiver);
                }

                EditorUtility.SetDirty(clip);
                unityAnimationClipTable.Remove(animationName);
            }

            foreach (var clip in unityAnimationClipTable.Values)
            {
                AnimationClip.DestroyImmediate(clip, true);
            }

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private static bool HasFlag(this UnityEngine.Object o, HideFlags flagToCheck)
        {
            return (o.hideFlags & flagToCheck) == flagToCheck;
        }
#endif
        #endregion

        #region Prefab and AnimationClip Baking
        /// <summary>
        /// Interval between key sampling for Bezier curves, IK controlled bones, and Inherit Rotation effected bones.
        /// </summary>
        private const float BakeIncrement = 1 / 60f;

        public static void BakeToPrefab(SkeletonDataAsset skeletonDataAsset, ExposedList<Skin> skins, string outputPath = "", bool bakeAnimations = true, bool bakeIK = true, SendMessageOptions eventOptions = SendMessageOptions.DontRequireReceiver)
        {
            if (skeletonDataAsset == null || skeletonDataAsset.GetSkeletonData(true) == null)
            {
                Debug.LogError("Could not export Spine Skeleton because SkeletonDataAsset is null or invalid!");
                return;
            }

            if (outputPath == "")
            {
                outputPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(skeletonDataAsset)).Replace('\\', '/') + "/Baked";
                System.IO.Directory.CreateDirectory(outputPath);
            }

            var skeletonData = skeletonDataAsset.GetSkeletonData(true);
            var hasAnimations = bakeAnimations && skeletonData.Animations.Count > 0;
            UnityEditor.Animations.AnimatorController controller = null;
            if (hasAnimations)
            {
                var controllerPath = outputPath + "/" + skeletonDataAsset.skeletonJSON.name + " Controller.controller";
                var newAnimContainer = false;

                var runtimeController = AssetDatabase.LoadAssetAtPath(controllerPath, typeof(RuntimeAnimatorController));

                if (runtimeController != null)
                {
                    controller = (UnityEditor.Animations.AnimatorController)runtimeController;
                }
                else
                {
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    newAnimContainer = true;
                }

                var existingClipTable = new Dictionary<string, AnimationClip>();
                var unusedClipNames = new List<string>();
                var animObjs = AssetDatabase.LoadAllAssetsAtPath(controllerPath);

                foreach (var o in animObjs)
                {
                    if (o is AnimationClip)
                    {
                        var clip = (AnimationClip)o;
                        existingClipTable.Add(clip.name, clip);
                        unusedClipNames.Add(clip.name);
                    }
                }

                var slotLookup = new Dictionary<int, List<string>>();

                var skinCount = skins.Count;

                for (var s = 0; s < skeletonData.Slots.Count; s++)
                {
                    var attachmentNames = new List<string>();
                    for (var i = 0; i < skinCount; i++)
                    {
                        var skin = skins.Items[i];
                        var skinEntries = new List<Skin.SkinEntry>();
                        skin.GetAttachments(s, skinEntries);
                        foreach (var entry in skinEntries)
                        {
                            if (!attachmentNames.Contains(entry.Name))
                                attachmentNames.Add(entry.Name);
                        }
                    }
                    slotLookup.Add(s, attachmentNames);
                }

                foreach (var anim in skeletonData.Animations)
                {

                    AnimationClip clip = null;
                    if (existingClipTable.ContainsKey(anim.Name))
                    {
                        clip = existingClipTable[anim.Name];
                    }

                    clip = ExtractAnimation(anim.Name, skeletonData, slotLookup, bakeIK, eventOptions, clip);

                    if (unusedClipNames.Contains(clip.name))
                    {
                        unusedClipNames.Remove(clip.name);
                    }
                    else
                    {
                        AssetDatabase.AddObjectToAsset(clip, controller);
                        controller.AddMotion(clip);
                    }
                }

                if (newAnimContainer)
                {
                    EditorUtility.SetDirty(controller);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(controllerPath, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.Refresh();
                }
                else
                {

                    foreach (var str in unusedClipNames)
                    {
                        AnimationClip.DestroyImmediate(existingClipTable[str], true);
                    }

                    EditorUtility.SetDirty(controller);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(controllerPath, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.Refresh();
                }
            }

            foreach (var skin in skins)
            {
                var newPrefab = false;

                var prefabPath = outputPath + "/" + skeletonDataAsset.skeletonJSON.name + " (" + skin.Name + ").prefab";

                var prefab = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));

                if (prefab == null)
                {
#if NEW_PREFAB_SYSTEM
                    var emptyGameObject = new GameObject();
                    prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(emptyGameObject, prefabPath, InteractionMode.AutomatedAction);
                    GameObject.DestroyImmediate(emptyGameObject);
#else
					prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
#endif
                    newPrefab = true;
                }

                var meshTable = new Dictionary<string, Mesh>();
                var unusedMeshNames = new List<string>();
                var assets = AssetDatabase.LoadAllAssetsAtPath(prefabPath);
                foreach (var obj in assets)
                {
                    if (obj is Mesh)
                    {
                        meshTable.Add(obj.name, (Mesh)obj);
                        unusedMeshNames.Add(obj.name);
                    }
                }

                var prefabRoot = EditorInstantiation.NewGameObject("root", true);

                var slotTable = new Dictionary<string, Transform>();
                var boneTable = new Dictionary<string, Transform>();
                var boneList = new List<Transform>();

                //create bones
                for (var i = 0; i < skeletonData.Bones.Count; i++)
                {
                    var boneData = skeletonData.Bones.Items[i];
                    var boneTransform = EditorInstantiation.NewGameObject(boneData.Name, true).transform;
                    boneTransform.parent = prefabRoot.transform;
                    boneTable.Add(boneTransform.name, boneTransform);
                    boneList.Add(boneTransform);
                }

                for (var i = 0; i < skeletonData.Bones.Count; i++)
                {

                    var boneData = skeletonData.Bones.Items[i];
                    var boneTransform = boneTable[boneData.Name];
                    Transform parentTransform = null;
                    if (i > 0)
                        parentTransform = boneTable[boneData.Parent.Name];
                    else
                        parentTransform = boneTransform.parent;

                    boneTransform.parent = parentTransform;
                    boneTransform.localPosition = new Vector3(boneData.X, boneData.Y, 0);
                    var tm = boneData.TransformMode;
                    if (tm.InheritsRotation())
                        boneTransform.localRotation = Quaternion.Euler(0, 0, boneData.Rotation);
                    else
                        boneTransform.rotation = Quaternion.Euler(0, 0, boneData.Rotation);

                    if (tm.InheritsScale())
                        boneTransform.localScale = new Vector3(boneData.ScaleX, boneData.ScaleY, 1);
                }

                //create slots and attachments
                for (var slotIndex = 0; slotIndex < skeletonData.Slots.Count; slotIndex++)
                {
                    var slotData = skeletonData.Slots.Items[slotIndex];
                    var slotTransform = EditorInstantiation.NewGameObject(slotData.Name, true).transform;
                    slotTransform.parent = prefabRoot.transform;
                    slotTable.Add(slotData.Name, slotTransform);

                    var skinEntries = new List<Skin.SkinEntry>();
                    skin.GetAttachments(slotIndex, skinEntries);
                    if (skin != skeletonData.DefaultSkin)
                        skeletonData.DefaultSkin.GetAttachments(slotIndex, skinEntries);

                    for (var a = 0; a < skinEntries.Count; a++)
                    {
                        var attachment = skinEntries[a].Attachment;
                        var attachmentName = skinEntries[a].Name;
                        var attachmentMeshName = "[" + slotData.Name + "] " + attachmentName;
                        var offset = Vector3.zero;
                        float rotation = 0;
                        Mesh mesh = null;
                        Material material = null;
                        var isWeightedMesh = false;

                        if (meshTable.ContainsKey(attachmentMeshName))
                            mesh = meshTable[attachmentMeshName];
                        if (attachment is RegionAttachment)
                        {
                            var regionAttachment = (RegionAttachment)attachment;
                            offset.x = regionAttachment.X;
                            offset.y = regionAttachment.Y;
                            rotation = regionAttachment.Rotation;
                            mesh = ExtractRegionAttachment(attachmentMeshName, regionAttachment, mesh);
                            material = attachment.GetMaterial();
                            unusedMeshNames.Remove(attachmentMeshName);
                            if (newPrefab || meshTable.ContainsKey(attachmentMeshName) == false)
                                AssetDatabase.AddObjectToAsset(mesh, prefab);
                        }
                        else if (attachment is MeshAttachment)
                        {
                            var meshAttachment = (MeshAttachment)attachment;
                            isWeightedMesh = (meshAttachment.Bones != null);
                            offset.x = 0;
                            offset.y = 0;
                            rotation = 0;

                            if (isWeightedMesh)
                                mesh = ExtractWeightedMeshAttachment(attachmentMeshName, meshAttachment, slotIndex, skeletonData, boneList, mesh);
                            else
                                mesh = ExtractMeshAttachment(attachmentMeshName, meshAttachment, mesh);

                            material = attachment.GetMaterial();
                            unusedMeshNames.Remove(attachmentMeshName);
                            if (newPrefab || meshTable.ContainsKey(attachmentMeshName) == false)
                                AssetDatabase.AddObjectToAsset(mesh, prefab);
                        }
                        else
                            continue;

                        var attachmentTransform = EditorInstantiation.NewGameObject(attachmentName, true).transform;

                        attachmentTransform.parent = slotTransform;
                        attachmentTransform.localPosition = offset;
                        attachmentTransform.localRotation = Quaternion.Euler(0, 0, rotation);

                        if (isWeightedMesh)
                        {
                            attachmentTransform.position = Vector3.zero;
                            attachmentTransform.rotation = Quaternion.identity;
                            var skinnedMeshRenderer = attachmentTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
                            skinnedMeshRenderer.rootBone = boneList[0];
                            skinnedMeshRenderer.bones = boneList.ToArray();
                            skinnedMeshRenderer.sharedMesh = mesh;
                        }
                        else
                        {
                            attachmentTransform.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                            attachmentTransform.gameObject.AddComponent<MeshRenderer>();
                        }

                        attachmentTransform.GetComponent<Renderer>().sharedMaterial = material;
                        attachmentTransform.GetComponent<Renderer>().sortingOrder = slotIndex;

                        if (attachmentName != slotData.AttachmentName)
                            attachmentTransform.gameObject.SetActive(false);
                    }

                }

                foreach (var slotData in skeletonData.Slots)
                {
                    var slotTransform = slotTable[slotData.Name];
                    slotTransform.parent = boneTable[slotData.BoneData.Name];
                    slotTransform.localPosition = Vector3.zero;
                    slotTransform.localRotation = Quaternion.identity;
                    slotTransform.localScale = Vector3.one;
                }

                if (hasAnimations)
                {
                    var animator = prefabRoot.AddComponent<Animator>();
                    animator.applyRootMotion = false;
                    animator.runtimeAnimatorController = controller;
                    EditorGUIUtility.PingObject(controller);
                }

                if (newPrefab)
                {
#if NEW_PREFAB_SYSTEM
                    PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, prefabPath, InteractionMode.AutomatedAction);
#else
					PrefabUtility.ReplacePrefab(prefabRoot, prefab, ReplacePrefabOptions.ConnectToPrefab);
#endif
                }
                else
                {

                    foreach (var str in unusedMeshNames)
                    {
                        Mesh.DestroyImmediate(meshTable[str], true);
                    }

#if NEW_PREFAB_SYSTEM
                    PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, prefabPath, InteractionMode.AutomatedAction);
#else
					PrefabUtility.ReplacePrefab(prefabRoot, prefab, ReplacePrefabOptions.ReplaceNameBased);
#endif
                }


                EditorGUIUtility.PingObject(prefab);

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                GameObject.DestroyImmediate(prefabRoot);

            }
        }

        #region Attachment Baking
        private static Bone DummyBone;
        private static Slot DummySlot;

        internal static Bone GetDummyBone()
        {
            if (DummyBone != null)
                return DummyBone;

            var skelData = new SkeletonData();
            var data = new BoneData(0, "temp", null)
            {
                ScaleX = 1,
                ScaleY = 1,
                Length = 100
            };

            skelData.Bones.Add(data);

            var skeleton = new Skeleton(skelData);

            var bone = new Bone(data, skeleton, null);
            bone.UpdateWorldTransform();

            DummyBone = bone;

            return DummyBone;
        }

        internal static Slot GetDummySlot()
        {
            if (DummySlot != null)
                return DummySlot;

            var bone = GetDummyBone();

            var data = new SlotData(0, "temp", bone.Data);
            var slot = new Slot(data, bone);
            DummySlot = slot;
            return DummySlot;
        }

        internal static Mesh ExtractRegionAttachment(string name, RegionAttachment attachment, Mesh mesh = null, bool centered = true)
        {
            var bone = GetDummyBone();

            if (centered)
            {
                bone.X = -attachment.X;
                bone.Y = -attachment.Y;
            }

            bone.UpdateWorldTransform();

            var uvs = ExtractUV(attachment.UVs);
            var floatVerts = new float[8];
            attachment.ComputeWorldVertices(bone, floatVerts, 0);
            var verts = ExtractVerts(floatVerts);

            //unrotate verts now that they're centered
            if (centered)
            {
                for (var i = 0; i < verts.Length; i++)
                    verts[i] = Quaternion.Euler(0, 0, -attachment.Rotation) * verts[i];
            }

            int[] triangles = { 1, 3, 0, 2, 3, 1 };
            var color = attachment.GetColor();

            if (mesh == null)
                mesh = new Mesh();

            mesh.triangles = new int[0];

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.colors = new[] { color, color, color, color };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.name = name;

            return mesh;
        }

        internal static Mesh ExtractMeshAttachment(string name, MeshAttachment attachment, Mesh mesh = null)
        {
            var slot = GetDummySlot();

            slot.Bone.X = 0;
            slot.Bone.Y = 0;
            slot.Bone.UpdateWorldTransform();

            var uvs = ExtractUV(attachment.UVs);
            var floatVerts = new float[attachment.WorldVerticesLength];
            attachment.ComputeWorldVertices(slot, floatVerts);
            var verts = ExtractVerts(floatVerts);

            var triangles = attachment.Triangles;
            var color = attachment.GetColor();

            if (mesh == null)
                mesh = new Mesh();

            mesh.triangles = new int[0];

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            var colors = new Color[verts.Length];
            for (var i = 0; i < verts.Length; i++)
                colors[i] = color;

            mesh.colors = colors;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.name = name;

            return mesh;
        }

        public class BoneWeightContainer
        {
            public struct Pair
            {
                public Transform bone;
                public float weight;

                public Pair(Transform bone, float weight)
                {
                    this.bone = bone;
                    this.weight = weight;
                }
            }

            public List<Transform> bones;
            public List<float> weights;
            public List<Pair> pairs;


            public BoneWeightContainer()
            {
                this.bones = new List<Transform>();
                this.weights = new List<float>();
                this.pairs = new List<Pair>();
            }

            public void Add(Transform transform, float weight)
            {
                this.bones.Add(transform);
                this.weights.Add(weight);

                this.pairs.Add(new Pair(transform, weight));
            }
        }

        internal static Mesh ExtractWeightedMeshAttachment(string name, MeshAttachment attachment, int slotIndex, SkeletonData skeletonData, List<Transform> boneList, Mesh mesh = null)
        {
            if (!attachment.IsWeighted())
                throw new System.ArgumentException("Mesh is not weighted.", "attachment");

            var skeleton = new Skeleton(skeletonData);
            skeleton.UpdateWorldTransform();

            var floatVerts = new float[attachment.WorldVerticesLength];
            attachment.ComputeWorldVertices(skeleton.Slots.Items[slotIndex], floatVerts);

            var uvs = ExtractUV(attachment.UVs);
            var verts = ExtractVerts(floatVerts);

            var triangles = attachment.Triangles;
            var color = new Color(attachment.R, attachment.G, attachment.B, attachment.A);

            mesh = (mesh == null) ? new Mesh() : mesh;

            mesh.triangles = new int[0];

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            var colors = new Color[verts.Length];
            for (var i = 0; i < verts.Length; i++)
                colors[i] = color;

            mesh.colors = colors;
            mesh.name = name;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Handle weights and binding
            var weightTable = new Dictionary<int, BoneWeightContainer>();
            var warningBuilder = new System.Text.StringBuilder();

            var bones = attachment.Bones;
            var weights = attachment.Vertices;
            for (int w = 0, v = 0, b = 0, n = bones.Length; v < n; w += 2)
            {

                var nn = bones[v++] + v;
                for (; v < nn; v++, b += 3)
                {
                    var boneTransform = boneList[bones[v]];
                    var vIndex = w / 2;
                    BoneWeightContainer container;
                    if (weightTable.ContainsKey(vIndex))
                        container = weightTable[vIndex];
                    else
                    {
                        container = new BoneWeightContainer();
                        weightTable.Add(vIndex, container);
                    }

                    var weight = weights[b + 2];
                    container.Add(boneTransform, weight);
                }
            }

            var boneWeights = new BoneWeight[weightTable.Count];

            for (var i = 0; i < weightTable.Count; i++)
            {
                var bw = new BoneWeight();
                var container = weightTable[i];

                var pairs = container.pairs.OrderByDescending(pair => pair.weight).ToList();

                for (var b = 0; b < pairs.Count; b++)
                {
                    if (b > 3)
                    {
                        if (warningBuilder.Length == 0)
                            warningBuilder.Insert(0, "[Weighted Mesh: " + name + "]\r\nUnity only supports 4 weight influences per vertex! The 4 strongest influences will be used.\r\n");

                        warningBuilder.AppendFormat("{0} ignored on vertex {1}!\r\n", pairs[b].bone.name, i);
                        continue;
                    }

                    var boneIndex = boneList.IndexOf(pairs[b].bone);
                    var weight = pairs[b].weight;

                    switch (b)
                    {
                        case 0:
                            bw.boneIndex0 = boneIndex;
                            bw.weight0 = weight;
                            break;
                        case 1:
                            bw.boneIndex1 = boneIndex;
                            bw.weight1 = weight;
                            break;
                        case 2:
                            bw.boneIndex2 = boneIndex;
                            bw.weight2 = weight;
                            break;
                        case 3:
                            bw.boneIndex3 = boneIndex;
                            bw.weight3 = weight;
                            break;
                    }
                }

                boneWeights[i] = bw;
            }

            var bindPoses = new Matrix4x4[boneList.Count];
            for (var i = 0; i < boneList.Count; i++)
            {
                bindPoses[i] = boneList[i].worldToLocalMatrix;
            }

            mesh.boneWeights = boneWeights;
            mesh.bindposes = bindPoses;

            var warningString = warningBuilder.ToString();
            if (warningString.Length > 0)
                Debug.LogWarning(warningString);


            return mesh;
        }

        internal static Vector2[] ExtractUV(float[] floats)
        {
            var arr = new Vector2[floats.Length / 2];

            for (var i = 0; i < floats.Length; i += 2)
            {
                arr[i / 2] = new Vector2(floats[i], floats[i + 1]);
            }

            return arr;
        }

        internal static Vector3[] ExtractVerts(float[] floats)
        {
            var arr = new Vector3[floats.Length / 2];

            for (var i = 0; i < floats.Length; i += 2)
            {
                arr[i / 2] = new Vector3(floats[i], floats[i + 1], 0);// *scale;
            }

            return arr;
        }
        #endregion

        #region Animation Baking
        private static AnimationClip ExtractAnimation(string name, SkeletonData skeletonData, Dictionary<int, List<string>> slotLookup, bool bakeIK, SendMessageOptions eventOptions, AnimationClip clip = null)
        {
            var animation = skeletonData.FindAnimation(name);

            var timelines = animation.Timelines;

            if (clip == null)
            {
                clip = new AnimationClip();
            }
            else
            {
                clip.ClearCurves();
                AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
            }

            clip.name = name;

            var skeleton = new Skeleton(skeletonData);

            var ignoreRotateTimelineIndexes = new List<int>();

            if (bakeIK)
            {
                foreach (var i in skeleton.IkConstraints)
                {
                    foreach (var b in i.Bones)
                    {
                        var index = skeleton.FindBoneIndex(b.Data.Name);
                        ignoreRotateTimelineIndexes.Add(index);
                        BakeBoneConstraints(b, animation, clip);
                    }
                }
            }

            foreach (var b in skeleton.Bones)
            {
                if (!b.Data.TransformMode.InheritsRotation())
                {
                    var index = skeleton.FindBoneIndex(b.Data.Name);

                    if (ignoreRotateTimelineIndexes.Contains(index) == false)
                    {
                        ignoreRotateTimelineIndexes.Add(index);
                        BakeBoneConstraints(b, animation, clip);
                    }
                }
            }

            foreach (var t in timelines)
            {
                skeleton.SetToSetupPose();

                if (t is ScaleTimeline)
                {
                    ParseScaleTimeline(skeleton, (ScaleTimeline)t, clip);
                }
                else if (t is TranslateTimeline)
                {
                    ParseTranslateTimeline(skeleton, (TranslateTimeline)t, clip);
                }
                else if (t is RotateTimeline)
                {
                    //bypass any rotation keys if they're going to get baked anyway to prevent localEulerAngles vs Baked collision
                    if (ignoreRotateTimelineIndexes.Contains(((RotateTimeline)t).BoneIndex) == false)
                        ParseRotateTimeline(skeleton, (RotateTimeline)t, clip);
                }
                else if (t is AttachmentTimeline)
                {
                    ParseAttachmentTimeline(skeleton, (AttachmentTimeline)t, slotLookup, clip);
                }
                else if (t is EventTimeline)
                {
                    ParseEventTimeline((EventTimeline)t, clip, eventOptions);
                }

            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.stopTime = Mathf.Max(clip.length, 0.001f);

            SetAnimationSettings(clip, settings);

            clip.EnsureQuaternionContinuity();

            EditorUtility.SetDirty(clip);

            return clip;
        }

        private static int BinarySearch(float[] values, float target)
        {
            var low = 0;
            var high = values.Length - 2;
            if (high == 0) return 1;
            var current = (int)((uint)high >> 1);
            while (true)
            {
                if (values[(current + 1)] <= target)
                    low = current + 1;
                else
                    high = current;

                if (low == high) return (low + 1);
                current = (int)((uint)(low + high) >> 1);
            }
        }

        private static void BakeBoneConstraints(Bone bone, Spine.Animation animation, AnimationClip clip)
        {
            var skeleton = bone.Skeleton;
            var inheritRotation = bone.Data.TransformMode.InheritsRotation();

            animation.Apply(skeleton, 0, 0, false, null, 1f, MixBlend.Setup, MixDirection.In);
            skeleton.UpdateWorldTransform();
            var duration = animation.Duration;

            var curve = new AnimationCurve();

            var keys = new List<Keyframe>();

            var rotation = bone.AppliedRotation;
            if (!inheritRotation)
                rotation = GetUninheritedAppliedRotation(bone);

            keys.Add(new Keyframe(0, rotation, 0, 0));

            var listIndex = 1;

            var r = rotation;

            var steps = Mathf.CeilToInt(duration / BakeIncrement);

            float currentTime = 0;
            var angle = rotation;

            for (var i = 1; i <= steps; i++)
            {
                currentTime += BakeIncrement;
                if (i == steps)
                    currentTime = duration;

                animation.Apply(skeleton, 0, currentTime, true, null, 1f, MixBlend.Setup, MixDirection.In);
                skeleton.UpdateWorldTransform();

                var pIndex = listIndex - 1;

                var pk = keys[pIndex];

                pk = keys[pIndex];

                rotation = inheritRotation ? bone.AppliedRotation : GetUninheritedAppliedRotation(bone);

                angle += Mathf.DeltaAngle(angle, rotation);

                r = angle;

                var rOut = (r - pk.value) / (currentTime - pk.time);

                pk.outTangent = rOut;

                keys.Add(new Keyframe(currentTime, r, rOut, 0));

                keys[pIndex] = pk;

                listIndex++;
            }

            curve = EnsureCurveKeyCount(new AnimationCurve(keys.ToArray()));

            var path = GetPath(bone.Data);
            var propertyName = "localEulerAnglesBaked";

            var xBind = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName + ".x");
            AnimationUtility.SetEditorCurve(clip, xBind, new AnimationCurve());
            var yBind = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName + ".y");
            AnimationUtility.SetEditorCurve(clip, yBind, new AnimationCurve());
            var zBind = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName + ".z");
            AnimationUtility.SetEditorCurve(clip, zBind, curve);
        }

        private static void ParseTranslateTimeline(Skeleton skeleton, TranslateTimeline timeline, AnimationClip clip)
        {
            var boneData = skeleton.Data.Bones.Items[timeline.BoneIndex];
            var bone = skeleton.Bones.Items[timeline.BoneIndex];

            var xCurve = new AnimationCurve();
            var yCurve = new AnimationCurve();
            var zCurve = new AnimationCurve();

            var endTime = timeline.Frames[(timeline.FrameCount * 3) - 3];

            var currentTime = timeline.Frames[0];

            var xKeys = new List<Keyframe>();
            var yKeys = new List<Keyframe>();

            xKeys.Add(new Keyframe(timeline.Frames[0], timeline.Frames[1] + boneData.X, 0, 0));
            yKeys.Add(new Keyframe(timeline.Frames[0], timeline.Frames[2] + boneData.Y, 0, 0));

            var listIndex = 1;
            var frameIndex = 1;
            var f = 3;
            var frames = timeline.Frames;
            skeleton.SetToSetupPose();
            float lastTime = 0;
            while (currentTime < endTime)
            {
                var pIndex = listIndex - 1;

                var curveType = timeline.GetCurveType(frameIndex - 1);
                if (curveType == 0)
                {
                    //linear
                    var px = xKeys[pIndex];
                    var py = yKeys[pIndex];

                    var time = frames[f];
                    var x = frames[f + 1] + boneData.X;
                    var y = frames[f + 2] + boneData.Y;

                    var xOut = (x - px.value) / (time - px.time);
                    var yOut = (y - py.value) / (time - py.time);

                    px.outTangent = xOut;
                    py.outTangent = yOut;

                    xKeys.Add(new Keyframe(time, x, xOut, 0));
                    yKeys.Add(new Keyframe(time, y, yOut, 0));

                    xKeys[pIndex] = px;
                    yKeys[pIndex] = py;

                    currentTime = time;

                    timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);

                    lastTime = time;
                    listIndex++;
                }
                else if (curveType == 1)
                {
                    //stepped
                    var px = xKeys[pIndex];
                    var py = yKeys[pIndex];

                    var time = frames[f];
                    var x = frames[f + 1] + boneData.X;
                    var y = frames[f + 2] + boneData.Y;

                    var xOut = float.PositiveInfinity;
                    var yOut = float.PositiveInfinity;

                    px.outTangent = xOut;
                    py.outTangent = yOut;

                    xKeys.Add(new Keyframe(time, x, xOut, 0));
                    yKeys.Add(new Keyframe(time, y, yOut, 0));

                    xKeys[pIndex] = px;
                    yKeys[pIndex] = py;

                    currentTime = time;

                    timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);

                    lastTime = time;
                    listIndex++;
                }
                else if (curveType == 2)
                {

                    //bezier
                    var px = xKeys[pIndex];
                    var py = yKeys[pIndex];

                    var time = frames[f];

                    var steps = Mathf.FloorToInt((time - px.time) / BakeIncrement);

                    for (var i = 1; i <= steps; i++)
                    {
                        currentTime += BakeIncrement;
                        if (i == steps)
                            currentTime = time;

                        timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);

                        px = xKeys[listIndex - 1];
                        py = yKeys[listIndex - 1];

                        var xOut = (bone.X - px.value) / (currentTime - px.time);
                        var yOut = (bone.Y - py.value) / (currentTime - py.time);

                        px.outTangent = xOut;
                        py.outTangent = yOut;

                        xKeys.Add(new Keyframe(currentTime, bone.X, xOut, 0));
                        yKeys.Add(new Keyframe(currentTime, bone.Y, yOut, 0));

                        xKeys[listIndex - 1] = px;
                        yKeys[listIndex - 1] = py;

                        listIndex++;
                        lastTime = currentTime;
                    }
                }

                frameIndex++;
                f += 3;
            }

            xCurve = EnsureCurveKeyCount(new AnimationCurve(xKeys.ToArray()));
            yCurve = EnsureCurveKeyCount(new AnimationCurve(yKeys.ToArray()));



            var path = GetPath(boneData);
            const string propertyName = "localPosition";

            clip.SetCurve(path, typeof(Transform), propertyName + ".x", xCurve);
            clip.SetCurve(path, typeof(Transform), propertyName + ".y", yCurve);
            clip.SetCurve(path, typeof(Transform), propertyName + ".z", zCurve);
        }

        private static void ParseScaleTimeline(Skeleton skeleton, ScaleTimeline timeline, AnimationClip clip)
        {
            var boneData = skeleton.Data.Bones.Items[timeline.BoneIndex];
            var bone = skeleton.Bones.Items[timeline.BoneIndex];

            var xCurve = new AnimationCurve();
            var yCurve = new AnimationCurve();
            var zCurve = new AnimationCurve();

            var endTime = timeline.Frames[(timeline.FrameCount * 3) - 3];

            var currentTime = timeline.Frames[0];

            var xKeys = new List<Keyframe>();
            var yKeys = new List<Keyframe>();

            xKeys.Add(new Keyframe(timeline.Frames[0], timeline.Frames[1] * boneData.ScaleX, 0, 0));
            yKeys.Add(new Keyframe(timeline.Frames[0], timeline.Frames[2] * boneData.ScaleY, 0, 0));

            var listIndex = 1;
            var frameIndex = 1;
            var f = 3;
            var frames = timeline.Frames;
            skeleton.SetToSetupPose();
            float lastTime = 0;
            while (currentTime < endTime)
            {
                var pIndex = listIndex - 1;
                var curveType = timeline.GetCurveType(frameIndex - 1);

                if (curveType == 0)
                {
                    //linear
                    var px = xKeys[pIndex];
                    var py = yKeys[pIndex];

                    var time = frames[f];
                    var x = frames[f + 1] * boneData.ScaleX;
                    var y = frames[f + 2] * boneData.ScaleY;

                    var xOut = (x - px.value) / (time - px.time);
                    var yOut = (y - py.value) / (time - py.time);

                    px.outTangent = xOut;
                    py.outTangent = yOut;

                    xKeys.Add(new Keyframe(time, x, xOut, 0));
                    yKeys.Add(new Keyframe(time, y, yOut, 0));

                    xKeys[pIndex] = px;
                    yKeys[pIndex] = py;

                    currentTime = time;

                    timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);

                    lastTime = time;
                    listIndex++;
                }
                else if (curveType == 1)
                {
                    //stepped
                    var px = xKeys[pIndex];
                    var py = yKeys[pIndex];

                    var time = frames[f];
                    var x = frames[f + 1] * boneData.ScaleX;
                    var y = frames[f + 2] * boneData.ScaleY;

                    var xOut = float.PositiveInfinity;
                    var yOut = float.PositiveInfinity;

                    px.outTangent = xOut;
                    py.outTangent = yOut;

                    xKeys.Add(new Keyframe(time, x, xOut, 0));
                    yKeys.Add(new Keyframe(time, y, yOut, 0));

                    xKeys[pIndex] = px;
                    yKeys[pIndex] = py;

                    currentTime = time;

                    timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);

                    lastTime = time;
                    listIndex++;
                }
                else if (curveType == 2)
                {
                    //bezier
                    var px = xKeys[pIndex];
                    var py = yKeys[pIndex];

                    var time = frames[f];

                    var steps = Mathf.FloorToInt((time - px.time) / BakeIncrement);

                    for (var i = 1; i <= steps; i++)
                    {
                        currentTime += BakeIncrement;
                        if (i == steps)
                            currentTime = time;

                        timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);

                        px = xKeys[listIndex - 1];
                        py = yKeys[listIndex - 1];

                        var xOut = (bone.ScaleX - px.value) / (currentTime - px.time);
                        var yOut = (bone.ScaleY - py.value) / (currentTime - py.time);

                        px.outTangent = xOut;
                        py.outTangent = yOut;

                        xKeys.Add(new Keyframe(currentTime, bone.ScaleX, xOut, 0));
                        yKeys.Add(new Keyframe(currentTime, bone.ScaleY, yOut, 0));

                        xKeys[listIndex - 1] = px;
                        yKeys[listIndex - 1] = py;

                        listIndex++;
                        lastTime = currentTime;
                    }
                }

                frameIndex++;
                f += 3;
            }

            xCurve = EnsureCurveKeyCount(new AnimationCurve(xKeys.ToArray()));
            yCurve = EnsureCurveKeyCount(new AnimationCurve(yKeys.ToArray()));

            var path = GetPath(boneData);
            var propertyName = "localScale";

            clip.SetCurve(path, typeof(Transform), propertyName + ".x", xCurve);
            clip.SetCurve(path, typeof(Transform), propertyName + ".y", yCurve);
            clip.SetCurve(path, typeof(Transform), propertyName + ".z", zCurve);
        }

        private static void ParseRotateTimeline(Skeleton skeleton, RotateTimeline timeline, AnimationClip clip)
        {
            var boneData = skeleton.Data.Bones.Items[timeline.BoneIndex];
            var bone = skeleton.Bones.Items[timeline.BoneIndex];

            var curve = new AnimationCurve();

            var endTime = timeline.Frames[(timeline.FrameCount * 2) - 2];

            var currentTime = timeline.Frames[0];

            var keys = new List<Keyframe>();

            var rotation = timeline.Frames[1] + boneData.Rotation;

            keys.Add(new Keyframe(timeline.Frames[0], rotation, 0, 0));

            var listIndex = 1;
            var frameIndex = 1;
            var f = 2;
            var frames = timeline.Frames;
            skeleton.SetToSetupPose();
            float lastTime = 0;
            var angle = rotation;
            while (currentTime < endTime)
            {
                var pIndex = listIndex - 1;
                var curveType = timeline.GetCurveType(frameIndex - 1);

                if (curveType == 0)
                {
                    //linear
                    var pk = keys[pIndex];

                    var time = frames[f];

                    rotation = frames[f + 1] + boneData.Rotation;
                    angle += Mathf.DeltaAngle(angle, rotation);
                    var r = angle;

                    var rOut = (r - pk.value) / (time - pk.time);

                    pk.outTangent = rOut;

                    keys.Add(new Keyframe(time, r, rOut, 0));

                    keys[pIndex] = pk;

                    currentTime = time;

                    timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);

                    lastTime = time;
                    listIndex++;
                }
                else if (curveType == 1)
                {
                    //stepped

                    var pk = keys[pIndex];

                    var time = frames[f];

                    rotation = frames[f + 1] + boneData.Rotation;
                    angle += Mathf.DeltaAngle(angle, rotation);
                    var r = angle;

                    var rOut = float.PositiveInfinity;

                    pk.outTangent = rOut;

                    keys.Add(new Keyframe(time, r, rOut, 0));

                    keys[pIndex] = pk;

                    currentTime = time;

                    timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);

                    lastTime = time;
                    listIndex++;
                }
                else if (curveType == 2)
                {
                    //bezier
                    var pk = keys[pIndex];

                    var time = frames[f];

                    timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);
                    skeleton.UpdateWorldTransform();

                    rotation = frames[f + 1] + boneData.Rotation;
                    angle += Mathf.DeltaAngle(angle, rotation);
                    var r = angle;

                    var steps = Mathf.FloorToInt((time - pk.time) / BakeIncrement);

                    for (var i = 1; i <= steps; i++)
                    {
                        currentTime += BakeIncrement;
                        if (i == steps)
                            currentTime = time;

                        timeline.Apply(skeleton, lastTime, currentTime, null, 1, MixBlend.Setup, MixDirection.In);
                        skeleton.UpdateWorldTransform();
                        pk = keys[listIndex - 1];

                        rotation = bone.Rotation;
                        angle += Mathf.DeltaAngle(angle, rotation);
                        r = angle;

                        var rOut = (r - pk.value) / (currentTime - pk.time);

                        pk.outTangent = rOut;

                        keys.Add(new Keyframe(currentTime, r, rOut, 0));

                        keys[listIndex - 1] = pk;

                        listIndex++;
                        lastTime = currentTime;
                    }
                }

                frameIndex++;
                f += 2;
            }

            curve = EnsureCurveKeyCount(new AnimationCurve(keys.ToArray()));

            var path = GetPath(boneData);
            const string propertyName = "localEulerAnglesBaked";

            var xBind = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName + ".x");
            AnimationUtility.SetEditorCurve(clip, xBind, new AnimationCurve());
            var yBind = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName + ".y");
            AnimationUtility.SetEditorCurve(clip, yBind, new AnimationCurve());
            var zBind = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName + ".z");
            AnimationUtility.SetEditorCurve(clip, zBind, curve);
        }

        private static void ParseEventTimeline(EventTimeline timeline, AnimationClip clip, SendMessageOptions eventOptions)
        {
            var frames = timeline.Frames;
            var events = timeline.Events;

            var animEvents = new List<AnimationEvent>();
            for (int i = 0, n = frames.Length; i < n; i++)
            {
                var spineEvent = events[i];
                var eventName = spineEvent.Data.Name;
                if (SpineEditorUtilities.Preferences.mecanimEventIncludeFolderName)
                    eventName = eventName.Replace("/", ""); // calls method FolderNameEventName()
                else
                    eventName = eventName.Substring(eventName.LastIndexOf('/') + 1); // calls method EventName()
                var unityAnimationEvent = new AnimationEvent
                {
                    time = frames[i],
                    functionName = eventName,
                    messageOptions = eventOptions
                };

                if (!string.IsNullOrEmpty(spineEvent.String))
                {
                    unityAnimationEvent.stringParameter = spineEvent.String;
                }
                else if (spineEvent.Int != 0)
                {
                    unityAnimationEvent.intParameter = spineEvent.Int;
                }
                else if (spineEvent.Float != 0)
                {
                    unityAnimationEvent.floatParameter = spineEvent.Float;
                } // else, paramless function/Action.

                animEvents.Add(unityAnimationEvent);
            }

            AnimationUtility.SetAnimationEvents(clip, animEvents.ToArray());
        }

        private static void ParseAttachmentTimeline(Skeleton skeleton, AttachmentTimeline timeline, Dictionary<int, List<string>> slotLookup, AnimationClip clip)
        {
            var attachmentNames = slotLookup[timeline.SlotIndex];

            var bonePath = GetPath(skeleton.Slots.Items[timeline.SlotIndex].Bone.Data);
            var slotPath = bonePath + "/" + skeleton.Slots.Items[timeline.SlotIndex].Data.Name;

            var curveTable = new Dictionary<string, AnimationCurve>();

            foreach (var str in attachmentNames)
            {
                curveTable.Add(str, new AnimationCurve());
            }

            var frames = timeline.Frames;

            if (frames[0] != 0)
            {
                var startingName = skeleton.Slots.Items[timeline.SlotIndex].Data.AttachmentName;
                foreach (var pair in curveTable)
                {
                    if (startingName == "" || startingName == null)
                    {
                        pair.Value.AddKey(new Keyframe(0, 0, float.PositiveInfinity, float.PositiveInfinity));
                    }
                    else
                    {
                        if (pair.Key == startingName)
                        {
                            pair.Value.AddKey(new Keyframe(0, 1, float.PositiveInfinity, float.PositiveInfinity));
                        }
                        else
                        {
                            pair.Value.AddKey(new Keyframe(0, 0, float.PositiveInfinity, float.PositiveInfinity));
                        }
                    }
                }
            }

            var currentTime = timeline.Frames[0];
            var endTime = frames[frames.Length - 1];
            var f = 0;
            while (currentTime < endTime)
            {
                var time = frames[f];

                var frameIndex = (time >= frames[frames.Length - 1] ? frames.Length : BinarySearch(frames, time)) - 1;

                var name = timeline.AttachmentNames[frameIndex];
                foreach (var pair in curveTable)
                {
                    if (name == "")
                    {
                        pair.Value.AddKey(new Keyframe(time, 0, float.PositiveInfinity, float.PositiveInfinity));
                    }
                    else
                    {
                        if (pair.Key == name)
                        {
                            pair.Value.AddKey(new Keyframe(time, 1, float.PositiveInfinity, float.PositiveInfinity));
                        }
                        else
                        {
                            pair.Value.AddKey(new Keyframe(time, 0, float.PositiveInfinity, float.PositiveInfinity));
                        }
                    }
                }

                currentTime = time;
                f += 1;
            }

            foreach (var pair in curveTable)
            {
                var path = slotPath + "/" + pair.Key;
                var prop = "m_IsActive";

                clip.SetCurve(path, typeof(GameObject), prop, pair.Value);
            }
        }

        private static AnimationCurve EnsureCurveKeyCount(AnimationCurve curve)
        {
            if (curve.length == 1)
                curve.AddKey(curve.keys[0].time + 0.25f, curve.keys[0].value);

            return curve;
        }

        private static float GetUninheritedAppliedRotation(Bone b)
        {
            var parent = b.Parent;
            var angle = b.AppliedRotation;

            while (parent != null)
            {
                angle -= parent.AppliedRotation;
                parent = parent.Parent;
            }

            return angle;
        }
        #endregion
        #endregion

        #region Region Baking
        public static GameObject BakeRegion(SpineAtlasAsset atlasAsset, AtlasRegion region, bool autoSave = true)
        {
            atlasAsset.GetAtlas(); // Initializes atlasAsset.

            var atlasAssetPath = AssetDatabase.GetAssetPath(atlasAsset);
            var atlasAssetDirPath = Path.GetDirectoryName(atlasAssetPath).Replace('\\', '/');
            var bakedDirPath = Path.Combine(atlasAssetDirPath, atlasAsset.name);
            var bakedPrefabPath = Path.Combine(bakedDirPath, AssetUtility.GetPathSafeName(region.name) + ".prefab").Replace("\\", "/");

            var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(bakedPrefabPath, typeof(GameObject));
            GameObject root;
            Mesh mesh;
            var isNewPrefab = false;

            if (!Directory.Exists(bakedDirPath))
                Directory.CreateDirectory(bakedDirPath);

            if (prefab == null)
            {
                root = EditorInstantiation.NewGameObject("temp", true, typeof(MeshFilter), typeof(MeshRenderer));
#if NEW_PREFAB_SYSTEM
                prefab = PrefabUtility.SaveAsPrefabAsset(root, bakedPrefabPath);
#else
				prefab = PrefabUtility.CreatePrefab(bakedPrefabPath, root);
#endif

                isNewPrefab = true;
                Object.DestroyImmediate(root);
            }

            mesh = (Mesh)AssetDatabase.LoadAssetAtPath(bakedPrefabPath, typeof(Mesh));

            mesh = atlasAsset.GenerateMesh(region.name, mesh, out var mat);
            if (isNewPrefab)
            {
                AssetDatabase.AddObjectToAsset(mesh, prefab);
                prefab.GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            EditorUtility.SetDirty(mesh);
            EditorUtility.SetDirty(prefab);

            if (autoSave)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            prefab.GetComponent<MeshRenderer>().sharedMaterial = mat;

            return prefab;
        }
        #endregion

        private static string GetPath(BoneData b)
        {
            return GetPathRecurse(b).Substring(1);
        }

        private static string GetPathRecurse(BoneData b)
        {
            if (b == null) return "";
            return GetPathRecurse(b.Parent) + "/" + b.Name;
        }

        private static void SetAnimationSettings(AnimationClip clip, AnimationClipSettings settings)
        {
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }


    }

}
