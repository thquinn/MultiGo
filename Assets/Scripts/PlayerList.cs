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
        if (PUNManager.hotseatMode) {
            UpdateListHotseat();
            return;
        }
        string[] nicks = PhotonNetwork.PlayerList.Select(p => p.NickName).ToArray();
        tmp.text = string.Format("Players waiting in room {0}:\n{1}", PhotonNetwork.CurrentRoom.Name, string.Join("\n", nicks));
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
    public void UpdateListHotseat() {
        List<string> lines = new List<string>();
        if (PUNManager.localPlayers.Count < Board.PLAYER_COLORS.Length) {
            lines.Add("Press F1 to add a player.");
        }
        lines.Add(string.Join("\n", PUNManager.localPlayers));
        if (PUNManager.localPlayers.Count > 1) {
            lines.Add("\n\n<size=60%>Press Enter to start the game with these players.");
        }
        tmp.text = string.Join("\n", lines);
    }
}
