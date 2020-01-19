using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Experimental.Rendering.LWRP;

internal class MyRenderPass : ScriptableRenderPass
{
    RenderTexture tmp;

    RenderQueueType renderQueueType;
    FilteringSettings m_FilteringSettings;
    string m_ProfilerTag;

    public Material overrideMaterial { get; set; }
    public int overrideMaterialPassIndex { get; set; }

    List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        tmp = RenderTexture.GetTemporary(cameraTextureDescriptor.width, cameraTextureDescriptor.height, 16, RenderTextureFormat.Default);
        RenderTargetIdentifier tmpId = new RenderTargetIdentifier(tmp);
        //RenderTargetIdentifier depthId = new RenderTargetIdentifier(Camera.current.targetTexture.depthBuffer);

        RenderTargetIdentifier[] colorAttachments = new RenderTargetIdentifier[]
        {
            BuiltinRenderTextureType.CameraTarget,
            tmpId
        };

        ConfigureTarget(tmpId, new RenderTargetIdentifier("_CameraDepthAttachment"));
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

    public MyRenderPass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask)
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
            m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
        }

        m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
            ? SortingCriteria.CommonTransparent
            : renderingData.cameraData.defaultOpaqueSortFlags;

        DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
        drawingSettings.overrideMaterial = overrideMaterial;
        drawingSettings.overrideMaterialPassIndex = overrideMaterialPassIndex;

        CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
        using (new ProfilingSample(cmd, m_ProfilerTag))
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            //Blit(cmd, BuiltinRenderTextureType.CurrentActive, tmp.depthBuffer);

            context.ExecuteCommandBuffer(cmd);

            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings,
                ref m_RenderStateBlock);
        }
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
        RenderTexture.ReleaseTemporary(tmp);
    }
}