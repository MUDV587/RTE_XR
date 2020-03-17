using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTHandles.URP
{
    public class RenderMeshesDemo : MonoBehaviour
    {
        [SerializeField]
        private MeshFilter[] m_meshFilters = null;

        private IRenderMeshesCache m_cache;
        
        private void Start()
        {
            m_cache = IOC.Resolve<IRenderMeshesCache>();
            for(int i = 0; i < m_meshFilters.Length; ++i)
            {
                MeshFilter filter = m_meshFilters[i];
                if(filter != null && filter.sharedMesh != null)
                {
                    m_cache.Add(filter.sharedMesh, filter.transform);
                }
                m_cache.Refresh();
            }
        }
        private void OnDestroy()
        {
            if(m_cache != null)
            {
                m_cache.Clear();
            }
        }
    }
}

