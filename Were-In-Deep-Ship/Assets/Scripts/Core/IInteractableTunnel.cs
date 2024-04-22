using Unity.Netcode;
using UnityEngine;

public class IInteractableTunnel : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject interactableObject;

    public void Interact(RaycastHit hit, NetworkObject Player)
    {
        if(interactableObject.TryGetComponent(out IInteractable interactable)){
            interactable.Interact(hit,Player);
        }
    }
}
