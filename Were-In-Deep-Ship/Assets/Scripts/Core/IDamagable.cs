using Unity.Netcode;

public interface IDamagable
{
    void TakeDamageRpc(int value, bool applyResistance, RpcParams rpcParams = default);
}

