using UnityEngine;

[CreateAssetMenu(menuName = "AI/Entity/EntityData")]
public class EntityData : ScriptableObject
{
    [Header("Water Specific Params")]
    public Vector2 DepthRange;

    [Header("Land Specific Params")]
    public int HearingRange;

    [Header("General Params")]
    public int EntityID;
    public int MaxHealth;
    public int WanderSpeed;
    public int AttackSpeed;
    public int FleeSpeed;
    public int FleeDistance;
    public int DetectionRange;
    public int Stamina;
    public int DamageResistance;
    public int WanderRadius;
    public int AttackStopDistance;
    [Header("Attacking Params")]
    public bool CanAttack;
    public int DamageInterval;
    public int AttackAmount;
    public int AttackDamage;
}
