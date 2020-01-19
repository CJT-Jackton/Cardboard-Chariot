using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Bloom render texture renderer feature.
    /// 
    /// This renderer feature apply unity built-in bloom effect on an render texture,
    /// then store the bloom result into the destination render texture for later use.
    /// </summary>
    public sealed class BloomRenderTexture : ScriptableRendererFeature
    {
        [System.Serializable]
        public class BloomSettings
        {
            public string passTag = "BloomFeature";
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public Source source = Source.Color;
            public string srcTextureId = "_BloomSrcTex";
            public Target destination = Target.NewRenderTexture;
            public string dstTextureId = "_BloomTex";

            public CustomBloomSettings customBloomSettings = new CustomBloomSettings();
        }

        public enum Source
        {
            Color,
            RenderTexture
        }

        public enum Target
        {
            NewRenderTexture
        }

        [System.Serializable]
        public class CustomBloomSettings
        {
            [Min(0f), Tooltip("Strength of the bloom filter. Values higher than 1 will make bloom contribute more energy to the final render.")]
            public float intensity = 0.5f;

            [Min(0f), Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
            public float threshold = 1f;

            [Range(0f, 1f), Tooltip("Makes transitions between under/over-threshold gradual. 0 for a hard threshold, 1 for a soft threshold).")]
            public float softKnee = 0.5f;

            [Tooltip("Clamps pixels to control the bloom amount. Value is in gamma-space.")]
            public float clamp = 65472f;

            [Range(1f, 10f), Tooltip("Changes the extent of veiling effects. For maximum quality, use integer values. Because this value changes the internal iteration count, You should not animating it as it may introduce issues with the perceived radius.")]
            public float diffusion = 7f;

            [Range(-1f, 1f), Tooltip("Distorts the bloom to give an anamorphic look. Negative values distort vertically, positive values distort horizontally.")]
            public float anamorphicRatio = 0f;

            [ColorUsage(false, true), Tooltip("Global tint of the bloom filter.")]
            public Color color = Color.white;

            [Tooltip("Boost performance by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms but can also provide a nice performance boost on desktops and consoles.")]
            public bool fastMode = false;

            [Min(0f), Tooltip("The intensity of the lens dirtiness.")]
            public float dirtIntensity = 0f;
        }

        public BloomSettings settings = new BloomSettings();

        RenderTargetHandle m_SrcRenderTextureHandle;
        RenderTargetHandle m_CameraColorTextureHandle;

        BloomPass bloomPass;

        BloomPass.BloomSetting bloomSetting = new BloomPass.BloomSetting();

        public override void Create()
        {
            bloomPass = new BloomPass(settings.Event, name);
            m_SrcRenderTextureHandle.Init(settings.srcTextureId);
            m_CameraColorTextureHandle.Init("_CameraColorTexture");
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var src = (settings.source == Source.Color) ? renderer.cameraColorTarget : m_SrcRenderTextureHandle.Identifier();

            bloomPass.Setup(src, settings.dstTextureId);
            SetupBloomProps();
            renderer.EnqueuePass(bloomPass);
        }

        public void SetupBloomProps()
        {
            bloomSetting.intensity = settings.customBloomSettings.intensity;
            bloomSetting.threshold = settings.customBloomSettings.threshold;
            bloomSetting.softKnee = settings.customBloomSettings.softKnee;
            bloomSetting.clamp = settings.customBloomSettings.clamp;
            bloomSetting.diffusion = settings.customBloomSettings.diffusion;
            bloomSetting.anamorphicRatio = settings.customBloomSettings.anamorphicRatio;
            bloomSetting.color = settings.customBloomSettings.color;
            bloomSetting.fastMode = settings.customBloomSettings.fastMode;
            bloomSetting.dirtIntensity = settings.customBloomSettings.dirtIntensity;

            bloomPass.SetBloomProps(bloomSetting);
        }
    }
}

