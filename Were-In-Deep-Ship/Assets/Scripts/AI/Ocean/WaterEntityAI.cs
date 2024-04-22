using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class WaterEntityAI : NetworkBehaviour, IDamagable
{
    public NetworkVariable<int> EntityHealth = new(100);
    public WaterEntityWanderState WanderState = new();
    public WaterEntityAttackState AttackState = new();
    public WaterEntityFleeState  FleeState = new();
    public WaterEntityBaseState CurrentState;
    public EntityData entityData;
    private new Rigidbody rigidbody;
    private new BoxCollider collider;
    private Transform target;
    [HideInInspector] public EntityManager entityManager;

    public BoxCollider Collider { get { return collider; } set { collider = value; } }
    public Rigidbody Rigidbody { get { return rigidbody; } set { rigidbody = value; } }
    public Transform Target { get { return target;} set { target = value; } }

    public void Awake()
    {
        entityManager = FindAnyObjectByType<EntityManager>(FindObjectsInactive.Exclude);
    }

    public void Start()
    {
        if (!IsServer) return;

        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<BoxCollider>(); 

        EntityHealth.Value = entityData.MaxHealth;

        CurrentState = WanderState;
        CurrentState.EnterState(this);
    }

    public void FixedUpdate()
    {
        if (!IsServer) return;

        CurrentState.FixedUpdateState(this);

        Rigidbody.position = new (Rigidbody.position.x, Mathf.Clamp(Rigidbody.position.y, entityData.DepthRange.x, entityData.DepthRange.y), Rigidbody.position.z);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            if (entityData.CanAttack)
            {
                Target = other.transform;
                ChangeState(AttackState);
            }
            else
            {
                Target = other.transform;
                ChangeState(FleeState);
            }
        }
    }
    public void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        
        if (other.CompareTag("Player") && CurrentState == AttackState)
        {
            CurrentState = WanderState;
            CurrentState.EnterState(this);
        }
    }
    public void ChangeState(WaterEntityBaseState state)
    {
        if (state == CurrentState) return;

        CurrentState = state;
        CurrentState.EnterState(this);
    }
    [Rpc(SendTo.Server)]
    public void TakeDamageRpc(int value, bool applyResistance, RpcParams rpcParams = default)
    {
        float resistanceMultiplier = 1;

        if (applyResistance) resistanceMultiplier = 1 - (entityData.DamageResistance / 100f); // Calculate resistance multiplier (e.g., 10 resistance = 0.9 multiplier)

        int damageTaken = Mathf.RoundToInt(value * resistanceMultiplier);

        EntityHealth.Value -= damageTaken;
    }
}
