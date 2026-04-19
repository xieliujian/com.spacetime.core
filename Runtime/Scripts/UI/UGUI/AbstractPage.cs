using System;
using UnityEngine;

namespace ST.Core.UI
{
    /// <summary>
    /// 页面基类，可以作为独立页面挂载在 <see cref="AbstractPanel"/> 下，
    /// 也可以作为面板内预置的内部子页面（Inner Page）被自动发现。
    /// <para>
    /// 生命周期顺序：<see cref="OnCreate"/> → <see cref="OnOpen"/> →
    /// [<see cref="OnVisibleChanged"/>] → <see cref="OnClose"/> → <see cref="OnDispose"/>
    /// </para>
    /// </summary>
    public abstract class AbstractPage : MonoBehaviour
    {
        int          m_UIID;
        UIPanelActive m_PanelActive;
        bool         m_IsCreated;
        bool         m_IsOpened;

        /// <summary>UI 整型标识，由 <see cref="UIManager"/> 在加载后赋值。</summary>
        public int uiID
        {
            get { return m_UIID; }
            set { m_UIID = value; }
        }

        /// <summary>所属面板的生命周期容器，由 <see cref="UIManager"/> 在加载后赋值。</summary>
        public UIPanelActive panelActive
        {
            get { return m_PanelActive; }
            set { m_PanelActive = value; }
        }

        /// <summary>页面当前是否处于打开状态。</summary>
        public bool isOpened
        {
            get { return m_IsOpened; }
        }

        // ─── 框架内部调用，子类请勿直接调用 ───────────────────────────────

        internal void InternalCreate()
        {
            if (m_IsCreated) return;
            m_IsCreated = true;
            OnCreate();
        }

        internal void InternalOpen(object[] args)
        {
            m_IsOpened = true;
            OnOpen(args);
        }

        internal void InternalClose()
        {
            m_IsOpened = false;
            OnClose();
        }

        internal void InternalDispose()
        {
            m_IsCreated = false;
            OnDispose();
        }

        internal void InternalVisibleChanged(bool visible)
        {
            OnVisibleChanged(visible);
        }

        internal void InternalPlayOpenAnimation(Action onComplete)
        {
            PlayOpenAnimation(onComplete);
        }

        internal void InternalPlayCloseAnimation(Action onComplete)
        {
            PlayCloseAnimation(onComplete);
        }

        // ─── 子类可重写的生命周期回调 ─────────────────────────────────────

        /// <summary>仅调用一次，用于初始化组件引用、注册按钮事件等。</summary>
        protected virtual void OnCreate() { }

        /// <summary>每次打开时调用，<paramref name="args"/> 为 <see cref="UIPanelActive.AttachPage"/> 传入的参数。</summary>
        protected virtual void OnOpen(object[] args) { }

        /// <summary>每次关闭时调用，对应 <see cref="OnOpen"/>。</summary>
        protected virtual void OnClose() { }

        /// <summary>仅调用一次（销毁前），用于释放资源、取消订阅事件。</summary>
        protected virtual void OnDispose() { }

        /// <summary>面板可见性变化时调用；子类可通过此回调控制渲染开关。</summary>
        protected virtual void OnVisibleChanged(bool isVisible) { }

        /// <summary>
        /// 页面打开进场动画；默认立即完成。
        /// 重写时必须在动画结束后调用 <paramref name="onComplete"/>。
        /// </summary>
        protected virtual void PlayOpenAnimation(Action onComplete)
        {
            onComplete?.Invoke();
        }

        /// <summary>
        /// 页面关闭退场动画；默认立即完成。
        /// 重写时必须在动画结束后调用 <paramref name="onComplete"/>。
        /// </summary>
        protected virtual void PlayCloseAnimation(Action onComplete)
        {
            onComplete?.Invoke();
        }
    }
}
