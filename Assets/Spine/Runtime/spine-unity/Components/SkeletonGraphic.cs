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
using UnityEngine;
using UnityEngine.UI;

namespace Spine.Unity
{
#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [RequireComponent(typeof(CanvasRenderer), typeof(RectTransform)), DisallowMultipleComponent]
    [AddComponentMenu("Spine/SkeletonGraphic (Unity UI Canvas)")]
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonGraphic-Component")]
    public class SkeletonGraphic : MaskableGraphic, ISkeletonComponent, IAnimationStateComponent, ISkeletonAnimation, IHasSkeletonDataAsset
    {

        #region Inspector
        public SkeletonDataAsset skeletonDataAsset;
        public SkeletonDataAsset SkeletonDataAsset { get { return this.skeletonDataAsset; } }

        [SpineSkin(dataField: "skeletonDataAsset", defaultAsEmptyString: true)]
        public string initialSkinName;
        public bool initialFlipX, initialFlipY;

        [SpineAnimation(dataField: "skeletonDataAsset")]
        public string startingAnimation;
        public bool startingLoop;
        public float timeScale = 1f;
        public bool freeze;

        /// <summary>Update mode to optionally limit updates to e.g. only apply animations but not update the mesh.</summary>
        public UpdateMode UpdateMode { get { return this.updateMode; } set { this.updateMode = value; } }
        protected UpdateMode updateMode = UpdateMode.FullUpdate;

        /// <summary>Update mode used when the MeshRenderer becomes invisible
        /// (when <c>OnBecameInvisible()</c> is called). Update mode is automatically
        /// reset to <c>UpdateMode.FullUpdate</c> when the mesh becomes visible again.</summary>
        public UpdateMode updateWhenInvisible = UpdateMode.FullUpdate;

        public bool unscaledTime;
        public bool allowMultipleCanvasRenderers = false;
        public List<CanvasRenderer> canvasRenderers = new List<CanvasRenderer>();
        protected List<RawImage> rawImages = new List<RawImage>();
        protected int usedRenderersCount = 0;

        // Submesh Separation
        public const string SeparatorPartGameObjectName = "Part";
        /// <summary>Slot names used to populate separatorSlots list when the Skeleton is initialized. Changing this after initialization does nothing.</summary>
        [SerializeField][SpineSlot] protected string[] separatorSlotNames = new string[0];

        /// <summary>Slots that determine where the render is split. This is used by components such as SkeletonRenderSeparator so that the skeleton can be rendered by two separate renderers on different GameObjects.</summary>
        [System.NonSerialized] public readonly List<Slot> separatorSlots = new List<Slot>();
        public bool enableSeparatorSlots = false;
        [SerializeField] protected List<Transform> separatorParts = new List<Transform>();
        public List<Transform> SeparatorParts { get { return this.separatorParts; } }
        public bool updateSeparatorPartLocation = true;

        private bool wasUpdatedAfterInit = true;
        private Texture baseTexture = null;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            // This handles Scene View preview.
            base.OnValidate();
            if (this.IsValid)
            {
                if (this.skeletonDataAsset == null)
                {
                    this.Clear();
                }
                else if (this.skeletonDataAsset.skeletonJSON == null)
                {
                    this.Clear();
                }
                else if (this.skeletonDataAsset.GetSkeletonData(true) != this.skeleton.data)
                {
                    this.Clear();
                    this.Initialize(true);
                    if (!this.allowMultipleCanvasRenderers && (this.skeletonDataAsset.atlasAssets.Length > 1 || this.skeletonDataAsset.atlasAssets[0].MaterialCount > 1))
                        Debug.LogError("Unity UI does not support multiple textures per Renderer. Please enable 'Advanced - Multiple CanvasRenderers' to generate the required CanvasRenderer GameObjects. Otherwise your skeleton will not be rendered correctly.", this);
                }
                else
                {
                    if (this.freeze) return;

                    if (!string.IsNullOrEmpty(this.initialSkinName))
                    {
                        var skin = this.skeleton.data.FindSkin(this.initialSkinName);
                        if (skin != null)
                        {
                            if (skin == this.skeleton.data.defaultSkin)
                                this.skeleton.SetSkin((Skin)null);
                            else
                                this.skeleton.SetSkin(skin);
                        }

                    }

                    // Only provide visual feedback to inspector changes in Unity Editor Edit mode.
                    if (!Application.isPlaying)
                    {
                        this.skeleton.ScaleX = this.initialFlipX ? -1 : 1;
                        this.skeleton.ScaleY = this.initialFlipY ? -1 : 1;

                        this.state.ClearTrack(0);
                        this.skeleton.SetToSetupPose();
                        if (!string.IsNullOrEmpty(this.startingAnimation))
                        {
                            this.state.SetAnimation(0, this.startingAnimation, this.startingLoop);
                            this.Update(0f);
                        }
                    }
                }
            }
            else
            {
                // Under some circumstances (e.g. sometimes on the first import) OnValidate is called
                // before SpineEditorUtilities.ImportSpineContent, causing an unnecessary exception.
                // The (skeletonDataAsset.skeletonJSON != null) condition serves to prevent this exception.
                if (this.skeletonDataAsset != null && this.skeletonDataAsset.skeletonJSON != null)
                    this.Initialize(true);
            }
        }

