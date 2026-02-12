using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class FuseButtonHoverPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform fuseButtonRect;

    public void OnPointerEnter(PointerEventData eventData)
    {
        var selectedCards = FusionGameManager.Instance.handManager
            .GetCurrentHand()
            .Where(c => c != null && c.IsSelected())
            .ToList();

        if (selectedCards.Count != 2) return;

        var result = FusionEvaluator.Evaluate(selectedCards[0].data, selectedCards[1].data);

        string fusionText = result.Type.ToString();
        string fpsText = $"+{result.BaseFPS} FPS";

        UIManager.Instance.ShowFloatingText(fusionText, Color.cyan, fuseButtonRect, offsetY: 330f, duration: 3f);
        UIManager.Instance.ShowFloatingText(fpsText, Color.yellow, fuseButtonRect, offsetY: 270f, duration: 3f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // No-op: Text will fade naturally
    }
}
