using FumoCore.Tools;
using Fumorin;
using TMPro;
using UnityEngine;

namespace Chireinia
{
    public class ScoreUI : MonoBehaviour
    {
        [SerializeField] TMP_Text scoreText;
        private void Start()
        {
            GeneralManager.OnScoreUpdate += UpdateScore;
            GeneralManager.RequestScoreRefresh();
        }
        private void UpdateScore(double s, double highScore)
        {
            scoreText.text = s.ToThousandsString(0, " ");
        }
    }
}
