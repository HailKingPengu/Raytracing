Shader "Unlit/RayShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            };

            // www.pcg-random.org
            float RandomF(inout uint state)
            {
                state = state * 747796405 + 2891336453;
                uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
                result = (result >> 22) ^ result;
                return result / 4294967295.0;
            }

            float RandomFNormalDis(inout uint state)
            {
                float theta = 2 * 3.1415926 * RandomF(state);
                float rho = sqrt(-2 * log(RandomF(state)));
                return rho * cos(theta);
            }

            float3 RandomDirection(inout uint state)
            {
                float x = RandomFNormalDis(state);
                float y = RandomFNormalDis(state);
                float z = RandomFNormalDis(state);
                return normalize(float3(x, y, z));
            }

            float3 RandomHemisphereDirection(float3 normal, inout uint state)
            {
                float3 dir = RandomDirection(state);
                return dir * sign(dot(normal, dir));
            }

            float2 RandomPointInCircle(inout uint rngState)
            {
                float angle = RandomF(rngState) * 6.2830;
                float2 pointOnCircle = float2(cos(angle), sin(angle));
                return pointOnCircle * sqrt(RandomF(rngState));
            }

            float3 CameraParameters;
            float4x4 CamWorldMatrix;

            struct Ray 
            {
                float3 origin;
                float3 dir;

                float3 at(float t)
                {
                    return origin + t * dir;
                }
            };

            struct RayMaterial
            {
                float4 colour;
                float4 emissionColour;
                float4 specularColour;
                float emissionStrength;
                float smoothness;
                float specular;
            };

            struct HitInfo
            {
                bool hit;
                float dist;
                float3 hitPoint;
                float3 normal;

                RayMaterial material;
            };

            struct Sphere
            {
                float3 center;
                float radius;
                RayMaterial material;
            };

            float3 SkyColourHorizon;
            float3 SkyColourZenith;

            float3 GetEnvironmentLight(Ray r)
            {
                float skyGradientT = pow(smoothstep(0, 0.4, r.dir.y), 0.35);
                return lerp(SkyColourHorizon, SkyColourZenith, skyGradientT);
            };

            HitInfo hit_sphere(float3 center, float radius, Ray r) {

                HitInfo hitInfo = (HitInfo)0;

                float3 oc = center - r.origin;
                float a = dot(r.dir, r.dir);
                float b = -2.0 * dot(r.dir, oc);
                float c = dot(oc, oc) - radius*radius;
                float discriminant = b*b - 4*a*c;
                
                if (discriminant >= 0) 
                {
                    float dist = (-b - sqrt(discriminant) ) / (2.0 * a);
                    
                    if(dist >= 0)
                    {
                        hitInfo.hit = true;
                        hitInfo.dist = dist;
                        hitInfo.hitPoint = r.origin + r.dir * dist;
                        hitInfo.normal = normalize(hitInfo.hitPoint - center);
                    }
                } 

                return hitInfo;
            };

            StructuredBuffer<Sphere> Spheres;
            int NumSpheres;

            HitInfo RayHit(Ray r){
                HitInfo closestHit = (HitInfo)0;
                //first hit should be closer than infinity!
                closestHit.dist = 1.#INF;

                for(int i = 0; i < NumSpheres; i++){
                    Sphere sphere = Spheres[i];
                    HitInfo hitInfo = hit_sphere(sphere.center, sphere.radius, r);

                    if(hitInfo.hit && hitInfo.dist < closestHit.dist)
                    {
                        closestHit = hitInfo;
                        closestHit.material = sphere.material;
                    }
                }

                return closestHit;
            };

            int MaxBounceCount;
            int NumRaysPerPixel;
            int CurrentFrame;
            float BlurStrength;

            bool RenderImage;

            float3 CamRight;
            float3 CamUp;

            float3 Trace(Ray r, inout uint rngState)
            {
                float3 incomingLight = 0;
                float3 rayColour = 1;

                for (int i = 0; i <= MaxBounceCount; i++)
                {
                    HitInfo hitInfo = RayHit(r);
                    if(hitInfo.hit)
                    {
                        RayMaterial material = hitInfo.material;

                        r.origin = hitInfo.hitPoint;
                        float3 diffuseDir = normalize(hitInfo.normal + RandomDirection(rngState));
                        float3 specularDir = reflect(r.dir, hitInfo.normal);
                        bool isSpecularBounce = material.specular >= RandomF(rngState);
                        r.dir = lerp(diffuseDir, specularDir, material.smoothness * isSpecularBounce);

                        float3 emittedLight = material.emissionColour * material.emissionStrength;
                        incomingLight += emittedLight * rayColour;
                        rayColour *= lerp(material.colour, material.specularColour, isSpecularBounce);
                    }
                    else
                    {
                        incomingLight += GetEnvironmentLight(r) * rayColour;
                        break;
                    }
                }

                return incomingLight;
            }

            bool IsRendering;
			sampler2D PrevFrame;
			int RenderedFrames;

            // float3 ray_color(Ray r, inout uint state) {

            //     // float t = hit_sphere(float3(0,0,-1), 0.5, r).dist;
            //     // if (t > 0.0) {
            //     //     float3 N = normalize(r.at(t) - float3(0,0,-1));
            //     //     return 0.5 * float3(N.x + 1, N.y + 1, N.z + 1);
            //     // }

            //     HitInfo hitInfo = RayHit(r);

            //     //nevermind

            //     // if(hitInfo.hit)
            //     // {
            //     //     float3 direction = RandomHemisphereDirection(hitInfo.normal, state);

            //     //     Ray nextRay;
            //     //     nextRay.origin = hitInfo.hitPoint;
            //     //     nextRay.dir = direction;

            //     //     return 0.5 * ray_color(nextRay, state);
            //     // }

            //     float3 unit_direction = normalize(r.dir);
            //     float a = 0.5*(unit_direction.y + 1.0);
            //     return (1.0-a)*float3(1.0, 1.0, 1.0) + a*float3(0.5, 0.7, 1.0);
            // }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewPointLocal = float3(i.uv - 0.5, 1) * CameraParameters;
                float3 viewPoint = mul(CamWorldMatrix, float4(viewPointLocal, 1));

                //seed generation 
                uint2 numPixels = _ScreenParams.xy;
                uint2 pixelCoord = i.uv * numPixels;
                uint pixelIndex = pixelCoord.y * numPixels.x + pixelCoord.x;
                uint rngState = pixelIndex + CurrentFrame * 2032400;

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint - ray.origin);

                float3 colour;
                for (int rayIndex = 0; rayIndex < NumRaysPerPixel; rayIndex++)
                {
                    float2 blurOffset = RandomPointInCircle(rngState) * BlurStrength / numPixels.x;
                    ray.origin += CamRight * blurOffset.x + CamUp * blurOffset.y;


                    colour += Trace(ray, rngState);
                }


                if(IsRendering)
                {
                    //rendertextures appear to be flipped(?)
                    float4 prevColour = tex2D(PrevFrame, float2(i.uv.x, 1 - i.uv.y));

                    //return float4(i.uv.x, i.uv.y, 0, 0);

                    float weight = 1.0 / (RenderedFrames + 1);
                    return float4(saturate(prevColour * (1 - weight) + (colour / NumRaysPerPixel) * weight), 0);
                    //return prevColour;
                }

                return float4(colour / NumRaysPerPixel, 1);
            }
            ENDCG
        }
    }
}
