using UnityEngine;
using Mirror;
using System;

public class HostManager : NetworkBehaviour
{
    public static HostManager Instance { get; private set; }

    [SyncVar(hook = nameof(OnHostCodeChanged))]
    public string hostCode = "";

    [SyncVar(hook = nameof(OnNetworkAddressChanged))]
    public string networkAddress = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        hostCode = GenerateHostCode();
        networkAddress = NetworkManager.singleton.networkAddress;
        Debug.Log($"HostManager khởi tạo trên server: Mã host: {hostCode}, Địa chỉ IP: {networkAddress}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"HostManager khởi tạo trên client, Mã host: {hostCode}, Địa chỉ IP: {networkAddress}");
    }

    void OnHostCodeChanged(string oldCode, string newCode)
    {
        Debug.Log($"Mã host đã thay đổi trên client: {newCode}");
    }

    void OnNetworkAddressChanged(string oldAddress, string newAddress)
    {
        Debug.Log($"Địa chỉ IP đã thay đổi trên client: {newAddress}");
    }

    private string GenerateHostCode()
    {
        const string chars = "0123456789";
        char[] code = new char[6];
        for (int i = 0; i < 6; i++)
        {
            code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        return new string(code);
    }

    [Command(requiresAuthority = false)]
    public void CmdValidateHostCode(string inputHostCode, NetworkConnectionToClient conn = null)
    {
        if (conn == null)
        {
            Debug.LogError("NetworkConnectionToClient là null trong CmdValidateHostCode!");
            return;
        }

        if (inputHostCode == hostCode)
        {
            Debug.Log($"Client {conn.connectionId} xác thực thành công với mã host: {inputHostCode}");
            TargetValidationResult(conn, true, "Xác thực mã host thành công!");
        }
        else
        {
            Debug.LogError($"Client {conn.connectionId} xác thực thất bại: Mã host nhập {inputHostCode} không khớp với {hostCode}");
            TargetValidationResult(conn, false, $"Mã host không đúng: {inputHostCode}");
            conn.Disconnect();
        }
    }

    [TargetRpc]
    private void TargetValidationResult(NetworkConnectionToClient conn, bool success, string message)
    {
        Debug.Log($"Kết quả xác thực trên client: {message}");
    }
}