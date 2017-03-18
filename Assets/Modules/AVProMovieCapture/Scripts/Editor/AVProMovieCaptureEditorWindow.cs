#if UNITY_5
	#if !UNITY_5_0
		#define AVPRO_MOVIECAPTURE_WINDOWTITLE_51
	#endif
	#if !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
		#define AVPRO_MOVIECAPTURE_SCENEMANAGER_53
	#endif
#endif

using UnityEngine;
using UnityEditor;

//-----------------------------------------------------------------------------
// Copyright 2012-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

/// <summary>
/// Creates a dockable window in Unity that can be used for handy in-editor capturing
/// </summary>
public class AVProMovieCaptureEditorWindow : EditorWindow 
{
	private const string TempGameObjectName = "Temp_MovieCapture";
	private const string SettingsPrefix = "AVProMovieCapture.EditorWindow.";
		
	private AVProMovieCaptureBase _capture;
	private AVProMovieCaptureFromScene _captureScene;
	private AVProMovieCaptureFromCamera _captureCamera;
	private AVProMovieCaptureFromCamera360 _captureCamera360;
	private AVProUnityAudioCapture _audioCapture;
	private static bool _isCreated = false;
	private static bool _isInit = false;
	private static bool _isFailedInit = false;
	private static string _lastCapturePath;
	
	private static string[] _videoCodecNames;
	private static string[] _audioCodecNames;
	private static string[] _audioDeviceNames;
	private static bool[] _videoCodecConfigurable;
	private static bool[] _audioCodecConfigurable;
	private readonly string[] _downScales = { "Original", "Half", "Quarter", "Eighth", "Sixteenth", "Custom" };	
	//private readonly string[] _frameRates = { "15", "24", "25", "30", "50", "60" };
	private readonly string[] _captureModes = { "Realtime Capture", "Offline Render" };
	private readonly string[] _outputFolders = { "Project Folder", "Persistent Data Folder", "Absolute Folder" };
	private readonly string[] _aaNames = { "Current", "None", "2x", "4x", "8x" };

	private enum SourceType
	{
		Scene,
		Camera,
		Camera360,
	}
	
	private SourceType _sourceType = SourceType.Scene;
	private bool _startPaused = false;
	private Camera _cameraNode;
	private string _cameraName;	
	private int _captureModeIndex;
	private int _outputFolderIndex;
	private bool _autoFilenamePrefixFromSceneName = true;
	private string _autoFilenamePrefix = "capture";
	private string _autoFilenameExtension = "avi";
	private string _outputFolderRelative = string.Empty;
	private string _outputFolderAbsolute = string.Empty;
	private bool _appendTimestamp = true;
	private int _downScaleIndex;
	private int _downscaleX;
	private int _downscaleY;

	private AVProMovieCaptureBase.Resolution _renderResolution = AVProMovieCaptureBase.Resolution.Original;
	private int _renderWidth;
	private int _renderHeight;
	private int _renderAntiAliasing;

	private AVProMovieCaptureBase.FrameRate _frameRate = AVProMovieCaptureBase.FrameRate.Thirty;
	private int _videoCodecIndex;
	private bool _captureAudio;
	private int _audioCodecIndex;
	private int _audioDeviceIndex;
	private Vector2 _scroll = Vector2.zero;
	private bool _queueStart;
    private int _queueConfigureVideoCodec = -1;
    private int _queueConfigureAudioCodec = -1;

	private bool _useMotionBlur = false;
	private int _motionBlurSampleCount = 16;
	
	private int _cubemapResolution = 2048;
	private int _cubemapDepth = 16;

    private long _lastFileSize;
    private uint _lastEncodedMinutes;
    private uint _lastEncodedSeconds;
	private int _selectedTool;
	private int _selectedConfigTool;
	
	private static Texture2D _icon;

	private const string LinkPluginWebsite = "http://renderheads.com/product/av-pro-movie-capture/";
	private const string LinkForumPage = "http://forum.unity3d.com/threads/released-avpro-movie-capture.120717/";
	private const string LinkAssetStorePage = "https://www.assetstore.unity3d.com/en/#!/content/2670";
	private const string LinkEmailSupport = "mailto:unitysupport@renderheads.com";
	private const string LinkUserManual = "http://downloads.renderheads.com/docs/UnityAVProMovieCapture.pdf";

	private const string SupportMessage = "If you are reporting a bug, please include any relevant files and details so that we may remedy the problem as fast as possible.\n\n" +
		"Essential details:\n" +
		"+ Error message\n" +
		"      + The exact error message\n" + 
		"      + The console/output log if possible\n" +
		"+ Development environment\n" + 
		"      + Unity version\n" +
		"      + Development OS version\n" +
		"      + AVPro Movie Capture plugin version\n";			

	[MenuItem ("Window/AVPro Movie Capture")]
	private static void Init()
	{
		if (_isInit || _isCreated)
		{
	        AVProMovieCaptureEditorWindow window = (AVProMovieCaptureEditorWindow)EditorWindow.GetWindow(typeof(AVProMovieCaptureEditorWindow));
	        window.Close();
			return;
		}
						
		_isCreated = true;
		
        // Get existing open window or if none, make a new one:
        AVProMovieCaptureEditorWindow window2 = (AVProMovieCaptureEditorWindow)EditorWindow.GetWindow(typeof(AVProMovieCaptureEditorWindow));
		if (window2 != null)
		{
			window2.SetupWindow();
		}
	}

	public void SetupWindow()
	{
		_isCreated = true;
		if (Application.platform == RuntimePlatform.WindowsEditor)
		{
			this.minSize = new Vector2(200f, 48f);
			this.maxSize = new Vector2(340f, 620f);
#if AVPRO_MOVIECAPTURE_WINDOWTITLE_51
			this.titleContent = new GUIContent("Movie Capture", "AVPro Movie Capture");
#else
			this.title = "Movie Capture";
#endif
			this.CreateGUI();
			this.LoadSettings();
			this.Repaint();
		}
	}

