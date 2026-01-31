using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 5f;     // units/sec
    public bool canMove = true;

    [Header("Runtime (ReadOnly)")]
    public Vector3 velocity;         // 当前速度向量（世界空间）
    public float speed;              // 速度标量（= |velocity|）

    [Header("Anim")]
    public Animator animator;
    public string walkingBoolName = "isWalking";
    public string moveXParamName = "moveX";   // -1/0/1
    //public string speedParamName = "speed";   // 可选

    // 内部状态
    private float _inputX;
    private Vector3 _lastPos;

    void Awake()
    {
        _lastPos = transform.position;
    }

    void Update()
    {
        // 1) 读取输入
        _inputX = Input.GetAxisRaw("Horizontal"); // -1/0/1

        // 2) 计算本帧移动（直接位移）
        float moveDir = canMove ? _inputX : 0f;

        Vector3 pos = transform.position;
        if (moveDir != 0f)
            pos.x += moveDir * moveSpeed * Time.deltaTime;

        transform.position = pos;

        // 3) 计算“真实速度向量”（由位移反推）
        velocity = (transform.position - _lastPos) / Mathf.Max(Time.deltaTime, 0.00001f);
        speed = velocity.magnitude;
        _lastPos = transform.position;

        // 4) 动画参数
        if (animator != null)
        {
            bool isWalking = canMove && Mathf.Abs(_inputX) > 0.01f;
            animator.SetBool(walkingBoolName, isWalking);

            // moveX：只表达“朝向/左右/不动”，最适合给 Animator 做分支或 BlendTree
            float moveX = isWalking ? Mathf.Sign(_inputX) : 0f;
            animator.SetFloat(moveXParamName, moveX);

            // 可选：speed 用于 BlendTree（比如 Idle/Walk/Run）
            //animator.SetFloat(speedParamName, speed);
        }
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            // 立即停下：把 lastPos 对齐，避免 velocity 突然跳
            _lastPos = transform.position;
            velocity = Vector3.zero;
            speed = 0f;

            if (animator != null)
            {
                animator.SetBool(walkingBoolName, false);
                animator.SetFloat(moveXParamName, 0f);
                //animator.SetFloat(speedParamName, 0f);
            }
        }
    }
}
