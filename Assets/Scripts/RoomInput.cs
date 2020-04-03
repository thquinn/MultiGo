﻿using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomInput : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TMP_InputField roomInput, nameInput;

    void Start() {
        roomInput.onValidateInput = ValidateRoom;
        nameInput.onValidateInput = ValidateName;
        roomInput.onSubmit.AddListener(Next);
        nameInput.onSubmit.AddListener(Submit);

        roomInput.ActivateInputField();
    }
    void Update()
    {
        canvasGroup.alpha += .05f;
        if (Input.GetKeyDown(KeyCode.Tab)) {
            if (roomInput.isFocused) {
                nameInput.ActivateInputField();
            } else {
                roomInput.ActivateInputField();
            }
        }
    }

    char ValidateRoom(string text, int charIndex, char addedChar) {
        if (text.Length >= 4) {
            return '\0';
        }
        if (char.IsLetter(addedChar)) {
            return char.ToUpper(addedChar);
        }
        return '\0';
    }
    char ValidateName(string text, int charIndex, char addedChar) {
        if (text.Length >= 8) {
            return '\0';
        }
        return addedChar;
    }
    void Next(string s) {
        nameInput.ActivateInputField();
    }
    void Submit(string s) {
        string room = roomInput.text, name = nameInput.text;
        if (room.Length == 0 || name.Length == 0) {
            return;
        }
        PhotonNetwork.NickName = name;
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.PublishUserId = true;
        PhotonNetwork.JoinOrCreateRoom(room, roomOptions, TypedLobby.Default);
        GameLog.Static(string.Format("Joining room {0}...", room));
    }
}
