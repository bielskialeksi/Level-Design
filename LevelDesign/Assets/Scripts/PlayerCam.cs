using DG.Tweening;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [Header("Transform")]
    public Transform _orientation;
    public Transform _camHolder;

    public float _sensX = 400f;
    public float _sensY = 400f;

    private float _xRotation;
    private float _yRotation;

    [Header("Gizmos")]
    public bool _showGizmos;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime *_sensX;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime *_sensY;

        _yRotation += mouseX;
        _xRotation -= mouseY;

        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        _camHolder.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
        _orientation.rotation = Quaternion.Euler(0, _yRotation, 0);
    }

    public void DoFov(float endValue = 80f)
    {
        GetComponent<Camera>().DOFieldOfView(endValue, 0.25f);
    }

    public void DoTilt(float zTilt = 0f)
    {
        transform.DOLocalRotate(new Vector3(0f, 0f, zTilt), 0.25f);
    }


    private void OnDrawGizmos()
    {
        if (_orientation == null || !_showGizmos) return;

        // Dessine une ligne pour visualiser la direction de la caméra
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        // Dessine une ligne pour montrer l'orientation du joueur
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(_orientation.position, _orientation.forward * 2f);
    }
}
