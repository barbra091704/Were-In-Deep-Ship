using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

[Serializable]
public class InventorySlot
{
    public ItemInfo itemInfo;
    public NetworkObject itemNetworkObject;
}

public class Inventory : NetworkBehaviour
{
    private InputManager inputManager;
    private NetworkObjectReference networkObjectReference;

    public NetworkVariable<sbyte> CurrentSlot = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> CurrentWeight = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    public DivingSuit currentSuit;
    public Transform handPos;
    public Transform dropPos;

    private Mesh originalMesh;
    private Material originalMaterial;

    public event Action<int> UISelectSlotEvent;
    public event Action<int,bool> UISetSlotImageEvent;
    public event Action<Sprite> UISetArmorEvent;


    [Header("Inventory Slots")]

    public InventorySlot[] InventorySlots;

    private void Start()
    {
        if (!IsOwner) return;

        inputManager = InputManager.Instance;

        GetComponent<Interaction>().ItemPickupEvent += PickupCheck;

        networkObjectReference = new(NetworkObject);

        originalMaterial = GetComponentInChildren<SkinnedMeshRenderer>().material;
        originalMesh = GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
    }
    private void Update()
    {
        if (!IsOwner) return;

        Handleinput();
    }

    private void Handleinput()
    {
        sbyte slotIndex = (sbyte)(
            inputManager.Slot1ThisFrame() ? 0 : 
            inputManager.Slot2ThisFrame() ? 1 : 
            inputManager.Slot3ThisFrame() ? 2 : 
            inputManager.Slot4ThisFrame() ? 3 : 
            inputManager.Slot5ThisFrame() ? 4 : 
        -1);

        if (slotIndex != -1) SelectSlotRpc(slotIndex, networkObjectReference);

        if (inputManager.DroppedThisFrame() && InventorySlots[CurrentSlot.Value].itemNetworkObject != null) 
        {
            DropItemRpc(networkObjectReference);
        }
        if (inputManager.RightBumperThisFrame())
        {
            BumperNavigation(true);
        }
        if (inputManager.LeftBumperThisFrame())
        {
            BumperNavigation(false);
        }
        if (inputManager.ShootPressed())
        {
            if (InventorySlots[CurrentSlot.Value].itemInfo == null) return;

            switch(InventorySlots[CurrentSlot.Value].itemInfo.ItemType)
            {
                case ItemType.Equippable:
                    EquipSuitRpc(networkObjectReference);
                    break;
                case ItemType.Usable:  
                    IUsable usable = InventorySlots[CurrentSlot.Value].itemInfo.GetComponent<IUsable>();
                    usable.Use(networkObjectReference);
                    break;

            }
        }
        ScrollWheelNavigation(inputManager.ScrollWheelMoved().y);
    }

    private void ScrollWheelNavigation(float value)
    {
        if (value == 0) return;
        
        int slotOffset = value > 0 ? -1 : 1;
        sbyte newSlot = (sbyte)((CurrentSlot.Value + slotOffset + 5) % 5); // Ensures cyclic behavior

        SelectSlotRpc(newSlot, networkObjectReference);
    }

    private void BumperNavigation(bool value)
    {
        int newSlot = value ? (CurrentSlot.Value == 0 ? 4 : CurrentSlot.Value - 1): 
                              (CurrentSlot.Value == 4 ? 0 : CurrentSlot.Value + 1);
        SelectSlotRpc((sbyte)newSlot, networkObjectReference);
    }


