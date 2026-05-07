using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpHeight = 4f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 200f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController _controller;
    private Vector3 _velocity;
    private float _xRotation = 0f;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        if (cameraTransform != null)
        {
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -80f, 80f);
            cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        _controller.Move(move * moveSpeed * Time.deltaTime);

        if (_controller.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        if (_controller.isGrounded && Input.GetButtonDown("Jump"))
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    public Vector3 GetAimDirection()
    {
        if (cameraTransform != null)
            return cameraTransform.forward;
        return transform.forward;
    }
}
