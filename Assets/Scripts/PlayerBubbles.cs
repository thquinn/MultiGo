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
    static Vector3 HOVER_SCALE = new Vector3(1.05f, 1.05f, 1), CLICKING_SCALE = new Vector3(.9f, .9f, 1);
    static float PASS_SCALE = .6f;

    private LayerMask layerMaskPlayerBubble;
    Camera cam;

    public GameObject playerBubblePrefab, boardLinePrefab;
    public GameObject currentPlayerIndicator;

    Board board;
    List<Collider2D> colliders;
    List<GameObject> bubbleVisuals;
    List<Material> materials;
    TextMeshProUGUI[] captureTexts;
    Dictionary<Tuple<int, int>, GameObject> allianceMarkers;
    int clickFrames;
    bool clickLockout;

    void Start() {
        layerMaskPlayerBubble = LayerMask.GetMask("PlayerBubble");
        cam = Camera.main;
    }

    public void SetBoard(Board board)
    {
        this.board = board;
        colliders = new List<Collider2D>();
        bubbleVisuals = new List<GameObject>();
        materials = new List<Material>();
        captureTexts = new TextMeshProUGUI[board.playerNames.Length];
        float d = 50 + board.playerNames.Length * 10;
        for (int i = 0; i <= board.playerNames.Length; i++) {
            GameObject playerBubble = Instantiate(playerBubblePrefab, transform);
            colliders.Add(playerBubble.GetComponent<Collider2D>());
            GameObject bubbleVisual = playerBubble.transform.GetChild(0).gameObject;
            bubbleVisuals.Add(bubbleVisual);
            Image bubbleImage = bubbleVisual.transform.GetChild(1).GetComponent<Image>();
            bubbleImage.material = Instantiate(bubbleImage.material);
            materials.Add(bubbleImage.material);
            if (i < board.playerNames.Length) {
                float angle = Mathf.PI / 2 - 2 * i * Mathf.PI / board.playerNames.Length;
                playerBubble.transform.localPosition = new Vector3(Mathf.Cos(angle) * d, Mathf.Sin(angle) * d);
                bubbleVisual.transform.GetChild(1).GetComponent<Image>().color = Board.PLAYER_COLORS[i];
                bubbleVisual.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = board.playerNames[i];
                captureTexts[i] = bubbleVisual.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
                if (board.IAmPlayer(board.playerNames[i])) {
                    bubbleVisual.transform.GetChild(0).gameObject.SetActive(true);
                }
            } else {
                playerBubble.transform.localPosition = new Vector3(-100, 150, 0);
                playerBubble.transform.localScale = new Vector3(PASS_SCALE, PASS_SCALE, 1);
                bubbleVisual.transform.GetChild(0).gameObject.SetActive(true);
                bubbleVisual.transform.GetChild(1).GetComponent<Image>().color = new Color(.9f, .9f, .9f);
                TextMeshProUGUI passText = bubbleVisual.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
                passText.color = Color.black;
                passText.fontSharedMaterial = Instantiate(passText.fontSharedMaterial);
                passText.fontSharedMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0);
                passText.text = "PASS";
                bubbleVisual.transform.GetChild(2).localPosition = Vector3.zero;
                bubbleVisual.transform.GetChild(3).gameObject.SetActive(false);
                bubbleVisual.transform.GetChild(4).gameObject.SetActive(false);
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
        bool canInput = board.CanTakeMainAction(PhotonNetwork.LocalPlayer.NickName);
        Collider2D mouseCollider = canInput ? Util.GetMouseCollider(cam, layerMaskPlayerBubble) : null;
        if (mouseCollider == colliders[board.currentPlayerIndex]) {
            mouseCollider = null;
        }
        for (int i = 0; i < colliders.Count; i++) {
            Collider2D collider = colliders[i];
            
            if (clickFrames > 0 && colliders[i] == mouseCollider) {
                materials[i].SetFloat("_Theta", clickFrames / 60f * 2 * Mathf.PI);
                Vector3 targetScale = Vector3.Lerp(HOVER_SCALE, CLICKING_SCALE, Mathf.Pow(clickFrames / 60f, .25f));
                bubbleVisuals[i].transform.localScale = Vector3.Lerp(bubbleVisuals[i].transform.localScale, targetScale, .2f);
            } else {
                bubbleVisuals[i].transform.localScale = Vector3.Lerp(bubbleVisuals[i].transform.localScale, mouseCollider == collider ? HOVER_SCALE : Vector3.one, .2f);
                materials[i].SetFloat("_Theta", 0);
            }
        }
        if (clickLockout) {
            if (Input.GetMouseButton(0)) {
                return;
            } else {
                clickLockout = false;
            }
        }
        if (mouseCollider != null && Input.GetMouseButton(0)) {
            clickFrames++;
        } else {
            clickFrames = 0;
        }
        if (mouseCollider == null) {
            return;
        }
        if (clickFrames == 60) {
            int colliderIndex = colliders.IndexOf(mouseCollider);
            if (colliderIndex == board.playerNames.Length) {
                // Passing.
                board.photonView.RPC("Pass", RpcTarget.MasterClient);
            } else {
                // Alliance stuff.
                bool allied = board.GetAlliance(board.currentPlayerIndex, colliderIndex);
                if (board.playerNames.Length == 2) {
                    GameLog.Static("No alliances in two-player games!");
                } else if (allied) {
                    board.photonView.RPC("BreakAlliance", RpcTarget.MasterClient, colliderIndex);
                } else {
                    board.photonView.RPC("RequestAlliance", RpcTarget.MasterClient, colliderIndex);
                }
            }
            clickFrames = 0;
            clickLockout = true;
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
