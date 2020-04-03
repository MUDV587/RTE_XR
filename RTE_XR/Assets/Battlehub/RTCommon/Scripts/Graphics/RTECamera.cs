using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.RTCommon
{
    public interface IRTECamera
    {
        CameraEvent Event
        {
            get;
            set;
        }

        IRenderersCache RenderersCache
        {
            get;
        }

        IMeshesCache MeshesCache
        {
            get;
        }
    }

    public class RTECamera : MonoBehaviour, IRTECamera
    {
        private Camera m_camera;
        private CommandBuffer m_commandBuffer;

        [SerializeField]
        private CameraEvent m_cameraEvent = CameraEvent.BeforeImageEffects;
        public CameraEvent Event
        {
            get { return m_cameraEvent; }
            set
            {
                m_cameraEvent = value;
                RemoveCommandBuffer();
                CreateCommandBuffer();
            }
        }

        private IRenderersCache m_renderersCache;
        private IMeshesCache m_meshesCache;

        public IRenderersCache RenderersCache
        {
            get { return m_renderersCache; }
            set { m_renderersCache = value; }
        }

        public IMeshesCache MeshesCache
        {
            get { return m_meshesCache; }
            set { m_meshesCache = value; }
        }

        private void Start()
        {
            m_camera = GetComponent<Camera>();
            CreateCommandBuffer();

            if(m_renderersCache == null)
            {
                m_renderersCache = gameObject.GetComponent<IRenderersCache>();
            }

            if(m_meshesCache == null)
            {
                m_meshesCache = gameObject.GetComponent<IMeshesCache>();
            }
            
            Refresh();

            if(m_renderersCache != null)
            {
                m_renderersCache.Refreshed += OnRefresh;
            }

            if(m_meshesCache != null)
            {
                m_meshesCache.Refreshed += OnRefresh;
            }
        }

        private void OnDestroy()
        {
            if (m_renderersCache != null)
            {
                m_renderersCache.Refreshed -= OnRefresh; 
            }

            if (m_meshesCache != null)
            {
                m_meshesCache.Refreshed -= OnRefresh;
            }

            if (m_camera != null)
            {
                RemoveCommandBuffer();
            }
        }

        private void OnRefresh()
        {
            Refresh();
        }

        private void CreateCommandBuffer()
        {
            if (m_commandBuffer != null || m_camera == null)
            {
                return;
            }
            m_commandBuffer = new CommandBuffer();
            m_commandBuffer.name = "RTECameraCommandBuffer";
            m_camera.AddCommandBuffer(m_cameraEvent, m_commandBuffer);
        }

        private void RemoveCommandBuffer()
        {
            if (m_commandBuffer == null)
            {
                return;
            }
            m_camera.RemoveCommandBuffer(m_cameraEvent, m_commandBuffer);
            m_commandBuffer = null;
        }

        private void Refresh()
        {
            m_commandBuffer.Clear();

            if(m_renderersCache != null)
            {
                IList<Renderer> renderers = m_renderersCache.Renderers;
                for (int i = 0; i < renderers.Count; ++i)
                {
                    Renderer renderer = renderers[i];
                    Material[] materials = renderer.sharedMaterials;
                    for (int j = 0; j < materials.Length; ++j)
                    {
                        Material material = materials[j];
                        m_commandBuffer.DrawRenderer(renderer, material, j, -1);
                    }
                }
            }

            if(m_meshesCache != null)
            {
                IList<RenderMeshesBatch> batches = m_meshesCache.Batches;
                for (int i = 0; i < batches.Count; ++i)
                {
                    RenderMeshesBatch batch = batches[i];
                    if (batch.Material == null)
                    {
                        continue;
                    }

                    if (batch.Material.enableInstancing)
                    {
                        for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                        {
                            if (batch.Mesh != null)
                            {
                                m_commandBuffer.DrawMeshInstanced(batch.Mesh, j, batch.Material, -1, batch.Matrices, batch.Matrices.Length);
                            }
                        }
                    }
                    else
                    {
                        Matrix4x4[] matrices = batch.Matrices;
                        for (int m = 0; m < matrices.Length; ++m)
                        {
                            for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                            {
                                if (batch.Mesh != null)
                                {
                                    m_commandBuffer.DrawMesh(batch.Mesh, matrices[m], batch.Material, j, -1);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
