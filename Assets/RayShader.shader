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

            struct HitInfo
            {
                bool hit;
                float dist;
                float3 hitPoint;
                float3 normal;
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
                        hitInfo.normal = normalize(hitInfo.hitPoint = center);
                    }
                } 
                return hitInfo;
            }

            float3 ray_color(Ray r) {

                float t = hit_sphere(float3(0,0,-1), 0.5, r).dist;
                if (t > 0.0) {
                    float3 N = normalize(r.at(t) - float3(0,0,-1));
                    return 0.5 * float3(N.x + 1, N.y + 1, N.z + 1);
                }

                float3 unit_direction = normalize(r.dir);
                float a = 0.5*(unit_direction.y + 1.0);
                return (1.0-a)*float3(1.0, 1.0, 1.0) + a*float3(0.5, 0.7, 1.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewPointLocal = float3(i.uv - 0.5, 1) * CameraParameters;
                float3 viewPoint = mul(CamWorldMatrix, float4(viewPointLocal, 1));

                Ray ray;
                ray.origin = _WorldSpaceCameraPos;
                ray.dir = normalize(viewPoint - ray.origin);

                return float4(ray_color(ray), 0);
            }
            ENDCG
        }
    }
}
