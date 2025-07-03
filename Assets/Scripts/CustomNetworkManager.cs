using UnityEngine;
using Mirror;
using kcp2k;

public class CustomNetworkManager : NetworkManager
{
    public string clientHostCode = ""; // Biến lưu mã host từ client

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"Server đã khởi động trên địa chỉ: {networkAddress}, cổng: {GetComponent<KcpTransport>().Port}");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log($"Client đã kết nối thành công! clientHostCode: {clientHostCode}");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.LogError($"Client bị ngắt kết nối! clientHostCode: {clientHostCode}");
    }
}