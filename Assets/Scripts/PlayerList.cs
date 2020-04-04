﻿using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerList : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI tmp;

    void Start() {
        UpdateList();
    }
    public override void OnPlayerEnteredRoom(Player player) {
        UpdateList();
    }
    public override void OnPlayerLeftRoom(Player player) {
        UpdateList();
    }
    public override void OnPlayerPropertiesUpdate(Player player, ExitGames.Client.Photon.Hashtable changedProps) {
        UpdateList();
    }
    void UpdateList()
    {
        string[] nicks = PhotonNetwork.PlayerList.Select(p => p.NickName).ToArray();
        tmp.text = "Players waiting:\n" + string.Join("\n", nicks);
        if (PhotonNetwork.IsMasterClient) {
            if (nicks.Length <= 1) {
                tmp.text += "\n\n<size=60%>Waiting for additional players...";
            } else if (nicks.Length <= Board.PLAYER_COLORS.Length) {
                tmp.text += "\n\n<size=60%>Press Enter to start the game with these players.";
            } else {
                tmp.text += string.Format("\n\n<size=60%>Press Enter to start the game with the first {0} players.", Board.PLAYER_COLORS.Length);
            }
        }
    }
}
