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
    public override void OnPlayerEnteredRoom(Player newPlayer) {
        UpdateList();
    }
    public override void OnPlayerLeftRoom(Player otherPlayer) {
        UpdateList();
    }
    void UpdateList()
    {
        tmp.text = "Players waiting:\n" + string.Join("\n", PhotonNetwork.PlayerList.Select(p => p.NickName));
    }
}
