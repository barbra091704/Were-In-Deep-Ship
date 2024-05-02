using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

public class SellingCrate : NetworkBehaviour, IInteractable
{
    public static SellingCrate Singleton;

    public NetworkVariable<int> CurrentValue = new();
    public Transform[] tablePositions;
    public List<ItemInfo> ItemsOnTable = new();

    private NetworkVariable<bool> HasSold = new(false);

    private MeshRenderer meshRenderer;
    private Vector3 initialPos;

    void Awake()
    {
        if (Singleton != null && Singleton != this) Destroy(this);
        else Singleton = this;

        initialPos = transform.position;

        meshRenderer = GetComponent<MeshRenderer>();
    }
    void Start()
    {
        if (!IsServer) return;

        GameManager.Singleton.CurrentLocation.OnValueChanged += ResetSellingCrate;
    }
    public void Interact<T>(RaycastHit hit, NetworkObject Player, T type)
    {
        switch(hit.collider.tag)
        {
            case "CrateButton":
                SellItemsRpc();
                break;
            case "Crate":
                SetOnTableRpc(Player);
                break;
        }
    }
    public void ResetSellingCrate(LocationData old, LocationData value)
    {
        if (value.ID == 0 && !old.Equals(value))
        {
            meshRenderer.enabled = true;
            HasSold.Value = false;
            transform.position = initialPos;
        }
    }

    [Rpc(SendTo.Server)]
    public void SellItemsRpc()
    {
        if (HasSold.Value || ItemsOnTable.Count == 0) return;
        StartCoroutine(StartItemSelling());
    }
    private IEnumerator StartItemSelling()
    {
        HasSold.Value = true;
        float duration = 25f;
        float speed = 5f;
        float startTime = Time.time;
        
        Vector3 initialPosition = transform.position;
        Vector3 finalPosition = initialPosition + duration * speed * -transform.right;

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            transform.position = Vector3.Lerp(initialPosition, finalPosition, t);
            yield return null;
        }

        CleanItemsOnTable();
        GameManager.Singleton.Credits.Value += CurrentValue.Value;
        CurrentValue.Value = 0;
        HasSold.Value = false;
        meshRenderer.enabled = false;
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

                slot.itemInfo.IsOnCrate.Value = true;

                CurrentValue.Value += slot.itemInfo.ItemValue.Value;

                SetOnTableClientRpc(Random.Range(0, tablePositions.Length), slot.itemNetworkObject, reference);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetOnTableClientRpc(int slot, NetworkObjectReference reference, NetworkObjectReference playerReference)
    {
        if (reference.TryGet(out NetworkObject itemNetworkObject))
        {
            if (playerReference.TryGet(out NetworkObject playerNetworkObject))
            {
                Inventory inventory = playerNetworkObject.GetComponent<Inventory>();

                ParentConstraint constraint = itemNetworkObject.GetComponent<ParentConstraint>();

                ConstraintSource constraintSource = new()
                {
                    sourceTransform = tablePositions[slot],
                    weight = 1,
                };

                itemNetworkObject.gameObject.layer = 6;

                constraint.RemoveSource(0);

                constraint.AddSource(constraintSource);

                if (IsServer) inventory.RemoveItemBySlotRpc(false, inventory.CurrentSlot.Value);

                return;
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
                    item.IsOnCrate.Value = false;
                }
                ItemsOnTable.Remove(item);

                return;         
            }
        }
    }


    public void CleanItemsOnTable(){
        foreach (var item in ItemsOnTable)
        {
            item.NetworkObject.Despawn(true);
        }
        ItemsOnTable.Clear();
    }
}
