﻿using Assets.Code;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PUNManager : MonoBehaviourPunCallbacks
{
    public static bool hotseatMode = false;
    public static List<string> localPlayers;
    public Canvas canvas;
    public GameObject roomInputPrefab, playerListPrefab, localPlayerInputPrefab, boardPrefab;

    GameObject roomInput, playerList, board;
    LocalPlayerInput localPlayerInput;

    void Start()
    {
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = Application.version.ToString();
        if (PhotonNetwork.IsConnected) {
            PhotonNetwork.Disconnect();
        }
        PhotonNetwork.ConnectUsingSettings();
        GameLog.Static("Connecting to server...");
        GameLog.Static("Press F1 to switch to hotseat mode.");
    }
    public override void OnConnectedToMaster() {
        if (hotseatMode) {
            return;
        }
        PhotonNetwork.JoinLobby(TypedLobby.Default);
        GameLog.Static("Connected to server.");
    }
    public override void OnDisconnected(DisconnectCause cause) {
        if (cause == DisconnectCause.MaxCcuReached) {
            GameLog.Static("The server's player cap has been reached. Try again later!");
        } else if (cause == DisconnectCause.ExceptionOnConnect) {
            GameLog.Static("Disconnected from the game server. Check your internet connection.");
        } else {
            GameLog.Static(string.Format("Couldn't connect due to unknown error: {0}", cause));
        }
        GameLog.Static("Quitting in 5 seconds...");
        StartCoroutine("DelayedQuit");
    }
    IEnumerator DelayedQuit() {
        yield return new WaitForSeconds(5);
        Application.Quit();
    }
    public override void OnJoinedLobby() {
        if (hotseatMode) {
            return;
        }
        roomInput = Instantiate(roomInputPrefab, canvas.transform);
        GameLog.Static("Joined lobby.");
    }
    public override void OnCreatedRoom() {
        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
        roomProperties.Add("guid", Guid.NewGuid().ToString());
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
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
        GameLog.Static(string.Format("{0} room {1}.", nicknames.Count == 0 ? "Created" : "Joined", PhotonNetwork.CurrentRoom.Name));
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Return) && (PhotonNetwork.InRoom || hotseatMode) && localPlayerInput == null && board == null) {
            StartGame();
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (PhotonNetwork.InRoom || hotseatMode) {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            } else {
                Application.Quit();
            }
        }
        if (Input.GetKeyDown(KeyCode.F1)) {
            if (!hotseatMode && !PhotonNetwork.InRoom) {
                // Initialize hotseat mode.
                hotseatMode = true;
                localPlayers = new List<string>();
                playerList = Instantiate(playerListPrefab, canvas.transform);
                Destroy(roomInput);
                GameLog.Static("Switched to hotseat mode.");
            } else if (hotseatMode && board == null && localPlayerInput == null && localPlayers.Count < Board.PLAYER_COLORS.Length) {
                // Add a player.
                localPlayerInput = Instantiate(localPlayerInputPrefab, canvas.transform).GetComponent<LocalPlayerInput>();
            }
        }
        if (Input.GetKeyDown(KeyCode.Return) && localPlayerInput != null) {
            string name = localPlayerInput.GetName();
            if (name.Length > 0) {
                localPlayers.Add(name);
                Destroy(localPlayerInput.gameObject);
                playerList.GetComponent<PlayerList>().UpdateListHotseat();
            }
        }
    }
    void StartGame() {
        if (!hotseatMode && !PhotonNetwork.IsMasterClient) {
            return;
        }
        if (!Application.isEditor && PhotonNetwork.PlayerList.Length <= 1) {
            return;
        }
        board = hotseatMode ? Instantiate(boardPrefab) : PhotonNetwork.InstantiateSceneObject("Board", Vector3.zero, Quaternion.identity);
        Board boardScript = board.GetComponent<Board>();
        string[] players = hotseatMode ? localPlayers.ToArray().Shuffle() : PhotonNetwork.PlayerList.Take(Board.PLAYER_COLORS.Length - 1).Select(p => p.NickName).ToArray().Shuffle();
        boardScript.InitPlayers(players);
    }
}
