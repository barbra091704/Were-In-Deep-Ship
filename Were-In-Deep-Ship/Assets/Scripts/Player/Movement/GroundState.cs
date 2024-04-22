using System.Collections;
using UnityEngine;

public class GroundState : MovementState
{
    private float lastJumpTime;
    private bool canJump;
    private RaycastHit slopeHit;

    public override void EnterState(PlayerMovement main)
    {
        main.rigidBody.useGravity = true;
        main.rigidBody.drag = 1;

        if (main.isGrounded)
        {
            Quaternion targetRotation = Quaternion.identity;

            // Start the rotation interpolation coroutine
            main.StopCoroutine(nameof(LerpRotation));
            main.StartCoroutine(LerpRotation(main.playerXZRotationObject, targetRotation, 0.5f));
        }
    }

    // Coroutine for lerping rotation
    private IEnumerator LerpRotation(Transform transform, Quaternion targetRotation, float duration)
    {
        float timeElapsed = 0f;
        Quaternion initialRotation = transform.localRotation;

        while (timeElapsed < duration)
        {
            // Increment time elapsed
            timeElapsed += Time.deltaTime;

            // Calculate the interpolation factor (0 to 1)
            float t = Mathf.Clamp01(timeElapsed / duration);

            // Lerp between initial and target rotation
            transform.localRotation = Quaternion.Lerp(initialRotation, targetRotation, t);

            // Wait for the end of frame
            yield return null;
        }

        // Ensure the final rotation is exactly the target rotation
        transform.localRotation = targetRotation;
    }


    public override void UpdateState(PlayerMovement main, bool jumpPressed)
    {
        GroundStamina(main.SprintHeld, main);
        Crouch(main, main.CrouchPressed);
        if (jumpPressed)
        { 
            canJump = true;
        }
    }

    public override void FixedUpdateState(PlayerMovement main, Vector2 input)
    {
        Move(main, input);
        Jump(main);
    }

    private void Move(PlayerMovement main, Vector2 input)
    {
        if (input == Vector2.zero && main.rigidBody.velocity.magnitude > 0.1f)
        {
            Vector3 targetVelocity = new(0f, main.rigidBody.velocity.y, 0f);
            main.rigidBody.velocity = Vector3.Lerp(main.rigidBody.velocity, targetVelocity, 10 * Time.deltaTime);
        }

        Vector3 movement = input.y * main.playerYRotationObject.forward + input.x * main.playerYRotationObject.right;

        float speed = main.SprintHeld ? main.sprintSpeed : main.isCrouched ? main.crouchSpeed : main.walkSpeed;

        movement *= speed;

        if (OnSlope(main) && main.isGrounded && !canJump)
        {
            MonoBehaviour.print("onSlope");
            movement = Vector3.ProjectOnPlane(movement, slopeHit.normal);
            main.rigidBody.AddForce(-slopeHit.normal * 80f, ForceMode.Force);
        }

        main.rigidBody.AddForce(movement * 20, ForceMode.Force);

        Vector3 magnitude = Vector3.ClampMagnitude(main.rigidBody.velocity, speed);

        main.rigidBody.velocity = new Vector3(magnitude.x, main.rigidBody.velocity.y, magnitude.z);

        main.rigidBody.useGravity = !OnSlope(main);
    }



    private void Jump(PlayerMovement main)
    {
        if (canJump && Time.time - lastJumpTime >= main.jumpDelay)
        {

            canJump = false;

            main.rigidBody.velocity = new Vector3(main.rigidBody.velocity.x, 0, main.rigidBody.velocity.z);
            
            float jumpForceMagnitude = Mathf.Sqrt(2 * main.jumpForce * Mathf.Abs(Physics.gravity.y));

            main.rigidBody.AddForce(Vector3.up * jumpForceMagnitude, ForceMode.Impulse);

            main.staminaRegenTimer = 0.0f;

            lastJumpTime = Time.time;
        }
    }
    private bool OnSlope(PlayerMovement main)
    {
        if (Physics.Raycast(main.footTransform.position, Vector3.down, out slopeHit, 1f))
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }
    private void Crouch(PlayerMovement main, bool crouchPressed)
    {
        if (crouchPressed)
        {
            MonoBehaviour.print("Crouching");
            main.isCrouched = !main.isCrouched;
            if (main.isCrouched)
            {
                main.capsuleCollider.height = main.crouchingHeight;
                main.capsuleCollider.center = new Vector3(0, 0.5f, 0);
                main.CameraScript.camHolder.transform.localPosition = new(0, main.originalCameraHolderLocalPos.y + main.crouchOffset,0);
            }
            else
            {
                main.capsuleCollider.height = main.standingHeight;
                main.capsuleCollider.center = new Vector3(0, 1, 0);
                main.CameraScript.camHolder.transform.localPosition = new(0, main.originalCameraHolderLocalPos.y ,0);
            }
        }
    }

    private void GroundStamina(bool sprintHeld, PlayerMovement main)
    {
        if (sprintHeld)
        {
            main.CurrentStamina.Value = Mathf.Clamp(main.CurrentStamina.Value - (main.groundStaminaDegenMultiplier * Time.deltaTime), 0.0f, 100f);
            main.staminaRegenTimer = 0.0f;
        }
        else if (main.CurrentStamina.Value < 100f)
        {
            if (main.staminaRegenTimer >= main.groundStaminaTimeToRegen)
            {
                main.CurrentStamina.Value = Mathf.Clamp(main.CurrentStamina.Value + (main.groundStaminaRegenMultiplier * Time.deltaTime), 0.0f, 100f);
            }
            else
            {
                main.staminaRegenTimer += Time.deltaTime;
            }
        }
    }
}
