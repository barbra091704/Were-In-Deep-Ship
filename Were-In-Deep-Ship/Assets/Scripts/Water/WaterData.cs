using KWS;
using Unity.Netcode;
using UnityEngine;
public class WaterData : NetworkBehaviour
{
    [SerializeField] private bool UseNetworkBuoyancy;
    [SerializeField] private bool UseNetworkTime;
    public static WaterData Singleton {get; private set;}
    private WaterSystem water;
    private void Awake()
    {
        water = GetComponent<WaterSystem>();
        if (Singleton != null && Singleton != this) Destroy(this);
        else Singleton = this;
    }
    public override void OnNetworkSpawn()
    { 
        WaterSystem.UseNetworkBuoyancy = UseNetworkBuoyancy;
        WaterSystem.UseNetworkTime = UseNetworkTime;
    }
    private void Update() 
    { 
        if (!UseNetworkTime) return;
        WaterSystem.NetworkTime = NetworkManager.ServerTime.TimeAsFloat; 
    }
    public WaterSurfaceData GetWaterData(Vector3 position)
    {
        return water.GetCurrentWaterSurfaceData(position);
    }
    public float GetWaterHeight(Vector3 position)
    {
        WaterSurfaceData data = water.GetCurrentWaterSurfaceData(position);
        return data.IsActualDataReady ? data.Position.y : 0;
    }
    public Vector3 GetWaterNormal(Vector3 position)
    {
        WaterSurfaceData data = water.GetCurrentWaterSurfaceData(position);
        return data.IsActualDataReady ? data.Normal : Vector3.zero;
    }


}
