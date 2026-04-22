using UnityEngine;

namespace ST.Core
{
    // ──────────────────────────────────────────
    // 场景漫游摄像机
    // ──────────────────────────────────────────

    /// <summary>
    /// 场景漫游摄像机控制器，支持键盘移动、鼠标右键旋转与滚轮调速。
    /// 兼容 WebGL 平台（不依赖光标锁定，不使用线程 API）。
    /// </summary>
    /// <remarks>
    /// 操作说明：
    /// <list type="bullet">
    ///   <item>WASD / 方向键 — 前后左右移动</item>
    ///   <item>Q / E — 下降 / 上升</item>
    ///   <item>按住鼠标右键拖拽 — 旋转视角</item>
    ///   <item>鼠标滚轮 — 调整移动速度</item>
    ///   <item>按住 Shift — 移动速度 × 加速倍率</item>
    /// </list>
    /// </remarks>
    public class SceneRoamCamera : MonoBehaviour
    {
        // ──────────────────────────────────────────
        // Inspector 参数
        // ──────────────────────────────────────────

        /// <summary>基础移动速度（单位/秒）。</summary>
        [Header("移动")]
        public float moveSpeed = 10f;

        /// <summary>移动速度最小值，防止滚轮调至零或负数。</summary>
        public float moveSpeedMin = 0.5f;

        /// <summary>移动速度最大值。</summary>
        public float moveSpeedMax = 100f;

        /// <summary>每格滚轮调整的速度步长。</summary>
        public float scrollSpeedStep = 2f;

        /// <summary>按住 Shift 时的速度倍率。</summary>
        public float boostMultiplier = 3f;

        /// <summary>鼠标旋转灵敏度。</summary>
        [Header("旋转")]
        public float mouseSensitivity = 2f;

        /// <summary>俯仰角（X 轴旋转）最小值，防止翻转。</summary>
        public float pitchMin = -89f;

        /// <summary>俯仰角（X 轴旋转）最大值。</summary>
        public float pitchMax = 89f;

        /// <summary>旋转平滑插值系数；0 表示不插值（即时响应），适合 WebGL。</summary>
        [Header("平滑")]
        [Range(0f, 1f)]
        public float rotationSmoothing = 0f;

        // ──────────────────────────────────────────
        // 私有状态
        // ──────────────────────────────────────────

        /// <summary>当前俯仰角（绕 X 轴）。</summary>
        float m_Pitch;

        /// <summary>当前偏航角（绕 Y 轴）。</summary>
        float m_Yaw;

        /// <summary>目标俯仰角，用于平滑插值。</summary>
        float m_TargetPitch;

        /// <summary>目标偏航角，用于平滑插值。</summary>
        float m_TargetYaw;

        // ──────────────────────────────────────────
        // Unity 生命周期
        // ──────────────────────────────────────────

        /// <summary>
        /// 从当前 Transform 旋转初始化俯仰角与偏航角，防止首帧跳变。
        /// </summary>
        void Start()
        {
            Vector3 euler = transform.eulerAngles;

            m_Pitch = NormalizeAngle(euler.x);
            m_Yaw   = euler.y;

            m_TargetPitch = m_Pitch;
            m_TargetYaw   = m_Yaw;
        }

        /// <summary>
        /// 每帧处理输入：旋转 → 移动速度调整 → 移动。
        /// </summary>
        void Update()
        {
            HandleRotation();
            HandleScrollSpeed();
            HandleMovement();
        }

        // ──────────────────────────────────────────
        // 输入处理
        // ──────────────────────────────────────────

        /// <summary>
        /// 按住鼠标右键时读取鼠标增量并更新摄像机旋转。
        /// 不依赖光标锁定，在 WebGL 浏览器中安全运行。
        /// </summary>
        void HandleRotation()
        {
            if (!Input.GetMouseButton(1))
                return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            m_TargetYaw   += mouseX;
            m_TargetPitch -= mouseY;
            m_TargetPitch  = Mathf.Clamp(m_TargetPitch, pitchMin, pitchMax);

            if (rotationSmoothing > 0f)
            {
                float t  = 1f - Mathf.Pow(rotationSmoothing, Time.deltaTime * 60f);
                m_Pitch  = Mathf.Lerp(m_Pitch, m_TargetPitch, t);
                m_Yaw    = Mathf.Lerp(m_Yaw,   m_TargetYaw,   t);
            }
            else
            {
                m_Pitch = m_TargetPitch;
                m_Yaw   = m_TargetYaw;
            }

            transform.rotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
        }

        /// <summary>
        /// 通过鼠标滚轮动态调整 <see cref="moveSpeed"/>，范围限定在 [moveSpeedMin, moveSpeedMax]。
        /// </summary>
        void HandleScrollSpeed()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (Mathf.Abs(scroll) < 0.0001f)
                return;

            moveSpeed += scroll * scrollSpeedStep;
            moveSpeed  = Mathf.Clamp(moveSpeed, moveSpeedMin, moveSpeedMax);
        }

        /// <summary>
        /// 读取 WASD / 方向键 / QE 输入，沿摄像机本地坐标系移动。
        /// Shift 键激活加速倍率。
        /// </summary>
        void HandleMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            float up = 0f;

            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.PageDown))
                up = -1f;
            else if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.PageUp))
                up = 1f;

            Vector3 dir = new Vector3(h, up, v);

            if (dir.sqrMagnitude < 0.0001f)
                return;

            dir.Normalize();

            float speed = moveSpeed;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                speed *= boostMultiplier;

            transform.Translate(dir * speed * Time.deltaTime, Space.Self);
        }

        // ──────────────────────────────────────────
        // 工具方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 将欧拉角规范化到 (-180, 180] 区间，确保俯仰角初始值正确映射。
        /// </summary>
        /// <param name="angle">原始欧拉角度数。</param>
        /// <returns>规范化后的角度，范围 (-180, 180]。</returns>
        static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
                angle -= 360f;

            while (angle <= -180f)
                angle += 360f;

            return angle;
        }
    }
}
