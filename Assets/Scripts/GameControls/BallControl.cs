using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallControl : MonoBehaviour
{
    public bool GeneratePlayer = false;
    public float rotateSpeed = 10.0f;

    public float movementSpeed = 10.0f;
    public float angDrag = 20.0f;
    public float maxAngVel = 7.0f;
    public float visionRadius = 100.0f;
    public bool DrawVisionRadius = true;

    [HideInInspector]
    public bool Activeplayer;

    Vector3 startPos;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //if there's no player and one is needed - spawn one
        if (GeneratePlayer && !Activeplayer)
        {
            Activeplayer = true;
            transform.position = startPos;
            GetComponent<MeshRenderer>().enabled = true;
            rb.useGravity = true;
            rb.isKinematic = false;

            //turn on highlight light
            GameObject.Find("HighLightSpot").GetComponent<Light>().enabled = true;
            GameObject.Find("PlayerLight").GetComponent<Light>().enabled = true;
        }
        //remove player if its unselected while running
        else if (!GeneratePlayer)
        {
            GetComponent<MeshRenderer>().enabled = false;
            Activeplayer = false;
            rb.useGravity = false;
            rb.isKinematic = true;

            //turn off highlight light
            GameObject.Find("HighLightSpot").GetComponent<Light>().enabled = false;
            GameObject.Find("PlayerLight").GetComponent<Light>().enabled = false;
        }

        //run player update if there is a player
        if (Activeplayer)
        {
            float ForwardForce = Input.GetAxis("Vertical");
            float RotateForce = Input.GetAxis("Horizontal");

            //update rb
            rb.angularDrag = angDrag;
            rb.maxAngularVelocity = maxAngVel;

            //forward from camera
            Vector3 trueFwd = Camera.main.transform.right.normalized;

            //general rolling movement and rotation
            rb.AddTorque(trueFwd * ForwardForce * movementSpeed);
            transform.Rotate(Vector3.up * rotateSpeed * RotateForce, Space.World);

            //check for reset
            ResetPlayer();
        }
    }


    void ResetPlayer()
    {
        float resetVertical = -40.0f;

        if (transform.position.y < resetVertical)
        {
            transform.position = startPos;
            rb.velocity = Vector3.zero;
        }
    }

    //vision raduis 
    private void OnDrawGizmos()
    {
        if (DrawVisionRadius)
        {
            Gizmos.color = Color.white;
            float theta = 0;
            float x = visionRadius * Mathf.Cos(theta);
            float y = visionRadius * Mathf.Sin(theta);
            Vector3 pos = transform.position + new Vector3(x, 0, y);
            Vector3 newPos;
            Vector3 lastPos = pos;

            //draw a 2D circle on the xz plane
            for (theta = 0.1f; theta < Mathf.PI * 2; theta += 0.1f)
            {
                x = visionRadius * Mathf.Cos(theta);
                y = visionRadius * Mathf.Sin(theta);
                newPos = transform.position + new Vector3(x, 0, y);
                Gizmos.DrawLine(pos, newPos);
                pos = newPos;
            }
            Gizmos.DrawLine(pos, lastPos);
        }
     
    }
}
