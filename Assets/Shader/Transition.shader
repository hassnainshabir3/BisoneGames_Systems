Shader "bison/UI/Transition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Fill Color", Color) = (0.176, 0.290, 0.459, 1)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _Softness ("Edge Softness (px)", Range(1, 30)) = 2
        _Outline ("Outline Width (px, 0 = off)", Range(0, 30)) = 0
        _Progress ("Progress", Range(0, 1)) = 0
        _Style ("Style", Float) = 0
        _Seed ("Seed", Float) = 0
        _Reveal ("Reveal", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            fixed4 _OutlineColor;
            float _Softness, _Outline, _Progress, _Style, _Seed, _Reveal;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hash21 (float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise (float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbm (float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    v += a * vnoise(p);
                    p = p * 2.03 + 17.1;
                    a *= 0.5;
                }
                return v;
            }

            // Har pixel ko 0..1 ka number: kam number pehle dhakta hai.
            float Field (float2 uv)
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 p = float2(uv.x * aspect, uv.y);
                float2 c = float2(0.5 * aspect, 0.5);
                float radial = length(p - c) / length(c);
                float f;

                if (_Style < 0.5) f = radial;
                else if (_Style < 1.5) f = 0.7 * radial + 0.3 * (fbm(p * 3.0 + _Seed) / 0.94);
                else if (_Style < 2.5) f = 0.3 * ((uv.x + uv.y) * 0.5) + 0.7 * (fbm(p * 2.2 + _Seed) / 0.94);
                else if (_Style < 3.5) f = 0.85 * uv.x + 0.15 * (0.5 + 0.5 * sin(uv.y * 9.4248 + _Seed * 6.2832));
                else if (_Style < 4.5) f = (uv.x + uv.y) * 0.5;
                else
                {
                    float2 l = frac(p * 7.0) - 0.5;
                    float d = saturate(length(l) * 1.4142);
                    f = 0.75 * d + 0.25 * uv.x;
                }
                return saturate(f);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float f  = Field(i.uv);
                float fw = max(fwidth(f), 0.00001);
                float t  = _Progress * 1.08;
                float raw = (_Reveal > 0.5) ? (f - t + 0.08) : (t - f);
                float d = raw / fw;

                float fill = smoothstep(0.0, max(_Softness, 1.0), d);

                float ring = 0.0;
                if (_Outline > 0.0)
                {
                    float w = _Outline;
                    ring = saturate(smoothstep(-2.0 * w - 1.0, -2.0 * w, d) - smoothstep(-w - 1.0, -w, d));
                    ring *= smoothstep(0.0, 0.04, _Progress) * (1.0 - smoothstep(0.92, 1.0, _Progress));
                }

                float a = fill * _Color.a;
                float rw = ring * (1.0 - a);
                float outA = a + rw;
                float3 rgb = (_Color.rgb * a + _OutlineColor.rgb * rw) / max(outA, 0.0001);
                return fixed4(rgb, outA);
            }
            ENDCG
        }
    }
}
