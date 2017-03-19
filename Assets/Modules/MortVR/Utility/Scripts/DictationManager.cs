using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using VRTK;
using System.Text;

public class DictationManager : MonoBehaviour {

    #region private variables
    //Inspector variables
    [Header("Dictation Button")]
    [SerializeField]
    MortVR.HandSide dictationHandSide = MortVR.HandSide.Left;
    [SerializeField]
    VRTK_ControllerEvents.ButtonAlias dictationAlias = VRTK_ControllerEvents.ButtonAlias.Application_Menu;

    [Space(4)]
    [Header("Web Settings")]
    [SerializeField]
    string projectName = "UWMC";
    [SerializeField]
    string s3Location = "https://j7m0zw3qec.execute-api.us-east-1.amazonaws.com/prod/chris2";

    //Controller actions
    GameObject cameraRig;
    GameObject mouthCollider;
    GameObject micLocation;

    //A boolean that flags whether there's a connected microphone
    private bool micConnected = false;
    //The maximum and minimum available recording frequencies
    private int minFreq;
    private int maxFreq;
    //A handle to the attached AudioSource
    private AudioSource goAudioSource;
    private string filepath;

    private bool record = false;
    private float timeStart;



    #endregion

    #region unity callbacks
    void Start()
    {
        //Wait for prefabs to be spawned and capture them;
        StartCoroutine(InitializeControls());
        StartMicrophone();
    }
    #endregion

    #region private functions

    IEnumerator InitializeControls()
    {
        while (GameObject.Find("[MortVRCameraRig](Clone)") == null)
        {
            yield return null;
        }
        cameraRig = GameObject.Find("[MortVRCameraRig](Clone)");

        CreateColliders();

        SetDictationButton(dictationHandSide, dictationAlias);
        print("Found camera rig.");
    }

