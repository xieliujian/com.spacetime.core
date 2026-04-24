using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if !UNITY_EDITOR && ENABLE_PROFILER
using Unity.Profiling;
#endif

namespace ST.Core.Diagnostics
{
    /// <summary>
    /// 运行时 OnGUI 性能浮层：FPS、顶点数、三角面数、Draw Call。
    /// 编辑器下使用 <see cref="UnityStats"/>；真机 Development Build 下使用 <see cref="ProfilerRecorder"/>。
    /// </summary>
    public class RuntimePerformanceHud : MonoBehaviour
    {
        [Tooltip("是否显示浮层。")]
        public bool Visible = true;

        [Tooltip("切换显示的热键（仅当 UseToggleKey 为 true）。")]
        public KeyCode ToggleKey = KeyCode.F3;

        [Tooltip("是否响应 ToggleKey 热键切换显示。")]
        public bool UseToggleKey = true;

        [Tooltip("网格统计刷新间隔（秒），避免每帧全场景遍历。")]
        public float MeshSampleInterval = 0.5f;

        [Tooltip("OnGUI 区域左上角坐标。")]
        public Vector2 ScreenPosition = new Vector2(8f, 8f);

        [Tooltip("标签字体大小。")]
        public int FontSize = 28;

        SceneMeshStats _meshStats;
        float _meshSampleTimer;
        readonly StringBuilder _sb = new StringBuilder(256);

        // FPS 采样
        int _fpsFrameCount;
        float _fpsTimer;
        float _fps;
        const float k_FpsSampleInterval = 0.5f;

        // OnGUI 缓存：GUIStyle 仅在 FontSize 变化时重建；Rect 仅在网格/FPS 采样周期更新
        GUIStyle _labelStyle;
        int _cachedFontSize = -1;
        Rect _cachedContentRect;
        bool _sizeNeedsRecalc = true;
        const float k_Pad = 10f;

#if !UNITY_EDITOR && ENABLE_PROFILER
        ProfilerRecorder _drawCallsRecorder;
#endif

        void OnEnable()
        {
#if !UNITY_EDITOR && ENABLE_PROFILER
            _drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
#endif
        }

        void OnDisable()
        {
#if !UNITY_EDITOR && ENABLE_PROFILER
            _drawCallsRecorder.Dispose();
#endif
        }

        void Update()
        {
            if (UseToggleKey && Input.GetKeyDown(ToggleKey))
                Visible = !Visible;

            // FPS 滑动平均
            _fpsFrameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= k_FpsSampleInterval)
            {
                _fps = _fpsFrameCount / _fpsTimer;
                _fpsFrameCount = 0;
                _fpsTimer = 0f;
                _sizeNeedsRecalc = true;
            }

            // 网格统计（按间隔采样，避免每帧遍历场景）
            _meshSampleTimer += Time.unscaledDeltaTime;
            if (_meshSampleTimer >= MeshSampleInterval)
            {
                _meshSampleTimer = 0f;
                if (Visible)
                {
                    _meshStats = SceneMeshStatistics.Gather();
                    _sizeNeedsRecalc = true;
                }
            }
        }

        void OnGUI()
        {
            if (!Visible)
                return;

            // GUIStyle 惰性初始化：GUI.skin 仅在 OnGUI 内有效，且 FontSize 变化时重建
            if (_labelStyle == null || _cachedFontSize != FontSize)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = FontSize,
                    alignment = TextAnchor.UpperLeft,
                    normal = { textColor = Color.white }
                };
                _cachedFontSize = FontSize;
                _sizeNeedsRecalc = true;
            }

            BuildText();

            // 仅在采样周期或样式变化时重新计算布局尺寸
            if (_sizeNeedsRecalc)
            {
                var size = _labelStyle.CalcSize(new GUIContent(_sb.ToString()));
                _cachedContentRect = new Rect(
                    ScreenPosition.x + k_Pad,
                    ScreenPosition.y + k_Pad,
                    size.x, size.y);
                _sizeNeedsRecalc = false;
            }

            var boxRect = new Rect(
                ScreenPosition.x, ScreenPosition.y,
                _cachedContentRect.width + k_Pad * 2f,
                _cachedContentRect.height + k_Pad * 2f);

            GUI.Box(boxRect, GUIContent.none);
            GUI.Label(_cachedContentRect, _sb.ToString(), _labelStyle);
        }

        void BuildText()
        {
            _sb.Clear();
            _sb.AppendLine("── Runtime Performance ──");
            _sb.Append("FPS: ").Append(_fps.ToString("F1")).AppendLine();
            _sb.Append("顶点: ").Append(_meshStats.TotalVertices.ToString("N0")).AppendLine();
            _sb.Append("三角面: ").Append(_meshStats.TotalTriangles.ToString("N0")).AppendLine();
            _sb.Append("MeshRenderer: ").Append(_meshStats.MeshRendererCount)
               .Append("  Skinned: ").Append(_meshStats.SkinnedMeshRendererCount).AppendLine();
            AppendDrawCalls(_sb);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            _sb.AppendLine();
            _sb.AppendLine("(真机 DrawCall 需 Development Build + Profiler)");
#endif
        }

        void AppendDrawCalls(StringBuilder sb)
        {
#if UNITY_EDITOR
            sb.Append("Draw Calls: ").Append(UnityStats.drawCalls);
            sb.Append("  Batches: ").Append(UnityStats.batches);
            sb.AppendLine();
#elif ENABLE_PROFILER
            if (_drawCallsRecorder.Valid)
                sb.Append("Draw Calls: ").Append(_drawCallsRecorder.LastValue).AppendLine();
            else
                sb.AppendLine("Draw Calls: (ProfilerRecorder 无效)");
#else
            sb.AppendLine("Draw Calls: —");
#endif
        }
    }
}
