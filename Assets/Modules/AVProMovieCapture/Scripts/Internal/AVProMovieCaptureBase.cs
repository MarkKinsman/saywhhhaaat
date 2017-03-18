#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1
	#define AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
#endif
using UnityEngine;
using System.IO;
using System;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2012-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

public class AVProMovieCaptureBase : MonoBehaviour 
{
	public enum FrameRate
	{
		Fifteen = 15,
		TwentyFour = 24,
		TwentyFive = 25,
		Thirty = 30,
		Fifty = 50,
		Sixty = 60,
		SeventyFive = 75,
		Ninety = 90,
		OneTwenty = 120,
	}

	public enum Resolution
	{
		POW2_4096x4096,
		POW2_4096x2048,
		POW2_2048x2048,
		POW2_2048x1024,
		POW2_1024x1024,
		UHD_3840x2160,
		HD_1920x1080,
		HD_1280x720,
		SD_1024x768,
		SD_800x600,
		SD_800x450,
		SD_640x480,
		SD_640x360,
		SD_320x240,
		Original,
		Custom,
	}
	
	public enum CubemapDepth
	{
		Depth_24 = 24,
		Depth_16 = 16,
		Depth_Zero = 0,
	}
	
	public enum CubemapResolution
	{
		POW2_4096 = 4096,
		POW2_2048 = 2048,
		POW2_1024 = 1024,
		POW2_512 = 512,
		POW2_256 = 256,
	}	

	public enum AntiAliasingLevel
	{
		UseCurrent,
		ForceNone,
		ForceSample2,
		ForceSample4,
		ForceSample8,
	}
	
	public enum DownScale
	{
		Original = 1,
		Half = 2,
		Quarter = 4,
		Eighth = 8,
		Sixteenth = 16,
		Custom = 100,
	}
	
	public enum OutputPath
	{
		RelativeToProject,
		RelativeToPeristentData,
		Absolute,
	}

	public enum OutputExtension
	{
		AVI,
		MP4,
		PNG,
		Custom = 100,
	}

	public enum OutputType
	{
		VideoFile,
		NamedPipe,
	}

	[Header("General")]

	public KeyCode _captureKey = KeyCode.None;
	public bool _captureOnStart = false;
	public bool _startPaused = false;
	public bool _listVideoCodecsOnStart = false;
	public bool _isRealTime = true;

	[Header("Video")]

	public string[] _videoCodecPriority = { "Lagarith Lossless Codec",
											"x264vfw - H.264/MPEG-4 AVC codec",
											"Xvid MPEG-4 Codec" };
	public FrameRate _frameRate = FrameRate.Thirty;
	public DownScale _downScale = DownScale.Original;
	public Vector2 _maxVideoSize = Vector2.zero;
	public int _forceVideoCodecIndex = -1;
	public bool _flipVertically = false;

	[Header("Audio")]

	public bool _noAudio = true;
	public string[] _audioCodecPriority = { };
	public int _forceAudioCodecIndex = -1;
	public int _forceAudioDeviceIndex = -1;
	public AVProUnityAudioCapture _audioCapture;

	[Header("Output")]

	public bool _autoGenerateFilename = true;
	public OutputPath _outputFolderType = OutputPath.RelativeToProject;
	public string _outputFolderPath;
	public string _autoFilenamePrefix = "MovieCapture";
	public string _autoFilenameExtension = "avi";
	public string _forceFilename = "movie.avi";
	public OutputType _outputType = OutputType.VideoFile;

	[Header("Camera Specific")]

	public Resolution _renderResolution = Resolution.Original;
	public int _renderWidth = 0;
	public int _renderHeight = 0;
	public int _renderAntiAliasing = -1;

	[Header("Motion Blur")]

	public bool _useMotionBlur = false;
	[Range(0, 64)]
	public int _motionBlurSamples = 0;
	public Camera[] _motionBlurCameras;
	protected AVProMovieCaptureMotionBlur _motionBlur;

	[Header("Performance")]

	public bool _allowVSyncDisable = true;
	public bool _allowFrameRateChange = true;

	[Header("Experimental")]
	public bool _useMediaFoundationH264 = false;

	

	[System.NonSerialized]
	public string _codecName = "uncompressed";
	[System.NonSerialized]
	public int _codecIndex = -1;

