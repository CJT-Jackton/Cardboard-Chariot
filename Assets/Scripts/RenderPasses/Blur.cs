using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experiemntal.Rendering.Universal
{
    public class Blur : ScriptableRendererFeature
    {
        [System.Serializable]
        public class BlurSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
            
            public Material blitMaterial = null;
            public int blitMaterialPassIndex = -1;
            public Target destination = Target.Color;
            public string srcTextureId = "_BlitPassTexture";
            public string dstTextureId = "_BlitPassTexture";
        }
        
        public enum Target
        {
            Color,
            Texture
        }

        public BlurSettings settings = new BlurSettings();
        RenderTargetHandle m_SrcRenderTextureHandle;
        RenderTargetHandle m_RenderTextureHandle;

        BlurPass blurPass;

        public override void Create()
        {
            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
            blurPass = new BlurPass(settings.Event, settings.blitMaterial, settings.blitMaterialPassIndex, name);
            m_SrcRenderTextureHandle.Init(settings.srcTextureId);
            m_RenderTextureHandle.Init(settings.dstTextureId);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            //var src = renderer.cameraColorTarget;
            var src = m_SrcRenderTextureHandle.Identifier();
            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : m_RenderTextureHandle;

            if (settings.blitMaterial == null)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            blurPass.Setup(src, dest);
            renderer.EnqueuePass(blurPass);
        }
    }
}

