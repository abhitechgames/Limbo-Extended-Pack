Shader "Custom/Water2D"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}
        _Color ("Water Color", Color) = (0.15, 0.55, 0.85, 0.65)
        _DeepColor ("Deep Color", Color) = (0.05, 0.2, 0.45, 0.85)

        [Header(Shape)]
        // 0 lets the shader work the stretch out from the object scale. That only works
        // for a plain sprite quad, so the water mesh writes the real value in here.
        _Aspect ("Width / Height (0 = auto)", Float) = 0

        [Header(Waves)]
        _WaveHeight ("Wave Height", Float) = 0.025
        _WaveFrequency ("Wave Frequency", Float) = 2.5
        _WaveSpeed ("Wave Speed", Float) = 1.2

        _Wave2Height ("Secondary Wave Height", Float) = 0.012
        _Wave2Frequency ("Secondary Wave Frequency", Float) = 5.0
        _Wave2Speed ("Secondary Wave Speed", Float) = 1.7

        _WaveSharpness ("Wave Smoothness", Range(0.5, 2)) = 1.1
        _WaveArea ("Wave Area", Range(0.01, 1)) = 0.12

        [Header(Flow)]
        _FlowSpeedX ("Horizontal Flow Speed", Float) = 0.08
        _FlowSpeedY ("Vertical Flow Speed", Float) = 0.03
        _Tiling ("Texture Tiling", Vector) = (2,2,0,0)

        [Header(Foam)]
        _FoamColor ("Foam Color", Color) = (1,1,1,0.9)
        _FoamWidth ("Foam Width", Range(0,0.5)) = 0.025
        _FoamNoiseScale ("Foam Wobble Scale", Float) = 8.0
        _FoamSpeed ("Foam Wobble Speed", Float) = 2.0

        [Header(Bubbles)]
        _BubbleColor ("Bubble Color", Color) = (0.8,0.95,1.0,0.65)
        _BubbleAmount ("Bubble Amount", Range(0,1)) = 0.15
        _BubbleSize ("Bubble Size", Range(0.005,0.4)) = 0.09
        _BubbleSpeed ("Bubble Speed", Float) = 0.20
        _BubbleDrift ("Bubble Horizontal Drift", Float) = 0.04
        _BubbleSoftness ("Bubble Softness", Range(0.01,0.5)) = 0.15
        _BubbleDepth ("Bubble Depth", Range(0,1)) = 0.90

        [Header(Refraction)]
        // Needs "Camera Sorting Layer Texture" on the 2D Renderer, with the foremost
        // layer set to whatever the water should be able to see through.
        [Toggle(WATER_REFRACTION)] _UseRefraction ("Refract Whats Behind", Float) = 1
        _DistortionStrength ("Distortion Strength", Range(0, 0.08)) = 0.012
        _DistortionScale ("Distortion Scale", Float) = 14
        _DistortionSpeed ("Distortion Speed", Float) = 0.9
        _ShallowShowThrough ("Show Through At Surface", Range(0,1)) = 0.85
        _DeepShowThrough ("Show Through At Bottom", Range(0,1)) = 0.3

        [Header(Edge)]
        _EdgeFade ("Top Edge Fade", Range(0.01, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            #pragma shader_feature_local WATER_REFRACTION
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 localUV : TEXCOORD1;
                float aspect : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color;
            fixed4 _DeepColor;

            float _Aspect;

            float _WaveHeight;
            float _WaveFrequency;
            float _WaveSpeed;
            float _Wave2Height;
            float _Wave2Frequency;
            float _Wave2Speed;
            float _WaveSharpness;
            float _WaveArea;

            float _EdgeFade;

            float _FlowSpeedX;
            float _FlowSpeedY;
            float4 _Tiling;

            fixed4 _FoamColor;
            float _FoamWidth;
            float _FoamNoiseScale;
            float _FoamSpeed;

            fixed4 _BubbleColor;
            float _BubbleAmount;
            float _BubbleSize;
            float _BubbleSpeed;
            float _BubbleDrift;
            float _BubbleSoftness;
            float _BubbleDepth;

            // Filled in by the 2D Renderer's Camera Sorting Layer Texture.
            sampler2D _CameraSortingLayerTexture;

            float _DistortionStrength;
            float _DistortionScale;
            float _DistortionSpeed;
            float _ShallowShowThrough;
            float _DeepShowThrough;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // How many times wider than tall the water is. Bubbles need this or they
            // come out as ovals on a wide pool.
            float SurfaceAspect()
            {
                if (_Aspect > 0.0001) return _Aspect;

                float sx = length(float3(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0]));
                float sy = length(float3(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1]));

                return max(0.0001, sx / max(0.0001, sy));
            }

            v2f vert(appdata v)
            {
                v2f o;

                float2 localUV = v.uv;

                // Only the band near the top is allowed to move.
                float surfaceMask = smoothstep(1.0 - _WaveArea, 1.0, localUV.y);

                float wave1 = sin(v.vertex.x * _WaveFrequency - _Time.y * _WaveSpeed);
                float wave2 = sin(v.vertex.x * _Wave2Frequency - _Time.y * _Wave2Speed + 1.7);

                float combined = wave1 * _WaveHeight + wave2 * _Wave2Height;
                combined = sign(combined) * pow(abs(combined), _WaveSharpness);

                v.vertex.y += combined * surfaceMask;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) * _Tiling.xy;
                o.localUV = localUV;
                o.aspect = SurfaceAspect();
                o.screenPos = ComputeScreenPos(o.pos);

                return o;
            }

            // One drifting field of bubbles. depthCoord is the untouched vertical UV so
            // every layer fades out at the same depth.
            float bubbleLayer(float2 uv, float layer, float aspect, float depthCoord)
            {
                // Square cells - the row count sets the size, the column count follows
                // the aspect so a cell is as wide as it is tall.
                float rows = 5.0 + layer * 2.0;
                float2 gridUV = uv * float2(rows * aspect, rows);

                float2 cell = floor(gridUV);
                float2 local = frac(gridUV) - 0.5;

                float random = hash21(cell + layer * 17.3);
                float bubbleMask = step(random, _BubbleAmount);

                // Bottom to top, each bubble on its own schedule.
                float bubbleTime = frac(_Time.y * _BubbleSpeed * (0.35 + random * 0.65) + random);
                float bubbleY = lerp(-0.5, 0.5, bubbleTime);
                float bubbleX = sin(_Time.y * 1.2 + random * 25.0) * _BubbleDrift;

                float2 bubblePos = float2(local.x + bubbleX * 0.15, local.y - bubbleY);

                float size = _BubbleSize * lerp(0.65, 1.35, random);
                float bubble = 1.0 - smoothstep(size * (1.0 - _BubbleSoftness), size, length(bubblePos));

                float bottomFade = smoothstep(0.02, 0.15, depthCoord);
                float topFade = smoothstep(1.0, _BubbleDepth, depthCoord);

                return bubble * bubbleMask * bottomFade * topFade;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 scrolledUV = i.uv + float2(_Time.y * _FlowSpeedX, _Time.y * _FlowSpeedY);
                fixed4 tex = tex2D(_MainTex, scrolledUV);

                fixed4 baseColor = lerp(_DeepColor, _Color, i.localUV.y);

                fixed4 col = tex * baseColor;
                col.a = baseColor.a * tex.a;

#if defined(WATER_REFRACTION)
                // Bend the view of whatever is behind the water. Anything submerged -
                // the player included - wobbles as soon as it drops below the surface.
                float2 screenUV = i.screenPos.xy / i.screenPos.w;

                float wobbleA = sin(i.localUV.y * _DistortionScale + _Time.y * _DistortionSpeed);
                float wobbleB = sin(i.localUV.x * _DistortionScale * 0.6 - _Time.y * _DistortionSpeed * 1.3);

                screenUV.x += (wobbleA + wobbleB * 0.5) * _DistortionStrength;
                screenUV.y += (wobbleB - wobbleA * 0.4) * _DistortionStrength * 0.5;

                fixed3 behind = tex2D(_CameraSortingLayerTexture, saturate(screenUV)).rgb;
                float behindLum = dot(behind, float3(0.299, 0.587, 0.114));

                // Darken the water where something solid is behind it rather than blending
                // towards it - open sky behind the pool then leaves the water its own colour,
                // and submerged silhouettes read as shadows that wobble with the surface.
                float show = lerp(_DeepShowThrough, _ShallowShowThrough, i.localUV.y);

                col.rgb *= lerp(1.0, behindLum, show);
                col.a = 1;
#endif

                // Foam - a wobbling band pinned to the top edge.
                float wobble = sin(i.localUV.x * _FoamNoiseScale + _Time.y * _FoamSpeed) * 0.5 + 0.5;
                float foamWidth = _FoamWidth * (0.6 + wobble * 0.4);
                float foamBand = smoothstep(1.0 - foamWidth, 1.0, i.localUV.y);

                col.rgb = lerp(col.rgb, _FoamColor.rgb, foamBand * _FoamColor.a);
                col.a = max(col.a, foamBand * _FoamColor.a);

                // Three offset layers so the bubbles do not sit on an obvious grid.
                float depthCoord = i.localUV.y;
                float bubbles = bubbleLayer(i.localUV, 1.0, i.aspect, depthCoord)
                              + bubbleLayer(i.localUV * 1.37 + 0.17, 2.0, i.aspect, depthCoord)
                              + bubbleLayer(i.localUV * 0.73 + 0.51, 3.0, i.aspect, depthCoord);

                bubbles = saturate(bubbles * 0.45);

                col.rgb = lerp(col.rgb, _BubbleColor.rgb, bubbles * _BubbleColor.a);
                col.a = max(col.a, bubbles * _BubbleColor.a);

                return col;
            }
            ENDCG
        }
    }

    FallBack "Transparent/VertexLit"
}
