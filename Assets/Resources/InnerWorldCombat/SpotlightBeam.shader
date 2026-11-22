Shader "InnerWorld/SpotlightBeam"
{
    Properties { _BeamColor ("Beam Color", Color) = (1,0.97,0.85,0.42) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BeamColor;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float radius = input.uv.x;
                float angle = abs((input.uv.y - 0.5) * 2.0 / max(radius, 0.001));
                float softEdge = 1.0 - smoothstep(0.45, 1.0, angle);
                float core = exp2(-angle * angle * 3.5);
                float reach = 1.0 - smoothstep(0.65, 1.0, radius);
                float source = smoothstep(0.0, 0.025, radius);
                float density = source * reach * softEdge * (0.25 + core * 0.75)
                    / (1.0 + radius * radius * 2.0);
                return half4(_BeamColor.rgb, _BeamColor.a * density);
            }
            ENDHLSL
        }
    }
}
