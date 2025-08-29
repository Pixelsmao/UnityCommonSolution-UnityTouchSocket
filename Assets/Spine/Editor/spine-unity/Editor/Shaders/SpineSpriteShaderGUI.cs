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

using Spine.Unity;
using UnityEditor;
using UnityEngine;

public class SpineSpriteShaderGUI : SpineShaderWithOutlineGUI
{
    private static readonly string kShaderVertexLit = "Spine/Sprite/Vertex Lit";
    private static readonly string kShaderPixelLit = "Spine/Sprite/Pixel Lit";
    private static readonly string kShaderUnlit = "Spine/Sprite/Unlit";

    private static readonly string kShaderVertexLitOutline = "Spine/Outline/Sprite/Vertex Lit";
    private static readonly string kShaderPixelLitOutline = "Spine/Outline/Sprite/Pixel Lit";
    private static readonly string kShaderUnlitOutline = "Spine/Outline/Sprite/Unlit";

    private static readonly string kShaderLitLW = "Lightweight Render Pipeline/Spine/Sprite";
    private static readonly string kShaderLitURP = "Universal Render Pipeline/Spine/Sprite";
    private static readonly string kShaderLitURP2D = "Universal Render Pipeline/2D/Spine/Sprite";
    private static readonly int kSolidQueue = 2000;
    private static readonly int kAlphaTestQueue = 2450;
    private static readonly int kTransparentQueue = 3000;

    private enum eBlendMode
    {
        PreMultipliedAlpha,
        StandardAlpha,
        Opaque,
        Additive,
        SoftAdditive,
        Multiply,
        Multiplyx2,
    };

    private enum eLightMode
    {
        VertexLit,
        PixelLit,
        Unlit,
        LitLightweight,
        LitUniversal,
        LitUniversal2D
    };

    private enum eCulling
    {
        Off = 0,
        Front = 1,
        Back = 2,
    };

    private enum eNormalsMode
    {
        MeshNormals = -1,
        FixedNormalsViewSpace = 0,
        FixedNormalsModelSpace = 1,
        FixedNormalsWorldSpace = 2
    };

    private enum eDiffuseRampMode
    {
        NoRampSpecified = -1,
        FullRangeHard = 0,
        FullRangeSoft = 1,
        OldHard = 2,
        OldSoft = 3,

        DefaultRampMode = OldHard
    };

    private MaterialProperty _mainTexture = null;
    private MaterialProperty _color = null;
    private MaterialProperty _maskTexture = null;

    private MaterialProperty _pixelSnap = null;

    private MaterialProperty _writeToDepth = null;
    private MaterialProperty _depthAlphaCutoff = null;
    private MaterialProperty _shadowAlphaCutoff = null;
    private MaterialProperty _renderQueue = null;
    private MaterialProperty _culling = null;
    private MaterialProperty _customRenderQueue = null;

    private MaterialProperty _overlayColor = null;
    private MaterialProperty _hue = null;
    private MaterialProperty _saturation = null;
    private MaterialProperty _brightness = null;

    private MaterialProperty _rimPower = null;
    private MaterialProperty _rimColor = null;

    private MaterialProperty _bumpMap = null;
    private MaterialProperty _bumpScale = null;
    private MaterialProperty _diffuseRamp = null;
    private MaterialProperty _fixedNormal = null;

    private MaterialProperty _blendTexture = null;
    private MaterialProperty _blendTextureLerp = null;

    private MaterialProperty _emissionMap = null;
    private MaterialProperty _emissionColor = null;
    private MaterialProperty _emissionPower = null;

    private MaterialProperty _metallic = null;
    private MaterialProperty _metallicGlossMap = null;
    private MaterialProperty _smoothness = null;
    private MaterialProperty _smoothnessScale = null;

