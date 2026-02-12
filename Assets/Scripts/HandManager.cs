using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class HandManager : MonoBehaviour
{
    [Header("Prefabs & Data")]
    public GameObject slotPrefab;
    public GameObject cardPrefab;
    public GameObject cardVisualPrefab;
    public Deck cardDeck;
    public PlayerDeck playerDeck;
    public bool usePlayerDeckForTrial = false;

    [Header("Scene References")]
    public Transform playerHandArea;
    public Transform playedHandArea;
    public Transform cardVisualHandler;
    public HorizontalCardHolder cardHolder;
    public Transform fusionDeckAnchor;
    public Transform playerDeckAnchor;

    [Header("Gameplay Settings")]
    public int startingHandSize = 10;
    private List<Card> currentHand = new List<Card>();
    private List<Transform> slotList = new List<Transform>();

    private void Start()
    {
        // GameOrchestrator handles round start
    }

    public void StartNewRound(bool isTrial = false)
    {
        ClearHand();
        ClearSlots();

        if (isTrial && usePlayerDeckForTrial)
        {
            List<CardData> trialDraw = playerDeck.GetRandomDraw(5)
                .ConvertAll(c => c.Clone()); // ensure deep copies

            StartCoroutine(SpawnHandWithDelay(trialDraw, true));

        }
        else
        {
            cardDeck.ShuffleDeck();

            List<CardData> newCards = new List<CardData>();
            for (int i = 0; i < startingHandSize; i++)
            {
                int id = cardDeck.DrawCardID();
                if (id >= 0)
                {
                    newCards.Add(new CardData
                    {
                        cardID = id,
                        isFused = false
                    });
                }
            }

            StartCoroutine(SpawnHandWithDelay(newCards, false));
        }
    }

    private IEnumerator SpawnHandWithDelay(List<CardData> cardsToSpawn, bool isTrial)
    {
        for (int i = 0; i < cardsToSpawn.Count; i++)
        {
            CreateCardWithSlot(i, cardsToSpawn[i], isTrial);
            yield return new WaitForSeconds(Random.Range(0.02f, 0.1f));
        }

        cardHolder.slots = slotList;
        cardHolder.UpdateLayout();
    }

    public void CreateCardWithSlot(int index, CardData data, bool isTrial = false)
    {
        slotList.RemoveAll(s => s == null);

        // 1️⃣ Try to find an existing empty slot
        Transform targetSlot = null;

        foreach (Transform slot in slotList) {
            if (slot.childCount == 0) {
                targetSlot = slot;
                break;
            }
        }

        // 2️⃣ If no empty slot, create a new one
        if (targetSlot == null) {
            GameObject slotObj = Instantiate(slotPrefab, playerHandArea);
            slotObj.name = $"Slot_{index}";
            slotList.Add(slotObj.transform);
            slotObj.tag = "Slot";
            targetSlot = slotObj.transform;
        }

        // 3️⃣ Create and initialize the card
        GameObject cardObj = Instantiate(cardPrefab, targetSlot);
        cardObj.name = $"Card_{index}";

        Card card = cardObj.GetComponent<Card>();    
        card.cardHolder = cardHolder;
        card.cardVisualPrefab = cardVisualPrefab;
        card.SetCardData(data);
        card.InitializeVisual(cardDeck.cardSprites, cardVisualHandler);

        currentHand.Add(card);
        cardHolder.AddCard(card);

        // 4️⃣ Visual animation
        if (card.cardVisual != null) {
            Transform from = isTrial ? playerDeckAnchor : fusionDeckAnchor;
            card.cardVisual.transform.position = from.position;
            card.cardVisual.transform.rotation = Quaternion.Euler(0, 180, 0);

            Sprite frontSprite = cardDeck.GetCardSprite(data.cardID);
            StartCoroutine(card.cardVisual.AnimateFromDeckWithFlip(card.transform.position, frontSprite));
        }
    }

    public void DrawNewCard()
    {
        int id = cardDeck.DrawCardID();
        if (id < 0)
        {
            Debug.LogWarning("Deck is empty.");
            return;
        }

        CardData newCard = new CardData
        {
            cardID = id,
            isFused = false
        };

        CreateCardWithSlot(slotList.Count, newCard);
        cardHolder.slots = slotList;
        cardHolder.UpdateLayout();
    }

    public void ClearHand()
    {
        foreach (Card card in currentHand)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        currentHand.Clear();
        cardHolder.ClearCards();
    }

    public void ClearSlots()
    {
        foreach (Transform slot in slotList)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        slotList.Clear();
    }

    public void TriggerFusionRound()
    {
        if (!FusionGameManager.Instance.CanAffordFusion())
        {
            Debug.LogWarning("Not enough money!");
            return;
        }

        FusionGameManager.Instance.SpendForFusion();
        StartNewRound(false);
        Debug.Log($"[Fusion] Round #{FusionGameManager.Instance.fusionRoundCount} | ${FusionGameManager.Instance.GetMoney()} left");
    }

    public List<Card> GetSelectedCards()
    {
        return currentHand.FindAll(c => c != null && c.IsSelected());
    }

    public void MoveCardsToPlayedArea(List<Card> selectedCards)
    {
        StartCoroutine(MoveCardsSequentially(selectedCards));
    }

    private IEnumerator MoveCardsSequentially(List<Card> selectedCards)
    {
        if (playedHandArea == null)
        {
            Debug.LogWarning("⚠️ PlayedHandArea not assigned!");
            yield break;
        }

        // Ensure valid list
        List<Card> validCards = new List<Card>(selectedCards.Where(c => c != null));

        for (int i = 0; i < validCards.Count; i++)
        {
            Card card = validCards[i];

            // Safety null checks
            if (card == null) continue;

            // Deselect to visually indicate it’s used
            card.Deselect();

            // Re-parent the card transform (the visual will follow automatically)
            card.transform.SetParent(playedHandArea, true);

            // Slight offset stacking (so they don’t all overlap)
            Vector3 localOffset = new Vector3(i * 0.3f, i * -0.3f, 0f);
            card.transform.localPosition = localOffset;

            // Reset rotation for cleanliness
            card.transform.localRotation = Quaternion.identity;

            // Give the visuals time to “catch up” smoothly
            yield return new WaitForSeconds(0.25f);
        }

        Debug.Log($"🎴 Moved {validCards.Count} cards to PlayedHandArea (visuals following smoothly).");
    }

    public void RealignAndDrawReplacements(List<Card> selectedCards)
    {
        int drawCount = selectedCards.Count;

        // Destroy selected cards
        foreach (var card in selectedCards)
        {
            if (card != null)
            {
                Destroy(card.transform.parent.gameObject);
                Destroy(card.gameObject);
                currentHand.Remove(card);
                if (card.cardVisual != null)
                    StartCoroutine(card.cardVisual.AnimateToDeckAndDestroy(GameOrchestrator.Instance.playerDeckVisualPoint.position));

                if (card.secondaryVisual != null)
                    StartCoroutine(card.secondaryVisual.AnimateToDeckAndDestroy(GameOrchestrator.Instance.playerDeckVisualPoint.position));
            }
        }

        // Get replacement cards from the player deck
        var replacementData = playerDeck.DrawFromAvailable(drawCount);

        // Realign slots
        RefreshHand();

        // Spawn new cards
        StartCoroutine(SpawnReplacementsWithDelay(replacementData));
    }

    private IEnumerator SpawnReplacementsWithDelay(List<CardData> replacementData)
    {
        foreach (var data in replacementData) {
            CreateCardWithSlot(slotList.Count, data, true);
            yield return new WaitForSeconds(0.1f);
        }

        cardHolder.slots = slotList;
        cardHolder.UpdateLayout();
    }

    public void RefreshHand()
    {
        currentHand.RemoveAll(c => c == null);
        cardHolder.ClearCards();
        foreach (var card in currentHand)
            cardHolder.AddCard(card);
    }

    public List<Card> GetCurrentHand()
    {
        return new List<Card>(currentHand);
    }

    public void ClearHandData()
    {
        currentHand.Clear();
        cardHolder.ClearCards();
    }
}
