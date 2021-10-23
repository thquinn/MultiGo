using Assets.Code;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;

public class GameLog : MonoBehaviour
{
    public static GameLog instance;
    static float FADE = .34f;

    private Camera cam;
    private LayerMask layerMaskBoardIntersection;

    public TextMeshProUGUI logTMP, gridCoorTMP, roomTMP;

    Board board;
    List<string> lines;
    StringBuilder stringBuilder;
    Guid localGuid;

    void Start() {
        cam = Camera.main;
        layerMaskBoardIntersection = LayerMask.GetMask("BoardIntersection");

        instance = this;
        logTMP.text = "";
        lines = new List<string>();
        stringBuilder = new StringBuilder();
        localGuid = Guid.NewGuid();
    }
    public static void Associate(Board board) {
        instance.board = board;
    }
    void Update() {
        if (board == null) {
            return;
        }
        Collider2D collider = Util.GetMouseCollider(cam, layerMaskBoardIntersection);
        gridCoorTMP.text = collider == null ? "" : Util.GetMGGCoorFromIndex(board.width, board.height, board.gridColliders[collider]);
        if (!PUNManager.hotseatMode) {
            roomTMP.text = "Room: " + PhotonNetwork.CurrentRoom.Name;
        }
    }

    public static void Static(string line) {
        instance.Log(line);
    }
    public void Log(string line) {
        lines.Add(string.Format("({0}) {1}", DateTime.Now.ToString("h:mm tt"), line));
        if (lines.Count > 10) {
            lines.RemoveAt(0);
        }
        stringBuilder.Clear();
        float alpha = Mathf.Min(FADE * (11 - lines.Count), 1);
        foreach (string l in lines) {
            stringBuilder.AppendLine(string.Format("<alpha=#{0}>{1}", Mathf.CeilToInt(alpha * 255).ToString("X"), l));
            alpha = Mathf.Min(alpha + FADE, 1);
        }
        logTMP.text = stringBuilder.ToString();
    }

    public static void StaticMGG(string line) {
        instance.AddToGameLog(line);
    }
    public void AddToGameLog(string line) {
        string guid;
        if (PUNManager.hotseatMode) {
            guid = localGuid.ToString();
        } else {
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("guid")) {
                return;
            }
            guid = (string)PhotonNetwork.CurrentRoom.CustomProperties["guid"];
        }
        string path = string.Format("{0}/{1}.mgg", Application.persistentDataPath, guid);
        using(StreamWriter sw = File.AppendText(path)) {
            sw.WriteLine(line);
        }
    }
}
