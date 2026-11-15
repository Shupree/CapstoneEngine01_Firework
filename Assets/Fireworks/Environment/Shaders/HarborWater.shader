Shader "Capstone/Harbor Water"
{
    Properties
    {
        _BaseMap("Supplied Water Texture", 2D) = "white" {}
        _DeepColor("Deep Water", Color) = (0.008,0.025,0.045,1)
        _HorizonColor("Sky Reflection", Color) = (0.08,0.13,0.20,1)
        _WaveScale("Texture World Scale", Float) = 0.025
        _FlowSpeed("Current Speed", Float) = 0.025
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _DeepColor, _HorizonColor;
            float _WaveScale, _FlowSpeed;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; half fog:TEXCOORD1; };
            Varyings Vert(Attributes a)
            {
                Varyings o; o.world=TransformObjectToWorld(a.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.world); o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.world.xz*_WaveScale;
                float t=_Time.y*_FlowSpeed;
                half a=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv+float2(t,t*0.27)).g;
                half b=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*0.73+float2(-t*0.4,t*0.6)).g;
                float3 n=normalize(float3(sin(i.world.x*0.35+_Time.y*0.7)*0.045,1,cos(i.world.z*0.4+_Time.y*0.5)*0.04));
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float fresnel=pow(1-saturate(dot(n,view)),4);
                half3 color=lerp(_DeepColor.rgb,_HorizonColor.rgb,fresnel*0.7);
                color+=(a*b)*half3(0.055,0.11,0.15);
                float streak=pow(saturate(sin(i.world.x*0.42+sin(i.world.z*0.16+_Time.y)*0.6)),18);
                float distanceFade=exp(-abs(i.world.z-65)*0.012);
                color+=half3(0.14,0.10,0.055)*streak*distanceFade*pow(saturate(a+b-0.6),2);
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