	private void LoadSettings()
	{
		_sourceType = (SourceType)EditorPrefs.GetInt(SettingsPrefix + "SourceType", (int)_sourceType);
		_cameraName = EditorPrefs.GetString(SettingsPrefix + "CameraName", string.Empty);
		_captureModeIndex = EditorPrefs.GetInt(SettingsPrefix + "CaptureModeIndex", 0);
		_startPaused = EditorPrefs.GetBool(SettingsPrefix + "StartPaused", false);
		
		_autoFilenamePrefixFromSceneName = EditorPrefs.GetBool(SettingsPrefix + "AutoFilenamePrefixFromScenename", _autoFilenamePrefixFromSceneName);
		_autoFilenamePrefix = EditorPrefs.GetString(SettingsPrefix + "AutoFilenamePrefix", "capture");
		_autoFilenameExtension = EditorPrefs.GetString(SettingsPrefix + "AutoFilenameExtension", "avi");
		_appendTimestamp = EditorPrefs.GetBool(SettingsPrefix + "AppendTimestamp", true);
		
		_outputFolderIndex = EditorPrefs.GetInt(SettingsPrefix + "OutputFolderIndex", 0);
		_outputFolderRelative = EditorPrefs.GetString(SettingsPrefix + "OutputFolderRelative", string.Empty);
		_outputFolderAbsolute = EditorPrefs.GetString(SettingsPrefix + "OutputFolderAbsolute", string.Empty);
		
		_downScaleIndex = EditorPrefs.GetInt(SettingsPrefix + "DownScaleIndex", 0);
		_downscaleX = EditorPrefs.GetInt(SettingsPrefix + "DownScaleX", 1);
		_downscaleY = EditorPrefs.GetInt(SettingsPrefix + "DownScaleY", 1);
		_frameRate = (AVProMovieCaptureBase.FrameRate)System.Enum.Parse(typeof(AVProMovieCaptureBase.FrameRate), EditorPrefs.GetString(SettingsPrefix + "FrameRate", "Thirty"));
		_videoCodecIndex = EditorPrefs.GetInt(SettingsPrefix + "VideoCodecIndex", 0);

		_renderResolution = (AVProMovieCaptureBase.Resolution)EditorPrefs.GetInt(SettingsPrefix + "RenderResolution", (int)_renderResolution);
		_renderWidth = EditorPrefs.GetInt(SettingsPrefix + "RenderWidth", 0);
		_renderHeight = EditorPrefs.GetInt(SettingsPrefix + "RenderHeight", 0);
		_renderAntiAliasing = EditorPrefs.GetInt(SettingsPrefix + "RenderAntiAliasing", 0);

		_captureAudio = EditorPrefs.GetBool(SettingsPrefix + "CaptureAudio", false);
		_audioCodecIndex = EditorPrefs.GetInt(SettingsPrefix + "AudioCodecIndex", 0);
		_audioDeviceIndex = EditorPrefs.GetInt(SettingsPrefix + "AudioDeviceIndex", 0);

		_useMotionBlur = EditorPrefs.GetBool(SettingsPrefix + "UseMotionBlur", false);
		_motionBlurSampleCount = EditorPrefs.GetInt(SettingsPrefix + "MotionBlurSampleCount", 16);
		
		_cubemapResolution = EditorPrefs.GetInt(SettingsPrefix + "CubemapResolution", 2048);
		_cubemapDepth = EditorPrefs.GetInt(SettingsPrefix + "CubemapDepth", 16);

		if (!string.IsNullOrEmpty(_cameraName))
		{
			Camera[] cameras = (Camera[])GameObject.FindObjectsOfType(typeof(Camera));
			foreach (Camera cam in cameras)
			{
				if (cam.name == _cameraName)
				{
					_cameraNode = cam;
					break;
				}
			}
		}

		// Check ranges
		if (_videoCodecIndex >= _videoCodecNames.Length)
		{
			_videoCodecIndex = 0;
		}
		if (_audioDeviceIndex >= _audioDeviceNames.Length)
		{
			_audioDeviceIndex = 0;
			_captureAudio = false;
		}
		if (_audioCodecIndex >= _audioCodecNames.Length)
		{
			_audioCodecIndex = 0;
			_captureAudio = false;
		}
	}
	
	private void SaveSettings()
	{	
		EditorPrefs.SetInt(SettingsPrefix + "SourceType", (int)_sourceType);
		EditorPrefs.SetString(SettingsPrefix + "CameraName", _cameraName);
		EditorPrefs.SetInt(SettingsPrefix + "CaptureModeIndex", _captureModeIndex);
		EditorPrefs.SetBool(SettingsPrefix + "StartPaused", _startPaused);
		
		EditorPrefs.SetBool(SettingsPrefix + "AutoFilenamePrefixFromScenename", _autoFilenamePrefixFromSceneName);
		EditorPrefs.SetString(SettingsPrefix + "AutoFilenamePrefix", _autoFilenamePrefix);
		EditorPrefs.SetString(SettingsPrefix + "AutoFilenameExtension", _autoFilenameExtension);
		EditorPrefs.SetBool(SettingsPrefix + "AppendTimestamp", _appendTimestamp);
		
		EditorPrefs.SetInt(SettingsPrefix + "OutputFolderIndex", _outputFolderIndex);
		EditorPrefs.SetString(SettingsPrefix + "OutputFolderRelative", _outputFolderRelative);
		EditorPrefs.SetString(SettingsPrefix + "OutputFolderAbsolute", _outputFolderAbsolute);
		
		EditorPrefs.SetInt(SettingsPrefix + "DownScaleIndex", _downScaleIndex);
		EditorPrefs.SetInt(SettingsPrefix + "DownScaleX", _downscaleX);
		EditorPrefs.SetInt(SettingsPrefix + "DownScaleY", _downscaleY);
		EditorPrefs.SetString(SettingsPrefix + "FrameRate", _frameRate.ToString());
		EditorPrefs.SetInt(SettingsPrefix + "VideoCodecIndex", _videoCodecIndex);

		EditorPrefs.SetInt(SettingsPrefix + "RenderResolution", (int)_renderResolution);
		EditorPrefs.SetInt(SettingsPrefix + "RenderWidth", _renderWidth);
		EditorPrefs.SetInt(SettingsPrefix + "RenderHeight", _renderHeight);
		EditorPrefs.SetInt(SettingsPrefix + "RenderAntiAliasing", _renderAntiAliasing);

		EditorPrefs.SetBool(SettingsPrefix + "CaptureAudio", _captureAudio);
		EditorPrefs.SetInt(SettingsPrefix + "AudioCodecIndex", _audioCodecIndex);
		EditorPrefs.SetInt(SettingsPrefix + "AudioDeviceIndex", _audioDeviceIndex);

		EditorPrefs.SetBool(SettingsPrefix + "UseMotionBlur", _useMotionBlur);
		EditorPrefs.SetInt(SettingsPrefix + "MotionBlurSampleCount", _motionBlurSampleCount);
		
		EditorPrefs.SetInt(SettingsPrefix + "CubemapResolution", _cubemapResolution);
		EditorPrefs.SetInt(SettingsPrefix + "CubemapDepth", _cubemapDepth);
	}
	
