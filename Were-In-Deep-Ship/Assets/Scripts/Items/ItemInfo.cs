using KWS;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

public enum ItemType{
    Item,
    Equippable,
    Special,
    Key,
    Usable
}
public enum KeyType{
    None,
    Door,
    Other
}

[RequireComponent(typeof(Rigidbody), typeof(ParentConstraint), typeof(NetworkObject))]
[RequireComponent(typeof(ClientNetworkTransform), typeof(NetworkRigidbody))]
public class ItemInfo : NetworkBehaviour
{   
    public int ID;
    public ItemType ItemType;
    public KeyType KeyType;
    public Sprite itemImage;
    [Range(1,3)] public NetworkVariable<int> ItemTier = new();
    public NetworkVariable<int> ItemWeight = new();
    public NetworkVariable<int> ItemValue = new();
    public NetworkVariable<bool> IsPickedUp = new();
    public NetworkVariable<bool> IsOnTable = new();
    public NetworkVariable<bool> IsOnFloatingPlatform = new();
    [SerializeReference] public ScriptableObject equippable;
    [SerializeField] private float gravitySpeed;
    [SerializeField] private float groundOffset = 1;
    [SerializeField] private Vector2Int minMaxWeight;
    [SerializeField] private Vector2Int minMaxValue;
    [SerializeField] private Vector2Int minMaxDrag;
    [SerializeField] private LayerMask layerMask;
    [HideInInspector] public Quaternion originalRotation;
    [HideInInspector] public Vector3 originalScale;
    public string ItemName;
    private Rigidbody rb;
    private WaterSystem waterSurface;
    public bool showVariables;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        waterSurface = FindObjectOfType<WaterSystem>();
        originalRotation = Quaternion.identity;
        originalScale = transform.localScale;
    }

    public void Start()
    {
        if (IsServer)
        {
            ItemWeight.Value = Random.Range(minMaxWeight.x, minMaxWeight.y);
            ItemValue.Value = Random.Range(minMaxValue.x, minMaxValue.y);
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
                transform.position = hit.point;
        }
    }
    void FixedUpdate()
    {
        WaterSurfaceData data = waterSurface.GetCurrentWaterSurfaceData(transform.position);
        if (!data.IsActualDataReady) return;
        if (!IsPickedUp.Value && !IsTouchingGround()){
            rb.drag = transform.position.y < data.Position.y ? minMaxDrag.y : minMaxDrag.x; // x is air y is underwater
            rb.AddForce(Vector3.down * gravitySpeed, ForceMode.Force);
        }
    }
    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject) 
    {
        if (parentNetworkObject == null || !IsServer) return;
        if (parentNetworkObject.CompareTag("Player"))
        {
            IsOnFloatingPlatform.Value = false;
            rb.isKinematic = false; 
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            GetComponent<Collider>().isTrigger = false;
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        transform.rotation = originalRotation;
        if (collision.collider.gameObject.layer == 4)
            waterSurface = collision.transform.GetComponent<WaterSystem>();

        if (!IsServer || IsOnFloatingPlatform.Value || IsPickedUp.Value) return;

        if (collision.transform.TryGetComponent(out FloatingPlatform component))
        {
            IsOnFloatingPlatform.Value = true;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
            GetComponent<Collider>().isTrigger = true;
            GetComponent<NetworkObject>().TrySetParent(collision.transform);
        }
    }


    bool IsTouchingGround() { return Physics.Raycast(transform.position, Vector3.down, 0.1f * groundOffset, layerMask); }

    [Rpc(SendTo.Server,RequireOwnership = false)]
    public void TogglePickedUpRpc(bool value)
    {
        IsPickedUp.Value = value;
    }

}


// CUSTOM EDITOR

#if UNITY_EDITOR
[CustomEditor(typeof(ItemInfo))]
public class ItemInfoAttributes : Editor {
    ItemInfo myClass;

    void OnEnable() {
        myClass = (ItemInfo)target;
    }

