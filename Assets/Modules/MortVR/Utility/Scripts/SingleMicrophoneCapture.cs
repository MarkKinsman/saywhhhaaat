using UnityEngine; //41 Post - Created by DimasTheDriver on July/28/2012 . Part of the 'Unity: Capturing audio from a microphone' post. Available at: http://www.41post.com/?p=4884
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof (AudioSource))]

public class SingleMicrophoneCapture : MonoBehaviour 
{
	//A boolean that flags whether there's a connected microphone
	private bool micConnected = false;

    private string filenametest;
	//The maximum and minimum available recording frequencies
	private int minFreq;
	private int maxFreq;
	
	//A handle to the attached AudioSource
	private AudioSource goAudioSource;
	
	//Use this for initialization
	void Start() 
	{
		//Check if there is at least one microphone connected
		if(Microphone.devices.Length <= 0)
		{
			//Throw a warning message at the console if there isn't
			Debug.LogWarning("Microphone not connected!");
		}
		else //At least one microphone is present
		{
			//Set 'micConnected' to true
			micConnected = true;
			
			//Get the default microphone recording capabilities
			Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
			
			//According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...
			if(minFreq == 0 && maxFreq == 0)
			{
				//...meaning 44100 Hz can be used as the recording sampling rate
				maxFreq = 44100;
			}
			
			//Get the attached AudioSource component
			goAudioSource = this.GetComponent<AudioSource>();
		}
	}
	
	void OnGUI() 
	{
		//If there is a microphone
		if(micConnected)
		{
			//If the audio from any microphone isn't being recorded
			if(!Microphone.IsRecording(null))
			{
				//Case the 'Record' button gets pressed
				if(GUI.Button(new Rect(Screen.width/2-100, Screen.height/2-25, 200, 50), "Record"))
				{
					//Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource
					goAudioSource.clip = Microphone.Start(null, true, 20, maxFreq);
				}
			}
			else //Recording is in progress
			{
				//Case the 'Stop and Play' button gets pressed
				if(GUI.Button(new Rect(Screen.width/2-100, Screen.height/2-25, 200, 50), "Stop and Play!"))
				{
					Microphone.End(null); //Stop the audio recording
                                          //goAudioSource.Play(); //Playback the recorded audio
                    AudioClip newClip = TrimSilence(goAudioSource.clip, 0);
                    WavUtility.FromAudioClip(newClip, out filenametest, true);
				}
				
				GUI.Label(new Rect(Screen.width/2-100, Screen.height/2+25, 200, 50), "Recording in progress...");
			}
		}
		else // No microphone
		{
			//Print a red "Microphone not connected!" message at the center of the screen
			GUI.contentColor = Color.red;
			GUI.Label(new Rect(Screen.width/2-100, Screen.height/2-25, 200, 50), "Microphone not connected!");
		}
	}

    public static AudioClip TrimSilence(AudioClip clip, float min)
    {
        var samples = new float[clip.samples];

        clip.GetData(samples, 0);

        return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
    }

    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
    {
        return TrimSilence(samples, min, channels, hz, false, false);
    }

    public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream)
    {
        int i;

        for (i = 0; i < samples.Count; i++)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }

        samples.RemoveRange(0, i);

        for (i = samples.Count - 1; i > 0; i--)
        {
            if (Mathf.Abs(samples[i]) > min)
            {
                break;
            }
        }

        samples.RemoveRange(i, samples.Count - i);

        var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);

        clip.SetData(samples.ToArray(), 0);

        return clip;
    }
}