	private void ResetSettings()
	{
		_sourceType = SourceType.Scene;
		_cameraNode = null;
		_cameraName = string.Empty;
		_captureModeIndex = 0;
		_startPaused = false;
		_autoFilenamePrefixFromSceneName = true;
		_autoFilenamePrefix = "capture";
		_autoFilenameExtension = "avi";
		_outputFolderIndex = 0;
		_outputFolderRelative = string.Empty;
		_outputFolderAbsolute = string.Empty;
		_appendTimestamp = true;
		_downScaleIndex = 0;
		_downscaleX = 1;
		_downscaleY = 1;
		_frameRate = AVProMovieCaptureBase.FrameRate.Thirty;
		_videoCodecIndex = 0;
		_renderResolution = AVProMovieCaptureBase.Resolution.Original;
		_renderWidth = 0;
		_renderHeight = 0;
		_renderAntiAliasing = 0;
		_captureAudio = false;
		_audioCodecIndex = 0;
		_audioDeviceIndex = 0;
		_useMotionBlur = false;
		_motionBlurSampleCount = 16;
		_cubemapResolution = 2048;
		_cubemapDepth = 16;
	}

	private static AVProMovieCaptureBase.DownScale GetDownScaleFromIndex(int index)
	{
		AVProMovieCaptureBase.DownScale result = AVProMovieCaptureBase.DownScale.Original;
		switch (index)
		{
		case 0:
			result = AVProMovieCaptureBase.DownScale.Original;
			break;
		case 1:
			result = AVProMovieCaptureBase.DownScale.Half;
			break;
		case 2:
			result = AVProMovieCaptureBase.DownScale.Quarter;
			break;
		case 3:
			result = AVProMovieCaptureBase.DownScale.Eighth;
			break;
		case 4:
			result = AVProMovieCaptureBase.DownScale.Sixteenth;
			break;
		case 5:
			result = AVProMovieCaptureBase.DownScale.Custom;
			break;
		}

		return result;
	}
		
	private void Configure(AVProMovieCaptureBase capture)
	{
		capture._videoCodecPriority = null;
		capture._audioCodecPriority = null;

		capture._captureOnStart = false;
		capture._listVideoCodecsOnStart = false;
		capture._frameRate = _frameRate;
		capture._downScale = GetDownScaleFromIndex(_downScaleIndex);
		if (capture._downScale == AVProMovieCaptureBase.DownScale.Custom)
		{
			capture._maxVideoSize.x = _downscaleX;
			capture._maxVideoSize.y = _downscaleY;
		}
		
		capture._isRealTime = (_captureModeIndex == 0);
		capture._startPaused = _startPaused;
		capture._autoGenerateFilename = _appendTimestamp;
		capture._autoFilenamePrefix = _autoFilenamePrefix;
		capture._autoFilenameExtension = _autoFilenameExtension;
		if (!capture._autoGenerateFilename)
		{
			capture._forceFilename = _autoFilenamePrefix + "." + _autoFilenameExtension;
		}
		
		capture._outputFolderType = AVProMovieCaptureBase.OutputPath.RelativeToProject;
		capture._outputFolderPath = _outputFolderRelative;
		if (_outputFolderIndex == 1)
		{
			capture._outputFolderType = AVProMovieCaptureBase.OutputPath.RelativeToPeristentData;
			capture._outputFolderPath = _outputFolderAbsolute;
		}
		if (_outputFolderIndex == 2)
		{
			capture._outputFolderType = AVProMovieCaptureBase.OutputPath.Absolute;
			capture._outputFolderPath = _outputFolderAbsolute;
		}
		
		capture._forceVideoCodecIndex = capture._codecIndex = Mathf.Max(-1, (_videoCodecIndex - 2));
		capture._noAudio = !_captureAudio;
		capture._forceAudioCodecIndex = capture._audioCodecIndex = Mathf.Max(-1, (_audioCodecIndex - 2));
		capture._forceAudioDeviceIndex = capture._audioDeviceIndex = Mathf.Max(-1, (_audioDeviceIndex - 2));

		if (_useMotionBlur && !capture._isRealTime && Camera.main != null)
		{
			capture._useMotionBlur = _useMotionBlur;
			capture._motionBlurSamples = _motionBlurSampleCount;
			capture._motionBlurCameras = new Camera[1];
			capture._motionBlurCameras[0] = Camera.main;
		}
		else
		{
			capture._useMotionBlur = false;
		}
	}
	
	private void CreateComponents()
	{
		switch (_sourceType)
		{
		case SourceType.Scene:
			_captureScene = (AVProMovieCaptureFromScene)GameObject.FindObjectOfType(typeof(AVProMovieCaptureFromScene));		
			if (_captureScene == null)
			{
				GameObject go = new GameObject(TempGameObjectName);
				_captureScene = go.AddComponent<AVProMovieCaptureFromScene>();
			}
			_capture = _captureScene;
			break;
		case SourceType.Camera:
			_captureCamera = _cameraNode.gameObject.GetComponent<AVProMovieCaptureFromCamera>();
			if (_captureCamera == null)
			{
				_captureCamera = _cameraNode.gameObject.AddComponent<AVProMovieCaptureFromCamera>();
			}
			_capture = _captureCamera;
			_capture._renderResolution = _renderResolution;
			_capture._renderWidth = _renderWidth;
			_capture._renderHeight = _renderHeight;
			_capture._renderAntiAliasing = _renderAntiAliasing;
			break;
		case SourceType.Camera360:
			_captureCamera360 = _cameraNode.gameObject.GetComponent<AVProMovieCaptureFromCamera360>();
			if (_captureCamera360 == null)
			{
				_captureCamera360 = _cameraNode.gameObject.AddComponent<AVProMovieCaptureFromCamera360>();
			}
			_capture = _captureCamera360;
			_capture._renderResolution = _renderResolution;
			_capture._renderWidth = _renderWidth;
			_capture._renderHeight = _renderHeight;
			_capture._renderAntiAliasing = _renderAntiAliasing;
			_captureCamera360._cubemapResolution = _cubemapResolution;
			_captureCamera360._cubemapDepth = _cubemapDepth;
			break;
		}
				
		_audioCapture = null;
		if (_captureAudio && _audioDeviceIndex == 0)
		{
			_audioCapture = (AVProUnityAudioCapture)GameObject.FindObjectOfType(typeof(AVProUnityAudioCapture));
			if (_audioCapture == null && Camera.main != null)
			{
				_audioCapture = Camera.main.gameObject.AddComponent<AVProUnityAudioCapture>();
			}
		}		
	}
	