    private void StartMicrophone()
    {
        //Check if there is at least one microphone connected
        if (Microphone.devices.Length <= 0)
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
            if (minFreq == 0 && maxFreq == 0)
            {
                //...meaning 44100 Hz can be used as the recording sampling rate
                maxFreq = 44100;
            }

            //Get the attached AudioSource component
            goAudioSource = this.GetComponent<AudioSource>();
        }
    }

    GameObject GetController(MortVR.HandSide handSide)
    {
        switch (handSide)
        {
            case MortVR.HandSide.Left:
                if (cameraRig.transform.GetChild(0).gameObject.GetComponent<VRTK_ControllerEvents>() != null)
                {
                    return cameraRig.transform.GetChild(0).gameObject;
                }
                else
                {
                    Debug.LogError("Could not find left hand");
                    return null;
                }
            case MortVR.HandSide.Right:
                if (cameraRig.transform.GetChild(1).gameObject.GetComponent<VRTK_ControllerEvents>() != null)
                {
                    return cameraRig.transform.GetChild(1).gameObject;
                }
                else
                {
                    Debug.LogError("Could not find right hand");
                    return null;
                }
            default:
                Debug.LogError("No hand assigned");
                return null;
        }
    }

    void SetDictationButton(MortVR.HandSide handSide, VRTK_ControllerEvents.ButtonAlias buttonAlias)
    {
        GameObject hand = GetController(handSide);

        switch (buttonAlias)
        {
            case VRTK_ControllerEvents.ButtonAlias.Application_Menu:
                hand.GetComponent<VRTK_ControllerEvents>().ApplicationMenuPressed +=
                    new ControllerInteractionEventHandler(Record);
                hand.GetComponent<VRTK_ControllerEvents>().ApplicationMenuReleased +=
                    new ControllerInteractionEventHandler(StopRecord);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Grip:
                hand.GetComponent<VRTK_ControllerEvents>().GripPressed +=
                   new ControllerInteractionEventHandler(Record);
                hand.GetComponent<VRTK_ControllerEvents>().GripReleased +=
                   new ControllerInteractionEventHandler(StopRecord);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Touchpad_Press:
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadPressed +=
                   new ControllerInteractionEventHandler(Record);
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadReleased +=
                    new ControllerInteractionEventHandler(StopRecord);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Touchpad_Touch:
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadTouchStart +=
                   new ControllerInteractionEventHandler(Record);
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadTouchEnd +=
                   new ControllerInteractionEventHandler(StopRecord);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Click:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerClicked +=
                   new ControllerInteractionEventHandler(Record);
                hand.GetComponent<VRTK_ControllerEvents>().TriggerReleased +=
                   new ControllerInteractionEventHandler(StopRecord);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Hairline:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerHairlineStart +=
                   new ControllerInteractionEventHandler(Record);
                hand.GetComponent<VRTK_ControllerEvents>().TriggerHairlineEnd +=
                   new ControllerInteractionEventHandler(StopRecord);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Press:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerPressed +=
                   new ControllerInteractionEventHandler(Record);
                hand.GetComponent<VRTK_ControllerEvents>().TriggerReleased +=
                   new ControllerInteractionEventHandler(StopRecord);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Touch:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerTouchStart +=
                   new ControllerInteractionEventHandler(Record);
                hand.GetComponent<VRTK_ControllerEvents>().TriggerTouchEnd +=
                   new ControllerInteractionEventHandler(StopRecord);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Undefined:
                Debug.LogError("You need to put a VRTK_ControllerEvents script on your SteamVR Controllers");
                break;
            default:
                Debug.LogError("No button alias assigned");
                break;
        }
    }

    void CreateColliders()
    {
        mouthCollider = new GameObject();
        mouthCollider.transform.SetParent(cameraRig.transform.GetChild(2).GetChild(2));
        mouthCollider.transform.localPosition = new Vector3(0f, -0.79f, 1.03f);
        mouthCollider.transform.localRotation = Quaternion.identity;
        mouthCollider.transform.localScale = new Vector3(1,1,1);
        mouthCollider.transform.name = "Mouth Collider";

        SphereCollider col = mouthCollider.AddComponent<SphereCollider>();
        col.radius = .6f;

        micLocation = new GameObject();
        micLocation.transform.SetParent(GetController(dictationHandSide).transform);
        micLocation.transform.localPosition = new Vector3(0f, -0.0593f, 0.0288f);
        micLocation.transform.name = "Mic Location";
    }

    static string SaveDictation(int width, int height)
    {
        if (!Directory.Exists(string.Format("{0}./screenshots", Application.dataPath)))
        {
            Directory.CreateDirectory(string.Format("{0}./screenshots", Application.dataPath));

        }

        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    void Record(object sender, ControllerInteractionEventArgs e)
    {
        if (mouthCollider.GetComponent<SphereCollider>().bounds.Contains(micLocation.transform.position))
        {
            if(micConnected)
            {
                if(!Microphone.IsRecording(null))
                {
                    goAudioSource.clip = Microphone.Start(null, true, 120, maxFreq);
                    timeStart = Time.time;
                }
            }
            record = true;
            Debug.Log("Recording.");
        }
    }

    void StopRecord(object sender, ControllerInteractionEventArgs e)
    {
        Microphone.End(null); //Stop the audio recording
        AudioClip newClip = TrimClip(goAudioSource.clip, Time.time - timeStart);
        byte[] wavFile = WavUtility.FromAudioClip(newClip, out filepath, true);
        Debug.Log("Stop Recording.");

        Dictation comment = new Dictation();
        comment.setPosition(mouthCollider.transform.position);
        comment.audio = wavFile;
        PostDictation(s3Location, comment);
    }

    void PostDictation(string url, Dictation comment)
    {
        string json = JsonUtility.ToJson(comment);

        //Dictionary<string, string> headers = new Dictionary<string, string>();
        //headers.Add("Content-Type", "application/json");
        //headers.Add("Cookie", "Our session cookie");

        //byte[] pData = Encoding.ASCII.GetBytes(json.ToCharArray());

        WWWForm form = new WWWForm();

        form.AddField("X", "10");
        form.AddField("Y", "11");
        form.AddField("Z", "12");
        form.AddField("Name", "Marc");

        //WWW www = new WWW(url, pData, headers);
        WWW www = new WWW(url, form);

        StartCoroutine(WaitForRequest(www));
    }

    void GetURL()
    {

    }

    IEnumerator WaitForRequest(WWW www)
    {
        yield return www;

         // check for errors
        if (www.error == null)
        {
            Debug.Log("WWW Ok!: " + www.text);
        }
        else
        {
            Debug.Log("WWW Error: " + www.error);
        }
    }

    public AudioClip TrimClip(AudioClip clip, float duration)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);
        Array.Resize(ref samples, (int)(duration * clip.frequency));

        AudioClip trimmedClip = AudioClip.Create("trimmedClip", samples.Length, clip.channels, clip.frequency, false);
        trimmedClip.SetData(samples, 0);

        return trimmedClip;
    }
    #endregion
}
