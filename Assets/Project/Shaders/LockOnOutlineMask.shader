Shader "Custom/LockOnOutlineMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        // Marks the pixels covered by the character in the stencil buffer.
        // Draws no color and no depth, so it is invisible.
        Pass
        {
            Name "Mask"
            Cull Back
            ZWrite Off
            ZTest LEqual
            ColorMask 0
            Offset -1, -1

            Stencil
            {
                Ref 33
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
    }
    FallBack Off
}
