using Battlehub.RTCommon;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Battlehub.RTHandles.URP
{
    public class RenderGraphics : ScriptableRendererFeature
    {
        [System.Serializable]
        public class RenderGraphicsSettings
        {
            public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            public string MeshesCacheName = "RenderMeshes";
            public string RenderersCacheName = "RenderRenderers";
            public LayerMask LayerMask = -1;
        }

        [SerializeField]
        public RenderGraphicsSettings m_settings = new RenderGraphicsSettings();

        class RenderSelectionPass : ScriptableRenderPass
        {
            public RenderGraphicsSettings Settings;

            private IMeshesCache m_meshesCache;
            private IRenderersCache m_renderersCache;
                        
            public void Setup(IMeshesCache meshesCache, IRenderersCache renderersCache)
            {
                m_meshesCache = meshesCache;
                m_renderersCache = renderersCache;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor camDesc)
            {
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get("RenderGraphics");
                
                if(m_meshesCache != null)
                {
                    IList<RenderMeshesBatch> batches = m_meshesCache.Batches;
                    for (int i = 0; i < batches.Count; ++i)
                    {
                        RenderMeshesBatch batch = batches[i];
                        for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                        {
                            if(batch.Mesh != null)
                            {
                                cmd.DrawMeshInstanced(batch.Mesh, j, batch.Material, 0, batch.Matrices, batch.Matrices.Length);
                            }
                        }
                    }
                }

                if(m_renderersCache != null)
                {
                    IList<Renderer> renderers = m_renderersCache.Renderers;
                    for(int i = 0; i < renderers.Count; ++i)
                    {
                        Renderer renderer = renderers[i];
                        if(renderer != null && renderer.enabled && renderer.gameObject.activeSelf)
                        {
                            Material[] materials = renderer.sharedMaterials;

                            for(int j = 0; j < materials.Length; ++j)
                            {
                                Material material = materials[j];
                                if(materials[j] != null)
                                {
                                    cmd.DrawRenderer(renderer, material, j);
                                }
                            }
                        }
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                
            }
        }

        private RenderSelectionPass m_scriptablePass;
        
        public override void Create()
        {
            m_scriptablePass = new RenderSelectionPass();
            m_scriptablePass.Settings = m_settings;
            m_scriptablePass.renderPassEvent = m_settings.RenderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if ((renderingData.cameraData.camera.cullingMask & m_settings.LayerMask) != 0)
            {
                IMeshesCache meshesCache = IOC.Resolve<IMeshesCache>(m_settings.MeshesCacheName);
                IRenderersCache renderersCache = IOC.Resolve<IRenderersCache>(m_settings.RenderersCacheName);

                if ((meshesCache == null || meshesCache.IsEmpty) && (renderersCache == null || renderersCache.IsEmpty))
                {
                    return;
                }

                m_scriptablePass.Setup(meshesCache, renderersCache); 
                renderer.EnqueuePass(m_scriptablePass);
            }
        }
    }



}
