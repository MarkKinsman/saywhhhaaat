  Shader "CrossSection/Transparent/Diffuse" {
    Properties {
   	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
      
    _SectionPlane ("SectionPlane (x, y, z)", vector) = (0.707,0,-0.2)
    _SectionPoint ("SectionPoint (x, y, z)", vector) = (0,0,0)
    _SectionOffset ("SectionOffset",float) = 0
 }

    
SubShader {
	Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
	LOD 400
	
		Zwrite Off
		CGPROGRAM
		#pragma surface surf Lambert alpha
		#pragma exclude_renderers flash
		#pragma debug

		sampler2D _MainTex;
		fixed4 _Color;
		
	    fixed3 _SectionPlane;
	    fixed3 _SectionPoint;
	    float _SectionOffset;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			clip (_SectionOffset -dot((IN.worldPos - _SectionPoint),_SectionPlane));
			fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = tex.rgb * _Color.rgb;
			o.Alpha = tex.a * _Color.a;
		}
		ENDCG
		
    } 

FallBack "Transparent/Cutout/VertexLit"
  }