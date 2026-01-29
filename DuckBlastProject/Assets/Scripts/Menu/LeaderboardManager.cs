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
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    [Header("Leaderboard Settings")]
    [SerializeField] private string leaderboardId = "HighScore";
    [SerializeField] private int topScoresLimit = 10;

    [Header("UI References")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Animator leaderboardPanelAnimator;
    [SerializeField] private Animator itemsTransitionAnimator;
    [SerializeField] private Transform[] leaderboardEntryContainers;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Color playerEntryColor = Color.green;

    private readonly List<(string name, long score)> defaultScores = new List<(string, long)>
    {
        ("Player_0001", 5000),
        ("Player_0002", 4500),
        ("Player_0003", 4000),
        ("Player_0004", 3500),
        ("Player_0005", 3000),
        ("Player_0006", 2500),
        ("Player_0007", 2000),
        ("Player_0008", 1500),
        ("Player_0009", 1000),
        ("Player_0010", 500),
        ("Player_0011", 400),
        ("Player_0012", 300),
    };

    private string playerId;
    private string playerName = "Player_0000";
    private static List<int> pendingScores = new List<int>();
    private bool isInitialized = false;
    private bool isSigningIn = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("LeaderboardManager instance created.");
            InitializeUnityServices();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void InitializeUnityServices()
    {
        if (isInitialized || isSigningIn) return;

        isSigningIn = true;
        try
        {
            var options = new InitializationOptions();
            await UnityServices.InitializeAsync();

            await Task.Delay(500);

            await SignInAnonymously();
            isInitialized = true;
            isSigningIn = false;

            if (pendingScores.Count > 0)
            {
                foreach (var score in pendingScores)
                {
                    await SubmitPendingScore(score);
                }
                pendingScores.Clear();
            }

            Debug.Log("Unity Services initialized successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
            isSigningIn = false;
        }
    }

    private async Task SignInAnonymously()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                playerId = AuthenticationService.Instance.PlayerId;
                Debug.Log($"Player signed in with ID: {playerId}");

                int randomNumber = Random.Range(0, 10000);
                playerName = $"Player_{randomNumber:D4}";
                Debug.Log($"Player name set to: {playerName}");
            }
            else
            {
                playerId = AuthenticationService.Instance.PlayerId;
                int randomNumber = Random.Range(0, 10000);
                playerName = $"Player_{randomNumber:D4}";
                Debug.Log($"Already signed in with ID: {playerId}, Name: {playerName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to sign in: {e.Message}");
            int randomNumber = Random.Range(0, 10000);
            playerName = $"Player_{randomNumber:D4}";
        }
    }

    public static void SubmitScore(int score)
    {
        Debug.Log($"Attempting to submit score: {score}");

        if (Instance == null)
        {
            Debug.LogWarning("LeaderboardManager instance not found. Score will be stored for later submission.");
            pendingScores.Add(score);
            return;
        }

        Instance.StartCoroutine(Instance.InternalSubmitScore(score));
    }

    private IEnumerator InternalSubmitScore(int score)
    {
        Debug.Log($"Starting submission of score: {score}");

        while (!isInitialized || isSigningIn)
        {
            yield return null;
        }

        int retryCount = 0;
        while (!AuthenticationService.Instance.IsSignedIn && retryCount < 10)
        {
            retryCount++;
            Debug.Log($"Waiting for player to sign in... Attempt {retryCount}/10");
            yield return new WaitForSeconds(0.5f);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("Player is not signed in. Cannot submit score.");
            pendingScores.Add(score);
            yield break;
        }

        Task submissionTask = LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);

        while (!submissionTask.IsCompleted)
        {
            yield return null;
        }

        if (submissionTask.IsFaulted)
        {
            Debug.LogError($"Failed to submit score: {submissionTask.Exception}");
            pendingScores.Add(score);
        }
        else
        {
            Debug.Log($"Score {score} submitted successfully!");
        }
    }

    private async Task SubmitPendingScore(int score)
    {
        try
        {
            Debug.Log($"Submitting pending score: {score}");
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
            Debug.Log($"Pending score {score} submitted successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to submit pending score: {e.Message}");
        }
    }

    public async void ShowLeaderboard()
    {
        if (leaderboardPanel == null)
        {
            Debug.LogError("Leaderboard panel reference is not set!");
            return;
        }

        if (!isInitialized || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("Player is not authenticated. Cannot show leaderboard.");
            DisplayDefaultScores();
            return;
        }

        if (leaderboardPanelAnimator != null)
        {
            leaderboardPanelAnimator.SetTrigger("SlideIn");
        }
        if (itemsTransitionAnimator != null)
        {
            itemsTransitionAnimator.SetTrigger("PlayAnimation");
        }

        leaderboardPanel.SetActive(true);

        foreach (Transform container in leaderboardEntryContainers)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        try
        {
            var allScoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId);
            var allScores = allScoresResponse.Results.OrderByDescending(e => e.Score).ToList();

            var playerEntry = allScores.FirstOrDefault(e => e.PlayerId == playerId);
            int playerRank = playerEntry != null ?
                allScores.IndexOf(playerEntry) + 1 :
                allScores.Count + 1;
            long playerScore = playerEntry != null ? (long)playerEntry.Score : 0;
            bool playerInTop10 = playerRank <= 10;

            Debug.Log($"Player score: {playerScore}, rank: {playerRank}, inTop10: {playerInTop10}");

            var topScoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId,
                new GetScoresOptions { Limit = 10 });

            List<(string name, long score, bool isPlayer, int displayRank)> entriesToDisplay = new List<(string, long, bool, int)>();

            int topScoreCount = 0;
            foreach (var entry in topScoresResponse.Results)
            {
                if (topScoreCount >= 10) break;

                if (entry.PlayerId == playerId && playerInTop10)
                {
                    continue;
                }

                entriesToDisplay.Add((entry.PlayerName, (long)entry.Score, false, topScoreCount + 1));
                topScoreCount++;
            }

            while (entriesToDisplay.Count < 10 && entriesToDisplay.Count < defaultScores.Count)
            {
                int defaultIndex = entriesToDisplay.Count;
                entriesToDisplay.Add((defaultScores[defaultIndex].name,
                                     defaultScores[defaultIndex].score,
                                     false,
                                     entriesToDisplay.Count + 1));
            }

            if (playerInTop10 && playerEntry != null)
            {
                int insertPos = 0;
                while (insertPos < entriesToDisplay.Count && entriesToDisplay[insertPos].score > playerScore)
                {
                    insertPos++;
                }

                entriesToDisplay.Insert(insertPos, (playerName, playerScore, true, insertPos + 1));

                if (entriesToDisplay.Count > 10)
                {
                    entriesToDisplay.RemoveAt(10);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                if (i < entriesToDisplay.Count)
                {
                    var (name, score, isPlayer, displayRank) = entriesToDisplay[i];
                    int containerIndex = (i < 5) ? 0 : 1;
                    CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex],
                                          displayRank,
                                          name,
                                          score,
                                          isPlayer);
                }
            }

            if (!playerInTop10)
            {
                int containerIndex = 1;
                CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex],
                                      10,
                                      playerName,
                                      playerScore,
                                      true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get leaderboard data: {e.Message}");
            DisplayDefaultScores();
        }
    }

    private void DisplayDefaultScores()
    {
        foreach (Transform container in leaderboardEntryContainers)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < 9; i++)
        {
            var (name, score) = defaultScores[i];
            int containerIndex = (i < 5) ? 0 : 1;
            CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex],
                                  i + 1,
                                  name,
                                  score,
                                  false);
        }

        if (!string.IsNullOrEmpty(playerName))
        {
            int containerIndex = 1;
            CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex],
                                  10,
                                  playerName,
                                  0,
                                  true);
        }
    }

    private void CreateLeaderboardEntry(Transform container, int rank, string playerName, long score, bool isPlayer)
    {
        GameObject entryObj = Instantiate(leaderboardEntryPrefab, container);
        LeaderboardEntryUI entryUI = entryObj.GetComponent<LeaderboardEntryUI>();

        if (entryUI != null)
        {
            entryUI.SetEntry(rank, playerName, score, isPlayer ? playerEntryColor : Color.white);
        }
    }

    public void HideLeaderboard()
    {
        if (leaderboardPanelAnimator != null)
        {
            leaderboardPanelAnimator.SetTrigger("SlideOut");
        }
        if (itemsTransitionAnimator != null)
        {
            itemsTransitionAnimator.SetTrigger("ReverseAnimation");
        }
    }

    public static void ResetPlayerData()
    {
        PlayerPrefs.DeleteKey("HighScore");

        if (Instance != null)
        {
            Instance.playerName = "Player_0000";
            Instance.isInitialized = false;
            Instance.isSigningIn = false;

            Instance.InitializeUnityServices();
        }

        Debug.Log("Player data reset successfully!");
    }
}