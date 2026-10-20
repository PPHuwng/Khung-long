
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsPlaying { get; private set; }

    [Header("Speed")]
    public float startSpeed = 5f;
    public float speedIncrease = 0.025f;
    public float maxSpeed = 12f;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text highScoreText;
    public TMP_Text finalScoreText;
    public GameObject gameOverPanel;

    public float CurrentSpeed { get; private set; }
    public int Score { get; private set; }

    private float scoreTimer;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        IsPlaying = true;
        CurrentSpeed = startSpeed;
    }

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateUI();
    }

    void Update()
    {
        if (!IsPlaying) return;

        scoreTimer += Time.deltaTime;
        Score = Mathf.FloorToInt(scoreTimer * 10f);

        CurrentSpeed = Mathf.Min(
            maxSpeed,
            startSpeed + scoreTimer * speedIncrease);

        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + Score;

        if (highScoreText != null)
            highScoreText.text = "Best: " +
                PlayerPrefs.GetInt("HighScore", 0);
    }

    public void GameOver()
    {
        if (!IsPlaying) return;

        IsPlaying = false;

        int best = PlayerPrefs.GetInt("HighScore", 0);

        if (Score > best)
        {
            PlayerPrefs.SetInt("HighScore", Score);
            PlayerPrefs.Save();
        }

        if (finalScoreText != null)
            finalScoreText.text = "Your Score: " + Score;

        UpdateUI();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsPlaying = false;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }

    public void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsPlaying = false;
        }
    }
}
