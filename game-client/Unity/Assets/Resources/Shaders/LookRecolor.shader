// Repaints a character texture's clothes (blit, once per outfit): where the mask is red the top,
// where it's green the bottom, blue the hair (on one-piece models), keeping the fabric's light and shade (brightness / region mean).
Shader "Hidden/ThaiLife/LookRecolor"
{
    Properties
    {
        _MainTex ("Base", 2D) = "white" {}
        _Mask ("Mask", 2D) = "black" {}
        _TopColor ("Top", Color) = (1,1,1,1)
        _BottomColor ("Bottom", Color) = (1,1,1,1)
        _HairColor ("Hair", Color) = (1,1,1,1)
        _Means ("Mean brightness (top, bottom, hair)", Vector) = (0.5, 0.5, 0.5, 0)
        _Use ("Use (top, bottom, hair)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Mask;
            float4 _TopColor, _BottomColor, _HairColor, _Means, _Use;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                fixed4 m = tex2D(_Mask, i.uv);
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float3 top = _TopColor.rgb * clamp(l / max(_Means.x, 0.02), 0.35, 1.45);
                float3 bottom = _BottomColor.rgb * clamp(l / max(_Means.y, 0.02), 0.35, 1.45);
                c.rgb = lerp(c.rgb, top, saturate(m.r) * _Use.x);
                c.rgb = lerp(c.rgb, bottom, saturate(m.g) * _Use.y);
                float3 hair = _HairColor.rgb * clamp(l / max(_Means.z, 0.02), 0.4, 1.6);
                c.rgb = lerp(c.rgb, hair, saturate(m.b) * _Use.z);
                return c;
            }
            ENDCG
        }
    }
}
