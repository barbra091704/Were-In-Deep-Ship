using Unity.Netcode;
using UnityEngine;
public interface IInteractable
{
    void Interact(RaycastHit hit, NetworkObject Player);
}   
