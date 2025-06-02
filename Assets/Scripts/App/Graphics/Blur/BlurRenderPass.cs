using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine;

namespace App.Graphics.Blur
{
    public class BlurRenderPass : ScriptableRenderPass
    {
        private Material material;
        private BlurSettings blurSettings;

        private RTHandle source;
        private RTHandle blurTex;

        public bool Setup(ScriptableRenderer renderer)
        {
            //source = renderer.cameraColorTargetHandle;
            //blurSettings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
            //renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

            //if (blurSettings != null && blurSettings.IsActive())
            //{
            //    material = new Material(Shader.Find("PostProcessing/Blur"));
            //    return true;
            //}

            return false;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (blurSettings == null || !blurSettings.IsActive())
                return;

            blurTex = RTHandles.Alloc(
                cameraTextureDescriptor.width,
                cameraTextureDescriptor.height,
                colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                name: "_BlurTex"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //if (blurSettings == null || !blurSettings.IsActive())
            //    return;

            //CommandBuffer cmd = CommandBufferPool.Get("Blur Post Process");

            //int gridSize = Mathf.CeilToInt(blurSettings.strength.value * 6.0f);
            //if (gridSize % 2 == 0) gridSize++;

            //material.SetInteger("_GridSize", gridSize);
            //material.SetFloat("_Spread", blurSettings.strength.value);

            //cmd.Blit(source, blurTex, material, 0);
            //cmd.Blit(blurTex, source, material, 1);

            //context.ExecuteCommandBuffer(cmd);
            //cmd.Clear();
            //CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (blurTex != null)
            {
                RTHandles.Release(blurTex);
                blurTex = null;
            }
        }
    }
}
