Shader "Custom/HolyAuroraStars_URP"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)

        // Aurora
        _BeamTint ("Aurora Tint", Color) = (1,0.9,0.3,1)
        _Intensity ("Aurora Intensity", Range(0,5)) = 2
        _Opacity ("Aurora Opacity", Range(0,1)) = 1

        _BeamCenterX ("Beam Center X", Range(0,1)) = 0.5
        _BeamWidth ("Beam Width", Range(0.01,0.5)) = 0.2
        _BeamSoftness ("Beam Softness", Range(0.001,0.5)) = 0.15

        _AuroraSpeed ("Aurora Speed", Range(0,3)) = 0.6
        _NoiseScale ("Aurora Noise Scale", Range(1,40)) = 10
        _NoiseStrength ("Aurora Noise Strength", Range(0,0.5)) = 0.12

        // Stars
        _StarDensity ("Star Density", Range(1,200)) = 80
        _StarSpeed ("Star Fall Speed", Range(0,5)) = 1.2
        _StarBrightness ("Star Brightness", Range(0,5)) = 2
        _StarSize ("Star Size", Range(0.001,0.02)) = 0.004

        // Pixelation
        _PixelSize ("Pixel Size", Range(16,512)) = 128

        _GlobalFade ("Global Fade", Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

            float4 _MainTex_ST;
            float4 _Color;

            float4 _BeamTint;
            float _Intensity;
            float _Opacity;

            float _BeamCenterX;
            float _BeamWidth;
            float _BeamSoftness;

            float _AuroraSpeed;
            float _NoiseScale;
            float _NoiseStrength;

            float _StarDensity;
            float _StarSpeed;
            float _StarBrightness;
            float _StarSize;

            float _PixelSize;
            float _GlobalFade;

            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };


            // -------- HASH --------

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.45);
                return frac(p.x * p.y);
            }


            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));

                float2 u = f*f*(3-2*f);

                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }


            float2 Pixelate(float2 uv)
            {
                uv *= _PixelSize;
                uv = floor(uv);
                uv /= _PixelSize;
                return uv;
            }


            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;

                return OUT;
            }


            // -------- AURORA --------

            float Aurora(float2 uv)
            {
                float time = _Time.y;

                float dx = abs(uv.x - _BeamCenterX);

                float beam = 1 - smoothstep(
                    _BeamWidth,
                    _BeamWidth + _BeamSoftness,
                    dx);

                float n = noise(
                    float2(
                        uv.x * _NoiseScale,
                        uv.y * _NoiseScale - time * _AuroraSpeed));

                float shimmer =
                    1 + (n - 0.5) * _NoiseStrength;

                float verticalFade = smoothstep(0, 1, uv.y);

                return beam * shimmer * verticalFade;
            }


            // -------- FALLING STARS --------

            float Stars(float2 uv)
            {
                float time = _Time.y * _StarSpeed;

                float2 grid = uv * _StarDensity;

                float2 id = floor(grid);

                float star = hash21(id);

                // falling motion
                float yOffset =
                    frac(star + time);

                float starY =
                    abs(frac(grid.y + time + star) - 0.5);

                float starX =
                    abs(frac(grid.x + star) - 0.5);

                float d =
                    max(starX, starY);

                float shape =
                    smoothstep(
                        _StarSize,
                        0,
                        d);

                return shape * star;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = Pixelate(IN.uv);

                half4 tex =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv) * IN.color;


                float aurora =
                    Aurora(uv)
                    * _Opacity
                    * _Intensity;

                float stars =
                    Stars(uv)
                    * _StarBrightness;

                float3 light =
                    _BeamTint.rgb
                    * (aurora + stars);

                float3 finalColor =
                    tex.rgb + light;

                float alpha =
                    tex.a
                    * saturate(aurora + stars)
                    * _GlobalFade;

                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }
}
