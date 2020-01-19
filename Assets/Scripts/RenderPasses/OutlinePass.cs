using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.Universal
{
    /// <summary>
    /// Copy the given color buffer to the given destination color buffer.
    ///
    /// You can use this pass to copy a color buffer to the destination,
    /// so you can use it later in rendering. For example, you can copy
    /// the opaque texture to use it for distortion effects.
    /// </summary>
    internal class OutlinePass : ScriptableRenderPass
    {
        public enum RenderTarget
        {
            Color,
            RenderTexture,
        }

        public Material blitMaterial = null;
        public int blitShaderPassIndex = 0;
        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle destination { get; set; }

        RenderTargetHandle m_TemporaryOutlineTexture;
        string m_ProfilerTag = "OutlinePass";

        // Render queue type
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        Outline.CustomCameraSettings m_CameraSettings;
        //string m_ProfilerTag;

        // Render state
        RenderStateBlock m_RenderStateBlock;

        // Override material of rendering objects
        public Material overrideMaterial { get; set; }
        public int overrideMaterialPassIndex { get; set; }

        // List of shader tag
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        /// <summary>
        /// Set up depth render state
        /// </summary>
        /// <param name="writeEnabled"></param>
        /// <param name="function"></param>
        public void SetDetphState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        /// <summary>
        /// Set up stencil render state
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="compareFunction"></param>
        /// <param name="passOp"></param>
        /// <param name="failOp"></param>
        /// <param name="zFailOp"></param>
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

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public OutlinePass(RenderPassEvent renderPassEvent, Material blitMaterial, int blitShaderPassIndex, string tag, int layerMask)
        {
            this.renderPassEvent = renderPassEvent;
            this.blitMaterial = blitMaterial;
            this.blitShaderPassIndex = blitShaderPassIndex;
            m_ProfilerTag = tag;
            m_TemporaryOutlineTexture.Init("_TemporaryOutlineTexture");

            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)
        {
            this.source = source;
            this.destination = destination;
        }

        /// <inheritdoc/>
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

                ConfigureTarget(m_TemporaryOutlineTexture.Identifier());

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, ref m_RenderStateBlock);
            }

            // ------------
            //
            // ------------
            // CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            // Can't read and write to same color target, create a temp render target to blit. 
            if (destination == RenderTargetHandle.CameraTarget)
            {
                cmd.GetTemporaryRT(m_TemporaryOutlineTexture.id, opaqueDesc, filterMode);
                Blit(cmd, source, m_TemporaryOutlineTexture.Identifier(), blitMaterial, blitShaderPassIndex);
                Blit(cmd, m_TemporaryOutlineTexture.Identifier(), source);
            }
            else
            {
                //Blit(cmd, source, destination.Identifier(), blitMaterial, blitShaderPassIndex);
                cmd.GetTemporaryRT(m_TemporaryOutlineTexture.id, opaqueDesc, filterMode);
                Blit(cmd, destination.Identifier(), m_TemporaryOutlineTexture.Identifier(), blitMaterial, blitShaderPassIndex);
                Blit(cmd, m_TemporaryOutlineTexture.Identifier(), source);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (destination == RenderTargetHandle.CameraTarget)
                cmd.ReleaseTemporaryRT(m_TemporaryOutlineTexture.id);
        }
    }
}
