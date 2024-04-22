using System.Collections;
using Unity.Netcode;
using UnityEngine;


public class Boat : NetworkBehaviour
{
    public int boatID;

    public override void OnNetworkSpawn()
    {
        GameManager.Singleton.CurrentLocationData.OnValueChanged += CurrentLocationValueChanged;
    }
    public override void OnNetworkDespawn()
    {
        GameManager.Singleton.CurrentLocationData.OnValueChanged -= CurrentLocationValueChanged;
    }


    private void CurrentLocationValueChanged(LocationData previousValue, LocationData newValue)
    {
        print($"Boat Received Callback From GameManager To Travel To: {newValue.Name} [ CurrentLocationValueChanged ] - Boat");

        StartCoroutine(TravelToNewLocation());
    }
    private IEnumerator TravelToNewLocation()
    {
        foreach (var item in GameManager.Singleton.PlayerDatas)
        {
            if (item.ID == NetworkManager.LocalClientId){

                if(item.Reference.TryGet(out NetworkObject playerObj))
                {
                    playerObj.GetComponent<PlayerMovement>().canMove = false;

                    GameManager.Singleton.sceneUI.blackout.Fade(0.5f, 4, 0.5f);
                
                    BoxCollider collider = GetComponent<BoxCollider>();
                    if (!collider.bounds.Contains(playerObj.transform.position))
                    {
                        playerObj.transform.position = RandomPointInBounds(collider.bounds);
                        print("RandomBounds");
                    }

                    yield return new WaitForSeconds(3f);

                    playerObj.GetComponent<PlayerMovement>().canMove = true;

                    break;
                }
            }
        }
    }
    public Vector3 RandomPointInBounds(Bounds bounds) {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }
}

