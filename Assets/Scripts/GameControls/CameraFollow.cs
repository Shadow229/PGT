using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.3F;
    public Vector3 cameraOffset;
    private Vector3 velocity = Vector3.zero;
    private Vector3 InitRot = new Vector3(27.86f, 0.0f, 0.0f);



    public float speed = 1.0f;
    public float fastSpeed = 2.0f;
    public float mouseSpeed = 4.0f;
    private Vector3 _angles;
    private bool CursorEngaged = false;

    private bool BallCamInit = false;
    private bool followCamInit = false;

    void LateUpdate()
    {
        //camera controlled by ball movement
        if (target.GetComponent<BallControl>().Activeplayer)
        {
            if (!BallCamInit)
            {
                BallCamInitialise();
            }

            // Define a target position above and behind the target transform
            Vector3 targetPosition = target.transform.position;

            Vector3 distance = cameraOffset.z * transform.forward.normalized;
            Vector3 height = cameraOffset.y * Vector3.up;
            Vector3 shoulder = cameraOffset.x * transform.right.normalized;

            Vector3 totaloffset = distance + height + shoulder;

            // Smoothly move the camera towards that target position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition + totaloffset, ref velocity, smoothTime);

            float RotateForce = Input.GetAxis("Horizontal");
            transform.RotateAround(target.transform.position, new Vector3(0, 1, 0), target.GetComponent<BallControl>().rotateSpeed * RotateForce);
        }
        //camera controlled freeflow
        else
        {
            if (CursorEngaged)
            {
                //listen for esc
                if (Input.GetKey(KeyCode.Escape))
                {
                    CursorEngaged = false;
                    //free the cursor
                    Cursor.lockState = CursorLockMode.None;
                }

                //init camera if needed
                if (!followCamInit) { FollowCamInitialise(); }

                //update camera movement
                _angles.x -= Input.GetAxis("Mouse Y") * mouseSpeed;
                _angles.y += Input.GetAxis("Mouse X") * mouseSpeed;
                transform.eulerAngles = _angles;
                float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : speed;
                transform.position +=
                Input.GetAxis("Horizontal") * moveSpeed * transform.right +
                Input.GetAxis("Vertical") * moveSpeed * transform.forward;
            }
            else
            {
                //reengage mouse if clicked
                if (Input.GetMouseButtonDown(0))
                {
                    CursorEngaged = true;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
    }



    void BallCamInitialise()
    {
        //free the cursor
        Cursor.lockState = CursorLockMode.None;
        //Recentre the camera
        transform.eulerAngles = InitRot;
        //set initialised
        BallCamInit = true;
        followCamInit = false;
    }

    void FollowCamInitialise()
    {
        _angles = transform.eulerAngles;
        Cursor.lockState = CursorLockMode.Locked;
        followCamInit = true;
        BallCamInit = false;
    }

    private void OnEnable()
    {
        if (!target.GetComponent<BallControl>().Activeplayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            CursorEngaged = true;
        }

    }
}


