Shader "Custom/LockOnOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        // Thickness as a fraction of the distance to the camera, so the
        // outline keeps about the same size on screen near and far.
        _OutlineWidth ("Outline Width", Float) = 0.003
    }

    SubShader
    {
        // Drawn AFTER the stencil mask (Geometry+1) so it can use it.
        Tags { "RenderType"="Opaque" "Queue"="Geometry+2" }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual

            // Only draw where the character itself was NOT drawn.
            // This removes the lines inside the body and leaves only the outer edge.
            Stencil
            {
                Ref 33
                Comp NotEqual
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
                float dist = distance(worldPos, _WorldSpaceCameraPos);
                worldPos += worldNormal * (_OutlineWidth * dist);
                o.pos = UnityWorldToClipPos(float4(worldPos, 1.0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    FallBack Off
}
