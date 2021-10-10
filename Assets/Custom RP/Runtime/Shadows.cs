using UnityEngine;
using  UnityEngine.Rendering;

public class Shadows
{
    const int maxShadowedDirectionalLightCount = 4,maxCascades=4;
    int ShadowedDirectionalLightCount;
    const string bufferName = "shadows";

    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
        cascadeCountId = Shader.PropertyToID("_CascadeCount"),
        cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
        shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

    private static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];

    private static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount*maxCascades ];

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
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
        int tiles = ShadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < ShadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split,tileSize);
        }

        float f = 1f - settings.directional.cascadeFade;
        //向Shader传递数据
        buffer.SetGlobalInt(cascadeCountId,settings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSpheresId,cascadeCullingSpheres);
        buffer.SetGlobalMatrixArray(dirShadowMatricesId,dirShadowMatrices);
        buffer.SetGlobalVector(shadowDistanceFadeId,new Vector4(
            1f/settings.maxDistance,
            1f/settings.distanceFade,
            1f/(1f-f*f)));
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows(int index,int split, int tileSize)
    {
        ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        //计算级联数与偏移
        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.CascadeRatios;

        //为每个级联进行绘制
        for (int i = 0; i < cascadeCount; i++)
        {
            //通过ComputeDirectionalShadowMatricesAndCullingPrimitives计算:视图矩阵、投影矩阵与ShadowSplitData结构
            //第一个参数是可见光指数。接下来的三个参数是两个整数和一个Vector3，它们控制阴影级联。稍后我们将处理级联，因此现在使用零，一和零向量。然后是纹理尺寸，我们需要使用平铺尺寸。第六个参数是靠近平面的阴影，我们现在将其忽略并将其设置为零。
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            shadowSettings.splitData = splitData;
            //因为剔除球都是等效的，所以只需要在循环的第一次计算即可。
            if (index == 0)
            {
                //我们需要着色器中的球体来检查表面碎片是否位于其中，这可以通过将距球体中心的平方距离与其半径进行比较来实现。因此，让我们存储平方半径，这样就不必在着色器中计算它了。
                Vector4 cullingSphere = splitData.cullingSphere;
                cullingSphere.w *= cullingSphere.w;
                cascadeCullingSpheres[i] = cullingSphere;
            }

            int tileIndex = tileOffset + i;
            
            //存储 世界空间=》方向光空间矩阵
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projectionMatrix * viewMatrix, 
                SetTileViewport(tileIndex, split, tileSize),split);
            buffer.SetViewProjectionMatrices(viewMatrix,projectionMatrix);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
        }
    }

    Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, int split) {
        if (SystemInfo.usesReversedZBuffer) {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

    Vector2 SetTileViewport(int index, int split,float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x*tileSize,offset.y*tileSize,tileSize,tileSize));
        return offset;
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

    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount &&
            light.shadows!=LightShadows.None && light.shadowStrength>0f && 
            cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b)) 
        {
            ShadowedDirectionalLights[ShadowedDirectionalLightCount] = new ShadowedDirectionalLight() { visibleLightIndex = visibleLightIndex};
            return new Vector2(light.shadowStrength, settings.directional.cascadeCount * ShadowedDirectionalLightCount++);
        }
        return Vector2.zero;
    }
}
