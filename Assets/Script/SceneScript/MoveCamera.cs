using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float sensitivity = 2.0f;
    public float maxYAngle = 80.0f;
    public float minYAngle = -80.0f;

    private Vector2 currentRotation;
    private float currentSpeed;

    void Start()
    {
        currentSpeed = moveSpeed;
    }
    void Update()
    {
        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.D))
        {
            currentSpeed = moveSpeed;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = -Input.GetAxis("Mouse Y");

        currentRotation.x += mouseX * sensitivity;
        currentRotation.y += mouseY * sensitivity;

        currentRotation.y = Mathf.Clamp(currentRotation.y, -maxYAngle, maxYAngle);
        transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);

        // 키보드 입력을 통해 카메라 이동
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 moveVector = transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, transform.position + moveVector, 0.3f);

        currentSpeed += moveSpeed * Time.deltaTime;
    }
}
