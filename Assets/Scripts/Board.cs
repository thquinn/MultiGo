﻿using Assets.Code;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour, IPunObservable {
    static byte NO_PLAYER = 255;
    public static Color[] PLAYER_COLORS = new Color[] { new Color(1, 0, 0), new Color(0, .2f, 1), new Color(0, .85f, .05f), new Color(1, 1, 0), new Color(1, .7f, 0), new Color(1, 0, .8f) };
    static Tuple<int, int>[] NEIGHBORS = new Tuple<int, int>[] { new Tuple<int, int>(-1, 0), new Tuple<int, int>(1, 0), new Tuple<int, int>(0, -1), new Tuple<int, int>(0, 1) };

    private Camera cam;
    public GameObject canvas;
    private LayerMask layerMaskBoardIntersection;

    public GameObject boardLinePrefab, boardIntersectionPrefab, stonePrefab, stonePreviewPrefab, playerBubblesPrefab, alliancePopupPrefab;
    public PhotonView photonView;
    public AudioSource sfxClack, sfxCapture;

    // Networked fields.
    public string[] playerNames;
    public byte currentPlayerIndex;
    public int width;
    public int height;
    public byte[] stones;
    public byte[] alliances;
    public int allianceRequest = -1;

    Dictionary<Collider2D, int> gridColliders;
    GameObject[] stoneObjects;
    SpriteRenderer stonePreviewRenderer;
    PlayerBubbles playerBubbles;
    AlliancePopup alliancePopup;

    void OnEnable() {
        GameObject playerList = GameObject.FindWithTag("PlayerList");
        if (playerList != null) {
            Destroy(playerList);
        }

        cam = Camera.main;
        canvas = GameObject.FindGameObjectWithTag("Canvas");
        layerMaskBoardIntersection = LayerMask.GetMask("BoardIntersection");
        gridColliders = new Dictionary<Collider2D, int>();

        width = 19;
        height = 19;
        transform.localPosition = new Vector3(-6, 0, 0);
        stones = new byte[width * height];
        for (int i = 0; i < stones.Length; i++) {
            stones[i] = NO_PLAYER;
        }
        DrawBoard();

        stoneObjects = new GameObject[stones.Length];
    }
    void DrawBoard() {
        foreach (bool horizontal in new bool[] { false, true }) {
            int bound = (horizontal ? height : width) - 1;
            int length = (horizontal ? width : height) - 1;
            for (int x = 0; x <= bound; x++) {
                GameObject line = Instantiate(boardLinePrefab, transform);
                float pos = bound / -2f + x;
                line.transform.localPosition = new Vector3(horizontal ? 0 : pos, horizontal ? pos : 0, 1);
                if (!horizontal) {
                    line.transform.localRotation = Quaternion.Euler(0, 0, 90);
                }
                bool outline = x == 0 || x == bound;
                float extraLength = outline ? 6 : 0;
                float thickness = outline ? 6 : 4;
                line.transform.localScale = new Vector3(length * 100 + extraLength, thickness, 1);
            }
        }
        for (byte x = 0; x < width; x++) {
            for (byte y = 0; y < height; y++) {
                GameObject intersection = Instantiate(boardIntersectionPrefab, transform);
                intersection.transform.localPosition = GetCoor(x, y);
                Collider2D collider = intersection.GetComponent<Collider2D>();
                gridColliders[collider] = y * width + x;
            }
        }
        // Star points.
        if (width == 19 && height == 19) {
            for (int x = -6; x <= 6; x += 6) {
                for (int y = -6; y <= 6; y += 6) {
                    GameObject starPoint = Instantiate(stonePrefab, transform);
                    starPoint.transform.localPosition = new Vector3(x, y, 0);
                    starPoint.transform.localScale = new Vector3(.25f, .25f, 1);
                    starPoint.GetComponent<SpriteRenderer>().color = Color.black;
                }
            }
        }
    }
    public void InitPlayers(string[] playerNames) {
        //playerNames = new string[] { "Tom", "Mag", "Rez", "Dyl", "Wil", "Lau" };
        this.playerNames = playerNames;
        alliances = new byte[playerNames.Length * playerNames.Length];
        for (int i = 0; i < playerNames.Length; i++) {
            alliances[i * playerNames.Length + i] = (byte)1;
        }
        for (int i = 0; i < alliances.Length; i++) {
            //alliances[i] = 1;
        }
    }

    void Update() {
        // Don't do anything until we have a state.
        if (playerNames == null || playerNames.Length == 0) {
            return;
        }
        // Initialization.
        if (playerBubbles == null) {
            playerBubbles = Instantiate(playerBubblesPrefab, canvas.transform).GetComponent<PlayerBubbles>();
            playerBubbles.SetBoard(this);
            GameLog.Static("Game has started.");
        }
        // Alliance popup.
        if (allianceRequest >= 0 && IAmPlayer(playerNames[allianceRequest]) && alliancePopup == null) {
            alliancePopup = Instantiate(alliancePopupPrefab, canvas.transform).GetComponent<AlliancePopup>();
            alliancePopup.Set(this);
        }
        if (alliancePopup != null && (allianceRequest == -1 || !IAmPlayer(playerNames[allianceRequest]))) {
            Destroy(alliancePopup.gameObject);
        }
        // Update stones.
        UpdateStoneObjects();
        // Active player controls.
        if (playerNames == null || playerNames.Length == 0) {
            return;
        }
        if (PhotonNetwork.LocalPlayer.NickName != playerNames[currentPlayerIndex]) {
            if (stonePreviewRenderer != null) {
                Color c = stonePreviewRenderer.color;
                c.a = 0;
                stonePreviewRenderer.color = c;
            }
            return;
        }
        Collider2D collider = Util.GetMouseCollider(cam, layerMaskBoardIntersection);
        int coor = collider == null ? -1 : gridColliders[collider];
        if (coor >= 0 && stones[coor] != NO_PLAYER) {
            collider = null;
        }
        // Stone preview.
        if (stonePreviewRenderer == null) {
            stonePreviewRenderer = Instantiate(stonePreviewPrefab, transform).GetComponent<SpriteRenderer>();
            stonePreviewRenderer.color = PLAYER_COLORS[currentPlayerIndex];
        }
        Color color = stonePreviewRenderer.color;
        if (collider == null) {
            color.a = Mathf.Max(0, color.a - .2f);
        } else {
            stonePreviewRenderer.transform.localPosition = collider.transform.localPosition;
            color.a = Mathf.Min(1, color.a + .1f);
        }
        stonePreviewRenderer.transform.Rotate(0, 0, .5f);
        // Stone placement.
        if (Input.GetMouseButtonDown(0) && collider != null) {
            photonView.RPC("PlaceStone", RpcTarget.MasterClient, coor);
            color.a = 0;
        }
        stonePreviewRenderer.color = color;
    }
    [PunRPC]
    void PlaceStone(int i, PhotonMessageInfo info) {
        if (!CanTakeMainAction(info.Sender.NickName)) {
            return;
        }
        stones[i] = currentPlayerIndex;
        // Find captured stones.
        int x = i % width, y = i / width;
        KillResult killResult;
        foreach (var neighbor in NEIGHBORS) {
            killResult = CheckGroupKill(x + neighbor.Item1, y + neighbor.Item2, false);
            ExecuteKillResult(killResult);
        }
        killResult = CheckGroupKill(x, y, true);
        ExecuteKillResult(killResult);
        AdvanceCurrentPlayer();
    }
    void UpdateStoneObjects() {
        bool added = false, removed = false;
        for (int i = 0; i < stones.Length; i++) {
            byte player = stones[i];
            // Add missing stones.
            if (stoneObjects[i] == null && player != NO_PLAYER) {
                stoneObjects[i] = Instantiate(stonePrefab, transform);
                stoneObjects[i].transform.localPosition = GetCoor(i % width, i / width);
                stoneObjects[i].GetComponent<SpriteRenderer>().color = PLAYER_COLORS[player];
                added = true;
            }
            // Remove captured stones.
            else if (stoneObjects[i] != null && player == NO_PLAYER) {
                Destroy(stoneObjects[i]);
                removed = true;
            }
        }
        if (added) {
            sfxClack.Play();
        }
        if (removed) {
            sfxCapture.PlayDelayed(.15f);
        }
    }
    KillResult CheckGroupKill(int x, int y, bool allowSuicide) {
        if (x < 0 || y < 0 || x >= width || y >= height) {
            return new KillResult();
        }
        byte player = GetPlayerAt(x, y);
        if (player == NO_PLAYER) {
            return new KillResult();
        }
        if (player == currentPlayerIndex && !allowSuicide) {
            return new KillResult();
        }
        Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();
        HashSet<Tuple<int, int>> seen = new HashSet<Tuple<int, int>>();
        Tuple<int, int> start = new Tuple<int, int>(x, y);
        queue.Enqueue(start);
        seen.Add(start);
        bool foundLiberty = false;
        while (queue.Count > 0) {
            Tuple<int, int> current = queue.Dequeue();
            byte currentPlayer = GetPlayerAt(current);
            foreach (var neighbor in NEIGHBORS) {
                Tuple<int, int> next = new Tuple<int, int>(current.Item1 + neighbor.Item1, current.Item2 + neighbor.Item2);
                if (next.Item1 < 0 || next.Item2 < 0 || next.Item1 >= width || next.Item2 >= height) {
                    continue;
                }
                if (seen.Contains(next)) {
                    continue;
                }
                byte nextPlayer = GetPlayerAt(next);
                if (nextPlayer == NO_PLAYER) {
                    foundLiberty = true;
                    break;
                }
                if (GetAlliance(currentPlayer, nextPlayer)) {
                    queue.Enqueue(next);
                    seen.Add(next);
                }
            }
        }
        return new KillResult(seen, !foundLiberty);
    }
    void ExecuteKillResult(KillResult killResult) {
        if (!killResult.kill) {
            return;
        }
        foreach (Tuple<int, int> coor in killResult.seen) {
            stones[coor.Item2 * width + coor.Item1] = NO_PLAYER;
        }
    }

    byte GetPlayerAt(int x, int y) {
        return stones[y * width + x];
    }
    byte GetPlayerAt(Tuple<int, int> coor) {
        return stones[coor.Item2 * width + coor.Item1];
    }
    Vector3 GetCoor(int x, int y) {
        float midX = (width - 1) / 2f, midY = (height - 1) / 2f;
        return new Vector3(x - midX, -y + midY);
    }
    public bool IsCurrentPlayer(string name) {
        return name.Equals(playerNames[currentPlayerIndex], StringComparison.OrdinalIgnoreCase);
    }
    public bool IAmPlayer(string name) {
        return PhotonNetwork.LocalPlayer.NickName.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
    bool CanTakeMainAction(string name) {
        return IsCurrentPlayer(name) && allianceRequest == -1;
    }
    void AdvanceCurrentPlayer() {
        currentPlayerIndex = (byte)((currentPlayerIndex + 1) % playerNames.Length);
        allianceRequest = -1;
    }

    public bool GetAlliance(int one, int two) {
        return alliances[one * playerNames.Length + two] == (byte)1;
    }
    void SetAlliance(int one, int two, bool allied) {
        alliances[one * playerNames.Length + two] = allied ? (byte)1 : (byte)0;
        alliances[two * playerNames.Length + one] = allied ? (byte)1 : (byte)0;
    }
    [PunRPC]
    public void RequestAlliance(int index, PhotonMessageInfo info) {
        if (!CanTakeMainAction(info.Sender.NickName)) {
            return;
        }
        allianceRequest = index;
        photonView.RPC("Log", RpcTarget.All, string.Format("{0} sent an alliance request.", info.Sender.NickName));
    }
    [PunRPC]
    public void RespondToAllianceRequest(bool yes, PhotonMessageInfo info) {
        int senderIndex = Array.IndexOf(playerNames, info.Sender.NickName);
        if (senderIndex != allianceRequest) {
            return;
        }
        if (yes) {
            SetAlliance(currentPlayerIndex, allianceRequest, true);
            photonView.RPC("Log", RpcTarget.All, string.Format("{0} is now allied with {1}!", playerNames[currentPlayerIndex], playerNames[allianceRequest]));
        } else {
            photonView.RPC("Log", RpcTarget.All, string.Format("{0}'s alliance request was denied.", playerNames[currentPlayerIndex]));
        }
        AdvanceCurrentPlayer();
    }
    [PunRPC]
    public void BreakAlliance(int index, PhotonMessageInfo info) {
        if (!IsCurrentPlayer(info.Sender.NickName)) {
            return;
        }
        if (allianceRequest >= 0) {
            return;
        }
        SetAlliance(currentPlayerIndex, index, false);
        BrokenAllianceKillCheck(currentPlayerIndex, index);
        photonView.RPC("Log", RpcTarget.All, string.Format("{0} broke their alliance with {1}!", playerNames[currentPlayerIndex], playerNames[index]));
    }
    void BrokenAllianceKillCheck(int one, int two) {
        HashSet<Tuple<int, int>> seen = new HashSet<Tuple<int, int>>();
        List<Tuple<int, int>> toKill = new List<Tuple<int, int>>();
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Tuple<int, int> coor = new Tuple<int, int>(x, y);
                byte player = GetPlayerAt(coor);
                if (player != one && player != two) {
                    continue;
                }
                if (seen.Contains(coor)) {
                    continue;
                }
                KillResult killResult = CheckGroupKill(coor.Item1, coor.Item2, true);
                seen.UnionWith(killResult.seen);
                if (killResult.kill) {
                    toKill.AddRange(killResult.seen);
                }
            }
        }
        if (toKill.Count > 0) {
            ExecuteKillResult(new KillResult(toKill, true));
        }
    }

    [PunRPC]
    public void Log(string line) {
        GameLog.Static(line);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(playerNames);
            stream.SendNext(currentPlayerIndex);
            stream.SendNext(width);
            stream.SendNext(height);
            stream.SendNext(stones);
            stream.SendNext(alliances);
            stream.SendNext(allianceRequest);
        } else {
            GameLog.Static("Received serialized view.");
            playerNames = (string[])stream.ReceiveNext();
            currentPlayerIndex = (byte)stream.ReceiveNext();
            width = (int)stream.ReceiveNext();
            height = (int)stream.ReceiveNext();
            stones = (byte[])stream.ReceiveNext();
            alliances = (byte[])stream.ReceiveNext();
            allianceRequest = (int)stream.ReceiveNext();
        }
    }
}

struct KillResult {
    public IEnumerable<Tuple<int, int>> seen;
    public bool kill;

    public KillResult(IEnumerable<Tuple<int, int>> seen, bool kill = false) {
        this.seen = seen;
        this.kill = kill;
    }
}