// Sprite que puede "encenderse" de un color (blanco) al recibir un golpe, sin perder su silueta.
// _FlashAmount: 0 = dibujo normal, 1 = todo del color _FlashColor. Lo mueve DamageFeedback con un
// MaterialPropertyBlock, así cada enemigo destella por su cuenta aunque compartan el material.
Shader "Odisea/Sprites/Flash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
        _FlashColor ("Color del destello", Color) = (1, 1, 1, 1)
        _FlashAmount ("Cantidad de destello", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _FlashColor;
            float _FlashAmount;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _FlashAmount);
                // Alfa premultiplicado: la orilla suave del dibujo se respeta también al destellar.
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
