using UnityEngine;

public class WaterEntityAttackState : WaterEntityBaseState
{
    private float lastDamageTime;
    private int currentAttackAmount;
    private bool attacked;
    public override void EnterState(WaterEntityAI main)
    {
        currentAttackAmount = 0;
        lastDamageTime = 0;
    }

    public override void FixedUpdateState(WaterEntityAI main)
    {
        if (Vector3.Distance(main.Rigidbody.position, main.Target.position) > main.entityData.AttackStopDistance)
        {
            if (main.Target.position.y >= main.entityData.DepthRange.x && main.Target.position.y <= main.entityData.DepthRange.y)
            {
                Vector3 direction = (main.Target.position - main.Rigidbody.position).normalized;

                Debug.DrawLine(main.transform.position, main.Target.position, Color.red);
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                main.Rigidbody.MoveRotation(Quaternion.Slerp(main.Rigidbody.rotation, targetRotation, Time.deltaTime * 5));

                main.Rigidbody.AddForce(direction * main.entityData.AttackSpeed, ForceMode.Acceleration);
            }
            else
            {
                main.ChangeState(main.WanderState);
            }
        }
        else
        {
            if (!attacked)
            {
                main.Target.GetComponent<PlayerHealth>().TakeDamageRpc(main.entityData.AttackDamage, true);
                lastDamageTime = Time.time;
                currentAttackAmount++;
                attacked = true;
            }
            else if (Time.time >= lastDamageTime + main.entityData.DamageInterval && currentAttackAmount < main.entityData.AttackAmount)
            {
                main.Target.GetComponent<PlayerHealth>().TakeDamageRpc(main.entityData.AttackDamage, true);
                lastDamageTime = Time.time;
                currentAttackAmount++;
            }
            else
            {
                main.ChangeState(main.FleeState);
            }
        }

    }


}
