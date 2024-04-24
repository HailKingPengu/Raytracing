using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    Camera cam;
    Vector3 anchorPoint;
    Quaternion anchorRot;

    [SerializeField]
    float moveSpeed;
    float updateMoveSpeed;

    [SerializeField]
    float sensitivity;

    bool heldDown;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void FixedUpdate()
    {

        if (Input.GetKey(KeyCode.LeftShift))
            updateMoveSpeed = moveSpeed * 2;
        else
            updateMoveSpeed = moveSpeed;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            move += Vector3.forward * updateMoveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            move -= Vector3.forward * updateMoveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            move += Vector3.right * updateMoveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            move -= Vector3.right * updateMoveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            move += Vector3.up * updateMoveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q))
            move -= Vector3.up * updateMoveSpeed * Time.deltaTime;
        transform.Translate(move);

        if (Input.GetMouseButton(1))
        {
            if (!heldDown)
            {
                anchorPoint = new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
                anchorRot = transform.rotation;
            }

            Quaternion rot = anchorRot;
            Vector3 dif = anchorPoint - new Vector3(Input.mousePosition.y, -Input.mousePosition.x);
            rot.eulerAngles += dif * sensitivity * Time.deltaTime;
            transform.rotation = rot;

            heldDown = true;
        }
        else
        {
            heldDown = false;
        }
    }
}
