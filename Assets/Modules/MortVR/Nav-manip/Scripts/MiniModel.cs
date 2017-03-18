using UnityEngine;
using System.Collections;
using Valve.VR;
using VRTK;
using UnityEngine.Networking;

namespace MortVR
{
    public class MiniModel : NetworkBehaviour
    {
        #region Variables

        [HideInInspector]
        public bool miniModelIsVisible = false;

        [Tooltip("Set starting miniModel offset")]
        public Vector3 miniModelOffset;

        [Tooltip("Mini Model Scale.")]
        public float miniModelScale;

        [HideInInspector]
        public Vector3 oneToOneModelOffset;

        GameObject cameraRig;
        GameObject leftHand;
        GameObject rightHand;

        [HideInInspector]
        public GameObject oneToOneModel;

        [HideInInspector]
        public GameObject miniModelInstance;
        
        Renderer[] buildingModelRenderers;

        int teleportableLayer;
        int unTeleportableLayer;

        public GameObject miniModelRoom;
        [HideInInspector]
        public GameObject miniModelRoomInstance;

        #endregion

        void Awake()
        {
            cameraRig = this.gameObject;

            //set up layermasks
            teleportableLayer = LayerMask.NameToLayer("Teleportable");
            unTeleportableLayer = LayerMask.NameToLayer("Unteleportable");

            oneToOneModel = GameObject.Find("Building_Model");
            oneToOneModelOffset = oneToOneModel.transform.position;
        }

        void Start()
        {           
            miniModelInstance = miniBuildingModelInstantiate(oneToOneModel);
            StaticBatchingUtility.Combine(oneToOneModel);
            StaticBatchingUtility.Combine(miniModelInstance);
            miniModelInstance.SetActive(false);

            miniModelRoomInstance = (GameObject)Instantiate(miniModelRoom, Vector3.zero, Quaternion.identity);
            miniModelRoomInstance.SetActive(false);

        }

        public GameObject miniBuildingModelInstantiate(GameObject model)
        {
            GameObject miniBuildingModelInstance = (GameObject)Instantiate(model, miniModelOffset, Quaternion.identity);
            miniBuildingModelInstance.transform.localScale = new Vector3(miniModelScale, miniModelScale, miniModelScale);
            foreach (Collider col in miniBuildingModelInstance.GetComponentsInChildren<Collider>())
            {
                if (col.gameObject.layer != teleportableLayer)
                {
                    Destroy(col);
                }
            }

            buildingModelRenderers = oneToOneModel.GetComponentsInChildren<Renderer>();
            Renderer[] miniBuildingModelInstanceRenderers = miniBuildingModelInstance.GetComponentsInChildren<Renderer>();

            for (int i = 0; i < buildingModelRenderers.Length; i++)
            {
                int lightmapIndex = buildingModelRenderers[i].lightmapIndex;
                Vector4 lightmapScaleOffset = buildingModelRenderers[i].lightmapScaleOffset;              
                miniBuildingModelInstanceRenderers[i].lightmapIndex = lightmapIndex;
                miniBuildingModelInstanceRenderers[i].lightmapScaleOffset = lightmapScaleOffset;
            }

            return miniBuildingModelInstance;
        }
    }
}
