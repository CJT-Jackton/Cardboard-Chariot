using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollideTree : MonoBehaviour
{
    private List<GameObject> leaves = new List<GameObject>();

    private Vector3 _lastPosition;

    [SerializeField]
    private Vector3 _speed;

    public Vector3 Speed { get { return _speed; } }

    // Update is called once per frame
    void Update()
    {
        _speed = (transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = transform.position;

        foreach(GameObject leaf in leaves)
        {
            //leaf.GetComponent<LeafShaderSetup2>().SetPlayerDirection(_speed);
        }
    }

    /*
    void OnCollisionEnter(Collision collision)
    {
        LeafShaderSetup2 l = collision.gameObject.GetComponent<LeafShaderSetup2>();

        if (l)
        {
            Debug.Log("Collided!");
            leaves.Add(collision.gameObject);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        leaves.Remove(collision.gameObject);
    }*/
}
