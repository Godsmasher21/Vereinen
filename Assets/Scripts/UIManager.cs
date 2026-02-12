using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Text References")]
    public TMP_Text moneyText;
    public TMP_Text FusionCost;
    public TMP_Text Ante;
    public TMP_Text deckCountText;

    [Header("Trial UI")]
    public GameObject trialInfoUI;
    public TMP_Text trialTitleText;
    public TMP_Text trialTitleTextDuringTrial;
    public TMP_Text trialObjectiveText;
    public TMP_Text trialTargetFPSText;
    public TMP_Text trialPlayLimitText;
    public TMP_Text trialSuitsText;
    public TMP_Text trialTypesText;
    public TMP_Text playedFPSText;
    public TMP_Text HandsLeft;

    [Header("Boss Trial UI")]
    public GameObject bossTrialInfoUI;
    public TMP_Text bossTrialTitleText;
    public TMP_Text bossTrialObjectiveText;
    public TMP_Text bossTrialTargetFPSText;
    public TMP_Text bossTrialPlayLimitText;
    public TMP_Text bossTrialSuitsText;
    public TMP_Text bossTrialTypesText;

    [Header("Floating Text Settings")]
    public GameObject floatingTextPrefab;

    [Header("Panels")]
    public GameObject HomeUI;
    public GameObject fusionUI;
    public GameObject trialUI;
    public GameObject shopUI;
    public GameObject YouLostUI;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateMoney(int money)
    {
        moneyText.text = $"$ {money}";
    }

    public void UpdateFusionCost(int money)
    {
        FusionCost.text = $"Fusion Cost\n$ {money}";
    }

    public void UpdateAnte(int ante)
    {
        ShowFloatingText("+1", Color.green, Ante.GetComponent<RectTransform>());
        Ante.text = $"Ante: {ante} / 4";
    }

    public void UpdateDeckCount()
    {
        int count = FusionGameManager.Instance.playerDeck.Count;
        deckCountText.text = $"Deck: {count} cards";
    }

    public void UpdateTrialUI(TrialDefinition trial, int fusionPower)
    {
        trialUI.SetActive(true);
        trialTitleTextDuringTrial.text = $"Trial: {trial.Name}";
        trialObjectiveText.text = GetObjectiveText(trial);
    }

    public void UpdatePlayedHands(int playedHands)
    {
        HandsLeft.text = $"Hands:\n{playedHands}";
    }

    public void ShowPlayedFPS(int totalFPS)
    {
        playedFPSText.text = $"{totalFPS}";
    }

    private string GetObjectiveText(TrialDefinition trial)
    {
        List<string> objectives = new List<string>();
        objectives.Add($"Achieve {trial.TargetFPS}+ Fusion Power");
        objectives.Add($"Play ≤ {trial.PlayLimit} hands");
        if (trial.RequiredSuits.Count > 0)
            objectives.Add($"Must include suit(s): {string.Join(", ", trial.RequiredSuits)}");
        if (trial.RequiredTypes.Count > 0)
            objectives.Add($"Must include fusion type(s): {string.Join(", ", trial.RequiredTypes)}");
        return string.Join("\n", objectives);
    }

    public void ShowFloatingText(string text, Color color, RectTransform targetUI, float offsetY = -30f, float duration = 1f)
    {
        GameObject floatingTextObj = Instantiate(floatingTextPrefab, targetUI);
        var textComponent = floatingTextObj.GetComponent<TMP_Text>(); 
        var textRect = floatingTextObj.GetComponent<RectTransform>();

        textComponent.text = text;
        textComponent.color = color;

        textRect.localPosition = new Vector3(0, offsetY, 0);

        var seq = DOTween.Sequence();
        seq.Append(textRect.DOShakeAnchorPos(0.25f, new Vector2(3f, 0f), 30, 90, false, false))
           .Append(textRect.DOAnchorPosY(textRect.anchoredPosition.y + 30f, 0.5f).SetEase(Ease.OutQuad))
           .Join(textComponent.DOFade(0f, 0.5f))
           .AppendInterval(0.3f)
           .OnComplete(() => Destroy(floatingTextObj));

        seq.Play();
    }
    

    public void ShowBossTrialInfo(TrialDefinition trial)
    {
        bossTrialInfoUI.SetActive(true);

        bossTrialTitleText.text = $"Boss Trial: {trial.Name}";
        bossTrialTargetFPSText.text = $"Target FPS: {trial.TargetFPS}";
        bossTrialPlayLimitText.text = $"Plays Allowed: {trial.PlayLimit}";

        if (trial.RequiredSuits.Count > 0)
            bossTrialSuitsText.text = $"Required Suits: {string.Join(", ", trial.RequiredSuits)}";
        else
            bossTrialSuitsText.text = $"Required Suits: None";

        if (trial.RequiredTypes.Count > 0)
            bossTrialTypesText.text = $"Required Fusion Types: {string.Join(", ", trial.RequiredTypes)}";
        else
            bossTrialTypesText.text = $"Required Fusion Types: None";
    }

    public void ShowTrialInfo(TrialDefinition trial)
    {
        trialInfoUI.SetActive(true);

        trialTitleText.text = $"Trial: {trial.Name}";
        trialTargetFPSText.text = $"Target FPS: {trial.TargetFPS}";
        trialPlayLimitText.text = $"Plays Allowed: {trial.PlayLimit}";

        if (trial.RequiredSuits.Count > 0)
            trialSuitsText.text = $"Required Suits: {string.Join(", ", trial.RequiredSuits)}";
        else
            trialSuitsText.text = $"Required Suits: None";

        if (trial.RequiredTypes.Count > 0)
            trialTypesText.text = $"Required Fusion Types: {string.Join(", ", trial.RequiredTypes)}";
        else
            trialTypesText.text = $"Required Fusion Types: None";
    }

    public void ShowUI(GamePhase phase)
    {
        HomeUI.SetActive(false);
        fusionUI.SetActive(false);
        trialUI.SetActive(false);
        shopUI.SetActive(false);
        YouLostUI.SetActive(false);

        HomeUI.SetActive(phase == GamePhase.Home);
        fusionUI.SetActive(phase == GamePhase.FusionRound);
        trialUI.SetActive(phase == GamePhase.TrialRound);
        shopUI.SetActive(phase == GamePhase.ShopPhase);
        YouLostUI.SetActive(phase == GamePhase.GameOver);
    }

    public void RestoreAllUIAfterReset()
    {
        try
        {
            Transform canvasRoot = YouLostUI.transform.parent;
            foreach (Transform child in canvasRoot)
            {
                // Re-enable everything except "You Lost"
                if (child.name.ToLower().Contains("you lost"))
                    child.gameObject.SetActive(false);
                else
                    child.gameObject.SetActive(true);
            }

            Debug.Log("🟢 UI restored after reset.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error restoring UI after reset: {ex.Message}");
        }
    }

    public void ShowYouLostMessage()
    {
        try
        {
            if (YouLostUI == null)
            {
                Debug.LogWarning("⚠️ YouLostUI reference missing in UIManager!");
                return;
            }

            // ✅ Update game phase
            GameOrchestrator.Instance.currentPhase = GamePhase.GameOver;

            // Get the Canvas root
            Transform canvasRoot = YouLostUI.transform.parent;

            // 1️⃣ Disable all siblings of YouLost except Background and Image
            foreach (Transform sibling in canvasRoot)
            {
                string lowerName = sibling.name.ToLower();

                // Keep Background, Image, and YouLost
                if (sibling == YouLostUI ||
                    lowerName.Contains("background") ||
                    lowerName.Contains("image"))
                {
                    sibling.gameObject.SetActive(true);
                }
                else
                {
                    sibling.gameObject.SetActive(false);
                }
            }

            // 2️⃣ Enable all children inside YouLost
            YouLostUI.SetActive(true);
            foreach (Transform child in YouLostUI.transform)
            {
                child.gameObject.SetActive(true);
            }

            Debug.Log("💀 You Lost screen displayed (kept background & image only, everything else disabled).");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error showing YouLostUI: {ex.Message}");
        }
    }

}
