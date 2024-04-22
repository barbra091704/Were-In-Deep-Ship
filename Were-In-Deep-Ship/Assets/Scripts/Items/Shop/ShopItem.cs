using Unity.Netcode;
using UnityEngine;

public class ShopItem : MonoBehaviour, IInteractable
{
    public ItemInfo PurchaseItem;
    public int itemCost;
    private ShopRack shopRack;

    void Awake()
    {
        shopRack = GetComponentInParent<ShopRack>();
    }

    public void Interact(RaycastHit hit, NetworkObject Player)
    {
        if (GameManager.Singleton.Credits.Value >= itemCost)
        {
            shopRack.currentSelectedItem = this;
            shopRack.OpenMenu(Player);
        }
    }

}
