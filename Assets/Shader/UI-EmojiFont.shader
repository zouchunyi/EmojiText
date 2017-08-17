
//	Author:zouchunyi
//	E-mail:zouchunyi@kingsoft.com

Shader "UI/EmojiFont" {
	Properties {
		[PerRendererData] _MainTex ("Font Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		
		_ColorMask ("Color Mask", Float) = 15
		
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		_EmojiTex ("Emoji Texture", 2D) = "white" {}
		_EmojiDataTex ("Emoji Data", 2D) = "white" {}
		_EmojiSize ("Emoji count of every line",float) = 200
		_FrameSpeed ("FrameSpeed",Range(0,10)) = 3
	}
	
	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "Default"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_ALPHACLIP
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				half2 texcoord1 : TEXCOORD1;
			};
			
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(float4(IN.vertex.x, IN.vertex.y, IN.vertex.z, 1.0));

				OUT.texcoord = IN.texcoord;
				OUT.texcoord1 = IN.texcoord1;
				
				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1) * OUT.vertex.w;
				#endif
				
				OUT.color = IN.color * _Color;
				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _EmojiTex;
			sampler2D _EmojiDataTex;
		    float _EmojiSize;
			float _FrameSpeed;

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 color;
				if (IN.texcoord1.x >0 && IN.texcoord1.y > 0)
				{
					// it's an emoji

					// compute the size of emoji
					half size = (1 / _EmojiSize);
					// compute the center uv of per pixel in the emoji
					half2 uv = half2(floor(IN.texcoord1.x * _EmojiSize) * size + 0.5 * size,floor(IN.texcoord1.y * _EmojiSize) * size + 0.5 * size);
					// read data
					fixed4 data = tex2D(_EmojiDataTex, uv);
					// compute the frame count of emoji
					half frameCount = 1 + sign(data.r) + sign(data.g) * 2 + sign(data.b) * 4;
					// compute current frame index of emoji
					half index = abs(fmod(floor(_Time.x * _FrameSpeed * 50), frameCount));
					// judge current frame is in the next line or not.
					half flag = (1 + sign(IN.texcoord1.x + index * size - 1)) * 0.5;
					// compute the final uv
					IN.texcoord1.x += index * size - flag;
					IN.texcoord1.y += size * flag;

					color = tex2D(_EmojiTex, IN.texcoord1);
				}else
				{
					// it's a text, and render it as normal ugui text
					color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
				}

				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif

				return color;
			}
		ENDCG
		}
	}
}
