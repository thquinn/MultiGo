using Assets.Code;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBubbles : MonoBehaviourPunCallbacks
{
    static Vector3 HOVER_SCALE = new Vector3(1.1f, 1.1f, 1);

    private LayerMask layerMaskPlayerBubble;
    Camera cam;

    public GameObject playerBubblePrefab, boardLinePrefab;
    public GameObject currentPlayerIndicator;

    Board board;
    List<Collider2D> colliders;
    TextMeshProUGUI[] captureTexts;
    Dictionary<Tuple<int, int>, GameObject> allianceMarkers;

    void Start() {
        layerMaskPlayerBubble = LayerMask.GetMask("PlayerBubble");
        cam = Camera.main;
    }

    public void SetBoard(Board board)
    {
        this.board = board;
        colliders = new List<Collider2D>();
        captureTexts = new TextMeshProUGUI[board.playerNames.Length];
        float d = 110;
        for (int i = 0; i < board.playerNames.Length; i++) {
            float angle = Mathf.PI / 2 - 2 * i * Mathf.PI / board.playerNames.Length;
            GameObject playerBubble = Instantiate(playerBubblePrefab, transform);
            playerBubble.transform.localPosition = new Vector3(Mathf.Cos(angle) * d, Mathf.Sin(angle) * d);
            playerBubble.transform.GetChild(1).GetComponent<Image>().color = Board.PLAYER_COLORS[i];
            playerBubble.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = board.playerNames[i];
            colliders.Add(playerBubble.GetComponent<Collider2D>());
            captureTexts[i] = playerBubble.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
            if (board.IAmPlayer(board.playerNames[i])) {
                playerBubble.transform.GetChild(0).gameObject.SetActive(true);
            }
        }
        currentPlayerIndicator.transform.SetAsLastSibling();
        allianceMarkers = new Dictionary<Tuple<int, int>, GameObject>();
    }

    void Update()
    {
        currentPlayerIndicator.transform.localPosition = colliders[board.currentPlayerIndex].transform.localPosition;
        UpdateCaptureTexts();
        UpdateAllianceMarkers();
        UpdateInput();
    }
    void UpdateCaptureTexts() {
        if (captureTexts == null || captureTexts.Length == 0) {
            return;
        }
        for (int i = 0; i < captureTexts.Length; i++) {
            captureTexts[i].text = board.captures[i].ToString();
        }
    }
    void UpdateAllianceMarkers() {
        for (int one = 0; one < board.playerNames.Length - 1; one++) {
            for (int two = one + 1; two < board.playerNames.Length; two++) {
                Tuple<int, int> tuple = new Tuple<int, int>(one, two);
                bool contains = allianceMarkers.ContainsKey(tuple);
                bool allied = board.GetAlliance(one, two);
                // Add missing markers.
                if (!contains && allied) {
                    Vector3 p1 = colliders[one].transform.localPosition;
                    Vector3 p2 = colliders[two].transform.localPosition;
                    // Bring line edges closer to center.
                    p1 *= .8f;
                    p2 *= .8f;
                    Vector3 d = p2 - p1;
                    GameObject allianceMarker = Instantiate(boardLinePrefab, transform);
                    Vector3 localPosition = (p1 + p2) / 2;
                    localPosition.z = 1;
                    allianceMarker.transform.localPosition = localPosition;
                    allianceMarker.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    allianceMarker.transform.localScale = new Vector3(d.magnitude * 100, 600);
                    allianceMarkers[tuple] = allianceMarker;
                }
                // Remove old markers.
                if (contains && !allied) {
                    Destroy(allianceMarkers[tuple]);
                    allianceMarkers.Remove(tuple);
                }
            }
        }
    }
    void UpdateInput() {
        Collider2D mouseCollider = board.IsCurrentPlayer(PhotonNetwork.LocalPlayer.NickName) ? Util.GetMouseCollider(cam, layerMaskPlayerBubble) : null;
        if (mouseCollider == colliders[board.currentPlayerIndex]) {
            mouseCollider = null;
        }
        foreach (Collider2D collider in colliders) {
            collider.transform.localScale = Vector3.Lerp(collider.transform.localScale, mouseCollider == collider ? HOVER_SCALE : Vector3.one, .1f);
        }
        if (mouseCollider == null) {
            return;
        }
        if (Input.GetMouseButtonDown(0)) {
            int colliderIndex = colliders.IndexOf(mouseCollider);
            bool allied = board.GetAlliance(board.currentPlayerIndex, colliderIndex);
            if (board.playerNames.Length == 2) {
                GameLog.Static("You can't request an alliance in a two-player game.");
            } else if (allied) {
                board.photonView.RPC("BreakAlliance", RpcTarget.MasterClient, colliderIndex);
            } else {
                board.photonView.RPC("RequestAlliance", RpcTarget.MasterClient, colliderIndex);
            }
        }
    }

    // PUN callbacks.
    public override void OnPlayerEnteredRoom(Player player) {
        GameLog.Static(string.Format("{0} entered the room.", player.NickName));
    }
    public override void OnPlayerLeftRoom(Player player) {
        GameLog.Static(string.Format("{0} left the room.", player.NickName));
    }
}