	[System.NonSerialized]
	public string _audioCodecName = "uncompressed";
	[System.NonSerialized]
	public int _audioCodecIndex = -1;

	[System.NonSerialized]
	public string _audioDeviceName = "Unity";
	[System.NonSerialized]
	public int _audioDeviceIndex = -1;
	
	[System.NonSerialized]
	public int _unityAudioSampleRate = -1;
	[System.NonSerialized]
	public int _unityAudioChannelCount = -1;

	protected Texture2D _texture;
	protected int _handle = -1;
	protected int _targetWidth, _targetHeight;
	protected bool _capturing = false;
	protected bool _paused = false;
	protected string _filePath;
	protected FileInfo _fileInfo;
	protected AVProMovieCapturePlugin.PixelFormat _pixelFormat = AVProMovieCapturePlugin.PixelFormat.YCbCr422_YUY2;
	private int _oldVSyncCount = 0;
	private int _oldTargetFrameRate = -1;
	private float _oldFixedDeltaTime = 0f;
	protected bool _isTopDown = true;
	protected bool _isDirectX11 = false;
	private bool _queuedStartCapture = false;
	private bool _queuedStopCapture = false;

	public string LastFilePath  
	{
		get { return _filePath; }
	}

	[Header("Other")]
	public long _minimumDiskSpaceMB = 16;
	private long _freeDiskSpaceMB;
	
	// Stats
	private uint _numDroppedFrames;
	private uint _numDroppedEncoderFrames;
	private uint _numEncodedFrames;
	private uint _totalEncodedSeconds;

#if AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
	protected System.IntPtr _renderEventFunction = System.IntPtr.Zero;
	protected System.IntPtr _freeEventFunction = System.IntPtr.Zero;
#endif

	public uint NumDroppedFrames
	{
		get { return _numDroppedFrames; }
	}
	
	public uint NumDroppedEncoderFrames
	{
		get { return _numDroppedEncoderFrames; }
	}

	public uint NumEncodedFrames
	{
		get { return _numEncodedFrames; }
	}

	public uint TotalEncodedSeconds
	{
		get { return _totalEncodedSeconds; }
	}

	public void Awake()
	{
		try
		{
			if (AVProMovieCapturePlugin.Init())
			{
				Debug.Log("[AVProMovieCapture] Init plugin version: " + AVProMovieCapturePlugin.GetPluginVersion().ToString("F2") + " with GPU " + SystemInfo.graphicsDeviceVersion);
#if AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
				_renderEventFunction = AVProMovieCapturePlugin.GetRenderEventFunc();
				_freeEventFunction = AVProMovieCapturePlugin.GetFreeResourcesEventFunc();
#endif
			}
			else
			{
				Debug.LogError("[AVProMovieCapture] Failed to initialise plugin version: " + AVProMovieCapturePlugin.GetPluginVersion().ToString("F2") + " with GPU " + SystemInfo.graphicsDeviceVersion);
			}
		}
		catch (DllNotFoundException e)
		{
			Debug.LogError("[AVProMovieCapture] Unity couldn't find the DLL, did you move the 'Plugins' folder to the root of your project?");
			throw e;
		}

		_isDirectX11 = SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 11");
		
		SelectCodec(_listVideoCodecsOnStart);
		SelectAudioCodec(_listVideoCodecsOnStart);
		SelectAudioDevice(_listVideoCodecsOnStart);		
	}
	
	public virtual void Start() 
	{
		Application.runInBackground = true;
		
		if (_captureOnStart)
		{
			StartCapture();
		}
	}
	
