

Shader "Custom/CustomTerrainShader" {

	Properties{

	_displayType("display type", Int) = 0
	_numberOfSections("number of sections (max 6)", Int) = 5

	[NoScaleOffset]
	_HeightMap("Height Map", 2D) = "white" {}

	[NoScaleOffset]
	_AlphaMap("Alpha Map", 2D) = "white" {}
	_InvertAlpha("Invert Alpha (0 or 1)", Int) = 0

	//[NoScaleOffset]
	//_RoadsMap("Roads Map", 2D) = "black" {}
	//[NoScaleOffset]
	//_RoadsColor("Roads Color", Color) = (0,0,0,0)
	//[NoScaleOffset]
	//_RoadsTexture("Roads Texture", 2D) = "white" {}
	//[NoScaleOffset]
	//_RoadsNormals("Roads Normals", 2D) = "white" {}
	//[NoScaleOffset]
	//_RoadsSpec("Roads Specular", 2D) = "white" {}
	//_ScaleRoads("Roads Scale", Float) = 1

	[NoScaleOffset]
	_Texture0("Texture0", 2D) = "white" {}
	[NoScaleOffset]
	_Normals0("Normals0", 2D) = "bump" {}
	[NoScaleOffset]
	_Spec0("Specular0", 2D) = "white" {}
	_Color0("Color0", Color) = (0,0,0,0)
	_Threshold0("threshold0", Range(0.0,1.0)) = 0.0
	_Scale0("scale0", Float) = 1
	_MinimumMergeDistance0("Minimum Merge Dist 0", Range(0.0, 0.1)) = 0.05

	[NoScaleOffset]
	_Texture1("Texture1", 2D) = "white" {}
	[NoScaleOffset]
	_Normals1("Normals1", 2D) = "bump" {}
	[NoScaleOffset]
	_Spec1("Specular1", 2D) = "white" {}
	_Color1("Color1", Color) = (0,0,0,0)
	_Threshold1("threshold1", Range(0.0,1.0)) = 0.2
	_Scale1("scale1", Float) = 1
	_MinimumMergeDistance1("Minimum Merge Dist 1", Range(0.0, 0.1)) = 0.05

	[NoScaleOffset]
	_Texture2("Texture2", 2D) = "white" {}
	[NoScaleOffset]
	_Normals2("Normals2", 2D) = "bump" {}
	[NoScaleOffset]
	_Spec2("Specular2", 2D) = "white" {}
	_Color2("Color2", Color) = (0,0,0,0)
	_Threshold2("threshold2", Range(0.0,1.0)) = 0.4
	_Scale2("scale2", Float) = 1
	_MinimumMergeDistance2("Minimum Merge Dist 2", Range(0.0, 0.1)) = 0.05

	[NoScaleOffset]
	_Texture3("Texture3", 2D) = "white" {}
	[NoScaleOffset]
	_Normals3("Normals3", 2D) = "bump" {}
	[NoScaleOffset]
	_Spec3("Specular3", 2D) = "white" {}
	_Color3("Color3", Color) = (0,0,0,0)
	_Threshold3("threshold3", Range(0.0,1.0)) = 0.6
	_Scale3("scale3", Float) = 1
	_MinimumMergeDistance3("Minimum Merge Dist 3", Range(0.0, 0.1)) = 0.05

	[NoScaleOffset]
	_Texture4("Texture4", 2D) = "white" {}
	[NoScaleOffset]
	_Normals4("Normals4", 2D) = "bump" {}
	[NoScaleOffset]
	_Spec4("Specular4", 2D) = "white" {}
	_Color4("Color4", Color) = (0,0,0,0)
	//_Threshold4("threshold4", Range(0.0,1.0)) = 0.8
	_Scale4("scale4", Float) = 1
	//_MinimumMergeDistance4("Minimum Merge Dist 4", Range(0.0, 0.1)) = 0.05

	//[NoScaleOffset]
	//_Texture5("Texture5", 2D) = "white" {}
	//[NoScaleOffset]
	//_Normals5("Normals5", 2D) = "white" {}
	//[NoScaleOffset]
	//_Spec5("Specular5", 2D) = "white" {}
	//_Color5("Color5", Color) = (0,0,0,0)
	//_Scale5("scale5", Float) = 1


	}
		SubShader{
			//Tags{ "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
			//Tags { "RenderType" = "TransparentCutout" /*"Queue" = "Geometry-1" "IgnoreProjector" = "True" */}
			//LOD 200

			Tags{ /*"RenderType" = "TransparentCutout"*/ "Queue" = "Geometry" "IgnoreProjector" = "True" }
			//Blend SrcAlpha OneMinusSrcAlpha
			//ZWrite On
			//Fog { Mode linear }
			LOD 200

		//Stencil{
		//	Ref 0
		//	Comp equal
		//	
		//}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows /*keepalpha*/
		//#pragma surface surf Standard alpha:fade
		//#pragma vertex vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		/*  STRUCTS ---------------------------------------------------------------------------- */
		//struct appdata {
		//	float4 vertex : POSITION;
		//	float3 normal : NORMAL;
		//	float4 texcoord : TEXCOORD0;
		//	float4 tangent : TANGENT;
		//};

		struct Input {
			float2 uv_HeightMap;
		};


		/* VARIABLES ---------------------------------------------------------------------------- */
		uniform sampler2D _HeightMap;
		uniform sampler2D _AlphaMap;
		uniform int _InvertAlpha;
		//uniform sampler2D _RoadsMap;
		//uniform sampler2D _RoadsTexture; uniform sampler2D _RoadsNormals; uniform sampler2D _RoadsSpec;
		uniform sampler2D _Texture0; uniform sampler2D _Normals0; uniform sampler2D _Spec0;
		uniform sampler2D _Texture1; uniform sampler2D _Normals1; uniform sampler2D _Spec1;
		uniform sampler2D _Texture2; uniform sampler2D _Normals2; uniform sampler2D _Spec2;
		uniform sampler2D _Texture3; uniform sampler2D _Normals3; uniform sampler2D _Spec3;
		uniform sampler2D _Texture4; uniform sampler2D _Normals4; uniform sampler2D _Spec4;
		//uniform sampler2D _Texture5; uniform sampler2D _Normals5; uniform sampler2D _Spec5;

		uniform float _Threshold0; uniform float4 _Color0; uniform int _Scale0; uniform float _MinimumMergeDistance0;
		uniform float _Threshold1; uniform float4 _Color1; uniform int _Scale1; uniform float _MinimumMergeDistance1;
		uniform float _Threshold2; uniform float4 _Color2; uniform int _Scale2; uniform float _MinimumMergeDistance2;
		uniform float _Threshold3; uniform float4 _Color3; uniform int _Scale3; uniform float _MinimumMergeDistance3;
		//uniform float _Threshold4; uniform float4 _Color4; uniform int _Scale4; uniform float _MinimumMergeDistance4;
		uniform float4 _Color4; int _Scale4;

		uniform int _displayType;
		uniform int _numberOfSections;

		#define MAX_SECTIONS 5
		float thresholds[MAX_SECTIONS];
		float minimumMergeDists[MAX_SECTIONS - 1];
		float scales[MAX_SECTIONS + 1]; // last one is left for roads
		float3 rgbs[MAX_SECTIONS + 1];
		float3 normals[MAX_SECTIONS + 1];
		float3 speculars[MAX_SECTIONS + 1];

		float roadIntensity;
		float3 roadsColor;
		float _ScaleRoads;
		float4 _RoadsColor;

		/* FUNCTIONS ---------------------------------------------------------------------------- */
		float3 getCoefficients(int level, float luminance);
		float3 interpolate(float3 coefficients, float3 value1, float3 value2, float3 value3);
		float luminanceFromRGB(float3 rgb);
		void initialize(float2 uv);


		/*  --------------------------------------------------------------------------------- */
		/*  MAIN ---------------------------------------------------------------------------- */
		/*  --------------------------------------------------------------------------------- */

		/*  --------------------------------------------------------------------------------- */
		void surf(Input IN, inout SurfaceOutputStandard o) {

			float3 rgb = tex2D(_HeightMap, IN.uv_HeightMap).rgb;

			if (_displayType == 0)
			o.Albedo = rgb;

			else {

				initialize(IN.uv_HeightMap);
				float luminance = luminanceFromRGB(rgb);
				int level = 0;

				for (int i = 0; i < _numberOfSections; i++) {

					if (luminance < thresholds[i]) {
						level = i;
						break;
					}
				}
	
				float3 coefficients = getCoefficients(level, luminance);
				int upperIndex = level == _numberOfSections - 1 ? level : level + 1;
				int lowerIndex = level == 0 ? level : level - 1;

				o.Albedo = interpolate(coefficients, rgbs[level], rgbs[upperIndex], rgbs[lowerIndex]);
				o.Normal = interpolate(coefficients, normals[level], normals[upperIndex], normals[lowerIndex]);
				//o.Metallic = 1;

				fixed alpha = luminanceFromRGB(tex2D(_AlphaMap, IN.uv_HeightMap).rgb);
				//alpha *= alpha;

				if (_InvertAlpha == 1)
					alpha = 1 - alpha;

				o.Alpha = 1;
			}
		}



		/*  --------------------------------------------------------------------------------- */
		/*  COEFFICIENTS -------------------------------------------------------------------- */
		/*  --------------------------------------------------------------------------------- */

		float3 getCoefficients(int level, float luminance) {

			// 1) determine distance from adjacent sections ------
			float upper = 1;
			float upperPercent = 0;

			if (level != _numberOfSections - 1)
			{
				upper = thresholds[level] - luminance;
				if (upper < minimumMergeDists[level])
					upperPercent = 0.5f - 0.5f / minimumMergeDists[level] * upper;
			}

			float lower = 1;
			float lowerPercent = 0;

			if (level != 0)
			{
				lower = luminance - thresholds[level - 1];

				if (lower < minimumMergeDists[level - 1])
					lowerPercent = 0.5f - 0.5f / minimumMergeDists[level - 1] * lower;
			}

			// 2) normalize coefficients ---------------
			float middlePercent = 1.0f;

			if (lowerPercent > 0 && upperPercent == 0)
				middlePercent = 1.0f - lowerPercent;

			else if (lowerPercent == 0 && upperPercent > 0)
				middlePercent = 1.0f - upperPercent;

			else if (lowerPercent > 0 && upperPercent > 0) {

				float denom = 1.0f + lowerPercent + upperPercent;
				middlePercent = 1.0f / denom;
				upperPercent /= denom;
				lowerPercent /= denom;
			}

			return float3(middlePercent, upperPercent, lowerPercent);
		}

		/*  --------------------------------------------------------------------------------- */
		/*  INTERPOLATE --------------------------------------------------------------------- */
		/*  --------------------------------------------------------------------------------- */


		/* --------------------------------------------------------------------------------- */
		float3 interpolate(float3 coefficients, float3 value1, float3 value2, float3 value3) {

			float3 result = value1 * coefficients[0];
			if (coefficients[1] > 0)
				result += coefficients[1] * value2;
			if (coefficients[2] > 0)
				result += coefficients[2] * value3;

			return result;
		}

		/* ---------------------------------------------------------------------------- */
		float luminanceFromRGB(float3 rgb) {

			return (0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2]);
		}


		/*  --------------------------------------------------------------------------------- */
		/*  INITIALIZE ---------------------------------------------------------------------- */
		/*  --------------------------------------------------------------------------------- */
		void initialize(float2 uv) {

			thresholds[0] = _Threshold0;
			thresholds[1] = _Threshold1;
			thresholds[2] = _Threshold2;
			thresholds[3] = _Threshold3;
			thresholds[4] = 1;

			scales[0] = _Scale0;
			scales[1] = _Scale1;
			scales[2] = _Scale2;
			scales[3] = _Scale3;
			scales[4] = _Scale4;

			minimumMergeDists[0] = _MinimumMergeDistance0;
			minimumMergeDists[1] = _MinimumMergeDistance1;
			minimumMergeDists[2] = _MinimumMergeDistance2;
			minimumMergeDists[3] = _MinimumMergeDistance3;

			if (_displayType == 1) {

				rgbs[0] = tex2D(_Texture0, float2(uv.x * scales[0], uv.y * scales[0])).rgb;
				rgbs[1] = tex2D(_Texture1, float2(uv.x * scales[1], uv.y * scales[1])).rgb;
				rgbs[2] = tex2D(_Texture2, float2(uv.x * scales[2], uv.y * scales[2])).rgb;
				rgbs[3] = tex2D(_Texture3, float2(uv.x * scales[3], uv.y * scales[3])).rgb;
				rgbs[4] = tex2D(_Texture4, float2(uv.x * scales[4], uv.y * scales[4])).rgb;

				normals[0] = tex2D(_Normals0, float2(uv.x * scales[0], uv.y * scales[0]));
				normals[1] = tex2D(_Normals1, float2(uv.x * scales[1], uv.y * scales[1]));
				normals[2] = tex2D(_Normals2, float2(uv.x * scales[2], uv.y * scales[2]));
				normals[3] = tex2D(_Normals3, float2(uv.x * scales[3], uv.y * scales[3]));
				normals[4] = tex2D(_Normals4, float2(uv.x * scales[4], uv.y * scales[4]));

				speculars[0] = tex2D(_Spec0, float2(uv.x * scales[0], uv.y * scales[0])).rgb;
				speculars[1] = tex2D(_Spec1, float2(uv.x * scales[1], uv.y * scales[1])).rgb;
				speculars[2] = tex2D(_Spec2, float2(uv.x * scales[2], uv.y * scales[2])).rgb;
				speculars[3] = tex2D(_Spec3, float2(uv.x * scales[3], uv.y * scales[3])).rgb;
				speculars[4] = tex2D(_Spec4, float2(uv.x * scales[4], uv.y * scales[4])).rgb;
			}
			else {

				rgbs[0] = _Color0.rgb;
				rgbs[1] = _Color1.rgb;
				rgbs[2] = _Color2.rgb;
				rgbs[3] = _Color3.rgb;
				rgbs[4] = _Color4.rgb;
			}
		}


		ENDCG
	}
		FallBack "Unlit/Transparent"
}
