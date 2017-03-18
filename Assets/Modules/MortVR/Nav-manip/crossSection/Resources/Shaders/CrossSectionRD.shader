// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with '_Object2World'

Shader "CrossSection/Reflective/Diffuse" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
	_MainTex ("Base (RGB) RefStrength (A)", 2D) = "white" {} 
	_Cube ("Reflection Cubemap", Cube) = "_Skybox" {}

	_SectionColor ("Section Color", Color) = (1,0,0,1)
    _SectionPlane ("SectionPlane (x, y, z)", vector) = (0.707,0,-0.2)
    _SectionPoint ("SectionPoint (x, y, z)", vector) = (0,0,0)
    _SectionOffset ("SectionOffset",float) = 0

}




SubShader {
	LOD 200
	Tags { "RenderType"="Opaque" }


				//  crossection pass (backfaces + fog)
	   Pass {
         Cull front // cull only front faces
         
         CGPROGRAM 
         
         #pragma vertex vert
         #pragma fragment frag
		 #pragma multi_compile_fog
		 #include "UnityCG.cginc"

 		 fixed4 _SectionColor;
 		 float3 _SectionPlane;
	     float3 _SectionPoint;
	     float _SectionOffset;
  		 
         struct vertexInput {
            float4 vertex : POSITION;			
         };

		 struct fragmentInput{
                float4 pos : SV_POSITION;
				float3 wpos : TEXCOORD0;
                UNITY_FOG_COORDS(1)
         };

		 fragmentInput vert(vertexInput i){
                fragmentInput o;
                o.pos = mul (UNITY_MATRIX_MVP, i.vertex);
                o.wpos = mul (unity_ObjectToWorld, i.vertex).xyz;

                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
         }

         fixed4 frag(fragmentInput i) : SV_Target {
				if(_SectionOffset -dot((i.wpos - _SectionPoint),_SectionPlane) < 0) discard;
				if( _SectionColor.a <0.5f) discard;
                fixed4 color = _SectionColor;
                UNITY_APPLY_FOG(i.fogCoord, color); 
                return color;
         }

         ENDCG  
         
      }

		// --------------------------------------------------------------------


	
			CGPROGRAM
			#pragma surface surf Lambert

			sampler2D _MainTex;
			samplerCUBE _Cube;

			fixed4 _Color;
			fixed4 _ReflectColor;
			float3 _SectionPlane;
			float3 _SectionPoint;
			float _SectionOffset;

			struct Input {
				float2 uv_MainTex;
				float3 worldRefl;
				float3 worldPos;
			};

			void surf (Input IN, inout SurfaceOutput o) {
				clip (_SectionOffset -dot((IN.worldPos - _SectionPoint),_SectionPlane));
				fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
				fixed4 c = tex * _Color;
				o.Albedo = c.rgb;
	
				fixed4 reflcol = texCUBE (_Cube, IN.worldRefl);
				reflcol *= tex.a;
				o.Emission = reflcol.rgb * _ReflectColor.rgb;
				o.Alpha = reflcol.a * _ReflectColor.a;
			}
			ENDCG
		}
	
		FallBack "Legacy Shaders/Reflective/VertexLit"
	} 