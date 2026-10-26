// Sprite iluminado para el renderer 3D de URP (estilo HD-2D).
// Compatible con SpriteRenderer (color, flipX/flipY), SRP Batcher, Forward y Forward+.
// Es opaco con recorte alfa: escribe profundidad, así que el DOF y las sombras lo tratan como geometría.
Shader "Cronicas/HD2D Sprite Lit"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        [MainColor] _Color ("Tinte", Color) = (1, 1, 1, 1)
        _Cutoff ("Umbral alfa", Range(0, 1)) = 0.5
        _NormalUp ("Normal hacia arriba", Range(0, 1)) = 0.6
        _LightWrap ("Envoltura de luz", Range(0, 1)) = 0.5
        _ShadowSampleOffset ("Desfase de muestreo de sombra (m)", Range(0, 1)) = 0.3
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

        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        // Mismo layout en todas las pasadas: requisito del SRP Batcher.
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _Cutoff;
            half _NormalUp;
            half _LightWrap;
            half _ShadowSampleOffset;
        CBUFFER_END

        // Aplica flipX/flipY del SpriteRenderer, que se resuelve en el shader y no en la malla.
        float3 SpritePositionOS(float3 positionOS)
        {
            SetUpSpriteInstanceProperties();
            // URP no entrega unity_SpriteProps a las pasadas de sombra y profundidad: llega en 0 y aplastaría
            // la geometría. Se trata como "sin volteo" (CharacterSpriteFacing voltea con la escala, no con flipX).
            float2 flip = unity_SpriteProps.xy;
            flip = lerp(float2(1, 1), flip, step(0.5, abs(flip)));
            return UnityFlipSprite(positionOS, flip);
        }

        // El sprite mira a la cámara por su -Z local. Inclinar la normal hacia arriba hace que lo ilumine
        // el sol como al suelo, en vez de oscurecerse cuando la luz viene de detrás.
        half3 SpriteNormalWS()
        {
            half3 facingWS = TransformObjectToWorldDir(float3(0, 0, -1));
            return normalize(lerp(facingWS, half3(0, 1, 0), _NormalUp));
        }

        half4 SpriteColor(half4 vertexColor)
        {
            return vertexColor * _Color * unity_SpriteColor;
        }

        half4 SampleSprite(float2 uv, half4 color)
        {
            half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * color;
            clip(albedo.a - _Cutoff);
            return albedo;
        }

        // Silueta para sombras y profundidad: solo la textura y el tinte del material, para que teñir el
        // sprite (SpriteRenderer.color) no cambie la forma de su sombra.
        void ClipSpriteShape(float2 uv)
        {
            clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * _Color.a - _Cutoff);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 fogFactorAndVertexLight : TEXCOORD3; // x: niebla, yzw: luces adicionales por vértice
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half3 WrappedLambert(Light light, half3 normalWS)
            {
                half ndotl = saturate((dot(normalWS, light.direction) + _LightWrap) / (1.0h + _LightWrap));
                return light.color * (light.distanceAttenuation * light.shadowAttenuation * ndotl);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs position = GetVertexPositionInputs(SpritePositionOS(input.positionOS));
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = SpriteNormalWS();
                output.uv = input.uv;
                output.color = SpriteColor(input.color);
                output.fogFactorAndVertexLight.x = ComputeFogFactor(position.positionCS.z);
                output.fogFactorAndVertexLight.yzw = VertexLighting(position.positionWS, output.normalWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedo = SampleSprite(input.uv, input.color);
                half3 normalWS = normalize(input.normalWS);

                // La sombra se muestrea un poco hacia la luz para que el sprite no se sombree a sí mismo.
                float3 shadowPositionWS = input.positionWS + _MainLightPosition.xyz * _ShadowSampleOffset;
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(shadowPositionWS), shadowPositionWS, half4(1, 1, 1, 1));

                half3 lighting = SampleSH(normalWS) + WrappedLambert(mainLight, normalWS) + input.fogFactorAndVertexLight.yzw;

                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0; // LIGHT_LOOP_BEGIN lo necesita en Forward+
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();

                #if USE_FORWARD_PLUS
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    lighting += WrappedLambert(GetAdditionalLight(lightIndex, input.positionWS), normalWS);
                }
                #endif

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    lighting += WrappedLambert(GetAdditionalLight(lightIndex, input.positionWS), normalWS);
                LIGHT_LOOP_END
                #endif

                half3 color = MixFog(albedo.rgb * lighting, input.fogFactorAndVertexLight.x);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Los asigna URP al renderizar cada luz con sombras (ShadowUtils).
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 positionWS = TransformObjectToWorld(SpritePositionOS(input.positionOS));
                float3 normalWS = TransformObjectToWorldDir(float3(0, 0, -1));

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                ClipSpriteShape(input.uv);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(SpritePositionOS(input.positionOS));
                output.uv = input.uv;
                return output;
            }

            half Frag(Varyings input) : SV_Target
            {
                ClipSpriteShape(input.uv);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        // Necesaria para SSAO (nivel Alto) y efectos que leen las normales de la escena.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(SpritePositionOS(input.positionOS));
                output.uv = input.uv;
                output.normalWS = SpriteNormalWS();
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                ClipSpriteShape(input.uv);
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0h);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
