using UnityEngine;
using UnityEngine.UI;
using Mirror;
using kcp2k;
using TMPro;

public class LoginUI : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField networkAddressInput; // Địa chỉ IP của server
    public TMP_InputField hostCodeInput; // Mã host của phòng
    public TMP_Text errorText; // Text UI để hiển thị lỗi
    private CustomNetworkManager networkManager; // Sử dụng CustomNetworkManager
    private KcpTransport kcpTransport;

    void Start()
    {
        // Tìm CustomNetworkManager
        networkManager = FindFirstObjectByType<CustomNetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("Không tìm thấy CustomNetworkManager trong scene! Vui lòng thêm GameObject với component CustomNetworkManager.");
            DisplayError("Không tìm thấy CustomNetworkManager!");
            return;
        }

        // Tìm KcpTransport
        kcpTransport = networkManager.GetComponent<KcpTransport>();
        if (kcpTransport == null)
        {
            Debug.LogError("Không tìm thấy KcpTransport trên CustomNetworkManager! Vui lòng thêm component KcpTransport.");
            DisplayError("Không tìm thấy KcpTransport!");
            return;
        }

        // Kiểm tra các nút và input UI
        if (hostButton == null || joinButton == null || networkAddressInput == null || hostCodeInput == null || errorText == null)
        {
            Debug.LogError("hostButton, joinButton, networkAddressInput, hostCodeInput hoặc errorText chưa được gán trong Inspector!");
            DisplayError("Cấu hình UI không hoàn chỉnh!");
            return;
        }

        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
    }

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

    void StartClient()
    {
        if (networkManager == null || kcpTransport == null)
        {
            Debug.LogError("Không thể khởi động Client: networkManager hoặc kcpTransport là null!");
            DisplayError("Không thể khởi động Client!");
            return;
        }

        if (networkAddressInput == null || string.IsNullOrEmpty(networkAddressInput.text))
        {
            networkManager.networkAddress = "localhost";
        }
        else
        {
            networkManager.networkAddress = networkAddressInput.text;
        }

        if (hostCodeInput == null || string.IsNullOrEmpty(hostCodeInput.text))
        {
            DisplayError("Vui lòng nhập mã host!");
            return;
        }

        // Gán mã host cho CustomNetworkManager
        networkManager.clientHostCode = hostCodeInput.text;

        kcpTransport.Port = 7777;
        networkManager.StartClient();
        Debug.Log($"Đang khởi động client tới {networkManager.networkAddress}:{kcpTransport.Port} với mã host: {hostCodeInput.text}");
    }

    private void DisplayError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            Debug.LogError(message);
        }
    }
}