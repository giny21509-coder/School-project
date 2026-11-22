Shader "GraduationGame/2D/Background Blur Lit"
{
    Properties
    {
        [PerRendererData] _MainTex("Diffuse", 2D) = "white" {}
        _BlurRadius("Blur Radius (Texture Pixels)", Range(0, 4)) = 0
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_TexelSize;
                float _BlurRadius;
            CBUFFER_END

            // Inline sampler keeps small radii smooth even on Point-filtered sprites.
            // sampler_LinearClamp is supplied by URP Core.

            half4 SampleBackgroundBlur(float2 uv)
            {
                half4 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (_BlurRadius >= 0.001)
                {

                // Separable 5x5 binomial kernel, accumulated in premultiplied alpha
                // to avoid dark fringes around transparent artwork.
                // Binomial weights are computed explicitly for cross-platform compilers.
                float4 sum = 0;
                float2 stepUV = _MainTex_TexelSize.xy * (_BlurRadius * 0.5);
                [unroll]
                for (int y = 0; y < 5; ++y)
                {
                    [unroll]
                    for (int x = 0; x < 5; ++x)
                    {
                        float2 sampleUV = uv + float2(x - 2, y - 2) * stepUV;
                        float4 sampleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, sampleUV);
                        sampleColor.rgb *= sampleColor.a;
                        float wx = x == 2 ? 6.0 : ((x == 1 || x == 3) ? 4.0 : 1.0);
                        float wy = y == 2 ? 6.0 : ((y == 1 || y == 3) ? 4.0 : 1.0);
                        sum += sampleColor * (wx * wy);
                    }
                }
                sum *= (1.0 / 256.0);
                result = half4(sum.rgb / max(sum.a, 0.00001), sum.a);
                }
                return result;
            }

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                const half4 main = input.color * SampleBackgroundBlur(input.uv);
                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                #if defined(DEBUG_DISPLAY)
                    SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);
                    surfaceData.normalWS = input.normalWS;
                #endif
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float4 _MainTex_TexelSize;
                float _BlurRadius;
            CBUFFER_END

            // Inline sampler keeps small radii smooth even on Point-filtered sprites.
            // sampler_LinearClamp is supplied by URP Core.

            half4 SampleBackgroundBlur(float2 uv)
            {
                half4 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (_BlurRadius >= 0.001)
                {

                // Separable 5x5 binomial kernel, accumulated in premultiplied alpha
                // to avoid dark fringes around transparent artwork.
                // Binomial weights are computed explicitly for cross-platform compilers.
                float4 sum = 0;
                float2 stepUV = _MainTex_TexelSize.xy * (_BlurRadius * 0.5);
                [unroll]
                for (int y = 0; y < 5; ++y)
                {
                    [unroll]
                    for (int x = 0; x < 5; ++x)
                    {
                        float2 sampleUV = uv + float2(x - 2, y - 2) * stepUV;
                        float4 sampleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, sampleUV);
                        sampleColor.rgb *= sampleColor.a;
                        float wx = x == 2 ? 6.0 : ((x == 1 || x == 3) ? 4.0 : 1.0);
                        float wy = y == 2 ? 6.0 : ((y == 1 || y == 3) ? 4.0 : 1.0);
                        sum += sampleColor * (wx * wy);
                    }
                }
                sum *= (1.0 / 256.0);
                result = half4(sum.rgb / max(sum.a, 0.00001), sum.a);
                }
                return result;
            }

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_TexelSize;
                float _BlurRadius;
            CBUFFER_END

            // Inline sampler keeps small radii smooth even on Point-filtered sprites.
            // sampler_LinearClamp is supplied by URP Core.

            half4 SampleBackgroundBlur(float2 uv)
            {
                half4 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                if (_BlurRadius >= 0.001)
                {

                // Separable 5x5 binomial kernel, accumulated in premultiplied alpha
                // to avoid dark fringes around transparent artwork.
                // Binomial weights are computed explicitly for cross-platform compilers.
                float4 sum = 0;
                float2 stepUV = _MainTex_TexelSize.xy * (_BlurRadius * 0.5);
                [unroll]
                for (int y = 0; y < 5; ++y)
                {
                    [unroll]
                    for (int x = 0; x < 5; ++x)
                    {
                        float2 sampleUV = uv + float2(x - 2, y - 2) * stepUV;
                        float4 sampleColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, sampleUV);
                        sampleColor.rgb *= sampleColor.a;
                        float wx = x == 2 ? 6.0 : ((x == 1 || x == 3) ? 4.0 : 1.0);
                        float wy = y == 2 ? 6.0 : ((y == 1 || y == 3) ? 4.0 : 1.0);
                        sum += sampleColor * (wx * wy);
                    }
                }
                sum *= (1.0 / 256.0);
                result = half4(sum.rgb / max(sum.a, 0.00001), sum.a);
                }
                return result;
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return input.color * SampleBackgroundBlur(input.uv);
            }
            ENDHLSL
        }
    }
}
