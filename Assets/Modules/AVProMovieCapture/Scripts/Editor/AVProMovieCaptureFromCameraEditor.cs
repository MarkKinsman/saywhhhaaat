using UnityEngine;
using UnityEditor;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

[CustomEditor(typeof(AVProMovieCaptureFromCamera))]
public class AVProMovieCaptureFromCameraEditor : Editor
{
	private AVProMovieCaptureFromCamera _capture;
	
	public override void OnInspectorGUI()
	{
		_capture = (this.target) as AVProMovieCaptureFromCamera;
		
		DrawDefaultInspector();
				
		GUILayout.Space(8.0f);
		
		if (Application.isPlaying)
		{		
			if (!_capture.IsCapturing())
			{
				GUI.backgroundColor = Color.green;
		   		if (GUILayout.Button("Start Recording"))
				{
					_capture.SelectCodec(false);
					_capture.SelectAudioDevice(false);
					// We have to queue the start capture otherwise Screen.width and height aren't correct
					_capture.QueueStartCapture();
					GUI.backgroundColor = Color.white;
				}
			}
			else
			{				
				GUILayout.BeginHorizontal();
				if (_capture._fps > 0f)
				{
					Color originalColor = GUI.color;
					float fpsDelta = (_capture._fps - (int)_capture._frameRate);
					GUI.color = Color.red;
					if (fpsDelta > -10)
						GUI.color = Color.yellow;
					if (fpsDelta > -2)
						GUI.color = Color.green;
					GUILayout.Label("Recording at " + _capture._fps.ToString("F1") + " fps");
					
					GUI.color = originalColor;
				}
				else
				{
					GUILayout.Label("Recording at ... fps");	
				}
					
				if (!_capture.IsPaused())
				{
					GUI.backgroundColor = Color.yellow;
					if (GUILayout.Button("Pause Capture"))
					{
						_capture.PauseCapture();
					}
				}
				else
				{
					GUI.backgroundColor = Color.green;
					if (GUILayout.Button("Resume Capture"))
					{
						_capture.ResumeCapture();
					}					
				}
				GUI.backgroundColor = Color.cyan;
				if (GUILayout.Button("Cancel"))
				{
					_capture.CancelCapture();
				}
				GUI.backgroundColor = Color.red;
		   		if (GUILayout.Button("Stop Recording"))
				{
					_capture.StopCapture();
				}				
				GUILayout.EndHorizontal();

				GUI.backgroundColor = Color.white;

				GUILayout.Space(8.0f);
				GUILayout.Label("Recording at: " + _capture.GetRecordingWidth() + "x" + _capture.GetRecordingHeight() + " @ " + ((int)_capture._frameRate).ToString() + "fps");
				GUILayout.Space(8.0f);
				GUILayout.Label("Using video codec: '" + _capture._codecName + "'");
				GUILayout.Label("Using audio device: '" + _capture._audioDeviceName + "'");
			}	
		}
	}
}