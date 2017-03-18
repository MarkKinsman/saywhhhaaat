using UnityEngine;
using Valve.VR;
using VRTK;
using System.Collections;

namespace MortVR
{
    [AddComponentMenu("Vive Teleporter/Vive Teleporter")]
    [RequireComponent(typeof(Camera), typeof(BorderRenderer))]
    public class MortVR_TeleportVive : MonoBehaviour
    {
        #region Variables
        ModelController myModelController;
        MiniModel myMiniModel;
        Vector3 miniModelOffset;
        Vector3 oneToOneModelOffset;
        float miniModelScale;
        GameObject PointerObject;
        MortVR_ParabolicPointer Pointer;

        /// Origin of SteamVR tracking space
        [Tooltip("Origin of the SteamVR tracking space")]

        Transform CameraRigTransform;
        /// Origin of the player's head

        [Tooltip("Transform of the player's head")]
        public Transform HeadTransform;
        /// How long, in seconds, the fade-in/fade-out animation should take
        [Tooltip("Duration of the \"blink\" animation (fading in and out upon teleport) in seconds.")]
        public float TeleportFadeDuration = .2f;
        /// Measure in degrees of how often the controller should respond with a haptic click.  Smaller value=faster clicks
        [Tooltip("The player feels a haptic pulse in the controller when they raise / lower the controller by this many degrees.  Lower value = faster pulses.")]
        public float HapticClickAngleStep = 10;

        /// BorderRenderer to render the chaperone bounds (when choosing a location to teleport to)
        private BorderRenderer RoomBorder;

        /// Material used to render the fade in/fade out quad
        [Tooltip("Material used to render the fade in/fade out quad.")]
        public Material FadeMaterial;
        private int MaterialFadeID;

        private SteamVR_TrackedObject ActiveController;

        private float LastClickAngle = 0;

        private bool Teleporting = false;
        private bool FadingIn = false;
        private float TeleportTimeMarker = -1;

        private Mesh PlaneMesh;
        private bool shouldTeleport = false;
        private bool teleportButtonIsPressed;

        #endregion

        void Awake()
        {
            myModelController = GetComponent<ModelController>();
            var controllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();

            InitTeleportListener(controllerManager.left);
            InitTeleportListener(controllerManager.right);
        }

        public void InitTeleportListener(GameObject cmanager)
        {
            if (cmanager)
            {
                VRTK_ControllerEvents controller = cmanager.GetComponent<VRTK_ControllerEvents>();
                {
                    controller.AliasTeleporterOn += new ControllerInteractionEventHandler(DoTeleporterPressed);
                    controller.AliasTeleporterOff += new ControllerInteractionEventHandler(DoTeleporterReleased);
                }
            }
        }

        void DoTeleporterPressed(object sender, ControllerInteractionEventArgs e)
        {
            // Set active controller to this controller, and enable the parabolic pointer and visual indicators
            // that the user can use to determine where they are able to teleport.
            var controller = VRTK_DeviceFinder.TrackedObjectByIndex(e.controllerIndex);
            ActiveController = controller.GetComponent<SteamVR_TrackedObject>();

            Pointer.transform.parent = controller.transform;
            Pointer.transform.localPosition = Vector3.zero;
            Pointer.transform.localRotation = Quaternion.identity;
            Pointer.transform.localScale = Vector3.one;
            Pointer.enabled = true;
            RoomBorder.enabled = true;
            teleportButtonIsPressed = true;

            StartCoroutine("updatePointer");

        }

        void DoTeleporterReleased(object sender, ControllerInteractionEventArgs e)
        {
            StopCoroutine("updatePointer");
            TeleportTimeMarker = Time.time;

            StartCoroutine("teleport");

            // Reset active controller, disable pointer, disable visual indicators
            ActiveController = null;
            Pointer.enabled = false;
            RoomBorder.enabled = false;
            //RoomBorder.Transpose = Matrix4x4.TRS(OriginTransform.position, Quaternion.identity, Vector3.one);

            Pointer.transform.parent = null;
            Pointer.transform.position = Vector3.zero;
            Pointer.transform.rotation = Quaternion.identity;
            Pointer.transform.localScale = Vector3.one;

        }

        void Start()
        {
            CameraRigTransform = gameObject.transform;
            myMiniModel = CameraRigTransform.GetComponent<MiniModel>();
            miniModelOffset = myMiniModel.miniModelOffset;
            miniModelScale = myMiniModel.miniModelScale;
            oneToOneModelOffset = myMiniModel.oneToOneModelOffset;

            PointerObject = GameObject.Find("Pointer");
            Pointer = PointerObject.GetComponent<MortVR_ParabolicPointer>();

            // Standard plane mesh used for "fade out" graphic when you teleport
            // This way you don't need to supply a simple plane mesh in the inspector
            PlaneMesh = new Mesh();
            Vector3[] verts = new Vector3[]
            {
            new Vector3(-1, -1, 0),
            new Vector3(-1, 1, 0),
            new Vector3(1, 1, 0),
            new Vector3(1, -1, 0)
            };
            int[] elts = new int[] { 0, 1, 2, 0, 2, 3 };
            PlaneMesh.vertices = verts;
            PlaneMesh.triangles = elts;
            PlaneMesh.RecalculateBounds();

            // Set some standard variables
            MaterialFadeID = Shader.PropertyToID("_Fade");

            RoomBorder = GetComponent<BorderRenderer>();

            Vector3 p0, p1, p2, p3;
            if (GetChaperoneBounds(out p0, out p1, out p2, out p3))
            {
                BorderPointSet p = new BorderPointSet(new Vector3[] {
                    p0, p1, p2, p3, p0
                });
                RoomBorder.Points = new BorderPointSet[]
                {
                p
                };
            }

            RoomBorder.enabled = false;
        }

