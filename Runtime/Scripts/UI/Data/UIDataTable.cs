using System.Collections.Generic;
using UnityEngine;

namespace ST.Core.UI
{
    /// <summary>
    /// UI 配置注册表（静态单例字典）。
    /// 上层工程在游戏启动时调用 <see cref="Register"/> 注册所有面板/页面数据；
    /// <see cref="UIManager"/> 在打开面板时通过 <see cref="GetData"/> 查询配置。
    /// </summary>
    public static class UIDataTable
    {
        static readonly Dictionary<int, UIData> s_DataMap =
            new Dictionary<int, UIData>(CommonDefine.s_ListConst_100);

        /// <summary>
        /// 注册一条 UI 配置。重复注册同一 <see cref="UIData.uiID"/> 时覆盖旧数据。
        /// </summary>
        public static void Register(UIData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[UIDataTable] Register: data is null.");
                return;
            }

            s_DataMap[data.uiID] = data;
        }

        /// <summary>
        /// 根据整型 uiID 获取配置；未注册时返回 <c>null</c>。
        /// </summary>
        public static UIData GetData(int uiID)
        {
            s_DataMap.TryGetValue(uiID, out UIData data);
            return data;
        }

        /// <summary>清空所有已注册配置，一般在场景切换或重新初始化时调用。</summary>
        public static void Clear()
        {
            s_DataMap.Clear();
        }
    }
}
