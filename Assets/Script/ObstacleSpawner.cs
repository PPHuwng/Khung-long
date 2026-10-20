
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Danh sach vat can")]
    public GameObject[] obstaclePrefabs;

    [Header("Thoi gian xuat hien")]
    public float minSpawnTime = 2f;
    public float maxSpawnTime = 4f;
    public float minimumGap = 8f;

    [Header("Vi tri")]
    public float groundY = -2.4f;
    public float spawnPadding = 2f;

    private float timer;
    private float nextSpawnTime;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (!GameManager.IsPlaying) return;

        timer += Time.deltaTime;

        if (timer >= nextSpawnTime)
        {
            SpawnObstacle();
            timer = 0f;
            ScheduleNextSpawn();
        }
    }

    void ScheduleNextSpawn()
    {
        float speed = GameManager.Instance.CurrentSpeed;
        float safeTime = minimumGap / Mathf.Max(speed, 0.1f);

        nextSpawnTime = Mathf.Max(
            Random.Range(minSpawnTime, maxSpawnTime),
            safeTime
        );
    }

    void SpawnObstacle()
    {
        if (obstaclePrefabs == null ||
            obstaclePrefabs.Length == 0 ||
            mainCamera == null)
            return;

        int index = Random.Range(0, obstaclePrefabs.Length);
        GameObject prefab = obstaclePrefabs[index];

        if (prefab == null) return;

        float rightEdge = mainCamera.ViewportToWorldPoint(
            new Vector3(1f, 0.5f, 0f)
        ).x;

        Vector3 spawnPosition = new Vector3(
            rightEdge + spawnPadding,
            0f,
            0f
        );

        GameObject obstacle = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity
        );

        Collider2D col = obstacle.GetComponentInChildren<Collider2D>();

        if (col != null)
        {
            float difference = groundY - col.bounds.min.y;
            obstacle.transform.position +=
                Vector3.up * difference;
        }

        if (obstacle.GetComponent<ObstacleMovement>() == null)
        {
            obstacle.AddComponent<ObstacleMovement>();
        }
    }
}
