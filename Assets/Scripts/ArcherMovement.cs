using UnityEngine;

public class ArcherMovement : MonoBehaviour
{
    public Animator animator;
    public float RunSpeed = 1.0f;
    public float SprintScale = 1.5f;
    public float TurnSpeed = 10.0f;

    private float _xMovement = 0.0f;
    private float _zMovement = 0.0f;

    private Vector3 _xDirection = new Vector3(1, 0, 0);
    private Vector3 _zDirection = new Vector3(0, 0, 1);

    private float _speed;
    public float Speed { get { return _speed; } }

    private bool _sprint = false;
    public bool Sprint { get { return _sprint; } }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            _xMovement -= 1.0f;
        }

        if (Input.GetKeyUp(KeyCode.A))
        {
            _xMovement += 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            _xMovement += 1.0f;
        }

        if (Input.GetKeyUp(KeyCode.D))
        {
            _xMovement -= 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            _zMovement += 1.0f;
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            _zMovement -= 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            _zMovement -= 1.0f;
        }

        if (Input.GetKeyUp(KeyCode.S))
        {
            _zMovement += 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            _sprint = true;

            animator.speed = SprintScale;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            _sprint = false;

            animator.speed = 1.0f;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetBool("Attack", true);
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            animator.SetBool("Attack", false);
        }

        if (_xMovement != 0.0f || _zMovement != 0.0f)
        {
            animator.SetBool("Move", true);
            Move();
        }
        else
        {
            _speed = 0.0f;
            animator.SetBool("Move", false);
        }
    }

    private void Move()
    {
        Quaternion ToRotation = Quaternion.LookRotation(new Vector3(_xMovement, 0, _zMovement));
        gameObject.transform.rotation = Quaternion.Lerp(gameObject.transform.rotation, ToRotation, Time.deltaTime * TurnSpeed);

        _speed = RunSpeed;

        if (_sprint)
        {
            _speed *= SprintScale;
        }

        gameObject.transform.position += _speed * gameObject.transform.forward * Time.deltaTime;
    }
}
