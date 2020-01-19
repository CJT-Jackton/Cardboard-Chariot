using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LeafShaderSetup2 : MonoBehaviour
{
    private Material _material;

    // Start is called before the first frame update
    void Start()
    {
        _material = new Material(GetComponent<SkinnedMeshRenderer>().sharedMaterials[1]);
        GetComponent<SkinnedMeshRenderer>().materials = new Material[] { GetComponent<SkinnedMeshRenderer>().sharedMaterial, _material };
    }

    void SetPlayerDirection(Vector3 Direction)
    {
        _material.SetVector("Vector3_8599735A", Direction);
        _material.SetFloat("Vector1_BA1297B3", Time.time);
    }

    void OnTriggerStay(Collider collider)
    {
        CollideTree collideTree = collider.gameObject.GetComponent<CollideTree>();

        if (collideTree && collideTree.Speed != Vector3.zero)
        {
            SetPlayerDirection(collideTree.Speed);
        }
    }
}
