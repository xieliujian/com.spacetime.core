using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 通用场景句柄辅助工具，支持在 Scene 视图中通过拖拽句柄调整 Behaviour 的体积尺寸。
    /// IResizableByHandle 接口可在 Runtime 中使用；句柄绘制逻辑仅在 Editor 下编译。
    /// </summary>
    public static class CustomHandle
    {
        /// <summary>
        /// 实现此接口的 Behaviour 可通过 <see cref="ActualHandle{T,U}"/> 在 Scene 视图中调整尺寸。
        /// </summary>
        public interface IResizableByHandle
        {
            /// <summary>句柄所操控的体积尺寸（世界单位）。</summary>
            Vector3 HandleSized { get; set; }
        }

        /// <summary>
        /// 泛型句柄绘制器，为实现 <see cref="IResizableByHandle"/> 的 Behaviour 在六个轴向上绘制可拖拽句柄。
        /// </summary>
        /// <typeparam name="T">目标 Behaviour 类型，须实现 <see cref="IResizableByHandle"/>。</typeparam>
        /// <typeparam name="U">坐标精度类型，支持 float 或 int。</typeparam>
        public class ActualHandle<T, U>
            where T : UnityEngine.Behaviour, IResizableByHandle
            where U : struct
        {
#if UNITY_EDITOR
            /// <summary>
            /// 为 <paramref name="zone"/> 在 Scene 视图的六个轴向上绘制尺寸调整句柄。
            /// </summary>
            /// <param name="zone">目标 Behaviour 实例。</param>
            public void DrawHandle(T zone)
            {
                Matrix4x4 mat = Matrix4x4.TRS(zone.transform.position, zone.transform.rotation, Vector3.one);

                DrawHandleForDirection(zone, mat, Vector3.up);
                DrawHandleForDirection(zone, mat, -Vector3.up);
                DrawHandleForDirection(zone, mat, Vector3.right);
                DrawHandleForDirection(zone, mat, -Vector3.right);
                DrawHandleForDirection(zone, mat, Vector3.forward);
                DrawHandleForDirection(zone, mat, -Vector3.forward);
            }

            /// <summary>在指定轴向上绘制一个自由移动句柄，并根据拖拽结果更新体积尺寸。</summary>
            void DrawHandleForDirection(T origin, Matrix4x4 matrix4X4, Vector3 axis)
            {
                Vector3 oldSize = origin.HandleSized;
                Vector3 snap = Vector3.one * 0.5f;

                Vector3 GetAxis(Vector3 value) => Vector3.Scale(value, axis);

                Transform t = origin.transform;
                Vector3 basePos = matrix4X4.MultiplyPoint(0.5f * Vector3.Scale(origin.HandleSized, axis));
                float size = UnityEditor.HandleUtility.GetHandleSize(basePos) * 0.1f;

                UnityEditor.EditorGUI.BeginChangeCheck();
#if UNITY_2022_1_OR_NEWER
                var fmh = Quaternion.identity;
                Vector3 hPos3 = UnityEditor.Handles.FreeMoveHandle(basePos, size, snap, UnityEditor.Handles.DotHandleCap);
#else
                Vector3 hPos3 = UnityEditor.Handles.FreeMoveHandle(basePos, Quaternion.identity, size, snap, UnityEditor.Handles.DotHandleCap);
#endif
                if (!UnityEditor.EditorGUI.EndChangeCheck())
                    return;

                string undoName = "Size changed";
                UnityEditor.Undo.RecordObject(origin, undoName);
                UnityEditor.Undo.RecordObject(origin.transform, undoName);

                if (axis.x < 0 || axis.y < 0 || axis.z < 0)
                {
                    Vector3 tmp = GetAxis(basePos) - GetAxis(hPos3);
                    tmp.x = GetValue(tmp.x);
                    tmp.y = GetValue(tmp.y);
                    tmp.z = GetValue(tmp.z);

                    origin.HandleSized = origin.HandleSized - tmp;

                    if (GetAxis(origin.HandleSized).magnitude <= 1)
                        origin.HandleSized = oldSize;
                    else
                        t.Translate(0.5f * tmp);
                }
                else
                {
                    Vector3 tmp = GetAxis(hPos3) - GetAxis(basePos);
                    tmp.x = GetValue(tmp.x);
                    tmp.y = GetValue(tmp.y);
                    tmp.z = GetValue(tmp.z);

                    origin.HandleSized = origin.HandleSized + tmp;

                    if (GetAxis(origin.HandleSized).magnitude <= 1)
                        origin.HandleSized = oldSize;
                    else
                        t.Translate(0.5f * tmp);
                }
            }

            /// <summary>根据精度类型 <typeparamref name="U"/> 将浮点值转换为对应精度的值。</summary>
            float GetValue(float x)
            {
                if (typeof(U) == typeof(float))
                    return x;

                if (typeof(U) == typeof(int))
                    return (int)x;

                throw new System.NotImplementedException();
            }
#endif
        }
    }
}
