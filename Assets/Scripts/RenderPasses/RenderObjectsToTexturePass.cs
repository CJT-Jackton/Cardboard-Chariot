﻿using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Render objects into an render texture other than the camera color buffer.
    /// 
    /// The specified source depth buffer will be copy into the destination buffer
    /// to enable depth testing.
    /// </summary>
    internal class RenderObjectsToTexturePass : ScriptableRenderPass
    {
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        RenderObjectsToTexture.CustomCameraSettings m_CameraSettings;
        string m_ProfilerTag;

        public Material overrideMaterial { get; set; }
        public int overrideMaterialPassIndex { get; set; }

        public bool createTemporaryDst = false;

        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle destination { get; set; }

        public FilterMode filterMode { get; set; }

        Material m_CopyDepthMaterial;

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, bool createTempDstRT = false)
        {
            this.source = source;
            this.destination = destination;
            createTemporaryDst = createTempDstRT;
        }

        public void SetDetphState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = reference;
            m_RenderStateBlock.stencilState = stencilState;
        }

        RenderStateBlock m_RenderStateBlock;

        public RenderObjectsToTexturePass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjectsToTexture.CustomCameraSettings cameraSettings)
        {
            m_ProfilerTag = profilerTag;
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            }

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_CameraSettings = cameraSettings;

            // Copy depth buffer material
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/Lightweight Render Pipeline/CopyDepth"));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = overrideMaterial;
            drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;

            Camera camera = renderingData.cameraData.camera;
            float cameraAspect = (float)camera.pixelWidth / (float)camera.pixelHeight;
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingSample(cmd, m_ProfilerTag))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.BeginSample("Copy Depth");
                cmd.SetGlobalTexture("_CameraDepthAttachment", "_CameraDepthTexture");

                // Copy the source depth buffer to the target render texture
                if (destination.Identifier() != source)
                {
                    Blit(cmd, source, destination.Identifier(), m_CopyDepthMaterial);
                }

                cmd.EndSample("Copy Depth");
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (m_CameraSettings.overrideCamera)
                {
                    Matrix4x4 projectionMatrix = Matrix4x4.Perspective(m_CameraSettings.cameraFieldOfView, cameraAspect,
                        camera.nearClipPlane, camera.farClipPlane);

                    Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
                    Vector4 cameraTranslation = viewMatrix.GetColumn(3);
                    viewMatrix.SetColumn(3, cameraTranslation + m_CameraSettings.offset);

                    cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                    context.ExecuteCommandBuffer(cmd);
                }

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings,
                    ref m_RenderStateBlock);

                if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera)
                {
                    Matrix4x4 projectionMatrix = Matrix4x4.Perspective(camera.fieldOfView, cameraAspect,
                        camera.nearClipPlane, camera.farClipPlane);

                    cmd.Clear();
                    cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, projectionMatrix);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor descriptor = cameraTextureDescriptor;
            //descriptor.depthBufferBits = 0;

            if (createTemporaryDst)
            {
                cmd.GetTemporaryRT(destination.id, descriptor, filterMode);
            }

            ConfigureTarget(destination.id, destination.id);
            ConfigureClear(ClearFlag.All, new Color(0, 0, 0, 0));

            //cmd.GetTemporaryRT(destination.id, descriptor, filterMode);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (createTemporaryDst)
            {
                cmd.ReleaseTemporaryRT(destination.id);
            }
        }
    }
}
