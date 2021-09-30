using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    CullingResults cullingResults;

    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    const string bufferName = "Render Camera";
    private CommandBuffer buffer = new CommandBuffer {name = bufferName};

    public void Render(ScriptableRenderContext context,Camera camera)
    {
        this.context=context;
        this.camera=camera;

        if (!Cull())
        {
            return;
        }

        Setup();   
        DrawVisibleGeometry();
        Submit();
    }

    bool Cull()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        buffer.BeginSample(bufferName);
        buffer.ClearRenderTarget(true,true,Color.clear);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry()
    {
        var sortingSettings = new SortingSettings(camera){criteria = SortingCriteria.CommonOpaque};
        var drawingSettings = new DrawingSettings(unlitShaderTagId,sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.all);
        
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
        
        context.DrawSkybox(camera);
    }

    void Submit()
    {
        buffer.EndSample(bufferName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}