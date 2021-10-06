using UnityEngine;
using  UnityEngine.Rendering;

public class Shadows
{
    const int maxShadowedDirectionalLightCount = 1;
    int ShadowedDirectionalLightCount;
    const string bufferName = "shadows";
    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    CullingResults cullingResults;

    ShadowSettings settings;

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }

    private ShadowedDirectionalLight[] ShadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    
    public void Setup(
        ScriptableRenderContext context,CullingResults cullingResults,
        ShadowSettings settings
    )
    {
        ShadowedDirectionalLightCount = 0;
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;
    }

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
    }
    
    /*
     * 使用"_DirectionalShadowAtlas"作为阴影图集
     */
    void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear,
            RenderTextureFormat.Shadowmap);
        //请求渲染纹理后，Shadows.Render还必须指示GPU渲染到该纹理而不是相机的目标。
        buffer.SetRenderTarget(dirShadowAtlasId,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true,false,Color.clear);
        ExecuteBuffer();
    }

    /*
     * 阴影图集释放函数
     */
    public void Clearup()
    {
        if (ShadowedDirectionalLightCount>0)
        {
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows!=LightShadows.None && light.shadowStrength>0f && 
            cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b)) 
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount++] = new ShadowedDirectionalLight() { visibleLightIndex = visibleLightIndex};
        }
    }
}
