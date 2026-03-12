Shader "Custom/SpriteOutline" {
    Properties {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,0.2,1)
        _OutlineSize ("Outline Size", Range(0,0.1)) = 0.03
    }

    SubShader {
        Tags {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;

            v2f vert (appdata_t IN) {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                if (c.a == 0) {
                    float2 offset = _OutlineSize * _MainTex_TexelSize.xy;
                    float alpha = 0.0;
                    alpha = max(alpha, tex2D(_MainTex, IN.texcoord + float2( offset.x,  0)).a);
                    alpha = max(alpha, tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a);
                    alpha = max(alpha, tex2D(_MainTex, IN.texcoord + float2(0,  offset.y)).a);
                    alpha = max(alpha, tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a);

                    if (alpha > 0.0)
                        return fixed4(_OutlineColor.rgb, _OutlineColor.a * alpha);
                }

                return c;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
