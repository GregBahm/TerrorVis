Shader "Unlit/HeatmapShader"
{
	Properties
	{
        _MapTexture("Map Texture", 2D) = "white"{}
        _HeightScale("Height Scale", Range(0, 0.001)) = 0.0001
        _HeatColorRamp("Heat Color Ramp", Float) = 1
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
			
			v2f vert (appdata v)
			{
				v2f o;
                v.uv.y = 1 - v.uv.y;

                float heat = tex2Dlod(_MainTex, float4(v.uv, 0, 0)).x;
                o.heat = heat;
                
				o.uv = v.uv.yx;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed mapVal = tex2D(_MapTexture, i.uv).b;
				return mapVal;
			}
			ENDCG
		}
		Pass
		{
            //Blend DstColor Zero
			Blend SrcAlpha OneMinusSrcAlpha
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
				float4 vertex : SV_POSITION;
                float heat : TEXCOORD1;
                float domesticAttackDeath : TEXCOORD2;
                float foreignAttackDeath : TEXCOORD3;
			};

			sampler2D _MainTex;
            float _HeightScale;
            float _HeatColorRamp;
			
			v2f vert (appdata v)
			{
				v2f o;
                v.uv.y = 1 - v.uv.y;

                float2 heat = tex2Dlod(_MainTex, float4(v.uv, 0, 0)).xy;
                o.heat = heat.x + heat.y;
                o.foreignAttackDeath = heat.x;
                o.domesticAttackDeath = heat.y;

                v.vertex.z += o.heat * _HeightScale;
                
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
            fixed3 GetColor(fixed3 mainColor, fixed3 highColor, float heatVal)
            {
                fixed3 bottomColor = fixed3(0, 0, 0);
                float lerper = heatVal / 10;
                float highLerp = heatVal / 200;

                fixed3 col = lerp(bottomColor, mainColor, saturate(pow(lerper, _HeatColorRamp)));
                fixed3 ret = lerp(col, highColor, saturate(highLerp));
                return saturate(ret);
            }

			fixed4 frag (v2f i) : SV_Target
			{
                
                fixed3 mainForeignColor = fixed3(1, 0, 0);
                fixed3 highForeignColor = fixed3(1, 1, 0);
                fixed3 mainDomesticColor = fixed3(0, .5, 1);
                fixed3 highDomesticColor = fixed3(0, 1, 1);
                
                float ratio = i.domesticAttackDeath / (i.foreignAttackDeath + i.domesticAttackDeath);

                fixed3 mainColor = lerp(mainForeignColor, mainDomesticColor, ratio);
                mainColor = lerp(fixed3(.5, .5, .5), mainColor, abs(ratio - .5) * 2);

                fixed3 highColor = lerp(highForeignColor, highDomesticColor, ratio);
                highColor = lerp(fixed3(.5, .5, .5), highColor, abs(ratio - .5) * 2);

                fixed3 col = GetColor(mainColor, highColor, i.heat);

				return fixed4(col, saturate(i.heat / 2));
			}
			ENDCG
		}
	}
}
