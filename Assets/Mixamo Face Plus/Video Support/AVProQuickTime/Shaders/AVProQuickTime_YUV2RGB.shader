Shader "Hidden/AVProQuickTime/CompositeYUV_2_RGB" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_TextureWidth ("Texure Width", Float) = 256.0
	}
	SubShader 
	{
		Pass
		{ 
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
		
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers flash xbox360 ps3 gles
//#pragma fragmentoption ARB_precision_hint_fastest 
#pragma fragmentoption ARB_precision_hint_nicest
#pragma multi_compile SWAP_RED_BLUE_ON SWAP_RED_BLUE_OFF
#include "UnityCG.cginc"
#include "AVProQuickTime_Shared.cginc"

uniform sampler2D _MainTex;
float _TextureWidth;
float4 _MainTex_ST;
float4 _MainTex_TexelSize;

struct v2f {
	float4 pos : POSITION;
	float4 uv : TEXCOORD0;
};

v2f vert( appdata_img v )
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
	
	// On D3D when AA is used, the main texture & scene depth texture
	// will come out in different vertical orientations.
	// So flip sampling of the texture when that is the case (main texture
	// texel size will have negative Y).
	#if SHADER_API_D3D9
	if (_MainTex_TexelSize.y < 0)
	{
		o.uv.y = 1-o.uv.y;
	}
	#endif
	
	o.uv.z = v.vertex.x * _TextureWidth * 0.5;

	return o;
}

float4 frag (v2f i) : COLOR
{
	float4 uv = i.uv;
	
	float4 col = tex2D(_MainTex, uv.xy);
#if defined(SWAP_RED_BLUE_ON)
	col = col.bgra;
#endif

	//yvyu
	float y = col.z;
	float u = col.w;
	float v = col.y;
	
	if (frac(uv.z) > 0.5 )
	{
		// ODD PIXELS
		y = col.x;
	}
	
	float4 oCol = convertYUV(y, u, v);
	
	return oCol;
} 
ENDCG
		}
	}
	
	FallBack Off
}