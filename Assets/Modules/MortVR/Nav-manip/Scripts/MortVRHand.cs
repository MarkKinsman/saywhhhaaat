using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using VRTK;

namespace MortVR
{
    public enum HandSide
    {
        Left,
        Right
    }
    public class MortVRHand : NetworkBehaviour
    {

        #region Variables

        [SyncVar]
        public HandSide Side;

        [SyncVar]
        public NetworkInstanceId OwnerID;


        [SyncVar (hook = "OnColorChange")]
        public Color avatarColor;

        private MortVRInteractableObject touchedObject;
        private MortVRInteractableObject objectInUse;
        private MortVRInteractableObject grabbedObject;

        private GameObject trackedController;

        private SteamVR_Controller.Device steamDevice;

        private MortVRPlayer localPlayer;
        private Vector3 currentVelocity;

        #endregion

        public override void OnStartAuthority()
        {
            // attach the controller model to the tracked controller object on the local client
            if (hasAuthority)
            {
                var controllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();

                if (Side.ToString() == "Left")
                {
                    InitHandListener(controllerManager.left);
                    trackedController = VRTK_DeviceFinder.GetControllerLeftHand();
                }
                if (Side.ToString() == "Right")
                {
                    InitHandListener(controllerManager.right);
                    trackedController = VRTK_DeviceFinder.GetControllerRightHand();
                }

                Helper.AttachAtGrip(trackedController.transform, transform);

                localPlayer = ClientScene.FindLocalObject(OwnerID).GetComponent<MortVRPlayer>();

                steamDevice = SteamVR_Controller.Input((int)trackedController.GetComponent<SteamVR_TrackedObject>().index);
            }
        }

        public override void OnStartClient()
        {
            if (isClient)
            {
                transform.GetChild(0).GetComponent<MeshRenderer>().material.color = avatarColor;
            }
        }

        void Update()
        {
            if(steamDevice != null)
            {
                currentVelocity = steamDevice.velocity;
            }
        }

        void OnUseOff(object sender, ControllerInteractionEventArgs e)
        {
            if (!hasAuthority)
            {
                return;
            }
            if (grabbedObject != null)
            {
                // we have an object in this hand
                if (grabbedObject.isUsable)
                {
                    // and it is usable
                    CmdStopUsing(grabbedObject.netId, this.netId);
                }
            }
            else if (touchedObject != null)
            {
                Debug.Log("While touching: " + touchedObject.name);
                // we are touch a usable object with this hand
                if (touchedObject.isUsable)
                {
                    // and it is usable
                    CmdStopUsing(touchedObject.netId, this.netId);
                }
            }
            else if (objectInUse != null)
            {
                Debug.Log("While using: " + objectInUse.name);
                if (objectInUse.isUsable)
                {
                    // and it is usable
                    CmdStopUsing(objectInUse.netId, this.netId);
                }
            }
        }

        void OnUseOn(object sender, ControllerInteractionEventArgs e)       
        {
            if (!hasAuthority)
            {
                return;
            }

            // interaction requested
            if (grabbedObject != null)
            {
                // we have an object in this hand
                if (grabbedObject.isUsable)
                {
                    // and it is usable
                    CmdStartUsing(grabbedObject.netId, this.netId);
                    objectInUse = grabbedObject;
                }
            }
            else if (touchedObject != null)
            {
                Debug.Log("While touching: " + touchedObject.name);
                // we are touch a usable object with this hand
                if (touchedObject.isUsable)
                {
                    // and it is usable
                    CmdStartUsing(touchedObject.netId, this.netId);
                    objectInUse = touchedObject;
                }
            }
        }

