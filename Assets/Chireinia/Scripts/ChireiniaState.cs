using Fumorin;
using TMPro;
using UnityEngine;

namespace Chireinia
{
    public class ChireiniaState : MonoBehaviour
    {
        public static void AddScore(double s, string key)
        {
            GeneralManager.AddScore(s, false);
            GeneralManager.AddScoreAnalysisKey(key, s);
        }
    }
}
