using UnityEngine;

public class PivotPainter2ShaderSetup : MonoBehaviour
{
    public GameObject player;
    public float playerSizes;

    private Vector3 _lastPosition;

    [SerializeField]
    private Vector3 _speed;

    public Vector3 Speed { get { return _speed; } }


    public Vector3 WindDirection;
    public float WindPower;
    public Material[] materials;

    private Material[] _materials;

    void Start()
    {
        WindDirection = new Vector3(1, 0, 0);
        WindPower = 0;

        _lastPosition = player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        _speed = (player.transform.position - _lastPosition) / Time.deltaTime;
        _lastPosition = player.transform.position;

        //WindDirection = Vector3.Normalize(WindDirection);

        foreach (Material mat in materials)
        {
            mat.SetVector("_WindDir", WindDirection);
            mat.SetFloat("_ForcePower", Vector3.Distance(_speed, Vector3.zero) * 0.005f);

            mat.SetVector("_PlayerPos", player.transform.position);
            //mat.SetFloat("_ForcePower", WindPower);
        }
    }
}
