using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocalPlayerInput : MonoBehaviour
{
    public TMP_InputField nameInput;

    void Start() {
        nameInput.ActivateInputField();
    }

    public string GetName() {
        return nameInput.text;
    }
}
