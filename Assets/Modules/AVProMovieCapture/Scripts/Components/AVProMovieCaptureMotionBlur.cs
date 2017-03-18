using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2012-2016 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

[RequireComponent(typeof(Camera))]
public class AVProMovieCaptureMotionBlur : MonoBehaviour 
{
	public RenderTextureFormat _format = RenderTextureFormat.ARGBFloat;
	private int _numSamples = 16;
	private RenderTexture _accum;
	private RenderTexture _lastComp;
	private Material _addMaterial;
	private Material _divMaterial;
	private int _frameCount;

	public bool IsFrameAccumulated
	{
		get;
		private set;
	}

	public int NumSamples
	{
		get { return _numSamples; }
		set { _numSamples = value; OnNumSamplesChanged(); }
	}

	public int FrameCount
	{
		get { return _frameCount; }
	}

	void Start()
	{
		Shader addShader = Resources.Load<Shader>("AVProMovieCapture_MotionBlur_Add");
		Shader divShader = Resources.Load<Shader>("AVProMovieCapture_MotionBlur_Div");
		_addMaterial = new Material(addShader);
		_divMaterial = new Material(divShader);

		_accum = new RenderTexture(Screen.width, Screen.height, 0, _format, RenderTextureReadWrite.Default);
		_lastComp = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);

		OnNumSamplesChanged();
	}

	void OnEnable()
	{
		_frameCount = 0;
		IsFrameAccumulated = false;

		RenderTexture prev = RenderTexture.active;
		RenderTexture.active = _accum;
		GL.Clear(false, true, Color.black);
		RenderTexture.active = prev;
	}

	void OnDestroy()
	{
		if (_addMaterial != null)
		{
			Material.Destroy(_addMaterial);
			_addMaterial = null;
		}
		if (_divMaterial != null)
		{
			Material.Destroy(_divMaterial);
			_divMaterial = null;
		}
		
		if (_accum != null)
		{
			RenderTexture.Destroy(_accum);
			_accum = null;
		}
		if (_lastComp != null)
		{
			RenderTexture.Destroy(_lastComp);
			_lastComp = null;
		}
	}
	
	public void OnNumSamplesChanged()
	{
		//Time.captureFramerate = 30 * _numSamples;
		if (_divMaterial != null)
		{
			_divMaterial.SetFloat("_NumSamples", _numSamples);
			_addMaterial.SetFloat("_Weight", 1f);
		}
	}

	private float LerpUnclamped(float a, float b, float t)
	{
		return a + ((b - a) * t);
	}

	public float _bias = 1f;
	private float _total = 0f;
	
	private void ApplyWeighting()
	{
		// Apply some frame weighting so the newer frames have the most contribution
		// Not sure this is better than non-weighted averaging.
		float weight = ((float)_frameCount / (float)_numSamples);
		weight = Mathf.Pow(weight, 2f);
		_total += weight;
		float numSamples = ((float)_numSamples / 2f) + 0.5f;
		numSamples = _total;
		weight = LerpUnclamped(weight, 1f, _bias);
		numSamples = LerpUnclamped(numSamples, _numSamples, _bias);
		_addMaterial.SetFloat("_Weight", weight);
		_divMaterial.SetFloat("_NumSamples", numSamples);	
	}
	
	void OnRenderImage(RenderTexture src, RenderTexture dst)
	{
		_frameCount++;

		//AppleWeighting();

		Graphics.Blit(src, _accum, _addMaterial);
		
		if (_frameCount >= _numSamples)
		{
			//Debug.Log("numSamples " + numSamples);
			//Debug.Log("_total " + _total);
			//Graphics.Blit(_accum, _lastComp);
			Graphics.Blit(_accum, _lastComp, _divMaterial);

			RenderTexture prev = RenderTexture.active;
			RenderTexture.active = _accum;
			GL.Clear(false, true, Color.black);
			RenderTexture.active = prev;

			//Graphics.Blit(src, _accum, _addMaterial);
			//Graphics.Blit(_lastComp, _accum, _divMaterial);
			IsFrameAccumulated = true;
			_frameCount = 0;
			_total = 0f;
		}
		else
		{
			IsFrameAccumulated = false;
		}

		Graphics.Blit(_lastComp, dst);//, _divMaterial);
	}
	
	/*void OnGUI()
	{
		GUILayout.Label("Real (slow) Motion Blur Demo");
		GUILayout.BeginHorizontal();
		GUILayout.Label("Samples");
		int numSamples = (int)GUILayout.HorizontalSlider(_numSamples, 1, 64, GUILayout.Width(128f));
		if (numSamples != _numSamples)
		{
			_numSamples = numSamples;
			OnNumSamplesChanged();
		}
		GUILayout.Label(_numSamples.ToString());
		GUILayout.EndHorizontal();
	}*/
}