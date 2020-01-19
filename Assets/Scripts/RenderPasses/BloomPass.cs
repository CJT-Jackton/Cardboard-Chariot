using System.Collections.Generic;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Apply bloom effect on a render texture.
    /// 
    /// TODO: Add bloom dirt texture.
    /// </summary>
    internal class BloomPass : ScriptableRenderPass
    {
        public struct BloomSetting
        {
            internal float intensity;
            internal float threshold;
            internal float softKnee;
            internal float clamp;
            internal float diffusion;
            internal float anamorphicRatio;
            internal Color color;
            internal bool fastMode;

            internal float dirtIntensity;
        }

        BloomSetting settings;

        // Bloom
        enum Pass
        {
            Prefilter13,
            Prefilter4,
            Downsample13,
            Downsample4,
            UpsampleTent,
            UpsampleBox,
            DebugOverlayThreshold,
            DebugOverlayTent,
            DebugOverlayBox
        }

        // [down,up]
        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16; // Just to make sure we handle 64k screens... Future-proof!

        struct Level
        {
            internal int down;
            internal int up;
        }

        static Mesh s_FullscreenTriangle;

        /// <summary>
        /// A fullscreen triangle mesh.
        /// </summary>
        public static Mesh fullscreenTriangle
        {
            get
            {
                if (s_FullscreenTriangle != null)
                    return s_FullscreenTriangle;

                s_FullscreenTriangle = new Mesh { name = "Fullscreen Triangle" };

                // Because we have to support older platforms (GLES2/3, DX9 etc) we can't do all of
                // this directly in the vertex shader using vertex ids :(
                s_FullscreenTriangle.SetVertices(new List<Vector3>
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(-1f,  3f, 0f),
                    new Vector3( 3f, -1f, 0f)
                });
                s_FullscreenTriangle.SetIndices(new[] { 0, 1, 2 }, MeshTopology.Triangles, 0, false);
                s_FullscreenTriangle.UploadMeshData(false);

                return s_FullscreenTriangle;
            }
        }

        public enum RenderTarget
        {
            Color,
            RenderTexture,
        }

        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source { get; set; }
        private string dstTextureId { get; set; }

        RenderTargetHandle m_TemporaryColorTexture;
        string m_ProfilerTag;

        /// <summary>
        /// Initialize the temporary render texture identifiers.
        /// </summary>
        public void Init()
        {
            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down = Shader.PropertyToID("_BloomMipDown" + i),
                    up = Shader.PropertyToID("_BloomMipUp" + i)
                };
            }
        }

        /// <summary>
        /// Create the CopyColorPass
        /// </summary>
        public BloomPass(RenderPassEvent renderPassEvent, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            m_ProfilerTag = tag;
            m_TemporaryColorTexture.Init("_TemporaryColorTexture");

            Init();
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="dstTextureId">Destination Render Target Identifier</param>
        public void Setup(RenderTargetIdentifier source, string dstTextureId)
        {
            this.source = source;
            this.dstTextureId = dstTextureId;
        }

        public void SetBloomProps(BloomSetting setting)
        {
            settings = setting;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            cmd.BeginSample("BloomPyramid");

            Shader shader = Shader.Find("Hidden/PostProcessing/Bloom");
            var shaderName = shader.name;
            var bloomMaterial = new Material(shader)
            {
                name = string.Format("PostProcess - {0}", shaderName.Substring(shaderName.LastIndexOf('/') + 1)),
                hideFlags = HideFlags.DontSave
            };

            MaterialPropertyBlock properties = new MaterialPropertyBlock();

            // Apply auto exposure adjustment in the prefiltering pass
            //properties.SetTexture("_AutoExposureTex", context.autoExposureTexture);
            properties.SetTexture("_AutoExposureTex", Texture2D.whiteTexture);

            // Negative anamorphic ratio values distort vertically - positive is horizontal
            float ratio = Mathf.Clamp(settings.anamorphicRatio, -1, 1);
            float rw = ratio < 0 ? -ratio : 0f;
            float rh = ratio > 0 ? ratio : 0f;

            // Do bloom on a half-res buffer, full-res doesn't bring much and kills performances on
            // fillrate limited platforms
            int tw = Mathf.FloorToInt(renderingData.cameraData.cameraTargetDescriptor.width / (2f - rw));
            int th = Mathf.FloorToInt(renderingData.cameraData.cameraTargetDescriptor.height / (2f - rh));
            // TODO:
            //bool singlePassDoubleWide = (context.stereoActive && (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) && (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
            bool singlePassDoubleWide = false;
            int tw_stereo = singlePassDoubleWide ? tw * 2 : tw;

            // Determine the iteration count
            int s = Mathf.Max(tw, th);
            float logs = Mathf.Log(s, 2f) + Mathf.Min(settings.diffusion, 10f) - 10f;
            int logs_i = Mathf.FloorToInt(logs);
            int iterations = Mathf.Clamp(logs_i, 1, k_MaxPyramidSize);
            float sampleScale = 0.5f + logs - logs_i;
            properties.SetFloat("_SampleScale", sampleScale);

            // Prefiltering parameters
            float lthresh = Mathf.GammaToLinearSpace(settings.threshold);
            float knee = lthresh * settings.softKnee + 1e-5f;
            var threshold = new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
            properties.SetVector("_Threshold", threshold);
            float lclamp = Mathf.GammaToLinearSpace(settings.clamp);
            properties.SetVector("_Params", new Vector4(lclamp, 0f, 0f, 0f));

            int qualityOffset = settings.fastMode ? 1 : 0;

            // Descriptor
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            // Downsample
            var lastDown = source;
            for (int i = 0; i < iterations; i++)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                int pass = i == 0
                    ? (int)Pass.Prefilter13 + qualityOffset
                    : (int)Pass.Downsample13 + qualityOffset;

                desc.width = tw_stereo;
                desc.height = th;

                //context.GetScreenSpaceTemporaryRT(cmd, mipDown, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_stereo, th);
                //context.GetScreenSpaceTemporaryRT(cmd, mipUp, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_stereo, th);
                cmd.GetTemporaryRT(mipDown, desc, FilterMode.Bilinear);
                cmd.GetTemporaryRT(mipUp, desc, FilterMode.Bilinear);

                //cmd.BlitFullscreenTriangle(lastDown, mipDown, sheet, pass);
                cmd.SetGlobalTexture("_MainTex", lastDown);
                cmd.SetRenderTarget(mipDown);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, bloomMaterial, 0, pass, properties);

                lastDown = mipDown;
                tw_stereo = (singlePassDoubleWide && ((tw_stereo / 2) % 2 > 0)) ? 1 + tw_stereo / 2 : tw_stereo / 2;
                tw_stereo = Mathf.Max(tw_stereo, 1);
                th = Mathf.Max(th / 2, 1);
            }

            // Upsample
            int lastUp = m_Pyramid[iterations - 1].down;
            for (int i = iterations - 2; i >= 0; i--)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                cmd.SetGlobalTexture("_BloomTex", mipDown);

                //cmd.BlitFullscreenTriangle(lastUp, mipUp, sheet, (int)Pass.UpsampleTent + qualityOffset);
                cmd.SetGlobalTexture("_MainTex", lastUp);
                cmd.SetRenderTarget(mipUp);
                cmd.DrawMesh(fullscreenTriangle, Matrix4x4.identity, bloomMaterial, 0, (int)Pass.UpsampleTent, properties);

                lastUp = mipUp;
            }

            var linearColor = settings.color.linear;
            //float intensity = RuntimeUtilities.Exp2(settings.intensity / 10f) - 1f;
            float intensity = Mathf.Pow(2, settings.intensity / 10f) - 1f;
            var shaderSettings = new Vector4(sampleScale, settings.intensity, 0, iterations);

            properties.SetVector("_ColorIntensity", new Vector4(linearColor.r, linearColor.g, linearColor.b, intensity));

            // Shader properties
            if (settings.fastMode)
                CoreUtils.SetKeyword(bloomMaterial, "BLOOM_LOW", settings.fastMode);
            else
                CoreUtils.SetKeyword(bloomMaterial, "BLOOM", !settings.fastMode);

            //properties.SetVector("_Bloom_DirtTileOffset", dirtTileOffset);
            //properties.SetVector("_Bloom_Settings", shaderSettings);
            //properties.SetColor("_Bloom_Color", linearColor);
            //properties.SetTexture("_Bloom_DirtTex", dirtTexture);
            //cmd.SetGlobalTexture("_BloomTex", lastUp);
            cmd.SetGlobalTexture(dstTextureId, lastUp);

            // Cleanup
            for (int i = 0; i < iterations; i++)
            {
                if (m_Pyramid[i].down != lastUp)
                    cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                if (m_Pyramid[i].up != lastUp)
                    cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
            }

            cmd.EndSample("BloomPyramid");

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (false)
            {
                cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
            }
        }
    }
}