	public void SelectCodec(bool listCodecs)
	{
		// Enumerate video codecs
		int numVideoCodecs = AVProMovieCapturePlugin.GetNumAVIVideoCodecs();
		if (listCodecs)
		{
			for (int i = 0; i < numVideoCodecs; i++)
			{
				Debug.Log("VideoCodec " + i + ": " + AVProMovieCapturePlugin.GetAVIVideoCodecName(i));
			}
		}
		
		// The user has specified their own codec index
		if (_forceVideoCodecIndex >= 0)
		{
			if (_forceVideoCodecIndex < numVideoCodecs)
			{
				_codecName = AVProMovieCapturePlugin.GetAVIVideoCodecName(_forceVideoCodecIndex);
				_codecIndex = _forceVideoCodecIndex;
			}
		}
		else
		{
			// Try to find the codec based on the priority list
			if (_videoCodecPriority != null)
			{
				foreach (string codec in _videoCodecPriority)
				{
					string codecName = codec.Trim();
					// Empty string means uncompressed
					if (string.IsNullOrEmpty(codecName))
						break;
					
					for (int i = 0; i < numVideoCodecs; i++)
					{
						if (codecName == AVProMovieCapturePlugin.GetAVIVideoCodecName(i))
						{
							_codecName = codecName;
							_codecIndex = i;
							break;
						}
					}
					
					if (_codecIndex >= 0)
						break;
				}
			}
		}
		
		if (_codecIndex < 0)
		{
			_codecName = "Uncompressed";
			Debug.LogWarning("[AVProMovieCapture] Codec not found.  Video will be uncompressed.");
		}
	}
	

	public void SelectAudioCodec(bool listCodecs)
	{
		// Enumerate audio codecs
		int numAudioCodecs = AVProMovieCapturePlugin.GetNumAVIAudioCodecs();
		if (listCodecs)
		{
			for (int i = 0; i < numAudioCodecs; i++)
			{
				Debug.Log("AudioCodec " + i + ": " + AVProMovieCapturePlugin.GetAVIAudioCodecName(i));
			}
		}
		
		// The user has specified their own codec index
		if (_forceAudioCodecIndex >= 0)
		{
			if (_forceAudioCodecIndex < numAudioCodecs)
			{
				_audioCodecName = AVProMovieCapturePlugin.GetAVIAudioCodecName(_forceAudioCodecIndex);
				_audioCodecIndex = _forceAudioCodecIndex;
			}
		}
		else
		{
			// Try to find the codec based on the priority list
			if (_audioCodecPriority != null)
			{
				foreach (string codec in _audioCodecPriority)
				{
					string codecName = codec.Trim();
					// Empty string means uncompressed
					if (string.IsNullOrEmpty(codecName))
						break;
					
					for (int i = 0; i < numAudioCodecs; i++)
					{
						if (codecName == AVProMovieCapturePlugin.GetAVIAudioCodecName(i))
						{
							_audioCodecName = codecName;
							_audioCodecIndex = i;
							break;
						}
					}
					
					if (_audioCodecIndex >= 0)
						break;
				}
			}
		}
		
		if (_audioCodecIndex < 0)
		{
			_audioCodecName = "Uncompressed";
			Debug.LogWarning("[AVProMovieCapture] Codec not found.  Audio will be uncompressed.");
		}
	}	

	public void SelectAudioDevice(bool display)
	{
		// Enumerate
		int num = AVProMovieCapturePlugin.GetNumAVIAudioInputDevices();
		if (display)
		{
			for (int i = 0; i < num; i++)
			{
				Debug.Log("AudioDevice " + i + ": " + AVProMovieCapturePlugin.GetAVIAudioInputDeviceName(i));
			}
		}

		// The user has specified their own device index
		if (_forceAudioDeviceIndex >= 0)
		{
			if (_forceAudioDeviceIndex < num)
			{
				_audioDeviceName = AVProMovieCapturePlugin.GetAVIAudioInputDeviceName(_forceAudioDeviceIndex);
				_audioDeviceIndex = _forceAudioDeviceIndex;
			}
		}
		else
		{
			/*_audioDeviceIndex = -1;
			// Try to find one of the loopback devices
			for (int i = 0; i < num; i++)
			{
				StringBuilder sbName = new StringBuilder(512);
				if (AVProMovieCapturePlugin.GetAVIAudioInputDeviceName(i, sbName))
				{
					string[] loopbackNames = { "Stereo Mix", "What U Hear", "What You Hear", "Waveout Mix", "Mixed Output" };
					for (int j = 0; j < loopbackNames.Length; j++)
					{
						if (sbName.ToString().Contains(loopbackNames[j]))
						{
							_audioDeviceIndex = i;
							_audioDeviceName = sbName.ToString();
						}
					}
				}
				if (_audioDeviceIndex >= 0)
					break;
			}
			
			if (_audioDeviceIndex < 0)
			{
				// Resort to the no recording device
				_audioDeviceName = "Unity";
				_audioDeviceIndex = -1;
			}*/

			_audioDeviceName = "Unity";
			_audioDeviceIndex = -1;
		}
	}