    private static readonly GUIContent _albedoText = new GUIContent("Albedo", "Albedo (RGB) and Transparency (A)");
    private static readonly GUIContent _maskText = new GUIContent("Light Mask", "Light mask texture (secondary Sprite texture)");
    private static readonly GUIContent _altAlbedoText = new GUIContent("Secondary Albedo", "When a secondary albedo texture is set the albedo will be a blended mix of the two textures based on the blend value.");
    private static readonly GUIContent _metallicMapText = new GUIContent("Metallic", "Metallic (R) and Smoothness (A)");
    private static readonly GUIContent _smoothnessText = new GUIContent("Smoothness", "Smoothness value");
    private static readonly GUIContent _smoothnessScaleText = new GUIContent("Smoothness", "Smoothness scale factor");
    private static readonly GUIContent _normalMapText = new GUIContent("Normal Map", "Normal Map");
    private static readonly GUIContent _emissionText = new GUIContent("Emission", "Emission (RGB)");
    private static readonly GUIContent _emissionPowerText = new GUIContent("Emission Power");
    private static readonly GUIContent _emissionToggleText = new GUIContent("Emission", "Enable Emission.");
    private static readonly GUIContent _diffuseRampText = new GUIContent("Diffuse Ramp", "A black and white gradient can be used to create a 'Toon Shading' effect.");
    private static readonly GUIContent _depthText = new GUIContent("Write to Depth", "Write to Depth Buffer by clipping alpha.");
    private static readonly GUIContent _depthAlphaCutoffText = new GUIContent("Depth Alpha Cutoff", "Threshold for depth write alpha cutoff");
    private static readonly GUIContent _shadowAlphaCutoffText = new GUIContent("Shadow Alpha Cutoff", "Threshold for shadow alpha cutoff");
    private static readonly GUIContent _receiveShadowsText = new GUIContent("Receive Shadows", "When enabled, other GameObjects can cast shadows onto this GameObject. 'Write to Depth' has to be enabled in Lightweight RP.");
    private static readonly GUIContent _fixedNormalText = new GUIContent("Fixed Normals", "If this is ticked instead of requiring mesh normals a Fixed Normal will be used instead (it's quicker and can result in better looking lighting effects on 2d objects).");
    private static readonly GUIContent _fixedNormalDirectionText = new GUIContent("Fixed Normal Direction", "Should normally be (0,0,1) if in view-space or (0,0,-1) if in model-space.");
    private static readonly GUIContent _adjustBackfaceTangentText = new GUIContent("Adjust Back-face Tangents", "Tick only if you are going to rotate the sprite to face away from the camera, the tangents will be flipped when this is the case to make lighting correct.");
    private static readonly GUIContent _sphericalHarmonicsText = new GUIContent("Light Probes & Ambient", "Enable to use spherical harmonics to aplpy ambient light and/or light probes. In vertex-lit mode this will be approximated from scenes ambient trilight settings.");
    private static readonly GUIContent _lightingModeText = new GUIContent("Lighting Mode", "Lighting Mode");
    private static readonly GUIContent[] _lightingModeOptions = {
        new GUIContent("Vertex Lit"),
        new GUIContent("Pixel Lit"),
        new GUIContent("Unlit"),
        new GUIContent("Lit Lightweight"),
        new GUIContent("Lit Universal"),
        new GUIContent("Lit Universal2D")
    };
    private static readonly GUIContent _blendModeText = new GUIContent("Blend Mode", "Blend Mode");
    private static readonly GUIContent[] _blendModeOptions = {
        new GUIContent("Pre-Multiplied Alpha"),
        new GUIContent("Standard Alpha"),
        new GUIContent("Opaque"),
        new GUIContent("Additive"),
        new GUIContent("Soft Additive"),
        new GUIContent("Multiply"),
        new GUIContent("Multiply x2")
    };
    private static readonly GUIContent _rendererQueueText = new GUIContent("Render Queue Offset");
    private static readonly GUIContent _cullingModeText = new GUIContent("Culling Mode");
    private static readonly GUIContent[] _cullingModeOptions = { new GUIContent("Off"), new GUIContent("Front"), new GUIContent("Back") };
    private static readonly GUIContent _pixelSnapText = new GUIContent("Pixel Snap");
    //static GUIContent _customRenderTypetagsText = new GUIContent("Use Custom RenderType tags");
    private static readonly GUIContent _fixedNormalSpaceText = new GUIContent("Fixed Normal Space");
    private static readonly GUIContent[] _fixedNormalSpaceOptions = { new GUIContent("View-Space"), new GUIContent("Model-Space"), new GUIContent("World-Space") };
    private static readonly GUIContent _rimLightingToggleText = new GUIContent("Rim Lighting", "Enable Rim Lighting.");
    private static readonly GUIContent _rimColorText = new GUIContent("Rim Color");
    private static readonly GUIContent _rimPowerText = new GUIContent("Rim Power");
    private static readonly GUIContent _specularToggleText = new GUIContent("Specular", "Enable Specular.");
    private static readonly GUIContent _colorAdjustmentToggleText = new GUIContent("Color Adjustment", "Enable material color adjustment.");
    private static readonly GUIContent _colorAdjustmentColorText = new GUIContent("Overlay Color");
    private static readonly GUIContent _colorAdjustmentHueText = new GUIContent("Hue");
    private static readonly GUIContent _colorAdjustmentSaturationText = new GUIContent("Saturation");
    private static readonly GUIContent _colorAdjustmentBrightnessText = new GUIContent("Brightness");
    private static readonly GUIContent _fogToggleText = new GUIContent("Fog", "Enable Fog rendering on this renderer.");
    private static readonly GUIContent _meshRequiresTangentsText = new GUIContent("Note: Material requires a mesh with tangents.");
    private static readonly GUIContent _meshRequiresNormalsText = new GUIContent("Note: Material requires a mesh with normals.");
    private static readonly GUIContent _meshRequiresNormalsAndTangentsText = new GUIContent("Note: Material requires a mesh with Normals and Tangents.");
    private static readonly GUIContent[] _fixedDiffuseRampModeOptions = { new GUIContent("Hard"), new GUIContent("Soft"), new GUIContent("Old Hard"), new GUIContent("Old Soft") };

    private const string _primaryMapsText = "Main Maps";
    private const string _depthLabelText = "Depth";
    private const string _shadowsText = "Shadows";
    private const string _customRenderType = "Use Custom RenderType";

    #region ShaderGUI

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.FindProperties(properties); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
        this._materialEditor = materialEditor;
        this.ShaderPropertiesGUI();
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        //If not originally a sprite shader set default keywords
        if (oldShader.name != kShaderVertexLit && oldShader.name != kShaderPixelLit && oldShader.name != kShaderUnlit &&
            oldShader.name != kShaderVertexLitOutline && oldShader.name != kShaderPixelLitOutline && oldShader.name != kShaderUnlitOutline &&
            oldShader.name != kShaderLitLW &&
            oldShader.name != kShaderLitURP &&
            oldShader.name != kShaderLitURP2D)
        {
            this.SetDefaultSpriteKeywords(material, newShader);
        }

