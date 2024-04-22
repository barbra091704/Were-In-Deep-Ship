using System;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton {get; private set;}
    public NetworkVariable<int> Credits = new(100);
    public NetworkVariable<int> CurrentBoatID = new();
    public Transform PierTransform;
    public SceneUI sceneUI;
    [HideInInspector] public NetworkVariable<LocationData> CurrentLocationData = new();
    [HideInInspector] public NetworkList<PlayerData> PlayerDatas;
    [HideInInspector] public Scene CurrentLocationScene;
    [HideInInspector] public Scene PreviousLocationScene;
    public LocationData[] Locations;
    public DepthDepletionRate[] OxygenDepthDepletionRates;
    public ItemInfo[] Items;

    void Awake()
    {
        PlayerDatas = new();

        if (Singleton != null && Singleton != this) Destroy(this);
        else Singleton = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientDisconnectCallback += RemovePlayerDataRpc;

            CurrentLocationData.Value = new(){
                Name = Locations[0].Name,
                ID = Locations[0].ID,
                Cost = Locations[0].Cost,
            };
        }

        SceneManager.SetActiveScene(gameObject.scene);

        CurrentLocationData.OnValueChanged += HandleNewLocation;
        
        TimeManager.Singleton.TickEventTriggered += CheckGameTime;
    }

    private void CheckGameTime(float time)
    {
        if (time >= (TimeManager.Singleton.maxGameTimeInMinutes * 60))
        {   
            CurrentLocationData.Value = new(){
                Name = Locations[0].Name,
                ID = Locations[0].ID,
                Cost = Locations[0].Cost,
            };
        }
    }
    private void HandleNewLocation(LocationData previousValue, LocationData newValue)
    {
        if (!previousValue.Equals(newValue))
        {
            StartCoroutine(DelayedHandling(previousValue, newValue));
        }
    }

    private IEnumerator DelayedHandling(LocationData previousValue, LocationData newValue)
    {
        yield return new WaitForSeconds(2f);

        PreviousLocationScene = CurrentLocationScene;

        if (IsServer)
        {
            if (newValue.ID != 0)
            {
                if (previousValue.ID != 0) FindObjectOfType<LevelGenerator.Scripts.LevelGenerator>().Cleanup();

                NetworkManager.SceneManager.OnLoadEventCompleted += UnloadPreviousLocation;

                yield return new WaitForEndOfFrame();

                NetworkManager.SceneManager.LoadScene(newValue.Name, LoadSceneMode.Additive);

                CurrentLocationScene = SceneManager.GetSceneByName(newValue.Name);

                yield return new WaitForEndOfFrame();

                TimeManager.Singleton.StartGameTime();
            }
            else
            {
                FindObjectOfType<LevelGenerator.Scripts.LevelGenerator>().Cleanup();

                NetworkManager.SceneManager.OnLoadEventCompleted += UnloadPreviousLocation;

                NetworkManager.SceneManager.UnloadScene(PreviousLocationScene);

                TimeManager.Singleton.ResetGameTime();
            }
        }
        if (newValue.ID == 0)
        {
            PierTransform.gameObject.SetActive(true);
        }
        else
        {
            PierTransform.gameObject.SetActive(false);
        }
    }


    private void UnloadPreviousLocation(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!PreviousLocationScene.isLoaded) return;
        // Unsubscribe from the OnLoadEventCompleted event
        NetworkManager.SceneManager.OnLoadEventCompleted -= UnloadPreviousLocation;

        // Unload the previous scene
        NetworkManager.SceneManager.UnloadScene(PreviousLocationScene);
    }

    public void TriggerDeathFromQuestFailure(int questID)
    {
        print("Faild Quest: " + questID);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void AddPlayerDataRpc(FixedString32Bytes name, ulong id, bool isAlive, NetworkObjectReference reference)
    {
        PlayerData playerdata = new()
        {
            Name = name,
            ID = id,
            IsAlive = isAlive,
            Reference = reference
        };
        PlayerDatas.Add(playerdata);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void RemovePlayerDataRpc(ulong disconnectedClientId)
    {
        for (int i = 0; i < PlayerDatas.Count; i++)
        {
            if (PlayerDatas[i].ID == disconnectedClientId){
                PlayerDatas.RemoveAt(i);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void SetPlayerAliveStateServerRpc(ulong ClientId, bool value)
    {
        for (int i = 0; i < PlayerDatas.Count; i++)
        {
            if (PlayerDatas[i].ID == ClientId){
                PlayerData information = PlayerDatas[i];
                information.IsAlive = value;
                PlayerDatas[i] = information;
            }
        }
    }
}

[Serializable]
public struct LocationData : INetworkSerializable{
    public string Name;
    public int ID;
    public int Cost;
    public int AbundantEntityID;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref ID);
        serializer.SerializeValue(ref Cost);
        serializer.SerializeValue(ref AbundantEntityID);
    }
}

[Serializable]
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
    public FixedString32Bytes Name;
    public NetworkObjectReference Reference;
    public ulong ID;
    public bool IsAlive;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref ID);
        serializer.SerializeValue(ref IsAlive);
        serializer.SerializeValue(ref Reference);
    }
    public bool Equals(PlayerData data)
    {
        return Name.Equals(data.Name) && ID == data.ID && IsAlive == data.IsAlive;
    }
}
[Serializable]
public struct DepthDepletionRate
{
    public int depthThreshold;
    public float depletionRate;
}