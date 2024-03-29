﻿using Assets.Code;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Board : MonoBehaviour, IPunObservable {
    static bool TEST_FLAG = false;

    static byte NO_PLAYER = 255;
    public static Color[] PLAYER_COLORS = new Color[] { new Color(0.9058824f, 0.1843137f, 0.1960784f),
                                                        new Color(0.254902f, 0.3764706f, 0.6666667f),
                                                        new Color(.966f, .966f, 0),
                                                        new Color(0.004538993f, 0.9622642f, 0.3151309f),
                                                        new Color(0.8679245f, 0, 0.6381932f),
                                                        new Color(0.4392132f, 0.8095631f, 0.8867924f) };
    static Tuple<int, int>[] NEIGHBORS = new Tuple<int, int>[] { new Tuple<int, int>(-1, 0), new Tuple<int, int>(1, 0), new Tuple<int, int>(0, -1), new Tuple<int, int>(0, 1) };

    public GameObject canvas;
    private Camera cam;
    private LayerMask layerMaskBoardIntersection;

    public GameObject boardLinePrefab, boardIntersectionPrefab, coordinateMarkerPrefab, stonePrefab, stonePreviewPrefab, playerBubblesPrefab, alliancePopupPrefab, allianceAdjacencyIndicatorPrefab, recentMoveIndicatorPrefab, captureIndicatorPrefab;
    public PhotonView photonView;
    public AudioSource sfxClack, sfxCapture;

    // Networked fields.
    public bool manualUpdate; // Changing the contents of arrays doesn't trigger a Photon update, so I flip this to force one.
    public string[] playerNames;
    public byte currentPlayerIndex;
    public int width;
    public int height;
    public byte[] stones;
    public int[] captures;
    public bool[] alliances;
    public int allianceRequest = -1;

    public Dictionary<Collider2D, int> gridColliders;
    GameObject[] stoneObjects;
    SpriteRenderer stonePreviewRenderer;
    PlayerBubbles playerBubbles;
    AlliancePopup alliancePopup;
    Dictionary<Tuple<int, int, bool>, GameObject> allianceAdjacencyIndicators;
    Queue<GameObject> recentMoveIndicators;
    List<GameObject> captureIndicators;

    void OnEnable() {
        GameLog.Associate(this);
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
        allianceAdjacencyIndicators = new Dictionary<Tuple<int, int, bool>, GameObject>();
        captureIndicators = new List<GameObject>();
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
                for (int coorMult = -1; coorMult <= 1; coorMult += 2) {
                    GameObject coordinateMarker = Instantiate(coordinateMarkerPrefab, transform);
                    float edgeCoor = (horizontal ? width : height) / 2f + .2f;
                    coordinateMarker.transform.localPosition = new Vector3(horizontal ? edgeCoor * coorMult : pos, horizontal ? pos : edgeCoor * coorMult, 0);
                    coordinateMarker.GetComponent<TextMeshPro>().text = horizontal ? (x + 1).ToString() : Util.GetCoorLetters(x);
                }
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
                    starPoint.transform.localPosition = new Vector3(x, y, 1);
                    starPoint.transform.localScale = new Vector3(.25f, .25f, 1);
                    starPoint.GetComponent<SpriteRenderer>().color = Color.black;
                }
            }
        }
    }
    public void InitPlayers(string[] playerNames) {
        if (TEST_FLAG) {
            playerNames = new string[] { "Alice", "Bob", "Carol", "Dan", "Edna" };
            PhotonNetwork.LocalPlayer.NickName = "Alice";
        }
        this.playerNames = playerNames;
        captures = new int[playerNames.Length];
        alliances = new bool[playerNames.Length * playerNames.Length];
        for (int i = 0; i < playerNames.Length; i++) {
            alliances[i * playerNames.Length + i] = true;
        }
        for (int i = 0; i < alliances.Length; i++) {
            //alliances[i] = true;
        }

        GameLog.StaticMGG(string.Format("S {0} {1}", width, height));
        for (int i = 0; i < playerNames.Length; i++) {
            GameLog.StaticMGG(string.Format("P{0} {1}", i + 1, playerNames[i]));
        }
        GameLog.StaticMGG("");
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
        if (!CanTakeMainAction(PhotonNetwork.LocalPlayer.NickName)) {
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
        }
        Color color = stonePreviewRenderer.color;
        color.r = PLAYER_COLORS[currentPlayerIndex].r;
        color.g = PLAYER_COLORS[currentPlayerIndex].g;
        color.b = PLAYER_COLORS[currentPlayerIndex].b;
        if (collider == null) {
            color.a = Mathf.Max(0, color.a - .2f);
        } else {
            stonePreviewRenderer.transform.localPosition = collider.transform.localPosition;
            color.a = Mathf.Min(1, color.a + .1f);
        }
        stonePreviewRenderer.transform.Rotate(0, 0, -.5f);
        // Stone placement.
        if (Input.GetMouseButtonDown(0) && collider != null) {
            if (PUNManager.hotseatMode) {
                PlaceStone(coor, new PhotonMessageInfo());
            } else {
                photonView.RPC("PlaceStone", RpcTarget.MasterClient, coor);
            }
            color.a = 0;
        }
        stonePreviewRenderer.color = color;
    }
    [PunRPC]
    void PlaceStone(int i, PhotonMessageInfo info) {
        if (!PUNManager.hotseatMode && !CanTakeMainAction(info.Sender.NickName)) {
            return;
        }
        stones[i] = currentPlayerIndex;
        // Find captured stones.
        int x = i % width, y = i / width;
        List<KillResult> killResults = new List<KillResult>();
        foreach (var neighbor in NEIGHBORS) {
            KillResult result = CheckGroupKill(x + neighbor.Item1, y + neighbor.Item2, false);
            if (result.kill) {
                killResults.Add(result);
            }
        }
        // Check for suicide.
        KillResult selfResult = CheckGroupKill(x, y, true);
        if (selfResult.kill && killResults.Count == 0) {
            stones[i] = NO_PLAYER;
            if (PUNManager.hotseatMode) {
                GameLog.Static("No suicide allowed!");
            } else {
                photonView.RPC("Log", info.Sender, "No suicide allowed!");
            }
            return;
        }
        // Perform all captures simultaneously, as they may be depedent upon each other:
        // https://senseis.xmp.net/diagrams/5/91266184e7497955d87b8087fffb1ecf.png
        HashSet<Tuple<int, int>> killCoors = new HashSet<Tuple<int, int>>();
        foreach (KillResult killResult in killResults) {
            killCoors.UnionWith(killResult.seen);
            ExecuteKillResult(killResult);
        }
        captures[currentPlayerIndex] += killCoors.Count;
        GameLog.StaticMGG(string.Format("{0} {1}", currentPlayerIndex + 1, Util.GetMGGCoorFromIndex(width, height, i)));
        if (PUNManager.hotseatMode && stonePreviewRenderer != null) {
            Color c = stonePreviewRenderer.color;
            c.a = 0;
            stonePreviewRenderer.color = c;
        }
        AdvanceCurrentPlayer();
    }

    void UpdateStoneObjects() {
        // Stone objects.
        bool added = false, removed = false;
        bool enableRecentMoveIndicators = recentMoveIndicators != null;
        if (recentMoveIndicators == null) {
            recentMoveIndicators = new Queue<GameObject>();
        }
        List<GameObject> newCaptureIndicators = null;
        for (int i = 0; i < stones.Length; i++) {
            byte player = stones[i];
            // Add missing stones.
            if (stoneObjects[i] == null && player != NO_PLAYER) {
                stoneObjects[i] = Instantiate(stonePrefab, transform);
                stoneObjects[i].transform.localPosition = GetCoor(i % width, i / width);
                stoneObjects[i].GetComponent<SpriteRenderer>().color = PLAYER_COLORS[player];
                added = true;
                if (!enableRecentMoveIndicators) {
                    GameObject recentMoveIndicator = Instantiate(recentMoveIndicatorPrefab, transform);
                    Vector3 recentMovePos = stoneObjects[i].transform.localPosition;
                    recentMovePos.z = -.1f;
                    recentMoveIndicator.transform.localPosition = recentMovePos;
                    recentMoveIndicators.Enqueue(recentMoveIndicator);
                }
            }
            // Remove captured stones.
            else if (stoneObjects[i] != null && player == NO_PLAYER) {
                Color captureColor = stoneObjects[i].GetComponent<SpriteRenderer>().color;
                Destroy(stoneObjects[i]);
                removed = true;
                GameObject captureIndicator = Instantiate(captureIndicatorPrefab, transform);
                captureIndicator.transform.localPosition = GetCoor(i % width, i / width);
                foreach (SpriteRenderer sr in captureIndicator.GetComponentsInChildren<SpriteRenderer>()) {
                    sr.color = captureColor;
                }
                if (newCaptureIndicators == null) {
                    newCaptureIndicators = new List<GameObject>();
                }
                newCaptureIndicators.Add(captureIndicator);
            }
        }
        while (recentMoveIndicators.Count >= playerNames.Length) {
            Destroy(recentMoveIndicators.Dequeue());
        }
        if (added) {
            sfxClack.Play();
        }
        if (removed) {
            sfxCapture.PlayDelayed(.15f);
        }
        if (added || removed) {
            foreach (GameObject go in captureIndicators) {
                Destroy(go);
            }
            if (newCaptureIndicators != null) {
                captureIndicators = newCaptureIndicators;
            }
        }
        // Alliance adjacency indicators.
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                byte thisPlayer = GetPlayerAt(x, y);
                foreach (bool horizontal in new bool[] { false, true }) {
                    if (horizontal && x == width - 1) {
                        continue;
                    }
                    if (!horizontal && y == height - 1) {
                        continue;
                    }
                    byte adjPlayer = GetPlayerAt(x + (horizontal ? 1 : 0), y + (horizontal ? 0 : 1));
                    bool shouldExist = thisPlayer != NO_PLAYER && adjPlayer != NO_PLAYER && thisPlayer != adjPlayer && GetAlliance(thisPlayer, adjPlayer);
                    Tuple<int, int, bool> tuple = new Tuple<int, int, bool>(x, y, horizontal);
                    // Add missing indicators.
                    if (shouldExist && !allianceAdjacencyIndicators.ContainsKey(tuple)) {
                        GameObject indicator = Instantiate(allianceAdjacencyIndicatorPrefab, transform);
                        Vector3 pos = GetCoor(x, y);
                        if (horizontal) {
                            pos.x += .5f;
                        } else {
                            pos.y -= .5f;
                        }
                        pos.z = -1;
                        indicator.transform.localPosition = pos;
                        allianceAdjacencyIndicators[tuple] = indicator;
                    }
                    // Remove old indicators.
                    else if (!shouldExist && allianceAdjacencyIndicators.ContainsKey(tuple)) {
                        Destroy(allianceAdjacencyIndicators[tuple]);
                        allianceAdjacencyIndicators.Remove(tuple);
                    }
                }
            }
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
        return PUNManager.hotseatMode || name.Equals(playerNames[currentPlayerIndex], StringComparison.OrdinalIgnoreCase);
    }
    public bool IAmPlayer(string name) {
        return PUNManager.hotseatMode || PhotonNetwork.LocalPlayer.NickName.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
    public bool CanTakeMainAction(string name) {
        return IsCurrentPlayer(name) && allianceRequest == -1;
    }
    void AdvanceCurrentPlayer() {
        currentPlayerIndex = (byte)((currentPlayerIndex + 1) % playerNames.Length);
        allianceRequest = -1;
        if (TEST_FLAG) {
            PhotonNetwork.LocalPlayer.NickName = playerNames[currentPlayerIndex];
        }
    }

    public bool GetAlliance(int one, int two) {
        return alliances[one * playerNames.Length + two];
    }
    void SetAlliance(int one, int two, bool allied) {
        alliances[one * playerNames.Length + two] = allied;
        alliances[two * playerNames.Length + one] = allied;
    }
    [PunRPC]
    public void RequestAlliance(int index, PhotonMessageInfo info) {
        if (!PUNManager.hotseatMode && !CanTakeMainAction(info.Sender.NickName)) {
            return;
        }
        allianceRequest = index;
        BroadcastLog(string.Format("{0} sent an alliance request.", playerNames[currentPlayerIndex]));
        GameLog.StaticMGG(string.Format("REQ{0} {1}", currentPlayerIndex + 1, allianceRequest + 1));
        if (TEST_FLAG) {
            PhotonNetwork.LocalPlayer.NickName = playerNames[allianceRequest];
        }
    }
    [PunRPC]
    public void RespondToAllianceRequest(bool yes, PhotonMessageInfo info) {
        if (!PUNManager.hotseatMode) {
            int senderIndex = Array.FindIndex(playerNames, t => t.Equals(info.Sender.NickName, StringComparison.InvariantCultureIgnoreCase));
            if (senderIndex != allianceRequest) {
                return;
            }
        }
        if (yes) {
            SetAlliance(currentPlayerIndex, allianceRequest, true);
            BroadcastLog(string.Format("{0} is now allied with {1}!", playerNames[currentPlayerIndex], playerNames[allianceRequest]));
        } else {
            BroadcastLog(string.Format("{0}'s alliance request was denied.", playerNames[currentPlayerIndex]));
        }
        GameLog.StaticMGG(yes ? "YES" : "NO");
        AdvanceCurrentPlayer();
    }
    [PunRPC]
    public void BreakAlliance(int index, PhotonMessageInfo info) {
        if (!PUNManager.hotseatMode && !IsCurrentPlayer(info.Sender.NickName)) {
            return;
        }
        if (allianceRequest >= 0) {
            return;
        }
        SetAlliance(currentPlayerIndex, index, false);
        BrokenAllianceKillCheck(currentPlayerIndex, index);
        ManualUpdate(); // Photon doesn't check array contents to trigger serialization.
        BroadcastLog(string.Format("{0} broke their alliance with {1}!", playerNames[currentPlayerIndex], playerNames[index]));
        GameLog.StaticMGG(string.Format("BRK{0} {1}", currentPlayerIndex + 1, index + 1));
    }
    void BrokenAllianceKillCheck(int one, int two) {
        HashSet<Tuple<int, int>> seen = new HashSet<Tuple<int, int>>();
        List<Tuple<int, int>> toKill = new List<Tuple<int, int>>();
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Tuple<int, int> coor = new Tuple<int, int>(x, y);
                byte player = GetPlayerAt(coor);
                if (player == NO_PLAYER) {
                    continue;
                }
                if (seen.Contains(coor)) {
                    continue;
                }
                KillResult killResult = CheckGroupKill(coor.Item1, coor.Item2, true);
                seen.UnionWith(killResult.seen);
                if (killResult.kill) {
                    // The current player gets credit for all captures except of their own stones,
                    // which go to their former ally.
                    captures[GetPlayerAt(coor) == one ? two : one] += killResult.seen.Count;
                    toKill.AddRange(killResult.seen);
                }
            }
        }
        if (toKill.Count > 0) {
            ExecuteKillResult(new KillResult(toKill, true));
        }
    }
    [PunRPC]
    public void Pass(PhotonMessageInfo info) {
        if (!PUNManager.hotseatMode && !IsCurrentPlayer(info.Sender.NickName)) {
            return;
        }
        if (allianceRequest >= 0) {
            return;
        }
        BroadcastLog(string.Format("{0} passes.", playerNames[currentPlayerIndex]));
        AdvanceCurrentPlayer();
    }

    void BroadcastLog(String line) {
        if (PUNManager.hotseatMode) {
            Log(line);
        } else {
            photonView.RPC("Log", RpcTarget.All, line);
        }
    }
    [PunRPC]
    public void Log(string line) {
        GameLog.Static(line);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(manualUpdate);
            stream.SendNext(playerNames);
            stream.SendNext(currentPlayerIndex);
            stream.SendNext(width);
            stream.SendNext(height);
            stream.SendNext(stones);
            stream.SendNext(captures);
            stream.SendNext(alliances);
            stream.SendNext(allianceRequest);
        } else {
            manualUpdate = (bool)stream.ReceiveNext();
            playerNames = (string[])stream.ReceiveNext();
            currentPlayerIndex = (byte)stream.ReceiveNext();
            width = (int)stream.ReceiveNext();
            height = (int)stream.ReceiveNext();
            stones = (byte[])stream.ReceiveNext();
            captures = (int[])stream.ReceiveNext();
            alliances = (bool[])stream.ReceiveNext();
            allianceRequest = (int)stream.ReceiveNext();
        }
    }
    void ManualUpdate() {
        manualUpdate = !manualUpdate;
    }
}

struct KillResult {
    public ICollection<Tuple<int, int>> seen;
    public bool kill;

    public KillResult(ICollection<Tuple<int, int>> seen, bool kill = false) {
        this.seen = seen;
        this.kill = kill;
    }
}