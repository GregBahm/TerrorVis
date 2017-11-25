Shader "Unlit/HeatmapShader"
{
	Properties
	{
        _MapTexture("Map Texture", 2D) = "white"{}
        _HeightScale("Height Scale", Range(0, 0.001)) = 0.0001
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
				float4 vertex : SV_POSITION;
                float heat : TEXCOORD1;
			};

			sampler2D _MainTex;
            sampler2D _MapTexture;
            float4 _MapTexture_ST;
            float _HeightScale;
			
			v2f vert (appdata v)
			{
				v2f o;
                v.uv.y = 1 - v.uv.y;

                float heat = tex2Dlod(_MainTex, float4(v.uv, 0, 0)).x;
                o.heat = heat;
                v.vertex.z += heat * _HeightScale;
                
				o.uv = TRANSFORM_TEX(v.uv.yx, _MapTexture);
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed mapVal = tex2D(_MapTexture, i.uv).b;
                fixed3 col = lerp(mapVal.xxx, float3(1, 0, 0), i.heat / 20);
				return fixed4(col, 1);
			}
			ENDCG
		}
	}
}
