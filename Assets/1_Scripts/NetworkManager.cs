using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    #region Constants
    private const int MAX_PLAYER_COUNT = 3;
    private const string ROOM_NAME = "myRoom";
    public const string HMD_NICKNAME = "hmd";
    #endregion

    #region Serialized Fields
    [SerializeField] private AppDeviceType device;
    [SerializeField] private TaskManager taskManager;
    #endregion

    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.NickName = device.ToString();
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("connected");
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = MAX_PLAYER_COUNT,
            IsOpen = true,
            IsVisible = true
        };
        PhotonNetwork.JoinOrCreateRoom(ROOM_NAME, roomOptions, TypedLobby.Default);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"failed to join room: error code = {returnCode}, msg = {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        PhotonNetwork.ReconnectAndRejoin();
    }
}