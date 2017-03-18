using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace MortVR
{
    public enum bodyPart
    {
        Left_Hand,
        Right_Hand,
        Head
    }

    public class miniMeObject : NetworkBehaviour
    {
        #region Variables

        //set on server by command from authoritative client
        //synchronized from server to non-authoritative client
        [SyncVar]
        public Color MiniMeColor;

        [SyncVar]
        public Vector3 MiniPosition;

        [SyncVar]
        public Quaternion MiniRotation;

        [SyncVar]
        public Vector3 MiniScale;

        //Set only by authoritative Client
        Vector3 Local_MiniPosition;
        Quaternion Local_MiniRotation;
        public Vector3 Local_MiniScale;
        bool miniModelIsVisible;

        //initialized on server on spawn, then synched to clients
        [SyncVar]
        public bodyPart myBodyPart;

        [SyncVar]
        public Vector3 bodyPartScale;

        [SyncVar]
        public Color Local_miniMeColor;

        //initialized by authoritative client OnStartAuthority
        GameObject cameraRig;
        GameObject trackedObject;
        float miniModelScale;
        Vector3 miniModelOffset;
        MiniModel myMiniModel;
        Renderer myRenderer;

        #endregion

        public override void OnStartAuthority()
        {
            if (!hasAuthority || !isClient)
            {
                return;
            }

            cameraRig = FindObjectOfType<SteamVR_ControllerManager>().gameObject;
            myMiniModel = cameraRig.GetComponent<MiniModel>();
            miniModelScale = myMiniModel.miniModelScale;
            miniModelOffset = myMiniModel.miniModelOffset;

            switch (myBodyPart)
            {
                case bodyPart.Left_Hand:
                    trackedObject = cameraRig.transform.GetChild(0).gameObject;
                    break;

                case bodyPart.Right_Hand:
                    trackedObject = cameraRig.transform.GetChild(1).gameObject;
                    break;

                case bodyPart.Head:
                    trackedObject = cameraRig.transform.GetChild(2).gameObject;
                    break;
            }
        }

        void FixedUpdate()
        {
            //The local miniMe
            if (hasAuthority && isClient)
            {
                CalcMiniMeTransform();
                GetComponent<MeshRenderer>().material.color = Local_miniMeColor;
                transform.position = Local_MiniPosition;
                transform.rotation = Local_MiniRotation;
                transform.localScale = Local_MiniScale;
                miniModelIsVisible = myMiniModel.miniModelIsVisible;
                if (!miniModelIsVisible)
                {
                    foreach (Renderer r in GetComponentsInChildren<Renderer>())
                    {
                        r.enabled = false;
                    }
                }
                else
                {
                    foreach (Renderer r in GetComponentsInChildren<Renderer>())
                    {
                        r.enabled = true;
                    }
                }
            }
            
            //the remote client's miniMe
            if(!hasAuthority && isClient)
            {
                GetComponent<MeshRenderer>().material.color = MiniMeColor;
                transform.position = MiniPosition;
                transform.rotation = MiniRotation;
                transform.localScale = MiniScale;
            }               
        }

        private void CalcMiniMeTransform()
        {
            if (!hasAuthority || !isClient)
            {
                return;
            }

            Local_MiniPosition = trackedObject.transform.position;
            Local_MiniPosition *= miniModelScale;
            Local_MiniPosition += miniModelOffset;

            if (miniModelIsVisible)
            {
                Local_MiniPosition += cameraRig.GetComponent<ModelController>().savedPosition * miniModelScale;
            }

            Local_MiniRotation = trackedObject.transform.rotation;

            Local_MiniScale = bodyPartScale * miniModelScale;

            Cmd_setServerMiniMeValues(Local_MiniPosition, Local_MiniRotation, Local_MiniScale, Local_miniMeColor);
        }

        [Command]
        private void Cmd_setServerMiniMeValues(Vector3 pos, Quaternion rot, Vector3 scale, Color color)
        {
            MiniPosition = pos;
            MiniRotation = rot;
            MiniScale = scale;
            MiniMeColor = color;
        }
    }
}
