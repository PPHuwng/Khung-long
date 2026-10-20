using UnityEngine;

public class SeamlessScroller : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 2f;    // Tốc độ trôi của Background (chậm hơn đường)
    [SerializeField] private Transform otherBackground; // Kéo tấm Background CÒN LẠI vào đây
    [SerializeField] private float bgWidth = 19.2f;     // Chiều rộng của tấm Background

    void Update()
    {
        // Cho Background trôi chậm sang bên trái
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        // Khi Background 1 chạy khuất màn hình bên trái, tự động nối ngay sau đít Background 2
        if (transform.position.x <= -bgWidth)
        {
            transform.position = new Vector3(otherBackground.position.x + bgWidth, transform.position.y, transform.position.z);
        }
    }
}