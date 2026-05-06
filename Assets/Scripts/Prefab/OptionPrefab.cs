using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionPrefab : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Button btn;
    [SerializeField] internal GameObject BlackTransParent;

    [SerializeField] private TMP_Text Name;
    [SerializeField] private TMP_Text Text;
    [SerializeField] internal ImageAnimation winAnimation;
    [SerializeField] internal ImageAnimation winAnimationBorder;
    [SerializeField] internal ImageAnimation PurpleBorderAnimation;
    [SerializeField] internal GameObject MyBetObj;
    [SerializeField] internal TMP_Text MyBetText;
    [SerializeField] internal TMP_Text PlayerChipText;
    [SerializeField] internal TMP_Text OtherChipText;
    [SerializeField] internal Image PlayerbetPos;
    [SerializeField] internal Transform PlayerbetStartPos;
    [SerializeField] internal Image OtherPlayerbetPos;

    // Add these references for chip display
    [SerializeField] internal Image PlayerChipImage;
    [SerializeField] internal Image OtherPlayerChipImage;

    // Track current bet values and displayed chip denomination
    private int currentPlayerBetValue = 0;
    private int currentOtherPlayerBetValue = 0;
    private int currentPlayerDisplayedChipValue = 0;
    private int currentOtherPlayerDisplayedChipValue = 0;

    internal int Optionindex;
    internal string VaridontWant;

    internal string NameT;

    void Start()
    {
        if (btn)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickBtn);
        }

        // Initialize chip displays
        if (PlayerChipImage != null) PlayerChipImage.gameObject.SetActive(false);
        if (OtherPlayerChipImage != null) OtherPlayerChipImage.gameObject.SetActive(false);
    }

    internal void OnClickBtn()
    {
        Debug.Log("&&" + Optionindex + " " + gameObject.name);
        gameManager.onClickOption(gameObject);
    }

    internal void SetData(int index, string name, List<int> ratio, string OptionType)
    {
        if (Name) Name.text = name;
        Text.text = ratio[0] + " : " + ratio[1];

        Optionindex = index;
        VaridontWant = OptionType;
        //  Debug.Log("&&" + index);
    }

    internal void DontShowText()
    {
        MyBetText.text = "";
        MyBetObj.SetActive(false);
    }

    internal void AddPlayerChip(int Amount, Sprite chipImg)
    {
        Debug.Log("adddChip" + Amount);
        // Update total bet value
        currentPlayerBetValue += Amount;

        // Update bet text
        if (MyBetText != null)
        {
            MyBetText.text = currentPlayerBetValue.ToString();
            MyBetObj.SetActive(currentPlayerBetValue > 0);
        }

        // Update chip sprite based on the new total amount
        UpdatePlayerChipDisplay();
    }
    internal void AddOtherPlayerChip(int Amount, Sprite chipImg)
    {
        // Update total bet value for other players
        currentOtherPlayerBetValue += Amount;

        // Update other player text
        if (OtherChipText != null)
        {
            OtherChipText.text = currentOtherPlayerBetValue.ToString();
            if (currentOtherPlayerBetValue <= 0 && OtherChipText.text == "0")
            {
                // Optionally hide the bet display when zero
            }
        }

        // Update chip sprite based on the new total amount
        UpdateOtherPlayerChipDisplay();
    }

    internal void ResetPlayerUI()
    {
        // Reset ONLY player's bets, keep other players' bets
        currentPlayerBetValue = 0;
        currentPlayerDisplayedChipValue = 0;

        if (MyBetText != null)
        {
            MyBetText.text = "0";
            MyBetObj.SetActive(false);
        }

        if (PlayerChipImage != null)
        {
            PlayerChipImage.gameObject.SetActive(false);
            PlayerChipImage.sprite = null;
        }

        if (PlayerChipText != null)
        {
            PlayerChipText.text = "0";
        }

        // DO NOT reset OtherPlayerChipImage or OtherChipText here
    }

    // private void UpdatePlayerChipDisplay()
    // {
    //     if (PlayerChipImage == null) return;

    //     if (currentPlayerBetValue <= 0)
    //     {
    //         PlayerChipImage.gameObject.SetActive(false);
    //         if (PlayerChipText != null) PlayerChipText.text = "0";
    //         return;
    //     }

    //     // Get the appropriate chip denomination for the current total bet
    //     int displayChipValue = GetChipDenominationForAmount(currentPlayerBetValue);

    //     // Only update sprite if the displayed chip denomination changed
    //     if (displayChipValue != currentPlayerDisplayedChipValue)
    //     {
    //         currentPlayerDisplayedChipValue = displayChipValue;

    //         // Get the sprite for this denomination from GameManager
    //         Sprite newSprite = gameManager.GetChipSpriteForAmount(displayChipValue, true);
    //         PlayerChipImage.sprite = newSprite;
    //         PlayerChipImage.gameObject.SetActive(true);

    //         // Update the text to show the chip value
    //         if (PlayerChipText != null)
    //         {
    //             PlayerChipText.text = displayChipValue.ToString();
    //         }
    //     }

    //     // Always update the total bet text
    //     if (PlayerChipText != null && PlayerChipText.text != currentPlayerBetValue.ToString())
    //     {
    //         PlayerChipText.text = currentPlayerBetValue.ToString();
    //     }
    // }
    private void UpdatePlayerChipDisplay()
    {
        if (PlayerChipImage == null) return;

        if (currentPlayerBetValue <= 0)
        {
            PlayerChipImage.gameObject.SetActive(false);
            if (PlayerChipText != null) PlayerChipText.text = "0";
            currentPlayerDisplayedChipValue = 0;  // ← ADD THIS LINE
            return;
        }

        // Get the appropriate chip denomination for the current total bet
        int displayChipValue = GetChipDenominationForAmount(currentPlayerBetValue);

        // Only update sprite if the displayed chip denomination changed
        if (displayChipValue != currentPlayerDisplayedChipValue)
        {
            currentPlayerDisplayedChipValue = displayChipValue;

            // Get the sprite for this denomination from GameManager
            Sprite newSprite = gameManager.GetChipSpriteForAmount(displayChipValue, true);
            PlayerChipImage.sprite = newSprite;
            PlayerChipImage.gameObject.SetActive(true);

            // Update the text to show the chip value
            if (PlayerChipText != null)
            {
                PlayerChipText.text = displayChipValue.ToString();
            }
        }

        // Always update the total bet text
        if (PlayerChipText != null && PlayerChipText.text != currentPlayerBetValue.ToString())
        {
            PlayerChipText.text = currentPlayerBetValue.ToString();
        }
    }

    private void UpdateOtherPlayerChipDisplay()
    {
        if (OtherPlayerChipImage == null) return;

        if (currentOtherPlayerBetValue <= 0)
        {
            OtherPlayerChipImage.gameObject.SetActive(false);
            if (OtherChipText != null) OtherChipText.text = "0";
            return;
        }

        // Get the appropriate chip denomination for the current total bet
        int displayChipValue = GetChipDenominationForAmount(currentOtherPlayerBetValue);

        // Only update sprite if the displayed chip denomination changed
        if (displayChipValue != currentOtherPlayerDisplayedChipValue)
        {
            currentOtherPlayerDisplayedChipValue = displayChipValue;

            // Get the sprite for this denomination from GameManager
            Sprite newSprite = gameManager.GetChipSpriteForAmount(displayChipValue, false);
            OtherPlayerChipImage.sprite = newSprite;
            OtherPlayerChipImage.gameObject.SetActive(true);

            // Update the text to show the chip value
            if (OtherChipText != null)
            {
                OtherChipText.text = displayChipValue.ToString();
            }
        }

        // Always update the total bet text
        if (OtherChipText != null && OtherChipText.text != currentOtherPlayerBetValue.ToString())
        {
            OtherChipText.text = currentOtherPlayerBetValue.ToString();
        }
    }

    private int GetChipDenominationForAmount(int amount)
    {
        // This method needs access to room chips. You can either:
        // Option 1: Store room chips in OptionPrefab or GameManager reference
        // Option 2: Pass room chips as parameter

        // For now, we'll get from GameManager
        List<int> roomChips = gameManager.FindRoom();
        if (roomChips == null || roomChips.Count == 0)
            return amount;

        // Find the largest chip denomination that is <= amount
        int selectedDenomination = roomChips[0]; // Start with smallest

        foreach (int chipValue in roomChips)
        {
            if (amount >= chipValue)
            {
                selectedDenomination = chipValue;
            }
            else
            {
                break; // Since roomChips should be sorted ascending
            }
        }

        return selectedDenomination;
    }

    // Helper method to reset the UI for this option
    internal void ResetOptionUI()
    {
        currentPlayerBetValue = 0;
        currentOtherPlayerBetValue = 0;
        currentPlayerDisplayedChipValue = 0;
        currentOtherPlayerDisplayedChipValue = 0;

        if (MyBetText != null)
        {
            MyBetText.text = "0";
            MyBetObj.SetActive(false);
        }

        if (PlayerChipImage != null)
        {
            PlayerChipImage.gameObject.SetActive(false);
            PlayerChipImage.sprite = null;
        }

        if (PlayerChipText != null)
        {
            PlayerChipText.text = "0";
        }

        if (OtherPlayerChipImage != null)
        {
            OtherPlayerChipImage.gameObject.SetActive(false);
            OtherPlayerChipImage.sprite = null;
        }

        if (OtherChipText != null)
        {
            OtherChipText.text = "0";
        }
    }
    internal void RefreshChipDisplay()
    {
        UpdatePlayerChipDisplay();
    }
}