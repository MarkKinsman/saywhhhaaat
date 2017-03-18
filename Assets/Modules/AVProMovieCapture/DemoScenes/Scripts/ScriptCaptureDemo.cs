using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class ScriptCaptureDemo : MonoBehaviour 
{
	private const string X264CodecName = "x264vfw - H.264/MPEG-4 AVC codec";
	public int _width = 512;
	public int _height = 512;
	public int _frameRate = 30;
	public string _filePath;

	private int _videoCodecIndex;
	private int _encoderHandle;

	void Start()
	{
		if (AVProMovieCapturePlugin.Init())
		{
			// Find the index for the video codec
			_videoCodecIndex = FindVideoCodecIndex(X264CodecName);
		}
		else
		{
			this.enabled = false;
		}
	}

	void OnDestroy()
	{
		AVProMovieCapturePlugin.Deinit();
	}

	public void CreateVideoFromByteArray(string filePath, int width, int height, int frameRate)
	{
		byte[] frameData = new byte[width * height * 4];
		GCHandle frameHandle = GCHandle.Alloc(frameData, GCHandleType.Pinned);

		// Start the recording session
		int encoderHandle = AVProMovieCapturePlugin.CreateRecorderAVI(filePath, (uint)width, (uint)height, frameRate, (int)AVProMovieCapturePlugin.PixelFormat.RGBA32, false, _videoCodecIndex, false, 0, 0, -1, -1, false, false);
		if (encoderHandle >= 0)
		{
			AVProMovieCapturePlugin.Start(encoderHandle);

			// Write out 100 frames
			int numFrames = 100;
			for (int i = 0; i < numFrames; i++)
			{
				// TODO: update the byte array with your data :)


				// Wait for the encoder to be ready for the next frame
				int numAttempts = 32;
				while (numAttempts > 0)
				{
					if (AVProMovieCapturePlugin.IsNewFrameDue(encoderHandle))
					{
						// Encode the new frame
						AVProMovieCapturePlugin.EncodeFrame(encoderHandle, frameHandle.AddrOfPinnedObject());
						break;
					}
					System.Threading.Thread.Sleep(1);
					numAttempts--;
				}
			}

			// End the session
			AVProMovieCapturePlugin.Stop(encoderHandle, false);
			AVProMovieCapturePlugin.FreeRecorder(encoderHandle);
		}

		if (frameHandle.IsAllocated)
		{
			frameHandle.Free();
		}
	}

	private static int FindVideoCodecIndex(string name)
	{
		int result = -1;
		int numVideoCodecs = AVProMovieCapturePlugin.GetNumAVIVideoCodecs();
		for (int i = 0; i < numVideoCodecs; i++)
		{
			if (AVProMovieCapturePlugin.GetAVIVideoCodecName(i) == name)
			{
				result = i;
				break;
			}
		}
		return result;
	}
}
