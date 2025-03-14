using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum MovementState
{
    Walking,
    Sprinting,
    Crouching,
    Sliding,
    WallRunning,
    Dashing,
    Air
}

public class PlayerMovement : MonoBehaviour
{
    #region Variables
    [Header("Movement")]
    public float _walkSpeed = 7f;
    public float _sprintSpeed = 10f;
    public float _slideSpeed = 30f;
    public float _wallRunSpeed = 15f;
    public float _dashSpeed = 20f;
    public float _dashChangedFactor = 5f;

    public float _maxYSpeed = 4f;
    public float _speedIncreaseMultiplier;
    public float _slopeIncreaseMultiplier;
    public float _groundDrag = 4f;

    [Header("Move Speed")]
    private float _desiredMoveSpeed;
    private float _lastDesiredMoveSpeed;
    private float _moveSpeed;

    [Header("Jumping")]
    public float _jumpForce = 12f;
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
    public float _playerHeight = 2;
    public LayerMask _whatIsGround;
    bool _grounded;

    [Header("Slope Handling")]
    public float _maxSlopeAngle = 40f;
    private RaycastHit _slopeHit;
    private bool _existingSlop;

    [Header("Others")]
    public Transform _playerObj;
    public Transform _orientation;
    public MovementState _state;
    public bool _isCrounch;
    public bool _isSliding;
    public bool _isWallRunning;
    public bool _isDashing;

    private MovementState _lastState;
    private Rigidbody _rb;
    private Vector3 _moveDirection;
    private float _horizontalInput;
    private float _verticalInput;
    private bool _keepMomentum;

    [Header("Gizmos")]
    public bool _showGizmos;
    #endregion

