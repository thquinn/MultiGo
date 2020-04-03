using ExitGames.Client.Photon;
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
        tmp.text = "Players waiting:\n" + string.Join("\n", PhotonNetwork.PlayerList.Select(p => p.NickName));
    }
}