	private void CreateGUI()
	{
		try
		{
			if (!AVProMovieCapturePlugin.Init())
			{
				Debug.LogError("[AVProMovieCapture] Failed to initialise");
				return;
			}
		}
		catch (System.DllNotFoundException e)
		{
			_isFailedInit = true;
			Debug.LogError("[AVProMovieCapture] Unity couldn't find the plugin DLL, please move the 'Plugins' folder to the root of your project.");
			throw e;
		}

		// Video codec enumeration
		int numVideoCodecs = Mathf.Max(0, AVProMovieCapturePlugin.GetNumAVIVideoCodecs());
		_videoCodecNames = new string[numVideoCodecs + 2];
		_videoCodecNames[0] = "Uncompressed";
		_videoCodecNames[1] = null;
		_videoCodecConfigurable = new bool[numVideoCodecs + 2];
		_videoCodecConfigurable[0] = false;
		_videoCodecConfigurable[1] = false;
		for (int i = 0; i < numVideoCodecs; i++)
		{
			_videoCodecNames[i+2] = i.ToString("D2") + ") " + AVProMovieCapturePlugin.GetAVIVideoCodecName(i).Replace("/", "_");
			_videoCodecConfigurable[i+2] = AVProMovieCapturePlugin.IsConfigureVideoCodecSupported(i);
		}

		// Audio device enumeration
		int numAudioDevices = Mathf.Max(0, AVProMovieCapturePlugin.GetNumAVIAudioInputDevices());
		_audioDeviceNames = new string[numAudioDevices+2];
		_audioDeviceNames[0] = "Unity";
		_audioDeviceNames[1] = null;
		for (int i = 0; i < numAudioDevices; i++)
		{
			_audioDeviceNames[i + 2] = i.ToString("D2") + ") " + AVProMovieCapturePlugin.GetAVIAudioInputDeviceName(i).Replace("/", "_");
		}

		// Audio codec enumeration
		int numAudioCodecs = Mathf.Max(0, AVProMovieCapturePlugin.GetNumAVIAudioCodecs());
		_audioCodecNames = new string[numAudioCodecs+2];
		_audioCodecNames[0] = "Uncompressed";
		_audioCodecNames[1] = null;
		_audioCodecConfigurable = new bool[numAudioCodecs+2];
		_audioCodecConfigurable[0] = false;
		_audioCodecConfigurable[1] = false;
		for (int i = 0; i < numAudioCodecs; i++)
		{
			_audioCodecNames[i + 2] = i.ToString("D2") + ") " + AVProMovieCapturePlugin.GetAVIAudioCodecName(i).Replace("/", "_");
			_audioCodecConfigurable[i + 2] = AVProMovieCapturePlugin.IsConfigureAudioCodecSupported(i);
		}

		_isInit = true;
	}

	void OnEnable()
	{
		if (!_isCreated)
		{
			SetupWindow();
		}		
	}
	
	void OnDisable()
	{
		SaveSettings();
		StopCapture();
		_isInit = false;
		_isCreated = false;
		Repaint();
	}
		
	private void StartCapture()
	{
		_lastFileSize = 0;
		CreateComponents();
		if (_capture != null)
		{
			_capture._audioCapture = _audioCapture;
			Configure(_capture);
			_capture.SelectCodec(false);
			if (!_capture._noAudio)
			{
				_capture.SelectAudioCodec(false);
				_capture.SelectAudioDevice(false);
			}
			_capture.QueueStartCapture();
		}		
	}
	
	private void StopCapture(bool cancelCapture = false)
	{
		if (_capture != null)
		{
			if (_capture.IsCapturing())
			{
				if (!cancelCapture)
				{
					_capture.StopCapture();
					_lastCapturePath = _capture.LastFilePath;
				}
				else
				{
					_capture.CancelCapture();
				}
			}
			_capture = null;
			_captureScene = null;
			_captureCamera = null;
		}
		_audioCapture = null;
		
		// TODO: put last captured link
	}