        protected override void Reset()
        {

            base.Reset();
            if (this.material == null || this.material.shader != Shader.Find("Spine/SkeletonGraphic"))
                Debug.LogWarning("SkeletonGraphic works best with the SkeletonGraphic material.");
        }
#endif
        #endregion

        #region Runtime Instantiation
        /// <summary>Create a new GameObject with a SkeletonGraphic component.</summary>
        /// <param name="material">Material for the canvas renderer to use. Usually, the default SkeletonGraphic material will work.</param>
        public static SkeletonGraphic NewSkeletonGraphicGameObject(SkeletonDataAsset skeletonDataAsset, Transform parent, Material material)
        {
            var sg = SkeletonGraphic.AddSkeletonGraphicComponent(new GameObject("New Spine GameObject"), skeletonDataAsset, material);
            if (parent != null) sg.transform.SetParent(parent, false);
            return sg;
        }

        /// <summary>Add a SkeletonGraphic component to a GameObject.</summary>
        /// <param name="material">Material for the canvas renderer to use. Usually, the default SkeletonGraphic material will work.</param>
        public static SkeletonGraphic AddSkeletonGraphicComponent(GameObject gameObject, SkeletonDataAsset skeletonDataAsset, Material material)
        {
            var c = gameObject.AddComponent<SkeletonGraphic>();
            if (skeletonDataAsset != null)
            {
                c.material = material;
                c.skeletonDataAsset = skeletonDataAsset;
                c.Initialize(false);
            }
            return c;
        }
        #endregion

        #region Overrides
        [System.NonSerialized] private readonly Dictionary<Texture, Texture> customTextureOverride = new Dictionary<Texture, Texture>();
        /// <summary>Use this Dictionary to override a Texture with a different Texture.</summary>
        public Dictionary<Texture, Texture> CustomTextureOverride { get { return this.customTextureOverride; } }

        [System.NonSerialized] private readonly Dictionary<Texture, Material> customMaterialOverride = new Dictionary<Texture, Material>();
        /// <summary>Use this Dictionary to override the Material where the Texture was used at the original atlas.</summary>
        public Dictionary<Texture, Material> CustomMaterialOverride { get { return this.customMaterialOverride; } }

        // This is used by the UI system to determine what to put in the MaterialPropertyBlock.
        private Texture overrideTexture;
        public Texture OverrideTexture
        {
            get { return this.overrideTexture; }
            set
            {
                this.overrideTexture = value;
                this.canvasRenderer.SetTexture(this.mainTexture); // Refresh canvasRenderer's texture. Make sure it handles null.
            }
        }
        #endregion

        #region Internals
        public override Texture mainTexture
        {
            get
            {
                if (this.overrideTexture != null) return this.overrideTexture;
                return this.baseTexture;
            }
        }

        protected override void Awake()
        {

            base.Awake();
            this.onCullStateChanged.AddListener(this.OnCullStateChanged);

            this.SyncRawImagesWithCanvasRenderers();
            if (!this.IsValid)
            {
#if UNITY_EDITOR
                // workaround for special import case of open scene where OnValidate and Awake are
                // called in wrong order, before setup of Spine assets.
                if (!Application.isPlaying)
                {
                    if (this.skeletonDataAsset != null && this.skeletonDataAsset.skeletonJSON == null)
                        return;
                }
#endif
                this.Initialize(false);
                this.Rebuild(CanvasUpdate.PreRender);
            }
        }

