using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateTrialDefinitionsEditor
{
    /*
    [MenuItem("Tools/Generate All Trial Definitions")]
    public static void GenerateTrials()
    {
        string basePath = "Assets/Trials/";  // 👈 Change this to wherever you want to store them
        if (!AssetDatabase.IsValidFolder(basePath))
        {
            AssetDatabase.CreateFolder("Assets", "Trials");
        }

        // ===========================
        // 🎯 Normal Trials
        // ===========================

        var normalTrials = new List<TrialDefinition>
        {
            CreateTrial("Standard Challenge", 15, 3),
            CreateTrial("Quick Strike", 12, 2),
            CreateTrial("Power Test", 20, 3),
            CreateTrial("One Shot", 18, 1),
            CreateTrial("Endurance", 25, 4),

            CreateTrial("Hearts Only", 18, 3, new List<CardData.Suit> { CardData.Suit.Hearts }),
            CreateTrial("Spades Mastery", 16, 2, new List<CardData.Suit> { CardData.Suit.Spades }),
            CreateTrial("Diamonds Focus", 20, 3, new List<CardData.Suit> { CardData.Suit.Diamonds }),
            CreateTrial("Clubs Challenge", 17, 3, new List<CardData.Suit> { CardData.Suit.Clubs }),
            CreateTrial("Red Suits", 22, 3, new List<CardData.Suit> { CardData.Suit.Hearts, CardData.Suit.Diamonds }),
            CreateTrial("Black Suits", 22, 3, new List<CardData.Suit> { CardData.Suit.Spades, CardData.Suit.Clubs }),
            CreateTrial("Rainbow", 28, 4, new List<CardData.Suit> { CardData.Suit.Hearts, CardData.Suit.Spades, CardData.Suit.Diamonds, CardData.Suit.Clubs }),

            CreateTrial("Pair Power", 14, 3, null, new List<CardData.FusionType> { CardData.FusionType.Pair }),
            CreateTrial("Flush Focus", 16, 2, null, new List<CardData.FusionType> { CardData.FusionType.Flush }),
            CreateTrial("Straight Line", 18, 2, null, new List<CardData.FusionType> { CardData.FusionType.Straight }),
            CreateTrial("Face Off", 20, 3, null, new List<CardData.FusionType> { CardData.FusionType.FaceFusion }),
            CreateTrial("Royal Road", 25, 2, null, new List<CardData.FusionType> { CardData.FusionType.RoyalFlush }),
            CreateTrial("Mixed Mastery", 30, 3, null, new List<CardData.FusionType> { CardData.FusionType.Pair, CardData.FusionType.Flush, CardData.FusionType.Straight }),

            CreateTrial("Precision Strike", 30, 1),
            CreateTrial("Perfect Flush", 24, 2, new List<CardData.Suit> { CardData.Suit.Hearts }, new List<CardData.FusionType> { CardData.FusionType.Flush }),
            CreateTrial("Straight Royal", 35, 2, null, new List<CardData.FusionType> { CardData.FusionType.StraightRoyalFlush }),
            CreateTrial("All or Nothing", 40, 2)
        };

        // ===========================
        // 👑 Boss Trials
        // ===========================

        var bossTrials = new List<TrialDefinition>
        {
            CreateTrial("The Pair King", 35, 3, null, new List<CardData.FusionType> { CardData.FusionType.Pair, CardData.FusionType.FaceFusion }),
            CreateTrial("Flush Empress", 40, 3, null, new List<CardData.FusionType> { CardData.FusionType.Flush, CardData.FusionType.StraightFlush }),
            CreateTrial("Straight Baron", 45, 2, null, new List<CardData.FusionType> { CardData.FusionType.Straight, CardData.FusionType.FaceSequence }),
            CreateTrial("Royal Sovereign", 50, 3, null, new List<CardData.FusionType> { CardData.FusionType.RoyalFlush, CardData.FusionType.StraightRoyalFlush }),

            CreateTrial("Heart Tyrant", 42, 4, new List<CardData.Suit> { CardData.Suit.Hearts }),
            CreateTrial("Spade Warlord", 38, 2, new List<CardData.Suit> { CardData.Suit.Spades }),
            CreateTrial("Diamond Mogul", 45, 3, new List<CardData.Suit> { CardData.Suit.Diamonds }),
            CreateTrial("Club Destroyer", 40, 3, new List<CardData.Suit> { CardData.Suit.Clubs }),
            CreateTrial("Chromatic Lord", 55, 4, new List<CardData.Suit> { CardData.Suit.Hearts, CardData.Suit.Spades, CardData.Suit.Diamonds, CardData.Suit.Clubs }),

            CreateTrial("The Perfectionist", 60, 2, null, new List<CardData.FusionType> { CardData.FusionType.RoyalFlush }),
            CreateTrial("Fusion Grandmaster", 65, 3, null, new List<CardData.FusionType> { CardData.FusionType.Pair, CardData.FusionType.Flush, CardData.FusionType.Straight, CardData.FusionType.FaceFusion }),
            CreateTrial("Rainbow Destroyer", 70, 4, new List<CardData.Suit> { CardData.Suit.Hearts, CardData.Suit.Spades, CardData.Suit.Diamonds, CardData.Suit.Clubs }, new List<CardData.FusionType> { CardData.FusionType.Flush, CardData.FusionType.Straight }),
            CreateTrial("Final Reckoning", 80, 3),
            CreateTrial("Impossible Odds", 50, 1, null, new List<CardData.FusionType> { CardData.FusionType.StraightRoyalFlush }),
            CreateTrial("Suite Master", 55, 2, new List<CardData.Suit> { CardData.Suit.Hearts, CardData.Suit.Spades }, new List<CardData.FusionType> { CardData.FusionType.RoyalFlush, CardData.FusionType.FaceSequence }),
            CreateTrial("The Final Boss", 100, 4, new List<CardData.Suit> { CardData.Suit.Hearts, CardData.Suit.Spades, CardData.Suit.Diamonds, CardData.Suit.Clubs }, new List<CardData.FusionType> { CardData.FusionType.RoyalFlush, CardData.FusionType.StraightRoyalFlush })
        };

        // ===========================
        // Save All
        // ===========================

        SaveTrials(normalTrials, basePath + "Normal_");
        SaveTrials(bossTrials, basePath + "Boss_");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ All Trial Definitions created successfully!");
    }

    private static TrialDefinition CreateTrial(string name, int fps, int playLimit, List<CardData.Suit> suits = null, List<CardData.FusionType> types = null)
    {
        var trial = ScriptableObject.CreateInstance<TrialDefinition>();
        trial.Name = name;
        trial.TargetFPS = fps;
        trial.PlayLimit = playLimit;
        trial.RequiredSuits = suits ?? new List<CardData.Suit>();
        trial.RequiredTypes = types ?? new List<CardData.FusionType>();
        return trial;
    }

    private static void SaveTrials(List<TrialDefinition> trials, string prefix)
    {
        for (int i = 0; i < trials.Count; i++)
        {
            var trial = trials[i];
            string path = $"{prefix}{trial.Name.Replace(" ", "_")}.asset";
            AssetDatabase.CreateAsset(trial, path);
        }
    }
    */
}
