Shader "Unlit/VolumetricCloudsShadow"
{
    Properties
    {
        _CloudNoiseTex ("Cloud Noise Texture", 3D) = "white" {}
        _BlueNoiseTex ("Blue Noise Texture", 2D) = "white" {}

        _ErosionStrength ("Erosion Strength", Float) = 0.5
        _BlueNoiseStrength ("Blue Noise Strength", Range(0, 1)) = 0.3
        _BlueNoiseScale ("Blue Noise Scale", Float) = 1.0

        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.1

        _OuterRadius ("Outer Radius", Float) = 80
        _InnerRadius ("Inner Radius", Float) = 10

        _CloudScale ("Cloud Scale", Float) = 0.62
        _DensityMultiplier ("Density Multiplier", Float) = 1.16
        _DensityThreshold ("Density Threshold", Float) = 0.76

        _TopBottomFadeFactor ("Top/Bottom Fade Factor", Range(0,1)) = 0.5
        _WhispyFactor ("Whispy Factor", Float) = 20.0

        _Offset ("Offset", Vector) = (0,0,0,0)
        _Center ("Center", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(1.0, 1.0, 1.0, 1.0);
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float4 screenPos : TEXCOORD2;
                float3 worldPos : TEXCOORD1;
            };

            UNITY_DECLARE_TEX3D(_CloudNoiseTex);
            sampler2D _BlueNoiseTex;

            float _ErosionStrength;
            float _BlueNoiseStrength;
            float _BlueNoiseScale;

            float _ShadowThreshold;

            float _OuterRadius;
            float _InnerRadius;

            float _CloudScale;
            float _DensityMultiplier;
            float _DensityThreshold;

            float _TopBottomFadeFactor;
            float _WhispyFactor;

            float3 _Offset;
            float3 _Center;

            v2f vert(appdata v)
            {
                v2f o;

                float4 vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(vertex);

                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float2 Noise(float3 p)
            {
                // Sample the 3D noise texture (significantly faster then generating noise on the go)
                float3 uvw = p / _OuterRadius;
                uvw = frac(uvw);

                float4 noise = UNITY_SAMPLE_TEX3D_LOD(_CloudNoiseTex, uvw, 0);

                float base   = noise.r;
                float edge   = noise.g;
                float detail = noise.b;
                float warp   = noise.a;

                float density = base;

                // Erode edges only
                density -= detail * edge * _ErosionStrength;

                density = saturate(density);
                return float2(density, detail);
            }

            float GetDensity(float3 worldPos)
            {
                // Sample noise
                float scale = _CloudScale;
                float2 noiseData = Noise((worldPos - _Center) * scale + _Offset);

                float density = noiseData.x;
                float detail = noiseData.y;

                density = smoothstep(_DensityThreshold, 1.0, density * _DensityMultiplier);

                // Fade the noise towards the edges of the inner and out sphere
                float height = distance(worldPos, _Center);
                float h = (height - _InnerRadius) / (_OuterRadius - _InnerRadius);

                // Add extra "whispy" detail towards the top edges of the cloud
                height += detail * _WhispyFactor * h;

                // Recalculate h after adding whispy detail
                h = (height - _InnerRadius) / (_OuterRadius - _InnerRadius);

                // Fade the clouds towards the edge of inner and outer boundary
                float fadeFactor = smoothstep(0.0, _TopBottomFadeFactor, h) * smoothstep(1.0, 1-_TopBottomFadeFactor, h);
                density *= fadeFactor;

                return density;
            }

            // 0..1 blue noise
            float SampleBlueNoise(float2 screenUV)
            {
                // Blue noise implementation from https://blog.maximeheckel.com/posts/real-time-cloudscapes-with-volumetric-raymarching/
                // Blue noise texture from https://github.com/Calinou/free-blue-noise-textures/blob/master/128_128/HDR_LA_0.png

                float2 noiseUV = floor(screenUV * _ScreenParams.xy) / 128.0;
                return tex2Dlod(_BlueNoiseTex, float4(noiseUV, 0, 0)).r;
            }

            float4 frag(v2f i) : SV_Target
            {
                //float2 uv = i.screenPos.xy / i.screenPos.w;

                // Calculate the spherical UV coordinates
                // From https://gamedev.stackexchange.com/questions/114412/how-to-get-uv-coordinates-for-sphere-cylindrical-projection
                float3 n = normalize(i.worldPos - _Center);
                float u = atan2(n.x, n.z) / (2 * UNITY_PI) + 0.5;
                float v = n.y * 0.5 + 0.5;
                float2 uv = float2(u, v);

                float blueNoise = SampleBlueNoise(uv * _BlueNoiseScale) * _BlueNoiseStrength;

                float density = GetDensity(i.worldPos);
                if (density < _ShadowThreshold + blueNoise) discard;

                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    //Fallback "Legacy Shaders/VertexLit"
}
