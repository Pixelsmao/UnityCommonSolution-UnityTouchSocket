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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    public struct SpineDrawerValuePair
    {
        public string stringValue;
        public SerializedProperty property;

        public SpineDrawerValuePair(string val, SerializedProperty property)
        {
            this.stringValue = val;
            this.property = property;
        }
    }

    public abstract class SpineTreeItemDrawerBase<T> : PropertyDrawer where T : SpineAttributeBase
    {
        protected SkeletonDataAsset skeletonDataAsset;
        internal const string NoneStringConstant = "<None>";

        internal virtual string NoneString { get { return NoneStringConstant; } }

        private GUIContent noneLabel;
        private GUIContent NoneLabel(Texture2D image = null)
        {
            if (this.noneLabel == null) this.noneLabel = new GUIContent(this.NoneString);
            this.noneLabel.image = image;
            return this.noneLabel;
        }

        protected T TargetAttribute { get { return (T)this.attribute; } }
        protected SerializedProperty SerializedProperty { get; private set; }

        protected abstract Texture2D Icon { get; }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            this.SerializedProperty = property;

            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, "ERROR:", "May only apply to type string");
                return;
            }

            // Handle multi-editing when instances don't use the same SkeletonDataAsset.
            if (!SpineInspectorUtility.TargetsUseSameData(property.serializedObject))
            {
                EditorGUI.DelayedTextField(position, property, label);
                return;
            }

            var dataField = property.FindBaseOrSiblingProperty(this.TargetAttribute.dataField);

            if (dataField != null)
            {
                var objectReferenceValue = dataField.objectReferenceValue;
                if (objectReferenceValue is SkeletonDataAsset)
                {
                    this.skeletonDataAsset = (SkeletonDataAsset)objectReferenceValue;
                }
                else if (objectReferenceValue is IHasSkeletonDataAsset)
                {
                    var hasSkeletonDataAsset = (IHasSkeletonDataAsset)objectReferenceValue;
                    if (hasSkeletonDataAsset != null)
                        this.skeletonDataAsset = hasSkeletonDataAsset.SkeletonDataAsset;
                }
                else if (objectReferenceValue != null)
                {
                    EditorGUI.LabelField(position, "ERROR:", "Invalid reference type");
                    return;
                }

            }
            else
            {
                var targetObject = property.serializedObject.targetObject;

                var hasSkeletonDataAsset = targetObject as IHasSkeletonDataAsset;
                if (hasSkeletonDataAsset == null)
                {
                    var component = targetObject as Component;
                    if (component != null)
                        hasSkeletonDataAsset = component.GetComponentInChildren(typeof(IHasSkeletonDataAsset)) as IHasSkeletonDataAsset;
                }

                if (hasSkeletonDataAsset != null)
                    this.skeletonDataAsset = hasSkeletonDataAsset.SkeletonDataAsset;
            }

            if (this.skeletonDataAsset == null)
            {
                if (this.TargetAttribute.fallbackToTextField)
                {
                    EditorGUI.PropertyField(position, property); //EditorGUI.TextField(position, label, property.stringValue);
                }
                else
                {
                    EditorGUI.LabelField(position, "ERROR:", "Must have reference to a SkeletonDataAsset");
                }

                this.skeletonDataAsset = property.serializedObject.targetObject as SkeletonDataAsset;
                if (this.skeletonDataAsset == null) return;
            }

            position = EditorGUI.PrefixLabel(position, label);

            var image = this.Icon;
            var propertyStringValue = (property.hasMultipleDifferentValues) ? SpineInspectorUtility.EmDash : property.stringValue;
            if (GUI.Button(position, string.IsNullOrEmpty(propertyStringValue) ? this.NoneLabel(image) : SpineInspectorUtility.TempContent(propertyStringValue, image), EditorStyles.popup))
                this.Selector(property);
        }

        public ISkeletonComponent GetTargetSkeletonComponent(SerializedProperty property)
        {
            var dataField = property.FindBaseOrSiblingProperty(this.TargetAttribute.dataField);

            if (dataField != null)
            {
                var skeletonComponent = dataField.objectReferenceValue as ISkeletonComponent;
                if (dataField.objectReferenceValue != null && skeletonComponent != null) // note the overloaded UnityEngine.Object == null check. Do not simplify.
                    return skeletonComponent;
            }
            else
            {
                var component = property.serializedObject.targetObject as Component;
                if (component != null)
                    return component.GetComponentInChildren(typeof(ISkeletonComponent)) as ISkeletonComponent;
            }

            return null;
        }

        protected virtual void Selector(SerializedProperty property)
        {
            var data = this.skeletonDataAsset.GetSkeletonData(true);
            if (data == null) return;

            var menu = new GenericMenu();
            this.PopulateMenu(menu, property, this.TargetAttribute, data);
            menu.ShowAsContext();
        }

        protected abstract void PopulateMenu(GenericMenu menu, SerializedProperty property, T targetAttribute, SkeletonData data);

        protected virtual void HandleSelect(object menuItemObject)
        {
            var clickedItem = (SpineDrawerValuePair)menuItemObject;
            var serializedProperty = clickedItem.property;
            if (serializedProperty.serializedObject.isEditingMultipleObjects) serializedProperty.stringValue = "oaifnoiasf��123526"; // HACK: to trigger change on multi-editing.
            serializedProperty.stringValue = clickedItem.stringValue;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 18;
        }

    }

    [CustomPropertyDrawer(typeof(SpineSlot))]
    public class SpineSlotDrawer : SpineTreeItemDrawerBase<SpineSlot>
    {

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.slot; } }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpineSlot targetAttribute, SkeletonData data)
        {
            if (this.TargetAttribute.includeNone)
                menu.AddItem(new GUIContent(this.NoneString), !property.hasMultipleDifferentValues && string.IsNullOrEmpty(property.stringValue), this.HandleSelect, new SpineDrawerValuePair(string.Empty, property));

            IEnumerable<SlotData> orderedSlots = data.Slots.Items.OrderBy(slotData => slotData.Name);
            foreach (var slotData in orderedSlots)
            {
                var slotIndex = slotData.Index;
                var name = slotData.Name;
                if (name.StartsWith(targetAttribute.startsWith, StringComparison.Ordinal))
                {

                    if (targetAttribute.containsBoundingBoxes)
                    {
                        var skinEntries = new List<Skin.SkinEntry>();
                        foreach (var skin in data.Skins)
                        {
                            skin.GetAttachments(slotIndex, skinEntries);
                        }

                        var hasBoundingBox = false;
                        foreach (var entry in skinEntries)
                        {
                            var bbAttachment = entry.Attachment as BoundingBoxAttachment;
                            if (bbAttachment != null)
                            {
                                var menuLabel = bbAttachment.IsWeighted() ? name + " (!)" : name;
                                menu.AddItem(new GUIContent(menuLabel), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
                                hasBoundingBox = true;
                                break;
                            }
                        }

                        if (!hasBoundingBox)
                            menu.AddDisabledItem(new GUIContent(name));

                    }
                    else
                    {
                        menu.AddItem(new GUIContent(name), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
                    }

                }

            }
        }

    }

    [CustomPropertyDrawer(typeof(SpineSkin))]
    public class SpineSkinDrawer : SpineTreeItemDrawerBase<SpineSkin>
    {
        private const string DefaultSkinName = "default";

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.skin; } }

        internal override string NoneString { get { return this.TargetAttribute.defaultAsEmptyString ? DefaultSkinName : NoneStringConstant; } }

        public static void GetSkinMenuItems(SkeletonData data, List<string> outputNames, List<GUIContent> outputMenuItems, bool includeNone = true)
        {
            if (data == null) return;
            if (outputNames == null) return;
            if (outputMenuItems == null) return;

            var skins = data.Skins;

            outputNames.Clear();
            outputMenuItems.Clear();

            var icon = SpineEditorUtilities.Icons.skin;

            if (includeNone)
            {
                outputNames.Add("");
                outputMenuItems.Add(new GUIContent(NoneStringConstant, icon));
            }

            foreach (var s in skins)
            {
                var skinName = s.Name;
                outputNames.Add(skinName);
                outputMenuItems.Add(new GUIContent(skinName, icon));
            }
        }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpineSkin targetAttribute, SkeletonData data)
        {
            menu.AddDisabledItem(new GUIContent(this.skeletonDataAsset.name));
            menu.AddSeparator("");

            for (var i = 0; i < data.Skins.Count; i++)
            {
                var name = data.Skins.Items[i].Name;
                if (name.StartsWith(targetAttribute.startsWith, StringComparison.Ordinal))
                {
                    var isDefault = string.Equals(name, DefaultSkinName, StringComparison.Ordinal);
                    var choiceValue = this.TargetAttribute.defaultAsEmptyString && isDefault ? string.Empty : name;
                    menu.AddItem(new GUIContent(name), !property.hasMultipleDifferentValues && choiceValue == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(choiceValue, property));
                }

            }
        }

    }

    [CustomPropertyDrawer(typeof(SpineAnimation))]
    public class SpineAnimationDrawer : SpineTreeItemDrawerBase<SpineAnimation>
    {

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.animation; } }

        public static void GetAnimationMenuItems(SkeletonData data, List<string> outputNames, List<GUIContent> outputMenuItems, bool includeNone = true)
        {
            if (data == null) return;
            if (outputNames == null) return;
            if (outputMenuItems == null) return;

            var animations = data.Animations;

            outputNames.Clear();
            outputMenuItems.Clear();

            if (includeNone)
            {
                outputNames.Add("");
                outputMenuItems.Add(new GUIContent(NoneStringConstant, SpineEditorUtilities.Icons.animation));
            }

            foreach (var a in animations)
            {
                var animationName = a.Name;
                outputNames.Add(animationName);
                outputMenuItems.Add(new GUIContent(animationName, SpineEditorUtilities.Icons.animation));
            }
        }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpineAnimation targetAttribute, SkeletonData data)
        {
            var animations = this.skeletonDataAsset.GetAnimationStateData().SkeletonData.Animations;

            if (this.TargetAttribute.includeNone)
                menu.AddItem(new GUIContent(this.NoneString), !property.hasMultipleDifferentValues && string.IsNullOrEmpty(property.stringValue), this.HandleSelect, new SpineDrawerValuePair(string.Empty, property));

            for (var i = 0; i < animations.Count; i++)
            {
                var name = animations.Items[i].Name;
                if (name.StartsWith(targetAttribute.startsWith, StringComparison.Ordinal))
                    menu.AddItem(new GUIContent(name), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
            }
        }

    }

    [CustomPropertyDrawer(typeof(SpineEvent))]
    public class SpineEventNameDrawer : SpineTreeItemDrawerBase<SpineEvent>
    {

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.userEvent; } }

        public static void GetEventMenuItems(SkeletonData data, List<string> eventNames, List<GUIContent> menuItems, bool includeNone = true)
        {
            if (data == null) return;

            var animations = data.Events;

            eventNames.Clear();
            menuItems.Clear();

            if (includeNone)
            {
                eventNames.Add("");
                menuItems.Add(new GUIContent(NoneStringConstant, SpineEditorUtilities.Icons.userEvent));
            }

            foreach (var a in animations)
            {
                var animationName = a.Name;
                eventNames.Add(animationName);
                menuItems.Add(new GUIContent(animationName, SpineEditorUtilities.Icons.userEvent));
            }
        }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpineEvent targetAttribute, SkeletonData data)
        {
            var events = this.skeletonDataAsset.GetSkeletonData(false).Events;

            if (this.TargetAttribute.includeNone)
                menu.AddItem(new GUIContent(this.NoneString), !property.hasMultipleDifferentValues && string.IsNullOrEmpty(property.stringValue), this.HandleSelect, new SpineDrawerValuePair(string.Empty, property));

            for (var i = 0; i < events.Count; i++)
            {
                var eventObject = events.Items[i];
                var name = eventObject.Name;
                if (name.StartsWith(targetAttribute.startsWith, StringComparison.Ordinal))
                {
                    if (!this.TargetAttribute.audioOnly || !string.IsNullOrEmpty(eventObject.AudioPath))
                    {
                        menu.AddItem(new GUIContent(name), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
                    }
                }

            }
        }

    }

    [CustomPropertyDrawer(typeof(SpineIkConstraint))]
    public class SpineIkConstraintDrawer : SpineTreeItemDrawerBase<SpineIkConstraint>
    {

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.constraintIK; } }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpineIkConstraint targetAttribute, SkeletonData data)
        {
            var constraints = this.skeletonDataAsset.GetSkeletonData(false).IkConstraints;

            if (this.TargetAttribute.includeNone)
                menu.AddItem(new GUIContent(this.NoneString), !property.hasMultipleDifferentValues && string.IsNullOrEmpty(property.stringValue), this.HandleSelect, new SpineDrawerValuePair(string.Empty, property));

            for (var i = 0; i < constraints.Count; i++)
            {
                var name = constraints.Items[i].Name;
                if (name.StartsWith(targetAttribute.startsWith, StringComparison.Ordinal))
                    menu.AddItem(new GUIContent(name), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
            }
        }

    }

    [CustomPropertyDrawer(typeof(SpineTransformConstraint))]
    public class SpineTransformConstraintDrawer : SpineTreeItemDrawerBase<SpineTransformConstraint>
    {

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.constraintTransform; } }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpineTransformConstraint targetAttribute, SkeletonData data)
        {
            var constraints = this.skeletonDataAsset.GetSkeletonData(false).TransformConstraints;

            if (this.TargetAttribute.includeNone)
                menu.AddItem(new GUIContent(this.NoneString), !property.hasMultipleDifferentValues && string.IsNullOrEmpty(property.stringValue), this.HandleSelect, new SpineDrawerValuePair(string.Empty, property));

            for (var i = 0; i < constraints.Count; i++)
            {
                var name = constraints.Items[i].Name;
                if (name.StartsWith(targetAttribute.startsWith, StringComparison.Ordinal))
                    menu.AddItem(new GUIContent(name), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
            }
        }
    }

    [CustomPropertyDrawer(typeof(SpinePathConstraint))]
    public class SpinePathConstraintDrawer : SpineTreeItemDrawerBase<SpinePathConstraint>
    {

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.constraintPath; } }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpinePathConstraint targetAttribute, SkeletonData data)
        {
            var constraints = this.skeletonDataAsset.GetSkeletonData(false).PathConstraints;

            if (this.TargetAttribute.includeNone)
                menu.AddItem(new GUIContent(this.NoneString), !property.hasMultipleDifferentValues && string.IsNullOrEmpty(property.stringValue), this.HandleSelect, new SpineDrawerValuePair(string.Empty, property));

            for (var i = 0; i < constraints.Count; i++)
            {
                var name = constraints.Items[i].Name;
                if (name.StartsWith(targetAttribute.startsWith, StringComparison.Ordinal))
                    menu.AddItem(new GUIContent(name), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
            }
        }
    }

    [CustomPropertyDrawer(typeof(SpineAttachment))]
    public class SpineAttachmentDrawer : SpineTreeItemDrawerBase<SpineAttachment>
    {

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.genericAttachment; } }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpineAttachment targetAttribute, SkeletonData data)
        {
            var skeletonComponent = this.GetTargetSkeletonComponent(property);
            var validSkins = new List<Skin>();

            if (skeletonComponent != null && targetAttribute.currentSkinOnly)
            {
                Skin currentSkin = null;

                var skinProperty = property.FindBaseOrSiblingProperty(targetAttribute.skinField);
                if (skinProperty != null) currentSkin = skeletonComponent.Skeleton.Data.FindSkin(skinProperty.stringValue);

                currentSkin = currentSkin ?? skeletonComponent.Skeleton.Skin;
                if (currentSkin != null)
                    validSkins.Add(currentSkin);
                else
                    validSkins.Add(data.Skins.Items[0]);

            }
            else
            {
                foreach (var skin in data.Skins)
                    if (skin != null) validSkins.Add(skin);
            }

            var attachmentNames = new List<string>();
            var placeholderNames = new List<string>();
            var prefix = "";

            if (skeletonComponent != null && targetAttribute.currentSkinOnly)
                menu.AddDisabledItem(new GUIContent((skeletonComponent as Component).gameObject.name + " (Skeleton)"));
            else
                menu.AddDisabledItem(new GUIContent(this.skeletonDataAsset.name));

            menu.AddSeparator("");
            if (this.TargetAttribute.includeNone)
            {
                const string NullAttachmentName = "";
                menu.AddItem(new GUIContent("Null"), !property.hasMultipleDifferentValues && property.stringValue == NullAttachmentName, this.HandleSelect, new SpineDrawerValuePair(NullAttachmentName, property));
                menu.AddSeparator("");
            }

            var defaultSkin = data.Skins.Items[0];
            var slotProperty = property.FindBaseOrSiblingProperty(this.TargetAttribute.slotField);

            var slotMatch = "";
            if (slotProperty != null)
            {
                if (slotProperty.propertyType == SerializedPropertyType.String)
                    slotMatch = slotProperty.stringValue.ToLower();
            }

            foreach (var skin in validSkins)
            {
                var skinPrefix = skin.Name + "/";

                if (validSkins.Count > 1)
                    prefix = skinPrefix;

                for (var i = 0; i < data.Slots.Count; i++)
                {
                    if (slotMatch.Length > 0 && !(data.Slots.Items[i].Name.Equals(slotMatch, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    attachmentNames.Clear();
                    placeholderNames.Clear();

                    var skinEntries = new List<Skin.SkinEntry>();
                    skin.GetAttachments(i, skinEntries);
                    foreach (var entry in skinEntries)
                    {
                        attachmentNames.Add(entry.Name);
                    }

                    if (skin != defaultSkin)
                    {
                        foreach (var entry in skinEntries)
                        {
                            placeholderNames.Add(entry.Name);
                        }
                        skinEntries.Clear();
                        defaultSkin.GetAttachments(i, skinEntries);
                        foreach (var entry in skinEntries)
                        {
                            attachmentNames.Add(entry.Name);
                        }
                    }

                    for (var a = 0; a < attachmentNames.Count; a++)
                    {
                        var attachmentPath = attachmentNames[a];
                        var menuPath = prefix + data.Slots.Items[i].Name + "/" + attachmentPath;
                        var name = attachmentNames[a];

                        if (targetAttribute.returnAttachmentPath)
                            name = skin.Name + "/" + data.Slots.Items[i].Name + "/" + attachmentPath;

                        if (targetAttribute.placeholdersOnly && !placeholderNames.Contains(attachmentPath))
                        {
                            menu.AddDisabledItem(new GUIContent(menuPath));
                        }
                        else
                        {
                            menu.AddItem(new GUIContent(menuPath), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
                        }
                    }

                }
            }
        }

    }

    [CustomPropertyDrawer(typeof(SpineBone))]
    public class SpineBoneDrawer : SpineTreeItemDrawerBase<SpineBone>
    {

        protected override Texture2D Icon { get { return SpineEditorUtilities.Icons.bone; } }

        protected override void PopulateMenu(GenericMenu menu, SerializedProperty property, SpineBone targetAttribute, SkeletonData data)
        {
            menu.AddDisabledItem(new GUIContent(this.skeletonDataAsset.name));
            menu.AddSeparator("");

            if (this.TargetAttribute.includeNone)
                menu.AddItem(new GUIContent(this.NoneString), !property.hasMultipleDifferentValues && string.IsNullOrEmpty(property.stringValue), this.HandleSelect, new SpineDrawerValuePair(string.Empty, property));

            for (var i = 0; i < data.Bones.Count; i++)
            {
                var bone = data.Bones.Items[i];
                var name = bone.Name;
                if (name.StartsWith(targetAttribute.startsWith, StringComparison.Ordinal))
                {
                    // jointName = "root/hip/bone" to show a hierarchial tree.
                    var jointName = name;
                    var iterator = bone;
                    while ((iterator = iterator.Parent) != null)
                        jointName = string.Format("{0}/{1}", iterator.Name, jointName);

                    menu.AddItem(new GUIContent(jointName), !property.hasMultipleDifferentValues && name == property.stringValue, this.HandleSelect, new SpineDrawerValuePair(name, property));
                }
            }
        }

    }

    [CustomPropertyDrawer(typeof(SpineAtlasRegion))]
    public class SpineAtlasRegionDrawer : PropertyDrawer
    {
        private SerializedProperty atlasProp;

        protected SpineAtlasRegion TargetAttribute { get { return (SpineAtlasRegion)this.attribute; } }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, "ERROR:", "May only apply to type string");
                return;
            }

            var atlasAssetFieldName = this.TargetAttribute.atlasAssetField;
            if (string.IsNullOrEmpty(atlasAssetFieldName))
                atlasAssetFieldName = "atlasAsset";

            this.atlasProp = property.FindBaseOrSiblingProperty(atlasAssetFieldName);

            if (this.atlasProp == null)
            {
                EditorGUI.LabelField(position, "ERROR:", "Must have AtlasAsset variable!");
                return;
            }
            else if (this.atlasProp.objectReferenceValue == null)
            {
                EditorGUI.LabelField(position, "ERROR:", "Atlas variable must not be null!");
                return;
            }
            else if (!this.atlasProp.objectReferenceValue.GetType().IsSubclassOf(typeof(AtlasAssetBase)) &&
                        this.atlasProp.objectReferenceValue.GetType() != typeof(AtlasAssetBase))
            {
                EditorGUI.LabelField(position, "ERROR:", "Atlas variable must be of type AtlasAsset!");
            }

            position = EditorGUI.PrefixLabel(position, label);

            if (GUI.Button(position, property.stringValue, EditorStyles.popup))
                this.Selector(property);

        }

        private void Selector(SerializedProperty property)
        {
            var menu = new GenericMenu();
            var atlasAsset = (AtlasAssetBase)this.atlasProp.objectReferenceValue;
            var atlas = atlasAsset.GetAtlas();
            var field = typeof(Atlas).GetField("regions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var regions = (List<AtlasRegion>)field.GetValue(atlas);

            for (var i = 0; i < regions.Count; i++)
            {
                var name = regions[i].name;
                menu.AddItem(new GUIContent(name), !property.hasMultipleDifferentValues && name == property.stringValue, HandleSelect, new SpineDrawerValuePair(name, property));
            }

            menu.ShowAsContext();
        }

        private static void HandleSelect(object val)
        {
            var pair = (SpineDrawerValuePair)val;
            pair.property.stringValue = pair.stringValue;
            pair.property.serializedObject.ApplyModifiedProperties();
        }

    }

}
