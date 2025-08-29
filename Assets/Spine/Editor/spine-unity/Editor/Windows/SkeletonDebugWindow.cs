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

// With contributions from: Mitch Thompson

#if UNITY_2018_3 || UNITY_2019 || UNITY_2018_3_OR_NEWER
#define NEW_PREFAB_SYSTEM
#else
#define NO_PREFAB_MESH
#endif

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace Spine.Unity.Editor
{
    using Icons = SpineEditorUtilities.Icons;

    public class SkeletonDebugWindow : EditorWindow
    {

        private const bool IsUtilityWindow = true;
        internal static bool showBoneNames, showPaths = true, showShapes = true, showConstraints = true;

        [MenuItem("CONTEXT/SkeletonRenderer/Open Skeleton Debug Window", false, 5000)]
        public static void Init()
        {
            var window = EditorWindow.GetWindow<SkeletonDebugWindow>(IsUtilityWindow);
            window.minSize = new Vector2(330f, 360f);
            window.maxSize = new Vector2(600f, 4000f);
            window.titleContent = new GUIContent("Skeleton Debug", Icons.spine);
            window.Show();
            window.OnSelectionChange();
        }


        private static readonly AnimBool showSkeleton = new AnimBool(true);
        private static readonly AnimBool showSlotsTree = new AnimBool(false);
        private static readonly AnimBool showConstraintsTree = new AnimBool(false);
        private static readonly AnimBool showDrawOrderTree = new AnimBool(false);
        private static readonly AnimBool showEventDataTree = new AnimBool(false);
        private static readonly AnimBool showDataTree = new AnimBool(false);
        private static readonly AnimBool showInspectBoneTree = new AnimBool(false);

        private Vector2 scrollPos;

        private GUIContent SlotsRootLabel, SkeletonRootLabel;
        private GUIStyle BoldFoldoutStyle;

        public SkeletonRenderer skeletonRenderer;
        private Skeleton skeleton;
        private Skin activeSkin;
        private bool isPrefab;

        private SerializedProperty bpo;
        private Bone bone;

        [SpineBone(dataField: "skeletonRenderer")]
        public string boneName;

        private readonly Dictionary<Slot, List<Skin.SkinEntry>> attachmentTable = new Dictionary<Slot, List<Skin.SkinEntry>>();

        private static bool staticLostValues = true;

        private void OnSceneGUI(SceneView sceneView)
        {
            if (this.skeleton == null || this.skeletonRenderer == null || !this.skeletonRenderer.valid || this.isPrefab)
                return;

            var transform = this.skeletonRenderer.transform;
            if (showPaths) SpineHandles.DrawPaths(transform, this.skeleton);
            if (showConstraints) SpineHandles.DrawConstraints(transform, this.skeleton);
            if (showBoneNames) SpineHandles.DrawBoneNames(transform, this.skeleton);
            if (showShapes) SpineHandles.DrawBoundingBoxes(transform, this.skeleton);

            if (this.bone != null)
            {
                SpineHandles.DrawBone(this.skeletonRenderer.transform, this.bone, 1.5f, Color.cyan);
                Handles.Label(this.bone.GetWorldPosition(this.skeletonRenderer.transform) + (Vector3.down * 0.15f), this.bone.Data.Name, SpineHandles.BoneNameStyle);
            }
        }

        private void OnSelectionChange()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= this.OnSceneGUI;
            SceneView.duringSceneGui += this.OnSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
			SceneView.onSceneGUIDelegate += this.OnSceneGUI;
#endif

            var noSkeletonRenderer = false;

            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
            {
                noSkeletonRenderer = true;
            }
            else
            {
                var selectedSkeletonRenderer = selectedObject.GetComponent<SkeletonRenderer>();
                if (selectedSkeletonRenderer == null)
                {
                    noSkeletonRenderer = true;
                }
                else if (this.skeletonRenderer != selectedSkeletonRenderer)
                {

                    this.bone = null;
                    if (this.skeletonRenderer != null && this.skeletonRenderer.SkeletonDataAsset != selectedSkeletonRenderer.SkeletonDataAsset)
                        this.boneName = null;

                    this.skeletonRenderer = selectedSkeletonRenderer;
                    this.skeletonRenderer.Initialize(false);
                    this.skeletonRenderer.LateUpdate();
                    this.skeleton = this.skeletonRenderer.skeleton;
#if NEW_PREFAB_SYSTEM
                    this.isPrefab = false;
#else
					isPrefab |= PrefabUtility.GetPrefabType(selectedObject) == PrefabType.Prefab;
#endif
                    this.UpdateAttachments();
                }
            }

            if (noSkeletonRenderer) this.Clear();
            this.Repaint();
        }

        private void Clear()
        {
            this.skeletonRenderer = null;
            this.skeleton = null;
            this.attachmentTable.Clear();
            this.isPrefab = false;
            this.boneName = string.Empty;
            this.bone = null;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= this.OnSceneGUI;
#else
			SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
#endif
        }

        private void OnDestroy()
        {
            this.Clear();
        }

        private static void FalseDropDown(string label, string stringValue, Texture2D icon = null, bool disabledGroup = false)
        {
            if (disabledGroup) EditorGUI.BeginDisabledGroup(true);
            var pos = EditorGUILayout.GetControlRect(true);
            pos = EditorGUI.PrefixLabel(pos, SpineInspectorUtility.TempContent(label));
            GUI.Button(pos, SpineInspectorUtility.TempContent(stringValue, icon), EditorStyles.popup);
            if (disabledGroup) EditorGUI.EndDisabledGroup();
        }

        // Window GUI
        private void OnGUI()
        {
            var requireRepaint = false;

            if (staticLostValues)
            {
                this.Clear();
                this.OnSelectionChange();
                staticLostValues = false;
                requireRepaint = true;
            }

            if (this.SlotsRootLabel == null)
            {
                this.SlotsRootLabel = new GUIContent("Slots", Icons.slotRoot);
                this.SkeletonRootLabel = new GUIContent("Skeleton", Icons.skeleton);
                this.BoldFoldoutStyle = new GUIStyle(EditorStyles.foldout);
                this.BoldFoldoutStyle.fontStyle = FontStyle.Bold;
                this.BoldFoldoutStyle.stretchWidth = true;
                this.BoldFoldoutStyle.fixedWidth = 0;
            }


            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(SpineInspectorUtility.TempContent("Debug Selection", Icons.spine), this.skeletonRenderer, typeof(SkeletonRenderer), true);
            EditorGUI.EndDisabledGroup();

            if (this.skeleton == null || this.skeletonRenderer == null)
            {
                EditorGUILayout.HelpBox("No SkeletonRenderer Spine GameObject selected.", MessageType.Info);
                return;
            }

            if (this.isPrefab)
            {
                EditorGUILayout.HelpBox("SkeletonDebug only debugs Spine GameObjects in the scene.", MessageType.Warning);
                return;
            }

            if (!this.skeletonRenderer.valid)
            {
                EditorGUILayout.HelpBox("Spine Component is invalid. Check SkeletonData Asset.", MessageType.Error);
                return;
            }

            if (this.activeSkin != this.skeleton.Skin)
                this.UpdateAttachments();

            this.scrollPos = EditorGUILayout.BeginScrollView(this.scrollPos);

            using (new SpineInspectorUtility.BoxScope(false))
            {
                if (SpineInspectorUtility.CenteredButton(SpineInspectorUtility.TempContent("Skeleton.SetToSetupPose()")))
                {
                    this.skeleton.SetToSetupPose();
                    requireRepaint = true;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("Scene View", EditorStyles.boldLabel);
                using (new SpineInspectorUtility.LabelWidthScope())
                {
                    showBoneNames = EditorGUILayout.Toggle("Show Bone Names", showBoneNames);
                    showPaths = EditorGUILayout.Toggle("Show Paths", showPaths);
                    showShapes = EditorGUILayout.Toggle("Show Shapes", showShapes);
                    showConstraints = EditorGUILayout.Toggle("Show Constraints", showConstraints);
                }
                requireRepaint |= EditorGUI.EndChangeCheck();


                // Skeleton
                showSkeleton.target = EditorGUILayout.Foldout(showSkeleton.target, this.SkeletonRootLabel, this.BoldFoldoutStyle);
                if (showSkeleton.faded > 0)
                {
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        using (new EditorGUILayout.FadeGroupScope(showSkeleton.faded))
                        {
                            EditorGUI.BeginChangeCheck();

                            EditorGUI.BeginDisabledGroup(true);
                            FalseDropDown(".Skin", this.skeleton.Skin != null ? this.skeletonRenderer.Skeleton.Skin.Name : "<None>", Icons.skin);
                            EditorGUI.EndDisabledGroup();

                            // Flip
                            this.skeleton.ScaleX = EditorGUILayout.DelayedFloatField(".ScaleX", this.skeleton.ScaleX);
                            this.skeleton.ScaleY = EditorGUILayout.DelayedFloatField(".ScaleY", this.skeleton.ScaleY);
                            //EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(160f));
                            ////EditorGUILayout.LabelField("Scale", GUILayout.Width(EditorGUIUtility.labelWidth - 20f));
                            //GUILayout.EndHorizontal();

                            // Color
                            this.skeleton.SetColor(EditorGUILayout.ColorField(".R .G .B .A", this.skeleton.GetColor()));

                            requireRepaint |= EditorGUI.EndChangeCheck();
                        }
                    }
                }

                // Bone
                showInspectBoneTree.target = EditorGUILayout.Foldout(showInspectBoneTree.target, SpineInspectorUtility.TempContent("Bone", Icons.bone), this.BoldFoldoutStyle);
                if (showInspectBoneTree.faded > 0)
                {
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        using (new EditorGUILayout.FadeGroupScope(showInspectBoneTree.faded))
                        {
                            showBoneNames = EditorGUILayout.Toggle("Show Bone Names", showBoneNames);
                            if (this.bpo == null) this.bpo = new SerializedObject(this).FindProperty("boneName");
                            EditorGUILayout.PropertyField(this.bpo, SpineInspectorUtility.TempContent("Bone"));
                            if (!string.IsNullOrEmpty(this.bpo.stringValue))
                            {
                                if (this.bone == null || this.bone.Data.Name != this.bpo.stringValue)
                                {
                                    this.bone = this.skeleton.FindBone(this.bpo.stringValue);
                                }

                                if (this.bone != null)
                                {
                                    using (new EditorGUI.DisabledGroupScope(true))
                                    {
                                        var wm = EditorGUIUtility.wideMode;
                                        EditorGUIUtility.wideMode = true;
                                        EditorGUILayout.Slider("Local Rotation", ViewRound(this.bone.Rotation), -180f, 180f);
                                        EditorGUILayout.Vector2Field("Local Position", RoundVector2(this.bone.X, this.bone.Y));
                                        EditorGUILayout.Vector2Field("Local Scale", RoundVector2(this.bone.ScaleX, this.bone.ScaleY));
                                        EditorGUILayout.Vector2Field("Local Shear", RoundVector2(this.bone.ShearX, this.bone.ShearY));

                                        EditorGUILayout.Space();

                                        var boneParent = this.bone.Parent;
                                        if (boneParent != null) FalseDropDown("Parent", boneParent.Data.Name, Icons.bone);

                                        const string RoundFormat = "0.##";
                                        var lw = EditorGUIUtility.labelWidth;
                                        var fw = EditorGUIUtility.fieldWidth;
                                        EditorGUIUtility.labelWidth *= 0.25f;
                                        EditorGUIUtility.fieldWidth *= 0.5f;
                                        EditorGUILayout.LabelField("LocalToWorld");

                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.Space();
                                        EditorGUILayout.TextField(".A", this.bone.A.ToString(RoundFormat));
                                        EditorGUILayout.TextField(".B", this.bone.B.ToString(RoundFormat));
                                        EditorGUILayout.EndHorizontal();
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.Space();
                                        EditorGUILayout.TextField(".C", this.bone.C.ToString(RoundFormat));
                                        EditorGUILayout.TextField(".D", this.bone.D.ToString(RoundFormat));
                                        EditorGUILayout.EndHorizontal();

                                        EditorGUIUtility.labelWidth = lw * 0.5f;
                                        EditorGUILayout.BeginHorizontal();
                                        EditorGUILayout.Space();
                                        EditorGUILayout.Space();
                                        EditorGUILayout.TextField(".WorldX", this.bone.WorldX.ToString(RoundFormat));
                                        EditorGUILayout.TextField(".WorldY", this.bone.WorldY.ToString(RoundFormat));
                                        EditorGUILayout.EndHorizontal();

                                        EditorGUIUtility.labelWidth = lw;
                                        EditorGUIUtility.fieldWidth = fw;
                                        EditorGUIUtility.wideMode = wm;

                                    }
                                }
                                requireRepaint = true;
                            }
                            else
                            {
                                this.bone = null;
                            }
                        }
                    }
                }

                // Slots
                var preSlotsIndent = EditorGUI.indentLevel;
                showSlotsTree.target = EditorGUILayout.Foldout(showSlotsTree.target, this.SlotsRootLabel, this.BoldFoldoutStyle);
                if (showSlotsTree.faded > 0)
                {
                    using (new EditorGUILayout.FadeGroupScope(showSlotsTree.faded))
                    {
                        if (SpineInspectorUtility.CenteredButton(SpineInspectorUtility.TempContent("Skeleton.SetSlotsToSetupPose()")))
                        {
                            this.skeleton.SetSlotsToSetupPose();
                            requireRepaint = true;
                        }

                        var baseIndent = EditorGUI.indentLevel;
                        foreach (var pair in this.attachmentTable)
                        {
                            var slot = pair.Key;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.indentLevel = baseIndent + 1;
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(slot.Data.Name, Icons.slot), GUILayout.ExpandWidth(false));
                                EditorGUI.BeginChangeCheck();
                                var c = EditorGUILayout.ColorField(new Color(slot.R, slot.G, slot.B, slot.A), GUILayout.Width(60));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    slot.SetColor(c);
                                    requireRepaint = true;
                                }
                            }

                            foreach (var skinEntry in pair.Value)
                            {
                                var attachment = skinEntry.Attachment;
                                GUI.contentColor = slot.Attachment == attachment ? Color.white : Color.grey;
                                EditorGUI.indentLevel = baseIndent + 2;
                                var icon = Icons.GetAttachmentIcon(attachment);
                                var isAttached = (attachment == slot.Attachment);
                                var swap = EditorGUILayout.ToggleLeft(SpineInspectorUtility.TempContent(attachment.Name, icon), attachment == slot.Attachment);
                                if (isAttached != swap)
                                {
                                    slot.Attachment = isAttached ? null : attachment;
                                    requireRepaint = true;
                                }
                                GUI.contentColor = Color.white;
                            }
                        }
                    }
                }
                EditorGUI.indentLevel = preSlotsIndent;

                // Constraints
                const string NoneText = "<none>";
                showConstraintsTree.target = EditorGUILayout.Foldout(showConstraintsTree.target, SpineInspectorUtility.TempContent("Constraints", Icons.constraintRoot), this.BoldFoldoutStyle);
                if (showConstraintsTree.faded > 0)
                {
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        using (new EditorGUILayout.FadeGroupScope(showConstraintsTree.faded))
                        {
                            const float MixMin = 0f;
                            const float MixMax = 1f;
                            EditorGUI.BeginChangeCheck();
                            showConstraints = EditorGUILayout.Toggle("Show Constraints", showConstraints);
                            requireRepaint |= EditorGUI.EndChangeCheck();

                            EditorGUILayout.Space();

                            EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(string.Format("IK Constraints ({0})", this.skeleton.IkConstraints.Count), Icons.constraintIK), EditorStyles.boldLabel);
                            using (new SpineInspectorUtility.IndentScope())
                            {
                                if (this.skeleton.IkConstraints.Count > 0)
                                {
                                    foreach (var c in this.skeleton.IkConstraints)
                                    {
                                        EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(c.Data.Name, Icons.constraintIK));
                                        FalseDropDown("Goal", c.Data.Target.Name, Icons.bone, true);
                                        using (new EditorGUI.DisabledGroupScope(true))
                                        {
                                            EditorGUILayout.Toggle(SpineInspectorUtility.TempContent("Data.Uniform", tooltip: "Uniformly scales a bone when Ik stretches or compresses."), c.Data.Uniform);
                                        }

                                        EditorGUI.BeginChangeCheck();
                                        c.Mix = EditorGUILayout.Slider("Mix", c.Mix, MixMin, MixMax);
                                        c.BendDirection = EditorGUILayout.Toggle(SpineInspectorUtility.TempContent("Bend Clockwise", tooltip: "IkConstraint.BendDirection == 1 if clockwise; -1 if counterclockwise."), c.BendDirection > 0) ? 1 : -1;
                                        c.Compress = EditorGUILayout.Toggle(SpineInspectorUtility.TempContent("Compress", tooltip: "Compress single bone IK when the target too close. Not applied when parent bone has nonuniform scale."), c.Compress);
                                        c.Stretch = EditorGUILayout.Toggle(SpineInspectorUtility.TempContent("Stretch", tooltip: "Stretch the parent bone when the target is out of range. Not applied when parent bone has nonuniform scale."), c.Stretch);
                                        if (EditorGUI.EndChangeCheck()) requireRepaint = true;

                                        EditorGUILayout.Space();
                                    }

                                }
                                else
                                {
                                    EditorGUILayout.LabelField(NoneText);
                                }
                            }

                            EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(string.Format("Transform Constraints ({0})", this.skeleton.TransformConstraints.Count), Icons.constraintTransform), EditorStyles.boldLabel);
                            using (new SpineInspectorUtility.IndentScope())
                            {
                                if (this.skeleton.TransformConstraints.Count > 0)
                                {
                                    foreach (var c in this.skeleton.TransformConstraints)
                                    {
                                        EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(c.Data.Name, Icons.constraintTransform));
                                        EditorGUI.BeginDisabledGroup(true);
                                        FalseDropDown("Goal", c.Data.Target.Name, Icons.bone);
                                        EditorGUI.EndDisabledGroup();

                                        EditorGUI.BeginChangeCheck();
                                        c.TranslateMix = EditorGUILayout.Slider("TranslateMix", c.TranslateMix, MixMin, MixMax);
                                        c.RotateMix = EditorGUILayout.Slider("RotateMix", c.RotateMix, MixMin, MixMax);
                                        c.ScaleMix = EditorGUILayout.Slider("ScaleMix", c.ScaleMix, MixMin, MixMax);
                                        c.ShearMix = EditorGUILayout.Slider("ShearMix", c.ShearMix, MixMin, MixMax);
                                        if (EditorGUI.EndChangeCheck()) requireRepaint = true;

                                        EditorGUILayout.Space();
                                    }
                                }
                                else
                                {
                                    EditorGUILayout.LabelField(NoneText);
                                }
                            }

                            EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(string.Format("Path Constraints ({0})", this.skeleton.PathConstraints.Count), Icons.constraintPath), EditorStyles.boldLabel);

                            EditorGUI.BeginChangeCheck();
                            showPaths = EditorGUILayout.Toggle("Show Paths", showPaths);
                            requireRepaint |= EditorGUI.EndChangeCheck();

                            using (new SpineInspectorUtility.IndentScope())
                            {
                                if (this.skeleton.PathConstraints.Count > 0)
                                {
                                    foreach (var c in this.skeleton.PathConstraints)
                                    {
                                        EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(c.Data.Name, Icons.constraintPath));
                                        EditorGUI.BeginDisabledGroup(true);
                                        FalseDropDown("Path Slot", c.Data.Target.Name, Icons.slot);
                                        var activeAttachment = c.Target.Attachment;
                                        FalseDropDown("Active Path", activeAttachment != null ? activeAttachment.Name : "<None>", activeAttachment is PathAttachment ? Icons.path : null);
                                        EditorGUILayout.LabelField("PositionMode." + c.Data.PositionMode);
                                        EditorGUILayout.LabelField("SpacingMode." + c.Data.SpacingMode);
                                        EditorGUILayout.LabelField("RotateMode." + c.Data.RotateMode);
                                        EditorGUI.EndDisabledGroup();

                                        EditorGUI.BeginChangeCheck();
                                        c.RotateMix = EditorGUILayout.Slider("RotateMix", c.RotateMix, MixMin, MixMax);
                                        c.TranslateMix = EditorGUILayout.Slider("TranslateMix", c.TranslateMix, MixMin, MixMax);
                                        c.Position = EditorGUILayout.FloatField("Position", c.Position);
                                        c.Spacing = EditorGUILayout.FloatField("Spacing", c.Spacing);
                                        if (EditorGUI.EndChangeCheck()) requireRepaint = true;

                                        EditorGUILayout.Space();
                                    }

                                }
                                else
                                {
                                    EditorGUILayout.LabelField(NoneText);
                                }
                            }
                        }
                    }
                }

                showDrawOrderTree.target = EditorGUILayout.Foldout(showDrawOrderTree.target, SpineInspectorUtility.TempContent("Draw Order and Separators", Icons.slotRoot), this.BoldFoldoutStyle);

                //var separatorSlotNamesField =
                //SpineInspectorUtility.ge
                if (showDrawOrderTree.faded > 0)
                {
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        using (new EditorGUILayout.FadeGroupScope(showDrawOrderTree.faded))
                        {

                            const string SeparatorString = "------------- v SEPARATOR v -------------";

                            if (Application.isPlaying)
                            {
                                foreach (var slot in this.skeleton.DrawOrder)
                                {
                                    if (this.skeletonRenderer.separatorSlots.Contains(slot)) EditorGUILayout.LabelField(SeparatorString);

                                    using (new EditorGUI.DisabledScope(!slot.Bone.Active))
                                    {
                                        EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(slot.Data.Name, Icons.slot), GUILayout.ExpandWidth(false));
                                    }
                                }
                            }
                            else
                            {
                                foreach (var slot in this.skeleton.DrawOrder)
                                {
                                    var slotNames = SkeletonRendererInspector.GetSeparatorSlotNames(this.skeletonRenderer);
                                    for (int i = 0, n = slotNames.Length; i < n; i++)
                                    {
                                        if (string.Equals(slotNames[i], slot.Data.Name, System.StringComparison.Ordinal))
                                        {
                                            EditorGUILayout.LabelField(SeparatorString);
                                            break;
                                        }
                                    }
                                    using (new EditorGUI.DisabledScope(!slot.Bone.Active))
                                    {
                                        EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(slot.Data.Name, Icons.slot), GUILayout.ExpandWidth(false));
                                    }
                                }
                            }

                        }
                    }
                }

                showEventDataTree.target = EditorGUILayout.Foldout(showEventDataTree.target, SpineInspectorUtility.TempContent("Events", Icons.userEvent), this.BoldFoldoutStyle);
                if (showEventDataTree.faded > 0)
                {
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        using (new EditorGUILayout.FadeGroupScope(showEventDataTree.faded))
                        {
                            if (this.skeleton.Data.Events.Count > 0)
                            {
                                foreach (var e in this.skeleton.Data.Events)
                                {
                                    EditorGUILayout.LabelField(SpineInspectorUtility.TempContent(e.Name, Icons.userEvent));
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField(NoneText);
                            }
                        }
                    }
                }

                showDataTree.target = EditorGUILayout.Foldout(showDataTree.target, SpineInspectorUtility.TempContent("Data Counts", Icons.spine), this.BoldFoldoutStyle);
                if (showDataTree.faded > 0)
                {
                    using (new SpineInspectorUtility.IndentScope())
                    {
                        using (new EditorGUILayout.FadeGroupScope(showDataTree.faded))
                        {
                            using (new SpineInspectorUtility.LabelWidthScope())
                            {
                                var skeletonData = this.skeleton.Data;
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Bones", Icons.bone, "Skeleton.Data.Bones"), new GUIContent(skeletonData.Bones.Count.ToString()));
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Slots", Icons.slotRoot, "Skeleton.Data.Slots"), new GUIContent(skeletonData.Slots.Count.ToString()));
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Skins", Icons.skinsRoot, "Skeleton.Data.Skins"), new GUIContent(skeletonData.Skins.Count.ToString()));
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Events", Icons.userEvent, "Skeleton.Data.Events"), new GUIContent(skeletonData.Events.Count.ToString()));
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("IK Constraints", Icons.constraintIK, "Skeleton.Data.IkConstraints"), new GUIContent(skeletonData.IkConstraints.Count.ToString()));
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Transform Constraints", Icons.constraintTransform, "Skeleton.Data.TransformConstraints"), new GUIContent(skeletonData.TransformConstraints.Count.ToString()));
                                EditorGUILayout.LabelField(SpineInspectorUtility.TempContent("Path Constraints", Icons.constraintPath, "Skeleton.Data.PathConstraints"), new GUIContent(skeletonData.PathConstraints.Count.ToString()));
                            }
                        }
                    }
                }

                if (IsAnimating(showSlotsTree, showSkeleton, showConstraintsTree, showDrawOrderTree, showEventDataTree, showInspectBoneTree, showDataTree))
                    this.Repaint();
            }

            if (requireRepaint)
            {
                this.skeletonRenderer.LateUpdate();
                this.Repaint();
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndScrollView();
        }

        private static float ViewRound(float x)
        {
            const float Factor = 100f;
            const float Divisor = 1f / Factor;
            return Mathf.Round(x * Factor) * Divisor;
        }

        private static Vector2 RoundVector2(float x, float y)
        {
            const float Factor = 100f;
            const float Divisor = 1f / Factor;
            return new Vector2(Mathf.Round(x * Factor) * Divisor, Mathf.Round(y * Factor) * Divisor);
        }

        private static bool IsAnimating(params AnimBool[] animBools)
        {
            foreach (var a in animBools)
                if (a.isAnimating) return true;
            return false;
        }

        private void UpdateAttachments()
        {
            //skeleton = skeletonRenderer.skeleton;
            var defaultSkin = this.skeleton.Data.DefaultSkin;
            var skin = this.skeleton.Skin ?? defaultSkin;
            var notDefaultSkin = skin != defaultSkin;

            this.attachmentTable.Clear();
            for (var i = this.skeleton.Slots.Count - 1; i >= 0; i--)
            {
                var attachments = new List<Skin.SkinEntry>();
                this.attachmentTable.Add(this.skeleton.Slots.Items[i], attachments);
                // Add skin attachments.
                skin.GetAttachments(i, attachments);
                if (notDefaultSkin && defaultSkin != null) // Add default skin attachments.
                    defaultSkin.GetAttachments(i, attachments);
            }

            this.activeSkin = this.skeleton.Skin;
        }
    }
}
