using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Leaderboards.Models;
using System.Collections;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [Header("Leaderboard Settings")]
    [SerializeField] private string leaderboardId = "HighScore";
    [SerializeField] private int topScoresLimit = 10;

    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Animator leaderboardPanelAnimator;
    [SerializeField] private Transform leaderboardEntryContainer;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Color playerEntryColor = Color.green;

    private string playerId;
    private string playerName = "Player";
    private int playerCounter = 1;
    private static int lastHighScore = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUnityServices();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await SignInAnonymously();
            Debug.Log("Unity Services initialized successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    private async Task SignInAnonymously()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            playerId = AuthenticationService.Instance.PlayerId;

            playerCounter = PlayerPrefs.GetInt("PlayerCounter", 1);
            playerName = $"Player_{playerCounter:D2}";
            playerCounter++;
            PlayerPrefs.SetInt("PlayerCounter", playerCounter);
            PlayerPrefs.Save();

            Debug.Log($"Player signed in with ID: {playerId}, Name: {playerName}");
        }
        else
        {
            playerId = AuthenticationService.Instance.PlayerId;
            playerCounter = PlayerPrefs.GetInt("PlayerCounter", 1);
            playerName = $"Player_{playerCounter:D2}";
        }
    }

    public static void SubmitScore(int score)
    {
        if (Instance == null)
        {
            Debug.LogWarning("LeaderboardManager instance not found. Score will be stored for later submission.");
            lastHighScore = Mathf.Max(lastHighScore, score);
            return;
        }

        // Handle the async operation internally
        Instance.StartCoroutine(Instance.InternalSubmitScoreCoroutine(score));
    }

    private IEnumerator InternalSubmitScoreCoroutine(int score)
    {
        Task submissionTask = LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);

        while (!submissionTask.IsCompleted)
        {
            yield return null;
        }

        if (submissionTask.IsFaulted)
        {
            Debug.LogError($"Failed to submit score: {submissionTask.Exception}");
            lastHighScore = Mathf.Max(lastHighScore, score);
        }
        else
        {
            Debug.Log("Score submitted successfully!");
            lastHighScore = 0;
        }
    }

    public async void ShowLeaderboard()
    {
        if (leaderboardPanel == null)
        {
            Debug.LogError("Leaderboard panel reference is not set!");
            return;
        }

        if (leaderboardPanelAnimator != null)
        {
            leaderboardPanelAnimator.SetTrigger("SlideIn");
        }

        leaderboardPanel.SetActive(true);

        foreach (Transform child in leaderboardEntryContainer)
        {
            Destroy(child.gameObject);
        }

        try
        {
            var topScoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId,
                new GetScoresOptions { Limit = topScoresLimit });

            var playerScoreResponse = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);

            bool playerInTop = false;
            double playerScore = 0;
            int playerRank = -1;

            foreach (var entry in topScoresResponse.Results)
            {
                if (entry.PlayerId == playerId)
                {
                    playerInTop = true;
                    break;
                }
            }

            for (int i = 0; i < topScoresResponse.Results.Count; i++)
            {
                var entry = topScoresResponse.Results[i];
                CreateLeaderboardEntry(i + 1, entry.PlayerName, entry.Score, entry.PlayerId == playerId);
            }

            while (leaderboardEntryContainer.childCount < 10)
            {
                CreateLeaderboardEntry(leaderboardEntryContainer.childCount + 1, "-", 0, false);
            }

            if (!playerInTop && playerScoreResponse != null)
            {
                var allScoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId);

                int rank = 1;
                foreach (var entry in allScoresResponse.Results)
                {
                    if (entry.PlayerId == playerId)
                    {
                        playerRank = rank;
                        playerScore = entry.Score;
                        break;
                    }
                    rank++;
                }

                if (playerRank > 0 && leaderboardEntryContainer.childCount >= 10)
                {
                    Transform tenthEntry = leaderboardEntryContainer.GetChild(9);
                    LeaderboardEntryUI entryUI = tenthEntry.GetComponent<LeaderboardEntryUI>();
                    if (entryUI != null)
                    {
                        entryUI.SetEntry(playerRank, $"{playerName} (You)", (long)playerScore, playerEntryColor);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get leaderboard data: {e.Message}");
            for (int i = 0; i < 10; i++)
            {
                if (i == 0)
                    CreateLeaderboardEntry(i + 1, "Error", 0, false);
                else if (i == 1)
                    CreateLeaderboardEntry(i + 1, "Loading failed", 0, false);
                else
                    CreateLeaderboardEntry(i + 1, "-", 0, false);
            }
        }

        StartCoroutine("CheckForPendingScore");
    }

    private void CreateLeaderboardEntry(int rank, string playerName, double score, bool isPlayer)
    {
        GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardEntryContainer);
        LeaderboardEntryUI entryUI = entryObj.GetComponent<LeaderboardEntryUI>();

        if (entryUI != null)
        {
            entryUI.SetEntry(rank, playerName, (long)score, isPlayer ? playerEntryColor : Color.white);
        }
    }

    public void HideLeaderboard()
    {
        if (leaderboardPanelAnimator != null)
        {
            leaderboardPanelAnimator.SetTrigger("SlideOut");
        }

        StartCoroutine(DisableAfterAnimation());
    }

    private IEnumerator DisableAfterAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }
}