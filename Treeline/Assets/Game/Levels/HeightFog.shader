// 바닥에 깔리는 안개. 씬 하나 안에서만 도는 전체 화면 패스다.
//
// URP 의 기본 안개는 거리만 본다. 눈밭에 낮게 깔린 층은 표현되지 않아서
// 멀리 있는 것이 균일하게 흐려질 뿐 「깔려 있다」는 느낌이 안 난다.
//
// IMPORTANT: 렌더러 기능(Renderer Feature)으로 붙이지 않는다. URP 렌더러는
// 여섯 장이 함께 쓰므로 거기 넣으면 남의 장까지 안개가 낀다. 대신 카메라
// 자식의 화면 채우는 삼각형 하나로 그린다 — 씬에 놓인 것만 영향받는다.
//
// 큐가 Transparent-1 인 것은 의도다. 불투명한 것(산 · 나무 · 실루엣)에는
// 안개가 끼고, 그 뒤에 그려지는 눈발 파티클은 또렷하게 남는다. 코앞의
// 눈송이가 먼 산 깊이로 잘못 흐려지는 것을 이 순서 하나로 막는다.
Shader "Treeline/HeightFog"
{
    Properties
    {
        [Header(Color)]
        _FogColorLow ("안개 색 - 바닥", Color) = (0.34, 0.40, 0.47, 1)
        _FogColorHigh ("안개 색 - 위", Color) = (0.10, 0.14, 0.20, 1)
        _ColorHeight ("두 색이 갈리는 높이(m)", Float) = 16

        [Header(Moon scatter)]
        _MoonColor ("달빛이 번지는 색", Color) = (0.62, 0.72, 0.92, 1)
        _MoonDir ("달이 있는 쪽(xyz)", Vector) = (0.65, 0.24, 0.72, 0)
        _MoonGlow ("번지는 세기", Range(0, 2)) = 0.5
        _MoonSharpness ("번지는 범위(클수록 좁다)", Range(1, 64)) = 10

        [Header(Density)]
        _FogBase ("안개 바닥 높이(world Y)", Float) = -1.2
        _FogFalloff ("높이에 따른 감쇠", Float) = 0.26
        _Density ("바닥에서의 밀도", Float) = 0.075
        _DistanceDensity ("거리에 따른 밀도", Float) = 0.0035
        _MaxDistance ("잘라내는 거리(m)", Float) = 210
        _MaxFog ("최대 농도", Range(0, 1)) = 0.94

        [Header(Drift)]
        _NoiseScale ("얼룩 크기(작을수록 크다)", Float) = 0.03
        _NoiseStrength ("얼룩 세기", Range(0, 1)) = 0.45
        _NoiseSpeed ("흐르는 속도(xz, m/s)", Vector) = (0.9, 0.25, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-1"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HeightFog"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColorLow;
                float4 _FogColorHigh;
                float  _ColorHeight;
                float4 _MoonColor;
                float4 _MoonDir;
                float  _MoonGlow;
                float  _MoonSharpness;
                float  _FogBase;
                float  _FogFalloff;
                float  _Density;
                float  _DistanceDensity;
                float  _MaxDistance;
                float  _MaxFog;
                float  _NoiseScale;
                float  _NoiseStrength;
                float4 _NoiseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos  : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 층 하나면 격자가 보인다. 셋을 겹치면 크고 작은 덩어리가 섞인다.
            float Fbm(float2 p)
            {
                return ValueNoise(p) * 0.6
                     + ValueNoise(p * 2.17 + 13.7) * 0.3
                     + ValueNoise(p * 4.31 + 31.3) * 0.1;
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                // 트랜스폼을 거치지 않는다. 정점이 이미 클립 공간의 [-1, 3] 이라
                // 어떤 화면비에서도 화면을 꽉 덮는 삼각형 하나가 된다.
                OUT.positionCS = float4(IN.positionOS.xy, UNITY_NEAR_CLIP_VALUE, 1.0);
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.screenPos.xy / IN.screenPos.w;

                float rawDepth = SampleSceneDepth(uv);

                // 하늘에는 깊이가 없다. 뒤집힌 Z 인지에 따라 「빈 값」이 반대다 —
                // 데스크톱은 0, WebGL(GLES) 은 1 이다. 하나로 보면 한쪽이 뒤집힌다.
                #if UNITY_REVERSED_Z
                    bool isSky = rawDepth < 1e-6;
                #else
                    bool isSky = rawDepth > 1.0 - 1e-6;
                #endif

                float3 worldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 camPos = _WorldSpaceCameraPos;
                float3 toSurface = worldPos - camPos;
                float dist = length(toSurface);
                float3 rayDir = dist > 1e-4 ? toSurface / dist : float3(0.0, 0.0, 1.0);

                // 하늘은 far plane 이라 거리가 터진다. 자르지 않으면 농도가 1 로
                // 붙어 산 능선이 통째로 사라진다.
                dist = isSky ? _MaxDistance : min(dist, _MaxDistance);

                // 밀도가 높이에 따라 exp 로 줄어들 때, 시선을 따라 적분한 값.
                // 층을 잘게 나눠 더하지 않아도 한 번에 나온다.
                float k = max(_FogFalloff, 1e-4);
                float baseDensity = _Density * exp(-k * clamp(camPos.y - _FogBase, -20.0, 200.0));
                float kd = k * rayDir.y;

                float tau;
                if (abs(kd) > 1e-4)
                {
                    // 아래를 볼 때 kd 가 음수라 지수가 발산한다. 어차피 농도는
                    // 1 에서 멎으므로 지수를 30 에서 자른다.
                    tau = baseDensity * (1.0 - exp(min(-kd * dist, 30.0))) / kd;
                }
                else
                {
                    tau = baseDensity * dist;
                }
                tau = clamp(tau, 0.0, 50.0);

                tau += _DistanceDensity * dist;

                // 얼룩은 시선이 안개층을 지나는 지점에서 뜬다. 표면 자리에서 바로
                // 뜨면 먼 산에 무늬가 찍혀 벽지처럼 보인다.
                float sampleDist = min(dist, 70.0);
                float2 driftUV = (camPos.xz + rayDir.xz * sampleDist + _Time.y * _NoiseSpeed.xy) * _NoiseScale;
                float patch = Fbm(driftUV);
                tau *= lerp(1.0 - _NoiseStrength * 0.6, 1.0 + _NoiseStrength * 0.6, patch);

                float fog = 1.0 - exp(-tau);

                float heightBlend = saturate((worldPos.y - _FogBase) / max(_ColorHeight, 0.01));
                float3 color = lerp(_FogColorLow.rgb, _FogColorHigh.rgb, heightBlend);

                // 안개는 광원 쪽을 볼 때 밝아진다. 달을 향한 시선에만 얹는다.
                float3 moon = normalize(_MoonDir.xyz);
                float facing = saturate(dot(rayDir, moon));
                color += _MoonColor.rgb * (pow(facing, _MoonSharpness) * _MoonGlow * fog);

                return half4(color, saturate(fog) * _MaxFog);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