        protected override void OnDestroy()
        {
            this.Clear();
            base.OnDestroy();
        }

        public override void Rebuild(CanvasUpdate update)
        {
            base.Rebuild(update);
            if (this.canvasRenderer.cull) return;
            if (update == CanvasUpdate.PreRender) this.UpdateMesh(keepRendererCount: true);
            if (this.allowMultipleCanvasRenderers) this.canvasRenderer.Clear();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach (var canvasRenderer in this.canvasRenderers)
            {
                canvasRenderer.Clear();
            }
        }

        public virtual void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                this.Update(0f);
                return;
            }
#endif

            if (this.freeze) return;
            this.Update(this.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        public virtual void Update(float deltaTime)
        {
            if (!this.IsValid) return;

            this.wasUpdatedAfterInit = true;
            if (this.updateMode < UpdateMode.OnlyAnimationStatus)
                return;
            this.UpdateAnimationStatus(deltaTime);

            if (this.updateMode == UpdateMode.OnlyAnimationStatus)
                return;
            this.ApplyAnimation();
        }

        protected void SyncRawImagesWithCanvasRenderers()
        {
            this.rawImages.Clear();
            foreach (var canvasRenderer in this.canvasRenderers)
            {
                var rawImage = canvasRenderer.GetComponent<RawImage>();
                if (rawImage == null)
                {
                    rawImage = canvasRenderer.gameObject.AddComponent<RawImage>();
                    rawImage.maskable = this.maskable;
                    rawImage.raycastTarget = false;
                }
                this.rawImages.Add(rawImage);
            }
        }

        protected void UpdateAnimationStatus(float deltaTime)
        {
            deltaTime *= this.timeScale;
            this.skeleton.Update(deltaTime);
            this.state.Update(deltaTime);
        }

        protected void ApplyAnimation()
        {
            if (BeforeApply != null)
                BeforeApply(this);

            if (this.updateMode != UpdateMode.OnlyEventTimelines)
                this.state.Apply(this.skeleton);
            else
                this.state.ApplyEventTimelinesOnly(this.skeleton);

            if (UpdateLocal != null)
                UpdateLocal(this);

            this.skeleton.UpdateWorldTransform();

            if (UpdateWorld != null)
            {
                UpdateWorld(this);
                this.skeleton.UpdateWorldTransform();
            }

            if (UpdateComplete != null)
                UpdateComplete(this);
        }

        public void LateUpdate()
        {
            // instantiation can happen from Update() after this component, leading to a missing Update() call.
            if (!this.wasUpdatedAfterInit) this.Update(0);
            if (this.freeze) return;
            if (this.updateMode != UpdateMode.FullUpdate) return;

            this.UpdateMesh();
        }

        protected void OnCullStateChanged(bool culled)
        {
            if (culled)
                this.OnBecameInvisible();
            else
                this.OnBecameVisible();
        }

        public void OnBecameVisible()
        {
            this.updateMode = UpdateMode.FullUpdate;
        }

        public void OnBecameInvisible()
        {
            this.updateMode = this.updateWhenInvisible;
        }

        public void ReapplySeparatorSlotNames()
        {
            if (!this.IsValid)
                return;

            this.separatorSlots.Clear();
            for (int i = 0, n = this.separatorSlotNames.Length; i < n; i++)
            {
                var slotName = this.separatorSlotNames[i];
                if (slotName == "")
                    continue;
                var slot = this.skeleton.FindSlot(slotName);
                if (slot != null)
                {
                    this.separatorSlots.Add(slot);
                }
#if UNITY_EDITOR
                else
                {
                    Debug.LogWarning(slotName + " is not a slot in " + this.skeletonDataAsset.skeletonJSON.name);
                }
#endif
            }
            this.UpdateSeparatorPartParents();
        }
        #endregion

        #region API
        protected Skeleton skeleton;
        public Skeleton Skeleton
        {
            get
            {
                this.Initialize(false);
                return this.skeleton;
            }
            set
            {
                this.skeleton = value;
            }
        }
        public SkeletonData SkeletonData { get { return this.skeleton == null ? null : this.skeleton.data; } }
        public bool IsValid { get { return this.skeleton != null; } }

        public delegate void SkeletonRendererDelegate(SkeletonGraphic skeletonGraphic);

        /// <summary>OnRebuild is raised after the Skeleton is successfully initialized.</summary>
        public event SkeletonRendererDelegate OnRebuild;

        /// <summary>OnMeshAndMaterialsUpdated is at the end of LateUpdate after the Mesh and
        /// all materials have been updated.</summary>
        public event SkeletonRendererDelegate OnMeshAndMaterialsUpdated;

        protected Spine.AnimationState state;
        public Spine.AnimationState AnimationState
        {
            get
            {
                this.Initialize(false);
                return this.state;
            }
        }

        [SerializeField] protected Spine.Unity.MeshGenerator meshGenerator = new MeshGenerator();
        public Spine.Unity.MeshGenerator MeshGenerator { get { return this.meshGenerator; } }
        private DoubleBuffered<Spine.Unity.MeshRendererBuffers.SmartMesh> meshBuffers;
        private readonly SkeletonRendererInstruction currentInstructions = new SkeletonRendererInstruction();
        private readonly ExposedList<Mesh> meshes = new ExposedList<Mesh>();

        public Mesh GetLastMesh()
        {
            return this.meshBuffers.GetCurrent().mesh;
        }

        public bool MatchRectTransformWithBounds()
        {
            this.UpdateMesh();

            if (!this.allowMultipleCanvasRenderers)
                return this.MatchRectTransformSingleRenderer();
            else
                return this.MatchRectTransformMultipleRenderers();
        }

        protected bool MatchRectTransformSingleRenderer()
        {
            var mesh = this.GetLastMesh();
            if (mesh == null)
            {
                return false;
            }
            if (mesh.vertexCount == 0)
            {
                this.rectTransform.sizeDelta = new Vector2(50f, 50f);
                this.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                return false;
            }
            mesh.RecalculateBounds();
            this.SetRectTransformBounds(mesh.bounds);
            return true;
        }

        protected bool MatchRectTransformMultipleRenderers()
        {
            var anyBoundsAdded = false;
            var combinedBounds = new Bounds();
            for (var i = 0; i < this.canvasRenderers.Count; ++i)
            {
                var canvasRenderer = this.canvasRenderers[i];
                if (!canvasRenderer.gameObject.activeSelf)
                    continue;

                var mesh = this.meshes.Items[i];
                if (mesh == null || mesh.vertexCount == 0)
                    continue;

                mesh.RecalculateBounds();
                var bounds = mesh.bounds;
                if (anyBoundsAdded)
                    combinedBounds.Encapsulate(bounds);
                else
                {
                    anyBoundsAdded = true;
                    combinedBounds = bounds;
                }
            }

            if (!anyBoundsAdded)
            {
                this.rectTransform.sizeDelta = new Vector2(50f, 50f);
                this.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                return false;
            }

            this.SetRectTransformBounds(combinedBounds);
            return true;
        }

        private void SetRectTransformBounds(Bounds combinedBounds)
        {
            var size = combinedBounds.size;
            var center = combinedBounds.center;
            var p = new Vector2(
                0.5f - (center.x / size.x),
                0.5f - (center.y / size.y)
            );

            this.rectTransform.sizeDelta = size;
            this.rectTransform.pivot = p;
        }

        public event UpdateBonesDelegate BeforeApply;
        public event UpdateBonesDelegate UpdateLocal;
        public event UpdateBonesDelegate UpdateWorld;
        public event UpdateBonesDelegate UpdateComplete;

        /// <summary> Occurs after the vertex data populated every frame, before the vertices are pushed into the mesh.</summary>
        public event Spine.Unity.MeshGeneratorDelegate OnPostProcessVertices;

        public void Clear()
        {
            this.skeleton = null;
            this.canvasRenderer.Clear();

            for (var i = 0; i < this.canvasRenderers.Count; ++i)
                this.canvasRenderers[i].Clear();
            this.DestroyMeshes();
            this.DisposeMeshBuffers();
        }

        public void TrimRenderers()
        {
            var newList = new List<CanvasRenderer>();
            foreach (var canvasRenderer in this.canvasRenderers)
            {
                if (canvasRenderer.gameObject.activeSelf)
                {
                    newList.Add(canvasRenderer);
                }
                else
                {
                    if (Application.isEditor && !Application.isPlaying)
                        DestroyImmediate(canvasRenderer.gameObject);
                    else
                        Destroy(canvasRenderer.gameObject);
                }
            }
            this.canvasRenderers = newList;
            this.SyncRawImagesWithCanvasRenderers();
        }

        public void Initialize(bool overwrite)
        {
            if (this.IsValid && !overwrite) return;

            if (this.skeletonDataAsset == null) return;
            var skeletonData = this.skeletonDataAsset.GetSkeletonData(false);
            if (skeletonData == null) return;

            if (this.skeletonDataAsset.atlasAssets.Length <= 0 || this.skeletonDataAsset.atlasAssets[0].MaterialCount <= 0) return;

            this.state = new Spine.AnimationState(this.skeletonDataAsset.GetAnimationStateData());
            if (this.state == null)
            {
                this.Clear();
                return;
            }

            this.skeleton = new Skeleton(skeletonData)
            {
                ScaleX = this.initialFlipX ? -1 : 1,
                ScaleY = this.initialFlipY ? -1 : 1
            };

            this.InitMeshBuffers();
            this.baseTexture = this.skeletonDataAsset.atlasAssets[0].PrimaryMaterial.mainTexture;
            this.canvasRenderer.SetTexture(this.mainTexture); // Needed for overwriting initializations.

            // Set the initial Skin and Animation
            if (!string.IsNullOrEmpty(this.initialSkinName))
                this.skeleton.SetSkin(this.initialSkinName);

            this.separatorSlots.Clear();
            for (var i = 0; i < this.separatorSlotNames.Length; i++)
                this.separatorSlots.Add(this.skeleton.FindSlot(this.separatorSlotNames[i]));

            this.wasUpdatedAfterInit = false;
            if (!string.IsNullOrEmpty(this.startingAnimation))
            {
                var animationObject = this.skeletonDataAsset.GetSkeletonData(false).FindAnimation(this.startingAnimation);
                if (animationObject != null)
                {
                    this.state.SetAnimation(0, animationObject, this.startingLoop);
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        this.Update(0f);
#endif
                }
            }

            if (OnRebuild != null)
                OnRebuild(this);
        }

        public void UpdateMesh(bool keepRendererCount = false)
        {
            if (!this.IsValid) return;

            this.skeleton.SetColor(this.color);

            var currentInstructions = this.currentInstructions;
            if (!this.allowMultipleCanvasRenderers)
            {
                this.UpdateMeshSingleCanvasRenderer();
            }
            else
            {
                this.UpdateMeshMultipleCanvasRenderers(currentInstructions, keepRendererCount);
            }

            if (OnMeshAndMaterialsUpdated != null)
                OnMeshAndMaterialsUpdated(this);
        }

        public bool HasMultipleSubmeshInstructions()
        {
            if (!this.IsValid)
                return false;
            return MeshGenerator.RequiresMultipleSubmeshesByDrawOrder(this.skeleton);
        }
        #endregion

        protected void InitMeshBuffers()
        {
            if (this.meshBuffers != null)
            {
                this.meshBuffers.GetNext().Clear();
                this.meshBuffers.GetNext().Clear();
            }
            else
            {
                this.meshBuffers = new DoubleBuffered<MeshRendererBuffers.SmartMesh>();
            }
        }

        protected void DisposeMeshBuffers()
        {
            if (this.meshBuffers != null)
            {
                this.meshBuffers.GetNext().Dispose();
                this.meshBuffers.GetNext().Dispose();
                this.meshBuffers = null;
            }
        }

        protected void UpdateMeshSingleCanvasRenderer()
        {
            if (this.canvasRenderers.Count > 0)
                this.DisableUnusedCanvasRenderers(usedCount: 0);

            var smartMesh = this.meshBuffers.GetNext();
            MeshGenerator.GenerateSingleSubmeshInstruction(this.currentInstructions, this.skeleton, null);
            var updateTriangles = SkeletonRendererInstruction.GeometryNotEqual(this.currentInstructions, smartMesh.instructionUsed);

            this.meshGenerator.Begin();
            if (this.currentInstructions.hasActiveClipping && this.currentInstructions.submeshInstructions.Count > 0)
            {
                this.meshGenerator.AddSubmesh(this.currentInstructions.submeshInstructions.Items[0], updateTriangles);
            }
            else
            {
                this.meshGenerator.BuildMeshWithArrays(this.currentInstructions, updateTriangles);
            }

            if (this.canvas != null) this.meshGenerator.ScaleVertexData(this.canvas.referencePixelsPerUnit);
            if (OnPostProcessVertices != null) OnPostProcessVertices.Invoke(this.meshGenerator.Buffers);

            var mesh = smartMesh.mesh;
            this.meshGenerator.FillVertexData(mesh);
            if (updateTriangles) this.meshGenerator.FillTriangles(mesh);
            this.meshGenerator.FillLateVertexData(mesh);

            this.canvasRenderer.SetMesh(mesh);
            smartMesh.instructionUsed.Set(this.currentInstructions);

            if (this.currentInstructions.submeshInstructions.Count > 0)
            {
                var material = this.currentInstructions.submeshInstructions.Items[0].material;
                if (material != null && this.baseTexture != material.mainTexture)
                {
                    this.baseTexture = material.mainTexture;
                    if (this.overrideTexture == null)
                        this.canvasRenderer.SetTexture(this.mainTexture);
                }
            }

            //this.UpdateMaterial(); // note: This would allocate memory.
            this.usedRenderersCount = 0;
        }

        protected void UpdateMeshMultipleCanvasRenderers(SkeletonRendererInstruction currentInstructions, bool keepRendererCount)
        {
            MeshGenerator.GenerateSkeletonRendererInstruction(currentInstructions, this.skeleton, null,
                this.enableSeparatorSlots ? this.separatorSlots : null,
                this.enableSeparatorSlots ? this.separatorSlots.Count > 0 : false,
                false);

            var submeshCount = currentInstructions.submeshInstructions.Count;
            if (keepRendererCount && submeshCount != this.usedRenderersCount)
                return;
            this.EnsureCanvasRendererCount(submeshCount);
            this.EnsureMeshesCount(submeshCount);
            this.EnsureSeparatorPartCount();

            var c = this.canvas;
            var scale = (c == null) ? 100 : c.referencePixelsPerUnit;

            // Generate meshes.
            var meshesItems = this.meshes.Items;
            var useOriginalTextureAndMaterial = (this.customMaterialOverride.Count == 0 && this.customTextureOverride.Count == 0);
            var separatorSlotGroupIndex = 0;
            var parent = this.separatorSlots.Count == 0 ? this.transform : this.separatorParts[0];

            if (this.updateSeparatorPartLocation)
            {
                for (var p = 0; p < this.separatorParts.Count; ++p)
                {
                    this.separatorParts[p].position = this.transform.position;
                    this.separatorParts[p].rotation = this.transform.rotation;
                }
            }

            var targetSiblingIndex = 0;
            for (var i = 0; i < submeshCount; i++)
            {
                var submeshInstructionItem = currentInstructions.submeshInstructions.Items[i];
                this.meshGenerator.Begin();
                this.meshGenerator.AddSubmesh(submeshInstructionItem);

                var targetMesh = meshesItems[i];
                this.meshGenerator.ScaleVertexData(scale);
                if (OnPostProcessVertices != null) OnPostProcessVertices.Invoke(this.meshGenerator.Buffers);
                this.meshGenerator.FillVertexData(targetMesh);
                this.meshGenerator.FillTriangles(targetMesh);
                this.meshGenerator.FillLateVertexData(targetMesh);

                var submeshMaterial = submeshInstructionItem.material;
                var canvasRenderer = this.canvasRenderers[i];
                if (i >= this.usedRenderersCount)
                    canvasRenderer.gameObject.SetActive(true);

                canvasRenderer.SetMesh(targetMesh);
                canvasRenderer.materialCount = 1;

                if (canvasRenderer.transform.parent != parent.transform)
                {
                    canvasRenderer.transform.SetParent(parent.transform, false);
                    canvasRenderer.transform.localPosition = Vector3.zero;
                }
                canvasRenderer.transform.SetSiblingIndex(targetSiblingIndex++);
                if (submeshInstructionItem.forceSeparate)
                {
                    targetSiblingIndex = 0;
                    parent = this.separatorParts[++separatorSlotGroupIndex];
                }

                if (useOriginalTextureAndMaterial)
                    canvasRenderer.SetMaterial(this.materialForRendering, submeshMaterial.mainTexture);
                else
                {
                    var originalTexture = submeshMaterial.mainTexture;
                    if (!this.customMaterialOverride.TryGetValue(originalTexture, out var usedMaterial))
                        usedMaterial = this.material;
                    if (!this.customTextureOverride.TryGetValue(originalTexture, out var usedTexture))
                        usedTexture = originalTexture;
                    canvasRenderer.SetMaterial(usedMaterial, usedTexture);
                }
            }

            this.DisableUnusedCanvasRenderers(usedCount: submeshCount);
            this.usedRenderersCount = submeshCount;
        }

        protected void EnsureCanvasRendererCount(int targetCount)
        {
#if UNITY_EDITOR
            this.RemoveNullCanvasRenderers();
#endif
            var currentCount = this.canvasRenderers.Count;
            for (var i = currentCount; i < targetCount; ++i)
            {
                var go = new GameObject(string.Format("Renderer{0}", i), typeof(RectTransform));
                go.transform.SetParent(this.transform, false);
                go.transform.localPosition = Vector3.zero;
                var canvasRenderer = go.AddComponent<CanvasRenderer>();
                this.canvasRenderers.Add(canvasRenderer);
                var rawImage = go.AddComponent<RawImage>();
                rawImage.maskable = this.maskable;
                rawImage.raycastTarget = false;
                this.rawImages.Add(rawImage);
            }
        }

        protected void DisableUnusedCanvasRenderers(int usedCount)
        {
#if UNITY_EDITOR
            this.RemoveNullCanvasRenderers();
#endif
            for (var i = usedCount; i < this.canvasRenderers.Count; i++)
            {
                this.canvasRenderers[i].Clear();
                this.canvasRenderers[i].gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        private void RemoveNullCanvasRenderers()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                for (var i = this.canvasRenderers.Count - 1; i >= 0; --i)
                {
                    if (this.canvasRenderers[i] == null)
                    {
                        this.canvasRenderers.RemoveAt(i);
                    }
                }
            }
        }
#endif

        protected void EnsureMeshesCount(int targetCount)
        {
            var oldCount = this.meshes.Count;
            this.meshes.EnsureCapacity(targetCount);
            for (var i = oldCount; i < targetCount; i++)
                this.meshes.Add(SpineMesh.NewSkeletonMesh());
        }

        protected void DestroyMeshes()
        {
            foreach (var mesh in this.meshes)
            {
#if UNITY_EDITOR
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(mesh);
                else
                    UnityEngine.Object.Destroy(mesh);
#else
					UnityEngine.Object.Destroy(mesh);
#endif
            }
            this.meshes.Clear();
        }

        protected void EnsureSeparatorPartCount()
        {
#if UNITY_EDITOR
            this.RemoveNullSeparatorParts();
#endif
            var targetCount = this.separatorSlots.Count + 1;
            if (targetCount == 1)
                return;

#if UNITY_EDITOR
            if (Application.isEditor && !Application.isPlaying)
            {
                for (var i = this.separatorParts.Count - 1; i >= 0; --i)
                {
                    if (this.separatorParts[i] == null)
                    {
                        this.separatorParts.RemoveAt(i);
                    }
                }
            }
#endif
            var currentCount = this.separatorParts.Count;
            for (var i = currentCount; i < targetCount; ++i)
            {
                var go = new GameObject(string.Format("{0}[{1}]", SeparatorPartGameObjectName, i), typeof(RectTransform));
                go.transform.SetParent(this.transform, false);
                go.transform.localPosition = Vector3.zero;
                this.separatorParts.Add(go.transform);
            }
        }

        protected void UpdateSeparatorPartParents()
        {
            var usedCount = this.separatorSlots.Count + 1;
            if (usedCount == 1)
            {
                usedCount = 0; // placed directly at the SkeletonGraphic parent
                for (var i = 0; i < this.canvasRenderers.Count; ++i)
                {
                    var canvasRenderer = this.canvasRenderers[i];
                    if (canvasRenderer.transform.parent.name.Contains(SeparatorPartGameObjectName))
                    {
                        canvasRenderer.transform.SetParent(this.transform, false);
                        canvasRenderer.transform.localPosition = Vector3.zero;
                    }
                }
            }
            for (var i = 0; i < this.separatorParts.Count; ++i)
            {
                var isUsed = i < usedCount;
                this.separatorParts[i].gameObject.SetActive(isUsed);
            }
        }

#if UNITY_EDITOR
        private void RemoveNullSeparatorParts()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                for (var i = this.separatorParts.Count - 1; i >= 0; --i)
                {
                    if (this.separatorParts[i] == null)
                    {
                        this.separatorParts.RemoveAt(i);
                    }
                }
            }
        }
#endif
    }
}
