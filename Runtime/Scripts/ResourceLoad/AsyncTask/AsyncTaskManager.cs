using System.Collections.Generic;

namespace ST.Core
{
    /// <summary>
    /// 默认的 <see cref="BaseAsyncTaskManager"/> 实现：在 <see cref="DoUpdate"/> 中遍历任务、调用 <see cref="AsyncTask.Update"/>，并移除已结束任务。
    /// </summary>
    public class AsyncTaskManager : BaseAsyncTaskManager
    {
        /// <summary>当前所有活跃任务列表。</summary>
        List<AsyncTask> m_TaskList = new List<AsyncTask>(CommonDefine.s_ListConst_100);
        /// <summary>每帧收集已结束任务，延迟从 <see cref="m_TaskList"/> 中移除以避免遍历中修改集合。</summary>
        List<AsyncTask> m_TempTaskList = new List<AsyncTask>(CommonDefine.s_ListConst_16);

        /// <inheritdoc />
        public override void AddTask(AsyncTask asynctask)
        {
            m_TaskList.Add(asynctask);
        }

        /// <summary>无关闭清理。</summary>
        public override void DoClose() { }

        /// <summary>无初始化。</summary>
        public override void DoInit() { }

        /// <summary>更新所有任务并移除 <see cref="AsyncTask.isEnd"/> 为真的项。</summary>
        public override void DoUpdate()
        {
            m_TempTaskList.Clear();

            for (int i = 0; i < m_TaskList.Count; i++)
            {
                var task = m_TaskList[i];
                if (task == null)
                    continue;

                task.Update();

                if (task.isEnd)
                    m_TempTaskList.Add(task);
            }

            for (int i = 0; i < m_TempTaskList.Count; i++)
            {
                var task = m_TempTaskList[i];
                if (task == null)
                    continue;

                m_TaskList.Remove(task);
            }

            m_TempTaskList.Clear();
        }
    }
}
