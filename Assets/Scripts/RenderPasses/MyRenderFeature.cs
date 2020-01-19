﻿using System.Collections.Generic;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;
using UnityEngine;

public enum RenderQueueType
{
    Opaque,
    Transparent,
}

public class MyRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class RenderObjectsSettings
    {
        public string passTag = "Render Outline";
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

        public FilterSettings filterSettings = new FilterSettings();

        public Material overrideMaterial = null;
        public int overrideMaterialPassIndex = 0;

        public bool overrideDepthState = false;
        public CompareFunction depthCompareFunction = CompareFunction.LessEqual;
        public bool enableWrite = true;

        public StencilStateData stencilSettings = new StencilStateData();
    }

    [System.Serializable]
    public class FilterSettings
    {
        // TODO: expose opaque, transparent, all ranges as drop down
        public RenderQueueType RenderQueueType;
        public LayerMask LayerMask;
        public string[] PassNames;

        public FilterSettings()
        {
            RenderQueueType = RenderQueueType.Opaque;
            LayerMask = 0;
        }
    }

    public RenderObjectsSettings settings = new RenderObjectsSettings();

    MyRenderPass renderObjectsPass;

    public override void Create()
    {
        FilterSettings filter = settings.filterSettings;
        renderObjectsPass = new MyRenderPass(settings.passTag, settings.Event, filter.PassNames,
            filter.RenderQueueType, filter.LayerMask);

        renderObjectsPass.overrideMaterial = settings.overrideMaterial;
        renderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;

        if (settings.overrideDepthState)
            renderObjectsPass.SetDetphState(settings.enableWrite, settings.depthCompareFunction);

        if (settings.stencilSettings.overrideStencilState)
            renderObjectsPass.SetStencilState(settings.stencilSettings.stencilReference,
                settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(renderObjectsPass);
    }
}