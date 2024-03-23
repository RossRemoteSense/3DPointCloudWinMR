// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "IATK/OutlineDots" 
{
	Properties 
	{
		_MainTex("Texture Array", 2DArray) = "White" {}
		_BrushedTexture("Base (RGB)", 2D) = "White" {}
		_Size("Size", Range(0, 1)) = 0.0						// Ross Brown - increased size to 1.0
		_MinSize("Min Size", Range(0, 1)) = 1.0
		_MaxSize("Max Size", Range(0, 1)) = 1.0
		_BrushSize("BrushSize",Float) = 0.05
		_MinX("_MinX",Range(0, 1)) = 0
		_MaxX("_MaxX",Range(0, 1)) = 1.0
		_MinY("_MinY",Range(0, 1)) = 0
		_MaxY("_MaxY",Range(0, 1)) = 1.0
		_MinZ("_MinZ",Range(0, 1)) = 0
		_MaxZ("_MaxZ",Range(0, 1)) = 1.0
		_data_size("data_size",Float) = 0
		_tl("Top Left", Vector) = (-1,1,0,0)
		_tr("Top Right", Vector) = (1,1,0,0)
		_bl("Bottom Left", Vector) = (-1,-1,0,0)
		_br("Bottom Right", Vector) = (1,-1,0,0)
		_MinNormX("_MinNormX",Range(0, 1)) = 0.0
		_MaxNormX("_MaxNormX",Range(0, 1)) = 1.0
		_MinNormY("_MinNormY",Range(0, 1)) = 0.0
		_MaxNormY("_MaxNormY",Range(0, 1)) = 1.0
		_MinNormZ("_MinNormZ",Range(0, 1)) = 0.0
		_MaxNormZ("_MaxNormZ",Range(0, 1)) = 1.0
		_MySrcMode("_SrcMode", Float) = 5
		_MyDstMode("_DstMode", Float) = 10
		_Tween("_Tween", Range(0, 1)) = 1
		_TweenSize("_TweenSize", Range(0, 1)) = 1


	}
    
	SubShader 
	{
		Pass
		{
			Name "Onscreen geometry"
			Tags { "Queue"="Transparent" "RenderType"="Transparent" }
			Zwrite On
			ZTest LEqual
			Blend [_MySrcMode][_MyDstMode]
			Cull Off 
			Lighting Off 
			Offset -1, -1 // This line is added to default Unlit/Transparent shader
			LOD 200

			CGPROGRAM
				#pragma target 5.0
				#pragma vertex VS_Main
				#pragma fragment FS_Main
				#pragma geometry GS_Main
				#include "UnityCG.cginc" 
				#include "Distort.cginc"

				// **************************************************************
				// Data structures												*
				// **************************************************************
			
		        struct VS_INPUT {
          		    float4 position : POSITION;
            		float4 color: COLOR;
					float3 normal: NORMAL;
					float4 uv_MainTex : TEXCOORD0; // index, vertex size, filtered, prev size
					float4 uv_Maintex2 : TEXCOORD1; // Index of the points coming over
					float4 tangent : TANGENT;
        		};
				
				struct GS_INPUT
				{
					float4	pos : POSITION;
					float3	normal : NORMAL;
					float2  tex0 : TEXCOORD0;
					float4  color : COLOR;
					float	isBrushed : FLOAT;
				};

				struct FS_INPUT
				{
					float4	pos : POSITION;
					float2  tex0 : TEXCOORD0;
					float4  color : COLOR;
					float	isBrushed : FLOAT;
					float3	normal : NORMAL;
				};

				struct FS_OUTPUT
				{
					float4 color : COLOR;
					float depth : SV_Depth;
				};

				// **************************************************************
				// Vars															*
				// **************************************************************

				float _Size;
				float _MinSize;
				float _MaxSize;

				float _BrushSize;
				
				//*******************
				// RANGE FILTERING
				//*******************

				float _MinX;
				float _MaxX;
				float _MinY;
				float _MaxY;
				float _MinZ;
				float _MaxZ;

				// ********************
				// Normalisation ranges
				// ********************

				float _MinNormX;
				float _MaxNormX;
				float _MinNormY;
				float _MaxNormY;
				float _MinNormZ;
				float _MaxNormZ;

				sampler2D _BrushedTexture;
				UNITY_DECLARE_TEX2DARRAY(_MainTex);

				float _DataWidth;
				float _DataHeight;
				float showBrush;
				float4 brushColor;
				
				float _Tween;
				float _TweenSize;

				//*********************************
				// helper functions
				//*********************************

				float normaliseValue(float value, float i0, float i1, float j0, float j1)
				{
				float L = (j0 - j1) / (i0 - i1);
				return (j0 - (L * i0) + (L * value));
				}

				float3 bezier(float3 p0, float3 p1, float3 p2, float3 p3, float t)
				{
					return p0 *  (-pow(t,3) + 3*pow(t,2) - 3*t + 1) +
						p1 * (3*pow(t, 3) - 6*pow(t, 2) + 3*t) +
						p2 * (-3*pow(t, 3) + 3* pow(t, 2)) +
						p3 * (pow(t, 3));
				}

				float rand(float2 co, float min, float max)
				{
					float random = frac(sin(dot(co.xy, float2(12.9898,78.233))) * 43758.5453);
					return min + (max - min) * random;
				}

				/*float3 RotateAroundYInDegrees(float3 vertex, float degrees)
				{
					float4 _vertex = float4(vertex, 1);
					float alpha = degrees * UNITY_PI / 180.0;
					float sina, cosa;
					sincos(alpha, sina, cosa);
					float2x2 m = float2x2(cosa, -sina, sina, cosa);
					return float3(mul(m, _vertex.xz), _vertex.yw).xzy;
				}*/

				// **************************************************************
				// Shader Programs												*
				// **************************************************************

				// Vertex Shader ------------------------------------------------
				GS_INPUT VS_Main(VS_INPUT v)
				{
					GS_INPUT output = (GS_INPUT)0;
					
					float idx = v.uv_MainTex.x;
					float isFiltered = v.uv_MainTex.z;

					//lookup the texture to see if the vertex is brushed...
					float2 indexUV = float2((idx % _DataWidth) / _DataWidth, ((idx / _DataWidth) / _DataHeight));
					float4 brushValue = tex2Dlod(_BrushedTexture, float4(indexUV.x,indexUV.y, 0.0, 0.0));
					//brushColor = float4(1.0, 0.0, 0.0, 1.0);
					/*if (brushValue.g<2) brushColor = float4(1.0, 0.0, 0.0, 1.0);
					if (brushValue.g <= 3 && brushValue.g >=2) brushColor = float4(0.0, 1.0, 0.0, 1.0);
					if (brushValue.g <= 4 && brushValue.g >= 3) brushColor = float4(0.0, 0.0, 1.0, 1.0);*/

					output.isBrushed = brushValue.r;




					float size = lerp(v.uv_MainTex.w, v.uv_MainTex.y, _TweenSize);
					//float3 pos = lerp(v.normal, v.position, _Tween);
					float3 pos = lerp(v.position, v.normal, _Tween);

					
//						normaliseValue(_Tween*v.position.x*5,0, v.position.x*5, 0,1));
					
					float4 normalisedPosition = float4(
					normaliseValue(pos.x,_MinNormX, _MaxNormX, 0,1),
					normaliseValue(pos.y,_MinNormY, _MaxNormY, 0,1),
					normaliseValue(pos.z,_MinNormZ, _MaxNormZ, 0,1), v.position.w);

					output.pos = normalisedPosition;
					output.normal = float3(idx, size, brushValue.a);
					output.tex0 = float2(0, 0);
					output.color = v.color;
				
					//precision filtering
					float epsilon = -0.00001; 

					//filtering
					if(
					 normalisedPosition.x < (_MinX + epsilon) ||
					 normalisedPosition.x > (_MaxX - epsilon) || 
					 normalisedPosition.y < (_MinY + epsilon) || 
					 normalisedPosition.y > (_MaxY - epsilon) || 
					 normalisedPosition.z < (_MinZ + epsilon) || 
					 normalisedPosition.z > (_MaxZ - epsilon) || isFiltered
					 )
					{
						output.color.w = 0;
					}

					return output;
				}



				// Geometry Shader -----------------------------------------------------
				[maxvertexcount(6)]
				void GS_Main(point GS_INPUT p[1], inout TriangleStream<FS_INPUT> triStream)
				{
					float4x4 MV = UNITY_MATRIX_MV;
					float4x4 vp = UNITY_MATRIX_VP;

					float3 up = UNITY_MATRIX_IT_MV[1].xyz;
					float3 right =  -UNITY_MATRIX_IT_MV[0].xyz;

					float dist = 1;
					float sizeFactor = normaliseValue(p[0].normal.y, 0.0, 1.0, _MinSize, _MaxSize);
										
					float halfS = 0.05f * (_Size + (dist * sizeFactor));
							
					float4 v[4];				

					v[0] = float4(p[0].pos + halfS * right - halfS * up, 1.0f);
					v[1] = float4(p[0].pos + halfS * right + halfS * up, 1.0f);
					v[2] = float4(p[0].pos - halfS * right - halfS * up, 1.0f);
					v[3] = float4(p[0].pos - halfS * right + halfS * up, 1.0f);
		
					FS_INPUT pIn;
					
					pIn.isBrushed = p[0].isBrushed;
					pIn.color = p[0].color;
					pIn.normal = p[0].normal;

					pIn.pos = UnityObjectToClipPos(v[0]);					
					pIn.tex0 = float2(0.0f, 0.0f);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					pIn.pos = UnityObjectToClipPos(v[1]);
					pIn.tex0 = float2(0.0f, 1.0f);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					pIn.pos = UnityObjectToClipPos(v[2]);
					pIn.tex0 = float2(1.0f, 0.0f);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);

					pIn.pos = UnityObjectToClipPos(v[3]);
					pIn.tex0 = float2(1.0f, 1.0f);
					pIn.normal = p[0].normal;
					triStream.Append(pIn);
					
				}

				// Fragment Shader -----------------------------------------------
				FS_OUTPUT FS_Main(FS_INPUT input)
				{
					FS_OUTPUT o;
					float4 _brushColor;
					//brushColor = float4(1.0, 0.0, 0.0, 1.0);
					/*if (input.isBrushed <= 0.3) _brushColor = float4(1.0, 0.0, 0.0, 1.0);
					if (input.isBrushed <= 0.5 && input.isBrushed >=0.3) _brushColor = float4(0.0, 1.0, 0.0, 1.0);
					if (input.isBrushed <= 1 && input.isBrushed >= 0.5) _brushColor = float4(0.0, 0.0, 1.0, 1.0);
					*/

					//float brushValue = 0.4;

					if (input.normal.z>0 && input.normal.z <= 0.3) {
						_brushColor = float4(1.0, 1.0, 1.0, 1.0);
					}
					else if (input.normal.z > 0.3 && input.normal.z <= 0.5) {
						_brushColor = float4(1.0, 0.0, 0.0, 1.0);					// Ross Brown: RED highlights
					}
					else if (input.normal.z > 0.5 && input.normal.z <= 1) {
						_brushColor = float4(0.0, 1.0, 0.0, 1.0);
					}

					float3 uvWithIndex = float3(input.tex0, input.normal.x); // Use the indice contained in the normal vector x value.

					if(input.isBrushed >0 && showBrush > 0.0)
						o.color = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvWithIndex) * _brushColor;
						//o.color = tex2D(_MainTex, input.tex0.xy) *_brushColor;					
					else
					{
						o.color = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvWithIndex);
						//o.color = tex2D(_MainTex, input.tex0.xy) * float4(1.0, 0.0, 0.0, 1.0); //input.color; // Ross Brown
					}

					o.depth = o.color.a > 0.5 ? input.pos.z : 0;
					return o;
				}


			ENDCG
		} // end pass
	} 
}