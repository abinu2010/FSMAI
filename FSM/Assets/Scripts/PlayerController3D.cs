using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float gravity = -15f;
    public float groundedGravity = -2f;
    [Header("Camera")]
    public Transform cameraTransform;
    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchHeight = 1.2f;
    public float crouchTransitionSpeed = 10f;
    [Header("Keys")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    CharacterController controller;
    Vector3 velocity;
    float targetHeight;
    float currentHeight;
    float baseHeight;
    float baseBottomY;
    Vector3 baseCenter;
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }
    public float CurrentSpeed { get; private set; }
    bool hasMoveInput;
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        baseHeight = controller.height;
        baseCenter = controller.center;
        baseBottomY = baseCenter.y - baseHeight * 0.5f;
        currentHeight = controller.height;
        targetHeight = controller.height;
        if (standingHeight <= 0f)
        {
            standingHeight = baseHeight;
        }
    }
    void Update()
    {
        HandleCrouching();
        HandleMovement();
        ApplyGravity();
    }
    void HandleCrouching()
    {
        bool sprintInput = Input.GetKey(sprintKey);
        bool crouchInput = Input.GetKey(crouchKey);
        bool prevCrouch = IsCrouching;
        bool prevSprint = IsSprinting;
        if (crouchInput && !sprintInput)
        {
            IsCrouching = true;
            IsSprinting = false;
            targetHeight = crouchHeight;
        }
        else
        {
            IsCrouching = false;

            if (sprintInput && !crouchInput)
            {
                IsSprinting = true;
            }
            else
            {
                IsSprinting = false;
            }

            targetHeight = standingHeight;
        }
        if (IsCrouching && !prevCrouch)
        {
            Debug.Log("[PlayerController3D] Crouch start");
        }
        else if (!IsCrouching && prevCrouch)
        {
            Debug.Log("[PlayerController3D] Stand up");
        }
        if (IsSprinting && !prevSprint)
        {
            Debug.Log("[PlayerController3D] Sprint start");
        }
        else if (!IsSprinting && prevSprint)
        {
            Debug.Log("[PlayerController3D] Sprint stop");
        }
        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
        {
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            controller.height = currentHeight;

            Vector3 c = controller.center;
            c.y = baseBottomY + currentHeight * 0.5f;
            controller.center = c;
        }
    }
    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0f, v);
        if (inputDir.sqrMagnitude > 1f)
        {
            inputDir.Normalize();
        }
        bool inputActive = inputDir.sqrMagnitude > 0.001f;
        Vector3 moveDir = inputDir;
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();
            moveDir = camForward * inputDir.z + camRight * inputDir.x;
            if (moveDir.sqrMagnitude > 1f)
            {
                moveDir.Normalize();
            }
        }
        float speed = 0f;
        if (inputActive)
        {
            if (IsSprinting)
            {
                speed = sprintSpeed;
            }
            else if (IsCrouching)
            {
                speed = crouchSpeed;
            }
            else
            {
                speed = walkSpeed;
            }

            controller.Move(moveDir * speed * Time.deltaTime);
        }
        CurrentSpeed = speed;
        if (inputActive && !hasMoveInput)
        {
            hasMoveInput = true;
            Debug.Log("[PlayerController3D] Movement input start");
        }
        else if (!inputActive && hasMoveInput)
        {
            hasMoveInput = false;
            Debug.Log("[PlayerController3D] Movement input stop");
        }
    }
    void ApplyGravity()
    {
        if (controller.isGrounded)
        {
            if (velocity.y < 0f)
            {
                velocity.y = groundedGravity;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }
    public Vector3 GetVelocity()
    {
        return controller.velocity;
    }
    public bool IsGrounded()
    {
        return controller.isGrounded;
    }
    public bool IsMoving()
    {
        return controller.velocity.magnitude > 0.1f;
    }
}
