#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1
#define AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
#endif

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Text;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2012-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

[AddComponentMenu("AVPro Movie Capture/From Scene")]
public class AVProMovieCaptureFromScene : AVProMovieCaptureBase
{
	private const int NewFrameSleepTimeMs = 6;

	public override bool PrepareCapture()
	{
		if (_capturing)
			return false;
		
		SelectRecordingResolution(Screen.width, Screen.height);
				
		_pixelFormat = AVProMovieCapturePlugin.PixelFormat.RGBA32;
		if (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"))
		{
			_pixelFormat = AVProMovieCapturePlugin.PixelFormat.BGRA32;
			_isTopDown = true;
		}
		else
		{
			_isTopDown = false;
			
			if (_isDirectX11)
			{
				_isTopDown = false;
			}
		}
		
		GenerateFilename();

		return base.PrepareCapture();
	}
	
	private IEnumerator FinalRenderCapture()
	{
		yield return new WaitForEndOfFrame();

		bool canGrab = true;

		if (_useMotionBlur && !_isRealTime && _motionBlur != null)
		{
			// If the motion blur is still accumulating, don't grab this frame
			canGrab = _motionBlur.IsFrameAccumulated;
		}

		if (canGrab)
		{
			// Wait for the encoder to require a new frame to be sent
			while (_handle >= 0 && !AVProMovieCapturePlugin.IsNewFrameDue(_handle))
			{
				System.Threading.Thread.Sleep(NewFrameSleepTimeMs);
			}

			// Send the new frame to encode
			if (_handle >= 0)
			{
				// Grab final RenderTexture into texture and encode
				if (_audioCapture && _audioDeviceIndex < 0 && !_noAudio && _isRealTime)
				{
					int audioDataLength = 0;
					System.IntPtr audioDataPtr = _audioCapture.ReadData(out audioDataLength);
					if (audioDataLength > 0)
					{
						AVProMovieCapturePlugin.EncodeAudio(_handle, audioDataPtr, (uint)audioDataLength);
					}
				}
#if AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
				GL.IssuePluginEvent(_renderEventFunction, AVProMovieCapturePlugin.PluginID | (int)AVProMovieCapturePlugin.PluginEvent.CaptureFrameBuffer | _handle);
#else
				GL.IssuePluginEvent(AVProMovieCapturePlugin.PluginID | (int)AVProMovieCapturePlugin.PluginEvent.CaptureFrameBuffer | _handle);
#endif
				GL.InvalidateState();

				UpdateFPS();
			}
		}
	
		yield return null;
	}
	
	public override void UpdateFrame()
	{
		if (_capturing && !_paused)
		{
			StartCoroutine("FinalRenderCapture");
		}
		base.UpdateFrame();
	}
}