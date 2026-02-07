Shader "UI/DitherTransition_URP"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0,0,0,1)
        _Progress ("Progress", Range(0,1)) = 0
        _PixelSize ("Pixel Size", Range(16,1024)) = 240
        _Invert ("Invert", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Overlay"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Upgraded to 3.0 to support array indexing in builds
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Progress;
                float _PixelSize;
                float _Invert;
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
            };

            // Defined globally as static const for build-time stability
            static const int BayerMatrix[64] = {
                 0, 32,  8, 40,  2, 34, 10, 42,
                48, 16, 56, 24, 50, 18, 58, 26,
                12, 44,  4, 36, 14, 46,  6, 38,
                60, 28, 52, 20, 62, 30, 54, 22,
                 3, 35, 11, 43,  1, 33,  9, 41,
                51, 19, 59, 27, 49, 17, 57, 25,
                15, 47,  7, 39, 13, 45,  5, 37,
                63, 31, 55, 23, 61, 29, 53, 21
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float Bayer8(int x, int y)
            {
                int idx = (y & 7) * 8 + (x & 7);
                return (float(BayerMatrix[idx]) + 0.5) / 64.0;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 p = floor(uv * _PixelSize);

                int x = (int)p.x;
                int y = (int)p.y;

                float threshold = Bayer8(x, y);

                float prog = saturate(_Progress);
                if (_Invert > 0.5) prog = 1.0 - prog;

                float mask = step(threshold, prog);

                half4 c = _Color;
                c.a *= mask;
                return c;
            }
            ENDHLSL
        }
    }
}