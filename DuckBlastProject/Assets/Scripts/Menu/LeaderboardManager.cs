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
    [SerializeField] private Transform[] leaderboardEntryContainers;
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Color playerEntryColor = Color.green;

    // Scores fixes par défaut
    private readonly List<(string name, long score)> defaultScores = new List<(string, long)>
    {
        ("Player_00001", 5000),
        ("Player_00002", 4500),
        ("Player_00003", 4000),
        ("Player_00004", 3500),
        ("Player_00005", 3000),
        ("Player_00006", 2500),
        ("Player_00007", 2000),
        ("Player_00008", 1500),
        ("Player_00009", 1000),
        ("Player_00010", 500),
        ("Player_00011", 400),
        ("Player_00012", 300),
    };

    private string playerId;
    private string playerName = "Player_00000";
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

                int randomNumber = Random.Range(0, 100000);
                playerName = $"Player_{randomNumber:D5}";
                Debug.Log($"Player name set to: {playerName}");
            }
            else
            {
                playerId = AuthenticationService.Instance.PlayerId;
                int randomNumber = Random.Range(0, 100000);
                playerName = $"Player_{randomNumber:D5}";
                Debug.Log($"Already signed in with ID: {playerId}, Name: {playerName}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to sign in: {e.Message}");
            int randomNumber = Random.Range(0, 100000);
            playerName = $"Player_{randomNumber:D5}";
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
            var topScoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId,
                new GetScoresOptions { Limit = topScoresLimit });

            List<(string name, long score, bool isPlayer, int rank)> scoresToDisplay = new List<(string, long, bool, int)>();

            bool playerInTop = false;
            int playerRank = -1;
            long playerScore = 0;

            for (int i = 0; i < topScoresResponse.Results.Count; i++)
            {
                var entry = topScoresResponse.Results[i];
                string displayName = (entry.PlayerId == playerId) ? playerName : entry.PlayerName;
                scoresToDisplay.Add((displayName, (long)entry.Score, entry.PlayerId == playerId, i + 1));

                if (entry.PlayerId == playerId)
                {
                    playerInTop = true;
                }
            }

            for (int i = topScoresResponse.Results.Count; i < 10; i++)
            {
                var defaultScore = defaultScores[i];
                scoresToDisplay.Add((defaultScore.name, defaultScore.score, false, i + 1));
            }

            if (!playerInTop)
            {
                try
                {
                    var playerScoreResponse = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
                    playerScore = (long)playerScoreResponse.Score;

                    var allScoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId);
                    var sortedScores = allScoresResponse.Results.OrderByDescending(e => e.Score).ToList();
                    playerRank = sortedScores.FindIndex(e => e.PlayerId == playerId) + 1;

                    if (scoresToDisplay.Count >= 10)
                    {
                        var topNine = scoresToDisplay.Take(9).ToList();
                        topNine.Add(($"{playerName}", playerScore, true, playerRank));
                        scoresToDisplay = topNine;
                    }
                    else
                    {
                        scoresToDisplay.Add(($"{playerName}", playerScore, true, playerRank));
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not get player score: {e.Message}");
                    if (scoresToDisplay.Count >= 10)
                    {
                        var topNine = scoresToDisplay.Take(9).ToList();
                        topNine.Add(($"{playerName}", 0, true, defaultScores.Count + 1));
                        scoresToDisplay = topNine;
                    }
                    else
                    {
                        scoresToDisplay.Add(($"{playerName}", 0, true, defaultScores.Count + 1));
                    }
                }
            }

            scoresToDisplay = scoresToDisplay.OrderByDescending(s => s.score).ToList();

            for (int i = 0; i < scoresToDisplay.Count; i++)
            {
                var item = scoresToDisplay[i];
                scoresToDisplay[i] = (item.name, item.score, item.isPlayer, i + 1);
            }

            for (int i = 0; i < scoresToDisplay.Count; i++)
            {
                var (name, score, isPlayer, rank) = scoresToDisplay[i];
                int containerIndex = (i < 5) ? 0 : 1;
                CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex], rank, name, score, isPlayer);
            }

            if (!playerInTop && scoresToDisplay.Count >= 10)
            {
                var playerEntry = scoresToDisplay.FirstOrDefault(s => s.isPlayer);
                if (playerEntry.name != null)
                {
                    int containerIndex = 1;
                    CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex], playerEntry.rank, playerEntry.name, playerEntry.score, true);
                }
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

        for (int i = 0; i < Mathf.Min(defaultScores.Count, 10); i++)
        {
            var (name, score) = defaultScores[i];
            int rank = i + 1;
            int containerIndex = (i < 5) ? 0 : 1;
            CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex], rank, name, score, false);
        }

        if (!string.IsNullOrEmpty(playerName))
        {
            int containerIndex = 1;
            CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex], defaultScores.Count + 1, $"{playerName}", 0, true);
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

    public static void ResetPlayerData()
    {
        PlayerPrefs.DeleteKey("HighScore");

        if (Instance != null)
        {
            Instance.playerName = "Player_00000";
            Instance.isInitialized = false;
            Instance.isSigningIn = false;

            Instance.InitializeUnityServices();
        }

        Debug.Log("Player data reset successfully!");
    }
}