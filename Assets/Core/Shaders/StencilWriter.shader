Shader "Custom/StencilWriter"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1"}

        Pass
        {
            Blend Zero One
            ZTest LEqual
            ZWrite Off
            Stencil
            {
                Ref 1
                Comp Always
                ZFail Replace
            }
        }
    }
}
