
using UnityEngine;

public class ObstacleMovement : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!GameManager.IsPlaying) return;

        float speed = GameManager.Instance.CurrentSpeed;

        transform.position += Vector3.left *
                              speed * Time.deltaTime;

        if (mainCamera != null)
        {
            float leftEdge = mainCamera.ViewportToWorldPoint(
                new Vector3(0f, 0.5f, 0f)
            ).x;

            if (transform.position.x < leftEdge - 10f)
            {
                Destroy(gameObject);
            }
        }
    }
}