    public override void OnInspectorGUI() {
        EditorGUILayout.Space();

        myClass.ItemType = (ItemType)EditorGUILayout.EnumPopup(new GUIContent("Item Type", "Choose the Items type to use different corresponding behaviours."), myClass.ItemType);
        switch(myClass.ItemType){
            case ItemType.Item:
                break;

            case ItemType.Usable:
                break;
            case ItemType.Equippable:
                myClass.equippable = (ScriptableObject)EditorGUILayout.ObjectField(new GUIContent("IEquippable Module", "equippable data"), myClass.equippable, typeof(IEquippable), false);

                break;

            case ItemType.Special:
                break;

            case ItemType.Key:
                myClass.KeyType = (KeyType)EditorGUILayout.EnumPopup(new GUIContent("Key Type","Key type to be used on different doors."), myClass.KeyType);
                break;
        }
        DrawCustomInspector();
    }

    private void DrawCustomInspector() {
        serializedObject.Update();
        GUIStyle headerStyle = new(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        EditorGUILayout.Space(15);

        EditorGUILayout.LabelField("--- Generic Parameters ---", headerStyle);    

        EditorGUILayout.Space();

        EditorGUILayout.Space();

        SerializedProperty itemWeightInternal = serializedObject.FindProperty("ItemWeight").FindPropertyRelative("m_InternalValue");


        SerializedProperty itemValueInternal = serializedObject.FindProperty("ItemValue").FindPropertyRelative("m_InternalValue");


        SerializedProperty itemTierInternal = serializedObject.FindProperty("ItemTier").FindPropertyRelative("m_InternalValue");

        itemTierInternal.intValue = EditorGUILayout.IntField(new GUIContent("Item Tier","Internal Value of Network Variable ItemTier | represents the Tier of the Item."), itemTierInternal.intValue);

        EditorGUILayout.Space();

        itemWeightInternal.intValue = EditorGUILayout.IntField(new GUIContent("Item Weight","Internal Value of Network Variable ItemWeight | Current weight of the Item | GENERATED AT RUNTIME."), itemWeightInternal.intValue);

        EditorGUILayout.Space();

        itemValueInternal.intValue = EditorGUILayout.IntField(new GUIContent("Item Value","Internal Value of Network Variable ItemValue | Current value of the Item | GENERATED AT RUNTIME."), itemValueInternal.intValue);
        
        EditorGUILayout.Space();

        myClass.showVariables = EditorGUILayout.BeginFoldoutHeaderGroup(myClass.showVariables, "Settings");
        if (myClass.showVariables) 
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ID"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ItemName"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("layerMask"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("gravitySpeed"));
            
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("groundOffset"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemImage"));

            EditorGUILayout.Space();
            // Display Min MaxWeight with "Min: " and "Max: " labels
            SerializedProperty minMaxWeight = serializedObject.FindProperty("minMaxWeight");

            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Min Weight: ", GUILayout.Width(85));

            EditorGUILayout.PropertyField(minMaxWeight.FindPropertyRelative("x"), GUIContent.none);

            GUILayout.Label("Max Weight: ", GUILayout.Width(85));

            EditorGUILayout.PropertyField(minMaxWeight.FindPropertyRelative("y"), GUIContent.none);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Display Min MaxValue with "Min: " and "Max: " labels
            SerializedProperty minMaxValue = serializedObject.FindProperty("minMaxValue");

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Min Value: ", GUILayout.Width(85));

            EditorGUILayout.PropertyField(minMaxValue.FindPropertyRelative("x"), GUIContent.none);

            GUILayout.Label("Max Value: ", GUILayout.Width(85));

            EditorGUILayout.PropertyField(minMaxValue.FindPropertyRelative("y"), GUIContent.none);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        
            SerializedProperty minMaxDrag = serializedObject.FindProperty("minMaxDrag");

            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Air Drag: ", GUILayout.Width(85));

            EditorGUILayout.PropertyField(minMaxDrag.FindPropertyRelative("x"), GUIContent.none);

            GUILayout.Label("Water Drag: ", GUILayout.Width(85));

            EditorGUILayout.PropertyField(minMaxDrag.FindPropertyRelative("y"), GUIContent.none);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

}
#endif