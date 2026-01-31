using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PickupToastUI : MonoBehaviour
{
    public CanvasGroup group;
    public RectTransform rt;
    public Image icon;
    public Image glow;

    public float life = 0.8f;
    public float popScale = 1.25f;

    void Reset()
    {
        rt = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
    }

    public void Play(Sprite sprite, Vector2 screenPos, Canvas canvas)
    {
        if (!rt) rt = GetComponent<RectTransform>();
        if (!group) group = GetComponent<CanvasGroup>();

        icon.sprite = sprite;
        icon.enabled = (sprite != null);

        RectTransform canvasRt = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPos);

        rt.anchoredPosition = localPos;

        StopAllCoroutines();
        StartCoroutine(Anim());
    }

    IEnumerator Anim()
    {
        group.alpha = 1f;
        rt.localScale = Vector3.one * 0.85f;

        // pop£º0.85 -> popScale
        float t = 0f;
        float popTime = 0.12f;
        while (t < popTime)
        {
            t += Time.unscaledDeltaTime;
            float k = t / popTime;
            rt.localScale = Vector3.one * Mathf.Lerp(0.85f, popScale, k);
            if (glow) glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, Mathf.Lerp(0.35f, 0.0f, k));
            yield return null;
        }

        // settle£ºpopScale -> 1
        t = 0f;
        float settleTime = 0.12f;
        while (t < settleTime)
        {
            t += Time.unscaledDeltaTime;
            float k = t / settleTime;
            rt.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, k);
            yield return null;
        }

        // hold 
        yield return new WaitForSecondsRealtime(life * 0.5f);

        // fade out
        t = 0f;
        float fadeTime = life * 0.5f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = t / fadeTime;
            group.alpha = Mathf.Lerp(1f, 0f, k);
            yield return null;
        }

        Destroy(gameObject);
    }
}