        void OnGrabOn(object sender, ControllerInteractionEventArgs e)
        {
            if (!hasAuthority)
            {
                return;
            }
            if (touchedObject != null)// we have nothing grabbed but something is colliding with our hands
            {
                Debug.Log("While touching: " + touchedObject.name);
                if (touchedObject.isGrabbable)
                {
                    localPlayer.CmdGrab(touchedObject.netId, netId);    // connectionToClient is only non-null on the player object
                                                                        // gets attached to controller in OnStartAuthority of InteractableObject
                    grabbedObject = touchedObject;
                }
            }
        }

        void OnGrabOff(object sender, ControllerInteractionEventArgs e)        
        {
            if (!hasAuthority)
            {
                return;
            }
            Debug.Log("Grip released");
            if (grabbedObject != null) // we have an object in this hand
            {
                localPlayer.CmdDrop(grabbedObject.netId, currentVelocity);
                grabbedObject = null;
            }
        }

        void OnTriggerStay(Collider col)
        {
            var iObject = col.gameObject.GetComponent<MortVRInteractableObject>();
            if (iObject != null)
            {
                if (touchedObject != iObject)
                {
                    Debug.Log("Touched Interactable Object: " + iObject.name);
                    touchedObject = iObject;
                    CmdTouch(touchedObject.netId, netId);
                }
            }
        }

        void OnTriggerExit(Collider col)
        {
            var iObject = col.gameObject.GetComponent<MortVRInteractableObject>();
            if (iObject != null && iObject == touchedObject)
            {
                Debug.Log("UnTouched Interactable Object: " + iObject.name);
                CmdUntouch(touchedObject.netId);
                touchedObject = null;
            }
        }
        
        //Hook from sync var to update hand color
        void OnColorChange(Color myColor)
        {
            avatarColor = myColor;
            transform.GetChild(0).GetComponent<MeshRenderer>().material.color = myColor;
        }

        //Public method to change color of hand, will update syncvar to change other hands
        [Command]
        public void CmdChangeColor(Color myColor)
        {
            avatarColor = myColor;
        }

        [Command]
        public void CmdTouch(NetworkInstanceId objectId, NetworkInstanceId controllerId)
        {
            var iObject = NetworkServer.FindLocalObject(objectId);
            //iObject.GetComponent<InteractableObject> ().touchingControllerId = controllerId;
            var touchable = iObject.GetComponent<ITouchable>();
            if (touchable != null)
                touchable.Touch(this.netId);
        }

        [Command]
        public void CmdUntouch(NetworkInstanceId objectId)
        {
            var iObject = NetworkServer.FindLocalObject(objectId);
            //iObject.GetComponent<InteractableObject> ().touchingControllerId = NetworkInstanceId.Invalid;
            var touchable = iObject.GetComponent<ITouchable>();
            if (touchable != null)
                touchable.Untouch(this.netId);
        }

        [Command]
        public void CmdStartUsing(NetworkInstanceId objectNetId, NetworkInstanceId handNetId)
        {
            var item = NetworkServer.FindLocalObject(objectNetId);
            var usable = item.GetComponent<IUsable>();
            if (usable != null)
                usable.StartUsing(handNetId);
        }

        [Command]
        public void CmdStopUsing(NetworkInstanceId objectNetId, NetworkInstanceId handNetId)
        {
            var item = NetworkServer.FindLocalObject(objectNetId);
            var usable = item.GetComponent<IUsable>();
            if (usable != null)
                usable.StopUsing(handNetId);
        }

        public void InitHandListener(GameObject ctrlr_Manager)
        {
            if (ctrlr_Manager)
            {
                VRTK_ControllerEvents controller = ctrlr_Manager.GetComponent<VRTK_ControllerEvents>();
                {
                    controller.AliasGrabOn += new ControllerInteractionEventHandler(OnGrabOn);
                    controller.AliasGrabOff += new ControllerInteractionEventHandler(OnGrabOff);
                    controller.AliasUseOn += new ControllerInteractionEventHandler(OnUseOn);
                    controller.AliasUseOff += new ControllerInteractionEventHandler(OnUseOff);
                }
            }
        }
    }
}
