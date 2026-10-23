// Transición tipo iris de caricatura clásica: cubre la pantalla de color excepto un círculo.
// _Radius y _Center están en unidades de la altura de la pantalla (el ancho se corrige con el aspecto),
// así que el círculo nunca se deforma en óvalo. Lo controla ScreenFader.
Shader "Odisea/UI/Iris"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Color", Color) = (0.169, 0.118, 0.086, 1)
        _Center ("Centro (0-1 de la pantalla)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radio (alturas de pantalla)", Float) = 1.2
        _Softness ("Borde suave", Float) = 0.004
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 delta = (i.uv - _Center.xy) * float2(aspect, 1.0);
                float distanceToCenter = length(delta);
                // 0 dentro del círculo (se ve el juego), 1 fuera (color de la cortina).
                // El suavizado va hacia adentro del borde: con radio 0 la pantalla queda cubierta por completo,
                // sin el puntito a medio cubrir que dejaría un suavizado centrado en el borde.
                float covered = _Radius <= 0.0 ? 1.0 : smoothstep(_Radius - _Softness, _Radius, distanceToCenter);
                return fixed4(_Color.rgb, _Color.a * covered);
            }
            ENDCG
        }
    }
}
