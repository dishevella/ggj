using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipFollow : MonoBehaviour
{
    public Canvas canvas;
    public CanvasGroup group;
    public Text idText;
    public Image hintImage;

    public Vector2 offset = new Vector2(18f, -18f);

    RectTransform _rt;
    RectTransform _parentRt;   //  tooltip 的父物体
    Camera _uiCam;
    bool _visible;

    void Awake()
    {
        _rt = (RectTransform)transform;
        _parentRt = _rt.parent as RectTransform;

        // Overlay => camera = null
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
        if (idText) idText.text = id;

        if (hintImage)
        {
            hintImage.sprite = hint;
            hintImage.enabled = (hint != null);
        }

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
        if (_parentRt == null || _rt == null) return;

        // 1) 鼠标屏幕坐标 -> 父物体本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRt, Input.mousePosition, _uiCam, out Vector2 localPos);

        // 2) 加 offset（右下：x 正，y 负）
        Vector2 target = localPos + offset;

        // 3) Clamp：不让 tooltip 超出父物体范围
        target = ClampToParent(target);

        _rt.anchoredPosition = target;
    }

    Vector2 ClampToParent(Vector2 anchoredPos)
    {
        // 父物体矩形（本地坐标）
        Rect parentRect = _parentRt.rect;

        // tooltip 自己的尺寸
        Vector2 size = _rt.rect.size;

        // pivot 影响“左/右、上/下”需要留多少边距
        Vector2 pivot = _rt.pivot;

        // 允许的最小/最大位置（保证 tooltip 整个都在 parentRect 内）
        float minX = parentRect.xMin + size.x * pivot.x;
        float maxX = parentRect.xMax - size.x * (1f - pivot.x);

        float minY = parentRect.yMin + size.y * pivot.y;
        float maxY = parentRect.yMax - size.y * (1f - pivot.y);

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        return anchoredPos;
    }

}