        public static bool GetChaperoneBounds(out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
        {
            var initOpenVR = (!SteamVR.active && !SteamVR.usingNativeSupport);
            if (initOpenVR)
            {
                var error = EVRInitError.None;
                OpenVR.Init(ref error, EVRApplicationType.VRApplication_Other);
            }

            var chaperone = OpenVR.Chaperone;
            HmdQuad_t rect = new HmdQuad_t();
            bool success = (chaperone != null) && chaperone.GetPlayAreaRect(ref rect);
            p0 = new Vector3(rect.vCorners0.v0, rect.vCorners0.v1, rect.vCorners0.v2);
            p1 = new Vector3(rect.vCorners1.v0, rect.vCorners1.v1, rect.vCorners1.v2);
            p2 = new Vector3(rect.vCorners2.v0, rect.vCorners2.v1, rect.vCorners2.v2);
            p3 = new Vector3(rect.vCorners3.v0, rect.vCorners3.v1, rect.vCorners3.v2);
            if (!success)
                Debug.LogWarning("Failed to get Calibrated Play Area bounds!  Make sure you have tracking first, and that your space is calibrated.");

            if (initOpenVR)
                OpenVR.Shutdown();

            return success;
        }

        IEnumerator updatePointer()
        {
            for (;;)
            {
                int index = (int)ActiveController.index;
                var device = SteamVR_Controller.Input(index);

                // The user is still deciding where to teleport and has the touchpad held down.
                // Note: rendering of the parabolic pointer / marker is done in ParabolicPointer

                Vector3 offset = HeadTransform.position - CameraRigTransform.position;
                offset.y = 0;
                if (MortVR_ParabolicPointer.hitCorrectCollider)
                {
                    RoomBorder.enabled = true;
                    // Render representation of where the chaperone bounds will be after teleporting
                    if (myMiniModel.miniModelIsVisible == true)
                    {
                        RoomBorder.Transpose = Matrix4x4.TRS(Pointer.SelectedPoint - (offset * miniModelScale), Quaternion.identity, new Vector3(miniModelScale, miniModelScale, miniModelScale));
                    }
                    else
                    {
                        RoomBorder.Transpose = Matrix4x4.TRS(Pointer.SelectedPoint - offset, Quaternion.identity, Vector3.one);
                    }
                }
                else
                {
                    RoomBorder.enabled = false;
                }

                // Haptic feedback click every [HaptickClickAngleStep] degrees
                float angleClickDiff = Pointer.CurrentParabolaAngle - LastClickAngle;

                if (Mathf.Abs(angleClickDiff) > HapticClickAngleStep)
                {
                    LastClickAngle = Pointer.CurrentParabolaAngle;
                    device.TriggerHapticPulse();
                }

                Pointer.ForceUpdateCurrentAngle();
                LastClickAngle = Pointer.CurrentParabolaAngle;


                yield return null;
            }
        }
        IEnumerator teleport()
        {
            if (MortVR_ParabolicPointer.hitPointHit && MortVR_ParabolicPointer.hitCorrectCollider)
            {
                while (Time.time - TeleportTimeMarker <= TeleportFadeDuration / 2)
                {
                    //fade out
                    float alpha = Mathf.Clamp01((Time.time - TeleportTimeMarker) / (TeleportFadeDuration / 2));
                    Matrix4x4 local = Matrix4x4.TRS(Vector3.forward * 0.3f, Quaternion.identity, Vector3.one);
                    FadeMaterial.SetPass(0);
                    FadeMaterial.SetFloat(MaterialFadeID, alpha);
                    Instantiate(PlaneMesh, new Vector3(0, 0, 0), Quaternion.identity);
                    Graphics.DrawMeshNow(PlaneMesh, transform.localToWorldMatrix * local);
                    yield return null;
                }

                if (myMiniModel.miniModelIsVisible)
                {
                    CameraRigTransform.position = (Pointer.SelectedPoint - miniModelOffset) * (1 / miniModelScale) + oneToOneModelOffset;
                    myModelController.switchModels();
                }

                else
                {
                    Vector3 offset = CameraRigTransform.position - HeadTransform.position;
                    offset.y = 0;
                    CameraRigTransform.position = Pointer.SelectedPoint + offset;
                }

                while (Time.time - TeleportTimeMarker <= TeleportFadeDuration)
                {
                    //fade in
                    float alpha = 1 - Mathf.Clamp01((Time.time - TeleportTimeMarker) / (TeleportFadeDuration / 2));
                    Matrix4x4 local = Matrix4x4.TRS(Vector3.forward * 0.3f, Quaternion.identity, Vector3.one);
                    FadeMaterial.SetPass(0);
                    FadeMaterial.SetFloat(MaterialFadeID, alpha);
                    Graphics.DrawMeshNow(PlaneMesh, transform.localToWorldMatrix * local);
                    yield return null;
                }
            }
        }
    }
}

  
