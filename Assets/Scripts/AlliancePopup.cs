﻿using Assets.Code;
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
        Color color = Color.Lerp(Board.PLAYER_COLORS[board.currentPlayerIndex], Color.black, 0);
        tmp.text = string.Format("<color=#{0}><b>{1}</b></color> has offered you an alliance. Accept?", ColorUtility.ToHtmlStringRGB(color), board.playerNames[board.currentPlayerIndex]);
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
                if (PUNManager.hotseatMode) {
                    board.RespondToAllianceRequest(mouseCollider == colliders[0], new Photon.Pun.PhotonMessageInfo());
                } else {
                    board.photonView.RPC("RespondToAllianceRequest", Photon.Pun.RpcTarget.MasterClient, mouseCollider == colliders[0]);
                }
            }
        }
    }
}