using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace MortVR
{
    public class MortVRPlayer : NetworkBehaviour
    {
        [SyncVar (hook = "OnNameChange")]
        public string playerName;
   
        [SyncVar (hook = "OnColorChange")]
        public Color playerColor;

        public GameObject leftHand;
        public GameObject rightHand;
        public GameObject miniLeftHand;
        public GameObject miniRightHand;
        public GameObject miniHead;

        public bool InMiniModel;
        public GameObject miniHandPrefab;
        public GameObject miniHeadPrefab;
        public GameObject sectionHandlePrefab;
        GameObject sectionHandle;

        float miniModelScale;
        Vector3 miniModelStartLocation;

        public GameObject CameraRigPrefab;
        public GameObject HandPrefab;
        public GameObject CameraRigInstance;

        //Attach a player info prefab that will manage setting color and name
        [SerializeField]
        private GameObject PlayerInfoPrefab;
        private GameObject _playerInfoInstance;

        //On syncvar update name variable and component text
        void OnNameChange(string myName)
        {
            playerName = myName;
            gameObject.transform.GetChild(0).GetComponent<TextMesh>().text = myName;
        }

        //On syncvar update color variable and color for head, hands, minime
        void OnColorChange(Color myColor)
        {
            playerColor = myColor;
            gameObject.GetComponent<MeshRenderer>().material.color = myColor;
            if (isClient & hasAuthority & leftHand != null)
            {

                leftHand.GetComponent<MortVRHand>().CmdChangeColor(playerColor);
                rightHand.GetComponent<MortVRHand>().CmdChangeColor(playerColor);

                miniRightHand.GetComponent<miniMeObject>().Local_miniMeColor = myColor;
                miniLeftHand.GetComponent<miniMeObject>().Local_miniMeColor = myColor;
                miniHead.GetComponent<miniMeObject>().Local_miniMeColor = myColor;
            }
        }

        public override void OnStartLocalPlayer()
        {
            if (!isClient)
                return;
            // delete main camera
            DestroyImmediate(Camera.main.gameObject);

            // create camera rig and attach player model to it
            CameraRigInstance = (GameObject)Instantiate(
                CameraRigPrefab,
                transform.position,
                transform.rotation);

            GameObject Head = CameraRigInstance.GetComponentInChildren<SteamVR_Camera>().gameObject;
            transform.parent = Head.transform;
            transform.localPosition = new Vector3(0f, -0.03f, -0.06f);

            //instantiate and subscribe to player info UI
            _playerInfoInstance = (GameObject)Instantiate(PlayerInfoPrefab);
            _playerInfoInstance.GetComponent<PlayerInfo>().EventOnPlayerInfoChange += bufferSetValue;              

            if (FindObjectOfType<SectionHandle>() == null)
            {
                Cmd_SpawnSectionHandle();
            }

            TryDetectControllers();
        }

        public override void OnStartClient()
        {
            if (isClient)
            {
                gameObject.GetComponent<MeshRenderer>().material.color = playerColor;
                gameObject.transform.GetChild(0).GetComponent<TextMesh>().text = playerName;
            }
        }

        void TryDetectControllers()
        {
            var controllers = CameraRigInstance.GetComponentsInChildren<SteamVR_TrackedObject>();
            if (controllers != null && controllers.Length == 2 && controllers[0] != null && controllers[1] != null)
            {
                CmdSpawnHands(netId);
                Cmd_SpawnMiniMe(netId);

                //Once all components are instantiated set colors
                _playerInfoInstance.GetComponent<PlayerInfo>().GetPlayerInfo();
            }
            else
            {
                Invoke("TryDetectControllers", 2f);
            }
        }

        //A subscribed event can not be a command, this acts as a buffer
        public void bufferSetValue(PlayerInfo.PlayerInfoPacket p )
        {
            CmdSetValuesFromUI(p);
        }

        //Set the syncvar on the server to trigger hooks
        [Command]
        public void CmdSetValuesFromUI(PlayerInfo.PlayerInfoPacket p)
        {
            playerName = p.playerName;
            playerColor = p.playerColor;
        }

        [Command]
        void CmdSpawnHands(NetworkInstanceId playerId)
        {
            // instantiate controllers
            // tell the server, to spawn two new networked controller model prefabs on all clients
            // give the local player authority over the newly created controller models

            leftHand = Instantiate(HandPrefab);
            rightHand = Instantiate(HandPrefab);
            //leftHand.name = "Player " + playerId + " LeftHand";
            //rightHand.name = "Player " + playerId + " RightHand";

            var leftVRHand = leftHand.GetComponent<MortVRHand>();
            var rightVRHand = rightHand.GetComponent<MortVRHand>();

            leftVRHand.Side = HandSide.Left;
            rightVRHand.Side = HandSide.Right;
            leftVRHand.OwnerID = playerId;
            rightVRHand.OwnerID = playerId;

            NetworkServer.SpawnWithClientAuthority(leftHand, base.connectionToClient);
            NetworkServer.SpawnWithClientAuthority(rightHand, base.connectionToClient);

            //Server send back components that were instantiated
            RpcSetHands(leftHand, rightHand);
        }

        //Store instantiated objects to variable to change later
        [ClientRpc]
        void RpcSetHands(GameObject _leftHand, GameObject _rightHand)
        {
            leftHand = _leftHand;
            rightHand = _rightHand;
        }

        [Command]
        public void Cmd_SpawnMiniMe(NetworkInstanceId playerId)
        {
            miniRightHand = Instantiate(miniHandPrefab);
            miniLeftHand = Instantiate(miniHandPrefab);
            miniHead = Instantiate(miniHeadPrefab);

            //miniLeftHand.name = "Player " + playerId + " MiniLeftHand";
            //miniRightHand.name = "Player " + playerId + " MiniRightHand";
            //miniHead.name = "Player " + playerId + " MiniHead";

            var leftMiniScript = miniRightHand.GetComponent<miniMeObject>();
            var rightMiniScript = miniLeftHand.GetComponent<miniMeObject>();
            var miniHeadScript = miniHead.GetComponent<miniMeObject>();

            leftMiniScript.myBodyPart = bodyPart.Left_Hand;
            rightMiniScript.myBodyPart = bodyPart.Right_Hand;
            miniHeadScript.myBodyPart = bodyPart.Head;

            leftMiniScript.bodyPartScale = miniHandPrefab.transform.localScale;
            rightMiniScript.bodyPartScale = miniHandPrefab.transform.localScale;
            miniHeadScript.bodyPartScale = miniHeadPrefab.transform.localScale;

            NetworkServer.SpawnWithClientAuthority(miniRightHand, base.connectionToClient);
            NetworkServer.SpawnWithClientAuthority(miniLeftHand, base.connectionToClient);
            NetworkServer.SpawnWithClientAuthority(miniHead, base.connectionToClient);

            //Server send back components that were instantiated
            RpcSetMiniMe(miniLeftHand, miniRightHand, miniHead);
        }

        //Store instantiated objects to variable to change later
        [ClientRpc]
        void RpcSetMiniMe(GameObject _leftHand, GameObject _rightHand, GameObject _head)
        {
            miniLeftHand = _leftHand;
            miniRightHand = _rightHand;
            miniHead = _head;
        }

        [Command]
        public void Cmd_SpawnSectionHandle()
        {
            if (!isServer)
            {
                return;
            }

            sectionHandle = Instantiate(sectionHandlePrefab);
            NetworkServer.Spawn(sectionHandle);
        }

        [Command]
        public void CmdGrab(NetworkInstanceId objectId, NetworkInstanceId controllerId)
        {
            var iObject = NetworkServer.FindLocalObject(objectId);
            var networkIdentity = iObject.GetComponent<NetworkIdentity>();

            //you can only grab the section handle if someone else isn't currently grabbing it.
            if (networkIdentity.clientAuthorityOwner == null)
            {
                networkIdentity.AssignClientAuthority(connectionToClient);
                var interactableObject = iObject.GetComponent<MortVRInteractableObject>();
                interactableObject.RpcAttachToHand(controllerId);    // client-side
                var hand = NetworkServer.FindLocalObject(controllerId);
                interactableObject.AttachToHand(hand);    // server-side
            }
        }

        [Command]
        public void CmdDrop(NetworkInstanceId objectId, Vector3 currentHolderVelocity)
        {
            var iObject = NetworkServer.FindLocalObject(objectId);
            var networkIdentity = iObject.GetComponent<NetworkIdentity>();
            if (networkIdentity.clientAuthorityOwner == GetComponent<NetworkIdentity>().clientAuthorityOwner)
            {
                networkIdentity.RemoveClientAuthority(connectionToClient);
                var interactableObject = iObject.GetComponent<MortVRInteractableObject>();
                interactableObject.RpcDetachFromHand(currentHolderVelocity); // client-side
                interactableObject.DetachFromHand(currentHolderVelocity); // server-side
            }
        }

        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }
    }
}
