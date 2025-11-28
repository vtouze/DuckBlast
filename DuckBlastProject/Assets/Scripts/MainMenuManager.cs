using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private GameObject leaderboardPanel;
    [SerializeField] private GameObject soundButton;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    private string gameSceneName = "Game";

    private bool isSoundMuted = false;

    private void Start()
    {
        UpdateSoundButtonSprite();

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ToggleLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            bool isActive = leaderboardPanel.activeSelf;
            leaderboardPanel.SetActive(!isActive);
        }
    }
    public void ToggleSound()
    {
        isSoundMuted = !isSoundMuted;
        AudioListener.volume = isSoundMuted ? 0f : 1f;
        UpdateSoundButtonSprite();
    }

    private void UpdateSoundButtonSprite()
    {
        if (soundButton != null)
        {
            SpriteRenderer spriteRenderer = soundButton.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = isSoundMuted ? soundOffSprite : soundOnSprite;
            }
        }
    }

    private void LoadLeaderboardData()
    {
        // Placeholder for the leaderboard logic
    }
}