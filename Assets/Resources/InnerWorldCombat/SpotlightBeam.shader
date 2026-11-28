Shader "InnerWorld/SpotlightBeam"
{
    Properties
    {
        _BeamColor ("Beam Color", Color) = (1,1,1,0.42)
        [HideInInspector] _FloorRectCount ("Floor rectangle count", Float) = -1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 worldXY : TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BeamColor;
                float _FloorRectCount;
                float4 _FloorRects[16];
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.worldXY = TransformObjectToWorld(input.positionOS).xy;
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                if (_FloorRectCount >= 0)
                {
                    bool onFloor = false;
                    for (int i = 0; i < (int)_FloorRectCount; i++)
                    {
                        float4 rect = _FloorRects[i];
                        onFloor = onFloor || (all(input.worldXY >= rect.xy) && all(input.worldXY <= rect.zw));
                    }
                    clip(onFloor ? 1.0 : -1.0);
                }
                float radius = saturate(length(input.uv));
                float fill = 0.55 * pow(radius, 3.5);
                float rim = smoothstep(0.982, 0.994, radius);
                return half4(_BeamColor.rgb, saturate(_BeamColor.a * (fill + 1.6 * rim)));
            }
            ENDHLSL
        }
    }
}
