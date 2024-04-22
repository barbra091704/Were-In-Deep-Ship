using UnityEngine.UI;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class PlayerUI : NetworkBehaviour
{
    [SerializeField] private Image[] slotBackroundImages;
    [SerializeField] private Image[] slotImages;
    [SerializeField] private Image suitBackroundImage;
    [SerializeField] private Image suitImage;
    public Slider interactionSlider;
    public Slider staminaSlider;
    public Transform inventoryUI;
    public Image QuestItemSprite;
    public TMP_Text QuestItemName;
    public TMP_Text QuestDaysLeft;
    public TMP_Text HealthText;
    public TMP_Text OxygenLevelText;
    public TMP_Text OxygenCapacityText;
    public TMP_Text ArmorLevelText;
    public TMP_Text DepthText;
    public TMP_Text DepthRateText;

    [Header("ITEM HUD")]
    public GameObject ItemHUD;
    public TMP_Text ItemName;
    public TMP_Text ItemValue;
    public TMP_Text ItemWeight;
    public Image ItemImage;

    private int questDays;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner || SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Lobby")) return; 

        Inventory inventory = GetComponent<Inventory>();
        inventory.UISelectSlotEvent += SelectSlot;
        inventory.UISetSlotImageEvent += SetItemImageUI;
        inventory.UISetArmorEvent += SetArmorUI;

        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        playerHealth.Health.OnValueChanged += SetHealthDisplay;
        playerHealth.Resistance.OnValueChanged += SetArmorDisplay;

        Oxygen oxygen = GetComponent<Oxygen>();
        oxygen.CurrentOxygenTankLevel.OnValueChanged += SetOxygenLevelDisplay;
        oxygen.CurrentOxygenTankCapacity.OnValueChanged += SetOxygenCapacityDisplay;
        oxygen.DepthBeforeCustomRates.OnValueChanged += SetDepthRateDisplay;

        DepthSensor sensor = GetComponent<DepthSensor>();
        sensor.Depth.OnValueChanged += SetDepthDisplay;

        Interaction interaction = GetComponent<Interaction>();
        interaction.SliderValueEvent += SetInteractionSliderValue;
        interaction.HoveredItemInfoEvent += SetHoverDisplayToItemInfo;

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        playerMovement.CurrentStamina.OnValueChanged += SetPlayerMovementCurrentStamina;

        QuestManager.Singleton.SetQuestItemInfo += SetQuestItemInfo;
        TimeManager.Singleton.DaysPassed.OnValueChanged += SetDaysLeft;
    }


    public override void OnNetworkDespawn()
    {
        if (!IsOwner || SceneManager.GetActiveScene() == SceneManager.GetSceneByName("Lobby")) return; 

        Inventory inventory = GetComponent<Inventory>();
        inventory.UISelectSlotEvent -= SelectSlot;
        inventory.UISetSlotImageEvent -= SetItemImageUI;
        inventory.UISetArmorEvent -= SetArmorUI;

        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        playerHealth.Health.OnValueChanged -= SetHealthDisplay;
        playerHealth.Resistance.OnValueChanged -= SetArmorDisplay;

        Oxygen oxygen = GetComponent<Oxygen>();
        oxygen.CurrentOxygenTankLevel.OnValueChanged -= SetOxygenLevelDisplay;
        oxygen.CurrentOxygenTankCapacity.OnValueChanged -= SetOxygenCapacityDisplay;
        oxygen.DepthBeforeCustomRates.OnValueChanged -= SetDepthRateDisplay;

        DepthSensor sensor = GetComponent<DepthSensor>();
        sensor.Depth.OnValueChanged -= SetDepthDisplay;

        Interaction interaction = GetComponent<Interaction>();
        interaction.SliderValueEvent -= SetInteractionSliderValue;
        interaction.HoveredItemInfoEvent -= SetHoverDisplayToItemInfo;

        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        playerMovement.CurrentStamina.OnValueChanged -= SetPlayerMovementCurrentStamina;

        QuestManager.Singleton.SetQuestItemInfo -= SetQuestItemInfo;
        TimeManager.Singleton.DaysPassed.OnValueChanged -= SetDaysLeft;
    }

    private void SelectSlot(int current)
    {
        for (int i = 0; i < slotBackroundImages.Length; i++)
        {
            if (slotBackroundImages[i].transform.localScale != new Vector3(1,1,1)){
                slotBackroundImages[i].transform.localScale = new Vector3(1,1,1);
            }
        }
        slotBackroundImages[current].transform.localScale = new Vector3(1.1f,1.1f,1.1f);
    }
    private void SetQuestItemInfo(Sprite sprite, string name, int days)
    {
        QuestItemName.text = name;
        QuestItemSprite.sprite = sprite;

        questDays = days;
        QuestDaysLeft.text = $"{questDays} Days Remaining.";
    }
    private void SetDaysLeft(int old, int value)
    {
        questDays--;
        QuestDaysLeft.text = $"{questDays} Days Remaining.";
    }

    private void SetPlayerMovementCurrentStamina(float old, float value)
    {
        staminaSlider.value = value;
    }    
    private void SetInteractionSliderValue(float value)
    {
        interactionSlider.value = value;
    }
    private void SetItemImageUI(int value, bool i) // true is Add false is Remove
    {
        slotImages[value].enabled = i;
    }
    private void SetArmorUI(Sprite sprite)
    {
        suitImage.enabled = true;
        suitImage.sprite = sprite;
    }
    private void SetOxygenLevelDisplay(float old, float value)
    {
        OxygenLevelText.text = $"Oxygen: {(int)value}";
    }
    private void SetOxygenCapacityDisplay(int old, int value)
    {
        OxygenCapacityText.text = $"/ {value}";
    }
    private void SetArmorDisplay(int previousValue, int value)
    {
        ArmorLevelText.text = $"Armor: {value} / 50";
    }
    private void SetDepthRateDisplay(int old, int value)
    {
        DepthRateText.text = $"Suit Depth: {value}";
    }
    private void SetHealthDisplay(int previousValue, int value)
    {
        HealthText.text = $"Health: {value} / 100";
    }
    private void SetDepthDisplay(int old, int value)
    {
        DepthText.text = $"Depth: {value}m";
    }
    private void SetHoverDisplayToItemInfo(Collider collider)
    {
        if (collider != null)
        {
            CancelInvoke(nameof(ToggleItemHUD));

            ItemHUD.SetActive(true);

            ItemInfo info = collider.GetComponent<ItemInfo>();

            ItemName.text = info.ItemName;
            ItemValue.text = $"Value: {info.ItemValue.Value}";
            ItemWeight.text = $"Weight: {info.ItemWeight.Value}";
            ItemImage.sprite = info.itemImage;
            Invoke(nameof(ToggleItemHUD),0.5f);
        }
        
    }
    private void ToggleItemHUD()
    {
        ItemHUD.SetActive(false);
    }

}
