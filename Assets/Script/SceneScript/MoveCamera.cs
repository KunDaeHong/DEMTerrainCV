using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField]
    private float scrollWeight = 10f;
    public float moveSpeed = 5.0f;
    public float sensitivity = 2.0f;
    public float maxYAngle = 80.0f;
    public float minYAngle = -80.0f;

    private Vector2 currentRotation;
    private float currentSpeed;

    void Start()
    {
        currentSpeed = moveSpeed;
        Cursor.visible = true;
    }
    void Update()
    {
        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.D))
        {
            currentSpeed = moveSpeed;
        }

        scrollZoom();
        mouseView();

        // 키보드 입력을 통해 카메라 이동
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 moveVector = transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, transform.position + moveVector, 0.3f);

        currentSpeed += moveSpeed * Time.deltaTime;
    }

    //MARK: Movement

    void scrollZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel") * scrollWeight;
        Vector3 moveVector = transform.TransformDirection(transform.forward.normalized) * currentSpeed * scroll;
        transform.position = Vector3.Lerp(transform.position, transform.position + moveVector, 0.3f);
    }

    void mouseView()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = -Input.GetAxis("Mouse Y");

            currentRotation.x += mouseX * sensitivity;
            currentRotation.y += mouseY * sensitivity;

            currentRotation.y = Mathf.Clamp(currentRotation.y, -maxYAngle, maxYAngle);
            transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);
        }
    }
}
