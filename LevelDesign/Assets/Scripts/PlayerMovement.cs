using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Variables
    [Header("Movement")]
    public float _force = 10f;
    public float _walkSpeed = 10f;
    public float _sprintSpeed = 20f;
    public float _groundDrag = 5f;
    private float _moveSpeed;

    [Header("Jumping")]
    public float _jumpForce = 20f;
    public float _jumpCooldown = 0.25f;
    public float _airMultiplier = 1.1f;
    private bool _readyToJump;

    [Header("Crounching")]
    public float _crouchSpeed = 3.5f;
    public float _crouchYScale = 0.5f;
    private float _startYScale;

    [Header("Keybinds")]
    public KeyCode _jumpKey = KeyCode.Space;
    public KeyCode _sprintKey = KeyCode.LeftShift;
    public KeyCode _crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float _playerHeight;
    public LayerMask _whatIsGround;
    bool _grounded;

    [Header("Slope Handling")]
    public float _maxSlopeAngle = 40f;
    private RaycastHit _slopeHit;
    private bool _existingSlop;

    [Header("Movement")]
    public MovementState _state;
    public enum MovementState
    {
        Walking,
        Sprinting,
        Crouching,
        Air
    }

    public Transform _orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 _moveDirection;

    Rigidbody _rb;
    // Start is called before the first frame update
    #endregion

    #region Mono Methods

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        ResetJump();
        _startYScale = transform.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        _grounded = Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.2f, _whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        if (_grounded)
        {
            _rb.drag = _groundDrag;
        }
        else
        {
            _rb.drag = 0;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    #endregion

    #region Class Methods
    private void MyInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        if (Input.GetKey(_jumpKey) && _readyToJump && _grounded)
        {
            _readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump), _jumpCooldown);
        }

        if (Input.GetKeyDown(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _crouchYScale, transform.localScale.z);
            _rb.AddForce(Vector3.down * _force, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(_crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, _startYScale, transform.localScale.z);
        }
    }
    private void StateHandler()
    {
        if (Input.GetKey(_crouchKey))
        {
            _state = MovementState.Crouching;
            _moveSpeed = _crouchSpeed;
        }

        if (_grounded && Input.GetKey(_sprintKey))
        {
            _state = MovementState.Sprinting;
            _moveSpeed = _sprintSpeed;
        }

        else if (_grounded)
        {
            _state = MovementState.Walking;
            _moveSpeed = _walkSpeed;
        }

        else
        {
            _state = MovementState.Air;
        }
    }

    private void MovePlayer()
    {
        _moveDirection = _orientation.forward * verticalInput + _orientation.right * horizontalInput;

        if (OnSlope() && !_existingSlop)
        {
            _rb.AddForce(GetSlopeMoveDirection() * _moveSpeed * _force, ForceMode.Force);

            if (_rb.velocity.y > 0)
            {
                _rb.AddForce(Vector3.down * _force, ForceMode.Force);
            }
        }
        else if (_grounded)
        {
            _rb.AddForce(_moveDirection.normalized * _moveSpeed * _force, ForceMode.Force);
        }
        else if(!_grounded)
        {
            _rb.AddForce(_moveDirection.normalized * _moveSpeed * _force * _airMultiplier, ForceMode.Force);
        }

        _rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (OnSlope() && !_existingSlop)
        {
            if (_rb.velocity.magnitude > _moveSpeed)
                _rb.velocity = _rb.velocity.normalized * _moveSpeed * _force;
        }

        else
        {
            Vector3 flatVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

            if (flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed * _force;
                _rb.velocity = new Vector3(limitedVel.x, _rb.velocity.y, limitedVel.z);

            }
        }

    }

    private void Jump()
    {
        _existingSlop = true;

        _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        _rb.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        _readyToJump = true;
        _existingSlop = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, _playerHeight * 0.5f + 0.3f, _whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;
    }
    #endregion

    private void OnDrawGizmos()
    {
        // Dessiner une sphère pour représenter la vérification du sol
        Gizmos.color = _grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * (_playerHeight * 0.5f), 0.2f);

        // Dessiner un rayon pour montrer la direction de la détection du sol
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (_playerHeight * 0.5f + 0.2f));

        // Dessiner la direction du mouvement
        if (_moveDirection != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + _moveDirection);
        }

        // Dessiner une indication visuelle de la pente détectée
        if (OnSlope()&& !_existingSlop)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + GetSlopeMoveDirection() * 2f);
        }
    }
}
