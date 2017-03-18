using UnityEngine;
using System.Collections;
using VRTK;

public class HiResScreenShots : MonoBehaviour
{
    public int resWidth = 2550; public int resHeight = 3300;
    Camera cam;
    private bool takeHiResShot = false;
    GameObject leftHand;
    GameObject rightHand;
    GameObject cameraRig;

    void TryDetectCameraRig()
    {
        cameraRig = GameObject.Find("[MortVRCameraRig](Clone)");
        if (cameraRig != null)
        {
            leftHand = cameraRig.transform.GetChild(0).gameObject;
            //rightHand = cameraRig.transform.GetChild(1).gameObject;
            setControllerButtons();
            print("found camera rig");
        }

        else
        {
            Invoke("TryDetectCameraRig", 2f);
        }
    }

    void Start()
    {
        TryDetectCameraRig();
        cam = GetComponent<Camera>();
    }


    public static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screenshots/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    void LateUpdate()
    {
        if (takeHiResShot)
        {
            RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
            cam.targetTexture = rt;
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            cam.targetTexture = null;
            RenderTexture.active = null; // JC: added to avoid errors
            Destroy(rt);
            byte[] bytes = screenShot.EncodeToPNG();
            string filename = ScreenShotName(resWidth, resHeight);
            System.IO.File.WriteAllBytes(filename, bytes);
            Debug.Log(string.Format("Took screenshot to: {0}", filename));
            takeHiResShot = false;
        }
    }

    void setControllerButtons()
    {
        //Setup controller event listeners
        if (leftHand.GetComponent<VRTK_ControllerEvents>() == null)
           //|| rightHand.GetComponent<VRTK_ControllerEvents>() == null)
        {
            Debug.LogError("You need to put a VRTK_ControllerEvents script on your SteamVR Controllers");
            return;
        }

        leftHand.GetComponent<VRTK_ControllerEvents>().ApplicationMenuPressed +=
            new ControllerInteractionEventHandler(DoMenuPressed);
        //rightHand.GetComponent<VRTK_ControllerEvents>().TriggerPressed +=
        //    new ControllerInteractionEventHandler(DoTriggerPressed);
    }

    void DoMenuPressed(object sender, ControllerInteractionEventArgs e)
    {
        takeHiResShot = true;
        print("hippy");
    }
}