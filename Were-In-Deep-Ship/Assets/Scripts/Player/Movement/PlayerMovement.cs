using Unity.Netcode;
using UnityEngine;
using Cinemachine;
using KWS;

public class PlayerMovement : NetworkBehaviour
{
    public bool SprintHeld {get; private set;}
    public bool FastSwimHeld {get; private set;}
    public bool CrouchPressed {get; private set;}
    public bool CrouchIsHeld {get; private set;}
    public bool SinkHeld {get; private set;}
    public bool FloatHeld {get; private set;}
    public bool JumpPressed {get; private set;}


    [Header("Generic Variables")]
    public NetworkVariable<bool> InWater = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> CurrentStamina = new(100,NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public MovementState CurrentState;
    public GroundState GroundState = new();
    public WaterState WaterState = new();
    public PlayerCamera CameraScript {get; private set;}
    public Transform playerXZRotationObject;
    public Transform playerYRotationObject;
    public Animator animator;
    public Rigidbody rigidBody;
    public CinemachineVirtualCamera virtualCamera;
    public CapsuleCollider capsuleCollider;
    public Camera playerCam;
    public Canvas UI;
    public SkinnedMeshRenderer meshRenderer;
    public Transform footTransform;
    public LayerMask groundMask;
    [HideInInspector] public Quaternion originalRotationObjectRotation;
    private InputManager inputManager;
    public bool canMove = true;
    public bool isGrounded;
    public bool isCrouched;
    public float standingHeight;
    public float crouchingHeight;
    public float staminaRegenTimer;
    private string CurrentAnimationState;
    
    [Header("Ground Parameters")]
    public float walkSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    public float jumpForce;
    public float jumpDelay;
    public float crouchOffset = -0.5f;
    [HideInInspector] public Vector3 originalCameraHolderLocalPos;
    public float groundDistance = 0.4f;
    public float groundStaminaRegenMultiplier;
    public float groundStaminaDegenMultiplier; 
    public float groundStaminaTimeToRegen;

    [Header("Water Parameters")]
    public float swimSpeed;
    public float sprintSwimSpeed;
    public float sinkSpeed;
    public float floatSpeed;
    public float waterStaminaRegenMultiplier;
    public float waterStaminaDegenMultiplier;
    public float waterStaminaTimeToRegen;

    [Header("Climbing Parameters")]
    public float climbSpeed;

    public void Start()
    {
        if (!IsOwner) return;
        inputManager = InputManager.Instance;

        originalCameraHolderLocalPos = CameraScript.camHolder.localPosition;
        CurrentState = GroundState;
        CurrentState.EnterState(this);
    }
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            UI.enabled = false;
            playerCam.enabled = false;
            playerCam.GetComponent<AudioListener>().enabled = false;
        }
        else
        {   
            virtualCamera.Priority = 1;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            CameraScript = GetComponent<PlayerCamera>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    void Update()
    {
        if (!IsOwner || !canMove) return;
        IsGroundedCheck();
        UpdateInput();
        JumpPressed = inputManager.JumpedThisFrame() && isGrounded && !isCrouched;
        CurrentState.UpdateState(this, JumpPressed);
        SurfaceCheck();
        AnimationCheck();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !canMove) return;

        CurrentState.FixedUpdateState(this, inputManager.GetPlayerMovement());
    }

    public void IsGroundedCheck(){
        isGrounded = Physics.CheckSphere(footTransform.position, groundDistance, groundMask);
    }
    private void ChangeState(MovementState state)
    {
        CurrentState = state;
        CurrentState.EnterState(this);
    }
    void SurfaceCheck()
    {
        if (WaterSystem.IsPositionUnderWater(new(transform.position.x, transform.position.y + 1.15f, transform.position.z)))
        {
            InWater.Value = true;
            ChangeState(WaterState);
        }
        else if (CurrentState != GroundState) // Check if not already in GroundState
        {
            InWater.Value = false;
            ChangeState(GroundState);
        }
    }

    private void UpdateInput()
    {
        SprintHeld = inputManager.SprintIsHeld() && !isCrouched && CurrentStamina.Value > 0 && inputManager.GetPlayerMovement().y > 0;
        FastSwimHeld = inputManager.SprintIsHeld() && !isCrouched && CurrentStamina.Value > 0 && inputManager.GetPlayerMovement().y > 0;
        CrouchPressed = inputManager.CrouchedThisFrame() && !InWater.Value && isGrounded;
        CrouchIsHeld = inputManager.CrouchIsHeld() && !InWater.Value && isGrounded;
        SinkHeld = inputManager.CrouchIsHeld() && InWater.Value;
        FloatHeld = inputManager.JumpIsHeld() && InWater.Value;
    }

    private void AnimationCheck()
    {
        Vector2 input = inputManager.GetPlayerMovement();
        string state = null;
        if (input.y > 0)
        {
            if (SprintHeld)
            {
                if (CurrentState == GroundState && isGrounded)
                {
                    state = "Sprinting";
                }
                else if (CurrentState == WaterState)
                {
                    state = "Fast Swimming";
                }
            }
            else
            {
                if (isCrouched)
                {
                    state = "Crouch Walk";
                }
                else if (CurrentState == GroundState && isGrounded)
                {
                    state = "Walking";
                }
                else if (CurrentState == WaterState)
                {
                    state = "Swimming";
                }
            }
        }
        else if (input.y < 0)
        {
            if (isCrouched)
            {
                state = "Crouch Walk Backwards";
            }
            else if (CurrentState == GroundState && isGrounded)
            {
                state = "Walking Backwards";
            }
            else if (CurrentState == WaterState)
            {
                state = "Swimming Backwards";
            }
        }
        else
        {
            if (isCrouched)
            {
                state = "Crouch";
            }
            else if (CurrentState == GroundState && isGrounded)
            {
                state = "Idle";
            }
            else if (CurrentState == WaterState)
            {
                state = "Floating";
            }
        }
        if (state != null)
        {
            ChangeAnimationState(state);
        }
    }


    public void ChangeAnimationState(string state)
    {
        if (CurrentAnimationState != state)
        {
            animator.CrossFadeInFixedTime(state, 0.35f);
            CurrentAnimationState = state;
        }
    }

    public float CurrentMaxSwimSpeed { get { return SprintHeld ? sprintSwimSpeed : swimSpeed; } }

    public float CurrentMaxBuoyancy { get { return FloatHeld ? floatSpeed : sinkSpeed; } }
}
