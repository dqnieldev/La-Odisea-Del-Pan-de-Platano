// Capa de brillo a pantalla completa: multiplica lo que ya está dibujado por 2 × el color de la UI.
// Gris 0.5 deja la imagen igual; más oscuro la oscurece y más claro la aclara (sin lavar los colores).
// La usa ScreenOverlay con el ajuste Brillo de Opciones.
Shader "Odisea/UI/Brightness"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
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
        ZTest Always
        // Resultado = color × destino + destino × color = 2 × color × destino.
        Blend DstColor SrcColor

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(i.color.rgb, 1);
            }
            ENDCG
        }
    }
}
