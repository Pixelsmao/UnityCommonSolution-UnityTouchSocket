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

namespace Spine.Unity
{

#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [HelpURL("http://esotericsoftware.com/spine-unity#BoundingBoxFollowerGraphic")]
    public class BoundingBoxFollowerGraphic : MonoBehaviour
    {
        internal static bool DebugMessages = true;

        #region Inspector
        public SkeletonGraphic skeletonGraphic;
        [SpineSlot(dataField: "skeletonGraphic", containsBoundingBoxes: true)]
        public string slotName;
        public bool isTrigger;
        public bool clearStateOnDisable = true;
        #endregion

        private Slot slot;
        private BoundingBoxAttachment currentAttachment;
        private string currentAttachmentName;
        private PolygonCollider2D currentCollider;

        public readonly Dictionary<BoundingBoxAttachment, PolygonCollider2D> colliderTable = new Dictionary<BoundingBoxAttachment, PolygonCollider2D>();
        public readonly Dictionary<BoundingBoxAttachment, string> nameTable = new Dictionary<BoundingBoxAttachment, string>();

        public Slot Slot { get { return this.slot; } }
        public BoundingBoxAttachment CurrentAttachment { get { return this.currentAttachment; } }
        public string CurrentAttachmentName { get { return this.currentAttachmentName; } }
        public PolygonCollider2D CurrentCollider { get { return this.currentCollider; } }
        public bool IsTrigger { get { return this.isTrigger; } }

        private void Start()
        {
            this.Initialize();
        }

        private void OnEnable()
        {
            if (this.skeletonGraphic != null)
            {
                this.skeletonGraphic.OnRebuild -= this.HandleRebuild;
                this.skeletonGraphic.OnRebuild += this.HandleRebuild;
            }

            this.Initialize();
        }

        private void HandleRebuild(SkeletonGraphic sr)
        {
            //if (BoundingBoxFollowerGraphic.DebugMessages) Debug.Log("Skeleton was rebuilt. Repopulating BoundingBoxFollowerGraphic.");
            this.Initialize();
        }

        /// <summary>
        /// Initialize and instantiate the BoundingBoxFollowerGraphic colliders. This is method checks if the BoundingBoxFollowerGraphic has already been initialized for the skeleton instance and slotName and prevents overwriting unless it detects a new setup.</summary>
        public void Initialize(bool overwrite = false)
        {
            if (this.skeletonGraphic == null)
                return;

            this.skeletonGraphic.Initialize(false);

            if (string.IsNullOrEmpty(this.slotName))
                return;

            // Don't reinitialize if the setup did not change.
            if (!overwrite
                &&
                this.colliderTable.Count > 0 && this.slot != null           // Slot is set and colliders already populated.
                &&
                this.skeletonGraphic.Skeleton == this.slot.Skeleton     // Skeleton object did not change.
                &&
                this.slotName == this.slot.data.name                        // Slot object did not change.
            )
                return;

            this.slot = null;
            this.currentAttachment = null;
            this.currentAttachmentName = null;
            this.currentCollider = null;
            this.colliderTable.Clear();
            this.nameTable.Clear();

            var skeleton = this.skeletonGraphic.Skeleton;
            if (skeleton == null)
                return;
            this.slot = skeleton.FindSlot(this.slotName);
            var slotIndex = skeleton.FindSlotIndex(this.slotName);

            if (this.slot == null)
            {
                if (BoundingBoxFollowerGraphic.DebugMessages)
                    Debug.LogWarning(string.Format("Slot '{0}' not found for BoundingBoxFollowerGraphic on '{1}'. (Previous colliders were disposed.)", this.slotName, this.gameObject.name));
                return;
            }

            var requiredCollidersCount = 0;
            var colliders = this.GetComponents<PolygonCollider2D>();
            if (this.gameObject.activeInHierarchy)
            {
                var canvas = this.skeletonGraphic.canvas;
                if (canvas == null) canvas = this.skeletonGraphic.GetComponentInParent<Canvas>();
                var scale = canvas != null ? canvas.referencePixelsPerUnit : 100.0f;

                foreach (var skin in skeleton.Data.Skins)
                    this.AddCollidersForSkin(skin, slotIndex, colliders, scale, ref requiredCollidersCount);

                if (skeleton.skin != null)
                    this.AddCollidersForSkin(skeleton.skin, slotIndex, colliders, scale, ref requiredCollidersCount);
            }
            this.DisposeExcessCollidersAfter(requiredCollidersCount);

            if (BoundingBoxFollowerGraphic.DebugMessages)
            {
                var valid = this.colliderTable.Count != 0;
                if (!valid)
                {
                    if (this.gameObject.activeInHierarchy)
                        Debug.LogWarning("Bounding Box Follower not valid! Slot [" + this.slotName + "] does not contain any Bounding Box Attachments!");
                    else
                        Debug.LogWarning("Bounding Box Follower tried to rebuild as a prefab.");
                }
            }
        }

