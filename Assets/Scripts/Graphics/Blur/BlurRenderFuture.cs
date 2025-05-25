using Graphics.Blur;
using UnityEngine.Rendering.Universal;

namespace Graphics.Blur
{
    public class BlurRendererFeature : ScriptableRendererFeature
    {
        BlurRenderPass blurRenderPass;

        public override void Create()
        {
            blurRenderPass = new BlurRenderPass();
            name = "Blur";
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (blurRenderPass.Setup(renderer))
            {
                renderer.EnqueuePass(blurRenderPass);
            }
        }
    }
}