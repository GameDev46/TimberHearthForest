Shader "Unlit/VolumetricClouds"
{
    Properties
    {
        _CloudNoiseTex ("Cloud Noise Texture", 3D) = "white" {}
        _ErosionStrength ("Erosion Strength", Float) = 0.5

        _OuterRadius ("Outer Radius", Float) = 80
        _InnerRadius ("Inner Radius", Float) = 10

        _NumSteps ("Number of Steps", Int) = 80
        _NumSunSteps ("Number of Sun Steps", Int) = 16
        _MinStepSize ("Minimum Step Size", Float) = 0.1

        _CloudScale ("Cloud Scale", Float) = 0.62
        _DensityMultiplier ("Density Multiplier", Float) = 1.16
        _DensityThreshold ("Density Threshold", Float) = 0.76

        _LightAbsorptionThroughCloud ("Light Absorption Through Cloud", Float) = 0.8
        _LightAbsorptionTowardsSun ("Light Absorption Towards Sun", Float) = 0.4
        _DarknessThreshold ("Darkness Threshold", Float) = 0.2
        _PhaseG ("Phase G", Float) = 0.5
        _PhaseIntensity ("Phase Intensity", Float) = 6
        _ForwardScatteringBias ("Forward Scattering Bias", Float) = 0.2

        _SunDirection ("Sun Direction", Vector) = (0,1,0,0)
        _SunColor ("Sun Color", Color) = (1,1,1,1)

        _AmbientTexture ("Ambient Texture", 2D) = "white" {}
        _AmbientStrength ("Ambient Strength", Float) = 1
        _AmbientMixFactor ("Ambient Mix Factor", Float) = 0.5

        _PlanetShadowStrength ("Planet Shadow Strength", Float) = 1
        _PlanetShadowSharpness ("Planet Shadow Sharpness", Float) = 1

        _Offset ("Offset", Vector) = (0,0,0,0)
        _Center ("Center", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Front
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
                float4 screenPos : TEXCOORD0;
                float3 viewVector : TEXCOORD1;
            };

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            UNITY_DECLARE_TEX3D(_CloudNoiseTex);
            float _ErosionStrength;

            float _OuterRadius;
            float _InnerRadius;

            int _NumSteps;
            int _NumSunSteps;
            float _MinStepSize;

            float _CloudScale;
            float _DensityMultiplier;
            float _DensityThreshold;

            float _LightAbsorptionThroughCloud;
            float _LightAbsorptionTowardsSun;
            float _DarknessThreshold;
            float _PhaseG;
            float _PhaseIntensity;
            float _ForwardScatteringBias;

            float3 _SunDirection;
            float4 _SunColor;

            sampler2D _AmbientTexture;
            float _AmbientStrength;
            float _AmbientMixFactor;

            float _PlanetShadowStrength;
            float _PlanetShadowSharpness;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);

                // Reconstruct camera's view ray
                float2 uv = o.screenPos.xy / o.screenPos.w;
                float4 clip = float4(uv * 2 - 1, 0, -1);

                float3 viewVec = mul(unity_CameraInvProjection, clip).xyz;
                o.viewVector = mul(unity_CameraToWorld, float4(viewVec, 0)).xyz;

                return o;
            }

            /*float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float worley(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);

                float minDist = 1.0;

                for (int x=-1; x<=1; x++)
                for (int y=-1; y<=1; y++)
                for (int z=-1; z<=1; z++)
                {
                    float3 cell = i + float3(x,y,z);
                    float3 rand = frac(sin(dot(cell, float3(12.9898,78.233,37.719))) * 43758.5453);

                    float3 diff = rand + float3(x,y,z) - f;
                    minDist = min(minDist, dot(diff,diff));
                }

                return 1.0 - minDist;
            }*/

            float noise(float3 p)
            {
                /*float3 i = floor(p);
                float3 f = frac(p);

                // 8 corners of cube
                float n000 = hash(i + float3(0,0,0));
                float n100 = hash(i + float3(1,0,0));
                float n010 = hash(i + float3(0,1,0));
                float n110 = hash(i + float3(1,1,0));
                float n001 = hash(i + float3(0,0,1));
                float n101 = hash(i + float3(1,0,1));
                float n011 = hash(i + float3(0,1,1));
                float n111 = hash(i + float3(1,1,1));

                float3 u = f * f * (3.0 - 2.0 * f);

                float nx00 = lerp(n000, n100, u.x);
                float nx10 = lerp(n010, n110, u.x);
                float nx01 = lerp(n001, n101, u.x);
                float nx11 = lerp(n011, n111, u.x);

                float nxy0 = lerp(nx00, nx10, u.y);
                float nxy1 = lerp(nx01, nx11, u.y);

                return worley(p) + lerp(nxy0, nxy1, u.z);*/

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
                return density;
            }

            float GetDensity(float3 worldPos)
            {
                // Sample noise
                float scale = _CloudScale;
                float n = noise((worldPos - _Center) * scale + _Offset);
                n = smoothstep(_DensityThreshold, 1.0, n * _DensityMultiplier);

                // Fade the noise towards the edges of the inner and out sphere
                float height = distance(worldPos, _Center);
                float h = (height - _InnerRadius) / (_OuterRadius - _InnerRadius);

                n *= smoothstep(0.0, 0.2, h) * smoothstep(1.0, 0.8, h);

                return n;
            }

            HitInfo RaySphere(Ray ray, fixed3 sphereCenter, float sphereRadius)
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

            // Inspired by Sebastian Lague's implementation: https://www.youtube.com/watch?v=4QOcCGI6xOU&t=4s
            // which is also avaliable on GitHub: https://github.com/SebLague/Clouds/blob/master/Assets/Scripts/Clouds/Shaders/Clouds.shader
            float MarchLight(float3 origin)
            {
                Ray sunRay;
                sunRay.origin = origin;
                sunRay.dir = normalize(_SunDirection);

                // Calculate the intersection point with the out sphere
                HitInfo hit = RaySphere(sunRay, _Center, _OuterRadius);
                if (!hit.didHit) return 1.0;

                // Calulate the sampling step size
                float stepSize = hit.exitDist / _NumSunSteps;
                stepSize = max(_MinStepSize, stepSize);

                float3 position = origin;
                float totalDensity = 0.0;

                // Step through the cloud towards the sun to calculate the total cloud density
                for (int i = 0; i < _NumSunSteps; i++)
                {
                    position += sunRay.dir * stepSize;
                    totalDensity += max(0.0, GetDensity(position) * stepSize);
                }

                // Calculate the light recieved to the point in the cloud
                float transmitance = exp(-totalDensity * _LightAbsorptionTowardsSun);
                return _DarknessThreshold + transmitance * (1.0 - _DarknessThreshold);
            }

            float3 GetAmbience(float3 toPointDir, float3 sunDir)
            {
                // In the ambience texture the x axis represents the time of day with middle as day and right as night (left is unused)
                // The y axis represents the y height where top is facing away from light, middle is perpendicular to light and bottom is facing towards the light
                // https://github.com/ow-mods/outer-wilds-unity-wiki/wiki/Effects-%E2%80%90-Ambient-Light#texture
                
                float3 equatorDir = normalize(float3(toPointDir.x, 0.0, toPointDir.z));

                // AIM: 1 at night and 0.5 at day
                float u = dot(equatorDir, sunDir) * 0.5 + 0.5; // 0 at night and 1 at day
                u = 1.0 - u * 0.5; // 1 at night and 0.5 at day

                // AIM: 0 when facing away and 1 when at equator
                float v = 1.0 - max(dot(toPointDir, equatorDir), 0.0); // 0 when facing away and 1 when at equator

                // Sample the ambience texture
                float4 ambience = tex2Dlod(_AmbientTexture, float4(u, v, 0, 0));

                return ambience.rgb;
            }

            float PhaseHG(float cosTheta, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / (4.0 * UNITY_PI * pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate the ray from the camera
                Ray ray;
                ray.origin = _WorldSpaceCameraPos;

                float viewLength = length(i.viewVector);
                ray.dir = i.viewVector / viewLength;

                // Get the inner and outer cloud sphere intersections
                HitInfo outerHit = RaySphere(ray, _Center, _OuterRadius);
                if (!outerHit.didHit) discard;

                HitInfo innerHit = RaySphere(ray, _Center, _InnerRadius);

                // Sample the camera's depth texture
                float2 uv = i.screenPos.xy / i.screenPos.w;

                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                float linearDepth = LinearEyeDepth(rawDepth);
                
                // Convert the camera's depth to a ray distance for easier comparison
                float rayDepth = linearDepth * viewLength;

                // Calculate the maximum ray distance
                float maxDistance = min(rayDepth, outerHit.exitDist);

                // Calculate the phase value
                float3 L = normalize(_SunDirection);
                float cosTheta = dot(ray.dir, L);

                float forward = PhaseHG(cosTheta, _PhaseG);
                float backward = PhaseHG(cosTheta, -0.2);

                float phaseVal = lerp(backward, forward, _ForwardScatteringBias);
                phaseVal *= _PhaseIntensity;

                // Raymarch through the clouds
                float t = outerHit.entryDist;
                float transmittance = 1.0;
                float lightEnergy = 0.0;

                float3 ambientColour = float3(0.0, 0.0, 0.0);

                float stepSize = (outerHit.exitDist - outerHit.entryDist) / _NumSteps;
                stepSize = max(_MinStepSize, stepSize);

                for (int step = 0; step < _NumSteps; step++)
                {
                    // Break if we go past the maximum distance
                    if (t > maxDistance) break;

                    // If we are inside the inner sphere then the density is 0 so skip
                    bool insideInner = innerHit.didHit && (t > innerHit.entryDist && t < innerHit.exitDist);

                    if (!insideInner) {
                        float3 worldPos = ray.origin + ray.dir * t;
                        float density = GetDensity(worldPos);

                        if (density > 0.0) {
                            float3 normSunDir = normalize(_SunDirection);
                            float3 toPoint = normalize(worldPos - _Center);

                            float3 ambience = GetAmbience(toPoint, normSunDir);
                            ambientColour += ambience * stepSize * density;

                            float lightTransmittance = MarchLight(worldPos);

                            float sunDot = dot(toPoint, normSunDir);

                            float shadow = saturate(-sunDot);
                            shadow = pow(shadow, _PlanetShadowSharpness);
                            lightTransmittance *= (1.0 - shadow * _PlanetShadowStrength);

                            float absorption = density * _LightAbsorptionThroughCloud * stepSize;
                            float contribution = density * transmittance * lightTransmittance * phaseVal * stepSize;

                            lightEnergy += contribution;
                            transmittance *= exp(-absorption);

                            if (transmittance < 0.01) break;
                        }
                    }

                    // Step forward (jitter prevents banding caused by Ambient Light, remove is no longer necessary)
                    float jitter = frac(sin(dot(i.screenPos.xy, float2(12.9898,78.233))) * 43758.5453);
                    t += stepSize + jitter * 0.2;
                }

                float3 col = _SunColor.rgb * lightEnergy;
                float alpha = 1.0 - transmittance;

                float3 ambience = ambientColour * _AmbientStrength * (1.0 - transmittance);
                col = lerp(col, ambience, _AmbientMixFactor);

                // Prevent oversaturation
                col = clamp(col, 0, 1);

                return float4(col, alpha);
            }
            ENDCG
        }
    }

    //Fallback "Legacy Shaders/VertexLit"
}
