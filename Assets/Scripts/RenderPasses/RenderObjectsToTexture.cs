namespace UnityEngine.Rendering.LWRP
{
    public enum RenderQueueType
    {
        Opaque,
        Transparent,
    }

    /// <summary>
    /// Render object to texture render feature.
    /// 
    /// This renderer feature render the selected objects into a render texture target
    /// other than the camera color buffer for later usage.
    /// </summary>
    public class RenderObjectsToTexture : ScriptableRendererFeature
    {
        [System.Serializable]
        public class RenderObjectsToTextureSettings
        {
            public string passTag = "RenderObjectsFeature";
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public Source source = Source.Color;
            public string srcTextureId = "_BlitPassSrcTexture";
            public Target destination = Target.Color;
            public string dstTextureId = "_BlitPassDstTexture";

            public FilterSettings filterSettings = new FilterSettings();

            public Material overrideMaterial = null;
            public int overrideMaterialPassIndex = 0;

            public bool overrideDepthState = false;
            public CompareFunction depthCompareFunction = CompareFunction.LessEqual;
            public bool enableWrite = true;

            public StencilStateData stencilSettings = new StencilStateData();

            public CustomCameraSettings cameraSettings = new CustomCameraSettings();
        }

        public enum Source
        {
            Color,
            RenderTexture
        }

        public enum Target
        {
            Color,
            ExistedRenderTexture,
            NewRenderTexture
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

        [System.Serializable]
        public class CustomCameraSettings
        {
            public bool overrideCamera = false;
            public bool restoreCamera = true;
            public Vector4 offset;
            public float cameraFieldOfView = 60.0f;
        }

        public RenderObjectsToTextureSettings settings = new RenderObjectsToTextureSettings();

        RenderTargetHandle m_SrcRenderTextureHandle;
        RenderTargetHandle m_DstRenderTextureHandle;
        RenderTargetHandle m_CameraColorTextureHandle;

        RenderObjectsToTexturePass renderObjectsPass;

        public override void Create()
        {
            FilterSettings filter = settings.filterSettings;
            renderObjectsPass = new RenderObjectsToTexturePass(settings.passTag, settings.Event, filter.PassNames,
                filter.RenderQueueType, filter.LayerMask, settings.cameraSettings);

            renderObjectsPass.overrideMaterial = settings.overrideMaterial;
            renderObjectsPass.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;

            if (settings.overrideDepthState)
                renderObjectsPass.SetDetphState(settings.enableWrite, settings.depthCompareFunction);

            if (settings.stencilSettings.overrideStencilState)
                renderObjectsPass.SetStencilState(settings.stencilSettings.stencilReference,
                    settings.stencilSettings.stencilCompareFunction, settings.stencilSettings.passOperation,
                    settings.stencilSettings.failOperation, settings.stencilSettings.zFailOperation);

            m_SrcRenderTextureHandle.Init(settings.srcTextureId);
            m_DstRenderTextureHandle.Init(settings.dstTextureId);
            m_CameraColorTextureHandle.Init("_CameraColorTexture");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var src = (settings.source == Source.Color) ? m_CameraColorTextureHandle.Identifier() : m_SrcRenderTextureHandle.Identifier();
            var dest = (settings.destination == Target.Color) ? m_CameraColorTextureHandle : m_DstRenderTextureHandle;

            renderObjectsPass.Setup(src, dest, settings.destination == Target.NewRenderTexture);

            renderer.EnqueuePass(renderObjectsPass);
        }
    }
}

