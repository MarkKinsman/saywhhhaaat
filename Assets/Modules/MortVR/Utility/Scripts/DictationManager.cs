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
    string s3Location = "https://j7m0zw3qec.execute-api.us-east-1.amazonaws.com/prod/chris2.csv";

    [Space(4)]
    [Header("User Settings")]
    [SerializeField]
    string userName;

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
    private GameObject screenshotCamera;
    RenderTexture screenShotRenderTexture;
    int resWidth = 1920;
    int resHeight = 1080;
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
        CreateCamera();

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

    void CreateCamera()
    {
        screenshotCamera = new GameObject();
        screenshotCamera.AddComponent<Camera>();
        screenshotCamera.transform.SetParent(mouthCollider.transform);
        screenshotCamera.transform.localRotation = Quaternion.identity;
        screenshotCamera.transform.localPosition = new Vector3(0f, 1f, 0f);

        Camera cam = screenshotCamera.GetComponent<Camera>();
        cam.fieldOfView = 53f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100;


        screenShotRenderTexture = new RenderTexture(resWidth, resHeight, 24);
        screenshotCamera.GetComponent<Camera>().targetTexture = screenShotRenderTexture;

    }

    byte[] TakePicture()
    {
        RenderTexture currentActiveRenderTexture = RenderTexture.active;
        RenderTexture.active = screenShotRenderTexture;

        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenShot.Apply();
        byte[] bytes = screenShot.EncodeToPNG();

        if (!Directory.Exists(string.Format("{0}./_Dictation", Application.dataPath)))
        {
            Directory.CreateDirectory(string.Format("{0}./_Dictations", Application.dataPath));

        }

        string filename = string.Format("{0}/_Dictations/screen_{1}.png", Application.dataPath, System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", filename));

        UnityEngine.Object.Destroy(screenShot);
        RenderTexture.active = currentActiveRenderTexture;

        return bytes;
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
                    record = true;
                }
            }

            Debug.Log("Recording.");
        }
    }

    void StopRecord(object sender, ControllerInteractionEventArgs e)
    {
        if (record)
        { 
            byte[] picture = TakePicture();
            Microphone.End(null); //Stop the audio recording
            AudioClip newClip = TrimClip(goAudioSource.clip, Time.time - timeStart);
            byte[] wavFile = WavUtility.FromAudioClip(newClip, out filepath, true, "_Dictations");
            Debug.Log("Stop Recording.");

            WWWForm positionForm = new WWWForm();
            positionForm.AddField("X", mouthCollider.transform.position.x.ToString());
            positionForm.AddField("Y", mouthCollider.transform.position.y.ToString());
            positionForm.AddField("Z", mouthCollider.transform.position.z.ToString());
            positionForm.AddField("UserName", userName);
            PostDictation(s3Location, positionForm);

            WWWForm imageForm = new WWWForm();
            Dictionary<string, string> imageHeaders = imageForm.headers;
            imageHeaders["Type"] = "PNG";
            //PostDictation(s3Location, picture, imageHeaders);

            WWWForm audioForm = new WWWForm();
            Dictionary<string, string> audioHeaders = audioForm.headers;
            audioHeaders["Type"] = "WAV";
            //PostDictation(s3Location, wavFile, audioHeaders);

        }
    }

    void PostDictation(string url, WWWForm form)
    {
        WWW www = new WWW(url, form);

        StartCoroutine(WaitForRequest(www));
    }

    void PostDictation(string url, byte[] rawdata, Dictionary<string, string> headers)
    {
        WWW www = new WWW(url, rawdata, headers);

        StartCoroutine(WaitForRequest(www));
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
