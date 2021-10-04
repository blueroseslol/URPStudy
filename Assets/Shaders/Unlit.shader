Shader "Custom RP/Unlit"
{
    Properties
    {
        _BaseColor("Color",Color)=(1.0,1.0,1.0,1.0)
        
        [Enum(UnityEngine.Rendering.BlendedMode)]
        _SrcBlend("Src Blend",Float)=1
        
        [Enum(UnityEngine.Rendering.BlendedMode)]
        _DstBlend("Src Blend",Float)=0
        
        [Enum(Off,0,On,1)] 
        _ZWrite ("Z Write",Float)=1
    }
    SubShader
    {
       Pass
       {
           Blend [_SrcBlend] [_DstBlend]
           ZWrite [_ZWrite]
           
           HLSLPROGRAM

           #pragma multi_compile_instancing
           #pragma vertex UnlitPassVertex
           #pragma fragment UnlitPassFragment
           #include "UnlitPass.hlsl"
           
           ENDHLSL
       }
    }
}
