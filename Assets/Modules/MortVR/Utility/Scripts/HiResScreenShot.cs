// Script for taking high res screen shots
// Attach to camera that takes the pictures

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using VRTK;

public class HiResScreenShot : MonoBehaviour {

    #region private variables
    //Inspector variables
    [Header("Camera Placement")]
    [SerializeField]
    MortVR.HandSide placeCameraHandSide = MortVR.HandSide.Left;
    [SerializeField]
    VRTK_ControllerEvents.ButtonAlias placeCameraAlias = VRTK_ControllerEvents.ButtonAlias.Application_Menu;

    [Space(4)]
    [Header("Camera View Finder")]
    [SerializeField]
    MortVR.HandSide cameraViewFinderHandSide = MortVR.HandSide.Left;
    [SerializeField]
    VRTK_ControllerEvents.ButtonAlias cameraViewFinderAlias = VRTK_ControllerEvents.ButtonAlias.Grip;

    [Space(4)]
    [Header("Screenshot")]
    [SerializeField]
    int resWidth = 3840;
    [SerializeField]
    int resHeight = 2160;
    [SerializeField]
    MortVR.HandSide takeScreenshotHandSide = MortVR.HandSide.Right;
    [SerializeField]
    VRTK_ControllerEvents.ButtonAlias takeScreenshotAlias = VRTK_ControllerEvents.ButtonAlias.Application_Menu;

    //Controller actions
    GameObject cameraRig;
    GameObject leftHand;
    GameObject rightHand;

    //Screen Shot
    Material viewFinderMaterial;
    Camera screenShotCamera;
    GameObject cameraViewFinder;
    RenderTexture screenShotRenderTexture;

    private bool takeHiResShot = false;

    #endregion

    #region unity callbacks

    void Awake()
    {
        screenShotCamera = gameObject.GetComponent<Camera>();
    }

	void Start ()
    {
        StartCoroutine(InitializeControls());
    }

    void LateUpdate()
    {
        if (takeHiResShot)
        {
            RenderTexture currentActiveRenderTexture = RenderTexture.active;
            RenderTexture.active = screenShotRenderTexture;

            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            screenShot.Apply();
            byte[] bytes = screenShot.EncodeToPNG();
            
            string filename = ScreenShotName(resWidth, resHeight);
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));

