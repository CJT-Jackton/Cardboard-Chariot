using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Visable : MonoBehaviour
{
    public Text text;

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Renderer>().isVisible)
        {
            text.enabled = true;
        }
        else
        {
            text.enabled = false;
        }
    }
}
