using UnityEngine;
using UnityEngine.UI;
using Mirror;
using kcp2k;
using TMPro;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField hostCodeInput; // Input field for room code
    public TMP_Text errorText; // Text to display errors
    private CustomNetworkManager networkManager;
    private KcpTransport kcpTransport;
    private GoogleSheetsManager sheetsManager;
    private Canvas canvas;

    void Awake()
    {
        // Get the Canvas component
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("LoginUI must be attached to a GameObject under a Canvas!");
        }
    }

    void Start()
    {
        // Find CustomNetworkManager
        networkManager = FindFirstObjectByType<CustomNetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("Không tìm thấy CustomNetworkManager trong scene!");
            DisplayError("Không tìm thấy CustomNetworkManager!");
            return;
        }

        // Find KcpTransport
        kcpTransport = networkManager.GetComponent<KcpTransport>();
        if (kcpTransport == null)
        {
            Debug.LogError("Không tìm thấy KcpTransport trên CustomNetworkManager!");
            DisplayError("Không tìm thấy KcpTransport!");
            return;
        }

        // Find GoogleSheetsManager
        sheetsManager = FindFirstObjectByType<GoogleSheetsManager>();
        if (sheetsManager == null)
        {
            Debug.LogError("Không tìm thấy GoogleSheetsManager trong scene!");
            DisplayError("Không tìm thấy GoogleSheetsManager!");
            return;
        }

        // Check UI components
        if (hostButton == null || joinButton == null || hostCodeInput == null || errorText == null)
        {
            Debug.LogError("hostButton, joinButton, hostCodeInput hoặc errorText chưa được gán trong Inspector!");
            DisplayError("Cấu hình UI không hoàn chỉnh!");
            return;
        }

        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);

        // Register scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Set initial Canvas state
        UpdateCanvasState();
    }

    void OnDestroy()
    {
        // Unregister scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCanvasState();
    }

    // Enable or disable Canvas based on the current scene
    private void UpdateCanvasState()
    {
        if (canvas != null)
        {
            bool isLoginScene = SceneManager.GetActiveScene().name == "LoginScene";
            canvas.enabled = isLoginScene;
            Debug.Log($"LoginUI Canvas {(isLoginScene ? "enabled" : "disabled")} in scene: {SceneManager.GetActiveScene().name}");
        }
    }

    // Hàm được gọi khi nhấn nút Host
    void StartHost()
    {
        if (networkManager == null || kcpTransport == null)
        {
            Debug.LogError("Không thể khởi động Host: networkManager hoặc kcpTransport là null!");
            DisplayError("Không thể khởi động Host!");
            return;
        }

        networkManager.networkAddress = "localhost";
        networkManager.clientHostCode = ""; // Reset clientHostCode khi làm host
        networkManager.StartHost();
        Debug.Log($"Đã khởi động Host trên địa chỉ: {networkManager.networkAddress}, cổng: {kcpTransport.Port}");
    }

    // Hàm được gọi khi nhấn nút Join
    async void StartClient()
    {
        if (networkManager == null || kcpTransport == null || sheetsManager == null)
        {
            Debug.LogError("Không thể khởi động Client: networkManager, kcpTransport hoặc sheetsManager là null!");
            DisplayError("Không thể khởi động Client!");
            return;
        }

        // Check room code
        if (hostCodeInput == null || string.IsNullOrEmpty(hostCodeInput.text))
        {
            DisplayError("Vui lòng nhập mã phòng!");
            return;
        }

        // Retrieve IP address from Google Sheet
        string roomCode = hostCodeInput.text;
        string ipAddress = await sheetsManager.GetIpAddressByRoomCode(roomCode);
        if (string.IsNullOrEmpty(ipAddress))
        {
            DisplayError("Mã phòng không hợp lệ hoặc không tìm thấy địa chỉ IP!");
            return;
        }

        // Set network address and host code
        networkManager.networkAddress = ipAddress;
        networkManager.clientHostCode = roomCode;

        kcpTransport.Port = 7777; // Ensure port is set
        networkManager.StartClient();
        Debug.Log($"Đang khởi động client tới {networkManager.networkAddress}:{kcpTransport.Port} với mã phòng: {roomCode}");
    }

    // Hàm hiển thị thông báo lỗi trên UI
    private void DisplayError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            Debug.LogError(message);
        }
    }
}