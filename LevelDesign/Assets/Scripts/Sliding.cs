using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliding : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform playerobj;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Sliding")]
    public float maxSlideTime;
    public float slideForce;
    private float sliderTime;
    
    public float SlideYscale;
    private float StartYScale;

    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;
    private bool sliding;
    private float slideTimer;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();

        StartYScale = playerobj.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");


        if (Input.GetKeyDown(slideKey) && (horizontalInput!=0 || verticalInput != 0))
        {
            StartSlide();
        }
        if (Input.GetKeyUp(slideKey) && sliding)
        {
            StopSlide();
        }
    }
    private void FixedUpdate()
    {
        if (sliding)
        {
            SlidingMovement();
        }
    }
    private void StartSlide()
    {
        sliding = true; 
        playerobj.localScale = new Vector3(playerobj.localScale.x,SlideYscale,playerobj.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }
    private void SlidingMovement()
    {
        Vector3 InputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput ;

        rb.AddForce(InputDirection.normalized * slideForce, ForceMode.Force);

        slideTimer -= Time.deltaTime;

        if (slideTimer <= 0)
        {
            StopSlide() ;
        }
    }
    private void StopSlide()
    {
        sliding = false;
        playerobj.localScale = new Vector3(playerobj.localScale.x, StartYScale, playerobj.localScale.z);

    }
}
