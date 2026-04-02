
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class HistoryPrefab : MonoBehaviour
{
    [SerializeField] private TMP_Text Index;
    [SerializeField] private TMP_Text RoundId;
    [SerializeField] private TMP_Text Stake;
    [SerializeField] private TMP_Text Win;
    [SerializeField] private TMP_Text PL;
    [SerializeField] private Image Middlecard;
    [SerializeField] private Image SideCard;

    internal void SetData(int index, History item, Sprite middle, Sprite side)
    {
        Index.text = index.ToString();
        RoundId.text = item.round_id;
        Stake.text = item.bet_amount;
        Win.text = item.win_amount;

        int pl = int.Parse(item.win_amount) - int.Parse(item.bet_amount);
        PL.text = pl.ToString();

        Middlecard.sprite = middle;
        SideCard.sprite = side;
    }
}
