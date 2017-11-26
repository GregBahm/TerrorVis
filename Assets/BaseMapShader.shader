Shader "Unlit/MapShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Depth ("Depth", Range(0, 1)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

        Cull Off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			
			#include "UnityCG.cginc"
            #define SliceCount 8

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

            struct v2g
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

			struct g2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
                float dist : TEXCOORD1;
			};

			sampler2D _MainTex;
            float _Depth;
			
			v2g vert (appdata v)
			{
				v2g o;
				o.vertex = v.vertex;
				o.uv = v.uv;
				return o;
			}

            void ApplyToTristream(v2g p[3], inout TriangleStream<g2f> triStream, float dist, float offset)
            {
				    g2f o;
                    o.dist = dist;
                    o.uv = p[0].uv;
                    o.vertex = UnityObjectToClipPos(p[0].vertex + float4(0, 0, offset, 0));
				    triStream.Append(o);
                
                    o.uv = p[1].uv;
                    o.vertex = UnityObjectToClipPos(p[1].vertex + float4(0, 0, offset, 0));
				    triStream.Append(o);
                
                    o.uv = p[2].uv;
                    o.vertex = UnityObjectToClipPos(p[2].vertex + float4(0, 0, offset, 0));
				    triStream.Append(o);
            }

            [maxvertexcount(3 * SliceCount)]
            void geo(triangle v2g p[3], inout TriangleStream<g2f> triStream)
            {
                ApplyToTristream(p, triStream, 0.6, 0);
                for(int i = 1; i < SliceCount; i++)
                {
                    float dist = (float)i / SliceCount;
                    float offset = i * _Depth;
                    ApplyToTristream(p, triStream, dist, offset);
                }
            }
			
			fixed4 frag (g2f i) : SV_Target
			{
				fixed4 mapVal = tex2D(_MainTex, i.uv);
                clip(mapVal.a - .5);
                float darkVal = lerp(0, 1, pow(i.dist, .5));
                fixed ret = lerp(darkVal, 1, mapVal.x);
				return ret;
			}
			ENDCG
		}
	}
}
