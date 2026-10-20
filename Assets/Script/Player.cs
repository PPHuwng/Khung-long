
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float screenPadding = 0.5f;

    [Header("Ground Settings")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float checkRadius = 0.2f;

    [Header("Collision Settings")]
    [SerializeField] private LayerMask obstacleLayer;

    private Rigidbody2D rb;
    private Animator anim;
    private Camera mainCamera;

    private float horizontalInput;
    private bool isGrounded;
    private bool isDead;

    private Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        mainCamera = Camera.main;

        originalScale = transform.localScale;
    }

    void Update()
    {
        if (isDead || !GameManager.IsPlaying)
        {
            horizontalInput = 0f;
            return;
        }

        // Kiểm tra khủng long có chạm đất hay không
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                checkRadius,
                groundLayer
            ) != null;
        }
        else
        {
            isGrounded = false;
        }

        // Điều khiển trái / phải
        horizontalInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                horizontalInput = -1f;
            }

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
            {
                horizontalInput = 1f;
            }

            // Nhảy khi đang đứng trên mặt đất
            if (Keyboard.current.spaceKey.wasPressedThisFrame
                && isGrounded)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    jumpForce
                );

                isGrounded = false;
            }
        }

        // Lật nhân vật theo hướng di chuyển
        if (horizontalInput != 0f)
        {
            float direction = Mathf.Sign(horizontalInput);

            transform.localScale = new Vector3(
                direction * Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }

        // Cập nhật Animator
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("IsGrounded", isGrounded);
    }

    void FixedUpdate()
    {
        if (isDead || !GameManager.IsPlaying)
        {
            return;
        }

        // Di chuyển trái / phải
        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed,
            rb.linearVelocity.y
        );

        // Không cho khủng long ra ngoài màn hình
        if (mainCamera != null && mainCamera.orthographic)
        {
            float halfWidth =
                mainCamera.orthographicSize *
                mainCamera.aspect;

            float minX =
                mainCamera.transform.position.x -
                halfWidth + screenPadding;

            float maxX =
                mainCamera.transform.position.x +
                halfWidth - screenPadding;

            float x = Mathf.Clamp(
                rb.position.x,
                minX,
                maxX
            );

            if (x != rb.position.x)
            {
                rb.position = new Vector2(
                    x,
                    rb.position.y
                );

                rb.linearVelocity = new Vector2(
                    0f,
                    rb.linearVelocity.y
                );
            }
        }
    }

    // Kiểm tra Layer chướng ngại vật

    private bool IsObstacle(GameObject obj)
    {
        return obj.GetComponentInParent<ObstacleMovement>() != null;
    }

    // Va chạm vật lý
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || !GameManager.IsPlaying)
            return;

        if (IsObstacle(collision.gameObject))
        {
            Die();
        }
    }

    // Va chạm với Trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || !GameManager.IsPlaying)
            return;

        if (IsObstacle(other.gameObject))
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        horizontalInput = 0f;

        rb.linearVelocity = Vector2.zero;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            checkRadius
        );
    }
}
