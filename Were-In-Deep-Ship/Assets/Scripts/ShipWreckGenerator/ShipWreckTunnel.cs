using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShipWreckTunnel : MonoBehaviour, IInteractable
{
    [Range(1, 9)] public int Channel = 1;
    public bool OverridePosition = false;
    public Transform overrideTransform;

    private static Dictionary<int, List<ShipWreckTunnel>> tunnelsByChannel = new();

    void Awake()
    {
        // Ensure there's a list for this channel
        if (!tunnelsByChannel.ContainsKey(Channel))
        {
            tunnelsByChannel[Channel] = new List<ShipWreckTunnel>();
        }

        // Add this tunnel to the list for its channel
        tunnelsByChannel[Channel].Add(this);
    }

    void OnDestroy()
    {
        // Remove this tunnel from its channel list
        if (tunnelsByChannel.ContainsKey(Channel))
        {
            tunnelsByChannel[Channel].Remove(this);
            // Optionally, clean up the dictionary entry if no tunnels are left for this channel
            if (tunnelsByChannel[Channel].Count == 0)
            {
                tunnelsByChannel.Remove(Channel);
            }
        }
    }

    public void Interact(RaycastHit hit = default, NetworkObject player = null)
    {
        if (player == null) return;

        // Check if there's another tunnel for this channel
        if (tunnelsByChannel.TryGetValue(Channel, out List<ShipWreckTunnel> channelTunnels))
        {
            // Find the other tunnel that is not 'this'
            foreach (var tunnel in channelTunnels)
            {
                if (tunnel != this) // Found the other tunnel
                {
                    ShipWreckTunnel targetTunnel = tunnel;
                    PlayerMovement movement = player.GetComponent<PlayerMovement>();
                    if (targetTunnel.OverridePosition && targetTunnel.overrideTransform != null)
                    {
                        movement.transform.position = targetTunnel.overrideTransform.position;
                    }
                    else
                    {
                        movement.transform.position = targetTunnel.transform.position;
                    }
                    movement.CurrentState = movement.GroundState;
                    break; // Exit the loop after handling teleportation
                }
            }
        }
    }

}
