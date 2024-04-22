using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GUIManager : NetworkBehaviour
{

    public static GUIManager Singleton { get; private set;}

    public NetworkVariable<bool> isInGui = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int GUIAmountOpen;
    public Canvas UI;
    private PlayerMovement playerMovement;
    private PlayerCamera playerCamera;
    private Interaction interaction;

    void Start()
    {
        if (!IsOwner) return;

        if (Singleton != null && Singleton != this) Destroy(this);
        else Singleton = this;

        isInGui.OnValueChanged += ToggleUI;

        playerMovement = GetComponent<PlayerMovement>();
        interaction = GetComponent<Interaction>();
        playerCamera = GetComponent<PlayerCamera>();
    }

    private void ToggleUI(bool previousValue, bool newValue)
    {
        if(!newValue)
        {
            playerMovement.canMove = true;
            interaction.canInteract = true;
            playerCamera.canLook = true;
            UI.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            playerMovement.canMove = false;
            interaction.canInteract = false;
            playerCamera.canLook = false;
            UI.enabled = false; 
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
    }

    public void AddGUI()
    {
        if (IsOwner)
        {
            if (GUIAmountOpen == 0)
            {
                isInGui.Value = true;
            }
            
            GUIAmountOpen++;
        }
    }

    public void RemoveGUI()
    {
        if (IsOwner)
        {
            if (GUIAmountOpen <= 1)
            {
                isInGui.Value = false;
                GUIAmountOpen = 0;
            }
            else
            {
                GUIAmountOpen--;
            }
        }
    }

}
