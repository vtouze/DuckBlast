using UnityEngine;
using TMPro;

public class LeaderboardEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;

    public void SetEntry(int rank, string playerName, long score, Color nameColor)
    {
        rankText.text = $"{rank}.";
        playerNameText.text = playerName;
        playerNameText.color = nameColor;
        scoreText.text = score.ToString();
    }
}