	public static Vector2 GetRecordingResolution(int width, int height, DownScale downscale, Vector2 maxVideoSize)
	{
		int targetWidth = width;
		int targetHeight = height;
		if (downscale != DownScale.Custom)
		{
			targetWidth /= (int)downscale;
			targetHeight /= (int)downscale;
		}
		else
		{
			if (maxVideoSize.x >= 1.0f && maxVideoSize.y >= 1.0f)
			{
				targetWidth = Mathf.FloorToInt(maxVideoSize.x);
				targetHeight = Mathf.FloorToInt(maxVideoSize.y);
			}
		}
		
		// Some codecs like Lagarith in YUY2 mode need size to be multiple of 4
		targetWidth = NextMultipleOf4(targetWidth);
		targetHeight = NextMultipleOf4(targetHeight);

		return new Vector2(targetWidth, targetHeight);
	}

	public void SelectRecordingResolution(int width, int height)
	{
		_targetWidth = width;
		_targetHeight = height;
		if (_downScale != DownScale.Custom)
		{
			_targetWidth /= (int)_downScale;
			_targetHeight /= (int)_downScale;
		}
		else
		{
			if (_maxVideoSize.x >= 1.0f && _maxVideoSize.y >= 1.0f)
			{
				_targetWidth = Mathf.FloorToInt(_maxVideoSize.x);
				_targetHeight = Mathf.FloorToInt(_maxVideoSize.y);
			}
		}
		
		// Some codecs like Lagarith in YUY2 mode need size to be multiple of 4
		_targetWidth = NextMultipleOf4(_targetWidth);
		_targetHeight = NextMultipleOf4(_targetHeight);
	}

	public virtual void OnDestroy()
	{
		StopCapture();
		AVProMovieCapturePlugin.Deinit();
	}
	
	void OnApplicationQuit()
	{
		StopCapture();
		AVProMovieCapturePlugin.Deinit();
	}
		
	protected void EncodeTexture(Texture2D texture)
	{
		Color32[] bytes = texture.GetPixels32();
		GCHandle _frameHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		
		EncodePointer(_frameHandle.AddrOfPinnedObject());		
				
		if (_frameHandle.IsAllocated)
			_frameHandle.Free();
	}
	
	public virtual void EncodePointer(System.IntPtr ptr)
	{
		if (_audioCapture == null || (_audioDeviceIndex >= 0 || _noAudio) && !_isRealTime)
		{
			AVProMovieCapturePlugin.EncodeFrame(_handle, ptr);
		}
		else
		{
			int audioDataLength = 0;
			System.IntPtr audioDataPtr = _audioCapture.ReadData(out audioDataLength);
			if (audioDataLength > 0)
            {
				AVProMovieCapturePlugin.EncodeFrameWithAudio(_handle, ptr, audioDataPtr, (uint)audioDataLength);
            }
            else
            {
                AVProMovieCapturePlugin.EncodeFrame(_handle, ptr);
            }
		}
	}
	
	public bool IsCapturing()
	{
		return _capturing;
	}

	public bool IsPaused()
	{
		return _paused;
	}
	
	public int GetRecordingWidth()
	{
		return _targetWidth;
	}
	
	public int GetRecordingHeight()
	{
		return _targetHeight;
	}
	
	protected virtual string GenerateTimestampedFilename(string filenamePrefix, string filenameExtension)
	{
		TimeSpan span = (DateTime.Now - DateTime.Now.Date);
		return string.Format("{0}-{1}-{2}-{3}-{4}s-{5}x{6}.{7}", filenamePrefix, DateTime.Now.Year, DateTime.Now.Month.ToString("D2"), DateTime.Now.Day.ToString("D2"), ((int)(span.TotalSeconds)).ToString(), _targetWidth, _targetHeight, filenameExtension);		
	}

	private static string GetFolder(OutputPath outputPathType, string path)
	{
		string fileFolder = string.Empty;
		if (outputPathType == OutputPath.RelativeToProject)
		{
			string projectFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
			fileFolder = System.IO.Path.Combine(projectFolder, path);
		}
		else if (outputPathType == OutputPath.RelativeToPeristentData)
		{
			string projectFolder = System.IO.Path.GetFullPath(Application.persistentDataPath);
			fileFolder = System.IO.Path.Combine(projectFolder, path);
		}
		else if (outputPathType == OutputPath.Absolute)
		{
			fileFolder = path;
		}
		return fileFolder;
	}
	