    #region Mono Methods
    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;
        ResetJump();
        _startYScale = _playerObj.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        _grounded = Physics.Raycast(_playerObj.position, Vector3.down, _playerHeight * 0.5f + 0.2f, _whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        if (_state == MovementState.Walking ||_state == MovementState.Sprinting ||_state == MovementState.Crouching)
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
    private float _speedChangeFactor;
    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        float time = 0;
        float difference = Mathf.Abs(_desiredMoveSpeed - _moveSpeed);
        float startValue = _moveSpeed;

        float boostFactor = _speedChangeFactor;

        while(time < difference)
        {
            _moveSpeed = Mathf.Lerp(startValue, _desiredMoveSpeed, time / difference);

            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * boostFactor * slopeAngleIncrease;
            }
            else
            {
                time += Time.deltaTime * boostFactor;
            }

            yield return null;
        }
        _moveSpeed = _desiredMoveSpeed;
        _speedChangeFactor = 1f;
        _keepMomentum = false;
    }

    private void MyInput()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");

        if (Input.GetKey(_jumpKey) && _readyToJump && _grounded)
        {
            _readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump), _jumpCooldown);
        }

        if (Input.GetKeyDown(_crouchKey))
        {
            _isCrounch = true;
            _playerObj.localScale = new Vector3(_playerObj.localScale.x, _crouchYScale, _playerObj.localScale.z);
            _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(_crouchKey) || (_isCrounch && !Input.GetKey(_crouchKey)))
        {
            TryStandUp();
        }
    }

    private void TryStandUp()
    {
        if (!Physics.Raycast(_playerObj.position, Vector3.up, _startYScale - _crouchYScale + 0.1f))
        {
            _playerObj.localScale = new Vector3(_playerObj.localScale.x, _startYScale, _playerObj.localScale.z);
            _isCrounch = false;
            _isSliding = false;
        }
    }


    private void StateHandler()
    {
        if (_isWallRunning)
        {
            _state = MovementState.WallRunning;
            _desiredMoveSpeed = _wallRunSpeed;
        }
        else if (_isDashing)
        {
            _state = MovementState.Dashing;
            _desiredMoveSpeed = _dashSpeed;
            _speedChangeFactor = _dashChangedFactor;
        }
        else if (_isSliding)
        {
            _state = MovementState.Sliding;

            if(OnSlope() && _rb.velocity.y < 0.1f)
            {
                _desiredMoveSpeed = _slideSpeed;
            }
            else
            {
                _desiredMoveSpeed = _sprintSpeed;
            }
        }
        else if (Input.GetKey(_crouchKey) || _isCrounch)
        {
            _state = MovementState.Crouching;
            _desiredMoveSpeed = _crouchSpeed;
        }
        else if(_grounded && Input.GetKey(_sprintKey))
        {
            _state = MovementState.Sprinting;
            _desiredMoveSpeed = _sprintSpeed;
        }
        else if (_grounded)
        {
            _state = MovementState.Walking;
            _desiredMoveSpeed = _walkSpeed;
        }
        else
        {
            _state = MovementState.Air;

            if(_desiredMoveSpeed < _sprintSpeed)
            {
                _desiredMoveSpeed = _walkSpeed;
            }
            else
            {
                _desiredMoveSpeed = _sprintSpeed;
            }
        }

        bool desiredMveSpeedHasChanged = _desiredMoveSpeed != _lastDesiredMoveSpeed;

        if (desiredMveSpeedHasChanged)
        {
            if (_keepMomentum)
            {
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeed());
            }
            else
            {
                StopAllCoroutines();
                _moveSpeed = _desiredMoveSpeed;
            }
        }

        _lastDesiredMoveSpeed = _desiredMoveSpeed;
        _lastState = _state;
    }

    private void MovePlayer()
    {
        if(_state == MovementState.Dashing)
        {
            return;
        }

        _moveDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;

        if (OnSlope() && !_existingSlop)
        {
            _rb.AddForce(GetSlopeMoveDirection(_moveDirection) * _moveSpeed * 20f, ForceMode.Force);

            if (_rb.velocity.y > 0)
            {
                _rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }
        else if (_grounded)
        {
            _rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        else if(!_grounded)
        {
            _rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f * _airMultiplier, ForceMode.Force);
        }

        if (!_isWallRunning)
        {
            _rb.useGravity = !OnSlope();
        }
    }

    private void SpeedControl()
    {
        if (OnSlope() && !_existingSlop)
        {
            if (_rb.velocity.magnitude > _moveSpeed)
                _rb.velocity = _rb.velocity.normalized * _moveSpeed;
        }

        else
        {
            Vector3 flatVel = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

            if (flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed;
                _rb.velocity = new Vector3(limitedVel.x, _rb.velocity.y, limitedVel.z);

            }
        }

        if (_maxYSpeed != 0 && _rb.velocity.y > _maxYSpeed)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, _maxYSpeed, _rb.velocity.z);
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

    public bool OnSlope()
    {
        if (Physics.Raycast(_playerObj.position, Vector3.down, out _slopeHit, _playerHeight * 0.5f + 0.3f, _whatIsGround))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }

        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, _slopeHit.normal).normalized;
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (!_showGizmos)
        {
            return;
        }

        // Dessiner une sph�re pour repr�senter la v�rification du sol
        Gizmos.color = _grounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(_playerObj.position + Vector3.down * (_playerHeight * 0.5f), 0.2f);

        // Dessiner un rayon pour montrer la direction de la d�tection du sol
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_playerObj.position, _playerObj.position + Vector3.down * (_playerHeight * 0.5f + 0.2f));

        // Dessiner la direction du mouvement
        if (_moveDirection != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_playerObj.position, _playerObj.position + _moveDirection);
        }

        // Dessiner une indication visuelle de la pente d�tect�e
        if (OnSlope()&& !_existingSlop)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_playerObj.position, _playerObj.position + GetSlopeMoveDirection(_moveDirection) * 2f);
        }

        // Dessiner la direction du mouvement avec un cercle au bout
        if (_rb != null && _rb.velocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Vector3 velocityDirection = _rb.velocity.normalized;
            Vector3 velocityEndPoint = _playerObj.position + velocityDirection * 2f;

            Gizmos.DrawLine(_playerObj.position, velocityEndPoint);
            Gizmos.DrawWireSphere(velocityEndPoint, 0.2f);
        }
    }
}
