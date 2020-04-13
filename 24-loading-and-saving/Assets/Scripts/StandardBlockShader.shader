Shader "Minecraft/Blocks" {

	Properties {
		_MainTex ("Block Texture Atlas", 2D) = "white" {}
		_Specular("Specular", Color) = (1,1,1,1)
		_Gloss("Gloss", Range(8,256)) = 8
	}

	SubShader {
		
		Tags {"LightMode"="ForwardBase" }
		LOD 100
		//Lighting Off
		//, 
		//

		Pass {
		
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma target 2.0

				#include "UnityCG.cginc"
				#include "Lighting.cginc"
				#include "AutoLight.cginc"

				struct appdata {
				
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float4 color : COLOR;
					float3 normal : NORMAL;

				};

				struct v2f {
				
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 color : COLOR;

					float3 worldNormal : TEXCOORD1;
					float4 worldPos: TEXCOORD2;
					SHADOW_COORDS(3)
					//LIGHTING_COORDS(4,6)
				};

				sampler2D _MainTex;

				fixed4 _Specular;
				float _Gloss;

				float GlobalLightLevel;
				float minGlobalLightLevel;
				float maxGlobalLightLevel;

				v2f vert (appdata v) {
				
					//v2f o;
					//
					//o.vertex = UnityObjectToClipPos(v.vertex);
					//o.uv = v.uv;
					//o.color = v.color;
					//TRANSFER_SHADOW(o);
					//return o;

					v2f o;

					if(v.color.r==1){
						o.pos = UnityObjectToClipPos(v.vertex += sin(v.uv.y+_Time.x*24)/10);
					} else {
						o.pos = UnityObjectToClipPos(v.vertex);
					}

					o.uv = v.uv;							
					o.color = v.color;
					o.worldNormal = UnityObjectToWorldNormal(v.normal);
					o.worldPos = mul(unity_ObjectToWorld, v.vertex);
					//o.pos.x += );
					//o.pos.z += cos();

					TRANSFER_SHADOW(o);
					return o;
				}

				float4 frag (v2f i) : SV_Target {
					float2 mainCoord = i.uv/(1/16.0);
					// I am fucking a genius!
					mainCoord.x = 1/256 + (1/16.0)*(int)mainCoord.x;
					mainCoord.y = 2/256 + (1/16.0)*(int)mainCoord.y;
					fixed4 MainColor = tex2D(_MainTex, mainCoord);

					fixed4 albedo = tex2D(_MainTex, i.uv);

					clip(albedo.a - 1);

					float dist = distance(_WorldSpaceCameraPos,i.worldPos)/(16*3);

					//albedo = lerp(albedo, MainColor, clamp(dist, 0.02, 0.8));

					float shade = (maxGlobalLightLevel - minGlobalLightLevel) * GlobalLightLevel + minGlobalLightLevel;
					shade *= i.color.a;
					shade = clamp (1 - shade, minGlobalLightLevel, maxGlobalLightLevel);
					
					fixed4 col = lerp(albedo, float4(0, 0, 0, 1), shade);
					
					//col = float4(1, 0, 0, 1);
					//
					//return col;// float4(1, 0, 0, 1);

					fixed4 ambient = albedo;// * UNITY_LIGHTMODEL_AMBIENT;

					float3 worldLight = normalize(UnityWorldSpaceLightDir(i.worldPos.xyz));
					float3 worldView = normalize(UnityWorldSpaceViewDir(i.worldPos.xyz));

					//  float4(.8,.8,.8,1)
					fixed4 diff = albedo * _LightColor0 * max(0, dot(i.worldNormal, worldLight));

					float3 halfDir = normalize(worldView + worldLight);
					fixed4 spec = albedo * _Specular * pow(max(0, dot(halfDir, i.worldNormal)), _Gloss);

					half atten = SHADOW_ATTENUATION(i);

					//fixed4 col1 = ambient + (diff + spec) * atten;
					fixed4 col1 = ambient*col + col* (diff + 0) * atten;
					//return col1/2+col/2;
					return  col;
					//return  ambient + (diff + spec) * atten;

				}

				ENDCG

		}

	}

	Fallback "VertexLit"
}