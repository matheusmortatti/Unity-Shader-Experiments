Shader "Hidden/Scan"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float4 ray : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 uv_depth : TEXCOORD1;
				float4 interpolatedRay : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

				o.uv_depth = v.uv.xy;
				o.interpolatedRay = v.ray;

                return o;
            }

            sampler2D _MainTex;
			sampler2D_float _CameraDepthTexture;

			float4 _WorldSpaceScannerPos;
			float _ScanDistance;
			float _ScanWidth;

			float4 horizontalBars(float y)
			{
				return 1 - saturate(round(abs(frac(y * 100) * 2)));
			}

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = tex2D(_MainTex, i.uv);

				float rawDepth = DecodeFloatRG(tex2D(_CameraDepthTexture, i.uv_depth));
				float linearDepth = Linear01Depth(rawDepth);
				float4 wsDir = linearDepth * i.interpolatedRay;
				float3 wsPos = _WorldSpaceCameraPos + wsDir;

				float dist = distance(wsPos, _WorldSpaceScannerPos);

				if (dist < _ScanDistance && dist > _ScanDistance - _ScanWidth && linearDepth < 1)
				{
					float scanCol = 1 - (_ScanDistance - dist) / _ScanWidth;
					col.rgb += scanCol + horizontalBars(i.uv.y);
				}

                // just invert the colors
                return col;
            }
            ENDCG
        }
    }
}
