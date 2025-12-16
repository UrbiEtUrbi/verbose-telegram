using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraHolder; // Empty parent for camera (for bobbing)
    private CharacterController characterController;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isJumpPressed;
    private bool isCrouchPressed;
    private bool isSprintPressed;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float airStrafeMultiplier = 0.7f;
    private float currentSpeed;
    private Vector3 currentVelocity;
    private Vector3 verticalVelocity;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -35f; // Stronger for better platforming
    [SerializeField] private float variableJumpMultiplier = 0.5f; // Release jump early = lower jump
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimeCounter;
    private bool isJumping;
    private bool jumpReleased;

    [Header("Crouch Parameters")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, -0.5f, 0);
    private bool isCrouching;
    private float targetHeight;
    private Vector3 targetCenter;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float maxSlopeAngle = 45f;
    private bool isGrounded;
    private RaycastHit groundHit;
    private Transform currentPlatform;
    private Vector3 platformVelocity;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalLookLimit = 85f;
    private float xRotation = 0f;

    [Header("Camera")]
    private float defaultFOV;
    private float defaultCameraY;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (cameraHolder == null && playerCamera != null) 
            cameraHolder = playerCamera.transform.parent;
        
        LockCursor();
        InitializeValues();
    }

    void InitializeValues()
    {
        defaultFOV = playerCamera.fieldOfView;
        if (cameraHolder != null) defaultCameraY = cameraHolder.localPosition.y;
        
        targetHeight = standingHeight;
        targetCenter = standingCenter;
        currentSpeed = walkSpeed;
        
        characterController.height = targetHeight;
        characterController.center = targetCenter;
    }

    void Update()
    {
        HandleGroundCheck();
        HandleCrouch();
        HandleJumpAndGravity();
        HandleMovement();
        HandleLook();
    }

    #region Input Methods
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        Debug.Log($"jump {context.started}");
        if (context.started)
        {
            isJumpPressed = true;
            jumpReleased = false;
        }
        else if (context.canceled)
        {
            jumpReleased = true;
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isCrouching = !isCrouching;
            UpdateCrouchTarget();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprintPressed = context.performed;
    }
    #endregion

    #region Ground & Platform Detection
    void HandleGroundCheck()
    {
        bool wasGrounded = isGrounded;
        
        // Use both CharacterController and spherecast for reliability
        Vector3 sphereOrigin = transform.position + characterController.center - Vector3.up * (characterController.height * 0.5f - groundCheckRadius);
        isGrounded = Physics.SphereCast(sphereOrigin, groundCheckRadius, -Vector3.up, out groundHit, 
                                        groundCheckDistance, groundMask);
        

        // Slope angle check
        if (isGrounded && Vector3.Angle(Vector3.up, groundHit.normal) > maxSlopeAngle)
        {
            isGrounded = false;
        }
        
        // Coyote time logic
        if (isGrounded && characterController.velocity.y <= 0)
        {
            coyoteTimeCounter = coyoteTime;
            verticalVelocity.y = -2f; // Small downward force for stability
            isJumping = false;
            
            // Handle moving platforms
            if (groundHit.collider != null && groundHit.collider.transform != currentPlatform)
            {
                currentPlatform = groundHit.collider.transform;
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // Leave platform when airborne
        if (!isGrounded && currentPlatform != null)
        {
            currentPlatform = null;
            platformVelocity = Vector3.zero;
        }
    }

    #endregion

    #region Movement
    void HandleMovement()
    {
        // Determine target speed based on state
        float targetSpeed = walkSpeed;
        
        if (isCrouching)
            targetSpeed = crouchSpeed;
        else if (isSprintPressed && !isCrouching && moveInput.y > 0)
            targetSpeed = runSpeed;
        
        // Smooth speed transition
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        
        // Calculate movement direction relative to player orientation
        Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        
        // Apply speed
        Vector3 targetVelocity = moveDirection * currentSpeed;
        
        // Air strafing reduction
        float strafeMultiplier = isGrounded ? 1f : airStrafeMultiplier;
        targetVelocity *= strafeMultiplier;
        
        // Smooth velocity transition
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime * strafeMultiplier);
        
        // Apply movement
//        Debug.Log(verticalVelocity);
        characterController.Move((currentVelocity + verticalVelocity) * Time.deltaTime);
    }
    #endregion

    #region Jump & Gravity
    void HandleJumpAndGravity()
    {
        // Jump logic
        if (isJumpPressed && (coyoteTimeCounter > 0 || isGrounded) && !isCrouching)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            coyoteTimeCounter = 0;
            isJumping = true;
            isJumpPressed = false;

        }
        
        // Variable jump height (early release = lower jump)
        if (jumpReleased && isJumping && verticalVelocity.y > 0)
        {
            verticalVelocity.y *= variableJumpMultiplier;
            isJumping = false;
        }
        
        // Apply gravity
        if (!isGrounded)
        {
            verticalVelocity.y += gravity * Time.deltaTime;
            
            // Terminal velocity clamp
            verticalVelocity.y = Mathf.Max(verticalVelocity.y, gravity * 2f);
        }
        
        // Reset jump input
        isJumpPressed = false;
    }
    #endregion

    #region Crouch
    void UpdateCrouchTarget()
    {
        if (isCrouching)
        {
            targetHeight = crouchingHeight;
            targetCenter = crouchingCenter;
            currentSpeed = crouchSpeed;
        }
        else
        {
            // Check for ceiling before standing
            float ceilingCheckDistance = standingHeight - crouchingHeight + 0.1f;
            if (!Physics.SphereCast(transform.position + Vector3.up * groundCheckRadius, 
                                   groundCheckRadius, Vector3.up, out _, ceilingCheckDistance, groundMask))
            {
                targetHeight = standingHeight;
                targetCenter = standingCenter;
            }
        }
    }

    void HandleCrouch()
    {
        // Smooth height transition
        characterController.height = Mathf.Lerp(characterController.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        characterController.center = Vector3.Lerp(characterController.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);
        
        // Adjust camera position
        if (cameraHolder != null)
        {
            float cameraHeightOffset = (targetHeight - standingHeight) * 0.5f;
            Vector3 targetCameraPos = cameraHolder.localPosition;
            targetCameraPos.y = defaultCameraY + cameraHeightOffset;
            cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, targetCameraPos, crouchTransitionSpeed * Time.deltaTime);
        }
    }
    #endregion

    #region Look
    void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        
        // Vertical rotation (clamped)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
        
        // Apply rotations
        if (cameraHolder != null)
            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    #endregion

    #region Utility
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    #endregion
}