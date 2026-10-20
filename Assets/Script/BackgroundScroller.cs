using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 2f; // Tốc độ trôi của background (thường chậm hơn đường để có chiều sâu)
    [SerializeField] private float bgLength = 20.48f; // Chiều dài thực tế của bức ảnh background (bạn có thể tinh chỉnh số này trong Inspector)

    void Update()
    {
        transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);

        if (transform.position.x <= -bgLength)
        {
            transform.position += new Vector3(bgLength * 2f, 0f, 0f);
        }
    }
}