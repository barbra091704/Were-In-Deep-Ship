using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShopRack : NetworkBehaviour
{
    public List<Transform> rackLocation = new();
    public ShopItem currentSelectedItem;
    public Canvas purchaseCanvas;
    public TMP_Text cost;

    private NetworkObject currentPlayer;
    public void OpenMenu(NetworkObject player)
    {
        if (player != null)
        {
            cost.text = $"Cost: ${currentSelectedItem.itemCost}";
            currentPlayer = player;
            purchaseCanvas.enabled = true;
            GUIManager.Singleton.AddGUI();
        }
    }
    public void CloseMenu()
    {
        if (currentPlayer != null)
        {
            purchaseCanvas.enabled = false;
            currentPlayer = null;
            GUIManager.Singleton.RemoveGUI();
        }
    }
    public void PurchaseItem()
    {
        PurchaseItemRpc();
        CloseMenu();
    }

    [Rpc(SendTo.Server)]
    public void PurchaseItemRpc()
    {
        if (currentSelectedItem == null || currentPlayer == null) return;

        if (currentPlayer.TryGetComponent(out Inventory inventory))
        {
            inventory.AddItemToInventoryRpc(currentSelectedItem.PurchaseItem.ID, currentPlayer);
            GameManager.Singleton.Credits.Value -= currentSelectedItem.itemCost;
        }
    }
}
