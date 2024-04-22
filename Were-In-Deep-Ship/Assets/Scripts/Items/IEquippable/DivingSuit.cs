using KWS;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "IEquippable/DivingSuit")]
public class DivingSuit : ScriptableObject, IEquippable
{
    public int Level;
    public int OxygenCapacity;
    public int DamageResistance;
    public int DepthRating;
    public Sprite SuitImage;
    public Mesh SuitMesh;
    public Material SuitMaterial;

    public void SetEffects(NetworkObject player)
    {
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        Oxygen oxygen = player.GetComponent<Oxygen>();
        WaterSystemScriptableData water = FindObjectOfType<WaterSystemScriptableData>();

        water.Transparent = 25;

        health.Resistance.Value = DamageResistance;

        oxygen.DepthBeforeCustomRates.Value = DepthRating;

        oxygen.CurrentOxygenTankCapacity.Value = OxygenCapacity;

        oxygen.CurrentOxygenTankLevel.Value = oxygen.CurrentOxygenTankCapacity.Value;
    }

}
