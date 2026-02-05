Shader "Custom/TMP_IgnoreZ"
{
    Properties
    {
        [PerRendererData]_MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0
        _UnderlayColor ("Underlay Color", Color) = (0,0,0,0.5)
        _UnderlayOffsetX ("Underlay Offset X", Range(-1,1)) = 0
        _UnderlayOffsetY ("Underlay Offset Y", Range(-1,1)) = 0
        _UnderlayDilate ("Underlay Dilate", Range(-1,1)) = 0
        _UnderlaySoftness ("Underlay Softness", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100
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
            sampler2D _MainTex;
            float4 _FaceColor;
            float _FaceDilate;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineSoftness;
            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };
            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texCol = tex2D(_MainTex, i.texcoord);
                float sdf = texCol.a;

                float base = 0.5;
                float outline = _OutlineWidth * 0.5;
                float softness = max(_OutlineSoftness * 0.5, 0.001);

                float outlineEdge = base - outline;
                float faceAlpha = smoothstep(outlineEdge - softness, outlineEdge + softness, sdf);
                float outlineAlpha = smoothstep(base - softness, base + softness, sdf);

                fixed4 faceCol = _FaceColor * i.color;
                faceCol.a *= faceAlpha * texCol.a * i.color.a * _FaceColor.a;

                fixed4 outlineCol = _OutlineColor;
                outlineCol.a *= (outlineAlpha - faceAlpha) * texCol.a * _OutlineColor.a;

                fixed4 col = faceCol + outlineCol;
                col.rgb = lerp(_OutlineColor.rgb, faceCol.rgb, faceAlpha);

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}