    private void PickupCheck(RaycastHit hit)
    {
        for (int i = 0; i < InventorySlots.Length; i++)
        {
            if (InventorySlots[i].itemNetworkObject == null)
            {
                if (hit.collider.TryGetComponent(out NetworkObject networkObject))
                {
                    int slot;

                    if (InventorySlots[i] == InventorySlots[CurrentSlot.Value] || InventorySlots[CurrentSlot.Value] == null) 
                    {
                        slot = CurrentSlot.Value;
                    }
                    else 
                    {
                        slot = i;
                    }

                    PickupItemRpc((sbyte)slot ,networkObject, networkObjectReference);

                    return;
                }
            }
        }
    }
    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    private void EquipSuitRpc(NetworkObjectReference playerReference)
    {
        if (playerReference.TryGet(out NetworkObject playerNetworkObject))
        {
            Inventory inventory = playerNetworkObject.GetComponent<Inventory>();
            DivingSuit suit = inventory.InventorySlots[inventory.CurrentSlot.Value].itemInfo.equippable as DivingSuit;

            if (inventory.currentSuit != null)
            {
                if (inventory.currentSuit.Level > suit.Level && IsOwner)
                {
                    Debug.LogWarning("Your Equipped Suit is better then that one!");
                    return;
                }
            }

            if (IsOwner)
            {
                suit.SetEffects(playerNetworkObject);

                UISetArmorEvent?.Invoke(suit.SuitImage);

                RemoveItemBySlotRpc(true, CurrentSlot.Value, playerReference);
            }

            inventory.currentSuit = suit;

            inventory.GetComponentInChildren<SkinnedMeshRenderer>().material = suit.SuitMaterial;
            inventory.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = suit.SuitMesh;
        }
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void PickupItemRpc(sbyte slot, NetworkObjectReference itemReference, NetworkObjectReference playerReference)
    {
        if (playerReference.TryGet(out NetworkObject playerNetworkObject))
        {
            if (itemReference.TryGet(out NetworkObject itemNetworkObject))
            {
                Inventory inventory = playerNetworkObject.GetComponent<Inventory>();
                
                SelectSlotRpc(slot, playerReference);

                ParentConstraint parentConstraint = itemNetworkObject.GetComponent<ParentConstraint>();

                ItemInfo info = itemNetworkObject.GetComponent<ItemInfo>();

                if (IsOwner)
                {
                    CurrentWeight.Value += itemNetworkObject.GetComponent<ItemInfo>().ItemWeight.Value;

                    info.TogglePickedUpRpc(true);

                    UISetSlotImageEvent?.Invoke(slot, true);
                }

                if (parentConstraint.sourceCount > 0)
                {
                    parentConstraint.RemoveSource(0);
                }

                ConstraintSource constraintSource = new()
                {
                    sourceTransform = inventory.handPos,
                    weight = 1
                };

                parentConstraint.AddSource(constraintSource);
                
                itemNetworkObject.transform.localScale = info.originalScale;

                itemNetworkObject.gameObject.layer = 2;

                itemNetworkObject.transform.LookAt(handPos.forward);

                if (info.IsOnTable.Value)
                {
                    BarteringTable.Singleton.RemoveFromTableRpc(info.gameObject.GetInstanceID());
                }

                if (IsServer)
                {
                    itemNetworkObject.ChangeOwnership(playerNetworkObject.OwnerClientId);

                    itemNetworkObject.TrySetParent(playerNetworkObject, false);
                }

                inventory.InventorySlots[slot] = new()
                {
                    itemNetworkObject = itemNetworkObject,
                    
                    itemInfo = info,

                };

            }
            else Debug.LogError("Failed to get =ITEM= Reference in Inventory at PickupItemRpc");
        }
        else Debug.LogError("Failed to get =PLAYER= Reference in Inventory at PickupItemRpc");
    }
    
    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void DropItemRpc(NetworkObjectReference playerReference)
    {
        if (playerReference.TryGet(out NetworkObject playerNetworkObject))
        {
            if (playerNetworkObject.TryGetComponent(out Inventory inventory))
            {
                InventorySlot slot = inventory.InventorySlots[inventory.CurrentSlot.Value];

                if (IsServer)
                {
                    slot.itemNetworkObject.TryRemoveParent();

                }

                slot.itemNetworkObject.GetComponent<ParentConstraint>().RemoveSource(0);

                if (IsOwner)
                {
                    slot.itemNetworkObject.GetComponent<Rigidbody>().position = inventory.dropPos.position;
                    slot.itemNetworkObject.transform.position = inventory.dropPos.position;

                    slot.itemInfo.TogglePickedUpRpc(false);

                    CurrentWeight.Value -= slot.itemInfo.ItemWeight.Value;
                }   


                slot.itemNetworkObject.gameObject.layer = 6;

                inventory.InventorySlots[inventory.CurrentSlot.Value] = new()
                {
                    itemInfo = null,
                    itemNetworkObject = null,
                };
                UISetSlotImageEvent?.Invoke(inventory.CurrentSlot.Value, false);
            }
        }
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void SelectSlotRpc(sbyte value, NetworkObjectReference playerReference)
    {
        if (IsOwner) 
        {
            CurrentSlot.Value = value;
            UISelectSlotEvent?.Invoke(value);
        }
        if (playerReference.TryGet(out NetworkObject playerNetworkObject))
        {
            Inventory inventory = playerNetworkObject.GetComponent<Inventory>();

            for (int i = 0; i < inventory.InventorySlots.Length; i++)
            {
                if (inventory.InventorySlots[i].itemNetworkObject != null)
                {
                    if (inventory.InventorySlots[i] == inventory.InventorySlots[value])
                    {
                        inventory.InventorySlots[i].itemNetworkObject.gameObject.SetActive(true);
                    }
                    else 
                    {
                        inventory.InventorySlots[i].itemNetworkObject.gameObject.SetActive(false);
                    }
                }
            }

        }
    }
    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Reliable)]
    public void RemoveItemBySlotRpc(bool despawn, sbyte value, NetworkObjectReference playerReference)
    {
        if (playerReference.TryGet(out NetworkObject playerNetworkObject))
        {
            Inventory inventory = playerNetworkObject.GetComponent<Inventory>();

            if (IsServer && despawn && inventory.InventorySlots[value].itemNetworkObject != null)
            {
                 inventory.InventorySlots[value].itemNetworkObject.Despawn(true);
            }

            inventory.InventorySlots[value] = new()
            {
                itemInfo = null,

                itemNetworkObject = null,
            };
            if (IsOwner) UISetSlotImageEvent?.Invoke(value, false);
        }
    }
    [Rpc(SendTo.Server, Delivery = RpcDelivery.Reliable)]
    public void AddItemToInventoryRpc(int id, NetworkObjectReference reference)
    {
        if (reference.TryGet(out NetworkObject playerNetworkObject))
        {
            if (playerNetworkObject.TryGetComponent(out Inventory inventory))
            {
                foreach (var item in GameManager.Singleton.Items)
                {
                    if (id == item.ID)
                    {
                        NetworkObject networkObject = Instantiate(item.gameObject, inventory.dropPos.position + Vector3.up, Quaternion.identity).GetComponent<NetworkObject>();
                        networkObject.Spawn();

                        for (int i = 0; i < inventory.InventorySlots.Length; i++)
                        {
                            if (inventory.InventorySlots[i].itemNetworkObject == null)
                            {
                                int slot;

                                if (inventory.InventorySlots[i] == inventory.InventorySlots[inventory.CurrentSlot.Value] || inventory.InventorySlots[inventory.CurrentSlot.Value] == null) 
                                {
                                    slot = inventory.CurrentSlot.Value;
                                }
                                else 
                                {
                                    slot = i;
                                }

                                inventory.PickupItemRpc((sbyte)slot ,networkObject, networkObjectReference);

                                return;
                            }
                        }
                        
                    }
                }
            }
        }
    }
}