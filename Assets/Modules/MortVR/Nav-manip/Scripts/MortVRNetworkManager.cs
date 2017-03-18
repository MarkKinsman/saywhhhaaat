using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

namespace MortVR
{
    public class MortVRNetworkManager : NetworkManager
    {
        public bool ShouldBeServer;
        public GameObject VRPlayerPrefab;

        private int PlayerCount = 0;

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader extraMessageReader)
        {
            SpawnMessage Message = new SpawnMessage();
            Message.Deserialize(extraMessageReader);

            bool IsVRPlayer = Message.isVrPlayer;

            Transform SpawnPoint = this.startPositions[PlayerCount];

            GameObject Player;
            if (IsVRPlayer)
            {
                Player = (GameObject)Instantiate(this.VRPlayerPrefab, SpawnPoint.position, SpawnPoint.rotation);
            }
            else
            {
                Player = (GameObject)Instantiate(this.playerPrefab, SpawnPoint.position, SpawnPoint.rotation);
            }
            NetworkServer.AddPlayerForConnection(conn, Player, playerControllerId);
            PlayerCount++;
        }

        public override void OnServerRemovePlayer(NetworkConnection conn, UnityEngine.Networking.PlayerController player)
        {
            base.OnServerRemovePlayer(conn, player);
            PlayerCount--;
        }

        void Start()
        {
            string SettingPath = Application.dataPath + "/settings.cfg";
            if (File.Exists(SettingPath))
            {
                StreamReader TextReader = new StreamReader(SettingPath, System.Text.Encoding.ASCII);
                ShouldBeServer = TextReader.ReadLine() == "Server";
                networkAddress = TextReader.ReadLine();
                TextReader.Close();
            }

            Debug.Log("Starting Network");
            if (ShouldBeServer)
            {
                StartHost();
            }
            else
            {
                StartClient();
            }
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            SpawnMessage extraMessage = new SpawnMessage();
            extraMessage.isVrPlayer = UnityEngine.VR.VRSettings.enabled;
            ClientScene.AddPlayer(client.connection, 0, extraMessage);
        }
    }
}
