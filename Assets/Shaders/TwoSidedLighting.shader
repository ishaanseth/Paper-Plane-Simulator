Shader "Custom/TwoSidedLighting" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)  // Allows color to be changed from Unity inspector
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Cull Off  // Disable culling to render both sides
        LOD 200

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Input and output structures for vertex and fragment shaders
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
            };

            // Vertex shader - handles normal transformation and position
            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);  // Transform vertex to clip space

                // Transform normal to world space
                o.normal = mul((float3x3)unity_ObjectToWorld, v.normal);

                return o;
            }

            // Fragment shader - applies two-sided lighting
            fixed4 _Color;

            fixed4 frag (v2f i) : SV_Target {
                // Check if the normal is pointing towards the camera
                if (dot(i.normal, normalize(i.normal)) < 0.0) {
                    // Invert normal for the back side
                    i.normal = -i.normal;
                }

                // Simple Lambert lighting based on world normal direction and light direction
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float lambert = saturate(dot(i.normal, lightDir));

                // Apply light to the color
                return _Color * lambert;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
