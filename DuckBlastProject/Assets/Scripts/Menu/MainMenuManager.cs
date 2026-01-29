using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject soundButton;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    private string gameSceneName = "Game";
    [SerializeField] private TransitionManager transitionManager;
    [SerializeField] private CreditsController creditsController;

    private bool isSoundMuted = false;

    private void Start()
    {
        UpdateSoundButtonSprite();

        if (creditsController != null)
        {
            creditsController.gameObject.SetActive(false);
        }
    }

    public void StartGame()
    {
        if (transitionManager != null)
        {
            transitionManager.StartTransition(gameSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void ToggleLeaderboard()
    {
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.ShowLeaderboard();
        }
    }

    public void ToggleHome()
    {
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.HideLeaderboard();
        }
    }

    public void ToggleSound()
    {
        isSoundMuted = !isSoundMuted;
        AudioListener.volume = isSoundMuted ? 0f : 1f;
        UpdateSoundButtonSprite();
    }

    public void ToggleCredits()
    {
        if (creditsController != null)
        {
            creditsController.StartCredits();
        }
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
}