	private static bool ShowInExplorer(string itemPath)
	{
		bool result = false;

	    itemPath = System.IO.Path.GetFullPath(itemPath.Replace(@"/", @"\"));   // explorer doesn't like front slashes
		if (System.IO.File.Exists(itemPath))
		{
			System.Diagnostics.Process.Start("explorer.exe", "/select," + itemPath);
			result = true;
		}

		return result;
	}

	// Updates 10 times/second
	void OnInspectorUpdate()
	{
		if (_capture != null)
		{
			if (Application.isPlaying)
			{
				_lastFileSize = _capture.GetCaptureFileSize();

				_lastEncodedSeconds = (uint)Mathf.FloorToInt((float)_capture.NumEncodedFrames / (float)_capture._frameRate);
				//_lastEncodedSeconds = _capture.TotalEncodedSeconds;
				_lastEncodedMinutes = _lastEncodedSeconds / 60;
				_lastEncodedSeconds = _lastEncodedSeconds % 60;
			}
			else
			{
				StopCapture();
			}

		}
		else
		{
	        if (_queueConfigureVideoCodec >= 0)
	        {
	            int configureVideoCodec = _queueConfigureVideoCodec;
	            _queueConfigureVideoCodec = -1;
				AVProMovieCapturePlugin.Init();
	            AVProMovieCapturePlugin.ConfigureVideoCodec(configureVideoCodec);
	        }

	        if (_queueConfigureAudioCodec >= 0)
	        {
	            int configureAudioCodec = _queueConfigureAudioCodec;
	            _queueConfigureAudioCodec = -1;
				AVProMovieCapturePlugin.Init();
	            AVProMovieCapturePlugin.ConfigureAudioCodec(configureAudioCodec);
	        }				

			if (_queueStart && Application.isPlaying)
			{
				_queueStart = false;
				StartCapture();
			}
		}
		
		Repaint();
	}
	
	private static bool ShowConfigList(string title, string[] items, bool[] isConfigurable, ref int itemIndex, bool showConfig = true)
	{
		bool result = false;

		if (itemIndex < 0 || items == null)
			return result;
		
		EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
		EditorGUILayout.BeginHorizontal();
		itemIndex = EditorGUILayout.Popup(itemIndex, items);
		
		if (showConfig && isConfigurable != null && itemIndex < isConfigurable.Length)
		{
			EditorGUI.BeginDisabledGroup(itemIndex == 0 || !isConfigurable[itemIndex]);
			if (GUILayout.Button("Configure"))
			{
				result = true;
			}
			EditorGUI.EndDisabledGroup();
		}
		
		EditorGUILayout.EndHorizontal();		

		return result;
	}

    void OnGUI()
	{
		if (Application.platform != RuntimePlatform.WindowsEditor)
		{
			EditorGUILayout.LabelField("AVPro Movie Capture only works on the Windows platform.");
			return;
		}

		if (!_isInit)
		{
			if (_isFailedInit)
			{
				GUILayout.Label("Error", EditorStyles.boldLabel);
				GUI.enabled = false;
				GUILayout.TextArea("Unity couldn't find the AVPro Movie Capture plugin DLL.\n\nPlease move the 'Plugins' folder to the root of your project and try again.\n\nYou may then need to restart Unity for it to find the plugin DLLs.");
				GUI.enabled = true;
				return;
			}
			else
			{
				EditorGUILayout.LabelField("Initialising...");
				return;
			}
		}

		DrawControlButtonsGUI();		
			
		// Live Capture Stats	
		if (Application.isPlaying && _capture != null && _capture.IsCapturing())
		{
			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			DrawCapturingGUI();
			EditorGUILayout.EndScrollView();
		}
		// Configuration
		else if (_capture == null)
		{
			string[] _toolNames = { "Settings", "About" };
			_selectedTool = GUILayout.Toolbar(_selectedTool, _toolNames);
			switch (_selectedTool)
			{
				case 0:
					DrawConfigGUI_Toolbar();
					_scroll = EditorGUILayout.BeginScrollView(_scroll);
					DrawConfigGUI();
					EditorGUILayout.EndScrollView();
					break;
				case 1:
					_scroll = EditorGUILayout.BeginScrollView(_scroll);
					DrawConfigGUI_About();
					EditorGUILayout.EndScrollView();
					break;
			}
		}		
	}

	private void DrawControlButtonsGUI()
	{	
		EditorGUILayout.BeginHorizontal();
		if (_capture == null)
		{
			GUI.backgroundColor = Color.green;
			string startString = "Start Capture";
			if (_captureModeIndex == 1)
			{
				startString = "Start Render";
			}
			if (GUILayout.Button(startString, GUILayout.Height(32f)))
			{
				bool isReady = true;
				if (_sourceType == SourceType.Camera && _cameraNode == null)
				{
					Debug.LogError("[AVProMovieCapture] Please select a Camera to capture from, or select to capture from Scene.");
					isReady = false;
				}
				
				if (isReady)
				{
					if (!Application.isPlaying)
					{
						EditorApplication.isPlaying = true;
						_queueStart = true;
					}
					else
					{
						StartCapture();
						Repaint();
					}
				}
			}
		}
		else
		{
			GUI.backgroundColor = Color.cyan;
			if (GUILayout.Button("Cancel", GUILayout.Height(32f)))
			{
				StopCapture(true);
				Repaint();
			}
			GUI.backgroundColor = Color.red;
			if (GUILayout.Button("Stop", GUILayout.Height(32f)))
			{
				StopCapture(false);
				Repaint();
			}
		}
		
		EditorGUI.BeginDisabledGroup(_capture == null);
		if (_capture != null && _capture.IsPaused())
		{
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Resume", GUILayout.Height(32f)))
			{
				_capture.ResumeCapture();
				Repaint();
			}
		}
		else
		{
			GUI.backgroundColor = Color.yellow;
			if (GUILayout.Button("Pause", GUILayout.Height(32f)))
			{
				_capture.PauseCapture();
				Repaint();
			}
		}
		EditorGUI.EndDisabledGroup();
		
		EditorGUILayout.EndHorizontal();

		GUI.backgroundColor = Color.white;
		
		EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_lastCapturePath));
		if (GUILayout.Button("Open Last Capture"))
		{
			if (!ShowInExplorer(_lastCapturePath))
			{
				_lastCapturePath = string.Empty;
			}
		}
		EditorGUI.EndDisabledGroup();		
	}


