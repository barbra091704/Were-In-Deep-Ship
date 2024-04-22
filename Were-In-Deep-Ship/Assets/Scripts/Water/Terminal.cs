using System;
using Cinemachine;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Terminal : NetworkBehaviour,IInteractable
{
    [SerializeField] public NetworkVariable<LocationData> selectedLocation = new();
    public NetworkVariable<bool> IsInUse = new();
    [SerializeField] private TMP_Text CurrencyDisplay;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Canvas canvas;

    private NetworkObject currentPlayer;

    private void Start(){
        GameManager.Singleton.Credits.OnValueChanged += SetCurrencyDisplay;
    }

    private void SetCurrencyDisplay(int previousValue, int newValue)
    {
        CurrencyDisplay.text = $"${newValue}";
    }


    [Rpc(SendTo.Server)]
    public void SetLocationRpc(int ID)
    {
        for (int i = 0; i < GameManager.Singleton.Locations.Length; i++)
        {
            if(GameManager.Singleton.Locations[i].ID == ID){
                if (GameManager.Singleton.Locations[i].Cost < GameManager.Singleton.Credits.Value){
                    selectedLocation.Value = GameManager.Singleton.Locations[i];
                    print("Set Location Successfully [ SetLocationRpc ] - Terminal");
                }
                else Debug.LogWarning("Not enough Currency to select Location. [ SetLocationRpc ] - Terminal");
            }
        }
    }
    [Rpc(SendTo.Server)]
    public void TravelToLocationRpc()
    {
        GameManager.Singleton.CurrentLocationData.Value = selectedLocation.Value;
        Exit();
    }

    public void Interact(RaycastHit hit, NetworkObject Player)
    {
        SetIsInUseRpc(true);
        canvas.worldCamera = Player.GetComponent<PlayerMovement>().playerCam;
        virtualCamera.Priority = 2;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        currentPlayer = Player;
        GUIManager.Singleton.AddGUI();
    }
    public void Exit()
    {
        SetIsInUseRpc(false);
        canvas.worldCamera = null;
        virtualCamera.Priority = 0;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GUIManager.Singleton.RemoveGUI();
        currentPlayer = null;
    }
    [Rpc(SendTo.Server)]
    public void SetIsInUseRpc(bool i)
    {
        IsInUse.Value = i;
    }
}
