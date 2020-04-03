using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class GameLog : MonoBehaviour
{
    public static GameLog instance;
    static float FADE = .34f;

    public TextMeshProUGUI tmp;

    List<string> lines;
    StringBuilder stringBuilder;

    void Start() {
        instance = this;
        tmp.text = "";
        lines = new List<string>();
        stringBuilder = new StringBuilder();
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
        tmp.text = stringBuilder.ToString();
    }
}
