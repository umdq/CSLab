Shader "Unlit/renderboid"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_boidID("boidID",Int)=0
		//哈哈一开始还愁StructuredBuffer怎么办，后来发现set根本不用这里的名，这里只为了显示到Inspector Pane！
	}
	SubShader
	{
		//LOD 200
        //Lighting Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0//哈哈！compile要用shader高版本，不然会报错"the target doesn't support UAVs",UAV SRV…
			//#pragma only_renderers d3d11
			#include "UnityCG.cginc"
			#include "HelpQuaternion.cginc"

			struct appdata
			{
				float4 posL : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 posH : SV_POSITION;
			};

			struct Boid
			{
				float3 pos;
				float3 rot;
				float speed;
				float rotSpeed;
			};
			//RWStructuredBuffer<Boid> _boidBuffer;
			uniform StructuredBuffer<Boid> _boidBuffer;

			int _boidID;

			sampler2D _MainTex;
			
			v2f vert (appdata v)
			{
				v2f o;

				Boid boid=_boidBuffer[_boidID];
				//哈哈，手动quaternion&translation
				float4 lookQuat=newQuat(float3(0,0,1),boid.rot);
				//太小了！他妈一开始还以为没画出来！手动放大
				v.posL.xyz*=5;
				v.posL.xyz=rotateVec( lookQuat, v.posL.xyz)+boid.pos;
				o.posH=mul(UNITY_MATRIX_VP,v.posL);

				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				//fixed4 col = tex2D(_MainTex, i.uv);
				return fixed4(1.0,0.5,0.5,1);
			}
			ENDCG
		}
	}
}
