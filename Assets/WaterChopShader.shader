Shader "Custom/WaterChopURP"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        
        [Header(Flow Settings)]
        _Speed ("Flow Speed", Float) = 15.0
        _Amplitude ("Wave Strength", Float) = 0.015
        _Frequency ("Wave Density", Float) = 25.0
        
        [Header(Mask Settings)]
        _StartHeight ("Effect Start Height (0 to 1)", Float) = 0.5 
        _EdgeWidth ("Edge Protection (0 to 0.5)", Float) = 0.1 
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float _Speed;
                float _Amplitude;
                float _Frequency;
                float _StartHeight;
                float _EdgeWidth;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                // Không di chuyển vertex nữa, giữ nguyên vị trí khung vuông
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 modifiedUV = i.uv;

                // 1. Tạo sóng cuộn liên tục từ trái sang phải
                // Dấu trừ (-) giúp nước trôi về bên phải. Đổi thành dấu (+) nếu muốn trôi về trái.
                float flow = sin(i.uv.x * _Frequency - _Time.y * _Speed);
                float wave = flow * _Amplitude;

                // 2. Tạo mặt nạ bảo vệ (Masks)
                // Bảo vệ phần đất bên dưới
                float maskY = smoothstep(_StartHeight - 0.1, _StartHeight + 0.1, i.uv.y);
                
                // Bảo vệ 2 bờ tường đất 2 bên
                float maskXLeft = smoothstep(0.0, _EdgeWidth, i.uv.x);
                float maskXRight = 1.0 - smoothstep(1.0 - _EdgeWidth, 1.0, i.uv.x);
                float maskX = maskXLeft * maskXRight;

                // 3. Ép hiệu ứng biến dạng vào Pixel (chỉ áp dụng nơi có nước)
                modifiedUV.x += wave * maskY * maskX;

                // Hiển thị hình ảnh với Pixel đã bị bẻ cong
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, modifiedUV) * i.color;
                return col;
            }
            ENDHLSL
        }
    }
}