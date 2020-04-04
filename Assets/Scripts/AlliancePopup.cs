using Assets.Code;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AlliancePopup : MonoBehaviour
{
    private LayerMask layerMaskAlliancePopupButton;
    private Camera cam;

    public CanvasGroup canvasGroup;
    public Collider2D[] colliders;
    public GameObject indicator;
    public TextMeshProUGUI tmp;

    Board board;

    // Start is called before the first frame update
    void Start()
    {
        layerMaskAlliancePopupButton = LayerMask.GetMask("AlliancePopupButton");
        cam = Camera.main;

        canvasGroup.alpha = 0;
        indicator.SetActive(false);
    }
    public void Set(Board board) {
        this.board = board;
        tmp.text = string.Format("<color=#{0}><b>{1}</b></color> has offered you an alliance. Accept?", ColorUtility.ToHtmlStringRGB(Board.PLAYER_COLORS[board.currentPlayerIndex]), board.playerNames[board.currentPlayerIndex]);
    }

    // Update is called once per frame
    void Update()
    {
        canvasGroup.alpha += .1f;
        Collider2D mouseCollider = Util.GetMouseCollider(cam, layerMaskAlliancePopupButton);
        indicator.SetActive(mouseCollider != null);
        if (mouseCollider != null) {
            indicator.transform.localPosition = mouseCollider.transform.localPosition;
            indicator.transform.Rotate(0, 0, -.5f);
            if (Input.GetMouseButtonDown(0)) {
                board.photonView.RPC("RespondToAllianceRequest", Photon.Pun.RpcTarget.MasterClient, mouseCollider == colliders[0]);
            }
        }
    }
}