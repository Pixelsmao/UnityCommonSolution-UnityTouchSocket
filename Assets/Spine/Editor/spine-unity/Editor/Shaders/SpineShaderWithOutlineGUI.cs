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
using SpineInspectorUtility = Spine.Unity.Editor.SpineInspectorUtility;

public class SpineShaderWithOutlineGUI : ShaderGUI
{

    protected MaterialEditor _materialEditor;
    private bool _showAdvancedOutlineSettings = false;
    private bool _showStencilSettings = false;

    private MaterialProperty _OutlineWidth = null;
    private MaterialProperty _OutlineColor = null;
    private MaterialProperty _OutlineReferenceTexWidth = null;
    private MaterialProperty _ThresholdEnd = null;
    private MaterialProperty _OutlineSmoothness = null;
    private MaterialProperty _Use8Neighbourhood = null;
    private MaterialProperty _OutlineMipLevel = null;
    private MaterialProperty _StencilComp = null;
    private MaterialProperty _StencilRef = null;

    private static readonly GUIContent _EnableOutlineText = new GUIContent("Outline", "Enable outline rendering. Draws an outline by sampling 4 or 8 neighbourhood pixels at a given distance specified via 'Outline Width'.");
    private static readonly GUIContent _OutlineWidthText = new GUIContent("Outline Width", "");
    private static readonly GUIContent _OutlineColorText = new GUIContent("Outline Color", "");
    private static readonly GUIContent _OutlineReferenceTexWidthText = new GUIContent("Reference Texture Width", "");
    private static readonly GUIContent _ThresholdEndText = new GUIContent("Outline Threshold", "");
    private static readonly GUIContent _OutlineSmoothnessText = new GUIContent("Outline Smoothness", "");
    private static readonly GUIContent _Use8NeighbourhoodText = new GUIContent("Sample 8 Neighbours", "");
    private static readonly GUIContent _OutlineMipLevelText = new GUIContent("Outline Mip Level", "");
    private static readonly GUIContent _StencilCompText = new GUIContent("Stencil Comparison", "");
    private static readonly GUIContent _StencilRefText = new GUIContent("Stencil Reference", "");

    private static readonly GUIContent _OutlineAdvancedText = new GUIContent("Advanced", "");
    private static readonly GUIContent _ShowStencilText = new GUIContent("Stencil", "Stencil parameters for mask interaction.");

    protected const string ShaderOutlineNamePrefix = "Spine/Outline/";
    protected const string ShaderNormalNamePrefix = "Spine/";
    protected const string ShaderWithoutStandardVariantSuffix = "OutlineOnly";

    #region ShaderGUI

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.FindProperties(properties); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
        this._materialEditor = materialEditor;

