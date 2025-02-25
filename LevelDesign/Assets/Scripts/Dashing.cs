using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dashing : MonoBehaviour
{
    [Header("Dashing")]
    public float _dashForce = 20f;
    public float _dashUpwardForce;
    public float _maxDashYSpeed = 15f;
    public float _dashDuration = 0.25f;

    [Header("Settings")]
    public bool _useCameraForward = true;
    public bool _allowAllDirections = true;
    public bool _disableGravity = false;
    public bool _resetVel = true;

    [Header("Cooldown")]
    public float _dashCd = 1.5f;
    private float _dashCdTimer;

    [Header("References")]
    public Transform _orientation;
    public Transform _playerCam;
    private PlayerMovement _pm;
    private Rigidbody _rb;

    [Header("Input")]
    public KeyCode _dashKey = KeyCode.E;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_dashKey))
        {
            Dash();
        }

        if (_dashCdTimer > 0)
        {
            _dashCdTimer -= Time.deltaTime;
        }
    }

    private void Dash()
    {
        if(_dashCdTimer > 0)
        {
            return;
        }
        else
        { 
            _dashCdTimer = _dashCd;
        }

        _pm._isDashing = true;
        _pm._maxYSpeed = _maxDashYSpeed;

        Transform forwardT;

        if (_useCameraForward)
        {
            forwardT = _playerCam;
        }
        else
        {
            forwardT = _orientation;
        }

        Vector3 direction = GetDirection(forwardT);
        Vector3 forceToApply = direction * _dashForce + _orientation.up * _dashUpwardForce;

        if(_disableGravity)
        {
            _rb.useGravity = false;
        }

        delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDashForce), 0.025f);
        Invoke(nameof(ResetDash), _dashDuration);
    }

    private Vector3 delayedForceToApply;
    private void DelayedDashForce()
    {
        if (_resetVel)
        {
            _rb.velocity = Vector3.zero;
        }

        _rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetDash()
    {
        _pm._isDashing = false;
        if (_disableGravity)
        {
            _rb.useGravity = true;
        }
    }

    private Vector3 GetDirection(Transform forwardT)
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3();
        if (_allowAllDirections)
        {
            direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;
        }
        else
        {
            direction = forwardT.forward;
        }

        if(verticalInput == 0 && horizontalInput == 0)
        {
            direction = forwardT.forward;
        }

        return direction.normalized;
    }
}
