using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using VRTK;

namespace MortVR
{
    public class ModelController : MonoBehaviour
    {

        #region Variables

        GameObject sectionHandle;
        SectionController sectionController = new SectionController();
        Transform sectionPlaneLocation;
        Quaternion sectionPlaneRotation;
        Vector3 sectionDirection = new Vector3(0, 1, 0);
        float sectionOffset = 0;
    
        GameObject oneToOneBuildingModel;
        GameObject miniBuildingModelInstance;

        MiniModel myMiniModel;

        Renderer[] sectionHandleBorders;

        private Shader sectionShader;
        private Shader standardShader;

        bool miniModelIsVisible;

        public Vector3 savedPosition;

        #endregion

        void Start()
        {
            sectionHandle = GameObject.Find("Section_Handle(Clone)");
            var controllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();

            InitTeleportListener(controllerManager.left);
            InitTeleportListener(controllerManager.right);

            sectionShader = Shader.Find("CrossSection/Standard");
            standardShader = Shader.Find("Standard");

            myMiniModel = GetComponent<MiniModel>();
            oneToOneBuildingModel = myMiniModel.oneToOneModel;
            miniBuildingModelInstance = myMiniModel.miniModelInstance;

            sectionHandleBorders = sectionHandle.GetComponentsInChildren<Renderer>();

            sectionController = new SectionController();
            sectionController.miniModel = miniBuildingModelInstance;
            sectionController.sectionShader = sectionShader;
            sectionController.standardShader = standardShader;
            sectionController.getMaterials();
            sectionController.SetStandardShader();

            sectionHandle.GetComponent<Renderer>().enabled = false;
            foreach (Renderer r in sectionHandleBorders)
            {
                r.enabled = false;
            }

        }

        void OnApplicationQuit()
        {
            sectionController.SetStandardShader();
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
            switchPosition();
            switchModels();
        }

        public void switchModels()
        {
            if (myMiniModel.miniModelIsVisible)
            {
                myMiniModel.miniModelInstance.SetActive(false);
                myMiniModel.miniModelRoomInstance.SetActive(false);
                myMiniModel.oneToOneModel.SetActive(true);
                myMiniModel.miniModelIsVisible = false;

                sectionController.SetStandardShader();

                sectionHandle.GetComponent<Renderer>().enabled = false;
                foreach (Renderer r in sectionHandleBorders)
                {
                    r.enabled = false;
                }

            }

            else
            {
                myMiniModel.miniModelRoomInstance.SetActive(true);
                myMiniModel.miniModelInstance.SetActive(true);
                myMiniModel.oneToOneModel.SetActive(false);
                myMiniModel.miniModelIsVisible = true;

                sectionController.SetSectionShader();

                sectionHandle.GetComponent<Renderer>().enabled = true;
                foreach (Renderer r in sectionHandleBorders)
                {
                    r.enabled = true;
                }
            }
        }

        public void switchPosition()
        {
            if (myMiniModel.miniModelIsVisible)
            {
                transform.position = savedPosition;
            }
            else
            {
                savedPosition = transform.position;
                transform.position = Vector3.zero;
            }
        }

        void FixedUpdate()
        {
            if (myMiniModel.miniModelIsVisible)
            {
                sectionPlaneLocation = sectionHandle.transform;
                sectionPlaneRotation = sectionPlaneLocation.rotation;
                float sectionOffset = 0;

                Vector4 sectionPlane = sectionPlaneRotation * sectionDirection;
                Vector4 sectionPoint = new Vector4(sectionPlaneLocation.position.x,
                                                    sectionPlaneLocation.position.y,
                                                    sectionPlaneLocation.position.z, 1f);

                sectionController.cutSection(sectionPoint, sectionPlane, sectionOffset);

            }
        }

    }
}
