using Battlehub.RTCommon;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.RTHandles.URP
{
    public interface IRenderersCache
    {
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
        void Clear();
    }

    public class RenderersCache : MonoBehaviour, IRenderersCache
    {
        public bool IsEmpty
        {
            get { return m_renderers.Count == 0;  }
        }

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
            m_renderers.Add(renderer);
        }

        public void Remove(Renderer renderer)
        {
            m_renderers.Remove(renderer);
        }

        public void Clear()
        {
            m_renderers.Clear();
        }
    }
}
