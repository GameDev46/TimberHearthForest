Shader "Unlit/VolumetricCloudsShadow"
{
    Properties
    {
        _CloudNoiseTex ("Cloud Noise Texture", 3D) = "white" {}

        _ErosionStrength ("Erosion Strength", Float) = 0.5

        _OuterRadius ("Outer Radius", Float) = 80
        _InnerRadius ("Inner Radius", Float) = 10
        
        _SunStepSize ("Sun Step Size", Range(1, 100)) = 10

        _CloudScale ("Cloud Scale", Float) = 0.62
        _DensityMultiplier ("Density Multiplier", Float) = 1.16
        _DensityThreshold ("Density Threshold", Float) = 0.76

        _TopBottomFadeFactor ("Top/Bottom Fade Factor", Range(0,1)) = 0.5
        _WhispyFactor ("Whispy Factor", Float) = 20.0
        
        _LightAbsorptionTowardsSun ("Light Absorption Towards Sun", Float) = 0.4

        _SunDirection ("Sun Direction", Vector) = (0,1,0,0)

        _Offset ("Offset", Vector) = (0,0,0,0)
        _Center ("Center", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags{ "RenderType"="Transparent" "Queue"="Transparent-400" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Front
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

            float _ErosionStrength;

            float _OuterRadius;
            float _InnerRadius;
            
            float _SunStepSize;

            float _CloudScale;
            float _DensityMultiplier;
            float _DensityThreshold;

            float _TopBottomFadeFactor;
            float _WhispyFactor;
            
            float _LightAbsorptionTowardsSun;

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
                hitInfo.entryDist = 0.0;
                hitInfo.exitDist = 0.0;

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

                hitInfo.didHit = true;
                return hitInfo;
            }

            float MarchLight(float3 origin)
            {
                Ray sunRay;
                sunRay.origin = origin;
                sunRay.dir = normalize(_SunDirection);

                // Calculate the intersection point with the out sphere
                HitInfo hit = RaySphere(sunRay, _Center, _OuterRadius);
                if (!hit.didHit) return 1.0;

                // Calulate the sampling step size
                int numSteps = (hit.exitDist - hit.entryDist) / _SunStepSize;

                float3 position = origin;
                float totalDensity = 0.0;

                // Step through the cloud towards the sun to calculate the total cloud density
                for (int i = 0; i < numSteps; i++)
                {
                    position += sunRay.dir * _SunStepSize;
                    totalDensity += max(0.0, GetDensity(position) * _SunStepSize);
                }

                // Calculate the light recieved to the point in the cloud
                float transmitance = exp(-totalDensity * _LightAbsorptionTowardsSun);
                return transmitance;
                // return _DarknessThreshold + transmitance * (1.0 - _DarknessThreshold);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 screenUv = i.screenPos.xy / i.screenPos.w;

                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUv);
                // Skip pixels which have no object to recieve shadow
                if (depth == 0.0) discard;

                depth = Linear01Depth(depth) * _ProjectionParams.z;

                float3 worldRay = normalize(i.ray);
                worldRay /= dot(worldRay, -UNITY_MATRIX_V[2].xyz);
                float3 groundWorldPos = _WorldSpaceCameraPos + worldRay * depth;

                float shadowAlpha = 1-MarchLight(groundWorldPos);

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
