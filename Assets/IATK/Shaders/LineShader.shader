﻿Shader "IATK/LinesShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	 _Size("Size", Range(0, 30)) = 0.5
		_MinSize("_MinSize",Float) = 0
		_MaxSize("_MaxSize",Float) = 0
		_MinX("_MinX",Range(0, 1)) = 0
		_MaxX("_MaxX",Range(0, 1)) = 1.0
		_MinY("_MinY",Range(0, 1)) = 0
		_MaxY("_MaxY",Range(0, 1)) = 1.0
		_MinZ("_MinZ",Range(0, 1)) = 0
		_MaxZ("_MaxZ",Range(0, 1)) = 1.0
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
		Tags{ "RenderType" = "Transparent" }
		//Blend func : Blend Off : turns alpha blending off
		//#ifdef(VISUAL_ACCUMULATION)
		//Blend SrcAlpha One
		//#else
		Blend[_MySrcMode][_MyDstMode]
		//#endif

		//AlphaTest Greater .01
		Cull Off
		//ZWrite On
		Lighting On
		//	Zwrite On
		LOD 200

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma geometry geom
		// make fog work
#pragma multi_compile_fog

#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
		float4 color : COLOR;
		float3 normal : NORMAL;
		float4 uv_MainTex : TEXCOORD0; // index, vertex size, filtered, prev size
	};

	struct v2g
	{
		float4 vertex : SV_POSITION;
		float4 color : COLOR;
		float3 normal : NORMAL;
		float  isBrushed : FLOAT;
	};

	struct g2f
	{
		float4 vertex : SV_POSITION;
		float4 color : COLOR;
		float2 tex0	: TEXCOORD0;
		float  isBrushed : FLOAT;
	};

	float _Size;
	float _MinSize;
	float _MaxSize;

	sampler2D _BrushedTexture;

	float showBrush;
	float4 brushColor;

	//Sampler2D _MainTexSampler;

	//SamplerState sampler_MainTex;

	float _DataWidth;
	float _DataHeight;

	//*******************
	// RANGE FILTERING
	//*******************

	float _MinX;
	float _MaxX;
	float _MinY;
	float _MaxY;
	float _MinZ;
	float _MaxZ;


	// Time
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

	float _MinNormX;
	float _MaxNormX;
	float _MinNormY;
	float _MaxNormY;
	float _MinNormZ;
	float _MaxNormZ;
				

	float4 RotateAroundYInDegrees(float3 vertex, float degrees)
	{
		float4 _vertex = float4(vertex, 1);
		float alpha = degrees * UNITY_PI / 180.0;
		float sina, cosa;
		sincos(alpha, sina, cosa);
		float2x2 m = float2x2(cosa, -sina, sina, cosa);
		return float4(mul(m, _vertex.xz), _vertex.yw).xzyw;
	}

	float3 RotateAroundZInDegrees(float3 vertex, float degrees)
	{
		float alpha = degrees * UNITY_PI / 180.0;
		float sina, cosa;
		sincos(alpha, sina, cosa);
		float2x2 m = float2x2(cosa, -sina, sina, cosa);
		//return float3(mul(m, vertex.xz), vertex.y).xzy;
		return float3(mul(m, vertex.xy), vertex.z).zxy;
	}

	v2g vert(appdata v)
	{
		v2g o;

		float idx = v.uv_MainTex.x;
		float isFiltered = v.uv_MainTex.z;

		//lookup the texture to see if the vertex is brushed...
		float2 indexUV = float2((idx % _DataWidth) / _DataWidth, ((idx / _DataWidth) / _DataHeight));
		float4 brushValue = tex2Dlod(_BrushedTexture, float4(indexUV, 0.0, 0.0));

		o.isBrushed = brushValue.r;
				
		float size = lerp(v.uv_MainTex.w, v.uv_MainTex.y, _TweenSize);
		
		float3 posT = v.vertex - float3(0, 1, 0);
		float3 pos = RotateAroundZInDegrees(v.vertex, _Tween * 90);// lerp(v.normal, v.vertex, _Tween);
		pos = pos + float3(0, 1, 0);

		float4 normalisedPosition = float4(
			normaliseValue(pos.x, _MinNormX, _MaxNormX, 0, 1),
			normaliseValue(pos.y, _MinNormY, _MaxNormY, 0, 1),
			normaliseValue(pos.z, _MinNormZ, _MaxNormZ, 0, 1), 1.0);
	
		float4 vert = UnityObjectToClipPos(normalisedPosition);

		o.vertex = vert;
		o.normal = float3(idx, size, isFiltered);
		o.color = v.color;
		//precision filtering
		float epsilon = -0.000001;

		//filtering
		/*if (normalisedPosition.x < (_MinX + epsilon) ||
			normalisedPosition.x >(_MaxX - epsilon) ||
			normalisedPosition.y < (_MinY + epsilon) ||
			normalisedPosition.y >(_MaxY - epsilon) ||
			normalisedPosition.z < (_MinZ + epsilon) ||
			normalisedPosition.z >(_MaxZ - epsilon) || isFiltered)
		{
			o.color.w = 0;
		}*/

		return o;
	}

	[maxvertexcount(6)]
	void geom(line v2g points[2], inout TriangleStream<g2f> triStream)
	{
		//handle brushing line topoolgy
		if (points[0].color.w == 0) points[1].color.w = 0;
		if (points[1].color.w == 0) points[0].color.w = 0;

		//line geometry
		float4 p0 = points[0].vertex;
		float4 p1 = points[1].vertex;

		float w0 = p0.w;
		float w1 = p1.w;

		p0.xyz /= p0.w;
		p1.xyz /= p1.w;

		float3 line01 = p1 - p0;
		float3 dir = normalize(line01);

		// scale to correct window aspect ratio
		float3 ratio = float3(1024, 768, 0);
		ratio = normalize(ratio);

		float3 unit_z = normalize(float3(0, 0, -1));

		float3 normal = normalize(cross(unit_z, dir) * ratio);

		float width = _Size * normaliseValue(points[0].normal.y, 0.0, 1.0, _MinSize, _MaxSize);

		g2f v[4];

		float3 dir_offset = dir * ratio * width;
		float3 normal_scaled = normal * ratio * width;

		float3 p0_ex = p0 - dir_offset;
		float3 p1_ex = p1 + dir_offset;

		v[0].vertex = float4(p0_ex - normal_scaled, 1) * w0;
		v[0].tex0 = float2(1,0);
		v[0].color = points[0].color;
		v[0].isBrushed = points[0].isBrushed;// || points[1].isBrushed;

		v[1].vertex = float4(p0_ex + normal_scaled, 1) * w0;
		v[1].tex0 = float2(0,0);
		v[1].color = points[0].color;
		v[1].isBrushed = points[0].isBrushed;// || points[1].isBrushed;

		v[2].vertex = float4(p1_ex + normal_scaled, 1) * w1;
		v[2].tex0 = float2(1,1);
		v[2].color = points[1].color;
		v[2].isBrushed = points[0].isBrushed;// || points[1].isBrushed;

		v[3].vertex = float4(p1_ex - normal_scaled, 1) * w1;
		v[3].tex0 = float2(0,1);
		v[3].color = points[1].color;
		v[3].isBrushed = points[0].isBrushed;// || points[1].isBrushed;

		triStream.Append(v[2]);
		triStream.Append(v[1]);
		triStream.Append(v[0]);

		triStream.RestartStrip();

		triStream.Append(v[3]);
		triStream.Append(v[2]);
		triStream.Append(v[0]);

		triStream.RestartStrip();

	}

	fixed4 frag(g2f i) : SV_Target
	{
	fixed4 col = i.color;
	
	if (i.isBrushed && showBrush>0.0)
	{
		col = brushColor;
	}
	
	if (col.w == 0) { discard; return float4(0.0,0.0,0.0,0.0); }
	return  col;
	}

	/*
	<<<<<<< HEAD
			} 
			
			fixed4 frag (g2f i) : SV_Target
			{
				fixed4 col = i.color;
				
				if (i.isBrushed && showBrush>0.0)
				{ col = brushColor;
				}
				if(col.w == 0) {discard; return float4(0.0,0.0,0.0,0.0);}
				return  col;
			}
			ENDCG
		}
=======
	*/
		ENDCG
	}
	}
}