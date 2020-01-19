using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public enum RenderQueueType
    {
        Opaque,
        Transparent,
    }

    public class Outline : ScriptableRendererFeature
    {
        [System.Serializable]
        public class OutlineSettings
        {
            public string passTag = "OutlineFeature";
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public FilterSettings filterSettings = new FilterSettings();

            public Material blitMaterial = null;
            public int blitMaterialPassIndex = -1;
            public Target destination = Target.Color;
            public string textureId = "_OutlinePassTexture";
        }

        [System.Serializable]
        public class CustomCameraSettings
        {
            public bool overrideCamera = false;
            public bool restoreCamera = true;
            public Vector4 offset;
            public float cameraFieldOfView = 60.0f;
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

        public enum Target
        {
            Color,
            Texture
        }

        public OutlineSettings settings = new OutlineSettings();
        RenderTargetHandle m_RenderTextureHandle;

        OutlinePass outlinePass;

        public override void Create()
        {
            FilterSettings filter = settings.filterSettings;
            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
            outlinePass = new OutlinePass(settings.Event, settings.blitMaterial, settings.blitMaterialPassIndex, name, filter.LayerMask);
            m_RenderTextureHandle.Init(settings.textureId);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var src = renderer.cameraColorTarget;
            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : m_RenderTextureHandle;

            if (settings.blitMaterial == null)
            {
                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            outlinePass.Setup(src, dest);
            renderer.EnqueuePass(outlinePass);
        }
    }
}
