using UnityEngine;
using System.Collections;
using VRTK;

namespace MortVR
{


    public class CameraScaler : MonoBehaviour
    {
        #region Varaibles

        [HideInInspector]
        public bool isScaleMiniModelVisible;
        private float cameraRigScale = 12;
        private float savedCameraRigScale = 12;
        private float savedDistance;
        private float startingDistance;
        private float currentDistance;

        private Vector3 macroSavedPosition;
        private Vector3 miniSavedPosition;
        private VRTK_ControllerEvents controller;
        private GameObject leftHand;
        private GameObject rightHand;
        private Transform head;
        private Vector3 cachedHeadPosition;

        private Vector3 startposition;
        private GameObject pressedController;

        #endregion

        void Awake()
        {
            var controllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();
            InitMiniModelListener(controllerManager.left);
            InitMiniModelListener(controllerManager.right);
            leftHand = VRTK_DeviceFinder.GetControllerLeftHand();
            rightHand = VRTK_DeviceFinder.GetControllerRightHand();
            head = VRTK_DeviceFinder.HeadsetTransform();
        }

        public void InitMiniModelListener(GameObject cmanager)
        {
            if (cmanager)
            {
                controller = cmanager.GetComponent<VRTK_ControllerEvents>();
                {
                    controller.AliasPointerOn += new ControllerInteractionEventHandler(DoAliasMiniModelPressed);
                    controller.AliasGrabOn += new ControllerInteractionEventHandler(DoAliasGrabPressed);
                    controller.AliasGrabOff += new ControllerInteractionEventHandler(DoAliasGrabReleased);
                }
            }
        }

        void DoAliasMiniModelPressed(object sender, ControllerInteractionEventArgs e)
        {
            if (!isScaleMiniModelVisible)
            {
                StopAllCoroutines();
                macroSavedPosition = transform.position;
                transform.localScale = new Vector3(savedCameraRigScale, savedCameraRigScale, savedCameraRigScale);
                transform.position = miniSavedPosition;
                isScaleMiniModelVisible = true;
            }
            else
            {
                StopAllCoroutines();
                miniSavedPosition = transform.position;
                gameObject.transform.localScale = Vector3.one;       
                isScaleMiniModelVisible = false;
                transform.position = macroSavedPosition;
            }
        }

        void DoAliasGrabReleased(object sender, ControllerInteractionEventArgs e)
        {
            if (isScaleMiniModelVisible &&
                   leftHand.GetComponent<VRTK_ControllerEvents>().IsButtonPressed(controller.grabToggleButton) ||
                   rightHand.GetComponent<VRTK_ControllerEvents>().IsButtonPressed(controller.grabToggleButton))
            {
                StopCoroutine("updateScale");
                savedCameraRigScale = cameraRigScale;
            }
            else
            {
                StopCoroutine("updatePosition");
            }
        }

        void DoAliasGrabPressed(object sender, ControllerInteractionEventArgs e)
        {
            if (isScaleMiniModelVisible)
            {
                if (leftHand.GetComponent<VRTK_ControllerEvents>().IsButtonPressed(controller.grabToggleButton) &&
                    rightHand.GetComponent<VRTK_ControllerEvents>().IsButtonPressed(controller.grabToggleButton))
                {
                    startingDistance = Vector3.Distance(leftHand.transform.position, rightHand.transform.position) / (cameraRigScale * savedCameraRigScale);
                    cachedHeadPosition = head.position;
                    StartCoroutine("updateScale");
                }
                else
                {
                    pressedController = VRTK_DeviceFinder.TrackedObjectByIndex(e.controllerIndex);
                    startposition = pressedController.transform.position;
                    StartCoroutine("updatePosition");
                }
            }
        }

        IEnumerator updateScale()
        {
            while (leftHand.GetComponent<VRTK_ControllerEvents>().IsButtonPressed(controller.grabToggleButton) &&
                    rightHand.GetComponent<VRTK_ControllerEvents>().IsButtonPressed(controller.grabToggleButton))
            {                
                float currentRatio = 1 / startingDistance;
                currentDistance = Vector3.Distance(leftHand.transform.position, rightHand.transform.position) / (cameraRigScale * savedCameraRigScale);
                cameraRigScale = savedCameraRigScale / (currentDistance * currentRatio);
                if(cameraRigScale < 4)
                {
                    cameraRigScale = 4f;
                }
                transform.localScale = new Vector3(cameraRigScale, cameraRigScale, cameraRigScale);
                yield return null;
            }
        }

        IEnumerator updatePosition()
        {
            while (pressedController.GetComponent<VRTK_ControllerEvents>().IsButtonPressed(controller.grabToggleButton))
            {
                transform.position = transform.position + (startposition - pressedController.transform.position);
                yield return null;
            }                          
        }
    }
}
