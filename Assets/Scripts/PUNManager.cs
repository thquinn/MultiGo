using Assets.Code;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PUNManager : MonoBehaviourPunCallbacks
{
    public Canvas canvas;
    public GameObject roomInputPrefab, playerListPrefab, boardPrefab;

    GameObject roomInput, playerList, board;

    void Start()
    {
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = "0.01";
        PhotonNetwork.ConnectUsingSettings();
        GameLog.Static("Connecting to server...");
    }
    public override void OnConnectedToMaster() {
        PhotonNetwork.JoinLobby(TypedLobby.Default);
        GameLog.Static("Connected to server.");
    }
    public override void OnJoinedLobby() {
        roomInput = Instantiate(roomInputPrefab, canvas.transform);
        GameLog.Static("Joined lobby.");
    }
    public override void OnJoinedRoom() {
        playerList = Instantiate(playerListPrefab, canvas.transform);
        Destroy(roomInput);
        GameLog.Static(string.Format("Joined room {0}.", PhotonNetwork.CurrentRoom.Name));
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Return) && PhotonNetwork.InRoom && board == null) {
            StartGame();
        }
    }
    void StartGame() {
        if (!PhotonNetwork.IsMasterClient) {
            return;
        }
        board = PhotonNetwork.InstantiateSceneObject("Board", Vector3.zero, Quaternion.identity);
        Board boardScript = board.GetComponent<Board>();
        boardScript.InitPlayers(PhotonNetwork.PlayerList.Select(p => p.NickName).ToArray().Shuffle().Take(Board.PLAYER_COLORS.Length).ToArray());
    }
}
