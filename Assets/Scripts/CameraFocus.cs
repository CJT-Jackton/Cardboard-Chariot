using UnityEngine;

public class CameraFocus : MonoBehaviour
{
    public GameObject focusObject;

    private Vector3 Offset;

    void Start()
    {
        Offset = transform.position - focusObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = focusObject.transform.position + Offset;
    }
}
