using UnityEngine;
using Unity.Netcode;
using System;


public class QuestManager : NetworkBehaviour, IInteractable
{
    public static QuestManager Singleton;

    public QuestSO CurrentQuest;

    public QuestSO[] QuestLists;

    public float CurrentQuestTime;

    public event Action<Sprite, string, int> SetQuestItemInfo;

    void Awake()
    {
        if (Singleton != null && Singleton != this) Destroy(this);
        else Singleton = this;
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        TimeManager.Singleton.DayEventTriggered += CheckQuestTime;

        int i = UnityEngine.Random.Range(0, QuestLists.Length);

        SetQuestRpc(i);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        TimeManager.Singleton.DayEventTriggered -= CheckQuestTime;
    }

    [Rpc(SendTo.Everyone)]
    public void SetQuestRpc(int i)
    {
        Singleton.CurrentQuest = QuestLists[i];
        Singleton.CurrentQuestTime = 0;

        Singleton.SetQuestItemInfo?.Invoke(Singleton.CurrentQuest.sprite, Singleton.CurrentQuest.ItemInfo.name, Singleton.CurrentQuest.DaysToComplete);
    }
    
    private void CheckQuestTime(int daysPassed)
    {
        if (daysPassed >= CurrentQuest.DaysToComplete)
        {
            GameManager.Singleton.TriggerDeathFromQuestFailure(CurrentQuest.QuestID);
        }
        else
        {
            CurrentQuestTime++;
        }
    }

    [Rpc(SendTo.Server)]
    public void OnQuestCompleteRpc()
    {
        int i = UnityEngine.Random.Range(0, QuestLists.Length);

        GameManager.Singleton.Credits.Value += CurrentQuest.Reward;

        TimeManager.Singleton.DaysPassed.Value = 0;

        SetQuestRpc(i);
    }

    public void Interact(RaycastHit hit, NetworkObject Player)
    {
        if (Player.TryGetComponent<Inventory>(out var inventory))
        {
            if (inventory.InventorySlots[inventory.CurrentSlot.Value].itemNetworkObject != null && inventory.InventorySlots[inventory.CurrentSlot.Value].itemInfo.ID == CurrentQuest.ItemID)
            {
                inventory.RemoveItemBySlotRpc(true, inventory.CurrentSlot.Value, Player);
                OnQuestCompleteRpc();
            }
        }
    }

}
