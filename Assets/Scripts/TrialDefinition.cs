using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewTrialDefinition", menuName = "Trials/TrialDefinition")]
public class TrialDefinition : ScriptableObject
{
    [Header("Trial Info")]
    public string Name;

    [Header("Difficulty & Rules")]
    public int TargetFPS;
    public int PlayLimit;

    [Header("Constraints")]
    public List<CardData.Suit> RequiredSuits = new List<CardData.Suit>();  
    public List<CardData.FusionType> RequiredTypes = new List<CardData.FusionType>();  

    [Header("Description")]
    [TextArea] public string Description;
}
