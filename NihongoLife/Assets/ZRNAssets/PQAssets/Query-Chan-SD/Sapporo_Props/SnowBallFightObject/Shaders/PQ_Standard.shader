// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge Beta 0.36 
// Shader Forge (c) Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:0.36;sub:START;pass:START;ps:flbk:,lico:1,lgpr:1,nrmq:1,limd:1,uamb:True,mssp:True,lmpd:False,lprd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,rpth:0,hqsc:True,hqlp:False,tesm:0,blpr:0,bsrc:0,bdst:0,culm:2,dpts:2,wrdp:True,ufog:True,aust:True,igpj:False,qofs:0,qpre:2,rntp:3,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:1,x:32209,y:32624|diff-56-OUT,spec-349-OUT,gloss-337-OUT,emission-8-OUT,clip-2-A;n:type:ShaderForge.SFN_Tex2d,id:2,x:33280,y:32554,ptlb:Main_Texture,ptin:_Main_Texture,tex:32c7815ef3d68b245aa82415ca645769,ntxv:2,isnm:False;n:type:ShaderForge.SFN_VertexColor,id:3,x:33486,y:32787;n:type:ShaderForge.SFN_ToggleProperty,id:5,x:32910,y:32821,ptlb:Unlit,ptin:_Unlit,on:False;n:type:ShaderForge.SFN_Multiply,id:6,x:32697,y:32548|A-286-OUT,B-9-OUT;n:type:ShaderForge.SFN_Multiply,id:8,x:32503,y:32727|A-286-OUT,B-5-OUT;n:type:ShaderForge.SFN_OneMinus,id:9,x:32910,y:32689|IN-5-OUT;n:type:ShaderForge.SFN_Slider,id:55,x:32697,y:32458,ptlb:Diffuse_Val,ptin:_Diffuse_Val,min:1,cur:1.5,max:4;n:type:ShaderForge.SFN_Multiply,id:56,x:32503,y:32552|A-55-OUT,B-6-OUT;n:type:ShaderForge.SFN_Slider,id:77,x:33425,y:32931,ptlb:VertexColor_Val,ptin:_VertexColor_Val,min:0,cur:1,max:1;n:type:ShaderForge.SFN_Multiply,id:286,x:33013,y:32555|A-2-RGB,B-312-OUT;n:type:ShaderForge.SFN_Lerp,id:312,x:33223,y:32742|A-315-OUT,B-3-RGB,T-77-OUT;n:type:ShaderForge.SFN_Vector1,id:315,x:33486,y:32733,v1:0.5;n:type:ShaderForge.SFN_Tex2d,id:326,x:32803,y:32968,ptlb:Spec_Texture,ptin:_Spec_Texture,ntxv:1,isnm:False;n:type:ShaderForge.SFN_Slider,id:336,x:32776,y:33151,ptlb:Spec_Power,ptin:_Spec_Power,min:0,cur:0.5,max:2;n:type:ShaderForge.SFN_Slider,id:337,x:32776,y:33254,ptlb:Gloss_Power,ptin:_Gloss_Power,min:0,cur:0.25,max:1;n:type:ShaderForge.SFN_Multiply,id:349,x:32551,y:32991|A-326-RGB,B-336-OUT;proporder:2-5-55-77-326-336-337;pass:END;sub:END;*/

Shader "PQ/PQ_Standard" {
    Properties {
        _Main_Texture ("Main_Texture", 2D) = "black" {}
        [MaterialToggle] _Unlit ("Unlit", Float ) = 0
        _Diffuse_Val ("Diffuse_Val", Range(1, 4)) = 1.5
        _VertexColor_Val ("VertexColor_Val", Range(0, 1)) = 1
        _Spec_Texture ("Spec_Texture", 2D) = "gray" {}
        _Spec_Power ("Spec_Power", Range(0, 2)) = 0.5
        _Gloss_Power ("Gloss_Power", Range(0, 1)) = 0.25
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "RenderType"="TransparentCutout"
        }
        Pass {
            Name "ForwardBase"
            Tags {
                "LightMode"="ForwardBase"
            }
            Cull Off
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma exclude_renderers flash d3d11_9x 
            #pragma target 3.0
            uniform float4 _LightColor0;
            uniform sampler2D _Main_Texture; uniform float4 _Main_Texture_ST;
            uniform fixed _Unlit;
            uniform float _Diffuse_Val;
            uniform float _VertexColor_Val;
            uniform sampler2D _Spec_Texture; uniform float4 _Spec_Texture_ST;
            uniform float _Spec_Power;
            uniform float _Gloss_Power;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(3,4)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), unity_WorldToObject).xyz;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
/////// Normals:
                float3 normalDirection =  i.normalDir;
                
                float nSign = sign( dot( viewDirection, i.normalDir ) ); // Reverse normal if this is a backface
                i.normalDir *= nSign;
                normalDirection *= nSign;
                
                float2 node_407 = i.uv0;
                float4 node_2 = tex2D(_Main_Texture,TRANSFORM_TEX(node_407.rg, _Main_Texture));
                clip(node_2.a - 0.5);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
