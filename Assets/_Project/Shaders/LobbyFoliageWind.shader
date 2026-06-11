Shader "Last Seed/2D/Lobby Foliage Wind"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _WindStrength ("Wind Strength", Float) = 0.024
        _WindSpeed ("Wind Speed", Float) = 5.2
        _PulseSpeed ("Pulse Speed", Float) = 0.52
        _PulseThreshold ("Pulse Threshold", Float) = 0.8
        _InfluenceStart ("Influence Start", Float) = 0.46
        _InfluencePower ("Influence Power", Float) = 2.4
        _WaveFrequency ("Wave Frequency", Float) = 5.8
        _PhaseOffset ("Phase Offset", Float) = 0
        [MaterialToggle] _ZWrite ("ZWrite", Float) = 0

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
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

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _WindStrength;
                float _WindSpeed;
                float _PulseSpeed;
                float _PulseThreshold;
                float _InfluenceStart;
                float _InfluencePower;
                float _WaveFrequency;
                float _PhaseOffset;
            CBUFFER_END

            float GetFoliageWindOffset(float uvY)
            {
                float influenceRange = max(0.0001, 1.0 - _InfluenceStart);
                float influence = saturate((uvY - _InfluenceStart) / influenceRange);
                influence = pow(influence, max(0.0001, _InfluencePower));

                float gust = sin(_Time.y * _PulseSpeed + _PhaseOffset) * 0.5 + 0.5;
                gust = smoothstep(_PulseThreshold, 1.0, gust);

                float wave = sin(_Time.y * _WindSpeed + uvY * _WaveFrequency + _PhaseOffset);
                return wave * _WindStrength * influence * gust;
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                input.positionOS.x += GetFoliageWindOffset(input.uv.y);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
