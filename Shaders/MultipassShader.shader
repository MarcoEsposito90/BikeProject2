Shader "Custom/MultipassShader" {
	Properties{
		_Color0("Color0", Color) = (1,1,1,1)
		_Color1("Color1", Color) = (0,0,0,0)
	}

		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 200

			Pass {
		
				Blend One Zero
				CGPROGRAM

					#pragma vertex vert
					#pragma fragment frag
					#pragma target 3.0

					struct VertexInput {
						float4 vertex : POSITION;
					};

					struct VertexOutput {
						float4 position : SV_POSITION;
					};

					fixed4 _Color0;
					fixed4 _Color1;

					VertexOutput vert(VertexInput v) 
					{
			
						VertexOutput o;
						o.position = mul(UNITY_MATRIX_MVP, v.vertex);
				
						return o;
					}

					half4 frag(VertexOutput o) : COLOR
					{
						return _Color0;
					}
			
				ENDCG
			}


			Pass {

				Blend OneMinusDstColor One
				CGPROGRAM
					
					#pragma vertex vert
					#pragma fragment frag
					#pragma target 3.0

					struct VertexInput {
						float4 vertex : POSITION;
					};

					struct VertexOutput {
						float4 position : SV_POSITION;
					};

					fixed4 _Color0;
					fixed4 _Color1;

					VertexOutput vert(VertexInput v)
					{

						VertexOutput o;
						o.position = mul(UNITY_MATRIX_MVP, v.vertex);

						return o;
					}

					half4 frag(VertexOutput o) : COLOR
					{
						return _Color1;
					}

				ENDCG
			}

	}
		FallBack "Diffuse"
}
