using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CurrentPlayerIndicator : MonoBehaviour
{
    public Image image;

    void Update()
    {
        transform.Rotate(0, 0, -1.4f);
    }
}