            takeHiResShot = false;
            UnityEngine.Object.Destroy(screenShot);
            RenderTexture.active = currentActiveRenderTexture;
        }
    }
    #endregion

    #region public functions



    #endregion

    #region private functions

    IEnumerator InitializeControls()
    {
        while (GameObject.Find("[MortVRCameraRig](Clone)") == null)
        {
            yield return null;
        }
        cameraRig = GameObject.Find("[MortVRCameraRig](Clone)");

        CreateViewFinder();

        SetPlaceCameraButton(placeCameraHandSide, placeCameraAlias);
        SetCameraViewFinderButton(cameraViewFinderHandSide, cameraViewFinderAlias);
        SetTakePictureButton(takeScreenshotHandSide, takeScreenshotAlias);
        print("Found camera rig.");
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

    void SetPlaceCameraButton(MortVR.HandSide handSide, VRTK_ControllerEvents.ButtonAlias buttonAlias)
    {
        GameObject hand = GetController(handSide);

        switch(buttonAlias)
        {
            case VRTK_ControllerEvents.ButtonAlias.Application_Menu:
                hand.GetComponent<VRTK_ControllerEvents>().ApplicationMenuPressed +=
                    new ControllerInteractionEventHandler(PlaceCamera);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Grip:
                hand.GetComponent<VRTK_ControllerEvents>().GripPressed +=
                   new ControllerInteractionEventHandler(PlaceCamera);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Touchpad_Press:
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadPressed +=
                   new ControllerInteractionEventHandler(PlaceCamera);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Touchpad_Touch:
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadTouchStart +=
                   new ControllerInteractionEventHandler(PlaceCamera);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Click:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerClicked +=
                   new ControllerInteractionEventHandler(PlaceCamera);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Hairline:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerHairlineStart +=
                   new ControllerInteractionEventHandler(PlaceCamera);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Press:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerPressed +=
                   new ControllerInteractionEventHandler(PlaceCamera);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Touch:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerTouchStart +=
                   new ControllerInteractionEventHandler(PlaceCamera);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Undefined:
                Debug.LogError("You need to put a VRTK_ControllerEvents script on your SteamVR Controllers");
                break;
            default:
                Debug.LogError("No button alias assigned");
                break;
        }
    }

    void SetCameraViewFinderButton(MortVR.HandSide handSide, VRTK_ControllerEvents.ButtonAlias buttonAlias)
    {
        GameObject hand = GetController(handSide);

        switch (buttonAlias)
        {
            case VRTK_ControllerEvents.ButtonAlias.Application_Menu:
                hand.GetComponent<VRTK_ControllerEvents>().ApplicationMenuPressed +=
                    new ControllerInteractionEventHandler(ToggleViewFinder);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Grip:
                hand.GetComponent<VRTK_ControllerEvents>().GripPressed +=
                   new ControllerInteractionEventHandler(ToggleViewFinder);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Touchpad_Press:
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadPressed +=
                   new ControllerInteractionEventHandler(ToggleViewFinder);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Touchpad_Touch:
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadTouchStart +=
                   new ControllerInteractionEventHandler(ToggleViewFinder);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Click:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerClicked +=
                   new ControllerInteractionEventHandler(ToggleViewFinder);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Hairline:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerHairlineStart +=
                   new ControllerInteractionEventHandler(ToggleViewFinder);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Press:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerPressed +=
                   new ControllerInteractionEventHandler(ToggleViewFinder);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Touch:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerTouchStart +=
                   new ControllerInteractionEventHandler(ToggleViewFinder);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Undefined:
                Debug.LogError("You need to assign a button alias");
                break;
            default:
                Debug.LogError("You need to put a VRTK_ControllerEvents script on your SteamVR Controllers");
                break;
        }
    }

    void SetTakePictureButton(MortVR.HandSide handSide, VRTK_ControllerEvents.ButtonAlias buttonAlias)
    {
        GameObject hand = GetController(handSide);

        switch (buttonAlias)
        {
            case VRTK_ControllerEvents.ButtonAlias.Application_Menu:
                hand.GetComponent<VRTK_ControllerEvents>().ApplicationMenuPressed +=
                    new ControllerInteractionEventHandler(TakePicture);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Grip:
                hand.GetComponent<VRTK_ControllerEvents>().GripPressed +=
                   new ControllerInteractionEventHandler(TakePicture);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Touchpad_Press:
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadPressed +=
                   new ControllerInteractionEventHandler(TakePicture);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Touchpad_Touch:
                hand.GetComponent<VRTK_ControllerEvents>().TouchpadTouchStart +=
                   new ControllerInteractionEventHandler(TakePicture);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Click:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerClicked +=
                   new ControllerInteractionEventHandler(TakePicture);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Hairline:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerHairlineStart +=
                   new ControllerInteractionEventHandler(TakePicture);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Press:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerPressed +=
                   new ControllerInteractionEventHandler(TakePicture);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Trigger_Touch:
                hand.GetComponent<VRTK_ControllerEvents>().TriggerTouchStart +=
                   new ControllerInteractionEventHandler(TakePicture);
                break;
            case VRTK_ControllerEvents.ButtonAlias.Undefined:
                Debug.LogError("You need to assign a button alias");
                break;
            default:
                Debug.LogError("You need to put a VRTK_ControllerEvents script on your SteamVR Controllers");
                break;
        }
    }

    void CreateViewFinder()
    {
        screenShotRenderTexture = new RenderTexture(resWidth, resHeight, 24);
        screenShotCamera.targetTexture = screenShotRenderTexture;

        viewFinderMaterial = new Material(Shader.Find("Standard"));
        viewFinderMaterial.mainTexture = screenShotRenderTexture;

        cameraViewFinder = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cameraViewFinder.transform.parent = GetController(cameraViewFinderHandSide).transform;
        cameraViewFinder.name = "Screenshot View Finder";
        cameraViewFinder.transform.localPosition = new Vector3(0f, 0.07f, 0.07f);
        cameraViewFinder.transform.eulerAngles = new Vector3(30f, -90f, 0f);
        cameraViewFinder.transform.localScale = new Vector3(0.32f, 0.18f, .01f);
        Destroy(cameraViewFinder.GetComponent<MeshCollider>());
        cameraViewFinder.GetComponent<Renderer>().material = viewFinderMaterial;

        cameraViewFinder.SetActive(false);
    }

    static string ScreenShotName(int width, int height)
    {
        if (!Directory.Exists(string.Format("{0}./screenshots",Application.dataPath)))
        {
            Directory.CreateDirectory(string.Format("{0}./screenshots", Application.dataPath));

        }

        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    void TakePicture(object sender, ControllerInteractionEventArgs e)
    {
        if (cameraViewFinder.activeInHierarchy == true)
        {
            takeHiResShot = true;
            Debug.Log("Screenshot taken.");
        }
    }

    void PlaceCamera(object sender, ControllerInteractionEventArgs e)
    {
        if (cameraViewFinder.activeInHierarchy == true)
        {
            screenShotCamera.transform.position = GetController(placeCameraHandSide).transform.position;
            screenShotCamera.transform.eulerAngles = GetController(placeCameraHandSide).transform.eulerAngles + new Vector3(30, 0, 0);
            Debug.Log("Camera Placed");
        }
    }

    void ToggleViewFinder(object sender, ControllerInteractionEventArgs e)
    {
        if(cameraViewFinder == null)
        {
            CreateViewFinder();
        }

        cameraViewFinder.SetActive(!cameraViewFinder.activeInHierarchy);
    }

    #endregion

}
