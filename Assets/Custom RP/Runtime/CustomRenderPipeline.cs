using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer=new CameraRenderer();
    private bool useDynamicBatching, useGPUInstancing;
    
    public CustomRenderPipeline(bool useDynamicBatching,bool useGPUInstancing,bool useSPRBatcher)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSPRBatcher;
    }

    protected override void Render(ScriptableRenderContext context,Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context,camera,useDynamicBatching,useGPUInstancing);
        }
    }
}