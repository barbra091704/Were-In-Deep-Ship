using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

public class BarteringTable : NetworkBehaviour, IInteractable
{
    public static BarteringTable Singleton;

    public NetworkVariable<int> CurrentValue = new();
    public Transform[] tablePositions;
    public List<ItemInfo> ItemsOnTable = new();

    void Awake()
    {
        if (Singleton != null && Singleton != this) Destroy(this);
        else Singleton = this;
    }

    public void Interact(RaycastHit hit, NetworkObject Player)
    {
        switch(hit.collider.tag)
        {
            case "MerchantSellButton":
                SellItemsRpc();
                break;
            case "MerchantTable":
                SetOnTableRpc(Player);
                break;
        }
    }

    [Rpc(SendTo.Server)]
    public void SetOnTableRpc(NetworkObjectReference reference)
    {
        if (reference.TryGet(out NetworkObject Player))
        {
            var inventory = Player.GetComponent<Inventory>();

            if (inventory.InventorySlots[inventory.CurrentSlot.Value].itemNetworkObject != null)
            {
                InventorySlot slot = inventory.InventorySlots[inventory.CurrentSlot.Value];
                
                ItemsOnTable.Add(slot.itemInfo);

                slot.itemNetworkObject.TrySetParent(transform);
                
                slot.itemInfo.IsPickedUp.Value = false;

                slot.itemInfo.IsOnTable.Value = true;

                CurrentValue.Value += slot.itemInfo.ItemValue.Value;

                SetOnTableClientRpc(Random.Range(0, tablePositions.Length), slot.itemNetworkObject, NetworkObject, reference);

            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetOnTableClientRpc(int slot, NetworkObjectReference reference, NetworkObjectReference scriptReference, NetworkObjectReference playerReference)
    {
        if (reference.TryGet(out NetworkObject itemNetworkObject))
        {
            if (scriptReference.TryGet(out NetworkObject scriptNetworkObject))
            {
                if (playerReference.TryGet(out NetworkObject playerNetworkObject))
                {
                    BarteringTable script = scriptNetworkObject.GetComponent<BarteringTable>();

                    Inventory inventory = playerNetworkObject.GetComponent<Inventory>();

                    ParentConstraint constraint = itemNetworkObject.GetComponent<ParentConstraint>();

                    ConstraintSource constraintSource = new()
                    {
                        sourceTransform = script.tablePositions[slot],
                        weight = 1,
                    };

                    itemNetworkObject.gameObject.layer = 6;

                    constraint.RemoveSource(0);

                    constraint.AddSource(constraintSource);

                    if (IsServer) inventory.RemoveItemBySlotRpc(false, inventory.CurrentSlot.Value, playerReference);

                    return;
                }
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void RemoveFromTableRpc(int id)
    {
        foreach (var item in ItemsOnTable)
        {
            if (item.gameObject.GetInstanceID() == id)
            {
                if (IsServer)
                {
                    CurrentValue.Value -= item.ItemValue.Value;
                    item.IsOnTable.Value = false;
                }
                print("g");
                ItemsOnTable.Remove(item);

                return;         
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void SellItemsRpc(){
        CleanItemsOnTable();
        GameManager.Singleton.Credits.Value += CurrentValue.Value;
        CurrentValue.Value = 0;
    }

    public void CleanItemsOnTable(){
        foreach (var item in ItemsOnTable)
        {
            item.NetworkObject.Despawn(true);
        }
        ItemsOnTable.Clear();
    }
}
