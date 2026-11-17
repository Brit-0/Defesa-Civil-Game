// Janelas acesas das casas à noite (docs/jam/ENERGIA_DIA_NOITE.md). Fica num SpriteRenderer filho "[W]" do Visual de
// cada casa, com o mesmo sprite: acende só os pixels de janela (amarelo quente ou vidro azul claro, pela cor) e soma
// por cima, fora da luz 2D, passando de 1 para o bloom pegar. A cor do SpriteRenderer manda: r liga a chave quente,
// g a do vidro e a é o quanto está aceso (DayNightCycle). Calibrado em scripts/v4b/mascara_janelas.py (analises-lilith).
Shader "DefesaCivil/Window Glow"
{
    Properties
    {
        _MainTex("Sprite", 2D) = "white" {}
        _GlowColor("Cor da janela", Color) = (1, 0.78, 0.42, 1)
        _Intensity("Intensidade", Float) = 1.6

        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex WindowVertex
            #pragma fragment WindowFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                half4 color         : COLOR;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _GlowColor;
                half _Intensity;
            CBUFFER_END

            Varyings WindowVertex(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 WindowFragment(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // A chave é feita com a cor da arte (sRGB); no espaço linear a amostra chega linearizada.
                #if defined(UNITY_COLORSPACE_GAMMA)
                    half3 c = tex.rgb;
                #else
                    half3 c = pow(max(tex.rgb, 0.0), 1.0 / 2.2);
                #endif
                half mx = max(c.r, max(c.g, c.b));
                half mn = min(c.r, min(c.g, c.b));
                half s = (mx - mn) / max(mx, 1e-4);

                half warm = (c.r >= c.g) * (c.g > c.b) * ((c.g - c.b) / max(c.r - c.b, 1e-4) > 0.45) * (s > 0.3) * (mx > 0.82);
                half glass = (c.b > c.r + 0.1) * (c.g > c.r) * (s > 0.12) * (s < 0.45) * (mx > 0.9);
                half mask = max(warm * input.color.r, glass * input.color.g) * (tex.a > 0.5);

                return half4(_GlowColor.rgb * _Intensity, mask * input.color.a);
            }
            ENDHLSL
        }
    }
}
