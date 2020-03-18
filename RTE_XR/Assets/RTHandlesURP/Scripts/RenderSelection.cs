﻿using Battlehub.RTCommon;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Battlehub.RTHandles.URP
{
   
    public class RenderSelection : ScriptableRendererFeature
    {
        [System.Serializable]
        public class RenderSelectionSettings
        {
            public RenderPassEvent RenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            public Material PrepassMaterial = null;
            public Material BlurMaterial = null;
            public Material CompositeMaterial = null;
            public Color OutlineColor = new Color32(255, 128, 0, 255);

            [Range(0.5f, 10f)]
            public float OutlineStength = 5;

            [Range(0.1f, 3)]
            public float BlurSize = 1f;
        }

        [SerializeField]
        public RenderSelectionSettings m_settings = new RenderSelectionSettings();

        class RenderSelectionPass : ScriptableRenderPass
        {
            public RenderSelectionSettings Settings;

            private IRenderMeshesCache m_cache;
                        
            private int m_prepassId;
            private RenderTargetIdentifier m_prepassRT;

            private int m_blurredId;
            private RenderTargetIdentifier m_blurredRT;

            private int m_tmpTexId;
            private RenderTargetIdentifier m_tmpRT;

            private RenderTargetIdentifier m_cameraColorRT;

            private int m_outlineColorId;
            private int m_outlineStrengthId;
            private int m_blurDirectionId;

            public void Setup(RenderTargetIdentifier camerColorRT)
            {
                m_cache = IOC.Resolve<IRenderMeshesCache>();
                m_cameraColorRT = camerColorRT;
            }

            private RenderTextureDescriptor GetStereoCompatibleDescriptor(RenderTextureDescriptor descriptor, int width, int height, GraphicsFormat format, int depthBufferBits = 0)
            {
                // Inherit the VR setup from the camera descriptor
                var desc = descriptor;
                desc.depthBufferBits = depthBufferBits;
                desc.msaaSamples = 1;
                desc.width = width;
                desc.height = height;
                desc.graphicsFormat = format;
                return desc;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor camDesc)
            {
                if(m_cache == null)
                {
                    return;
                }
                
                var width = camDesc.width;
                var height = camDesc.height;

                m_prepassId = Shader.PropertyToID("_PrepassTex");
                m_blurredId = Shader.PropertyToID("_BlurredTex");
                m_tmpTexId = Shader.PropertyToID("_TmpTex");
                m_outlineColorId = Shader.PropertyToID("_OutlineColor");
                m_outlineStrengthId = Shader.PropertyToID("_OutlineStrength");
                m_blurDirectionId = Shader.PropertyToID("_BlurDirection");

                var desc = GetStereoCompatibleDescriptor(camDesc, width, height, camDesc.graphicsFormat);
                cmd.GetTemporaryRT(m_prepassId, desc);
                cmd.GetTemporaryRT(m_blurredId, desc);
                cmd.GetTemporaryRT(m_tmpTexId, desc);

                m_prepassRT = new RenderTargetIdentifier(m_prepassId);
                m_blurredRT = new RenderTargetIdentifier(m_blurredId);
                m_tmpRT = new RenderTargetIdentifier(m_tmpTexId);

                ConfigureTarget(m_prepassRT);
                ConfigureClear(ClearFlag.Color, new Color(0, 0, 0, 1));
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_cache == null)
                {
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get("RenderSelection");
                
                RenderMeshesBatch[] batches = m_cache.Batches;
                for (int i = 0; i < batches.Length; ++i)
                {
                    RenderMeshesBatch batch = batches[i];
                    for (int j = 0; j < batch.Mesh.subMeshCount; ++j)
                    {
                        cmd.DrawMeshInstanced(batch.Mesh, j, Settings.PrepassMaterial, 0, batch.Matrices, batch.Matrices.Length);
                    }
                }

                cmd.Blit(m_prepassRT, m_blurredRT);
                cmd.SetGlobalFloat(m_outlineStrengthId, Settings.OutlineStength);
                cmd.SetGlobalVector(m_blurDirectionId, new Vector2(Settings.BlurSize, 0));
                cmd.Blit(m_blurredRT, m_tmpRT, Settings.BlurMaterial, 0);
                cmd.SetGlobalVector(m_blurDirectionId, new Vector2(0, Settings.BlurSize));
                cmd.Blit(m_tmpRT, m_blurredRT, Settings.BlurMaterial, 0);
                
                cmd.Blit(m_cameraColorRT, m_tmpRT);
                cmd.SetGlobalTexture(m_prepassId, m_prepassRT);
                cmd.SetGlobalTexture(m_blurredId, m_blurredId);
                cmd.SetGlobalColor(m_outlineColorId, Settings.OutlineColor);
                cmd.Blit(m_tmpRT, m_cameraColorRT, Settings.CompositeMaterial);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                if (m_cache == null)
                {
                    return;
                }

                cmd.ReleaseTemporaryRT(m_prepassId);
                cmd.ReleaseTemporaryRT(m_blurredId);
                cmd.ReleaseTemporaryRT(m_tmpTexId);
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
            var src = renderer.cameraColorTarget;
            m_scriptablePass.Setup(src);
            renderer.EnqueuePass(m_scriptablePass);
        }
    }



}