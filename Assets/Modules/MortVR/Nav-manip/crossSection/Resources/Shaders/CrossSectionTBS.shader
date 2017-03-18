  Shader "CrossSection/Transparent/Bumped Specular" {
    Properties {
   	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
      
    _SectionPlane ("SectionPlane (x, y, z)", vector) = (0.707,0,-0.2)
    _SectionPoint ("SectionPoint (x, y, z)", vector) = (0,0,0)
    _SectionOffset ("SectionOffset",float) = 0
 }

    
SubShader {
	Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
	LOD 400
	
		Zwrite Off
		CGPROGRAM
		#pragma surface surf BlinnPhong alpha
		#pragma exclude_renderers flash
		#pragma debug

		sampler2D _MainTex;
		sampler2D _BumpMap;
		fixed4 _Color;
		half _Shininess;
		
	    fixed3 _SectionPlane;
	    fixed3 _SectionPoint;
	    float _SectionOffset;

		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			clip (_SectionOffset -dot((IN.worldPos - _SectionPoint),_SectionPlane));
			fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = tex.rgb * _Color.rgb;
			o.Gloss = tex.a;
			o.Alpha = tex.a * _Color.a;
			o.Specular = _Shininess;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
		}
		ENDCG
    } 

FallBack "Transparent/Cutout/VertexLit"
  }