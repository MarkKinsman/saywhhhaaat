using UnityEngine;
using System.Collections;
using VRTK;

namespace MortVR
{
    public class TeleportBetweenModels : MonoBehaviour
    {
        public Vector3 savedPosition;
        MiniModel myMiniModel;

        void Awake()
        {
            var controllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();

            InitTeleportListener(controllerManager.left);
            InitTeleportListener(controllerManager.right);
        }

        void Start()
        {
            myMiniModel = GetComponent<MiniModel>();
        }

        public void InitTeleportListener(GameObject cmanager)
        {
            if (cmanager)
            {
                VRTK_ControllerEvents controller = cmanager.GetComponent<VRTK_ControllerEvents>();
                {
                    controller.AliasMiniModelOn += new ControllerInteractionEventHandler(DoAliasMiniModelPressed);
                }
            }
        }

        void DoAliasMiniModelPressed(object sender, ControllerInteractionEventArgs e)
        {
            switchModels();
            if (!myMiniModel.miniModelIsVisible)
            {
                this.transform.position = savedPosition;
            }
            else
            {
                savedPosition = this.transform.position;
                this.transform.position = Vector3.zero;
            }
        }

        public void switchModels()
        {
            if (myMiniModel.miniModelIsVisible)
            {
                myMiniModel.miniModelInstance.SetActive(false);
                myMiniModel.miniModelInstance.SetActive(false);
                myMiniModel.miniModelRoomInstance.SetActive(false);
                myMiniModel.oneToOneModel.SetActive(true);
                myMiniModel.miniModelIsVisible = false;

            }
            else
            {
                myMiniModel.miniModelRoomInstance.SetActive(true);
                myMiniModel.miniModelInstance.SetActive(true);
                myMiniModel.oneToOneModel.SetActive(false);
                myMiniModel.miniModelIsVisible = true;
            }
        }
    }
}
