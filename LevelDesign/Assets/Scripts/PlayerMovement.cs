using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Variables
    [Header("Movement")]
    public float _walkSpeed = 50f;
    public float _sprintSpeed = 100f;
    public float _groundDrag = 5f;
    private float _moveSpeed;

    [Header("Jumping")]
    public float _jumpForce = 200f;
    public float _jumpCooldown = 0.25f;
    public float _airMultiplier = 0.4f;
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

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 _moveDirection;

    Rigidbody rb;
    // Start is called before the first frame update
    #endregion

    #region Mono Methods

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        ResetJump();
        _startYScale = transform.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        _grounded = Physics.Raycast(transform.position, Vector3.down, _playerHeight * 0.5f + 0.2f, _whatIsGround);

        MyInput();
        SpeedControl();
        if (_grounded)
        {
            rb.drag = _groundDrag;
        }
        else
        {
            rb.drag = 0;
        }
        StateHandler();
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
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
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
        _moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (OnSlope() && !_existingSlop)
        {
            rb.AddForce(GetSlopeMoveDirection() * _moveSpeed * 20f, ForceMode.Force);

            if(rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        if (_grounded)
        {
            rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        else if(!_grounded)
        {
            rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f * _airMultiplier, ForceMode.Force);
        }

        rb.useGravity = !OnSlope();
    }

    private void SpeedControl()
    {
        if (OnSlope() && !_existingSlop)
        {
            if (rb.velocity.magnitude > _moveSpeed)
                rb.velocity = rb.velocity.normalized * _moveSpeed;
        }

        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);

            }
        }

    }

    private void Jump()
    {
        _existingSlop = true;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        _readyToJump = true;
        _existingSlop = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, _playerHeight * 0.5f + 0.3f, _whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, hitInfo.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;
    }
    #endregion
}
