using System;
using KWS;
using Unity.Netcode;
using UnityEngine;

public class Oxygen : NetworkBehaviour
{
    public NetworkVariable<float> CurrentOxygenTankLevel = new(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> CurrentOxygenTankCapacity = new(30, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> DepthBeforeCustomRates = new(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int defaultOxygenLevel = 30;
    public int defaultDepthRate = 50;
    public int asphyxiationDamage = 8;
    public float damageInterval;
    private float oxygenDepletionRate = 2f;
    private float oxygenTimer = 0;
    private float asphyxiateTimer = 0f;
    private bool InWater;
    private int currentDepth;
    public event Action<int,bool, RpcParams> AsphyxiateEvent;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        
        GetComponent<PlayerMovement>().InWater.OnValueChanged += IsInWater;
        GetComponent<DepthSensor>().Depth.OnValueChanged += UpdateOxygenDepletionRate;

        CurrentOxygenTankCapacity.Value = defaultOxygenLevel;
        CurrentOxygenTankCapacity.Value = defaultOxygenLevel;
        DepthBeforeCustomRates.Value = defaultDepthRate;
    }


    public void Update()
    {
        if (!IsOwner) return;

        if (InWater)
        {
            AsphyxiationCheck();
            oxygenTimer += Time.deltaTime;

            if (oxygenTimer >= 3f) // Decrease oxygen every second
            {
                oxygenTimer = 0f;
                CurrentOxygenTankLevel.Value -= oxygenDepletionRate;
                CurrentOxygenTankLevel.Value = Mathf.Clamp(CurrentOxygenTankLevel.Value, 0, CurrentOxygenTankCapacity.Value);
            }
        }

        if (!WaterSystem.IsPositionUnderWater(new(transform.position.x, transform.position.y + 0.5f, transform.position.z)))
        {
            oxygenTimer += Time.deltaTime;
            if (oxygenTimer >= 0.1f && CurrentOxygenTankLevel.Value < CurrentOxygenTankCapacity.Value)
            {
                oxygenTimer = 0f;
                CurrentOxygenTankLevel.Value += 2f;
                CurrentOxygenTankLevel.Value = Mathf.Clamp(CurrentOxygenTankLevel.Value, 0, CurrentOxygenTankCapacity.Value);
            }
        }
        
    }

    void UpdateOxygenDepletionRate(int old, int newvalue)
    {
        currentDepth = newvalue;

        if (currentDepth > DepthBeforeCustomRates.Value){ 
            oxygenDepletionRate = FindNearestDepletionRate(currentDepth);
        }
    }

    private float FindNearestDepletionRate(int currentDepth)
    {
        int lastPassedThreshold = -1; // Initialize with a value that represents no threshold passed

        foreach (var depthRate in GameManager.Singleton.OxygenDepthDepletionRates)
        {
            if (currentDepth >= depthRate.depthThreshold && depthRate.depthThreshold > lastPassedThreshold)
            {
                lastPassedThreshold = depthRate.depthThreshold;
                return depthRate.depletionRate;
            }
        }
        return 0;
    }

    void AsphyxiationCheck()
    {
        if (CurrentOxygenTankLevel.Value <= 0 || currentDepth > DepthBeforeCustomRates.Value)
        {
            asphyxiateTimer += Time.deltaTime;
            
            if (asphyxiateTimer >= damageInterval)
            {
                asphyxiateTimer = 0f;
                AsphyxiateEvent?.Invoke(asphyxiationDamage, false, default);
            }
        }
        else
        {
            asphyxiateTimer = 0f;
        }
    }
    private void IsInWater(bool previousValue, bool newValue)
    {
        InWater = newValue;
        oxygenTimer = 0;
        asphyxiateTimer = 0;
    }
}
