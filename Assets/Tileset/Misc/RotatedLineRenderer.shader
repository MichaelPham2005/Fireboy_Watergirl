Shader "Custom/RotatedLineRenderer"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // IMPORTANT: Apply Tiling and Offset to the raw LineRenderer UVs FIRST.
                // This ensures that modifying the Material's Tiling X or Offset X in C#
                // correctly scales/scrolls along the LENGTH of the line.
                float2 scaledUV = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // NOW we rotate the UVs 90 degrees so the vertical sprite aligns with the line.
                // scaledUV.x is the length of the line. scaledUV.y is the width of the line.
                float2 rotatedUV = float2(scaledUV.y, 1.0 - scaledUV.x);
                
                o.texcoord = rotatedUV;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.texcoord) * i.color;
                return c;
            }
            ENDCG
        }
    }
}