/////// Diffuse:
                float NdotL = dot( normalDirection, lightDirection );
                float3 diffuse = max( 0.0, NdotL) * attenColor + UNITY_LIGHTMODEL_AMBIENT.rgb;
////// Emissive:
                float node_315 = 0.5;
                float3 node_286 = (node_2.rgb*lerp(float3(node_315,node_315,node_315),i.vertexColor.rgb,_VertexColor_Val));
                float3 emissive = (node_286*_Unlit);
///////// Gloss:
                float gloss = _Gloss_Power;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                NdotL = max(0.0, NdotL);
                float3 specularColor = (tex2D(_Spec_Texture,TRANSFORM_TEX(node_407.rg, _Spec_Texture)).rgb*_Spec_Power);
                float3 specular = (floor(attenuation) * _LightColor0.xyz) * pow(max(0,dot(halfDirection,normalDirection)),specPow) * specularColor;
                float3 finalColor = 0;
                float3 diffuseLight = diffuse;
                finalColor += diffuseLight * (_Diffuse_Val*(node_286*(1.0 - _Unlit)));
                finalColor += specular;
                finalColor += emissive;
/// Final Color:
                return fixed4(finalColor,1);
            }
            ENDCG
        }
        Pass {
            Name "ForwardAdd"
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            Cull Off
            
            
            Fog { Color (0,0,0,0) }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDADD
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #pragma multi_compile_fwdadd_fullshadows
            #pragma exclude_renderers flash d3d11_9x 
            #pragma target 3.0
            uniform float4 _LightColor0;
            uniform sampler2D _Main_Texture; uniform float4 _Main_Texture_ST;
            uniform fixed _Unlit;
            uniform float _Diffuse_Val;
            uniform float _VertexColor_Val;
            uniform sampler2D _Spec_Texture; uniform float4 _Spec_Texture_ST;
            uniform float _Spec_Power;
            uniform float _Gloss_Power;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 vertexColor : COLOR;
                LIGHTING_COORDS(3,4)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(float4(v.normal,0), unity_WorldToObject).xyz;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
/////// Normals:
                float3 normalDirection =  i.normalDir;
                
                float nSign = sign( dot( viewDirection, i.normalDir ) ); // Reverse normal if this is a backface
                i.normalDir *= nSign;
                normalDirection *= nSign;
                
                float2 node_408 = i.uv0;
                float4 node_2 = tex2D(_Main_Texture,TRANSFORM_TEX(node_408.rg, _Main_Texture));
                clip(node_2.a - 0.5);
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
/////// Diffuse:
                float NdotL = dot( normalDirection, lightDirection );
                float3 diffuse = max( 0.0, NdotL) * attenColor;
///////// Gloss:
                float gloss = _Gloss_Power;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                NdotL = max(0.0, NdotL);
                float3 specularColor = (tex2D(_Spec_Texture,TRANSFORM_TEX(node_408.rg, _Spec_Texture)).rgb*_Spec_Power);
                float3 specular = attenColor * pow(max(0,dot(halfDirection,normalDirection)),specPow) * specularColor;
                float3 finalColor = 0;
                float3 diffuseLight = diffuse;
                float node_315 = 0.5;
                float3 node_286 = (node_2.rgb*lerp(float3(node_315,node_315,node_315),i.vertexColor.rgb,_VertexColor_Val));
                finalColor += diffuseLight * (_Diffuse_Val*(node_286*(1.0 - _Unlit)));
                finalColor += specular;
/// Final Color:
                return fixed4(finalColor * 1,0);
            }
            ENDCG
        }
        Pass {
            Name "ShadowCollector"
            Tags {
                "LightMode"="ShadowCollector"
            }
            Cull Off
            
            Fog {Mode Off}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCOLLECTOR
            #define SHADOW_COLLECTOR_PASS
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcollector
            #pragma exclude_renderers flash d3d11_9x 
            #pragma target 3.0
            uniform sampler2D _Main_Texture; uniform float4 _Main_Texture_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                V2F_SHADOW_COLLECTOR;
                float2 uv0 : TEXCOORD5;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_SHADOW_COLLECTOR(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float2 node_409 = i.uv0;
                float4 node_2 = tex2D(_Main_Texture,TRANSFORM_TEX(node_409.rg, _Main_Texture));
                clip(node_2.a - 0.5);
                SHADOW_COLLECTOR_FRAGMENT(i)
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags {
                "LightMode"="ShadowCaster"
            }
            Cull Off
            Offset 1, 1
            
            Fog {Mode Off}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_SHADOWCASTER
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcaster
            #pragma exclude_renderers flash d3d11_9x 
            #pragma target 3.0
            uniform sampler2D _Main_Texture; uniform float4 _Main_Texture_ST;
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                V2F_SHADOW_CASTER;
                float2 uv0 : TEXCOORD1;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv0 = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                float2 node_410 = i.uv0;
                float4 node_2 = tex2D(_Main_Texture,TRANSFORM_TEX(node_410.rg, _Main_Texture));
                clip(node_2.a - 0.5);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
