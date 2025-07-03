using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    private const string ROOM1_SCENE = "Room1Scene";
    private const string ROOM2_SCENE = "Room2Scene";
    private const string LOBBY_SCENE = "LobbyScene";

    [SyncVar(hook = nameof(OnRoomChanged))]
    public string selectedRoom = "";

    [SyncVar(hook = nameof(OnHostCodeChanged))]
    public string hostCode = "";

    private static string serverHostCode = ""; // Biến tĩnh để lưu mã host của server

    public void JoinRoom(string roomName)
    {
        if (!isLocalPlayer) return;
        CmdJoinRoom(roomName);
    }

    [Command]
    void CmdJoinRoom(string roomName)
    {
        if (!isServer) return; // Chỉ host được chọn phòng
        Debug.Log($"Server nhận lệnh chuyển sang phòng: {roomName}");
        if (roomName != ROOM1_SCENE && roomName != ROOM2_SCENE)
        {
            Debug.LogError($"Phòng không hợp lệ: {roomName}");
            return;
        }
        selectedRoom = roomName;
        NetworkManager.singleton.ServerChangeScene(roomName);
    }

    [Command]
    void CmdValidateHostCode(string clientHostCode)
    {
        if (clientHostCode == serverHostCode)
        {
            Debug.Log($"Client với mã host {clientHostCode} đã xác nhận hợp lệ.");
            RpcNotifyHostCodeValid(true);
        }
        else
        {
            Debug.LogError($"Mã host không hợp lệ: {clientHostCode}. Mã host thực tế: {serverHostCode}");
            RpcNotifyHostCodeValid(false);
            connectionToClient.Disconnect();
        }
    }

    [TargetRpc]
    void RpcNotifyHostCodeValid(bool isValid)
    {
        if (isLocalPlayer && SceneManager.GetActiveScene().name == LOBBY_SCENE)
        {
            LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>();
            if (lobbyUI != null)
            {
                if (isValid)
                {
                    lobbyUI.DisplayMessage("Kết nối thành công!");
                }
                else
                {
                    lobbyUI.DisplayMessage("Mã host không hợp lệ!");
                }
            }
            else
            {
                Debug.LogError("Không tìm thấy LobbyUI trong scene LobbyScene!");
            }
        }
    }

    void OnRoomChanged(string oldRoom, string newRoom)
    {
        if (!string.IsNullOrEmpty(newRoom))
        {
            Debug.Log($"Đã chuyển sang phòng: {newRoom}");
        }
    }

    void OnHostCodeChanged(string oldCode, string newCode)
    {
        Debug.Log($"Mã host trên client: {newCode}");
        UpdateHostCodeUI(newCode);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        selectedRoom = LOBBY_SCENE;
        if (string.IsNullOrEmpty(serverHostCode)) // Chỉ tạo mã host nếu chưa có
        {
            serverHostCode = GenerateHostCode();
            hostCode = serverHostCode; // Gán vào SyncVar để đồng bộ
            Debug.Log($"Mã host được tạo trên server: {hostCode}");
        }
        else
        {
            hostCode = serverHostCode; // Sử dụng mã host hiện có
            Debug.Log($"Sử dụng mã host hiện có: {hostCode}");
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"LobbyManager khởi tạo trên client, netId: {netId}, mã host: {hostCode}");
        if (isLocalPlayer)
        {
            Debug.Log("Đã vào lobby!");
            if (SceneManager.GetActiveScene().name == LOBBY_SCENE)
            {
                UpdateHostCodeUI(hostCode);
            }

            // Nếu là host (server + client), không cần kiểm tra clientHostCode
            if (isServer)
            {
                Debug.Log("Đây là host, bỏ qua kiểm tra clientHostCode.");
                RpcNotifyHostCodeValid(true); // Host tự động hợp lệ
                return;
            }

            // Nếu là client thuần, kiểm tra clientHostCode
            CustomNetworkManager networkManager = NetworkManager.singleton as CustomNetworkManager;
            if (networkManager == null)
            {
                Debug.LogError("Không tìm thấy CustomNetworkManager! Kiểm tra GameObject NetworkManager trong scene.");
                RpcNotifyHostCodeValid(false);
                NetworkManager.singleton.StopClient();
                return;
            }

            Debug.Log($"clientHostCode từ CustomNetworkManager: {networkManager.clientHostCode}");
            if (!string.IsNullOrEmpty(networkManager.clientHostCode))
            {
                CmdValidateHostCode(networkManager.clientHostCode);
            }
            else
            {
                Debug.LogError("clientHostCode rỗng! Đảm bảo mã host được nhập trong LoginUI.");
                RpcNotifyHostCodeValid(false);
                NetworkManager.singleton.StopClient();
            }
        }
    }

    private string GenerateHostCode()
    {
        // Tạo chuỗi 6 chữ số ngẫu nhiên từ 0 đến 9
        string code = "";
        for (int i = 0; i < 6; i++)
        {
            code += Random.Range(0, 10).ToString();
        }
        return code;
    }

    private void UpdateHostCodeUI(string code)
    {
        if (SceneManager.GetActiveScene().name == LOBBY_SCENE)
        {
            LobbyUI lobbyUI = FindFirstObjectByType<LobbyUI>();
            if (lobbyUI != null)
            {
                lobbyUI.UpdateHostCodeText(code);
            }
            else
            {
                Debug.LogError("Không tìm thấy LobbyUI trong scene LobbyScene!");
            }
        }
    }
}