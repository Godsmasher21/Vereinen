using UnityEngine;
using UnityEngine.UI;

public class FusionGameManager : MonoBehaviour
{
    public static FusionGameManager Instance;

    [Header("Gameplay State")]
    public int fusionRoundCount = 1;

    [SerializeField] private int money = 8;
    [SerializeField] private int fusionCostBase = 2;
    public int Money => money;

    [Header("References")]
    public PlayerDeck playerDeck;
    public Deck baseDeck;
    public HorizontalCardHolder cardHolder;
    public HandManager handManager;
    public Button endFusionRoundButton;
    public Button FuseButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);
    }

    public void Start()
    {
        if (endFusionRoundButton != null)
            endFusionRoundButton.onClick.AddListener(() => EndFusionRound());

        if (FuseButton != null)
            FuseButton.onClick.AddListener(() => cardHolder.TryFuseSelectedCards());
    }

    public void Initialize()
    {
        money = 8;
        fusionRoundCount = 1;
    }

    // === PHASES ===

    public void StartFusionRound()
    {
        GameOrchestrator.Instance.currentPhase = GamePhase.FusionRound;
        endFusionRoundButton.gameObject.SetActive(true);

        if (!CanAffordFusion())
        {
            Debug.LogWarning("[Fusion] Not enough money.");
            return;
        }

        SpendForFusion();
        fusionRoundCount++;

        handManager.usePlayerDeckForTrial = false;
        handManager.StartNewRound(isTrial: false);

        Debug.Log($"[Fusion Round #{fusionRoundCount}] Cost: ${GetFusionCost()} | Remaining: ${money}");
    }

    public void EndFusionRound()
    {
        Debug.Log("✅ Ending Fusion Round...");

        var handCards = handManager.GetCurrentHand();
        int savedCount = 0;

        // Save card data to deck
        foreach (var card in handCards)
        {
            if (card == null) continue;
            if (card.data.isFused)
            {
                playerDeck.AddCard(card.data.Clone());
                savedCount++;
            }
        }

        GameOrchestrator.Instance.DestroyHand(handCards);
        GameOrchestrator.Instance.currentPhase = GamePhase.Home;
        UIManager.Instance.UpdateFusionCost(GetFusionCost());
        Debug.Log($"💾 Saved {savedCount} cards to player deck.");
        endFusionRoundButton.gameObject.SetActive(false);
    }

    public void StartShopPhase()
    {
        Debug.Log("[Shop Phase] Offer rewards here.");
        // Hook to reward UI later
    }

    public void StartBossTrial()
    {
        Debug.Log("[Boss Trial] Epic showdown.");
    }

    public void EndGame()
    {
        Debug.Log("🎮 Game Over. You survived the fusion trials.");
    }

    // === ECONOMY ===

    public void SetMoney(int amount)
    {
        money = amount;
        UIManager.Instance.UpdateMoney(money);
    }

    public int GetFusionCost()
    {
        return fusionCostBase * fusionRoundCount;
    }

    public bool CanAffordFusion()
    {
        return money >= GetFusionCost();
    }

    public void SpendForFusion()
    {
        money -= GetFusionCost();
        UIManager.Instance.UpdateMoney(money);
        UIManager.Instance.ShowFloatingText($"-${GetFusionCost()}", Color.red, UIManager.Instance.moneyText.GetComponent<RectTransform>());
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UIManager.Instance.UpdateMoney(money);
        UIManager.Instance.ShowFloatingText($"+${amount}", Color.yellow, UIManager.Instance.moneyText.GetComponent<RectTransform>());
        Debug.Log($"💰 Gained ${amount}. Total: ${money}");
    }

    public int GetMoney()
    {
        return money;
    }

    public void ResetFusionCost()
    {
        fusionRoundCount = 1;
        UIManager.Instance.UpdateFusionCost(GetFusionCost());
        Debug.Log("🔁 Fusion cost reset.");
    }

    public void ResetGameState()
    {
        money = 8;
        fusionRoundCount = 0;
        playerDeck.ClearDeck();
        handManager.ClearHand();
        handManager.ClearSlots();
        GameOrchestrator.Instance.currentPhase = GamePhase.Home;
        Debug.Log("🔄 Game state reset.");
    }

}
