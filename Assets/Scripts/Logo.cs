using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Logo : MonoBehaviour
{
    public GameObject connector;
    public TextMeshProUGUI versionTMP;

    int frames = 0;

    void Start() {
        versionTMP.text = "v" + Application.version.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        frames++;
        int m = frames % 240;
        float angle;
        if (m < 210) {
            angle = Mathf.SmoothStep(45, 0, (m - 180) / 30f);
        } else {
            angle = (Mathf.SmoothStep(0, -45, (m - 210) / 30f));
        }
        connector.transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
