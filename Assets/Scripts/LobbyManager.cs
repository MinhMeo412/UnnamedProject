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

    // Hàm được gọi từ LobbyUI để yêu cầu tham gia phòng
    public void JoinRoom(string roomName)
    {
        if (!isLocalPlayer) return; // Chỉ người chơi cục bộ mới có thể gửi lệnh
        CmdJoinRoom(roomName);
    }

    // Command: Gửi yêu cầu tham gia phòng từ client lên server
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
        selectedRoom = roomName; // Cập nhật SyncVar để đồng bộ
        NetworkManager.singleton.ServerChangeScene(roomName); // Chuyển scene trên server và tất cả client
    }

    // Command: Gửi mã host từ client lên server để xác thực
    [Command]
    void CmdValidateHostCode(string clientHostCode)
    {
        if (clientHostCode == serverHostCode)
        {
            Debug.Log($"Client với mã host {clientHostCode} đã xác nhận hợp lệ.");
            RpcNotifyHostCodeValid(true); // Thông báo client mã host hợp lệ
        }
        else
        {
            Debug.LogError($"Mã host không hợp lệ: {clientHostCode}. Mã host thực tế: {serverHostCode}");
            RpcNotifyHostCodeValid(false); // Thông báo client mã host không hợp lệ
            connectionToClient.Disconnect(); // Ngắt kết nối client nếu mã host sai
        }
    }

    // TargetRpc: Gửi thông báo về tính hợp lệ của mã host đến client cụ thể
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
                    lobbyUI.DisplayMessage("Mã phòng không hợp lệ! Đang ngắt kết nối.");
                }
            }
            else
            {
                Debug.LogError("Không tìm thấy LobbyUI trong scene LobbyScene!");
            }
        }
    }

    // Hook: Được gọi khi SyncVar selectedRoom thay đổi
    void OnRoomChanged(string oldRoom, string newRoom)
    {
        if (!string.IsNullOrEmpty(newRoom))
        {
            Debug.Log($"Đã chuyển sang phòng: {newRoom}");
        }
    }

    // Hook: Được gọi khi SyncVar hostCode thay đổi
    void OnHostCodeChanged(string oldCode, string newCode)
    {
        Debug.Log($"Mã phòng trên client: {newCode}");
        UpdateHostCodeUI(newCode); // Cập nhật UI hiển thị mã phòng
    }

    // Được gọi trên server khi đối tượng này được spawn
    public override void OnStartServer()
    {
        base.OnStartServer();
        selectedRoom = LOBBY_SCENE; // Đặt phòng ban đầu là LobbyScene trên server
        if (!string.IsNullOrEmpty(serverHostCode)) // Use existing serverHostCode if set
        {
            hostCode = serverHostCode; // Gán vào SyncVar để đồng bộ cho tất cả client
            Debug.Log($"Sử dụng mã phòng hiện có: {hostCode}");
        }
    }

    // Được gọi trên client khi đối tượng này được spawn
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"LobbyManager khởi tạo trên client, netId: {netId}, mã phòng: {hostCode}");
        if (isLocalPlayer)
        {
            Debug.Log("Đã vào lobby!");
            if (SceneManager.GetActiveScene().name == LOBBY_SCENE)
            {
                UpdateHostCodeUI(hostCode); // Cập nhật UI mã phòng khi vào lobby
            }

            if (isServer)
            {
                Debug.Log("Đây là host, bỏ qua kiểm tra clientHostCode.");
                RpcNotifyHostCodeValid(true); // Host tự động hợp lệ
                return;
            }

            CustomNetworkManager networkManager = NetworkManager.singleton as CustomNetworkManager;
            if (networkManager == null)
            {
                Debug.LogError("Không tìm thấy CustomNetworkManager!");
                RpcNotifyHostCodeValid(false);
                NetworkManager.singleton.StopClient();
                return;
            }

            Debug.Log($"clientHostCode từ CustomNetworkManager: {networkManager.clientHostCode}");
            if (!string.IsNullOrEmpty(networkManager.clientHostCode))
            {
                CmdValidateHostCode(networkManager.clientHostCode); // Gửi mã phòng để xác thực
            }
            else
            {
                Debug.LogError("clientHostCode rỗng! Đảm bảo mã phòng được nhập trong LoginUI.");
                RpcNotifyHostCodeValid(false);
                NetworkManager.singleton.StopClient();
            }
        }
    }

    // Update serverHostCode (called from CustomNetworkManager)
    public void UpdateServerHostCode(string newCode)
    {
        if (isServer)
        {
            serverHostCode = newCode;
            hostCode = serverHostCode; // Sync to clients via SyncVar
            Debug.Log($"Updated serverHostCode to: {serverHostCode}");
        }
    }

    // Cập nhật UI hiển thị mã phòng
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