	private void DrawCapturingGUI()
	{
		GUILayout.Space(8.0f);
		GUILayout.Label("Output", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		EditorGUI.indentLevel++;

		GUILayout.Label("Recording to: " + System.IO.Path.GetFileName(_capture.LastFilePath));
		GUILayout.Space(8.0f);

		GUILayout.Label("Video");
		EditorGUILayout.LabelField("Dimensions", _capture.GetRecordingWidth() + "x" + _capture.GetRecordingHeight() + " @ " + ((int)_capture._frameRate).ToString() + "hz");	
		EditorGUILayout.LabelField("Codec", _capture._codecName);

		if (!_capture._noAudio && _captureModeIndex == 0)
		{
			GUILayout.Label("Audio");
			EditorGUILayout.LabelField("Source", _capture._audioDeviceName);
			EditorGUILayout.LabelField("Codec", _capture._audioCodecName);
			if (_capture._audioDeviceName == "Unity")
			{
				EditorGUILayout.LabelField("Sample Rate", _capture._unityAudioSampleRate.ToString() + "hz");
				EditorGUILayout.LabelField("Channels", _capture._unityAudioChannelCount.ToString());
			}
		}

		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();

		GUILayout.Space(8.0f);

		GUILayout.Label("Stats", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		EditorGUI.indentLevel++;

		if (_capture._fps > 0f)
		{
			Color originalColor = GUI.color;
			float fpsDelta = (_capture._fps - (int)_capture._frameRate);
			GUI.color = Color.red;
			if (fpsDelta > -10)
				GUI.color = Color.yellow;
			if (fpsDelta > -2)
				GUI.color = Color.green;

			EditorGUILayout.LabelField("Capture Rate", _capture._fps.ToString("F1") + " FPS");
			
			GUI.color = originalColor;
		}
		else
		{
			EditorGUILayout.LabelField("Capture Rate", ".. FPS");
		}

		EditorGUILayout.LabelField("File Size", ((float)_lastFileSize / (1024f * 1024f)).ToString("F1") + "MB");
		EditorGUILayout.LabelField("Video Length", _lastEncodedMinutes + ":" + _lastEncodedSeconds + "s");
		EditorGUILayout.LabelField("Encoded Frames", _capture.NumEncodedFrames.ToString());

		GUILayout.Label("Dropped Frames");
		EditorGUILayout.LabelField("In Unity", _capture.NumDroppedFrames.ToString());
		EditorGUILayout.LabelField("In Encoder", _capture.NumDroppedEncoderFrames.ToString());
		if (!_capture._noAudio && _captureModeIndex == 0)
		{
			if (_capture._audioCapture && _capture._audioDeviceName == "Unity")
			{
				EditorGUILayout.LabelField("Audio Overflows", _capture._audioCapture.OverflowCount.ToString());
			}
		}

		EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
	}
		
	private void DrawConfigGUI_Toolbar()
	{
		string[] _toolNames = { "General", "Visual", "Audio", "Output" };
		_selectedConfigTool = GUILayout.Toolbar(_selectedConfigTool, _toolNames);
	}

	private void DrawConfigGUI()
	{	
		switch (_selectedConfigTool)
		{
			case 0:
				DrawConfigGUI_General();
				break;
			case 1:
				DrawConfigGUI_Visual();
				break;
			case 2:
				DrawConfigGUI_Audio();
				break;
			case 3:
				DrawConfigGUI_Output();
				break;
		}
		
		GUILayout.FlexibleSpace();	
	}
	
	private void DrawConfigGUI_About()
	{
		string version = AVProMovieCapturePlugin.GetPluginVersion().ToString();

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (_icon == null)
		{
			_icon = Resources.Load<Texture2D>("AVProMovieCaptureIcon");
		}
		if (_icon != null)
		{
			GUILayout.Label(new GUIContent(_icon));
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUI.color = Color.yellow;
		GUILayout.Label("AVPro Movie Capture by RenderHeads Ltd", EditorStyles.boldLabel);
		GUI.color = Color.white;
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUI.color = Color.yellow;
		GUILayout.Label("version " + version + " (scripts v" + AVProMovieCapturePlugin.ScriptVersion + ")");
		GUI.color = Color.white;
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		

		GUILayout.Space(32f);
		GUI.backgroundColor = Color.white;

		EditorGUILayout.LabelField("AVPro Movie Capture Links", EditorStyles.boldLabel);

		GUILayout.Space(8f);

		EditorGUILayout.LabelField("Documentation");
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("User Manual", GUILayout.ExpandWidth(false)))
		{
			Application.OpenURL(LinkUserManual);
		}
		EditorGUILayout.EndHorizontal();


		GUILayout.Space(16f);

		GUILayout.Label("Rate and Review", GUILayout.ExpandWidth(false));
		if (GUILayout.Button("Unity Asset Store Page", GUILayout.ExpandWidth(false)))
		{
			Application.OpenURL(LinkAssetStorePage);
		}

		GUILayout.Space(16f);

		GUILayout.Label("Community");
		if (GUILayout.Button("Unity Forum Page", GUILayout.ExpandWidth(false)))
		{
			Application.OpenURL(LinkForumPage);
		}

		GUILayout.Space(16f);

		GUILayout.Label("Homepage", GUILayout.ExpandWidth(false));
		if (GUILayout.Button("AVPro Movie Capture Website", GUILayout.ExpandWidth(false)))
		{
			Application.OpenURL(LinkPluginWebsite);
		}

		GUILayout.Space(16f);

		GUILayout.Label("Bugs and Support");
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Email unitysupport@renderheads.com", GUILayout.ExpandWidth(false)))
		{
			Application.OpenURL(LinkEmailSupport);
		}
		EditorGUILayout.EndHorizontal();

		GUILayout.Space(32f);

		EditorGUILayout.LabelField("Bug Reporting Notes", EditorStyles.boldLabel);

		EditorGUILayout.SelectableLabel(SupportMessage, EditorStyles.wordWrappedLabel, GUILayout.Height(180f));
	}

	private void DrawConfigGUI_General()
	{
		//GUILayout.Label("General", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		//EditorGUI.indentLevel++;

		_captureModeIndex = EditorGUILayout.Popup("Capture Mode", _captureModeIndex, _captureModes);
		_startPaused = EditorGUILayout.Toggle("Start Paused", _startPaused);

		// Source
		GUILayout.Space(8f);		
		EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;
		_sourceType = (SourceType)EditorGUILayout.EnumPopup("Type", _sourceType);
		if (_sourceType == SourceType.Camera || _sourceType == SourceType.Camera360)
		{
			if (_cameraNode == null && Camera.main != null)
			{
				_cameraNode = Camera.main;
			}
			_cameraNode = (Camera)EditorGUILayout.ObjectField("Camera", _cameraNode, typeof(Camera), true);
		}
		EditorGUI.indentLevel--;
		
		// Camera Overrides
		if (_sourceType == SourceType.Camera || _sourceType == SourceType.Camera360)
		{				
			GUILayout.Space(8f);
			EditorGUILayout.LabelField("Camera Overrides", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			_renderResolution = (AVProMovieCaptureBase.Resolution)EditorGUILayout.EnumPopup("Resolution", _renderResolution);
			if (_renderResolution == AVProMovieCaptureBase.Resolution.Custom)
			{
				Vector2 renderSize = new Vector2(_renderWidth, _renderHeight);
				renderSize = EditorGUILayout.Vector2Field("Size", renderSize);
				_renderWidth = Mathf.Clamp((int)renderSize.x, 1, AVProMovieCapturePlugin.MaxRenderWidth);
				_renderHeight = Mathf.Clamp((int)renderSize.y, 1, AVProMovieCapturePlugin.MaxRenderHeight);
			}

			int renderAntiAliasingIndex = 0;
			switch (_renderAntiAliasing)
			{
				case -1:
					renderAntiAliasingIndex = 0;
					break;
				case 1:
					renderAntiAliasingIndex = 1;
					break;
				case 2:
					renderAntiAliasingIndex = 2;
					break;
				case 4:
					renderAntiAliasingIndex = 3;
					break;
				case 8:
					renderAntiAliasingIndex = 4;
					break;
			}
			renderAntiAliasingIndex = EditorGUILayout.Popup("Anti Aliasing", renderAntiAliasingIndex, _aaNames);
			switch (renderAntiAliasingIndex)
			{
				case 0:
					_renderAntiAliasing = -1;
					break;
				case 1:
					_renderAntiAliasing = 1;
					break;
				case 2:
					_renderAntiAliasing = 2;
					break;
				case 3:
					_renderAntiAliasing = 4;
					break;
				case 4:
					_renderAntiAliasing = 8;
					break;
			}
			
			if (_cameraNode != null)
			{
				if (_cameraNode.clearFlags == CameraClearFlags.Nothing || _cameraNode.clearFlags == CameraClearFlags.Depth)
				{
					if (_renderResolution != AVProMovieCaptureBase.Resolution.Original || _renderAntiAliasing != -1)
					{
						GUI.color = Color.yellow;
						GUILayout.TextArea("Warning: Overriding camera resolution or anti-aliasing when clear flag is set to " + _cameraNode.clearFlags + " may result in incorrect captures");
						GUI.color = Color.white;
					}
				}
			}
			EditorGUI.indentLevel--;
		}
				
		// 360 Cubemap
		if (_sourceType == SourceType.Camera360)
		{
			GUILayout.Space(8f);
			EditorGUILayout.LabelField("360 Cubemap", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;

			AVProMovieCaptureBase.CubemapResolution cubemapEnum = (AVProMovieCaptureBase.CubemapResolution)_cubemapResolution;
			_cubemapResolution = (int)((AVProMovieCaptureBase.CubemapResolution)EditorGUILayout.EnumPopup("Resolution", cubemapEnum));

			AVProMovieCaptureBase.CubemapDepth depthEnum = (AVProMovieCaptureBase.CubemapDepth)_cubemapDepth;
			_cubemapDepth = (int)((AVProMovieCaptureBase.CubemapDepth)EditorGUILayout.EnumPopup("Depth", depthEnum));
			EditorGUI.indentLevel--;
		}
			
		
		GUILayout.Space(8f);
		if (GUILayout.Button("Reset All Settings"))
		{
			ResetSettings();
		}
		GUILayout.Space(4f);
		
		//EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
	}

	private void DrawConfigGUI_Output()
	{
		//GUILayout.Label("Target", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		//EditorGUI.indentLevel++;

		// File path
		EditorGUILayout.LabelField("File Path", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;
		_outputFolderIndex = EditorGUILayout.Popup("Relative to", _outputFolderIndex, _outputFolders);
		if (_outputFolderIndex == 0 || _outputFolderIndex == 1)
		{
			_outputFolderRelative = EditorGUILayout.TextField("SubFolder(s)", _outputFolderRelative);
		}
		else
		{
			EditorGUILayout.BeginHorizontal();
			_outputFolderAbsolute = EditorGUILayout.TextField("Path", _outputFolderAbsolute);
			if (GUILayout.Button(">", GUILayout.Width(22)))
			{
				_outputFolderAbsolute = EditorUtility.SaveFolderPanel("Select Folder To Store Video Captures", System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../")), "");
				EditorUtility.SetDirty(this);
			}
			EditorGUILayout.EndHorizontal();
		}
		EditorGUI.indentLevel--;

		GUILayout.Space(8f);
		
		// File name
		EditorGUILayout.LabelField("File Name", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;
		_autoFilenamePrefixFromSceneName = EditorGUILayout.Toggle("From Scene Name", _autoFilenamePrefixFromSceneName);
		if (_autoFilenamePrefixFromSceneName)
		{
#if AVPRO_MOVIECAPTURE_SCENEMANAGER_53
			string currentScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
#else
			string currentScenePath = EditorApplication.currentScene;
#endif
			_autoFilenamePrefix = System.IO.Path.GetFileNameWithoutExtension(currentScenePath);
			if (string.IsNullOrEmpty(_autoFilenamePrefix))
			{
				_autoFilenamePrefix = "capture";
			}
		}
		EditorGUI.BeginDisabledGroup(_autoFilenamePrefixFromSceneName);
		_autoFilenamePrefix = EditorGUILayout.TextField("Prefix", _autoFilenamePrefix);
		EditorGUI.EndDisabledGroup();
		_autoFilenameExtension = EditorGUILayout.TextField("Extension", _autoFilenameExtension);
		_appendTimestamp = EditorGUILayout.Toggle("Append Timestamp", _appendTimestamp);
		EditorGUI.indentLevel--;



		//EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
	}
		
	private void DrawConfigGUI_Visual()
	{
		//GUILayout.Label("Video", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		//EditorGUI.indentLevel++;

		{
			Vector2 outSize = Vector2.zero;
			if (_sourceType == SourceType.Scene)
			{
				// We can't just use Screen.width and Screen.height because Unity returns the size of this window
				// So instead we look for a camera with no texture target and a valid viewport
				int inWidth = 1;
				int inHeight = 1;
				foreach (Camera cam in Camera.allCameras)
				{
					if (cam.targetTexture == null)
					{
						float rectWidth = Mathf.Clamp01(cam.rect.width + cam.rect.x) - Mathf.Clamp01(cam.rect.x);
						float rectHeight = Mathf.Clamp01(cam.rect.height + cam.rect.y) - Mathf.Clamp01(cam.rect.y);
						if (rectWidth > 0.0f && rectHeight > 0.0f)
						{
							inWidth = Mathf.FloorToInt(cam.pixelWidth / rectWidth);
							inHeight = Mathf.FloorToInt(cam.pixelHeight / rectHeight);
							//Debug.Log (rectWidth + "    " + (cam.rect.height - cam.rect.y) + " " + cam.pixelHeight + " = " + inWidth + "x" + inHeight);
							break;
						}
					}
				}
				outSize = AVProMovieCaptureBase.GetRecordingResolution(inWidth, inHeight, GetDownScaleFromIndex(_downScaleIndex), new Vector2(_downscaleX, _downscaleY));
			}
			else
			{
				if (_cameraNode != null)
				{
					int inWidth = Mathf.FloorToInt(_cameraNode.pixelRect.width);
					int inHeight = Mathf.FloorToInt(_cameraNode.pixelRect.height);

					if (_renderResolution != AVProMovieCaptureBase.Resolution.Original)
					{
						float rectWidth = Mathf.Clamp01(_cameraNode.rect.width + _cameraNode.rect.x) - Mathf.Clamp01(_cameraNode.rect.x);
						float rectHeight = Mathf.Clamp01(_cameraNode.rect.height + _cameraNode.rect.y) - Mathf.Clamp01(_cameraNode.rect.y);

						if (_renderResolution == AVProMovieCaptureBase.Resolution.Custom)
						{
							inWidth = _renderWidth;
							inHeight = _renderHeight;
						}
						else
						{
							AVProMovieCaptureBase.GetResolution(_renderResolution, ref inWidth, ref inHeight);
							inWidth = Mathf.FloorToInt(inWidth * rectWidth);
							inHeight = Mathf.FloorToInt(inHeight * rectHeight);
						}
					}

					outSize = AVProMovieCaptureBase.GetRecordingResolution(inWidth, inHeight, GetDownScaleFromIndex(_downScaleIndex), new Vector2(_downscaleX, _downscaleY));
				}
			}

			GUILayout.Space(8f);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUI.color = Color.cyan;
			GUILayout.TextArea("Output: " + (int)outSize.x + " x " + (int)outSize.y + " @ " + (int)_frameRate);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(8f);
			GUI.color = Color.white;

		}

		_downScaleIndex = EditorGUILayout.Popup("Down Scale", _downScaleIndex, _downScales);
		if (_downScaleIndex == 5)
		{
			Vector2 maxVideoSize = new Vector2(_downscaleX, _downscaleY);
			maxVideoSize = EditorGUILayout.Vector2Field("Size", maxVideoSize);
			_downscaleX = Mathf.Clamp((int)maxVideoSize.x, 1, AVProMovieCapturePlugin.MaxRenderWidth);
			_downscaleY = Mathf.Clamp((int)maxVideoSize.y, 1, AVProMovieCapturePlugin.MaxRenderHeight);
		}

		_frameRate = (AVProMovieCaptureBase.FrameRate)EditorGUILayout.EnumPopup("Frame Rate", _frameRate);

		GUILayout.Space(8f);
		if (ShowConfigList("Codec", _videoCodecNames, _videoCodecConfigurable, ref _videoCodecIndex))
		{
			_queueConfigureVideoCodec = Mathf.Max(-1, _videoCodecIndex - 2);
		}


		if (_videoCodecIndex > 0 && (
				_videoCodecNames[_videoCodecIndex].EndsWith("Cinepak Codec by Radius")
				|| _videoCodecNames[_videoCodecIndex].EndsWith("DV Video Encoder")
				|| _videoCodecNames[_videoCodecIndex].EndsWith("Microsoft Video 1")
				|| _videoCodecNames[_videoCodecIndex].EndsWith("Microsoft RLE")
				|| _videoCodecNames[_videoCodecIndex].EndsWith("Logitech Video (I420)")
				|| _videoCodecNames[_videoCodecIndex].EndsWith("Intel IYUV codec")
				))
		{
			GUI.color = Color.yellow;
			GUILayout.TextArea("Warning: Legacy codec, not recommended");
			GUI.color = Color.white;
		}
		if (_videoCodecIndex >= 0 && _videoCodecNames[_videoCodecIndex].Contains("Decoder"))
		{
			GUI.color = Color.yellow;
			GUILayout.TextArea("Warning: Codec may contain decompressor only");
			GUI.color = Color.white;
		}

		if (_videoCodecIndex >= 0 && _videoCodecNames[_videoCodecIndex].EndsWith("Uncompressed"))
		{
			GUI.color = Color.yellow;
			GUILayout.TextArea("Warning: May result in very large files");
			GUI.color = Color.white;
		}
		if (_videoCodecNames.Length < 8)
		{
			GUI.color = Color.cyan;
			EditorGUILayout.TextArea("Low number of codecs, consider installing more");
			GUI.color = Color.white;
		}
		
		DrawConfigGUI_MotionBlur();

		//EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
	}

	private void DrawConfigGUI_MotionBlur()
	{	
		EditorGUI.BeginDisabledGroup(_captureModeIndex == 0);
		//EditorGUILayout.BeginVertical("box");
		//EditorGUI.indentLevel++;
	
		GUILayout.Space(8f);			
		GUILayout.Label("Motion Blur (beta)", EditorStyles.boldLabel);
		//EditorGUILayout.BeginVertical("box");
		//EditorGUI.indentLevel++;

		if (_captureModeIndex == 0)
		{
			GUI.color = Color.yellow;
			EditorGUILayout.TextArea("Motion Blur only available in offline mode");
			GUI.color = Color.white;
		}
		
		_useMotionBlur = EditorGUILayout.Toggle("Use Motion Blur", _useMotionBlur);
		EditorGUI.BeginDisabledGroup(!_useMotionBlur);
		EditorGUILayout.PrefixLabel("Samples");
		EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
		//EditorGUILayout.LabelField("moo", GUILayout.ExpandWidth(false));
		_motionBlurSampleCount = EditorGUILayout.IntSlider(_motionBlurSampleCount, 0, 64);
		EditorGUILayout.EndHorizontal();
		EditorGUI.EndDisabledGroup();

		//EditorGUI.indentLevel--;
		//EditorGUILayout.EndVertical();
		EditorGUI.EndDisabledGroup();
	}
	
	private void DrawConfigGUI_Audio()
	{
		EditorGUI.BeginDisabledGroup(_captureModeIndex != 0);
		//GUILayout.Label("Audio", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical("box");
		//EditorGUI.indentLevel++;

		if (_captureModeIndex != 0)
		{

			GUI.color = Color.yellow;
			EditorGUILayout.TextArea("Audio capture not available in offline mode");
			GUI.color = Color.white;
		}

		_captureAudio = EditorGUILayout.Toggle("Capture Audio", _captureAudio);
		
		GUILayout.Space(8f);
		EditorGUI.BeginDisabledGroup(!_captureAudio);
		if (ShowConfigList("Source", _audioDeviceNames, null, ref _audioDeviceIndex, false))
		{
		}
		
		GUILayout.Space(8f);
		if (ShowConfigList("Codec", _audioCodecNames, _audioCodecConfigurable, ref _audioCodecIndex))
		{
			_queueConfigureAudioCodec = Mathf.Max(-1, _audioCodecIndex - 2);
		}

		if (_audioCodecIndex > 0 && (_audioCodecNames[_audioCodecIndex].EndsWith("MPEG Layer-3")))
		{
			GUI.color = Color.yellow;
			GUILayout.TextArea("Warning: We have had reports that this codec doesn't work. Consider using a different coddec");
			GUI.color = Color.white;
		}
		EditorGUI.EndDisabledGroup();

		//EditorGUI.indentLevel--;
		EditorGUILayout.EndVertical();
		EditorGUI.EndDisabledGroup();
	}
}