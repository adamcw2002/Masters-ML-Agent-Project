using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float acceleration = 10f;
    public float turnSmooth = 0.15f;

    private float moveX, moveZ;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private CharacterController controller;
    private PlayerAgent agent;

    [HideInInspector] public Vector3 lastFacingDirection = Vector3.forward;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        agent = GetComponent<PlayerAgent>();
    }

    void Update()
    {
        Move(Time.deltaTime);
    }

    public void SetMoveInput(float x, float z)
    {
        this.moveX = x;
        this.moveZ = z;
    }

    public void Move(float deltaTime)
    {
        Vector3 targetDirection = new Vector3(moveX, 0f, moveZ);
        if (targetDirection.magnitude > 1)
            targetDirection.Normalize();

        // Frame-rate independent interpolation
        float movementBlend = 1f - Mathf.Exp(-acceleration * deltaTime);
        moveDirection = Vector3.Lerp(moveDirection, targetDirection, movementBlend);

        // Frame-rate independent rotation
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            float rotationBlend = 1f - Mathf.Exp(-1f / turnSmooth * deltaTime);
            lastFacingDirection = Vector3.Slerp(lastFacingDirection, moveDirection, rotationBlend);
            transform.forward = lastFacingDirection;
        }

        // Apply gravity
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        velocity.y += gravity * deltaTime;

        // Combine horizontal + vertical
        Vector3 move = (moveDirection * moveSpeed + new Vector3(0, velocity.y, 0)) * deltaTime;
        controller.Move(move);
    }
}