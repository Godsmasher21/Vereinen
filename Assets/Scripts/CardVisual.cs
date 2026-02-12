using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CardVisual : MonoBehaviour
{
    public Image cardImage;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 followOffset = Vector3.zero;
    public float followSpeed = 15f;
    public float rotationAmount = 20f;
    public float rotationSpeed = 20f;
    [HideInInspector] public bool isSecondaryVisual = false;
    private Card parentCard;
    private Transform cardTransform;
    private Vector3 movementDelta;
    private Vector3 rotationDelta;
    [SerializeField] private Transform tilt;
    [SerializeField] private float tiltAmount3D = 20f;
    [SerializeField] private float tiltSpeed3D = 20f;
    private float tiltPhaseOffset3D;

    [Header("Scale Settings")]
    [SerializeField] private Vector3 dragScale = Vector3.one * 1.25f;
    private Vector3 hoverscale;

    private Vector3 originalScale;

    [Header("Shadow Offset Settings")]
    [SerializeField] private Transform shadow;
    [SerializeField] private float shadowDragOffset = 15f;

    private Vector3 originalShadowPos;

    [Header("Curve Settings")]
    [SerializeField] private CurveParameters curve;
    private float curveYOffset;
    private float curveRotationOffset;

    public void ReassignParent(Card newCard)
    {
        parentCard = newCard;
        cardTransform = newCard.transform;
    }

    public void SetFollowOffset(Vector3 offset)
    {
        followOffset = offset;
    }

    void Start()
    {
        if (cardImage != null)
            cardImage.raycastTarget = false;

        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.ignoreParentGroups = true;

        originalShadowPos = shadow.localPosition;
    }

    public void Initialize(Card card, Sprite[] spriteDeck)
    {
        parentCard = card;
        cardTransform = card.transform;
        originalScale = transform.localScale;
        dragScale = originalScale * 1.25f;
        hoverscale = originalScale * 1.15f;
        tiltPhaseOffset3D = Random.Range(0f, 10f);

        if (shadow != null)
        {
            originalShadowPos = shadow.localPosition;
        }

        if (cardImage != null && spriteDeck != null && card.CardID < spriteDeck.Length)
        {
            cardImage.sprite = spriteDeck[card.CardID];
        }

        if (cardImage != null)
        {
            cardImage.raycastTarget = false;
        }

        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.blocksRaycasts = false;
        }
        Debug.Log($"[CardVisual] Initializing {name} with CardID {parentCard.data?.cardID}");
    }

    private void Update()
    {
        if (!cardTransform) return;

        if (parentCard == null || parentCard.data == null) return;

        if (parentCard.cardHolder.allowHoverScale)
        {
            if (parentCard.isHovering)
                transform.localScale = hoverscale;
            else if (!parentCard.isHovering && !parentCard.isDragging)
                transform.localScale = originalScale;
        }

        if (parentCard != null && parentCard.cardHolder != null && transform.parent != null)
        {
            HandPositioning();
        }
        Follow();
        Tilt();
        ApplyTilt3D();
    }

    private void EnsureFanRotationCurve()
    {
        curve.rotation = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0f),
            new Keyframe(1f, -1f)
        );
    }

    private void HandPositioning()
    {   
        EnsureFanRotationCurve();
        curveYOffset = curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence * parentCard.SiblingAmount();
        curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());
    }

    private void Follow()
    {
        Vector3 verticalOffset = Vector3.up * (parentCard.isDragging ? 0 : curveYOffset);
        transform.position = Vector3.Lerp(transform.position, cardTransform.position + followOffset + verticalOffset , followSpeed * Time.deltaTime);
    }

    private void Tilt()
    {
        Vector3 movement = transform.position - cardTransform.position;
        movementDelta = Vector3.Lerp(movementDelta, movement, 25 * Time.deltaTime);
        Vector3 movementRotation = parentCard.isDragging ? movementDelta : movement;
        movementRotation *= rotationAmount;

        rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, cardTransform.eulerAngles.y, Mathf.Clamp(rotationDelta.x, -90f, 90f));
    }

    private void ApplyTilt3D()
    {
        if (tilt == null || parentCard == null) return;

        float xTilt = 0f;
        float yTilt = 0f;
        float zTilt = 0f;
        float time = Time.time + tiltPhaseOffset3D;
        float sin = Mathf.Sin(time) * 15f * (parentCard.isHovering ? 0.2f : 1f);
        float cos = Mathf.Cos(time) * 15f * (parentCard.isHovering ? 0.2f : 1f);

        Vector3 offset = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);

        xTilt = parentCard.isHovering ? (offset.y * -1 * tiltAmount3D) : 0f;
        yTilt = parentCard.isHovering ? (offset.x * 1 * tiltAmount3D) : 0f;
        zTilt = parentCard.isDragging ? tilt.eulerAngles.z : (curveRotationOffset * (curve.rotationInfluence * parentCard.SiblingAmount()));

        float lerpX = Mathf.LerpAngle(tilt.eulerAngles.x, xTilt + sin, tiltSpeed3D * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tilt.eulerAngles.y, yTilt + cos, tiltSpeed3D * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tilt.eulerAngles.z, zTilt, tiltSpeed3D / 2 * Time.deltaTime);

        tilt.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
        shadow.eulerAngles = new Vector3(0f, 0f, zTilt);
    }

    public void OnStartDragg()
    {
        transform.SetAsLastSibling(); // puts this visual at top of visual hierarchy
        Debug.Log($"[CardVisual] {name} brought to front");

        transform.localScale = dragScale;

        if (shadow != null)
        {
            shadow.localPosition += (-Vector3.up) * shadowDragOffset;
        }

    }

    public void OnEndDragg()
    {
        // Match sibling index with the card’s position in the holder
        if (parentCard != null)
        {
            transform.SetSiblingIndex(parentCard.transform.GetSiblingIndex());
        }

        transform.localScale = originalScale;
        
        if (shadow != null)
        {
            shadow.localPosition = originalShadowPos;
        }

    }

    public Sprite cardBackSprite; // assign in prefab or externally

    public IEnumerator AnimateToDeckAndDestroy(Vector3 destination, float duration = 0.3f)
    {
        float time = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 180, 0); // flip Y

        // halfway through, flip the sprite
        bool flipped = false;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            transform.position = Vector3.Lerp(startPos, destination, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (!flipped && t > 0.5f)
            {
                cardImage.sprite = cardBackSprite;
                flipped = true;
            }

            yield return null;
        }
        Destroy(gameObject);
    }

    public IEnumerator AnimateFromDeckWithFlip(Vector3 handTargetPos, Sprite frontSprite, float duration = 0.3f)
    {
        float time = 0f;
        Quaternion startRot = Quaternion.Euler(0, 180, 0); // start flipped (back)
        Quaternion targetRot = Quaternion.identity; // flat (0°) = front face

        cardImage.sprite = cardBackSprite;

        bool flipped = false;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (!flipped && t > 0.5f)
            {
                cardImage.sprite = frontSprite;
                flipped = true;
            }

            yield return null;
        }

        transform.rotation = targetRot;
    }

}
