using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.RTCommon
{
    public interface IRenderersCache
    {
        event Action Refreshed;

        bool IsEmpty
        {
            get;
        }

        IList<Renderer> Renderers
        {
            get;
        }

        void Add(Renderer renderer);
        void Remove(Renderer renderer);
        void Refresh();
        void Clear();
    }

    public class RenderersCache : MonoBehaviour, IRenderersCache
    {
        public event Action Refreshed;

        public bool IsEmpty
        {
            get { return m_renderers.Count == 0;  }
        }

        private readonly List<Tuple<bool, bool>> m_settingsBackup = new List<Tuple<bool, bool>>();
        private readonly List<Renderer> m_renderers = new List<Renderer>();
        public IList<Renderer> Renderers
        {
            get { return m_renderers; }
        }

        private void Awake()
        {
            IOC.Register<IRenderersCache>(name, this);
        }

        private void OnDestroy()
        {
            IOC.Unregister<IRenderersCache>(name, this);
        }

        public void Add(Renderer renderer)
        {
            bool isRendererEnabled = renderer.enabled;
            bool forceMatrixRecalculation = false;
            
            if (renderer is SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)renderer;
                forceMatrixRecalculation = skinnedMeshRenderer.forceMatrixRecalculationPerRender;
                skinnedMeshRenderer.forceMatrixRecalculationPerRender = true;
            }

            if (!renderer.forceRenderingOff)
            {
                renderer.enabled = false;
            }

            m_renderers.Add(renderer);
            m_settingsBackup.Add(new Tuple<bool, bool>(isRendererEnabled, forceMatrixRecalculation));
        }

        public void Remove(Renderer renderer)
        {
            int index = m_renderers.IndexOf(renderer);
            if(index < 0)
            {
                return;
            }

            Tuple<bool, bool> settings = m_settingsBackup[index];
            if (renderer is SkinnedMeshRenderer)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)renderer;
                skinnedMeshRenderer.forceMatrixRecalculationPerRender = settings.Item2;
            }

            renderer.enabled = settings.Item1;
            m_renderers.RemoveAt(index);
            m_settingsBackup.RemoveAt(index);
        }

        public void Refresh()
        {
            if (Refreshed != null)
            {
                Refreshed();
            }
        }

        public void Clear()
        {
            m_renderers.Clear();
        }
    }
}
