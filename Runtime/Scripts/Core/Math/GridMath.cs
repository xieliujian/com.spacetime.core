using UnityEngine;

namespace ST.Core
{
    /// <summary>
    /// 三维网格（Grid）常用数学运算工具集。
    /// 提供坐标展平/还原、世界坐标与格子索引转换等静态方法。
    /// </summary>
    public static class GridMath
    {
        // ──────────────────────────────────────────
        // 坐标展平 / 还原
        // ──────────────────────────────────────────

        /// <summary>
        /// 将三维格子坐标 (x, y, z) 展平为一维线性索引。
        /// </summary>
        /// <param name="x">X 轴格子坐标。</param>
        /// <param name="y">Y 轴格子坐标。</param>
        /// <param name="z">Z 轴格子坐标。</param>
        /// <param name="cellCount">各轴格子数量。</param>
        /// <returns>对应的一维索引。</returns>
        public static int FlattenXYZ(int x, int y, int z, Vector3 cellCount)
        {
            return Mathf.CeilToInt((z * cellCount.x * cellCount.y) + (y * cellCount.x) + x);
        }

        /// <summary>
        /// 将三维格子坐标展平为一维索引，并将坐标钳位到合法范围内。
        /// </summary>
        public static int FlattenXYZClamp(int x, int y, int z, Vector3 cellCount)
        {
            int clampedX = (int)Mathf.Clamp(x, 0, cellCount.x - 1);
            int clampedY = (int)Mathf.Clamp(y, 0, cellCount.y - 1);
            int clampedZ = (int)Mathf.Clamp(z, 0, cellCount.z - 1);
            return FlattenXYZ(clampedX, clampedY, clampedZ, cellCount);
        }

        /// <summary>
        /// 将一维线性索引还原为三维格子坐标 (x, y, z)。
        /// </summary>
        /// <param name="index">一维索引。</param>
        /// <param name="x">输出的 X 轴格子坐标。</param>
        /// <param name="y">输出的 Y 轴格子坐标。</param>
        /// <param name="z">输出的 Z 轴格子坐标。</param>
        /// <param name="cellCount">各轴格子数量。</param>
        public static void UnflattenToXYZ(int index, out int x, out int y, out int z, Vector3 cellCount)
        {
            x = index % (int)cellCount.x;
            y = (index / (int)cellCount.x) % (int)cellCount.y;
            z = index / ((int)cellCount.x * (int)cellCount.y);
        }

        /// <summary>
        /// 根据本地坐标和格子尺寸将位置还原为三维格子坐标。
        /// </summary>
        /// <param name="localPos">本地空间坐标。</param>
        /// <param name="cellSize">单个格子的尺寸。</param>
        /// <param name="x">输出的 X 轴格子坐标。</param>
        /// <param name="y">输出的 Y 轴格子坐标。</param>
        /// <param name="z">输出的 Z 轴格子坐标。</param>
        public static void UnflattenToXYZ(Vector3 localPos, Vector3 cellSize, out int x, out int y, out int z)
        {
            x = (int)(localPos.x / cellSize.x);
            y = (int)(localPos.y / cellSize.y);
            z = (int)(localPos.z / cellSize.z);
        }

        // ──────────────────────────────────────────
        // ushort 与字节互转
        // ──────────────────────────────────────────

        /// <summary>
        /// 将 ushort 值拆分为高字节和低字节。
        /// </summary>
        /// <param name="data">原始 ushort 值。</param>
        /// <param name="highByte">输出高字节。</param>
        /// <param name="lowByte">输出低字节。</param>
        public static void FlattenUShort2Byte(ushort data, out byte highByte, out byte lowByte)
        {
            highByte = (byte)(data / 256);
            lowByte = (byte)(data % 256);
        }

        /// <summary>
        /// 将高字节和低字节合并为 ushort 值。
        /// </summary>
        /// <param name="highByte">高字节。</param>
        /// <param name="lowByte">低字节。</param>
        /// <returns>合并后的 ushort 值。</returns>
        public static ushort UnflattenByte2UShort(byte highByte, byte lowByte)
        {
            return (ushort)(highByte * 256 + lowByte);
        }

        // ──────────────────────────────────────────
        // 坐标合法性检查
        // ──────────────────────────────────────────

        /// <summary>
        /// 判断三维格子坐标是否在合法范围内。
        /// </summary>
        public static bool IsXYZInBounds(int x, int y, int z, Vector3 cellCount)
        {
            if (x < 0 || y < 0 || z < 0)
                return false;

            if (x >= cellCount.x || y >= cellCount.y || z >= cellCount.z)
                return false;

            return true;
        }

        // ──────────────────────────────────────────
        // 格子数量计算
        // ──────────────────────────────────────────

        /// <summary>
        /// 根据体积尺寸和格子尺寸计算总格子数量。
        /// </summary>
        public static int CalculateNumberOfCells(Vector3 scale, Vector3 cellSize)
        {
            return Mathf.CeilToInt(scale.x / cellSize.x)
                 * Mathf.CeilToInt(scale.y / cellSize.y)
                 * Mathf.CeilToInt(scale.z / cellSize.z);
        }

