Shader "Custom/Underwater2D"
{
    Properties
    {
        _DistortionStrength ("Distortion Strength", Range(0, 0.05)) = 0.013
        _DistortionScale ("Distortion Scale", Float) = 22
        _DistortionSpeed ("Distortion Speed", Float) = 1.4

        _Tint ("Underwater Tint", Color) = (0.72, 0.76, 0.82, 1)
        _TintAmount ("Tint Amount", Range(0,1)) = 0.35

        // Both of these are driven by UnderwaterEffect at runtime.
        _WaterLevel ("Water Level (world Y)", Float) = 0
        _Strength ("Effect Strength", Range(0,1)) = 0

        _SurfaceSoftness ("Surface Softness", Float) = 0.35
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float worldY : TEXCOORD1;
            };

            // Everything the 2D Renderer drew up to the bound sorting layer.
            sampler2D _CameraSortingLayerTexture;

            float _DistortionStrength;
            float _DistortionScale;
            float _DistortionSpeed;

            fixed4 _Tint;
            float _TintAmount;

            float _WaterLevel;
            float _Strength;
            float _SurfaceSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                o.worldY = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Only the part of the screen under the surface is affected, so you get a
                // clean split at the water line instead of the whole view wobbling.
                float below = smoothstep(_WaterLevel + _SurfaceSoftness,
                                         _WaterLevel - _SurfaceSoftness,
                                         i.worldY) * _Strength;

                clip(below - 0.002);

                float2 uv = i.screenPos.xy / i.screenPos.w;

                float waveA = sin(uv.y * _DistortionScale + _Time.y * _DistortionSpeed);
                float waveB = sin(uv.x * _DistortionScale * 0.7 - _Time.y * _DistortionSpeed * 1.3);

                uv.x += (waveA + waveB * 0.5) * _DistortionStrength * below;
                uv.y += (waveB - waveA * 0.4) * _DistortionStrength * 0.6 * below;

                fixed4 src = tex2D(_CameraSortingLayerTexture, saturate(uv));
                src.rgb = lerp(src.rgb, src.rgb * _Tint.rgb, _TintAmount * below);

                return fixed4(src.rgb, below);
            }
            ENDCG
        }
    }

    FallBack Off
}
