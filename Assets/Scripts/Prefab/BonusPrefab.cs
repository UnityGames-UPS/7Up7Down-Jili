using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class BonusPrefab : MonoBehaviour
{
    [SerializeField] private List<Sprite> NumbersSprites;
    [SerializeField] private GameObject NumbersPatrent;
    [SerializeField] private ImageAnimation BgAnimatio;
    [SerializeField] private List<Sprite> Numbers;
}
