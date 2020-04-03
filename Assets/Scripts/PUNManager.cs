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
        // Deduplicate nickname.
        string originalNickname = PhotonNetwork.LocalPlayer.NickName;
        List<string> nicknames = PhotonNetwork.PlayerList.Select(p => p.NickName).ToList();
        nicknames.Remove(originalNickname);
        int attempt = 1;
        string attemptedNickname = originalNickname;
        while (nicknames.Contains(attemptedNickname, System.StringComparer.OrdinalIgnoreCase)) {
            attempt++;
            attemptedNickname = originalNickname + attempt;
        }
        PhotonNetwork.NickName = attemptedNickname;
        // Finish joining room.
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
        boardScript.InitPlayers(PhotonNetwork.PlayerList.Take(Board.PLAYER_COLORS.Length).Select(p => p.NickName).ToArray().Shuffle());
    }
}
