﻿Shader "Custom/Stencil/Diffuse2"
{

Properties
{
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader
{
	Tags { "RenderType"="Opaque" "Queue"="Geometry" }
	LOD 200

	Stencil
	{
		Ref 2
		Comp equal
		Pass keep
	}

	CGPROGRAM
	#pragma surface surf Lambert

	sampler2D _MainTex;
	fixed4 _Color;

	struct Input
	{
		float2 uv_MainTex;
	};

	void surf (Input IN, inout SurfaceOutput o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgba;
		//o.Alpha = c.a;
	}
	
	ENDCG
}

Fallback "VertexLit"
}
