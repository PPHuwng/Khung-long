
using UnityEngine;

public class RoadScroller : MonoBehaviour
{
    [SerializeField] private float roadLength = 19.2f;

    void Update()
    {
        if (!GameManager.IsPlaying) return;

        float speed = GameManager.Instance.CurrentSpeed;

        transform.Translate(
            Vector3.left * speed * Time.deltaTime,
            Space.World);

        if (transform.position.x <= -roadLength)
        {
            transform.position +=
                new Vector3(roadLength * 2f, 0f, 0f);
        }
    }
}