        private void AddCollidersForSkin(Skin skin, int slotIndex, PolygonCollider2D[] previousColliders, float scale, ref int collidersCount)
        {
            if (skin == null) return;
            var skinEntries = new List<Skin.SkinEntry>();
            skin.GetAttachments(slotIndex, skinEntries);

            foreach (var entry in skinEntries)
            {
                var attachment = skin.GetAttachment(slotIndex, entry.Name);
                var boundingBoxAttachment = attachment as BoundingBoxAttachment;

                if (BoundingBoxFollowerGraphic.DebugMessages && attachment != null && boundingBoxAttachment == null)
                    Debug.Log("BoundingBoxFollowerGraphic tried to follow a slot that contains non-boundingbox attachments: " + this.slotName);

                if (boundingBoxAttachment != null)
                {
                    if (!this.colliderTable.ContainsKey(boundingBoxAttachment))
                    {
                        var bbCollider = collidersCount < previousColliders.Length ?
                            previousColliders[collidersCount] : this.gameObject.AddComponent<PolygonCollider2D>();
                        ++collidersCount;
                        SkeletonUtility.SetColliderPointsLocal(bbCollider, this.slot, boundingBoxAttachment, scale);
                        bbCollider.isTrigger = this.isTrigger;
                        bbCollider.enabled = false;
                        bbCollider.hideFlags = HideFlags.NotEditable;
                        bbCollider.isTrigger = this.IsTrigger;
                        this.colliderTable.Add(boundingBoxAttachment, bbCollider);
                        this.nameTable.Add(boundingBoxAttachment, entry.Name);
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (this.clearStateOnDisable)
                this.ClearState();

            if (this.skeletonGraphic != null)
                this.skeletonGraphic.OnRebuild -= this.HandleRebuild;
        }

        public void ClearState()
        {
            if (this.colliderTable != null)
                foreach (var col in this.colliderTable.Values)
                    col.enabled = false;

            this.currentAttachment = null;
            this.currentAttachmentName = null;
            this.currentCollider = null;
        }

        private void DisposeExcessCollidersAfter(int requiredCount)
        {
            var colliders = this.GetComponents<PolygonCollider2D>();
            if (colliders.Length == 0) return;

            for (var i = requiredCount; i < colliders.Length; ++i)
            {
                var collider = colliders[i];
                if (collider != null)
                {
#if UNITY_EDITOR
                    if (Application.isEditor && !Application.isPlaying)
                        DestroyImmediate(collider);
                    else
#endif
                        Destroy(collider);
                }
            }
        }

        private void LateUpdate()
        {
            if (this.slot != null && this.slot.Attachment != this.currentAttachment)
                this.MatchAttachment(this.slot.Attachment);
        }

        /// <summary>Sets the current collider to match attachment.</summary>
        /// <param name="attachment">If the attachment is not a bounding box, it will be treated as null.</param>
        private void MatchAttachment(Attachment attachment)
        {
            var bbAttachment = attachment as BoundingBoxAttachment;

            if (BoundingBoxFollowerGraphic.DebugMessages && attachment != null && bbAttachment == null)
                Debug.LogWarning("BoundingBoxFollowerGraphic tried to match a non-boundingbox attachment. It will treat it as null.");

            if (this.currentCollider != null)
                this.currentCollider.enabled = false;

            if (bbAttachment == null)
            {
                this.currentCollider = null;
                this.currentAttachment = null;
                this.currentAttachmentName = null;
            }
            else
            {
                this.colliderTable.TryGetValue(bbAttachment, out var foundCollider);
                if (foundCollider != null)
                {
                    this.currentCollider = foundCollider;
                    this.currentCollider.enabled = true;
                    this.currentAttachment = bbAttachment;
                    this.currentAttachmentName = this.nameTable[bbAttachment];
                }
                else
                {
                    this.currentCollider = null;
                    this.currentAttachment = bbAttachment;
                    this.currentAttachmentName = null;
                    if (BoundingBoxFollowerGraphic.DebugMessages) Debug.LogFormat("Collider for BoundingBoxAttachment named '{0}' was not initialized. It is possibly from a new skin. currentAttachmentName will be null. You may need to call BoundingBoxFollowerGraphic.Initialize(overwrite: true);", bbAttachment.Name);
                }
            }
        }
    }

}
