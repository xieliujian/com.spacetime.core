using UnityEngine;

namespace ST.Core.Diagnostics
{
    /// <summary>
    /// 当前场景中静态网格与蒙皮网格的粗略几何统计（按 <see cref="MeshRenderer"/> / <see cref="SkinnedMeshRenderer"/> 实例累加，不含粒子、UI Canvas 等）。
    /// </summary>
    public struct SceneMeshStats
    {
        public int MeshRendererCount;
        public int SkinnedMeshRendererCount;
        public int TotalVertices;
        public int TotalTriangles;
    }

    /// <summary>
    /// 从场景中的渲染器收集顶点与三角面数量。
    /// </summary>
    public static class SceneMeshStatistics
    {
        /// <param name="includeInactive">
        /// 为 <c>false</c>（默认）时仅统计激活 GameObject 上已启用的渲染器（即实际参与渲染的部分）；
        /// 为 <c>true</c> 时额外统计非激活 GameObject 上的渲染器（用于资产总量排查）。
        /// </param>
        public static SceneMeshStats Gather(bool includeInactive = false)
        {
            var stats = new SceneMeshStats();

            // FindObjectsOfType(false) 仅返回激活 GameObject 上的组件，无需再检查 activeInHierarchy
            var meshRenderers = Object.FindObjectsOfType<MeshRenderer>(includeInactive);
            for (var i = 0; i < meshRenderers.Length; i++)
            {
                var r = meshRenderers[i];
                if (!r.enabled)
                    continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null)
                    continue;
                var mesh = mf.sharedMesh;
                if (mesh == null)
                    continue;
                stats.MeshRendererCount++;
                AccumulateMesh(ref stats, mesh);
            }

            var skinned = Object.FindObjectsOfType<SkinnedMeshRenderer>(includeInactive);
            for (var i = 0; i < skinned.Length; i++)
            {
                var r = skinned[i];
                if (!r.enabled)
                    continue;
                var mesh = r.sharedMesh;
                if (mesh == null)
                    continue;
                stats.SkinnedMeshRendererCount++;
                AccumulateMesh(ref stats, mesh);
            }

            return stats;
        }

        static void AccumulateMesh(ref SceneMeshStats stats, Mesh mesh)
        {
            stats.TotalVertices += mesh.vertexCount;
            stats.TotalTriangles += CountTriangles(mesh);
        }

        static int CountTriangles(Mesh mesh)
        {
            long sum = 0;
            var subCount = mesh.subMeshCount;
            for (var s = 0; s < subCount; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles)
                    continue;
                sum += (long)mesh.GetIndexCount(s) / 3;
            }

            if (sum > int.MaxValue)
                return int.MaxValue;
            return (int)sum;
        }
    }
}
