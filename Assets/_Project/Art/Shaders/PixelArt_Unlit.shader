// Sprite prerenderizado del flujo Blender -> Pixel Art (Docs/Arte_Olmeca_Demo.md §10): la luz ya viene horneada,
// así que no se ilumina. Compatible con SpriteRenderer (color, flipX/flipY), SRP Batcher e instancing.
// Recorte alfa (los sprites no tienen semitransparencias). Con ZWrite activo, un sprite orientado al plano de la
// cámara escribe una profundidad constante: la de su pivote. Eso reproduce el orden por punto de apoyo de la
// composición sin ordenar los renderers. El suelo horneado y el agua van con ZWrite desactivado y una cola anterior.
Shader "Cronicas/Pixel Art Unlit"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tinte", Color) = (1, 1, 1, 1)
        _Cutoff ("Umbral alfa", Range(0, 1)) = 0.5
        [Enum(Off, 0, On, 1)] _ZWrite ("Escribir profundidad", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite [_ZWrite]
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Cutoff;
                half _ZWrite;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                output.positionCS = TransformObjectToHClip(UnityFlipSprite(input.positionOS, unity_SpriteProps.xy));
                output.color = input.color * _Color * unity_SpriteColor;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                clip(color.a - _Cutoff);
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
