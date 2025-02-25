using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")]
    public Transform _orientation;
    public Transform _playerObj;
    private Rigidbody _rb;
    private PlayerMovement _pm;

    [Header("Sliding")]
    public float _maxSlideTime = 0.75f;
    public float _slideForce = 200f;
    private float _slideTime = 0.5f;

    private float _startYScale;

    [Header("Input")]
    public KeyCode _slideKey = KeyCode.LeftControl;
    private float _horizontalInput;
    private float _verticalInput;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();

        _startYScale = _playerObj.localScale.y;
    }


    private void Update()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKeyDown(_slideKey) && (_horizontalInput != 0 || _verticalInput != 0))
        {
            StartSlide();
        }

        if(Input.GetKeyUp(_slideKey) && _pm._isSliding)
        {
            StopSlide();
        }
    }


    private void FixedUpdate()
    {
        if (_pm._isSliding)
        {
            SlidingMovement();
        }
    }


    private void StartSlide()
    {
        _pm._isSliding = true;
        _playerObj.localScale = new Vector3(_playerObj.localScale.x, _pm._crouchYScale, _playerObj.localScale.z);
        _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        _slideTime = _maxSlideTime;
    }


    private void SlidingMovement()
    {
        Vector3 inputDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;

        if(!_pm.OnSlope() || _rb.velocity.y > -0.1f)
        {
            _rb.AddForce(inputDirection.normalized * _slideForce, ForceMode.Force);
            _slideTime -= Time.deltaTime;
        }

        else
        {
            _rb.AddForce(_pm.GetSlopeMoveDirection(inputDirection) * _slideForce, ForceMode.Force);
        }

        if(_slideTime <= 0)
        {
            StopSlide();
        }
    }

    private void StopSlide()
    {
        _pm._isSliding = false;

        if (!Physics.Raycast(transform.position, Vector3.up, _startYScale - _pm._crouchYScale + 0.1f))
        {
            _playerObj.localScale = new Vector3(_playerObj.localScale.x, _startYScale, _playerObj.localScale.z);
        }
        else
        {
            _pm._state = MovementState.Crouching;
            _pm._isCrounch = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (_pm == null || !_pm._showGizmos) return;

        // Afficher une bo�te autour du joueur pour indiquer s'il est en slide
        Gizmos.color = _pm._isSliding ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, _pm._crouchYScale, 1f));

        // Afficher la direction du mouvement en slide
        Vector3 moveDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;
        if (moveDirection != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + moveDirection * 2f);
        }

        // Indiquer si le joueur est sur une pente et montrer la direction du mouvement en pente
        if (_pm.OnSlope())
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + _pm.GetSlopeMoveDirection(moveDirection) * 2f);
        }
    }
}
