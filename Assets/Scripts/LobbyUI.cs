using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class LobbyUI : MonoBehaviour
{
    public Button room1Button;
    public Button room2Button;
    public TMP_Text hostCodeText; // Text UI để hiển thị mã host
    public TMP_Text messageText; // Text UI để hiển thị thông báo

    private LobbyManager lobbyManager;

    void Start()
    {
        if (room1Button == null || room2Button == null || hostCodeText == null || messageText == null)
        {
            Debug.LogError("room1Button, room2Button, hostCodeText hoặc messageText chưa được gán trong Inspector!");
            return;
        }
        StartCoroutine(WaitForLobbyManager());
    }

    System.Collections.IEnumerator WaitForLobbyManager()
    {
        while (lobbyManager == null)
        {
            lobbyManager = FindFirstObjectByType<LobbyManager>();
            if (lobbyManager == null)
            {
                Debug.LogWarning("LobbyManager chưa được tìm thấy, đang thử lại...");
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Chỉ host mới được tương tác với các nút chọn phòng
        room1Button.interactable = lobbyManager.isServer;
        room2Button.interactable = lobbyManager.isServer;

        room1Button.onClick.AddListener(() => lobbyManager.JoinRoom("Room1Scene"));
        room2Button.onClick.AddListener(() => lobbyManager.JoinRoom("Room2Scene"));
        Debug.Log("Đã gán sự kiện cho các nút Room 1 và Room 2!");
        UpdateHostCodeText(lobbyManager.hostCode); // Hiển thị mã host ban đầu
    }

    public void UpdateHostCodeText(string code)
    {
        if (hostCodeText != null)
        {
            hostCodeText.text = $"Mã Host: {code}";
        }
        else
        {
            Debug.LogError("hostCodeText chưa được gán trong Inspector!");
        }
    }

    public void DisplayMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        else
        {
            Debug.LogError("messageText chưa được gán trong Inspector!");
        }
    }
}