        base.OnGUI(materialEditor, properties);
        EditorGUILayout.Space();
        this.RenderStencilProperties();
        EditorGUILayout.Space();
        this.RenderOutlineProperties();
    }

    #endregion

    #region Virtual Interface

    protected virtual void FindProperties(MaterialProperty[] props)
    {

        this._OutlineWidth = FindProperty("_OutlineWidth", props, false);
        this._OutlineReferenceTexWidth = FindProperty("_OutlineReferenceTexWidth", props, false);
        this._OutlineColor = FindProperty("_OutlineColor", props, false);
        this._ThresholdEnd = FindProperty("_ThresholdEnd", props, false);
        this._OutlineSmoothness = FindProperty("_OutlineSmoothness", props, false);
        this._Use8Neighbourhood = FindProperty("_Use8Neighbourhood", props, false);
        this._OutlineMipLevel = FindProperty("_OutlineMipLevel", props, false);

        this._StencilComp = FindProperty("_StencilComp", props, false);
        this._StencilRef = FindProperty("_StencilRef", props, false);
        if (this._StencilRef == null)
            this._StencilRef = FindProperty("_Stencil", props, false);
    }

    protected virtual void RenderStencilProperties()
    {
        if (this._StencilComp == null)
            return; // not a shader supporting custom stencil operations

        // Use default labelWidth
        EditorGUIUtility.labelWidth = 0f;
        this._showStencilSettings = EditorGUILayout.Foldout(this._showStencilSettings, _ShowStencilText);
        if (this._showStencilSettings)
        {
            using (new SpineInspectorUtility.IndentScope())
            {
                this._materialEditor.ShaderProperty(this._StencilComp, _StencilCompText);
                this._materialEditor.ShaderProperty(this._StencilRef, _StencilRefText);
            }
        }
    }

    protected virtual void RenderOutlineProperties()
    {

        if (this._OutlineWidth == null)
            return; // not an outline shader

        // Use default labelWidth
        EditorGUIUtility.labelWidth = 0f;

        var hasOutlineVariant = !IsShaderWithoutStandardVariantShader(this._materialEditor, out var mixedValue);
        var isOutlineEnabled = true;
        if (hasOutlineVariant)
        {
            isOutlineEnabled = IsOutlineEnabled(this._materialEditor, out mixedValue);
            EditorGUI.showMixedValue = mixedValue;
            EditorGUI.BeginChangeCheck();

            var origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = FontStyle.Bold;
            isOutlineEnabled = EditorGUILayout.Toggle(_EnableOutlineText, isOutlineEnabled);
            EditorStyles.label.fontStyle = origFontStyle;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material material in this._materialEditor.targets)
                {
                    this.SwitchShaderToOutlineSettings(material, isOutlineEnabled);
                }
            }
        }
        else
        {
            var origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField(_EnableOutlineText);
            EditorStyles.label.fontStyle = origFontStyle;
        }

        if (isOutlineEnabled)
        {
            this._materialEditor.ShaderProperty(this._OutlineWidth, _OutlineWidthText);
            this._materialEditor.ShaderProperty(this._OutlineColor, _OutlineColorText);

            this._showAdvancedOutlineSettings = EditorGUILayout.Foldout(this._showAdvancedOutlineSettings, _OutlineAdvancedText);
            if (this._showAdvancedOutlineSettings)
            {
                using (new SpineInspectorUtility.IndentScope())
                {
                    this._materialEditor.ShaderProperty(this._OutlineReferenceTexWidth, _OutlineReferenceTexWidthText);
                    this._materialEditor.ShaderProperty(this._ThresholdEnd, _ThresholdEndText);
                    this._materialEditor.ShaderProperty(this._OutlineSmoothness, _OutlineSmoothnessText);
                    this._materialEditor.ShaderProperty(this._Use8Neighbourhood, _Use8NeighbourhoodText);
                    this._materialEditor.ShaderProperty(this._OutlineMipLevel, _OutlineMipLevelText);
                }
            }
        }
    }

    #endregion

    #region Private Functions

    private void SwitchShaderToOutlineSettings(Material material, bool enableOutline)
    {

        var shaderName = material.shader.name;
        var isSetToOutlineShader = shaderName.Contains(ShaderOutlineNamePrefix);
        if (isSetToOutlineShader && !enableOutline)
        {
            shaderName = shaderName.Replace(ShaderOutlineNamePrefix, ShaderNormalNamePrefix);
            this._materialEditor.SetShader(Shader.Find(shaderName), false);
            return;
        }
        else if (!isSetToOutlineShader && enableOutline)
        {
            shaderName = shaderName.Replace(ShaderNormalNamePrefix, ShaderOutlineNamePrefix);
            this._materialEditor.SetShader(Shader.Find(shaderName), false);
            return;
        }
    }

    private static bool IsOutlineEnabled(MaterialEditor editor, out bool mixedValue)
    {
        mixedValue = false;
        var isAnyEnabled = false;
        foreach (Material material in editor.targets)
        {
            if (material.shader.name.Contains(ShaderOutlineNamePrefix))
            {
                isAnyEnabled = true;
            }
            else if (isAnyEnabled)
            {
                mixedValue = true;
            }
        }
        return isAnyEnabled;
    }

    private static bool IsShaderWithoutStandardVariantShader(MaterialEditor editor, out bool mixedValue)
    {
        mixedValue = false;
        var isAnyShaderWithoutVariant = false;
        foreach (Material material in editor.targets)
        {
            if (material.shader.name.Contains(ShaderWithoutStandardVariantSuffix))
            {
                isAnyShaderWithoutVariant = true;
            }
            else if (isAnyShaderWithoutVariant)
            {
                mixedValue = true;
            }
        }
        return isAnyShaderWithoutVariant;
    }

    private static bool BoldToggleField(GUIContent label, bool value)
    {
        var origFontStyle = EditorStyles.label.fontStyle;
        EditorStyles.label.fontStyle = FontStyle.Bold;
        value = EditorGUILayout.Toggle(label, value, EditorStyles.toggle);
        EditorStyles.label.fontStyle = origFontStyle;
        return value;
    }

    #endregion
}
