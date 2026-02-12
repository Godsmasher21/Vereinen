using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class TrialManager : MonoBehaviour
{
    public static TrialManager Instance;

    [Header("Current Trial State")]
    public bool trialActive;
    private int playsUsed = 0;

    public bool CanPlayAnotherHand() => playsUsed < currentTrialDefinition.PlayLimit;

    [Header("Dependencies")]
    public HandManager handManager;
    public Button playButton;

    private TrialDefinition currentTrialDefinition;
    private List<CardData> playedCards = new List<CardData>();
    private List<int> playedFPSValues = new List<int>();

    public int totalFPS = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (playButton != null)
        {
            playButton.onClick.AddListener(TryPlayFusedResults);
        }
    }

    private void Update()
    {
        if (playButton != null)
            playButton.gameObject.SetActive(trialActive);

        if (trialActive && currentTrialDefinition != null)
        {
            UIManager.Instance.UpdateTrialUI(currentTrialDefinition, GetTotalFusionPower());
        }
    }

    public void StartTrial(TrialDefinition definition)
    {
        trialActive = true;
        currentTrialDefinition = definition;
        playsUsed = 0;
        totalFPS = 0;
        playedCards.Clear();
        UIManager.Instance.UpdatePlayedHands(currentTrialDefinition.PlayLimit - playsUsed);

        Debug.Log($"🧪 [Trial Start] {definition.Name}");
        FusionGameManager.Instance.playerDeck.ResetAvailableCards();
        handManager.usePlayerDeckForTrial = true;
        handManager.StartNewRound(isTrial: true);
        UIManager.Instance.ShowPlayedFPS(0);
    }

    public void TryPlayFusedResults()
    {
        if (!trialActive)
        {
            Debug.LogWarning("Trial is not active.");
            return;
        }

        var selectedCards = FusionGameManager.Instance.handManager.GetCurrentHand()
            .FindAll(c => c != null && c.IsSelected());

        if (selectedCards.Count == 0)
        {
            Debug.LogWarning("No cards selected.");
            if (!CheckForEmptyHandFail())
                CompleteTrial(false);
            return; // ✅ prevent coroutine start after losing
        }

        playsUsed++;
        Debug.Log($"▶️ Attempting Play {playsUsed}/{currentTrialDefinition.PlayLimit}");
        UIManager.Instance.UpdatePlayedHands(currentTrialDefinition.PlayLimit - playsUsed);

        StartCoroutine(ProcessCardsSequentially(selectedCards));
    }

    private IEnumerator ProcessCardsSequentially(List<Card> selectedCards)
    {   
        // Move cards to played area
        handManager.MoveCardsToPlayedArea(selectedCards);

        foreach (var card in selectedCards)
        {
            if (card.data != null && card.data.isFused)
            {
                int oldFPS = card.data.baseFPS;
                int bonusFPS = ApplySuitPowers(card.data);
                card.data.baseFPS += bonusFPS;
                playedCards.Add(card.data);

                Debug.Log($"[Trial] Played Fused Card ID: {card.data.cardID}, Type: {card.data.fusionType}, BaseFPS before: {oldFPS}, Bonus: {bonusFPS}, Final: {card.data.baseFPS}");

                // Record and display combo-style cumulative text
                playedFPSValues.Add(card.data.baseFPS);

                string comboText = "";
                int total = 0;
                for (int i = 0; i < playedFPSValues.Count; i++)
                {
                    total += playedFPSValues[i];
                    comboText += playedFPSValues[i].ToString();
                    if (i < playedFPSValues.Count - 1)
                        comboText += "+";
                }

                if (playedFPSValues.Count > 1)
                    comboText += "=" + total;

                // Show using the same existing floating text logic
                UIManager.Instance.ShowFloatingText("+" + comboText, Color.yellow, UIManager.Instance.playedFPSText.GetComponent<RectTransform>(), offsetY: -50f, duration: 1.2f);

                yield return new WaitForSeconds(0.3f); // Delay between each card
            }
            else
            {
                Debug.LogWarning($"Card {card.data?.cardID} is not a valid Fused Card.");
            }
        }

        // Update total FPS
        totalFPS += selectedCards.Sum(card => card.data.baseFPS);
        playedFPSValues.Clear();
        UIManager.Instance.ShowPlayedFPS(totalFPS);
        UIManager.Instance.ShowFloatingText($"+{selectedCards.Sum(card => card.data.baseFPS)}", Color.green, UIManager.Instance.playedFPSText.GetComponent<RectTransform>());

        bool passed = ValidateTrial();
        StartCoroutine(HandlePostPlayResult(passed, selectedCards));
    }

    public bool ValidateTrial()
    {
        Debug.Log($"✅ [Validating] {currentTrialDefinition.Name}");

        int totalFPS = GetTotalFusionPower();

        bool suitsSatisfied = !currentTrialDefinition.RequiredSuits.Any() ||
            currentTrialDefinition.RequiredSuits.All(rs => playedCards.Any(c => c.suit == rs));

        bool typesSatisfied = !currentTrialDefinition.RequiredTypes.Any() ||
            currentTrialDefinition.RequiredTypes.All(rt => playedCards.Any(c => FusionTypeMatchesWithHierarchy(c.fusionType, rt)));

        bool passed = totalFPS >= currentTrialDefinition.TargetFPS && suitsSatisfied && typesSatisfied;

        Debug.Log($"Result -> TotalFPS: {totalFPS}, SuitsOK: {suitsSatisfied}, TypesOK: {typesSatisfied}, Pass: {passed}");
        return passed;
    }

    public void CompleteTrial(bool passed)
    {
        trialActive = false;

        if (passed)
        {
            int reward = CalculateReward();
            FusionGameManager.Instance.AddMoney(reward);
            Debug.Log($"💰 [Trial Success] {currentTrialDefinition.Name}. Earned ${reward}");
            FusionGameManager.Instance.ResetFusionCost();
        }
        else
        {
            Debug.LogWarning($"⚠️ [Trial Failed] No reward.");
            UIManager.Instance.ShowYouLostMessage();
        }

        GameOrchestrator.Instance.DestroyHand(FusionGameManager.Instance.handManager.GetCurrentHand());
        GameOrchestrator.Instance.currentPhase = GamePhase.Home;
        GameOrchestrator.Instance.CompleteTrial(passed);

        currentTrialDefinition = null;
        totalFPS = 0;
        playedCards.Clear();
    }

    private IEnumerator HandlePostPlayResult(bool passed, List<Card> selectedCards)
    {
        yield return new WaitForSeconds(1f);

        if (passed)
        {
            CompleteTrial(true);
            yield break;
        }

        if (playsUsed < currentTrialDefinition.PlayLimit)
        {
            if (!CheckForEmptyHandFail())
            {
                FusionGameManager.Instance.handManager.RealignAndDrawReplacements(selectedCards);
            }
            else
            {
                CompleteTrial(false);
                yield break; // ✅ Stop the coroutine right here
            }
        }
        else
        {
            CompleteTrial(false);
            yield break; // ✅ Stop coroutine cleanly
        }
    }

    private int ApplySuitPowers(CardData card)
    {
        int bonusFPS = 0;

        switch (card.suit)
        {
            case CardData.Suit.Hearts:
                if (ValidateTrial())
                {
                    int bonusMoney = 1;
                    FusionGameManager.Instance.AddMoney(bonusMoney);
                    Debug.Log($"❤️ Lifeline activated: +${bonusMoney} money.");
                }

                int extraHearts = FusionGameManager.Instance.handManager.GetCurrentHand()
                    .Count(c => c.data.suit == CardData.Suit.Hearts && !c.IsSelected());
                bonusFPS = extraHearts;
                break;

            case CardData.Suit.Spades:
                int remainingInHand = FusionGameManager.Instance.handManager.GetCurrentHand()
                    .Count(c => !c.IsSelected());
                bonusFPS = remainingInHand;
                break;

            case CardData.Suit.Diamonds:
                int diamondsPlayedSoFar = playedCards.Count(c => c.suit == CardData.Suit.Diamonds);
                bonusFPS = diamondsPlayedSoFar + 1; // +1 for this card
                break;

            case CardData.Suit.Clubs:
                GameOrchestrator.Instance.reduceNextTrial = 2;
                Debug.Log($"♣ Disruption activated: Next trial target FPS reduced by 2.");
                bonusFPS = 0;
                break;
        }

        return bonusFPS;
    }

    private int CalculateReward()
    {
        return Mathf.Clamp(currentTrialDefinition.TargetFPS / 4, 3, 10);
    }

    public void ForceFail()
    {
        trialActive = false;
        Debug.LogWarning($"❌ [Trial Force Failed]");
        FusionGameManager.Instance.StartShopPhase();
    }

    public int GetTotalFusionPower()
    {
        return playedCards.Sum(c => c.baseFPS);
    }

    private bool FusionTypeMatchesWithHierarchy(CardData.FusionType playedType, CardData.FusionType requiredType)
    {
        if (playedType == requiredType)
            return true;

        switch (playedType)
        {
            case CardData.FusionType.StraightRoyalFlush:
                return requiredType == CardData.FusionType.RoyalFlush
                    || requiredType == CardData.FusionType.StraightFlush
                    || requiredType == CardData.FusionType.Straight
                    || requiredType == CardData.FusionType.Flush
                    || requiredType == CardData.FusionType.FaceSequence
                    || requiredType == CardData.FusionType.FaceFusion
                    || requiredType == CardData.FusionType.Pair
                    || requiredType == CardData.FusionType.FacePair;

            case CardData.FusionType.RoyalFlush:
                return requiredType == CardData.FusionType.StraightFlush
                    || requiredType == CardData.FusionType.Straight
                    || requiredType == CardData.FusionType.Flush
                    || requiredType == CardData.FusionType.FaceSequence
                    || requiredType == CardData.FusionType.FaceFusion
                    || requiredType == CardData.FusionType.Pair
                    || requiredType == CardData.FusionType.FacePair;

            case CardData.FusionType.StraightFlush:
                return requiredType == CardData.FusionType.Straight
                    || requiredType == CardData.FusionType.Flush;

            case CardData.FusionType.FaceSequence:
                return requiredType == CardData.FusionType.Straight
                    || requiredType == CardData.FusionType.FaceFusion;

            case CardData.FusionType.FaceFusion:
                return requiredType == CardData.FusionType.Pair
                    || requiredType == CardData.FusionType.FacePair;

            case CardData.FusionType.FacePair:
                return requiredType == CardData.FusionType.Pair
                    || requiredType == CardData.FusionType.FaceFusion;

            default:
                return false;
        }
    }

    private bool CheckForEmptyHandFail()
    {
        var currentHand = FusionGameManager.Instance.handManager.GetCurrentHand();
        bool hasNoCardsInHand = currentHand == null || currentHand.Count == 0;

        if (hasNoCardsInHand)
        {
            Debug.LogWarning("💀 [Trial Failed] No cards left to play or draw.");

            // ✅ Trigger trial failure visuals
            CompleteTrial(false);
            return true;
        }

        return false;
    }
    
}