        SetMaterialKeywords(material);
    }

    #endregion

    #region Virtual Interface

    protected override void FindProperties(MaterialProperty[] props)
    {
        base.FindProperties(props);

        this._mainTexture = FindProperty("_MainTex", props);
        this._maskTexture = FindProperty("_MaskTex", props, false);
        this._color = FindProperty("_Color", props);

        this._pixelSnap = FindProperty("PixelSnap", props);

        this._writeToDepth = FindProperty("_ZWrite", props, false);
        this._depthAlphaCutoff = FindProperty("_Cutoff", props);
        this._shadowAlphaCutoff = FindProperty("_ShadowAlphaCutoff", props);
        this._renderQueue = FindProperty("_RenderQueue", props);
        this._culling = FindProperty("_Cull", props);
        this._customRenderQueue = FindProperty("_CustomRenderQueue", props);

        this._bumpMap = FindProperty("_BumpMap", props, false);
        this._bumpScale = FindProperty("_BumpScale", props, false);
        this._diffuseRamp = FindProperty("_DiffuseRamp", props, false);
        this._fixedNormal = FindProperty("_FixedNormal", props, false);
        this._blendTexture = FindProperty("_BlendTex", props, false);
        this._blendTextureLerp = FindProperty("_BlendAmount", props, false);

        this._overlayColor = FindProperty("_OverlayColor", props, false);
        this._hue = FindProperty("_Hue", props, false);
        this._saturation = FindProperty("_Saturation", props, false);
        this._brightness = FindProperty("_Brightness", props, false);

        this._rimPower = FindProperty("_RimPower", props, false);
        this._rimColor = FindProperty("_RimColor", props, false);

        this._emissionMap = FindProperty("_EmissionMap", props, false);
        this._emissionColor = FindProperty("_EmissionColor", props, false);
        this._emissionPower = FindProperty("_EmissionPower", props, false);

        this._metallic = FindProperty("_Metallic", props, false);
        this._metallicGlossMap = FindProperty("_MetallicGlossMap", props, false);
        this._smoothness = FindProperty("_Glossiness", props, false);
        this._smoothnessScale = FindProperty("_GlossMapScale", props, false);
    }

    private static bool BoldToggleField(GUIContent label, bool value)
    {
        var origFontStyle = EditorStyles.label.fontStyle;
        EditorStyles.label.fontStyle = FontStyle.Bold;
        value = EditorGUILayout.Toggle(label, value, EditorStyles.toggle);
        EditorStyles.label.fontStyle = origFontStyle;
        return value;
    }

    protected virtual void ShaderPropertiesGUI()
    {
        // Use default labelWidth
        EditorGUIUtility.labelWidth = 0f;

        this.RenderMeshInfoBox();

        // Detect any changes to the material
        var dataChanged = this.RenderModes();

        GUILayout.Label(_primaryMapsText, EditorStyles.boldLabel);
        {
            dataChanged |= this.RenderTextureProperties();
        }

        GUILayout.Label(_depthLabelText, EditorStyles.boldLabel);
        {
            dataChanged |= this.RenderDepthProperties();
        }

        GUILayout.Label(_shadowsText, EditorStyles.boldLabel);
        {
            dataChanged |= this.RenderShadowsProperties();
        }

        if (this._metallic != null)
        {
            dataChanged |= this.RenderSpecularProperties();
        }

        if (this._emissionMap != null && this._emissionColor != null)
        {
            dataChanged |= this.RenderEmissionProperties();
        }

        if (this._fixedNormal != null)
        {
            dataChanged |= this.RenderNormalsProperties();
        }

        if (this._fixedNormal != null)
        {
            dataChanged |= this.RenderSphericalHarmonicsProperties();
        }

        {
            dataChanged |= this.RenderFogProperties();
        }

        {
            dataChanged |= this.RenderColorProperties();
        }

        if (this._rimColor != null)
        {
            dataChanged |= this.RenderRimLightingProperties();
        }

        {
            EditorGUILayout.Space();
            this.RenderStencilProperties();
        }

        {
            EditorGUILayout.Space();
            this.RenderOutlineProperties();
        }

        if (dataChanged)
        {
            MaterialChanged(this._materialEditor);
        }
    }

    protected virtual bool RenderModes()
    {
        var dataChanged = false;

        //Lighting Mode
        {
            EditorGUI.BeginChangeCheck();

            var lightMode = GetMaterialLightMode((Material)this._materialEditor.target);
            EditorGUI.showMixedValue = false;
            foreach (Material material in this._materialEditor.targets)
            {
                if (lightMode != GetMaterialLightMode(material))
                {
                    EditorGUI.showMixedValue = true;
                    break;
                }
            }

            lightMode = (eLightMode)EditorGUILayout.Popup(_lightingModeText, (int)lightMode, _lightingModeOptions);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material material in this._materialEditor.targets)
                {
                    switch (lightMode)
                    {
                        case eLightMode.VertexLit:
                            if (material.shader.name != kShaderVertexLit)
                                this._materialEditor.SetShader(Shader.Find(kShaderVertexLit), false);
                            break;
                        case eLightMode.PixelLit:
                            if (material.shader.name != kShaderPixelLit)
                                this._materialEditor.SetShader(Shader.Find(kShaderPixelLit), false);
                            break;
                        case eLightMode.Unlit:
                            if (material.shader.name != kShaderUnlit)
                                this._materialEditor.SetShader(Shader.Find(kShaderUnlit), false);
                            break;
                        case eLightMode.LitLightweight:
                            if (material.shader.name != kShaderLitLW)
                                this._materialEditor.SetShader(Shader.Find(kShaderLitLW), false);
                            break;
                        case eLightMode.LitUniversal:
                            if (material.shader.name != kShaderLitURP)
                                this._materialEditor.SetShader(Shader.Find(kShaderLitURP), false);
                            break;
                        case eLightMode.LitUniversal2D:
                            if (material.shader.name != kShaderLitURP2D)
                                this._materialEditor.SetShader(Shader.Find(kShaderLitURP2D), false);
                            break;
                    }
                }

                dataChanged = true;
            }
        }

        //Blend Mode
        {
            var blendMode = GetMaterialBlendMode((Material)this._materialEditor.target);
            EditorGUI.showMixedValue = false;
            foreach (Material material in this._materialEditor.targets)
            {
                if (blendMode != GetMaterialBlendMode(material))
                {
                    EditorGUI.showMixedValue = true;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            blendMode = (eBlendMode)EditorGUILayout.Popup(_blendModeText, (int)blendMode, _blendModeOptions);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material mat in this._materialEditor.targets)
                {
                    SetBlendMode(mat, blendMode);
                }

                dataChanged = true;
            }

            if (QualitySettings.activeColorSpace == ColorSpace.Linear &&
                !EditorGUI.showMixedValue && blendMode == eBlendMode.PreMultipliedAlpha)
            {
                EditorGUILayout.HelpBox(MaterialChecks.kPMANotSupportedLinearMessage, MessageType.Error, true);
            }
        }

        EditorGUI.BeginDisabledGroup(true);
        this._materialEditor.RenderQueueField();
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = this._renderQueue.hasMixedValue;
        var renderQueue = EditorGUILayout.IntField(_rendererQueueText, (int)this._renderQueue.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            this.SetInt("_RenderQueue", renderQueue);
            dataChanged = true;
        }

        EditorGUI.BeginChangeCheck();
        var culling = (eCulling)Mathf.RoundToInt(this._culling.floatValue);
        EditorGUI.showMixedValue = this._culling.hasMixedValue;
        culling = (eCulling)EditorGUILayout.Popup(_cullingModeText, (int)culling, _cullingModeOptions);
        if (EditorGUI.EndChangeCheck())
        {
            this.SetInt("_Cull", (int)culling);
            dataChanged = true;
        }

        EditorGUI.showMixedValue = false;

        EditorGUI.BeginChangeCheck();
        this._materialEditor.ShaderProperty(this._pixelSnap, _pixelSnapText);
        dataChanged |= EditorGUI.EndChangeCheck();

        return dataChanged;
    }

    protected virtual bool RenderTextureProperties()
    {
        var dataChanged = false;

        EditorGUI.BeginChangeCheck();

        this._materialEditor.TexturePropertySingleLine(_albedoText, this._mainTexture, this._color);

        if (this._bumpMap != null)
            this._materialEditor.TexturePropertySingleLine(_normalMapText, this._bumpMap, this._bumpMap.textureValue != null ? this._bumpScale : null);

        if (this._maskTexture != null)
            this._materialEditor.TexturePropertySingleLine(_maskText, this._maskTexture);

        dataChanged |= this.RenderDiffuseRampProperties();

        dataChanged |= EditorGUI.EndChangeCheck();

        if (this._blendTexture != null)
        {
            EditorGUI.BeginChangeCheck();
            this._materialEditor.TexturePropertySingleLine(_altAlbedoText, this._blendTexture, this._blendTextureLerp);
            if (EditorGUI.EndChangeCheck())
            {
                SetKeyword(this._materialEditor, "_TEXTURE_BLEND", this._blendTexture != null);
                dataChanged = true;
            }
        }

        EditorGUI.BeginChangeCheck();
        this._materialEditor.TextureScaleOffsetProperty(this._mainTexture);
        dataChanged |= EditorGUI.EndChangeCheck();

        EditorGUI.showMixedValue = false;

        return dataChanged;
    }

    protected virtual bool RenderDepthProperties()
    {
        var dataChanged = false;

        EditorGUI.BeginChangeCheck();

        var showDepthAlphaCutoff = true;
        // e.g. Pixel Lit shader always has ZWrite enabled
        if (this._writeToDepth != null)
        {
            var mixedValue = this._writeToDepth.hasMixedValue;
            EditorGUI.showMixedValue = mixedValue;
            var writeTodepth = EditorGUILayout.Toggle(_depthText, this._writeToDepth.floatValue != 0.0f);

            if (EditorGUI.EndChangeCheck())
            {
                this.SetInt("_ZWrite", writeTodepth ? 1 : 0);
                this._depthAlphaCutoff.floatValue = writeTodepth ? 0.5f : 0.0f;
                mixedValue = false;
                dataChanged = true;
            }

            showDepthAlphaCutoff = writeTodepth && !mixedValue && GetMaterialBlendMode((Material)this._materialEditor.target) != eBlendMode.Opaque;
        }
        if (showDepthAlphaCutoff)
        {
            EditorGUI.BeginChangeCheck();
            this._materialEditor.RangeProperty(this._depthAlphaCutoff, _depthAlphaCutoffText.text);
            dataChanged |= EditorGUI.EndChangeCheck();
        }

        {
            var useCustomRenderType = this._customRenderQueue.floatValue > 0.0f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = this._customRenderQueue.hasMixedValue;
            useCustomRenderType = EditorGUILayout.Toggle(_customRenderType, useCustomRenderType);
            if (EditorGUI.EndChangeCheck())
            {
                dataChanged = true;

                this._customRenderQueue.floatValue = useCustomRenderType ? 1.0f : 0.0f;

                foreach (Material material in this._materialEditor.targets)
                {
                    var blendMode = GetMaterialBlendMode(material);

                    switch (blendMode)
                    {
                        case eBlendMode.Opaque:
                            {
                                SetRenderType(material, "Opaque", useCustomRenderType);
                            }
                            break;
                        default:
                            {
                                var zWrite = HasZWriteEnabled(material);
                                SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderType);
                            }
                            break;
                    }
                }
            }
        }

        EditorGUI.showMixedValue = false;

        return dataChanged;
    }

    protected virtual bool RenderNormalsProperties()
    {
        var dataChanged = false;

        var normalsMode = GetMaterialNormalsMode((Material)this._materialEditor.target);
        var mixedNormalsMode = false;
        foreach (Material material in this._materialEditor.targets)
        {
            if (normalsMode != GetMaterialNormalsMode(material))
            {
                mixedNormalsMode = true;
                break;
            }
        }

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = mixedNormalsMode;
        var fixedNormals = BoldToggleField(_fixedNormalText, normalsMode != eNormalsMode.MeshNormals);

        if (EditorGUI.EndChangeCheck())
        {
            normalsMode = fixedNormals ? eNormalsMode.FixedNormalsViewSpace : eNormalsMode.MeshNormals;
            SetNormalsMode(this._materialEditor, normalsMode, false);
            this._fixedNormal.vectorValue = new Vector4(0.0f, 0.0f, normalsMode == eNormalsMode.FixedNormalsViewSpace ? 1.0f : -1.0f, 1.0f);
            mixedNormalsMode = false;
            dataChanged = true;
        }

        if (fixedNormals)
        {
            //Show drop down for normals space
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixedNormalsMode;
            normalsMode = (eNormalsMode)EditorGUILayout.Popup(_fixedNormalSpaceText, (int)normalsMode, _fixedNormalSpaceOptions);
            if (EditorGUI.EndChangeCheck())
            {
                SetNormalsMode((Material)this._materialEditor.target, normalsMode, GetMaterialFixedNormalsBackfaceRenderingOn((Material)this._materialEditor.target));

                foreach (Material material in this._materialEditor.targets)
                {
                    SetNormalsMode(material, normalsMode, GetMaterialFixedNormalsBackfaceRenderingOn(material));
                }

                //Reset fixed normal to default (Vector3.forward for model-space, -Vector3.forward for view-space).
                this._fixedNormal.vectorValue = new Vector4(0.0f, 0.0f, normalsMode == eNormalsMode.FixedNormalsViewSpace ? 1.0f : -1.0f, 1.0f);

                mixedNormalsMode = false;
                dataChanged = true;
            }

            //Show fixed normal
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = this._fixedNormal.hasMixedValue;
            var normal = EditorGUILayout.Vector3Field(_fixedNormalDirectionText, this._fixedNormal.vectorValue);
            if (EditorGUI.EndChangeCheck())
            {
                this._fixedNormal.vectorValue = new Vector4(normal.x, normal.y, normal.z, 1.0f);
                dataChanged = true;
            }

            //Show adjust for back face rendering
            {
                var fixBackFaceRendering = GetMaterialFixedNormalsBackfaceRenderingOn((Material)this._materialEditor.target);
                var mixedBackFaceRendering = false;
                foreach (Material material in this._materialEditor.targets)
                {
                    if (fixBackFaceRendering != GetMaterialFixedNormalsBackfaceRenderingOn(material))
                    {
                        mixedBackFaceRendering = true;
                        break;
                    }
                }

                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = mixedBackFaceRendering;
                var backRendering = EditorGUILayout.Toggle(_adjustBackfaceTangentText, fixBackFaceRendering);

                if (EditorGUI.EndChangeCheck())
                {
                    SetNormalsMode(this._materialEditor, normalsMode, backRendering);
                    dataChanged = true;
                }
            }

        }

        EditorGUI.showMixedValue = false;

        return dataChanged;
    }

    protected virtual bool RenderDiffuseRampProperties()
    {
        var dataChanged = false;

        var rampMode = GetMaterialDiffuseRampMode((Material)this._materialEditor.target);
        var mixedRampMode = false;
        foreach (Material material in this._materialEditor.targets)
        {
            if (rampMode != GetMaterialDiffuseRampMode(material))
            {
                mixedRampMode = true;
                break;
            }
        }

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = mixedRampMode;
        EditorGUILayout.BeginHorizontal();

        if (this._diffuseRamp != null)
            this._materialEditor.TexturePropertySingleLine(_diffuseRampText, this._diffuseRamp);

        if (EditorGUI.EndChangeCheck())
        {
            if (rampMode == eDiffuseRampMode.NoRampSpecified)
                rampMode = eDiffuseRampMode.DefaultRampMode;

            SetDiffuseRampMode(this._materialEditor, rampMode);
            mixedRampMode = false;
            dataChanged = true;
        }

        if (this._diffuseRamp != null && this._diffuseRamp.textureValue != null)
        {
            //Show drop down for ramp mode
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = mixedRampMode;
            rampMode = (eDiffuseRampMode)EditorGUILayout.Popup((int)rampMode, _fixedDiffuseRampModeOptions);
            if (EditorGUI.EndChangeCheck())
            {
                SetDiffuseRampMode(this._materialEditor, rampMode);
                mixedRampMode = false;
                dataChanged = true;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.showMixedValue = false;

        return dataChanged;
    }

    protected virtual bool RenderShadowsProperties()
    {
        var dataChanged = false;

        EditorGUI.BeginChangeCheck();
        this._materialEditor.RangeProperty(this._shadowAlphaCutoff, _shadowAlphaCutoffText.text);
        dataChanged = EditorGUI.EndChangeCheck();
        var hasReceiveShadowsParameter = IsLWRPShader(this._materialEditor, out var areMixedShaders) ||
            IsURP3DShader(this._materialEditor, out areMixedShaders);

        if (hasReceiveShadowsParameter)
        {
            EditorGUI.BeginChangeCheck();
            var enableReceive = !IsKeywordEnabled(this._materialEditor, "_RECEIVE_SHADOWS_OFF", out var mixedValue);
            EditorGUI.showMixedValue = mixedValue;
            enableReceive = EditorGUILayout.Toggle(_receiveShadowsText, enableReceive);

            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                SetKeyword(this._materialEditor, "_RECEIVE_SHADOWS_OFF", !enableReceive);
                dataChanged = true;
            }
        }

        return dataChanged;
    }

    protected virtual bool RenderSphericalHarmonicsProperties()
    {

        var isLWRPShader = IsLWRPShader(this._materialEditor, out var areMixedShaders);
        var isURP3DShader = IsURP3DShader(this._materialEditor, out areMixedShaders);
        var isURP2DShader = IsURP2DShader(this._materialEditor, out areMixedShaders);
        var hasSHParameter = !(isLWRPShader || isURP3DShader || isURP2DShader);
        if (!hasSHParameter)
            return false;

        EditorGUI.BeginChangeCheck();
        var enabled = IsKeywordEnabled(this._materialEditor, "_SPHERICAL_HARMONICS", out var mixedValue);
        EditorGUI.showMixedValue = mixedValue;
        enabled = BoldToggleField(_sphericalHarmonicsText, enabled);
        EditorGUI.showMixedValue = false;

        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword(this._materialEditor, "_SPHERICAL_HARMONICS", enabled);
            return true;
        }

        return false;
    }

    protected virtual bool RenderFogProperties()
    {

        var isURP2DShader = IsURP2DShader(this._materialEditor, out var areMixedShaders);

        if (isURP2DShader && !areMixedShaders)
            return false;

        EditorGUI.BeginChangeCheck();
        var fog = IsKeywordEnabled(this._materialEditor, "_FOG", out var mixedValue);
        EditorGUI.showMixedValue = mixedValue;
        fog = BoldToggleField(_fogToggleText, fog);
        EditorGUI.showMixedValue = false;

        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword(this._materialEditor, "_FOG", fog);
            return true;
        }

        return false;
    }

    protected virtual bool RenderColorProperties()
    {
        var dataChanged = false;

        EditorGUI.BeginChangeCheck();
        var colorAdjust = IsKeywordEnabled(this._materialEditor, "_COLOR_ADJUST", out var mixedValue);
        EditorGUI.showMixedValue = mixedValue;
        colorAdjust = BoldToggleField(_colorAdjustmentToggleText, colorAdjust);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword(this._materialEditor, "_COLOR_ADJUST", colorAdjust);
            mixedValue = false;
            dataChanged = true;
        }

        if (colorAdjust && !mixedValue)
        {
            EditorGUI.BeginChangeCheck();
            this._materialEditor.ColorProperty(this._overlayColor, _colorAdjustmentColorText.text);
            this._materialEditor.RangeProperty(this._hue, _colorAdjustmentHueText.text);
            this._materialEditor.RangeProperty(this._saturation, _colorAdjustmentSaturationText.text);
            this._materialEditor.RangeProperty(this._brightness, _colorAdjustmentBrightnessText.text);
            dataChanged |= EditorGUI.EndChangeCheck();
        }

        return dataChanged;
    }

    protected virtual bool RenderSpecularProperties()
    {
        var dataChanged = false;

        var specular = IsKeywordEnabled(this._materialEditor, "_SPECULAR", out var mixedSpecularValue);
        var specularGlossMap = IsKeywordEnabled(this._materialEditor, "_SPECULAR_GLOSSMAP", out var mixedSpecularGlossMapValue);
        var mixedValue = mixedSpecularValue || mixedSpecularGlossMapValue;

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = mixedValue;
        var specularEnabled = BoldToggleField(_specularToggleText, specular || specularGlossMap);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material material in this._materialEditor.targets)
            {
                var hasGlossMap = material.GetTexture("_MetallicGlossMap") != null;
                SetKeyword(material, "_SPECULAR", specularEnabled && !hasGlossMap);
                SetKeyword(material, "_SPECULAR_GLOSSMAP", specularEnabled && hasGlossMap);
            }

            mixedValue = false;
            dataChanged = true;
        }

        if (specularEnabled && !mixedValue)
        {
            EditorGUI.BeginChangeCheck();
            var hasGlossMap = this._metallicGlossMap.textureValue != null;
            this._materialEditor.TexturePropertySingleLine(_metallicMapText, this._metallicGlossMap, hasGlossMap ? null : this._metallic);
            if (EditorGUI.EndChangeCheck())
            {
                hasGlossMap = this._metallicGlossMap.textureValue != null;
                SetKeyword(this._materialEditor, "_SPECULAR", !hasGlossMap);
                SetKeyword(this._materialEditor, "_SPECULAR_GLOSSMAP", hasGlossMap);

                dataChanged = true;
            }

            const int indentation = 2;
            this._materialEditor.ShaderProperty(hasGlossMap ? this._smoothnessScale : this._smoothness, hasGlossMap ? _smoothnessScaleText : _smoothnessText, indentation);
        }

        return dataChanged;
    }

    protected virtual bool RenderEmissionProperties()
    {
        var dataChanged = false;

        var emission = IsKeywordEnabled(this._materialEditor, "_EMISSION", out var mixedValue);

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = mixedValue;
        emission = BoldToggleField(_emissionToggleText, emission);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword(this._materialEditor, "_EMISSION", emission);
            mixedValue = false;
            dataChanged = true;
        }

        if (emission && !mixedValue)
        {
            EditorGUI.BeginChangeCheck();

#if UNITY_2018_1_OR_NEWER
            this._materialEditor.TexturePropertyWithHDRColor(_emissionText, this._emissionMap, this._emissionColor, true);
#else
			_materialEditor.TexturePropertyWithHDRColor(_emissionText, _emissionMap, _emissionColor, new ColorPickerHDRConfig(0, 1, 0.01010101f, 3), true);
#endif
            this._materialEditor.FloatProperty(this._emissionPower, _emissionPowerText.text);
            dataChanged |= EditorGUI.EndChangeCheck();
        }

        return dataChanged;
    }

    protected virtual bool RenderRimLightingProperties()
    {
        var dataChanged = false;

        var rimLighting = IsKeywordEnabled(this._materialEditor, "_RIM_LIGHTING", out var mixedValue);

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = mixedValue;
        rimLighting = BoldToggleField(_rimLightingToggleText, rimLighting);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword(this._materialEditor, "_RIM_LIGHTING", rimLighting);
            mixedValue = false;
            dataChanged = true;
        }

        if (rimLighting && !mixedValue)
        {
            EditorGUI.BeginChangeCheck();
            this._materialEditor.ColorProperty(this._rimColor, _rimColorText.text);
            this._materialEditor.FloatProperty(this._rimPower, _rimPowerText.text);
            dataChanged |= EditorGUI.EndChangeCheck();
        }

        return dataChanged;
    }

    #endregion

    #region Private Functions

    private void RenderMeshInfoBox()
    {
        var material = (Material)this._materialEditor.target;
        var requiresNormals = this._fixedNormal != null && GetMaterialNormalsMode(material) == eNormalsMode.MeshNormals;
        var requiresTangents = material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null;

        if (requiresNormals || requiresTangents)
        {
            GUILayout.Label(requiresNormals && requiresTangents ? _meshRequiresNormalsAndTangentsText : requiresNormals ? _meshRequiresNormalsText : _meshRequiresTangentsText, GUI.skin.GetStyle("helpBox"));
        }
    }

    private void SetInt(string propertyName, int value)
    {
        foreach (Material material in this._materialEditor.targets)
        {
            material.SetInt(propertyName, value);
        }
    }

    private void SetDefaultSpriteKeywords(Material material, Shader shader)
    {
        //Disable emission by default (is set on by default in standard shader)
        SetKeyword(material, "_EMISSION", false);
        //Start with preMultiply alpha by default
        SetBlendMode(material, eBlendMode.PreMultipliedAlpha);
        SetDiffuseRampMode(material, eDiffuseRampMode.DefaultRampMode);
        //Start with mesh normals by default
        SetNormalsMode(material, eNormalsMode.MeshNormals, false);
        if (this._fixedNormal != null)
            this._fixedNormal.vectorValue = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        //Start with spherical harmonics disabled?
        SetKeyword(material, "_SPHERICAL_HARMONICS", false);
        //Start with specular disabled
        SetKeyword(material, "_SPECULAR", false);
        SetKeyword(material, "_SPECULAR_GLOSSMAP", false);
        //Start with Culling disabled
        material.SetInt("_Cull", (int)eCulling.Off);
        //Start with Z writing disabled
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);
    }

    //Z write is on then

    private static void SetRenderType(Material material, string renderType, bool useCustomRenderQueue)
    {
        //Want a check box to say if should use Sprite render queue (for custom writing depth and normals)
        var zWrite = HasZWriteEnabled(material);

        if (useCustomRenderQueue)
        {
            //If sprite has fixed normals then assign custom render type so we can write its correct normal with soft edges
            var normalsMode = GetMaterialNormalsMode(material);

            switch (normalsMode)
            {
                case eNormalsMode.FixedNormalsViewSpace:
                    renderType = "SpriteViewSpaceFixedNormal";
                    break;
                case eNormalsMode.FixedNormalsModelSpace:
                    renderType = "SpriteModelSpaceFixedNormal";
                    break;
                case eNormalsMode.MeshNormals:
                    {
                        //If sprite doesn't write to depth assign custom render type so we can write its depth with soft edges
                        if (!zWrite)
                        {
                            renderType = "Sprite";
                        }
                    }
                    break;
            }
        }

        //If we don't write to depth set tag so custom shaders can write to depth themselves
        material.SetOverrideTag("AlphaDepth", zWrite ? "False" : "True");

        material.SetOverrideTag("RenderType", renderType);
    }

    private static void SetMaterialKeywords(Material material)
    {
        var blendMode = GetMaterialBlendMode(material);
        SetBlendMode(material, blendMode);

        var zWrite = HasZWriteEnabled(material);
        var clipAlpha = zWrite && blendMode != eBlendMode.Opaque && material.GetFloat("_Cutoff") > 0.0f;
        SetKeyword(material, "_ALPHA_CLIP", clipAlpha);

        var normalMap = material.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != null;
        SetKeyword(material, "_NORMALMAP", normalMap);

        var diffuseRamp = material.HasProperty("_DiffuseRamp") && material.GetTexture("_DiffuseRamp") != null;
        SetKeyword(material, "_DIFFUSE_RAMP", diffuseRamp);

        var blendTexture = material.HasProperty("_BlendTex") && material.GetTexture("_BlendTex") != null;
        SetKeyword(material, "_TEXTURE_BLEND", blendTexture);
    }

    private static void MaterialChanged(MaterialEditor materialEditor)
    {
        foreach (Material material in materialEditor.targets)
            SetMaterialKeywords(material);
    }

    private static void SetKeyword(MaterialEditor m, string keyword, bool state)
    {
        foreach (Material material in m.targets)
        {
            SetKeyword(material, keyword, state);
        }
    }

    private static void SetKeyword(Material m, string keyword, bool state)
    {
        if (state)
            m.EnableKeyword(keyword);
        else
            m.DisableKeyword(keyword);
    }

    private static bool IsLWRPShader(MaterialEditor editor, out bool mixedValue)
    {
        return IsShaderType(kShaderLitLW, editor, out mixedValue);
    }

    private static bool IsURP3DShader(MaterialEditor editor, out bool mixedValue)
    {
        return IsShaderType(kShaderLitURP, editor, out mixedValue);
    }

    private static bool IsURP2DShader(MaterialEditor editor, out bool mixedValue)
    {
        return IsShaderType(kShaderLitURP2D, editor, out mixedValue);
    }

    private static bool IsShaderType(string shaderType, MaterialEditor editor, out bool mixedValue)
    {

        mixedValue = false;
        var isAnyTargetTypeShader = false;
        foreach (Material material in editor.targets)
        {
            if (material.shader.name == shaderType)
            {
                isAnyTargetTypeShader = true;
            }
            else if (isAnyTargetTypeShader)
            {
                mixedValue = true;
            }
        }
        return isAnyTargetTypeShader;
    }

    private static bool IsKeywordEnabled(MaterialEditor editor, string keyword, out bool mixedValue)
    {
        var keywordEnabled = ((Material)editor.target).IsKeywordEnabled(keyword);
        mixedValue = false;

        foreach (Material material in editor.targets)
        {
            if (material.IsKeywordEnabled(keyword) != keywordEnabled)
            {
                mixedValue = true;
                break;
            }
        }

        return keywordEnabled;
    }

    private static eLightMode GetMaterialLightMode(Material material)
    {
        if (material.shader.name == kShaderPixelLit ||
            material.shader.name == kShaderPixelLitOutline)
        {
            return eLightMode.PixelLit;
        }
        else if (material.shader.name == kShaderUnlit ||
                material.shader.name == kShaderUnlitOutline)
        {
            return eLightMode.Unlit;
        }
        else if (material.shader.name == kShaderLitLW)
        {
            return eLightMode.LitLightweight;
        }
        else if (material.shader.name == kShaderLitURP)
        {
            return eLightMode.LitUniversal;
        }
        else if (material.shader.name == kShaderLitURP2D)
        {
            return eLightMode.LitUniversal2D;
        }
        else
        { // if (material.shader.name == kShaderVertexLit || kShaderVertexLitOutline)
            return eLightMode.VertexLit;
        }
    }

    private static eBlendMode GetMaterialBlendMode(Material material)
    {
        if (material.IsKeywordEnabled("_ALPHABLEND_ON"))
            return eBlendMode.StandardAlpha;
        if (material.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"))
            return eBlendMode.PreMultipliedAlpha;
        if (material.IsKeywordEnabled("_MULTIPLYBLEND"))
            return eBlendMode.Multiply;
        if (material.IsKeywordEnabled("_MULTIPLYBLEND_X2"))
            return eBlendMode.Multiplyx2;
        if (material.IsKeywordEnabled("_ADDITIVEBLEND"))
            return eBlendMode.Additive;
        if (material.IsKeywordEnabled("_ADDITIVEBLEND_SOFT"))
            return eBlendMode.SoftAdditive;

        return eBlendMode.Opaque;
    }

    private static void SetBlendMode(Material material, eBlendMode blendMode)
    {
        SetKeyword(material, "_ALPHABLEND_ON", blendMode == eBlendMode.StandardAlpha);
        SetKeyword(material, "_ALPHAPREMULTIPLY_ON", blendMode == eBlendMode.PreMultipliedAlpha);
        SetKeyword(material, "_MULTIPLYBLEND", blendMode == eBlendMode.Multiply);
        SetKeyword(material, "_MULTIPLYBLEND_X2", blendMode == eBlendMode.Multiplyx2);
        SetKeyword(material, "_ADDITIVEBLEND", blendMode == eBlendMode.Additive);
        SetKeyword(material, "_ADDITIVEBLEND_SOFT", blendMode == eBlendMode.SoftAdditive);

        int renderQueue;
        var useCustomRenderQueue = material.GetFloat("_CustomRenderQueue") > 0.0f;

        switch (blendMode)
        {
            case eBlendMode.Opaque:
                {
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    SetRenderType(material, "Opaque", useCustomRenderQueue);
                    renderQueue = kSolidQueue;
                }
                break;
            case eBlendMode.Additive:
                {
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    var zWrite = HasZWriteEnabled(material);
                    SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
                    renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
                }
                break;
            case eBlendMode.SoftAdditive:
                {
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                    var zWrite = HasZWriteEnabled(material);
                    SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
                    renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
                }
                break;
            case eBlendMode.Multiply:
                {
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
                    var zWrite = HasZWriteEnabled(material);
                    SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
                    renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
                }
                break;
            case eBlendMode.Multiplyx2:
                {
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
                    var zWrite = HasZWriteEnabled(material);
                    SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
                    renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
                }
                break;
            default:
                {
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    var zWrite = HasZWriteEnabled(material);
                    SetRenderType(material, zWrite ? "TransparentCutout" : "Transparent", useCustomRenderQueue);
                    renderQueue = zWrite ? kAlphaTestQueue : kTransparentQueue;
                }
                break;
        }

        material.renderQueue = renderQueue + material.GetInt("_RenderQueue");
        material.SetOverrideTag("IgnoreProjector", blendMode == eBlendMode.Opaque ? "False" : "True");
    }

    private static eNormalsMode GetMaterialNormalsMode(Material material)
    {
        if (material.IsKeywordEnabled("_FIXED_NORMALS_VIEWSPACE") || material.IsKeywordEnabled("_FIXED_NORMALS_VIEWSPACE_BACKFACE"))
            return eNormalsMode.FixedNormalsViewSpace;
        if (material.IsKeywordEnabled("_FIXED_NORMALS_WORLDSPACE"))
            return eNormalsMode.FixedNormalsWorldSpace;
        if (material.IsKeywordEnabled("_FIXED_NORMALS_MODELSPACE") || material.IsKeywordEnabled("_FIXED_NORMALS_MODELSPACE_BACKFACE"))
            return eNormalsMode.FixedNormalsModelSpace;

        return eNormalsMode.MeshNormals;
    }


    private static void SetNormalsMode(MaterialEditor materialEditor, eNormalsMode normalsMode, bool allowBackFaceRendering)
    {
        foreach (Material material in materialEditor.targets)
        {
            SetNormalsMode(material, normalsMode, allowBackFaceRendering);
        }
    }

    private static void SetNormalsMode(Material material, eNormalsMode normalsMode, bool allowBackFaceRendering)
    {
        SetKeyword(material, "_FIXED_NORMALS_VIEWSPACE", normalsMode == eNormalsMode.FixedNormalsViewSpace && !allowBackFaceRendering);
        SetKeyword(material, "_FIXED_NORMALS_VIEWSPACE_BACKFACE", normalsMode == eNormalsMode.FixedNormalsViewSpace && allowBackFaceRendering);
        SetKeyword(material, "_FIXED_NORMALS_WORLDSPACE", normalsMode == eNormalsMode.FixedNormalsWorldSpace);
        SetKeyword(material, "_FIXED_NORMALS_MODELSPACE", normalsMode == eNormalsMode.FixedNormalsModelSpace && !allowBackFaceRendering);
        SetKeyword(material, "_FIXED_NORMALS_MODELSPACE_BACKFACE", normalsMode == eNormalsMode.FixedNormalsModelSpace && allowBackFaceRendering);
    }

    private static bool GetMaterialFixedNormalsBackfaceRenderingOn(Material material)
    {
        return material.IsKeywordEnabled("_FIXED_NORMALS_VIEWSPACE_BACKFACE") || material.IsKeywordEnabled("_FIXED_NORMALS_MODELSPACE_BACKFACE");
    }

    private static eDiffuseRampMode GetMaterialDiffuseRampMode(Material material)
    {
        if (material.IsKeywordEnabled("_FULLRANGE_HARD_RAMP"))
            return eDiffuseRampMode.FullRangeHard;
        if (material.IsKeywordEnabled("_FULLRANGE_SOFT_RAMP"))
            return eDiffuseRampMode.FullRangeSoft;
        if (material.IsKeywordEnabled("_OLD_HARD_RAMP"))
            return eDiffuseRampMode.OldHard;
        if (material.IsKeywordEnabled("_OLD_SOFT_RAMP"))
            return eDiffuseRampMode.OldSoft;

        return eDiffuseRampMode.NoRampSpecified;
    }

    private static void SetDiffuseRampMode(MaterialEditor materialEditor, eDiffuseRampMode rampMode)
    {
        foreach (Material material in materialEditor.targets)
        {
            SetDiffuseRampMode(material, rampMode);
        }
    }

    private static void SetDiffuseRampMode(Material material, eDiffuseRampMode rampMode)
    {
        SetKeyword(material, "_FULLRANGE_HARD_RAMP", rampMode == eDiffuseRampMode.FullRangeHard);
        SetKeyword(material, "_FULLRANGE_SOFT_RAMP", rampMode == eDiffuseRampMode.FullRangeSoft);
        SetKeyword(material, "_OLD_HARD_RAMP", rampMode == eDiffuseRampMode.OldHard);
        SetKeyword(material, "_OLD_SOFT_RAMP", rampMode == eDiffuseRampMode.OldSoft);
    }

    private static bool HasZWriteEnabled(Material material)
    {
        if (material.HasProperty("_ZWrite"))
        {
            return material.GetFloat("_ZWrite") > 0.0f;
        }
        else return true; // Pixel Lit shader always has _ZWrite enabled.
    }
    #endregion
}
