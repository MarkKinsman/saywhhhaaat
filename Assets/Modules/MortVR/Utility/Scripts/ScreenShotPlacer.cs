using UnityEngine;
using System.Collections;
using VRTK;

public class ScreenShotPlacer : MonoBehaviour {

    GameObject Hand;
    GameObject screenShotCamera;

    void Awake()
    {
        Hand = gameObject;
        screenShotCamera = GameObject.Find("ScreenShot_Camera");
    }

	void Start ()
    {
        setControllerButtons();
	}

    void setControllerButtons()
    {
        //Setup controller event listeners
        if (Hand.GetComponent<VRTK_ControllerEvents>() == null)
         {
            Debug.LogError("You need to put a VRTK_ControllerEvents script on your SteamVR Controllers");
            return;
        }

        Hand.GetComponent<VRTK_ControllerEvents>().ApplicationMenuPressed +=
            new ControllerInteractionEventHandler(DoMenuPressed);
    }

    void DoMenuPressed(object sender, ControllerInteractionEventArgs e)
    {
        screenShotCamera.transform.position = Hand.transform.position;
        screenShotCamera.transform.rotation = Hand.transform.rotation;
    }
}
