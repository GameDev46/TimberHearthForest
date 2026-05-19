Shader "Unlit/VolumetricCloudsShadow"
{
    Properties
    {
        _CloudNoiseTex ("Cloud Noise Texture", 3D) = "white" {}
        _BlueNoiseTex ("Blue Noise Texture", 2D) = "white" {}

        _ErosionStrength ("Erosion Strength", Float) = 0.5
        _BlueNoiseStrength ("Blue Noise Strength", Range(0, 1)) = 0.3
        _BlueNoiseScale ("Blue Noise Scale", Float) = 1.0

        _ShadowDarkness ("Shadow Darkness", Range(0,1)) = 0.5
        _ShadowFalloff ("Shadow Falloff", Float) = 1.0
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.1

        _OuterRadius ("Outer Radius", Float) = 80
        _InnerRadius ("Inner Radius", Float) = 10

        _CloudScale ("Cloud Scale", Float) = 0.62
        _DensityMultiplier ("Density Multiplier", Float) = 1.16
        _DensityThreshold ("Density Threshold", Float) = 0.76

        _TopBottomFadeFactor ("Top/Bottom Fade Factor", Range(0,1)) = 0.5
        _WhispyFactor ("Whispy Factor", Float) = 20.0

        _SunDirection ("Sun Direction", Vector) = (0,1,0,0)

        _Offset ("Offset", Vector) = (0,0,0,0)
        _Center ("Center", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags{ "RenderType"="Transparent" "Queue"="Transparent-400" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ZTest Always

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
                float4 position : SV_POSITION;
				float4 screenPos : TEXCOORD0;
				float3 ray : TEXCOORD1;
            };

            sampler2D_float _CameraDepthTexture;

            UNITY_DECLARE_TEX3D(_CloudNoiseTex);
            sampler2D _BlueNoiseTex;

            float _ErosionStrength;
            float _BlueNoiseStrength;
            float _BlueNoiseScale;

            float _ShadowDarkness;
            float _ShadowFalloff;
            float _ShadowThreshold;

            float _OuterRadius;
            float _InnerRadius;

            float _CloudScale;
            float _DensityMultiplier;
            float _DensityThreshold;

            float _TopBottomFadeFactor;
            float _WhispyFactor;

            float3 _SunDirection;

            float3 _Offset;
            float3 _Center;

            struct Ray {
                float3 origin;
                float3 dir;
            };

            struct HitInfo {
                bool didHit;
                float entryDist;
                float exitDist;
                float3 entryPoint;
                float3 exitPoint;
                float3 entryNormal;
                float3 exitNormal;
            };

            v2f vert(appdata v)
            {
                v2f o;
				// Convert the vertex positions from object space to clip space so they can be rendered correctly
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.position = UnityWorldToClipPos(worldPos);

				// Calculate the ray between the camera to the vertex
				o.ray = worldPos - _WorldSpaceCameraPos;
				// Calculate the screen position
				o.screenPos = ComputeScreenPos (o.position);

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

            HitInfo RaySphere(Ray ray, float3 sphereCenter, float sphereRadius)
            {
                HitInfo hitInfo;
                hitInfo.didHit = false;

                fixed3 oc = ray.origin - sphereCenter;

                float a = dot(ray.dir, ray.dir);
                float b = 2.0 * dot(oc, ray.dir);
                float c = dot(oc, oc) - sphereRadius * sphereRadius;

                float discriminant = b * b - 4 * a * c;

                if (discriminant < 0.0) return hitInfo;

                float sqrtD = sqrt(discriminant);

                float t0 = (-b - sqrtD) / (2.0 * a);
                float t1 = (-b + sqrtD) / (2.0 * a);

                // Ensure that t0 occurs before t1
                if (t0 > t1)
                {
                    float tmp = t0;
                    t0 = t1;
                    t1 = tmp;
                }

                // If both t0 and t1 are behind the camera then no intersection with the cloud sphere
                if (t1 < 0.0) return hitInfo;

                // If inside sphere then the "entry" is the camera
                hitInfo.entryDist = max(t0, 0.0);
                hitInfo.exitDist  = t1;

                hitInfo.entryPoint = ray.origin + hitInfo.entryDist * ray.dir;
                hitInfo.exitPoint  = ray.origin + hitInfo.exitDist * ray.dir;

                hitInfo.entryNormal = normalize(hitInfo.entryPoint - sphereCenter);
                hitInfo.exitNormal = normalize(hitInfo.exitPoint - sphereCenter);

                hitInfo.didHit = true;
                return hitInfo;
            }

            // [0, 1] blue noise
            float SampleBlueNoise(float2 screenUV)
            {
                // Blue noise implementation from https://blog.maximeheckel.com/posts/real-time-cloudscapes-with-volumetric-raymarching/
                // Blue noise texture from https://github.com/Calinou/free-blue-noise-textures/blob/master/128_128/HDR_LA_0.png

                float2 noiseUV = floor(screenUV * _ScreenParams.xy) / 128.0;
                return tex2Dlod(_BlueNoiseTex, float4(noiseUV, 0, 0)).r;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 screenUv = i.screenPos.xy / i.screenPos.w;

                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUv);
                // Skip pixels which have no object to recieve shadow
                if (depth > 1.0) discard;

                depth = Linear01Depth(depth) * _ProjectionParams.z;

                float3 worldRay = normalize(i.ray);
                worldRay /= dot(worldRay, -UNITY_MATRIX_V[2].xyz);
                float3 groundWorldPos = _WorldSpaceCameraPos + worldRay * depth;

                Ray lightRay;
                lightRay.origin = groundWorldPos;
                lightRay.dir = normalize(_SunDirection);

                float biasedCloudRadius = (_InnerRadius * 0.8 + _OuterRadius * 0.2);
                HitInfo hit = RaySphere(lightRay, _Center, biasedCloudRadius);

                // Sun ray doesn't hit cloud sphere, so skip
                if (!hit.didHit) discard;

                float3 cloudSamplePoint = lightRay.origin + lightRay.dir * hit.exitDist;

                float density = GetDensity(cloudSamplePoint);
                if (density < _ShadowThreshold) discard;
                
                float shadowAlpha = smoothstep(_ShadowThreshold, 1.0, density * _ShadowFalloff) * _ShadowDarkness;

                // Output shadow color blended smoothly by density alpha
                fixed4 finalShadow = float4(0.0, 0.0, 0.0, 1.0);
                finalShadow.a *= shadowAlpha;

                return finalShadow;
            }
            ENDCG
        }
    }
    //Fallback "Legacy Shaders/VertexLit"
}
