using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Initializer : MonoBehaviour
{

    void Start()
    {
        ModalWindow.Instance.Touch();
        NetworkManager.Instance.Touch();
        GameLogic.Instance.Touch();
    }
}
