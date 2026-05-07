using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.5f;

    private CharacterController _controller;
    private bool _isDashing = false;
    private bool _dashOnCooldown = false;
    private Vector3 _dashDirection;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !_isDashing && !_dashOnCooldown)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 inputDir = transform.right * h + transform.forward * v;
            _dashDirection = inputDir.magnitude > 0.1f ? inputDir.normalized : transform.forward;
            StartCoroutine(DashCoroutine());
        }

        if (_isDashing)
        {
            _controller.Move(_dashDirection * dashSpeed * Time.deltaTime);
        }
    }

    IEnumerator DashCoroutine()
    {
        _isDashing = true;
        _dashOnCooldown = true;
        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
        yield return new WaitForSeconds(dashCooldown - dashDuration);
        _dashOnCooldown = false;
    }
}
