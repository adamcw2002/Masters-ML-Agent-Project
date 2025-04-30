using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 velocity;

    [HideInInspector]
    public Vector3 lastFacingDirection = Vector3.forward; // default facing

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        moveDirection = new Vector3(moveX, 0f, moveZ);

        if (moveDirection.magnitude > 1)
            moveDirection.Normalize();

        if (moveDirection.sqrMagnitude > 0.01f)
            lastFacingDirection = moveDirection;

        ClampLastDirection();

        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void ClampLastDirection()
    {
        // Snap to cardinal directions only
        if (Mathf.Abs(lastFacingDirection.x) > Mathf.Abs(lastFacingDirection.z))
        {
            lastFacingDirection = new Vector3(Mathf.Sign(lastFacingDirection.x), 0, 0);
        }
        else
        {
            lastFacingDirection = new Vector3(0, 0, Mathf.Sign(lastFacingDirection.z));
        }
    }
}