        /// <summary>
        /// 根据体积尺寸和均匀格子边长计算各轴格子数量向量。
        /// </summary>
        public static Vector3 CalculateCellCount(Vector3 scale, float cellSizeAxis)
        {
            return CalculateCellCount(scale, new Vector3(cellSizeAxis, cellSizeAxis, cellSizeAxis));
        }

        /// <summary>
        /// 根据体积尺寸和各轴格子尺寸计算各轴格子数量向量。
        /// </summary>
        public static Vector3 CalculateCellCount(Vector3 scale, Vector3 cellSize)
        {
            return new Vector3(
                Mathf.CeilToInt(scale.x / cellSize.x),
                Mathf.CeilToInt(scale.y / cellSize.y),
                Mathf.CeilToInt(scale.z / cellSize.z));
        }

        /// <summary>
        /// 返回 Vector3 三个分量中的最大值。
        /// </summary>
        public static float CalcMaxValue(Vector3 val)
        {
            return Mathf.Max(Mathf.Max(val.x, val.y), val.z);
        }

        // ──────────────────────────────────────────
        // 世界 / 本地坐标 → 格子索引
        // ──────────────────────────────────────────

        /// <summary>
        /// 根据本地坐标计算格子索引（自动钳位到合法范围）。
        /// </summary>
        public static int GetIndexForLocalPos(Vector3 localPos, Vector3 cellSize, Vector3 cellCount)
        {
            int unclampedX = (int)(localPos.x / cellSize.x);
            int unclampedY = (int)(localPos.y / cellSize.y);
            int unclampedZ = (int)(localPos.z / cellSize.z);

            int clampedX = (int)Mathf.Clamp(unclampedX, 0, cellCount.x - 1);
            int clampedY = (int)Mathf.Clamp(unclampedY, 0, cellCount.y - 1);
            int clampedZ = (int)Mathf.Clamp(unclampedZ, 0, cellCount.z - 1);

            return FlattenXYZ(clampedX, clampedY, clampedZ, cellCount);
        }

        /// <summary>
        /// 根据世界坐标计算格子索引，同时输出是否越界及对应本地坐标。
        /// </summary>
        /// <param name="worldPos">查询的世界坐标。</param>
        /// <param name="gridOrigin">网格原点（世界坐标）。</param>
        /// <param name="gridCurrentOrientation">网格当前旋转。</param>
        /// <param name="gridScale">网格世界尺寸。</param>
        /// <param name="gridBakeOrientation">网格烘焙时的旋转。</param>
        /// <param name="cellCount">各轴格子数量。</param>
        /// <param name="cellSize">单个格子尺寸。</param>
        /// <param name="isOutOfBounds">输出：坐标是否超出网格范围。</param>
        /// <param name="outLocalPos">输出：对应的本地坐标。</param>
        /// <returns>格子一维索引。</returns>
        public static int GetIndexForWorldPos(
            Vector3 worldPos, Vector3 gridOrigin,
            Quaternion gridCurrentOrientation, Vector3 gridScale,
            Quaternion gridBakeOrientation, Vector3 cellCount, Vector3 cellSize,
            out bool isOutOfBounds, out Vector3 outLocalPos)
        {
            worldPos = ((Quaternion.Inverse(gridCurrentOrientation) * gridBakeOrientation)
                        * (worldPos - gridOrigin)) + gridOrigin;
            worldPos = Quaternion.Inverse(gridBakeOrientation) * worldPos;

            Vector3 localPosition = (worldPos - gridOrigin) + 0.5f * gridScale;
            outLocalPos = localPosition;

            int unclampedX = (int)(localPosition.x / cellSize.x);
            int unclampedY = (int)(localPosition.y / cellSize.y);
            int unclampedZ = (int)(localPosition.z / cellSize.z);

            int clampedX = (int)Mathf.Clamp(unclampedX, 0, cellCount.x - 1);
            int clampedY = (int)Mathf.Clamp(unclampedY, 0, cellCount.y - 1);
            int clampedZ = (int)Mathf.Clamp(unclampedZ, 0, cellCount.z - 1);

            isOutOfBounds = Mathf.Abs(unclampedX - clampedX) > 2
                         || Mathf.Abs(unclampedY - clampedY) > 2
                         || Mathf.Abs(unclampedZ - clampedZ) > 2;

            return FlattenXYZ(clampedX, clampedY, clampedZ, cellCount);
        }

        /// <summary>
        /// 根据体积原点、旋转和本地坐标计算世界坐标。
        /// </summary>
        /// <param name="volumePos">体积原点（世界坐标）。</param>
        /// <param name="volumeRot">体积旋转。</param>
        /// <param name="localPos">本地坐标。</param>
        /// <returns>对应世界坐标。</returns>
        public static Vector3 CalcWorldPos(Vector3 volumePos, Quaternion volumeRot, Vector3 localPos)
        {
            return volumePos + volumeRot * localPos;
        }
    }
}
