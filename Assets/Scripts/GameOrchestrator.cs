using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameOrchestrator : MonoBehaviour
{
    public static GameOrchestrator Instance;
    public TrialDefinition currentBossTrial;
    public TrialDefinition nextTrial;
    public List<TrialDefinition> allTrialDefinitions;
    public List<TrialDefinition> bossTrialDefinitions;

    [Header("Dependencies")]
    public GamePhase currentPhase = GamePhase.Home;
    public FusionGameManager fusionGameManager;
    public TrialManager trialManager;
    public Transform playerDeckVisualPoint;
    public UIManager UImanager;

    [Header("UI Buttons")]
    public Button startFusionButton;
    public Button startTrialButton;
    public Button resetRunButton;
    public Button startBossTrialButton;

    [Header("Trials")]
    public TrialDefinition firstSparkTrialDefinition;

    // 🟩 Ante Logic
    public int currentAnte = 0;

    // 🟩 Balance Parameters
    public int trialsPerAnte = 2;
    public int baseTrialFPS = 15;
    public int trialScale = 3;

    public int baseBossFPS = 25;
    public int bossScale = 5;

    public bool isBoss = false;

    private int trialsCompletedThisAnte = 0;

    public int reduceNextTrial = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ Only auto-start the first time the orchestrator is created
        if (currentAnte == 0 || currentPhase == GamePhase.Home)
        {
            BeginGame();
        }
    }

    private void Start()
    {
        WireButtons();
    }

    private float GetAnteMultiplier(int ante)
    {
        switch (ante)
        {
            case 1: return 1.0f;
            case 2: return 1.2f;
            case 3: return 1.5f;
            case 4: return 1.8f;
            default: return (ante / 2);
        }
    }

    private void WireButtons()
    {
        if (startFusionButton != null)
            startFusionButton.onClick.AddListener(() => fusionGameManager.StartFusionRound());

        if (startTrialButton != null)
            startTrialButton.onClick.AddListener(() => BeginTrialPhase());

        if (startBossTrialButton != null)
            startBossTrialButton.onClick.AddListener(() => BeginBossTrial());

        if (resetRunButton != null)
            resetRunButton.onClick.AddListener(() => ResetRun());
    }

    public TrialDefinition GetRandomTrial(int ante)
    {
        TrialDefinition chosen = allTrialDefinitions[Random.Range(0, allTrialDefinitions.Count)];
        float multiplier = GetAnteMultiplier(ante);

        // Create a cloned instance so we don't overwrite the original asset
        TrialDefinition clonedTrial = ScriptableObject.Instantiate(chosen);
        clonedTrial.TargetFPS = Mathf.RoundToInt(clonedTrial.TargetFPS * multiplier) - reduceNextTrial;
        reduceNextTrial = 0;
        return clonedTrial;
    }

    public TrialDefinition GetRandomBossTrial(int ante)
    {
        TrialDefinition chosen = bossTrialDefinitions[Random.Range(0, bossTrialDefinitions.Count)];
        float multiplier = GetAnteMultiplier(ante);

        TrialDefinition clonedTrial = ScriptableObject.Instantiate(chosen);
        clonedTrial.TargetFPS = Mathf.RoundToInt(clonedTrial.TargetFPS * multiplier) - reduceNextTrial;
        reduceNextTrial = 0;
        return clonedTrial;
    }

    public void BeginGame()
    {
        Debug.Log("🎬 Game Started!");
        currentAnte = 1;
        trialsCompletedThisAnte = 0;

        fusionGameManager.ResetGameState();
        fusionGameManager.SetMoney(8);
        fusionGameManager.ResetFusionCost();
        fusionGameManager.playerDeck.ClearDeck();
        currentBossTrial = GetRandomBossTrial(currentAnte);
        nextTrial = GetRandomTrial(currentAnte);
        UIManager.Instance.UpdateFusionCost(fusionGameManager.GetFusionCost());
        UIManager.Instance.UpdateAnte(currentAnte);
        UImanager.ShowBossTrialInfo(currentBossTrial);
        UImanager.ShowTrialInfo(nextTrial);
        currentPhase = GamePhase.Home;
        UImanager.ShowUI(currentPhase);
    }

    public void ResetRun()
    {
        Debug.Log("🔁 Restarting Run");
        DestroyAllCardVisuals();
        fusionGameManager.ResetGameState();
        UIManager.Instance.RestoreAllUIAfterReset();
        BeginGame();
    }

    public void BeginTrialPhase()
    {
        Debug.Log($"⚔️ Starting Trial: Ante {currentAnte}, Trial {trialsCompletedThisAnte + 1}");
        currentPhase = GamePhase.TrialRound;

        trialManager.StartTrial(nextTrial);
    }

    public void BeginBossTrial()
    {
        isBoss = true;
        Debug.Log($"👑 Starting Boss Trial: Ante {currentAnte}");
        currentPhase = GamePhase.TrialRound;

        trialManager.StartTrial(currentBossTrial);
    }

    public void CompleteTrial(bool passed)
    {
        if (!passed)
        {
            Debug.Log($"❌ Ante {currentAnte} failed. Game Over.");
            fusionGameManager.EndGame();
            UImanager.ShowYouLostMessage();
            return;
        }

        if (isBoss)
        {
            Debug.Log($"✅ Ante {currentAnte} Boss Completed!");
            AdvanceAnte();
            isBoss = false;
        }
        else
        {
            trialsCompletedThisAnte++;
            if (trialsCompletedThisAnte < trialsPerAnte)
            {
                Debug.Log($"✅ Trial {trialsCompletedThisAnte} Complete. Returning to Home for Fusion/Shop.");
                currentPhase = GamePhase.Home;
                nextTrial = GetRandomTrial(currentAnte);
                UImanager.ShowUI(currentPhase);
                UImanager.ShowTrialInfo(nextTrial);
            }
            else
            {
                isBoss = true;
                currentPhase = GamePhase.Home;
                nextTrial = GetRandomTrial(currentAnte);
                UImanager.ShowUI(currentPhase);
                UImanager.ShowTrialInfo(nextTrial);
            }
        }
    }

    private void AdvanceAnte()
    {
        currentAnte++;
        trialsCompletedThisAnte = 0;
        currentBossTrial = GetRandomBossTrial(currentAnte);
        nextTrial = GetRandomTrial(currentAnte);
        UIManager.Instance.UpdateAnte(currentAnte);
        // You can set an end condition here
        if (currentAnte > 4)
        {
            Debug.Log($"🏁 Final Ante Complete. Game Won!");
            // Trigger win screen
        }
        else
        {
            Debug.Log($"👑 Advancing to Ante {currentAnte}. Returning Home.");
            currentPhase = GamePhase.Home;
            UImanager.ShowUI(currentPhase);
        }
        UImanager.ShowTrialInfo(nextTrial);
        UImanager.ShowBossTrialInfo(currentBossTrial);

    }

    public void DestroyHand(List<Card> handCards)
    {
        StartCoroutine(CascadeDestroyRoutine(handCards, playerDeckVisualPoint));
    }

    private IEnumerator CascadeDestroyRoutine(List<Card> cards, Transform deckTarget)
    {
        float delay = Random.Range(0.06f, 0.1f);

        foreach (var card in cards)
        {
            if (card == null) continue;

            if (card.cardVisual != null)
                StartCoroutine(card.cardVisual.AnimateToDeckAndDestroy(deckTarget.position));

            if (card.secondaryVisual != null)
                StartCoroutine(card.secondaryVisual.AnimateToDeckAndDestroy(deckTarget.position));

            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(Random.Range(0.12f, 0.5f));
        fusionGameManager.handManager.ClearSlots();
        fusionGameManager.handManager.ClearHandData();
    }

    private void Update()
    {
        UImanager.ShowUI(currentPhase);
        UImanager.UpdateDeckCount();

        // 🔁 Quick restart shortcut
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("🔄 R pressed — restarting run!");
            ResetRun();
        }
    }

    private void DestroyAllCardVisuals()
    {
        // Find and destroy all Cards in the scene
        var allCards = FindObjectsOfType<Card>();
        foreach (var card in allCards)
        {
            if (card.cardVisual != null)
                Destroy(card.cardVisual.gameObject);

            if (card.secondaryVisual != null)
                Destroy(card.secondaryVisual.gameObject);

            Destroy(card.gameObject);
        }

        Debug.Log($"🧹 Cleared {allCards.Length} card objects from scene.");
    }
}
