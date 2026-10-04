
using TMPro;
using UnityEngine;


// One bet-history row. Not populated yet: waiting on the dice BET_HISTORY payload.
public class HistoryPrefab : MonoBehaviour
{
    [SerializeField] private TMP_Text Index;
    [SerializeField] private TMP_Text RoundId;
    [SerializeField] private TMP_Text Stake;
    [SerializeField] private TMP_Text Win;
    [SerializeField] private TMP_Text PL;
}
