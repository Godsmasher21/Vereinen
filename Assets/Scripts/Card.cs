using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class Card : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public HorizontalCardHolder cardHolder;

    [Header("Card Setup")]
    public GameObject cardVisualPrefab;
    [HideInInspector] public CardVisual cardVisual;
    [HideInInspector] public CardVisual secondaryVisual;
    [HideInInspector] public Transform originalParent;

    [Header("Runtime States")]
    public bool isDragging = false;
    public bool isHovering = false;
    public bool wasDragged = false;

    private bool isSelected = false;
    private Vector3 baseLocalPosition;

    [Header("Events")]
    public UnityEvent<Card> BeginDragEvent = new UnityEvent<Card>();
    public UnityEvent<Card> EndDragEvent = new UnityEvent<Card>();
    public UnityEvent<Card> PointerEnterEvent = new UnityEvent<Card>();
    public UnityEvent<Card> PointerExitEvent = new UnityEvent<Card>();

    [Header("Selection")]
    [SerializeField] private float selectionOffset = 50f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    // ✅ CardData Object
    [SerializeField]
    public CardData data;

    public void SetCardData(CardData newData)
    {
        data = newData.Clone(); // ✅ always clone internally

        if (data.isFused)
            SetFusionData(data.fusedID1, data.fusedID2, data.fusionType, data.suit);
    }
    
    // Instead of public:
    private int cardID;
    public int CardID => cardID; // read-only accessor
    public bool IsFused => data != null && data.isFused;
    public int FusedID1 => data?.fusedID1 ?? -1;
    public int FusedID2 => data?.fusedID2 ?? -1;
    public int basefps => data?.baseFPS ?? 0;
    public string Suit => data != null ? data.suit.ToString() : "";
    public string FusionType => data != null ? data.fusionType.ToString() : ""; 
    public FusionResult fusionResult; // nullable FusionResult

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void InitializeVisual(Sprite[] spriteDeck, Transform visualParent)
    {
        if (cardVisualPrefab == null || data == null)
        {
            Debug.LogError("Missing CardVisual prefab or CardData!");
            return;
        }

        if (IsFused == false)
        {
            GameObject visualObj = Instantiate(cardVisualPrefab, visualParent);
            cardVisual = visualObj.GetComponent<CardVisual>();
            visualObj.name = $"CardVisual_{cardID}";
            cardVisual.Initialize(this, spriteDeck);
        }
        // Fusion overlay
        if (IsFused && FusedID2 >= 0)
        {
            // 🔷 Primary Visual (FusedID1)
            GameObject visualObj = Instantiate(cardVisualPrefab, visualParent);
            visualObj.name = $"CardVisual_{FusedID1}";
            cardVisual = visualObj.GetComponent<CardVisual>();
            cardVisual.Initialize(this, spriteDeck);

            if (spriteDeck != null && FusedID1 < spriteDeck.Length)
                cardVisual.cardImage.sprite = spriteDeck[FusedID1]; // ✅ Explicitly set

            // 🔶 Secondary Visual (FusedID2)
            GameObject secondaryObj = Instantiate(cardVisualPrefab, visualParent);
            secondaryObj.name = $"CardVisual_Fused_{FusedID2}";
            secondaryVisual = secondaryObj.GetComponent<CardVisual>();
            secondaryVisual.Initialize(this, spriteDeck);

            if (spriteDeck != null && FusedID2 < spriteDeck.Length)
                secondaryVisual.cardImage.sprite = spriteDeck[FusedID2]; // ✅ Explicitly set

            secondaryVisual.SetFollowOffset(new Vector3(-0.5f, 0f, 0f));
            secondaryVisual.isSecondaryVisual = true;
        }
        Debug.Log($"[FusionVisuals] Primary = {cardID}, Secondary = {FusedID1}, FusionType = {FusionType}");
    }

    public void SetFusionData(int id1, int id2, CardData.FusionType type, CardData.Suit Suit)
    {
        if (data == null)
            data = new CardData();

        data.isFused = true;
        data.fusedID1 = id1;
        data.fusedID2 = id2;
        data.fusionType = type;
        data.suit = Suit;
    }

    public bool IsSelected() => isSelected;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[BeginDrag] {name} from {transform.parent.name}");

        if (!canvasGroup.blocksRaycasts || !canvasGroup.interactable)
        {
            Debug.LogWarning($"{name} not interactive, forcing reset.");
            ForceReset();
            return;
        }

        cardHolder.allowHoverScale = false;
        isDragging = true;
        if (isSelected)
        {
            isSelected = false;
            transform.localPosition = baseLocalPosition;
        }

        originalParent = transform.parent;
        transform.SetParent(originalParent.parent);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;

        cardVisual?.OnStartDragg();
        cardHolder.currentlyDragging = this;

        BeginDragEvent.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 globalMousePos))
        {
            rectTransform.position = globalMousePos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[EndDrag] {name} to {originalParent?.name}");

        cardHolder.allowHoverScale = true;
        isDragging = false;

        transform.SetParent(originalParent);
        rectTransform.localPosition = Vector3.zero;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;

        cardVisual?.OnEndDragg();
        cardHolder.currentlyDragging = null;
        cardHolder.ReorderCardVisuals();

        EndDragEvent.Invoke(this);
        wasDragged = true;
    }

    public void ForceReset()
    {
        transform.SetParent(originalParent);
        rectTransform.localPosition = Vector3.zero;
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 1f;

        isDragging = false;
        wasDragged = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;

        isSelected = !isSelected;
        Debug.Log($"[Selection] {name} is now {(isSelected ? "Selected" : "Deselected")}");

        if (isSelected)
        {
            baseLocalPosition = transform.localPosition;
            transform.localPosition = baseLocalPosition + new Vector3(0, selectionOffset, 0);
        }
        else
        {
            transform.localPosition = baseLocalPosition;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        PointerEnterEvent.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        PointerExitEvent.Invoke(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[PointerDown] {name}");
    }

    public int IndexInHolder() => transform.parent.GetSiblingIndex();

    public int SiblingAmount()
    {
        if (data == null || transform.parent == null || transform.parent.parent == null) return 0;  
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    public float NormalizedPosition()
    {   
        if (data == null || transform.parent == null || transform.parent.parent == null) return 0f;
        if ((transform.parent.parent.childCount - 1) <= 0) return 0f;
        return transform.parent.CompareTag("Slot") ? ExtensionMethods.Remap((float)ParentIndex(), 0, (float)(transform.parent.parent.childCount - 1), 0, 1) : 0;
    }

    public void Deselect()
    {
        if (!isSelected) return;
        isSelected = false;
        transform.localPosition = baseLocalPosition;
        Debug.Log($"[Deselection] {name} automatically deselected after fusion");
    }
}
