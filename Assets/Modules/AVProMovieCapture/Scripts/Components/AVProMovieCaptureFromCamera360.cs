#if UNITY_5
	#if !UNITY_5_0 && !UNITY_5_1
		#define AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
	#endif
	#if !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2 && !UNITY_5_3
		#define AVPRO_MOVIECAPTURE_RENDERTEXTUREDIMENSIONS_54
	#endif
#endif
using UnityEngine;

//-----------------------------------------------------------------------------
// Copyright 2012-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

[RequireComponent(typeof(Camera))]
[AddComponentMenu("AVPro Movie Capture/From Camera 360 VR (requires Camera)")]
public class AVProMovieCaptureFromCamera360 : AVProMovieCaptureBase
{
	[Header("Cube map")]
	public int _cubemapResolution = 2048;
	public int _cubemapDepth = 16;

	private Material _cubemapToEquirectangularMaterial;
	private RenderTexture _target;
	private RenderTexture _cubeTarget;
	private Camera _cubeCamera;
	private Camera _camera;

#if false
    private void OnRenderImage(RenderTexture source, RenderTexture dest)
	{
#if false
		if (_capturing && !_paused)
		{
			while (_handle >= 0 && !AVProMovieCapturePlugin.IsNewFrameDue(_handle))
			{
				System.Threading.Thread.Sleep(1);
			}
			if (_handle >= 0)
			{
                if (_audioCapture && _audioDeviceIndex < 0 && !_noAudio)
                {
                    uint bufferLength = (uint)_audioCapture.BufferLength;
                    if (bufferLength > 0)
                    {
                        AVProMovieCapturePlugin.EncodeAudio(_handle, _audioCapture.BufferPtr, bufferLength);
                        _audioCapture.FlushBuffer();
                    }
                }

                // In Direct3D the RT can be flipped vertically
                /*if (source.texelSize.y < 0)
                {

                }*/

				Graphics.Blit(_cubeTarget, _target, _cubemapToEquirectangularMaterial);

#if AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
				GL.IssuePluginEvent(_renderEventFunction, AVProMovieCapturePlugin.PluginID | (int)AVProMovieCapturePlugin.PluginEvent.CaptureFrameBuffer | _handle);
#else
                GL.IssuePluginEvent(AVProMovieCapturePlugin.PluginID | (int)AVProMovieCapturePlugin.PluginEvent.CaptureFrameBuffer | _handle);
#endif
				GL.InvalidateState();
				
				UpdateFPS();
			}
		}
#endif
		// Pass-through

		if (_cubeTarget != null)
		{
			Graphics.Blit(_cubeTarget, dest, _cubemapToEquirectangularMaterial);
		}
		else
		{
			Graphics.Blit(source, dest);
		}
	}
#endif

	public override void UpdateFrame()
	{
		if (_capturing && !_paused)
		{
			if (_cubeTarget != null && _camera != null)
			{
				while (_handle >= 0 && !AVProMovieCapturePlugin.IsNewFrameDue(_handle))
				{
					System.Threading.Thread.Sleep(1);
				}
				if (_handle >= 0)
				{
					if (_audioCapture && _audioDeviceIndex < 0 && !_noAudio && _isRealTime)
					{
						uint bufferLength = (uint)_audioCapture.BufferLength;
						if (bufferLength > 0)
						{
							AVProMovieCapturePlugin.EncodeAudio(_handle, _audioCapture.BufferPtr, bufferLength);
							_audioCapture.FlushBuffer();
						}
					}

					// In Direct3D the RT can be flipped vertically
					/*if (source.texelSize.y < 0)
					{

					}*/

					_cubeCamera.transform.position = _camera.transform.position;
					_cubeCamera.transform.rotation = _camera.transform.rotation;
					_cubeCamera.RenderToCubemap(_cubeTarget, 63);

					Graphics.Blit(_cubeTarget, _target, _cubemapToEquirectangularMaterial);

#if AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
					GL.IssuePluginEvent(_renderEventFunction, AVProMovieCapturePlugin.PluginID | (int)AVProMovieCapturePlugin.PluginEvent.CaptureFrameBuffer | _handle);
#else
					GL.IssuePluginEvent(AVProMovieCapturePlugin.PluginID | (int)AVProMovieCapturePlugin.PluginEvent.CaptureFrameBuffer | _handle);
#endif
					GL.InvalidateState();

					UpdateFPS();
				}
			}
		}

		base.UpdateFrame();
	}

