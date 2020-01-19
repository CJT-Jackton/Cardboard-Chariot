using UnityEngine;

public class LeafShaderSetup : MonoBehaviour
{
    public GameObject Player;
    public Material[] materials;
    public float PlayerSize;

    // Update is called once per frame
    void Update()
    {
        foreach (Material material in materials)
        {
            material.SetVector("_PlayerPos", Player.transform.position);
            material.SetFloat("_PlayerSize", PlayerSize);
        }
    }
}
