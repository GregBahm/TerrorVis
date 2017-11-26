Shader "Unlit/HeatmapShader"
{
	Properties
	{
        _MapTexture("Map Texture", 2D) = "white"{}
        _HeightScaleA("Height Scale A", Float) = 1
        _HeightScaleB("Height Scale B", Float) = 1
        _HeatColorRamp("Heat Color Ramp", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100
        
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
            #define TextureResolution 256

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
                float heightAdjust : TEXCOORD4;
			};
            
            float _HeightScaleA;
            float _HeightScaleB;
            float _HeatColorRamp;
            
            Buffer<int> _ForeignKillsBuffer;
            Buffer<int> _DomesticKilsBuffer;
			
            uint UvsToIndex(float2 uv)
            {
                uint yPart = (uint)(uv.y * TextureResolution) * TextureResolution;
                uint xPart = uv.x * TextureResolution; 
                return xPart + yPart;
            }

			v2f vert (appdata v) 
			{
				v2f o;
                v.uv.y = 1 - v.uv.y;

                uint bufferIndex = UvsToIndex(v.uv);
                
                int foreignKills = _ForeignKillsBuffer[bufferIndex];
                int domesticKills = _DomesticKilsBuffer[bufferIndex];
                o.heat = domesticKills + foreignKills;
                o.foreignAttackDeath = foreignKills;
                o.domesticAttackDeath = domesticKills;

                float heightAdjust = (domesticKills + foreignKills) / _HeightScaleA;
                if(heightAdjust > 0)
                {
                    heightAdjust = pow(heightAdjust, 1 / _HeightScaleB);
                }
                
                o.heightAdjust = heightAdjust;
                v.vertex.z += heightAdjust;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
            fixed3 GetColor(fixed3 mainColor, fixed3 highColor, float heatVal)
            {
                fixed3 bottomColor = 0;
                float highLerp = heatVal / 10000;

                fixed3 col = mainColor;
                fixed3 ret = lerp(col, highColor, saturate(highLerp));
                return saturate(ret);
            }

			fixed4 frag (v2f i) : SV_Target
			{
                
                fixed3 mainForeignColor = fixed3(1, 0, 0);
                fixed3 highForeignColor = fixed3(1, 1, 0);
                fixed3 mainDomesticColor = fixed3(0, .5, 1);
                fixed3 highDomesticColor = fixed3(0, 1, 1);
                fixed3 mainMidColor = .3;
                fixed3 highMidColor = .9;
                
                float ratio = i.domesticAttackDeath / (i.foreignAttackDeath + i.domesticAttackDeath);

                fixed3 mainColor = lerp(mainForeignColor, mainDomesticColor, ratio);
                mainColor = lerp(mainMidColor, mainColor, abs(ratio - .5) * 2);

                fixed3 highColor = lerp(highForeignColor, highDomesticColor, ratio);
                highColor = lerp(highMidColor, highColor, abs(ratio - .5) * 2);

                fixed3 col = GetColor(mainColor, highColor, i.heat);

				return fixed4(col, saturate(i.heightAdjust * 100));
			}
			ENDCG
		}
	}
}
