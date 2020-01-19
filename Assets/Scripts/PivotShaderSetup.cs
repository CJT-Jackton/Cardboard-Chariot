///
/// Setting up shader properties of pivot painter2.
///

using UnityEngine;

[RequireComponent(typeof(Collider), typeof(MeshRenderer))]
public class PivotShaderSetup : MonoBehaviour
{
    private float Falloff = 3.9f;

    private Material[] _materials;
    private Vector3 _center;

    private Vector3 _playerPos;
    private Vector3 _playerDir;
    private float _speed = 0.0f;

    private Vector3 _forceDir;
    private float _forcePower = 0.0f;

    private float fallOff;
    private float forcePower;

    // Start is called before the first frame update
    void Start()
    {
        SphereCollider collider = GetComponent<SphereCollider>();
        _center = transform.position + collider.center;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        _materials = new Material[meshRenderer.sharedMaterials.Length];

        for (int i = 0; i < _materials.Length; ++i)
        {
            _materials[i] = new Material(meshRenderer.sharedMaterials[i]);
        }

        meshRenderer.materials = _materials;
    }

    void SphereMask(Vector3 Coords, Vector3 Center, float Radius, float Hardness, out float Out)
    {
        Out = 1 - Mathf.Clamp((Vector3.Distance(Coords, Center) - Radius) / (1 - Hardness), 0.0f, 1.0f);
    }

    void SetShaderParameters()
    {
        if (forcePower > 0.0f)
        {
            foreach (Material material in _materials)
            {
                material.SetVector("_PlayerPos", _playerPos);
                material.SetVector("_ForceDir", -_forceDir);
                if (forcePower > _forcePower)
                    material.SetFloat("_ForcePower", _forcePower);
                material.SetFloat("_TimeSinceLastTouch", Time.time);
            }
        }
    }

    void OnTriggerStay(Collider collider)
    {
        ArcherMovement movement = collider.gameObject.GetComponent<ArcherMovement>();

        if (movement)
        {
            _playerPos = collider.gameObject.transform.position;
            _playerDir = collider.gameObject.transform.forward;

            _speed = movement.Speed;
            _speed = movement.Sprint ? _speed * movement.SprintScale : _speed;

            _forceDir = _center - _playerPos;

            fallOff = Mathf.Pow(Mathf.Clamp(0.0f, 1.0f, (3.9f - Vector3.Magnitude(_forceDir)) / Falloff), 8.0f);

            forcePower = Mathf.Max(Vector3.Dot(_playerDir, Vector3.Normalize(_center - _playerPos)), 0.0f) * _speed / 4.0f * fallOff;

            if (forcePower > _forcePower)
            {
                _forcePower = Mathf.SmoothStep(_forcePower, forcePower, 10.1f * Time.deltaTime);
            }
            else
            {
                _forcePower = Mathf.SmoothStep(_forcePower, forcePower, 8.0f * Time.deltaTime);
            }

            SetShaderParameters();
        }
    }
}