	private static string AutoGenerateFilename(OutputPath outputPathType, string path, string filename)
	{
		// Create folder
		string fileFolder = GetFolder(outputPathType, path);
		
		// Combine path and filename
		return System.IO.Path.Combine(fileFolder, filename);
	}
	
	private static string ManualGenerateFilename(OutputPath outputPathType, string path, string filename)
	{
		string result = GetFolder(outputPathType, path);

		if (System.IO.Path.IsPathRooted(filename))
		{
			result = filename;
		}
		else
		{
			result = System.IO.Path.Combine(result, filename);
		}
		
		return result;
	}
	
	/*[ContextMenu("Debug GenerateFilename")]
	public void DebugGenereateFilename()
	{
		GenerateFilename();
		Debug.Log("PATH: " + _filePath);
	}*/
	
	protected void GenerateFilename()
	{	
		if (_autoGenerateFilename || string.IsNullOrEmpty(_forceFilename))
		{
			string filename = GenerateTimestampedFilename(_autoFilenamePrefix, _autoFilenameExtension);
			_filePath = AutoGenerateFilename(_outputFolderType, _outputFolderPath, filename);
		}
		else
		{
			_filePath = ManualGenerateFilename(_outputFolderType, _outputFolderPath, _forceFilename);
		}
		
		// Create target directory if doesn't exist
		String directory = Path.GetDirectoryName(_filePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			Directory.CreateDirectory(directory);
	}
	
	public virtual bool PrepareCapture()
	{
		// Delete file if it already exists
		if (File.Exists(_filePath))
		{
			File.Delete(_filePath);
		}

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		if (_minimumDiskSpaceMB > 0)
		{
			ulong freespace = 0;
			if (DriveFreeBytes(System.IO.Path.GetPathRoot(_filePath), out freespace))
			{
				_freeDiskSpaceMB = (long)(freespace / (1024 * 1024));
			}

			if (!IsEnoughDiskSpace())
			{
				Debug.LogError("[AVProMovieCapture] Not enough free space to start capture.  Stopping capture.");
				return false;
			}
		}
#endif

		// Disable vsync
		if (_allowVSyncDisable && !Screen.fullScreen && QualitySettings.vSyncCount > 0)
		{
			_oldVSyncCount = QualitySettings.vSyncCount;
			QualitySettings.vSyncCount = 0;
		}
		
		if (_isRealTime)
		{
			if (_allowFrameRateChange)
			{
				_oldTargetFrameRate = Application.targetFrameRate;
				Application.targetFrameRate = (int)_frameRate;
			}
		}
		else
		{
			if (_useMotionBlur && _motionBlurSamples > 1 && _motionBlurCameras.Length > 0)
			{
				Time.captureFramerate = _motionBlurSamples * (int)_frameRate;

				// Setup the motion blur filters
				foreach (Camera camera in _motionBlurCameras)
				{
					AVProMovieCaptureMotionBlur mb = camera.GetComponent<AVProMovieCaptureMotionBlur>();
					if (mb == null)
					{
						mb = camera.gameObject.AddComponent<AVProMovieCaptureMotionBlur>();
					}
					if (mb != null)
					{
						mb.NumSamples = _motionBlurSamples;
						_motionBlur = mb;
					}

					mb.enabled = true;
				}
			}
			else
			{
				Time.captureFramerate = (int)_frameRate;
			}

			// Change physics update speed
			_oldFixedDeltaTime = Time.fixedDeltaTime;
			Time.fixedDeltaTime = 1.0f / Time.captureFramerate;
		}
		
		int audioDeviceIndex = _audioDeviceIndex;
		int audioCodecIndex = _audioCodecIndex;
		bool noAudio = _noAudio;
		if (!_isRealTime)
			noAudio = true;

		// We if try to capture audio from Unity but there isn't an AVProUnityAudioCapture component set
		if (!noAudio && _audioCapture == null && _audioDeviceIndex < 0)
		{
			// Try to find it locally
			_audioCapture = this.GetComponent<AVProUnityAudioCapture>();
			if (_audioCapture == null)
			{
				// Try to find it globally
				_audioCapture = GameObject.FindObjectOfType<AVProUnityAudioCapture>();
			}

			if (_audioCapture == null)
			{
				AudioListener audioListener = this.GetComponent<AudioListener>();
				if (audioListener == null)
				{
					audioListener = GameObject.FindObjectOfType<AudioListener>();
				}
				if (audioListener != null)
				{
					_audioCapture = audioListener.gameObject.AddComponent<AVProUnityAudioCapture>();
					Debug.LogWarning("[AVProMovieCapture] Capturing audio from Unity without an AVProUnityAudioCapture assigned so we had to create one manually (slow)");
				}
				else
				{
					noAudio = true;
					Debug.LogWarning("[AVProMovieCapture] No audio listener found in scene.  Unable to capture audio from Untiy.");
				}
			}
			else
			{
				Debug.LogWarning("[AVProMovieCapture] Capturing audio from Unity without an AVProUnityAudioCapture assigned so we had to search for one manually (slow)");
			}
		}

		if (noAudio || (_audioCapture == null && _audioDeviceIndex < 0))
		{
			audioCodecIndex = audioDeviceIndex = -1;
			_audioDeviceName = "none";
			noAudio = true;
		}
		
		_unityAudioSampleRate = -1;
		_unityAudioChannelCount = -1;
		if (!noAudio && _audioDeviceIndex < 0 && _audioCapture != null)
		{
			if (!_audioCapture.enabled)
				_audioCapture.enabled = true;
			_unityAudioSampleRate = AudioSettings.outputSampleRate;
			_unityAudioChannelCount = _audioCapture.NumChannels;
		}
		
		string info = string.Format("{0}x{1} @ {2}fps [{3}]", _targetWidth, _targetHeight, ((int)_frameRate).ToString(), _pixelFormat.ToString());
		info += string.Format(" vcodec:'{0}'", _codecName);
		if (!noAudio)
		{
			if (_audioDeviceIndex >= 0)
			{
				info += string.Format(" audio device:'{0}'", _audioDeviceName);
			}
			else
			{
				info += string.Format(" audio device:'Unity' {0}hz {1} channels", _unityAudioSampleRate, _unityAudioChannelCount);
			}
			info += string.Format(" acodec:'{0}'", _audioCodecName);
		}
		info += string.Format(" to file: '{0}'", _filePath);

        // If the user has overriden the vertical flip
        if (_flipVertically)
            _isTopDown = !_isTopDown;

		if (_outputType == OutputType.VideoFile)
		{
			// TOOD: make _frameRate floating point, or add timeLapse time system
			Debug.Log("[AVProMovieCapture] Start File Capture: " + info);
			_handle = AVProMovieCapturePlugin.CreateRecorderAVI(_filePath, (uint)_targetWidth, (uint)_targetHeight, (int)_frameRate,
			                                                    (int)(_pixelFormat), _isTopDown, _codecIndex, !noAudio, _unityAudioSampleRate, _unityAudioChannelCount, audioDeviceIndex, audioCodecIndex, _isRealTime, _useMediaFoundationH264);
		}
		else if (_outputType == OutputType.NamedPipe)
		{
			Debug.Log("[AVProMovieCapture] Start Pipe Capture: " + info);
			_handle = AVProMovieCapturePlugin.CreateRecorderPipe(_filePath, (uint)_targetWidth, (uint)_targetHeight, (int)_frameRate,
			                                                     (int)(_pixelFormat), _isTopDown);
		}

		if (_handle < 0)
		{
			Debug.LogError("[AVProMovieCapture] Failed to create recorder");
			StopCapture();
		}

		return (_handle >= 0);
	}
	
	public void QueueStartCapture()
	{
		_queuedStartCapture = true;
	}

	public bool StartCapture()
	{
		if (_capturing)
			return false;

		if (_handle < 0)
		{
			if (!PrepareCapture())
			{
				return false;
			}
		}

		if (_handle >= 0)
		{
			if (_audioCapture && _audioDeviceIndex < 0 && !_noAudio)
			{
				_audioCapture.FlushBuffer();
			}

			AVProMovieCapturePlugin.Start(_handle);
			ResetFPS();
			_capturing = true;
			_paused = false;
		}

		if (_startPaused)
		{
			PauseCapture();
		}

		return _capturing;
	}
	
	public void PauseCapture()
	{
		if (_capturing && !_paused)
		{
			AVProMovieCapturePlugin.Pause(_handle);
			_paused = true;
			ResetFPS();
		}
	}
	
	public void ResumeCapture()
	{
		if (_capturing && _paused)
		{
			if (_audioCapture && _audioDeviceIndex < 0 && !_noAudio)
			{
				_audioCapture.FlushBuffer();
			}

			AVProMovieCapturePlugin.Start(_handle);
			_paused = false;
		}
	}

	public void CancelCapture()
	{
		StopCapture(true);

		// Delete file
		if (File.Exists(_filePath))
		{
			File.Delete(_filePath);
		}
	}

	public virtual void UnprepareCapture()
	{
	}
	
	public virtual void StopCapture(bool skipPendingFrames = false)
	{
		UnprepareCapture();

		if (_capturing)
		{
			Debug.Log("[AVProMovieCapture] Stopping capture");
			_capturing = false;
		}
		
#if AVPRO_MOVIECAPTURE_GLISSUEEVENT_52
		GL.IssuePluginEvent(_freeEventFunction, AVProMovieCapturePlugin.PluginID | (int)AVProMovieCapturePlugin.PluginEvent.FreeResources);
#else
		GL.IssuePluginEvent(AVProMovieCapturePlugin.PluginID | (int)AVProMovieCapturePlugin.PluginEvent.FreeResources);
#endif		
		
		if (_handle >= 0)
		{
			AVProMovieCapturePlugin.Stop(_handle, skipPendingFrames);
			//System.Threading.Thread.Sleep(100);
			AVProMovieCapturePlugin.FreeRecorder(_handle);
			_handle = -1;
		}

		_fileInfo = null;

		if (_audioCapture)
		{
			_audioCapture.enabled = false;
		}

		if (_motionBlur)
		{
			_motionBlur.enabled = false;
		}
		
		// Restore Unity timing
		Time.captureFramerate = 0;
		Application.targetFrameRate = _oldTargetFrameRate;
		_oldTargetFrameRate = -1;

		if (_oldFixedDeltaTime > 0f)
		{
			Time.fixedDeltaTime = _oldFixedDeltaTime;
		}
		_oldFixedDeltaTime = 0f;		

		if (_oldVSyncCount > 0)
		{
			QualitySettings.vSyncCount = _oldVSyncCount;
			_oldVSyncCount = 0;
		}

		_motionBlur = null;
		
		if (_texture != null)
		{
			Destroy(_texture);
			_texture = null;
		}
	}
	
	private void ToggleCapture()
	{
		if (_capturing)
		{
			//_queuedStopCapture = true;
			//_queuedStartCapture = false;
			StopCapture();
		}
		else
		{
			//_queuedStartCapture = true;
			//_queuedStopCapture = false;
			StartCapture();
		}
	}

	private bool IsEnoughDiskSpace()
	{
		bool result = true;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		long fileSizeMB = GetCaptureFileSize() / (1024 * 1024);

		if ((_freeDiskSpaceMB - fileSizeMB) < _minimumDiskSpaceMB)
		{
			result = false;
		}
#endif
		return result;
	}

	void Update()
	{
		if (_handle >= 0 && !_paused)
		{
			CheckFreeDiskSpace();
		}

		UpdateFrame();
	}

	private void CheckFreeDiskSpace()
	{
		if (_minimumDiskSpaceMB > 0)
		{
			if (!IsEnoughDiskSpace())
			{
				Debug.LogWarning("[AVProMovieCapture] Free disk space getting too low.  Stopping capture.");
				StopCapture();
			}
		}

	}
	
	public virtual void UpdateFrame() 
	{
		if (Input.GetKeyDown(_captureKey))
		{
			ToggleCapture();
		}
		
		if (_handle >= 0 && !_paused)
		{
			_numDroppedFrames = AVProMovieCapturePlugin.GetNumDroppedFrames(_handle);
			_numDroppedEncoderFrames = AVProMovieCapturePlugin.GetNumDroppedEncoderFrames(_handle);
			_numEncodedFrames = AVProMovieCapturePlugin.GetNumEncodedFrames(_handle);
			_totalEncodedSeconds = AVProMovieCapturePlugin.GetEncodedSeconds(_handle);
		}
		
		if (_queuedStopCapture)
		{
			_queuedStopCapture = false;
			_queuedStartCapture = false;
			StopCapture();
		}
		if (_queuedStartCapture)
		{
			_queuedStartCapture = false;
			StartCapture();
		}
	}

	[NonSerializedAttribute]
	public float _fps;
	[NonSerializedAttribute]
	public int _frameTotal;
	
	private int _frameCount;
	private float _startFrameTime;
	
	protected void ResetFPS()
	{
		_frameCount = 0;
		_frameTotal = 0;
		_fps = 0.0f;
		_startFrameTime = 0.0f;
	}
	
	public void UpdateFPS()
	{
		_frameCount++;
		_frameTotal++;
		
		float timeNow = Time.realtimeSinceStartup;
		float timeDelta = timeNow - _startFrameTime;
		if (timeDelta >= 1.0f)
		{
			_fps = (float)_frameCount / timeDelta;
			_frameCount  = 0;
			_startFrameTime = timeNow;
		}
	}	
	
    private void ConfigureCodec() 
	{
		AVProMovieCapturePlugin.Init();
       	SelectCodec(false);
		if (_codecIndex >= 0)
		{
			AVProMovieCapturePlugin.ConfigureVideoCodec(_codecIndex);
		}
		//AVProMovieCapture.Deinit();
	}

	public long GetCaptureFileSize()
	{
		long result = 0;
#if !UNITY_WEBPLAYER
		if (_handle >= 0)
		{
			if (_fileInfo == null && File.Exists(_filePath))
			{
				_fileInfo = new System.IO.FileInfo(_filePath);
			}
			if (_fileInfo != null)
			{
				_fileInfo.Refresh();
				result = _fileInfo.Length;
			}
		}
#endif
		return result;
	}

	private static long GetFileSize(string filename)
	{
#if UNITY_WEBPLAYER
		return 0;
#else
		System.IO.FileInfo fi = new System.IO.FileInfo(filename);
		return fi.Length;
#endif
	}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
	// Pinvoke for API function
	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
	out ulong lpFreeBytesAvailable,
	out ulong lpTotalNumberOfBytes,
	out ulong lpTotalNumberOfFreeBytes);

