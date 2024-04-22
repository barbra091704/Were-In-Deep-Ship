using UnityEngine;

public class WaterState : MovementState
{
    float setBuoyancy;
    public override void EnterState(PlayerMovement main)
    {
        main.rigidBody.useGravity = false;
        main.rigidBody.drag = 4;
    }

    public override void UpdateState(PlayerMovement main, bool _)
    {
        ClampVelocity(main);
        WaterStamina(main);
        setBuoyancy = main.FloatHeld ? main.floatSpeed : main.SinkHeld ? main.sinkSpeed : 0;
    }

    public override void FixedUpdateState(PlayerMovement main, Vector2 input)
    {
        Move(main, input);
    }

    private void Move(PlayerMovement main, Vector2 input)
    {
        Vector3 movement = input.y * main.playerCam.transform.forward + input.x * main.playerCam.transform.right;

        float speed = main.FastSwimHeld ? main.sprintSwimSpeed : main.swimSpeed;

        movement *= speed;

        float waterHeight = WaterData.Singleton.GetWaterHeight(main.rigidBody.position);

        if (Mathf.Abs(main.rigidBody.position.y - waterHeight) < .2f && !(main.CameraScript.camHolder.localRotation.eulerAngles.x > 35f && main.CameraScript.camHolder.localRotation.eulerAngles.x < 90f))
        {
            // Calculate the corrective force to keep the player at the water height
            float direction = waterHeight - main.rigidBody.position.y - 0.25f;
            movement.y += direction;
        }

        movement.y += setBuoyancy;

        Quaternion targetRotation = Quaternion.LookRotation(main.playerCam.transform.forward, Vector3.up);

        main.playerXZRotationObject.rotation = targetRotation;

        main.rigidBody.AddForce(movement, ForceMode.Force);


    }


    private void ClampVelocity(PlayerMovement main)
    {
        float maxSpeed = main.CurrentMaxSwimSpeed;

        Rigidbody rigidBody = main.GetComponent<Rigidbody>();

        // Clamp the horizontal velocity (X and Z components) if it exceeds the max speed
        Vector2 horizontalVelocity = new Vector2(rigidBody.velocity.x, rigidBody.velocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rigidBody.velocity = new Vector3(horizontalVelocity.x, 0, horizontalVelocity.y);
        }
    }

    private void WaterStamina(PlayerMovement main)
    {
        if (main.SprintHeld)
        {
            main.CurrentStamina.Value = Mathf.Clamp(main.CurrentStamina.Value - (main.waterStaminaDegenMultiplier * Time.deltaTime), 0.0f, 100f);
            main.staminaRegenTimer = 0.0f;
        }
        else if (main.CurrentStamina.Value < 100f)
        {
            if (main.staminaRegenTimer >= main.waterStaminaTimeToRegen)
            {
                main.CurrentStamina.Value = Mathf.Clamp(main.CurrentStamina.Value + (main.waterStaminaRegenMultiplier * Time.deltaTime), 0.0f, 100f);
            }
            else
            {
                main.staminaRegenTimer += Time.deltaTime;
            }
        }
    }
}
