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
    [SerializeField] private Transform[] leaderboardEntryContainers; // Tableau pour les deux conteneurs
    [SerializeField] private GameObject leaderboardEntryPrefab;
    [SerializeField] private Color playerEntryColor = Color.green;

    // Scores fixes par défaut
    private readonly List<(string name, long score)> defaultScores = new List<(string, long)>
    {
        ("Player_01", 5000),
        ("Player_02", 4500),
        ("Player_03", 4000),
        ("Player_04", 3500),
        ("Player_05", 3000),
        ("Player_06", 2500),
        ("Player_07", 2000),
        ("Player_08", 1500),
        ("Player_09", 1000),
        ("Player_10", 500)
    };

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
            var options = new InitializationOptions();
            await UnityServices.InitializeAsync(options);
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

        // Effacer les entrées précédentes dans les deux conteneurs
        foreach (Transform container in leaderboardEntryContainers)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        try
        {
            // Récupérer les meilleurs scores
            var topScoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId,
                new GetScoresOptions { Limit = topScoresLimit });

            // Liste pour stocker les scores réels et par défaut
            List<(string name, long score, bool isPlayer, int rank)> scoresToDisplay = new List<(string, long, bool, int)>();

            // Vérifier si le joueur est dans le top
            bool playerInTop = false;
            int playerRank = -1;
            long playerScore = 0;

            // Ajouter les scores réels
            for (int i = 0; i < topScoresResponse.Results.Count; i++)
            {
                var entry = topScoresResponse.Results[i];
                scoresToDisplay.Add((entry.PlayerName, (long)entry.Score, entry.PlayerId == playerId, i + 1));

                if (entry.PlayerId == playerId)
                {
                    playerInTop = true;
                }
            }

            // Si moins de 10 scores réels, compléter avec les scores par défaut
            for (int i = topScoresResponse.Results.Count; i < 10; i++)
            {
                var defaultScore = defaultScores[i];
                scoresToDisplay.Add((defaultScore.name, defaultScore.score, false, i + 1));
            }

            // Si le joueur n'est pas dans le top, essayer de récupérer son score
            if (!playerInTop)
            {
                try
                {
                    var playerScoreResponse = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
                    playerScore = (long)playerScoreResponse.Score;

                    // Récupérer le rang du joueur
                    var allScoresResponse = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId);
                    playerRank = 1;
                    foreach (var entry in allScoresResponse.Results)
                    {
                        if (entry.PlayerId == playerId)
                        {
                            break;
                        }
                        playerRank++;
                    }

                    // Remplacer la 10ème entrée avec le score du joueur
                    if (playerRank > 10 && scoresToDisplay.Count >= 10)
                    {
                        scoresToDisplay[9] = ($"{playerName} (You)", playerScore, true, playerRank);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not get player score: {e.Message}");
                    // Si on ne peut pas récupérer le score du joueur, on ne fait rien
                }
            }

            // Créer les entrées pour tous les scores
            for (int i = 0; i < scoresToDisplay.Count; i++)
            {
                var (name, score, isPlayer, rank) = scoresToDisplay[i];
                int containerIndex = i / 5; // 0 pour les 5 premiers, 1 pour les 5 suivants
                CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex], rank, name, score, isPlayer);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get leaderboard data: {e.Message}");
            // En cas d'erreur, afficher les scores par défaut
            DisplayDefaultScores();
        }
    }

    private void DisplayDefaultScores()
    {
        // Effacer les entrées précédentes dans les deux conteneurs
        foreach (Transform container in leaderboardEntryContainers)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        // Afficher les scores par défaut
        for (int i = 0; i < defaultScores.Count; i++)
        {
            var (name, score) = defaultScores[i];
            int containerIndex = i / 5;
            CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex], i + 1, name, score, false);
        }

        // Ajouter le joueur actuel s'il est connecté
        if (!string.IsNullOrEmpty(playerName))
        {
            int containerIndex = 9 / 5; // 10ème entrée dans le 2ème conteneur
            CreateLeaderboardEntry(leaderboardEntryContainers[containerIndex], 10, $"{playerName} (You)", 0, true);
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
}