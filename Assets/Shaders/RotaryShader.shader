Shader "thquinn/RotaryShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Theta("Theta", Range(0, 6.28)) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"}
        LOD 100

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Theta;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                i.uv *= 2;
                i.uv -= float2(1, 1);
                float theta = atan2(i.uv.y, -i.uv.x) + 3.14;
                theta = (theta + 1.57) % 6.28;
                float t = smoothstep(_Theta, _Theta - .02, theta) / 8;
                fixed4 col = i.color;
                col.r = lerp(col.r, 0, t);
                col.g = lerp(col.g, 0, t);
                col.b = lerp(col.b, 0, t);
                return col;
            }
            ENDCG
        }
    }
}
