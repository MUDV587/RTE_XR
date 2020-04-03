using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTHandles.URP
{
    public class RenderSelectionDemo : MonoBehaviour
    {
        [SerializeField]
        private MeshFilter[] m_meshFilters = null;

        [SerializeField]
        private Renderer[] m_renderers = null;

        private IMeshesCache m_meshesCache;
        private IRenderersCache m_renderersCache;
        
        private void Start()
        {
            m_meshesCache = gameObject.GetComponentInChildren<IMeshesCache>();
            m_renderersCache = gameObject.GetComponentInChildren<IRenderersCache>();

            for(int i = 0; i < m_meshFilters.Length; ++i)
            {
                MeshFilter filter = m_meshFilters[i];
                if(filter != null && filter.sharedMesh != null)
                {
                    m_meshesCache.Add(filter.sharedMesh, filter.transform);
                }
            }

            if(m_meshesCache != null)
            {
                m_meshesCache.Refresh();
            }

            for (int i = 0; i < m_renderers.Length; ++i)
            {
                Renderer renderer = m_renderers[i];
                if(renderer != null)
                {
                    m_renderersCache.Add(renderer);
                }
            }
        }
        private void OnDestroy()
        {
            if(m_meshesCache != null)
            {
                m_meshesCache.Clear();
            }

            if(m_renderersCache != null)
            {
                m_renderersCache.Clear();
            }
        }
    }
}

