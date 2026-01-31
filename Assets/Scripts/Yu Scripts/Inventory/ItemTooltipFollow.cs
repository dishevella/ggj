using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipFollow : MonoBehaviour
{
    public Canvas canvas;          
    public CanvasGroup group;
    public Text idText;
    public Image hintImage;

    public Vector2 offset = new(18f, -18f); 
    RectTransform _rt;
    RectTransform _parentRt;
    RectTransform _canvasRt;
    Camera _uiCam;

    bool _visible;

    void Awake()
    {
        _rt = (RectTransform)transform;

        // 关键：anchoredPosition 是相对父物体的，所以用父物体Rect做坐标转换
        _parentRt = _rt.parent as RectTransform;

        _canvasRt = (RectTransform)canvas.transform;
        _uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Hide();
    }

    void Update()
    {
        if (!_visible) return;
        FollowMouse();
    }

    public void Show(string id, Sprite hint)
    {
        idText.text = id;

        hintImage.sprite = hint;
        hintImage.enabled = (hint != null);

        group.alpha = 1;
        group.blocksRaycasts = false;
        group.interactable = false;

        _visible = true;
        FollowMouse();
    }

    public void Hide()
    {
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;
        _visible = false;
    }

    void FollowMouse()
    {
        // 用父物体的RectTransform算localPos（坐标系对齐）
        var bounds = _parentRt != null ? _parentRt : _canvasRt;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            bounds, Input.mousePosition, _uiCam, out Vector2 localPos);

        Vector2 pos = localPos + offset;
        pos = ClampToBounds(pos, bounds);

        _rt.anchoredPosition = pos;
    }

    Vector2 ClampToBounds(Vector2 pos, RectTransform bounds)
    {
        Rect r = bounds.rect;
        float w = _rt.rect.width;
        float h = _rt.rect.height;

        // Pivot 通用：计算四个方向相对 pivot 的外扩
        Vector2 pv = _rt.pivot;
        float left = w * pv.x;
        float right = w * (1f - pv.x);
        float bottom = h * pv.y;
        float top = h * (1f - pv.y);

        pos.x = Mathf.Clamp(pos.x, r.xMin + left, r.xMax - right);
        pos.y = Mathf.Clamp(pos.y, r.yMin + bottom, r.yMax - top);

        return pos;
    }
}

