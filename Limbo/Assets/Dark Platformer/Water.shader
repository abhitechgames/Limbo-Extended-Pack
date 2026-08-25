Shader "Custom/Water2D"
{
    Properties
    {
        _MainTex ("Water Texture", 2D) = "white" {}

        _Color ("Water Color", Color) =
            (0.15, 0.55, 0.85, 0.65)

        _DeepColor ("Deep Color", Color) =
            (0.05, 0.2, 0.45, 0.85)


        // =====================================================
        // WAVE
        // =====================================================

        _WaveHeight ("Wave Height", Float) = 0.025
        _WaveFrequency ("Wave Frequency", Float) = 2.5
        _WaveSpeed ("Wave Speed", Float) = 1.2

        _Wave2Height ("Secondary Wave Height", Float) = 0.012
        _Wave2Frequency ("Secondary Wave Frequency", Float) = 5.0
        _Wave2Speed ("Secondary Wave Speed", Float) = 1.7

        _WaveSharpness ("Wave Smoothness", Range(0.5, 2)) = 1.1

        // Only top portion of water moves
        _WaveArea ("Wave Area", Range(0.01, 1)) = 0.12


        // =====================================================
        // EDGE
        // =====================================================

        _EdgeFade ("Top Edge Fade", Range(0.01, 1)) = 0.25


        // =====================================================
        // WATER FLOW
        // =====================================================

        _FlowSpeedX ("Horizontal Flow Speed", Float) = 0.08
        _FlowSpeedY ("Vertical Flow Speed", Float) = 0.03

        _Tiling ("Texture Tiling", Vector) =
            (2,2,0,0)


        // =====================================================
        // FOAM
        // =====================================================

        _FoamColor ("Foam Color", Color) =
            (1,1,1,0.9)

        _FoamWidth ("Foam Width", Range(0,0.5)) =
            0.025

        _FoamNoiseScale ("Foam Wobble Scale", Float) =
            8.0

        _FoamSpeed ("Foam Wobble Speed", Float) =
            2.0


        // =====================================================
        // BUBBLES
        // =====================================================

        _BubbleColor ("Bubble Color", Color) =
            (0.8,0.95,1.0,0.65)

        _BubbleAmount ("Bubble Amount", Range(0,1)) =
            0.15

        _BubbleSize ("Bubble Size", Range(0.005,0.15)) =
            0.025

        _BubbleSpeed ("Bubble Speed", Float) =
            0.20

        _BubbleDrift ("Bubble Horizontal Drift", Float) =
            0.04

        _BubbleSoftness ("Bubble Softness", Range(0.01,0.5)) =
            0.15

        _BubbleDepth ("Bubble Depth", Range(0,1)) =
            0.90
    }


    // =========================================================
    // SUBSHADER
    // =========================================================

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

            #include "UnityCG.cginc"


            // =================================================
            // STRUCTURES
            // =================================================

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
            };


            // =================================================
            // TEXTURE
            // =================================================

            sampler2D _MainTex;

            float4 _MainTex_ST;


            // =================================================
            // WATER
            // =================================================

            fixed4 _Color;

            fixed4 _DeepColor;


            // =================================================
            // WAVE
            // =================================================

            float _WaveHeight;
            float _WaveFrequency;
            float _WaveSpeed;

            float _Wave2Height;
            float _Wave2Frequency;
            float _Wave2Speed;

            float _WaveSharpness;

            float _WaveArea;


            // =================================================
            // EDGE
            // =================================================

            float _EdgeFade;


            // =================================================
            // FLOW
            // =================================================

            float _FlowSpeedX;
            float _FlowSpeedY;

            float4 _Tiling;


            // =================================================
            // FOAM
            // =================================================

            fixed4 _FoamColor;

            float _FoamWidth;

            float _FoamNoiseScale;

            float _FoamSpeed;


            // =================================================
            // BUBBLES
            // =================================================

            fixed4 _BubbleColor;

            float _BubbleAmount;

            float _BubbleSize;

            float _BubbleSpeed;

            float _BubbleDrift;

            float _BubbleSoftness;

            float _BubbleDepth;


            // =================================================
            // RANDOM
            // =================================================

            float hash21(float2 p)
            {
                p =
                    frac(
                        p *
                        float2(
                            123.34,
                            456.21
                        )
                    );

                p +=
                    dot(
                        p,
                        p + 45.32
                    );

                return frac(
                    p.x * p.y
                );
            }


            // =================================================
            // VERTEX
            // =================================================

            v2f vert(appdata v)
            {
                v2f o;


                float2 localUV = v.uv;


                // -------------------------------------------------
                // TOP SURFACE MASK
                // -------------------------------------------------

                float surfaceMask =
                    smoothstep(
                        1.0 - _WaveArea,
                        1.0,
                        localUV.y
                    );


                // -------------------------------------------------
                // MAIN TRAVELING WAVE
                // -------------------------------------------------

                float wave1 =
                    sin(
                        v.vertex.x
                        * _WaveFrequency
                        -
                        _Time.y
                        * _WaveSpeed
                    );


                // -------------------------------------------------
                // SECONDARY WAVE
                // -------------------------------------------------

                float wave2 =
                    sin(
                        v.vertex.x
                        * _Wave2Frequency
                        -
                        _Time.y
                        * _Wave2Speed
                        +
                        1.7
                    );


                // -------------------------------------------------
                // COMBINE WAVES
                // -------------------------------------------------

                float combinedWave =
                    wave1 * _WaveHeight
                    +
                    wave2 * _Wave2Height;


                // -------------------------------------------------
                // SMOOTH WAVE
                // -------------------------------------------------

                combinedWave =
                    sign(combinedWave)
                    *
                    pow(
                        abs(combinedWave),
                        _WaveSharpness
                    );


                // -------------------------------------------------
                // APPLY ONLY NEAR SURFACE
                // -------------------------------------------------

                float displacement =
                    combinedWave
                    * surfaceMask;


                v.vertex.y += displacement;


                // -------------------------------------------------
                // POSITION
                // -------------------------------------------------

                o.pos =
                    UnityObjectToClipPos(
                        v.vertex
                    );


                // -------------------------------------------------
                // UV
                // -------------------------------------------------

                o.uv =
                    TRANSFORM_TEX(
                        v.uv,
                        _MainTex
                    )
                    *
                    _Tiling.xy;


                o.localUV =
                    localUV;


                return o;
            }


            // =================================================
            // BUBBLE LAYER
            // =================================================

            float bubbleLayer(
                float2 uv,
                float layer
            )
            {

                // -------------------------------------------------
                // CREATE GRID
                // -------------------------------------------------

                float2 gridUV =
                    uv
                    *
                    float2(
                        7.0 + layer * 3.0,
                        5.0 + layer * 2.0
                    );


                float2 cell =
                    floor(gridUV);


                float2 local =
                    frac(gridUV)
                    -
                    0.5;


                // -------------------------------------------------
                // RANDOM VALUE
                // -------------------------------------------------

                float random =
                    hash21(
                        cell
                        +
                        layer * 17.3
                    );


                // -------------------------------------------------
                // BUBBLE AMOUNT
                // -------------------------------------------------

                float bubbleMask =
                    step(
                        random,
                        _BubbleAmount
                    );


                // -------------------------------------------------
                // BOTTOM -> TOP MOVEMENT
                // -------------------------------------------------

                float bubbleTime =
                    frac(
                        _Time.y
                        *
                        _BubbleSpeed
                        *
                        (
                            0.35
                            +
                            random * 0.65
                        )
                        +
                        random
                    );


                // -------------------------------------------------
                // BUBBLE Y POSITION
                //
                // -0.5 = bottom
                // +0.5 = top
                // -------------------------------------------------

                float bubbleY =
                    lerp(
                        -0.5,
                        0.5,
                        bubbleTime
                    );


                // -------------------------------------------------
                // HORIZONTAL NATURAL DRIFT
                // -------------------------------------------------

                float bubbleX =
                    sin(
                        _Time.y
                        * 1.2
                        +
                        random * 25.0
                    )
                    *
                    _BubbleDrift;


                // -------------------------------------------------
                // BUBBLE POSITION
                // -------------------------------------------------

                float2 bubblePos;


                bubblePos.x =
                    local.x
                    +
                    bubbleX * 0.15;


                bubblePos.y =
                    local.y
                    -
                    bubbleY;


                // -------------------------------------------------
                // RANDOM BUBBLE SIZE
                // -------------------------------------------------

                float size =
                    _BubbleSize
                    *
                    lerp(
                        0.65,
                        1.35,
                        random
                    );


                // -------------------------------------------------
                // CIRCLE
                // -------------------------------------------------

                float distanceFromCenter =
                    length(
                        bubblePos
                    );


                float bubble =
                    1.0
                    -
                    smoothstep(
                        size
                        *
                        (
                            1.0
                            -
                            _BubbleSoftness
                        ),
                        size,
                        distanceFromCenter
                    );


                // -------------------------------------------------
                // DEPTH FADE
                // -------------------------------------------------

                float bottomFade =
                    smoothstep(
                        0.02,
                        0.15,
                        uv.y
                    );


                float topFade =
                    smoothstep(
                        1.0,
                        _BubbleDepth,
                        uv.y
                    );


                float depthFade =
                    bottomFade
                    *
                    topFade;


                // -------------------------------------------------
                // FINAL BUBBLE
                // -------------------------------------------------

                return
                    bubble
                    *
                    bubbleMask
                    *
                    depthFade;
            }


            // =================================================
            // FRAGMENT
            // =================================================

            fixed4 frag(v2f i) : SV_Target
            {

                // =================================================
                // WATER TEXTURE FLOW
                // =================================================

                float2 scrolledUV =
                    i.uv
                    +
                    float2(
                        _Time.y
                        * _FlowSpeedX,

                        _Time.y
                        * _FlowSpeedY
                    );


                fixed4 tex =
                    tex2D(
                        _MainTex,
                        scrolledUV
                    );


                // =================================================
                // WATER COLOR GRADIENT
                // =================================================

                fixed4 baseColor =
                    lerp(
                        _DeepColor,
                        _Color,
                        i.localUV.y
                    );


                fixed4 col =
                    tex
                    *
                    baseColor;


                col.a =
                    baseColor.a
                    *
                    tex.a;


                // =================================================
                // FOAM
                // =================================================

                float wobble =
                    sin(
                        i.localUV.x
                        *
                        _FoamNoiseScale
                        +
                        _Time.y
                        *
                        _FoamSpeed
                    )
                    *
                    0.5
                    +
                    0.5;


                float foamWidth =
                    _FoamWidth
                    *
                    (
                        0.6
                        +
                        wobble * 0.4
                    );


                float foamBand =
                    smoothstep(
                        1.0 - foamWidth,
                        1.0,
                        i.localUV.y
                    );


                col.rgb =
                    lerp(
                        col.rgb,
                        _FoamColor.rgb,
                        foamBand
                        *
                        _FoamColor.a
                    );


                col.a =
                    max(
                        col.a,
                        foamBand
                        *
                        _FoamColor.a
                    );


                // =================================================
                // BUBBLES
                // =================================================

                float bubbles = 0.0;


                bubbles +=
                    bubbleLayer(
                        i.localUV,
                        1.0
                    );


                bubbles +=
                    bubbleLayer(
                        i.localUV
                        *
                        1.37
                        +
                        0.17,
                        2.0
                    );


                bubbles +=
                    bubbleLayer(
                        i.localUV
                        *
                        0.73
                        +
                        0.51,
                        3.0
                    );


                // -------------------------------------------------
                // LIMIT BUBBLE INTENSITY
                // -------------------------------------------------

                bubbles =
                    saturate(
                        bubbles
                        *
                        0.45
                    );


                // -------------------------------------------------
                // BUBBLE COLOR
                // -------------------------------------------------

                col.rgb =
                    lerp(
                        col.rgb,
                        _BubbleColor.rgb,
                        bubbles
                        *
                        _BubbleColor.a
                    );


                col.a =
                    max(
                        col.a,
                        bubbles
                        *
                        _BubbleColor.a
                    );


                return col;
            }


            ENDCG
        }
    }


    FallBack "Transparent/VertexLit"
}