	public override bool PrepareCapture()
	{
		if (_capturing)
			return false;
		
		// Setup material
		_pixelFormat = AVProMovieCapturePlugin.PixelFormat.RGBA32;
        _isTopDown = true;

		Camera camera = this.GetComponent<Camera>();
		int width = Mathf.FloorToInt(camera.pixelRect.width);
		int height = Mathf.FloorToInt(camera.pixelRect.height);
		_camera = camera;

		// Setup rendering a different render target if we're overriding resolution or anti-aliasing
		//if (_renderResolution != Resolution.Original || _renderAntiAliasing != QualitySettings.antiAliasing)
		{

			// Resolution
			if (_renderResolution == Resolution.Custom)
			{
				width = _renderWidth;
				height = _renderHeight;
			}
			else if (_renderResolution != Resolution.Original)
			{
				GetResolution(_renderResolution, ref width, ref height);
			}

			// Anti-aliasing 
			int aaLevel = QualitySettings.antiAliasing;
			if (aaLevel == 0)
				aaLevel = 1;

			if (_renderAntiAliasing > 0)
				aaLevel = _renderAntiAliasing;

			if (aaLevel != 1 && aaLevel != 2 && aaLevel != 4 && aaLevel != 8)
			{
				Debug.LogError("[AVProMovieCapture] Invalid antialiasing value, must be 1, 2, 4 or 8.  Defaulting to 1. >> " + aaLevel);
				aaLevel = 1;
			}

			// Create the render target
			if (_target != null)
			{
				_target.DiscardContents();
				if (_target.width != width || _target.height != height)
				{
					RenderTexture.ReleaseTemporary(_target);
					_target = null;
				}
			}
			if (_target == null)
			{
				_target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
			}

			// Create the cube render target
			if (_cubeTarget != null)
			{
				_cubeTarget.DiscardContents();
				if (_cubeTarget.width != _cubemapResolution || _cubeTarget.height != _cubemapResolution || aaLevel != _cubeTarget.antiAliasing)
				{
					RenderTexture.Destroy(_cubeTarget);
					_cubeTarget = null;
				}
			}
			if (_cubeTarget == null)
			{
				if (!Mathf.IsPowerOfTwo(_cubemapResolution))
				{
					Debug.LogWarning ("[AVProMovieCapture] Cubemap must be power-of-2 dimensions, resizing to closest");
					_cubemapResolution = Mathf.ClosestPowerOfTwo(_cubemapResolution);
				}

				_cubeTarget = new RenderTexture(_cubemapResolution, _cubemapResolution, _cubemapDepth, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
				_cubeTarget.isPowerOfTwo = true;

#if AVPRO_MOVIECAPTURE_RENDERTEXTUREDIMENSIONS_54
				_cubeTarget.dimension = UnityEngine.Rendering.TextureDimension.Cube;
#else
				_cubeTarget.isCubemap = true;
#endif

				_cubeTarget.useMipMap = false;
				_cubeTarget.generateMips = false;
				_cubeTarget.antiAliasing = aaLevel;
				_cubeTarget.wrapMode = TextureWrapMode.Clamp;
				_cubeTarget.filterMode = FilterMode.Bilinear;

				if (_cubeCamera == null)
				{
					GameObject go = new GameObject("AVProMovieCapture-CubemapCamera", typeof(Camera));
					go.hideFlags = HideFlags.HideAndDontSave;
					_cubeCamera = go.GetComponent<Camera>();
					_cubeCamera.CopyFrom(camera);
					_cubeCamera.targetTexture = null;
					_cubeCamera.enabled = false;
				}
			}

			// Adjust size for camera rectangle
			/*if (camera.rect.width < 1f || camera.rect.height < 1f)
			{
				float rectWidth = Mathf.Clamp01(camera.rect.width + camera.rect.x) - Mathf.Clamp01(camera.rect.x);
				float rectHeight = Mathf.Clamp01(camera.rect.height + camera.rect.y) - Mathf.Clamp01(camera.rect.y);
				width = Mathf.FloorToInt(width * rectWidth);
				height = Mathf.FloorToInt(height * rectHeight);
			}*/
		}

		SelectRecordingResolution(width, height);

		GenerateFilename();

		return base.PrepareCapture();
	}

	public override void Start()
	{
		Shader shader = Resources.Load<Shader>("CubemapToEquirectangular");
		if (shader != null)
		{
			_cubemapToEquirectangularMaterial = new Material(shader);
		}
		else
		{
			Debug.LogError("[AVProMovieCapture] Can't find CubemapToEquirectangular shader");
		}

		base.Start();
	}

	public override void OnDestroy()
	{
		if (_target != null)
		{
			RenderTexture.ReleaseTemporary(_target);
			_target = null;
		}
		if (_cubeTarget != null)
		{
			RenderTexture.Destroy(_cubeTarget);
			_cubeTarget = null;
		}
		if (_cubeCamera != null)
		{
			GameObject.Destroy(_cubeCamera.gameObject);
			_cubeCamera = null;
		}

		base.OnDestroy();
	}
}