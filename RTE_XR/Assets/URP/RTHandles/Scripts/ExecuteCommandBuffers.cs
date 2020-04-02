using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Battlehub.RTCommon
{
    public class ExecuteCommandBuffers : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        [SerializeField]
        public Settings m_settings = new Settings();

        class RendererPass : ScriptableRenderPass
        {
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
               
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.camera.commandBufferCount > 0)
                {
                    CommandBuffer[] cmdBuffer = renderingData.cameraData.camera.GetCommandBuffers(CameraEvent.BeforeImageEffects);
                    for(int i = 0; i < cmdBuffer.Length; ++i)
                    {
                        context.ExecuteCommandBuffer(cmdBuffer[i]);
                    }
                }
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
            }
        }

        RendererPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new RendererPass();

            // Configures where the render pass should be injected.
            m_ScriptablePass.renderPassEvent = m_settings.RenderPassEvent;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }
}

