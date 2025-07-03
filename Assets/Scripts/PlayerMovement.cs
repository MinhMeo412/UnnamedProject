using UnityEngine;
using Mirror;

// Script này kế thừa từ NetworkBehaviour để hỗ trợ các tính năng mạng của Mirror
public class PlayerMovement : NetworkBehaviour
{
    // Biến tham chiếu đến CharacterController để điều khiển chuyển động
    private CharacterController controller;
    // Biến tham chiếu đến MeshRenderer để thay đổi màu sắc của người chơi
    private MeshRenderer meshRenderer;

    // Các biến cấu hình có thể chỉnh sửa trong Inspector
    [SerializeField] private float moveSpeed = 5f; // Tốc độ di chuyển bình thường
    [SerializeField] private float sprintSpeed = 8f; // Tốc độ khi chạy nhanh (nhấn Shift)
    [SerializeField] private float jumpForce = 5f; // Lực nhảy khi nhấn phím Space
    [SerializeField] private float gravity = -9.81f; // Gia tốc trọng lực để mô phỏng rơi
    [SerializeField] private float groundCheckDistance = 0.4f; // Khoảng cách kiểm tra xem người chơi có chạm đất không
    [SerializeField] private LayerMask groundLayer; // Layer để xác định mặt đất

    // Biến SyncVar để đồng bộ màu sắc từ server sang tất cả client
    // Hook OnColorChanged được gọi khi giá trị playerColor thay đổi
    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    // Hàm Start để ghi log màu của người chơi khi đối tượng được khởi tạo
    void Start()
    {
        // In màu hiện tại của người chơi vào Console để debug
        Debug.Log("Player color: " + playerColor);
    }

    // Biến lưu hướng di chuyển dựa trên input
    private Vector3 moveDirection;
    // Biến lưu vận tốc theo trục y (dùng cho nhảy và rơi)
    private float yVelocity;
    // Biến kiểm tra xem người chơi có đang chạm đất không
    private bool isGrounded;

    // Hàm Awake được gọi khi đối tượng được khởi tạo
    void Awake()
    {
        // Lấy component CharacterController từ Player Prefab
        controller = GetComponent<CharacterController>();
        // Lấy component MeshRenderer từ Player Prefab
        meshRenderer = GetComponent<MeshRenderer>();

        // Kiểm tra null để tránh lỗi nếu thiếu component
        if (controller == null)
        {
            Debug.LogError("CharacterController is missing on Player Prefab!", gameObject);
            // Vô hiệu hóa script để tránh lỗi runtime
            enabled = false;
        }
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer is missing on Player Prefab!", gameObject);
            // Vô hiệu hóa script để tránh lỗi runtime
            enabled = false;
        }
    }

    // Hàm Update chạy mỗi frame để xử lý input và chuyển động
    void Update()
    {
        // Chỉ xử lý input cho người chơi cục bộ (local player) để tránh điều khiển các nhân vật khác
        if (!isLocalPlayer) return;

        // Kiểm tra xem người chơi có chạm đất không bằng cách sử dụng Physics.CheckSphere
        isGrounded = Physics.CheckSphere(transform.position, groundCheckDistance, groundLayer);

        // Lấy input từ người chơi (trái/phải và tiến/lùi)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        // Tạo vector hướng di chuyển, chuẩn hóa để đảm bảo tốc độ đồng đều
        moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // Nếu người chơi chạm đất, đặt lại vận tốc y để tránh tích lũy trọng lực
        if (isGrounded && yVelocity < 0)
        {
            yVelocity = -2f;
        }

        // Nếu nhấn phím Space và đang chạm đất, gửi lệnh nhảy lên server
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            CmdJump();
        }

        // Xác định tốc độ hiện tại (chạy nhanh nếu nhấn Left Shift, bình thường nếu không)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        // Tạo vector di chuyển, bao gồm vận tốc y (cho nhảy/rơi)
        Vector3 move = moveDirection * currentSpeed;
        move.y = yVelocity;

        // Nếu nhấn phím Space, gửi lệnh bắn lên server
        // Lưu ý: Phím Space hiện được dùng cho cả nhảy và bắn, nên cần sửa để tránh xung đột
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CmdFire();
        }

        // Gửi lệnh di chuyển lên server
        CmdMove(move);

        // Áp dụng trọng lực để người chơi rơi xuống
        yVelocity += gravity * Time.deltaTime;
    }

    // Hàm Command để gửi lệnh bắn từ client lên server
    [Command]
    void CmdFire()
    {
        // Ghi log trên server để debug hành động bắn
        Debug.Log("Server nhận được lệnh bắn!");

        // Gọi ClientRpc để thông báo tất cả client hiển thị hiệu ứng bắn
        RpcFireEffect();
    }

    // Hàm Command để gửi lệnh di chuyển từ client lên server
    [Command]
    void CmdMove(Vector3 move)
    {
        // Kiểm tra null để đảm bảo CharacterController tồn tại
        if (controller != null)
        {
            // Di chuyển người chơi trên server
            controller.Move(move * Time.deltaTime);
            // Đồng bộ chuyển động sang tất cả client
            RpcSyncMove(move);
        }
    }

    // Hàm Command để gửi lệnh nhảy từ client lên server
    [Command]
    void CmdJump()
    {
        // Chỉ cho phép nhảy nếu người chơi đang chạm đất
        if (isGrounded)
        {
            // Áp dụng lực nhảy trên server
            yVelocity = jumpForce;
            // Đồng bộ lực nhảy sang tất cả client
            RpcSyncJump(jumpForce);
        }
    }

    // Hàm ClientRpc để đồng bộ lực nhảy từ server sang các client
    [ClientRpc]
    void RpcSyncJump(float force)
    {
        // Chỉ áp dụng trên client không phải server để tránh lặp lại
        if (!isServer)
        {
            yVelocity = force;
        }
    }

    // Hàm ClientRpc để đồng bộ chuyển động từ server sang các client
    [ClientRpc]
    void RpcSyncMove(Vector3 move)
    {
        // Chỉ áp dụng trên client không phải server và nếu có CharacterController
        if (!isServer && controller != null)
        {
            controller.Move(move * Time.deltaTime);
        }
    }

    // Hàm ClientRpc để hiển thị hiệu ứng bắn trên tất cả client
    [ClientRpc]
    void RpcFireEffect()
    {
        // Ghi log trên client để debug, bao gồm netId của người chơi bắn
        Debug.Log($"Client nhận được thông báo về việc bắn từ đối tượng có netId: {netId}");
        // Có thể thêm hiệu ứng particle hoặc âm thanh tại đây
    }

    // Hàm hook được gọi trên client khi giá trị SyncVar playerColor thay đổi
    void OnColorChanged(Color oldColor, Color newColor)
    {
        // Kiểm tra null để đảm bảo MeshRenderer tồn tại
        if (meshRenderer != null)
        {
            // Tạo material mới để tránh thay đổi material chung
            Material newMaterial = new Material(meshRenderer.material);
            newMaterial.color = newColor;
            meshRenderer.material = newMaterial;
            // Đặt lại màu của material để hiển thị trên client
            meshRenderer.material.color = newColor;
        }
    }

    // Hàm được gọi trên server khi đối tượng người chơi được tạo
    public override void OnStartServer()
    {
        base.OnStartServer();
        // Gán màu ngẫu nhiên cho người chơi trên server
        playerColor = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
    }
}