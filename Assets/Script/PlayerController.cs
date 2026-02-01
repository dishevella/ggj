using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;     // units/sec
    public bool canMove = true;

    [Header("Anim")]
    public Animator animator;
    public string walkingBoolName = "isWalking";
    private bool _isWalking;

    [Header("Facing")]
    public Transform visual;          // 推荐拖 Visual 子物体；不拖则默认使用自己 transform
    public bool isRight = true;       // 默认朝右
    public float flipLerpSpeed = 18f; // 翻转平滑速度（10~25 常用）
    public float faceDeadZone = 0.01f;// 输入死区，避免误触/摇杆抖动

    // 内部：当前用于平滑的目标
    private float _targetScaleX;

    void Awake()
    {
        // 初始化目标 scaleX：按当前 visual 的 scale 作为基准
        Transform t = (visual != null) ? visual : transform;
        float absX = Mathf.Abs(t.localScale.x);
        _targetScaleX = isRight ? absX : -absX;
    }

    void Update()
    {
        float x = canMove ? Input.GetAxisRaw("Horizontal") : 0f;

        // 1) 移动
        Vector3 pos = transform.position;
        if (x != 0f)
            pos.x += x * moveSpeed * Time.deltaTime;
        transform.position = pos;

        // 2) Animator：只用 isWalking（稳）
        bool wantWalk = canMove && Mathf.Abs(x) > 0.01f;
        if (_isWalking != wantWalk)
        {
            _isWalking = wantWalk;
            if (animator != null)
                animator.SetBool(walkingBoolName, _isWalking);
        }

        // 3) 朝向：只有输入明确时才更新 isRight
        UpdateFacing(x);

        // 4) 平滑把 scale.x 推向目标
        ApplyFlipSmoothing();
    }

    void UpdateFacing(float inputX)
    {
        if (!canMove) return;
        if (Mathf.Abs(inputX) <= faceDeadZone) return;

        bool wantRight = inputX > 0f;

        // 方向相同 -> 不变；方向不同 -> 切换
        if (wantRight == isRight) return;

        isRight = wantRight;

        // ⭐ 目标不是 ±1，而是“当前缩放幅度 × 方向符号”
        Transform t = (visual != null) ? visual : transform;
        float absX = Mathf.Abs(t.localScale.x);
        _targetScaleX = isRight ? absX : -absX;
    }

    void ApplyFlipSmoothing()
    {
        Transform t = (visual != null) ? visual : transform;
        Vector3 s = t.localScale;

        // 用指数衰减形式的插值，帧率更稳定
        float k = 1f - Mathf.Exp(-flipLerpSpeed * Time.deltaTime);

        s.x = Mathf.Lerp(s.x, _targetScaleX, k);
        t.localScale = s;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            // 立刻把走路动画停掉（避免锁住时还在走）
            if (_isWalking)
            {
                _isWalking = false;
                if (animator != null)
                    animator.SetBool(walkingBoolName, false);
            }
        }
    }
}
