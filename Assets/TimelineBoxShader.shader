Shader "Unlit/TimelineBoxShader"
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
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
                float objY : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.objY = v.vertex.y;
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                float shadow = i.objY + .5;
                float2 uvDist = abs(i.uv - .5) * 2;
                float maxDist = 1 - max(uvDist.x, uvDist.y);
                float param = saturate(maxDist * 10);
                return lerp(shadow, .77, param);
			}
			ENDCG
		}
	}
}
