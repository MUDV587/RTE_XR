using Battlehub.RTHandles.URP;
using UnityEngine;

namespace Battlehub.RTCommon.URP
{
    public class RuntimeGraphicsLayerURP : MonoBehaviour, IRuntimeGraphicsLayer
    {
        private IRenderersCache m_renderersCache;
        private IMeshesCache m_meshesCache;

        private void Awake()
        {
            m_renderersCache = GetComponent<IRenderersCache>();
            m_meshesCache = GetComponent<IMeshesCache>();
        }

        public void AddMesh(Mesh mesh, Matrix4x4 matrix, Material material)
        {
            m_meshesCache.AddBatch(mesh, material, new[] { matrix });
        }

        public void RemoveMesh(Mesh mesh)
        {
            m_meshesCache.RemoveBatch(mesh);
        }

        public void AddRenderers(Renderer[] renderers)
        {
            for(int i = 0; i < renderers.Length; ++i)
            {
                m_renderersCache.Add(renderers[i]);
            }
        }

        public void RemoveRenderers(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; ++i)
            {
                m_renderersCache.Remove(renderers[i]);
            }
        }

        public void BeginRefresh()
        {
            
        }

        public void EndRefresh()
        {
            m_meshesCache.Refresh();
        }      
    }

}
