// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "CrossSection/Solid" {

Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	
	_SectionColor ("Section Color", Color) = (1,0,0,1)
    _SectionPlane ("SectionPlane (x, y, z)", vector) = (0.707,0,-0.2)
    _SectionPoint ("SectionPoint (x, y, z)", vector) = (0,0,0)
    _SectionOffset ("SectionOffset",float) = 0
}

   SubShader {

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
     
     Pass {
         Cull BACK // cull only front faces
         
         CGPROGRAM 
         #include "UnityCG.cginc"
         #pragma vertex vert
         #pragma fragment frag
         fixed4 _Color;
         
 		 fixed4 _SectionColor;
 		 fixed3 _SectionPlane;
	     fixed3 _SectionPoint;
	     float _SectionOffset;
  		 
         struct vertexInput {
            float4 vertex : POSITION;
         };
         struct vertexOutput {
            float4 pos : SV_POSITION;
            float3 wpos : TEXCOORD0;
         };
 
         vertexOutput vert(appdata_base input) {
            vertexOutput output;
            output.pos =  mul(UNITY_MATRIX_MVP, input.vertex);
            output.wpos = mul (unity_ObjectToWorld, input.vertex).xyz;
            return output;
         }
 
         float4 frag(vertexOutput input) : COLOR  {
         	if(_SectionOffset -dot((input.wpos - _SectionPoint),_SectionPlane) < 0) discard;
         	return _Color;
         }
         ENDCG  
         
      }
   }
}