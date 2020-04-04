using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeIn : MonoBehaviour
{
    public Image image;

    void Start()
    {
        if (!Application.isEditor) {
            image.color = Color.black;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Color c = image.color;
        c.a -= .1f;
        if (c.a <= 0) {
            Destroy(gameObject);
            return;
        }
        image.color = c;
    }
}
