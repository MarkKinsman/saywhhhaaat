#ifndef PLANE_CLIPPING_INCLUDED
#define PLANE_CLIPPING_INCLUDED

//Plane clipping definitions

#if CLIP_ONE
	//PLANE_CLIPPING_ENABLED will be defined.
	//This makes it easier to check if this feature is available or not.
	#define PLANE_CLIPPING_ENABLED 1

	float _SectionOffset;
	float3 _SectionPlane;
	float3 _SectionPoint;


	//discard drawing of a point in the world if it is behind any one of the planes.
	void PlaneClip(float3 posWorld) {
	  clip (_SectionOffset - dot((posWorld - _SectionPoint),_SectionPlane));
	}

//preprocessor macro that will produce an empty block if no clipping planes are used.
#define PLANE_CLIP(posWorld) PlaneClip(posWorld);
    
#else
//empty definition
#define PLANE_CLIP(s)
#endif

#endif // PLANE_CLIPPING_INCLUDED