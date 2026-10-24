// Vuelve transparente lo negro de un video o textura (para videos exportados sin canal alfa).
// Lo usa la hoja que da vuelta en el recetario de Opciones: el fondo negro del video desaparece y
// debajo se sigue viendo el libro. El alfa del color de la UI (RawImage) permite hacer fundidos.
Shader "Odisea/UI/LumaKey"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "black" {}
        _Low ("Transparente por debajo de", Range(0, 1)) = 0.06
        _High ("Opaco por encima de", Range(0, 1)) = 0.16
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
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
            float _Low;
            float _High;

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
                fixed4 c = tex2D(_MainTex, i.uv);
                float luminance = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float alpha = smoothstep(_Low, _High, luminance);
                return fixed4(c.rgb * i.color.rgb, alpha * i.color.a);
            }
            ENDCG
        }
    }
}
