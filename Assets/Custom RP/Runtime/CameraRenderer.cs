using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    CullingResults cullingResults;

    const string bufferName = "Render Camera";
    private CommandBuffer buffer = new CommandBuffer {name = bufferName};

    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

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
        DrawUnsupportedShaders();
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
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
        context.DrawSkybox(camera);
    
        sortingSettings.criteria=SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings=sortingSettings;
        filteringSettings.renderQueueRange=RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults,ref drawingSettings,ref filteringSettings);
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