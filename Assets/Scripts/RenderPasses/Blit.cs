namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Blit renderer feature.
    /// 
    /// This renderer feature can blit the camera color buffer or selected render texture 
    /// to one of the camera color buffer, an existed render texture, or an new temporary
    /// render texture.
    /// </summary>
    public class Blit : ScriptableRendererFeature
    {
        [System.Serializable]
        public class BlitSettings
        {
            public string passTag = "BlitFeature";
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
            
            public Material blitMaterial = null;
            public int blitMaterialPassIndex = -1;
            public Source source = Source.Color;
            public string srcTextureId = "_BlitPassSrcTexture";
            public Target destination = Target.Color;
            public string dstTextureId = "_BlitPassDstTexture";
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

        public BlitSettings settings = new BlitSettings();
        RenderTargetHandle m_SrcRenderTextureHandle;
        RenderTargetHandle m_DstRenderTextureHandle;
        RenderTargetHandle m_CameraColorTextureHandle;

        BlitPass blitPass;

        public override void Create()
        {
            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
            blitPass = new BlitPass(settings.Event, settings.blitMaterial, settings.blitMaterialPassIndex, name);
            m_SrcRenderTextureHandle.Init(settings.srcTextureId);
            m_DstRenderTextureHandle.Init(settings.dstTextureId);
            m_CameraColorTextureHandle.Init("_CameraColorTexture");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var src = (settings.source == Source.Color) ? renderer.cameraColorTarget : m_SrcRenderTextureHandle.Identifier();
            var dest = (settings.destination == Target.Color) ? m_CameraColorTextureHandle : m_DstRenderTextureHandle;

            blitPass.Setup(src, dest, settings.destination == Target.NewRenderTexture);
            renderer.EnqueuePass(blitPass);
        }
    }
}

