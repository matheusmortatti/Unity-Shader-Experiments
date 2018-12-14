Shader "Custom/Waves"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [NoScaleOffset] _FlowMap ("Flow (RG, A noise)", 2D) = "black" {}
        // [NoScaleOffset] _FlowNormalMap ("Flow Normal", 2D) = "bump" {}
        [NoScaleOffset] _DerivHeightMap ("Derig (AG) Height (B)", 2D) = "black" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        _UJump ("U jump per phase", Range(-0.25, 0.25)) = 0.25
        _VJump ("V jump per phase", Range(-0.25, 0.25)) = 0.25
        _Tiling ("Flow Tiling", Float) = 1
        _Speed ("Flow Speed", Float) = 1
        _FlowStrength ("Flow Strength", Float) = 1
        _HeightScale ("Height Scale", Float) = 1
        _HeightScaleModulated ("Height Scale, Modulated", Float) = 0.75

        _WaveA ("Wave A (dir (2D), steepness, wavelength)", Vector) = (1, 0, 0.5, 10)
        _WaveB ("Wave B (dir (2D), steepness, wavelength)", Vector) = (0, 1, 0.5, 10)
        _WaveC ("Wave C (dir (2D), steepness, wavelength)", Vector) = (1, 1, 0.5, 10)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard alpha vertex:vert addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _FlowMap;
        // sampler2D _FlowNormalMap;
        sampler2D _DerivHeightMap;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _UJump;
        float _VJump;
        float _Tiling;
        float _Speed;
        float _FlowStrength;
        float _HeightScale;
        float _HeightScaleModulated;

        float4 _WaveA;
        float4 _WaveB;
        float4 _WaveC;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float3 FlowUVW(float2 uv, float2 flowVector, float2 jump, float tiling, float time, bool flowB)
        {
            float phaseOffset = flowB ? 0.5 : 0;
            float progress = frac(time + phaseOffset);
            float3 uvw;
            uvw.xy = uv - flowVector * progress;
            uvw.xy *= tiling;
            uvw.xy += phaseOffset;
            uvw.xy += (time - progress) * jump;
            uvw.z = 1 - abs(1 - 2 * progress);
            return uvw;
        }

        float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
        {
            float steepness = wave.z;
            float wavelength = wave.w;
            float k = 2 * UNITY_PI / wavelength;
            float c = sqrt(9.8 / k);
            float2 d = normalize(wave.xy);
            float f = k * (dot(d, p.xz) - c * _Time.y);
            float a = steepness / k;

            tangent += float3(
                                1 - d.x * d.x * steepness * sin(f),
                                d.x * steepness * cos(f), 
                                -d.y * d.x * steepness * sin(f)
                        );
            binormal += float3(
                                -d.y * d.x * steepness * sin(f), 
                                d.y * steepness * cos(f), 
                                1 - d.y * d.y * steepness * sin(f)
                        );

            return float3(d.x * a * cos(f), a * sin(f), d.y * a * cos(f));
        }

        float3 UnpackDerivativeHeight(float4 textureData)
        {
            float3 dh = textureData.agb;
            dh.xy = dh.xy * 2 - 1;
            return dh;
        }

        void vert(inout appdata_full vertexData)
        {
            float3 gridPoint = vertexData.vertex.xyz;

            float3 tangent = float3(1, 0, 0);
            float3 binormal = float3(0, 0, 1);
            float3 p = gridPoint;

            p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
            p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
            p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);

            float3 normal = normalize(cross(binormal, tangent));

            vertexData.vertex.xyz = p;
            vertexData.normal = normal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {

            // Sample directional vector from flow map texture
            float3 flow = tex2D(_FlowMap, IN.uv_MainTex).rgb;
            flow.xy = flow.xy * 2 - 1;
            flow *= _FlowStrength;

            // Sample the alpha channel of the flow map texture
            // which gives us a noise value to offset the time variable
            // giving more variance
            float noise = tex2D(_FlowMap, IN.uv_MainTex).a;
            float time = _Time.y * _Speed + noise;

            // Jump value to create more complex looping animations
            float2 jump = float2(_UJump, _VJump);

            // Get displaced uv values
            float3 uvwA = FlowUVW(IN.uv_MainTex, flow.xy, jump, _Tiling,  time, false);
            float3 uvwB = FlowUVW(IN.uv_MainTex, flow.xy, jump, _Tiling, time, true);

            // Scale height of the height derivative map using a combination of
            // an uniform height scale and a modulated height by the velocity of the wave
            // (the faster the wave from the flow, the taller it is)
            float finalHeightScale = flow.z * _HeightScaleModulated + _HeightScale;

            // Unpack and assign height values for the normal map
            float3 dhA = UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvwA.xy)) * uvwA.z * finalHeightScale;
            float3 dhB = UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvwB.xy)) * uvwB.z * finalHeightScale;
            o.Normal = normalize(float3(-(dhA.xy + dhB.xy), 1));

            // Sample texture value with modified uv values
            fixed4 texA = tex2D (_MainTex, uvwA.xy) * uvwA.z;
            fixed4 texB = tex2D (_MainTex, uvwB.xy) * uvwB.z;


            fixed4 c = (texA + texB) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }

        ENDCG
    }
}
