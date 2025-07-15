using UnityEngine;
using Mirror;
using kcp2k;
using System.Threading.Tasks;
using System.Linq;

public class CustomNetworkManager : NetworkManager
{
    public string clientHostCode = ""; // Biến lưu mã phòng từ client
    private string currentRoomCode = ""; // Biến lưu mã phòng hiện tại của host
    private GoogleSheetsManager sheetsManager;

    public override void Awake()
    {
        // Find GoogleSheetsManager in the scene
        sheetsManager = FindFirstObjectByType<GoogleSheetsManager>();
        if (sheetsManager == null)
        {
            Debug.LogError("GoogleSheetsManager not found in scene! Please add GoogleSheetsManager component to a GameObject.");
        }
    }

    // Được gọi khi server khởi động
    public override async void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"Server đã khởi động trên địa chỉ: {networkAddress}, cổng: {GetComponent<KcpTransport>().Port}");

        // Generate room code and upload to Google Sheet
        if (sheetsManager != null)
        {
            currentRoomCode = GenerateHostCode(); // Generate a 6-digit room code
            string ipAddress = GoogleSheetsManager.GetLocalIPAddress();
            bool success = await sheetsManager.AppendRoomData(currentRoomCode, ipAddress);
            if (success)
            {
                Debug.Log($"Uploaded room code {currentRoomCode} and IP {ipAddress} to Google Sheet.");
                // Update LobbyManager's serverHostCode via the Player Prefab
                var player = NetworkServer.connections.Values.FirstOrDefault()?.identity?.GetComponent<LobbyManager>();
                if (player != null)
                {
                    player.UpdateServerHostCode(currentRoomCode);
                }
            }
            else
            {
                Debug.LogError("Failed to upload room data to Google Sheet.");
            }
        }
    }

    // Được gọi khi server dừng
    public override async void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Server has stopped.");

        // Delete room data from Google Sheet
        if (sheetsManager != null && !string.IsNullOrEmpty(currentRoomCode))
        {
            bool success = await sheetsManager.DeleteRoomData(currentRoomCode);
            if (success)
            {
                Debug.Log($"Successfully deleted room code {currentRoomCode} from Google Sheet.");
            }
            else
            {
                Debug.LogError($"Failed to delete room code {currentRoomCode} from Google Sheet.");
            }
            currentRoomCode = ""; // Reset room code
        }
    }

    // Được gọi khi client kết nối thành công
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log($"Client đã kết nối thành công! clientHostCode: {clientHostCode}");
    }

    // Được gọi khi client bị ngắt kết nối
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.LogError($"Client bị ngắt kết nối! clientHostCode: {clientHostCode}");
    }

    // Generate a 6-digit room code
    private string GenerateHostCode()
    {
        string code = "";
        for (int i = 0; i < 6; i++)
        {
            code += Random.Range(0, 10).ToString();
        }
        return code;
    }
}