	public static bool DriveFreeBytes(string folderName, out ulong freespace)
	{
		freespace = 0;
		if (string.IsNullOrEmpty(folderName))
		{
			throw new ArgumentNullException("folderName");
		}

		if (!folderName.EndsWith("\\"))
		{
			folderName += '\\';
		}

		ulong free = 0, dummy1 = 0, dummy2 = 0;

		if (GetDiskFreeSpaceEx(folderName, out free, out dummy1, out dummy2))
		{
			freespace = free;
			return true;
		}
		else
		{
			return false;
		}
	}
#endif

	public static void GetResolution(Resolution res, ref int width, ref int height)
	{
		switch (res) 
		{
		case Resolution.POW2_4096x4096:
			width = 4096; height = 4096;
			break;
		case Resolution.POW2_4096x2048:
			width = 4096; height = 2048;
			break;
		case Resolution.POW2_2048x2048:
			width = 2048; height = 2048;
			break;
		case Resolution.POW2_2048x1024:
			width = 2048; height = 1024;
			break;
		case Resolution.POW2_1024x1024:
			width = 1024; height = 1024;
			break;
		case Resolution.UHD_3840x2160:
			width = 3840; height = 2160;
			break;
		case Resolution.HD_1920x1080:
			width = 1920; height = 1080;
			break;
		case Resolution.HD_1280x720:
			width = 1280; height = 720;
			break;
		case Resolution.SD_1024x768:
			width = 1024; height = 768;
			break;
		case Resolution.SD_800x600:
			width = 800; height = 600;
			break;
		case Resolution.SD_800x450:
			width = 800; height = 450;
			break;
		case Resolution.SD_640x480:
			width = 640; height = 480;
			break;
		case Resolution.SD_640x360:
			width = 640; height = 360;
			break;
		case Resolution.SD_320x240:
			width = 320; height = 240;
			break;
		}
	}
	// Returns the next multiple of 4 or the same value if it's already a multiple of 4
	protected static int NextMultipleOf4(int value)
	{
		return (value + 3) & ~0x03;
	}
}