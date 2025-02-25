using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("WallRunning")]
    public LayerMask _whatIsWall;
    public LayerMask _whatIsGround;
    public float _wallRunForce = 200f;
    public float _wallJumpUpForce = 7f;
    public float _wallJumpSideForce = 12f;
    public float _wallClimbSpeed = 3f;
    public float _maxWallRunTime = 0.7f;
    private float _wallRunTimer;

    [Header("Input")]
    public KeyCode _jumpKey = KeyCode.Space;
    public KeyCode _upwardsRunKey = KeyCode.LeftShift;
    public KeyCode _downwardsRunKey = KeyCode.LeftControl;
    private bool _upwardsRunning;
    private bool _downwardsRunning;
    private float _horizontalInput;
    private float _verticalInput;

    [Header("Detection")]
    public float _wallCheckDistance = 0.7f;
    public float _minJumpHeight = 2f;
    public RaycastHit _leftWallHit;
    public RaycastHit _rightWallHit;
    private bool _wallLeft;
    private bool _wallRight;

    [Header("Exiting")]
    private bool _exitingWall;
    public float _exitWallTime = 0.2f;
    private float _exitWallTimer;

    [Header("Gravity")]
    public bool _useGravity;
    public float _gravityCounterForce;

    [Header("References")]
    public Transform _orientation;
    public PlayerCam _cam;
    private PlayerMovement _pm;
    private Rigidbody _rb;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        CheckForWall();
        StateMachine();
    }

    private void FixedUpdate()
    {
        if (_pm._wallRunning)
        {
            WallRunningMovement();
        }
    }

    private void CheckForWall()
    {
        _wallRight = Physics.Raycast(transform.position, _orientation.right, out _rightWallHit, _wallCheckDistance, _whatIsWall);
        _wallLeft = Physics.Raycast(transform.position, -_orientation.right, out _leftWallHit, _wallCheckDistance, _whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, _minJumpHeight, _whatIsGround);
    }

    private void StateMachine()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        _upwardsRunning = Input.GetKey(_upwardsRunKey);
        _downwardsRunning = Input.GetKey(_downwardsRunKey);

        if ((_wallLeft || _wallRight) && _verticalInput > 0 && AboveGround() && !_exitingWall)
        {
            if (!_pm._wallRunning)
            {
                StartWallRun();
            }

            if(_wallRunTimer > 0)
            {
                _wallRunTimer -= Time.deltaTime;
            }

            if(_wallRunTimer <= 0 && _pm._wallRunning)
            {
                _exitingWall = true;
                _exitWallTimer = _exitWallTime;
            }

            if (Input.GetKeyDown(_jumpKey))
            {
                WallJump();
            }
        }

        else if (_exitingWall)
        {
            if (_pm._wallRunning)
            {
                StopWallRun();
            }

            if (_exitWallTimer > 0)
            {
                _exitWallTimer -= Time.deltaTime;
            }

            if (_exitWallTimer <= 0)
            {
                _exitingWall = false;
            }
        }

        else
        {
            if (_pm._wallRunning)
            {
                StopWallRun();
            }
        }
    }

    private void StartWallRun()
    {
        _pm._wallRunning = true;
        _wallRunTimer = _maxWallRunTime;
        _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);

        _cam.DoFov(90f);

        if (_wallLeft)
        {
            _cam.DoTilt(-5f);
        }
        if (_wallRight)
        {
            _cam.DoTilt(5f);
        }
    }

    private void WallRunningMovement()
    {
        _rb.useGravity = _useGravity;

        Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if((_orientation.forward - wallForward).magnitude > (_orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        _rb.AddForce(wallForward * _wallRunForce, ForceMode.Force);

        if(_upwardsRunning)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, _wallClimbSpeed, _rb.velocity.z);
        }

        if(_downwardsRunning)
        {
            _rb.velocity = new Vector3(_rb.velocity.x, -_wallClimbSpeed, _rb.velocity.z);
        }

        if (!(_wallLeft && _horizontalInput > 0) && !(_wallRight && _horizontalInput < 0))
        {
            _rb.AddForce(-wallNormal * 100f, ForceMode.Force);
        }

        if (_useGravity)
        {
            _rb.AddForce(transform.up * _gravityCounterForce, ForceMode.Force);
        }
    }

    private void StopWallRun()
    {
        _pm._wallRunning = false;
        _cam.DoFov();
        _cam.DoTilt();
    }

    private void WallJump()
    {
        _exitingWall = true;
        _exitWallTimer = _exitWallTime;

        Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;
        Vector3 forceToApply = transform.up * _wallJumpUpForce + wallNormal * _wallJumpSideForce;

        _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        _rb.AddForce(forceToApply, ForceMode.Impulse);
    }

    private void OnDrawGizmos()
    {
        if (_pm == null || !_pm._showGizmos) return;

        // Couleur du cube autour du joueur selon l'état du wallrun
        Gizmos.color = _pm._wallRunning ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 1f));

        // Détection des murs avec les raycasts
        Gizmos.color = _wallLeft ? Color.blue : Color.gray;
        Gizmos.DrawLine(transform.position, transform.position - _orientation.right * _wallCheckDistance);

        Gizmos.color = _wallRight ? Color.blue : Color.gray;
        Gizmos.DrawLine(transform.position, transform.position + _orientation.right * _wallCheckDistance);

        // Direction du mouvement en wallrun
        if (_pm._wallRunning)
        {
            Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;
            Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);

            if ((_orientation.forward - wallForward).magnitude > (_orientation.forward - -wallForward).magnitude)
            {
                wallForward = -wallForward;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + wallForward * 2f);
        }

        // Direction du wall jump
        if (_exitingWall)
        {
            Vector3 wallNormal = _wallRight ? _rightWallHit.normal : _leftWallHit.normal;
            Vector3 forceToApply = transform.up * _wallJumpUpForce + wallNormal * _wallJumpSideForce;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + forceToApply);
        }
    }
}
