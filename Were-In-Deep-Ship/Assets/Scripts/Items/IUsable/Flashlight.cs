using UnityEngine;
using Unity.Netcode;

public class Flashlight : NetworkBehaviour, IUsable
{
    public NetworkVariable<bool> IsOn = new(false, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

    public Light bulb;

    private ItemInfo info;

    public void Start()
    {
        info = GetComponent<ItemInfo>();

        switch(info.ItemTier.Value)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
        }
    }
    
    public void Use(NetworkObject player)
    {
        UseRpc(player);
    }

    [Rpc(SendTo.Everyone)]
    public void UseRpc(NetworkObjectReference reference)
    {   
        if (reference.TryGet(out NetworkObject networkObject))
        {
            if (IsServer) IsOn.Value = !IsOn.Value;
            var component = networkObject.GetComponent<Inventory>();
            component.InventorySlots[component.CurrentSlot.Value].itemNetworkObject.GetComponent<Flashlight>().bulb.enabled = IsOn.Value;
        }
    }
}
