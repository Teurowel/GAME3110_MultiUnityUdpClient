using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Target to follow
    public Transform target;

    //Offset from target
    public Vector3 offset;

    //Offset of looking at target
    public float pitch = 2f;

    //Zoom
    public float zoomSpeed = 4f;
    public float minZoom = 5f;
    public float maxZoom = 15f;

    //Rotating yaw
    public float yawSpeed = 100f;

    private float currentZoom = 10f;
    private float currentYaw = 0f;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //Controll zoom using mouse wheel
        currentZoom -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;

        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        //rotate camera in yaw
        currentYaw -= Input.GetAxis("Mouse X") * yawSpeed * Time.deltaTime;
    }

    //LateUpdate will be called after all update has been called, 
    //follow camera should be implemented in late update because it check if target has been moved in update.
    private void LateUpdate()
    {
        //Follow and look at target
        transform.position = target.position - offset * currentZoom;
        transform.LookAt(target.position + Vector3.up * pitch);


        //Rotate yaw of camera
        transform.RotateAround(target.position, Vector3.up, currentYaw);
    }
}
