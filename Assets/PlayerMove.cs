using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public Rigidbody rb;
    public float speed = 10f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //rb.AddForce(new Vector3(Input.GetAxis("Horizontal") * speed * Time.deltaTime, 0, 0), ForceMode.VelocityChange);
        //rb.AddForce(new Vector3(0, 0, Input.GetAxis("Vertical") * speed * Time.deltaTime), ForceMode.VelocityChange);
        transform.Translate(new Vector3(Input.GetAxis("Horizontal") * speed * Time.deltaTime, 0, 0));
        transform.Translate(new Vector3(0, 0, Input.GetAxis("Vertical") * speed * Time.deltaTime));
    }
}
