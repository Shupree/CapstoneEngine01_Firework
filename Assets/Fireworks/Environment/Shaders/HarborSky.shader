Shader "Capstone/Harbor Night Sky"
{
    Properties { _Zenith("Zenith", Color)=(0.004,0.009,0.025,1) _Horizon("Horizon", Color)=(0.065,0.075,0.12,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _Zenith,_Horizon;
            struct A { float4 p:POSITION; };
            struct V { float4 p:SV_POSITION; float3 dir:TEXCOORD0; };
            V Vert(A a) { V o; o.p=TransformObjectToHClip(a.p.xyz);o.dir=a.p.xyz;return o; }
            half4 Frag(V i):SV_Target
            {
                float3 d=normalize(i.dir);
                half3 col=lerp(_Horizon.rgb,_Zenith.rgb,pow(saturate(d.y),0.4));
                float2 uv=float2(atan2(d.z,d.x),asin(d.y))*260;
                float2 cell=floor(uv); float2 local=frac(uv)-0.5;
                float hash=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
                float star=step(0.996,hash)*pow(saturate(1-length(local)*3.5),8)*smoothstep(0.08,0.4,d.y);
                return half4(col+star*0.32,1);
            }
            ENDHLSL
        }
    }
}
