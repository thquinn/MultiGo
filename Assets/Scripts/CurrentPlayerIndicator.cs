using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentPlayerIndicator : MonoBehaviour
{
    public Image image;

    int frames;

    void Update()
    {
        transform.Rotate(0, 0, -1.4f);

        frames++;
        bool onCycle = (frames / 180) % 2 == 1;
        float t = Mathf.Clamp01(((frames % 180) - 150) / 30f);
        image.color = Color.Lerp(onCycle ? Color.black : Color.white, onCycle ? Color.white : Color.black, t);
    }
}
