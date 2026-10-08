using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerData : MonoBehaviour
{
  [SerializeField] internal Image PlayerIcon;
  [SerializeField] internal TMP_Text playername;
  [SerializeField] internal TMP_Text playerBalence;
  [SerializeField] internal string PlayerId;
  // Where this player's winning chips fly to; this object when unset
  [SerializeField] private Transform chipAnchor;

  internal Transform ChipAnchor => chipAnchor ? chipAnchor : transform;

  internal void SetData(string idz, string balance, Sprite icon)
  {
    gameObject.SetActive(true);
    PlayerIcon.sprite = icon;
    PlayerId = idz;
    playername.text = PlayerId;
    playerBalence.text = balance;
  }
}
