Shader "Graduation/Screen Edge Fog"
{
    Properties
    {
        _Fog_color ("Fog Color", Color) = (0.36,0.36,0.36,1)
        _Fog_Size ("Smoke Scale", Range(1,40)) = 5
        _FogSpeed ("Smoke Speed", Vector) = (0.055,0.018,0,0)
        _Density ("Density", Range(0,2)) = 0.72
        _ClearProgress ("Clear Progress", Range(0,1)) = 0
        _ClearRadius ("Clear Center", Range(0,1)) = 0.48
        _EdgeSoftness ("Soft Edge", Range(0.05,0.6)) = 0.32
        _EdgeDarkness ("Outer Ring Darkness", Range(0,1)) = 0.48
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Fog_color;
                float4 _FogSpeed;
                float _Fog_Size, _Density, _ClearProgress, _ClearRadius, _EdgeSoftness, _EdgeDarkness;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 screenPos : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.color = input.color;
                return output;
            }
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float Noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), f.x),
                    lerp(Hash(cell + float2(0,1)), Hash(cell + 1.0), f.x), f.y);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.screenPos.xy / input.screenPos.w;
                float2 p = (uv - 0.5) * float2(_ScreenParams.x / _ScreenParams.y, 1.0);
                p *= _Fog_Size;
                float2 drift = _Time.y * _FogSpeed.xy;
                // Two crossing cloud layers slowly curl and drift instead of sliding as one image.
                float2 flow = float2(Noise(p * 0.45 + drift * 0.7),
                    Noise(p * 0.45 - drift * 0.5 + 8.2)) - 0.5;
                float smoke = Noise(p + flow * 1.6 + drift) * 0.60
                    + Noise(p * 1.85 - flow * 0.8 - drift * 0.55 + 7.1) * 0.28
                    + Noise(p * 3.7 + drift * 1.2 + 19.3) * 0.12;
                // A rounded rectangle keeps fog on all four sides and leaves the middle clear.
                float2 edge = abs(uv * 2.0 - 1.0);
                float distance = pow(pow(edge.x, 6.0) + pow(edge.y, 6.0), 1.0 / 6.0);
                float progress = saturate(_ClearProgress);
                float radius = lerp(_ClearRadius, 0.98, progress);
                // Spread the outer-to-inner fade across a wider band, with a clear center.
                float softness = max(0.01, _EdgeSoftness);
                float gradientStart = max(0.12, radius - softness * 0.75);
                float gradientEnd = radius + softness * 1.5;
                float mask = smoothstep(gradientStart, gradientEnd,
                    distance + (smoke - 0.5) * 0.10);
                float wisps = smoothstep(0.20, 0.80, smoke);
                // A little lingering mist reaches inward while keeping the player's center readable.
                float innerMist = smoothstep(max(0.12, radius - 0.26), radius + 0.05, distance);
                mask = max(mask, innerMist * wisps * 0.14);
                float alpha = min(0.65, mask * (0.28 + wisps * 0.45) * _Density)
                    * (1.0 - progress) * _Fog_color.a * input.color.a;
                half3 fogColor = _Fog_color.rgb * (0.65 + wisps * 0.75);
                // Composite a smooth dark outer ring over the drifting mist.
                float darkStart = lerp(max(0.36, _ClearRadius - 0.05), 0.98, progress);
                float darkAlpha = smoothstep(darkStart, 1.04, distance) * saturate(_EdgeDarkness)
                    * (1.0 - progress) * _Fog_color.a * input.color.a;
                float combinedAlpha = alpha + darkAlpha * (1.0 - alpha);
                half3 combinedColor = fogColor * input.color.rgb * alpha * (1.0 - darkAlpha)
                    / max(0.0001, combinedAlpha);
                return half4(combinedColor, saturate(combinedAlpha));
            }
            ENDHLSL
        }
    }
}
