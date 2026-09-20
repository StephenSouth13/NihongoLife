// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Shader created with Shader Forge v1.04 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.04;sub:START;pass:START;ps:flbk:,lico:1,lgpr:1,nrmq:1,limd:1,uamb:True,mssp:True,lmpd:False,lprd:False,rprd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,rpth:0,hqsc:True,hqlp:False,tesm:0,blpr:1,bsrc:3,bdst:7,culm:0,dpts:2,wrdp:False,dith:2,ufog:True,aust:True,igpj:True,qofs:0,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:1521,x:33370,y:32682,varname:node_1521,prsc:2|diff-7928-RGB,spec-929-OUT,gloss-7666-OUT,alpha-3406-R;n:type:ShaderForge.SFN_Tex2d,id:7928,x:32898,y:32421,ptovrint:False,ptlb:tex_defuse,ptin:_tex_defuse,varname:node_7928,prsc:2,tex:2223ddcf59a316649abd5fdb98818ea7,ntxv:0,isnm:False|UVIN-4027-UVOUT;n:type:ShaderForge.SFN_Tex2d,id:3406,x:32889,y:33042,ptovrint:False,ptlb:tex_alpha,ptin:_tex_alpha,varname:_node_7928_copy,prsc:2,tex:47e1f52776f46ac469838049d25662db,ntxv:0,isnm:False|UVIN-1803-UVOUT;n:type:ShaderForge.SFN_Tex2d,id:9163,x:32846,y:32737,ptovrint:False,ptlb:tex_spec,ptin:_tex_spec,varname:_node_7928_copy_copy,prsc:2,tex:5e59d0e7afe639d40888035cdbd308ce,ntxv:0,isnm:False|UVIN-1803-UVOUT;n:type:ShaderForge.SFN_ValueProperty,id:7666,x:32859,y:32925,ptovrint:False,ptlb:gloss_val,ptin:_gloss_val,varname:node_7666,prsc:2,glob:False,v1:0.2;n:type:ShaderForge.SFN_Multiply,id:929,x:33056,y:32737,varname:node_929,prsc:2|A-627-OUT,B-9163-RGB;n:type:ShaderForge.SFN_ValueProperty,id:627,x:33056,y:32681,ptovrint:False,ptlb:spec_val,ptin:_spec_val,varname:node_627,prsc:2,glob:False,v1:1.3;n:type:ShaderForge.SFN_Time,id:9689,x:32086,y:32704,varname:node_9689,prsc:2;n:type:ShaderForge.SFN_Multiply,id:7906,x:32355,y:32706,varname:node_7906,prsc:2|A-4323-OUT,B-9689-T;n:type:ShaderForge.SFN_ValueProperty,id:4323,x:32355,y:32643,ptovrint:False,ptlb:scroll_spec,ptin:_scroll_spec,varname:node_4323,prsc:2,glob:False,v1:1.2;n:type:ShaderForge.SFN_Panner,id:1803,x:32590,y:32749,varname:node_1803,prsc:2,spu:0,spv:1|UVIN-2411-UVOUT,DIST-7906-OUT;n:type:ShaderForge.SFN_TexCoord,id:2411,x:32355,y:32848,varname:node_2411,prsc:2,uv:0;n:type:ShaderForge.SFN_Multiply,id:4883,x:32349,y:32312,varname:node_4883,prsc:2|A-6306-OUT,B-9689-T;n:type:ShaderForge.SFN_ValueProperty,id:6306,x:32349,y:32249,ptovrint:False,ptlb:scroll_def,ptin:_scroll_def,varname:_scroll_spec_copy,prsc:2,glob:False,v1:0.9;n:type:ShaderForge.SFN_Panner,id:4027,x:32584,y:32355,varname:node_4027,prsc:2,spu:0,spv:1|UVIN-1711-UVOUT,DIST-4883-OUT;n:type:ShaderForge.SFN_TexCoord,id:1711,x:32349,y:32454,varname:node_1711,prsc:2,uv:0;proporder:7928-3406-9163-627-7666-6306-4323;pass:END;sub:END;*/

Shader "su/su_Zmap_funsui" {
    Properties {
        _tex_defuse ("tex_defuse", 2D) = "white" {}
        _tex_alpha ("tex_alpha", 2D) = "white" {}
        _tex_spec ("tex_spec", 2D) = "white" {}
        _spec_val ("spec_val", Float ) = 1.3
        _gloss_val ("gloss_val", Float ) = 0.2
        _scroll_def ("scroll_def", Float ) = 0.9
        _scroll_spec ("scroll_spec", Float ) = 1.2
        [HideInInspector]_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }
        Pass {
            Name "ForwardBase"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma exclude_renderers flash d3d11_9x 
            #pragma target 3.0
            uniform float4 _LightColor0;
            uniform float4 _TimeEditor;
            uniform sampler2D _tex_defuse; uniform float4 _tex_defuse_ST;
            uniform sampler2D _tex_alpha; uniform float4 _tex_alpha_ST;
            uniform sampler2D _tex_spec; uniform float4 _tex_spec_ST;
            uniform float _gloss_val;
            uniform float _spec_val;
            uniform float _scroll_spec;
            uniform float _scroll_def;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = mul(unity_ObjectToWorld, float4(v.normal,0)).xyz;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
/////// Vectors:
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                float3 lightColor = _LightColor0.rgb;
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = 1;
                float3 attenColor = attenuation * _LightColor0.xyz;
///////// Gloss:
                float gloss = _gloss_val;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                float NdotL = max(0, dot( normalDirection, lightDirection ));
                float4 node_9689 = _Time + _TimeEditor;
                float2 node_1803 = (i.uv0+(_scroll_spec*node_9689.g)*float2(0,1));
                float4 _tex_spec_var = tex2D(_tex_spec,TRANSFORM_TEX(node_1803, _tex_spec));
                float3 specularColor = (_spec_val*_tex_spec_var.rgb);
                float3 directSpecular = (floor(attenuation) * _LightColor0.xyz) * pow(max(0,dot(halfDirection,normalDirection)),specPow);
                float3 specular = directSpecular * specularColor;
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                float3 indirectDiffuse = float3(0,0,0);
                float3 directDiffuse = max( 0.0, NdotL) * attenColor;
                indirectDiffuse += UNITY_LIGHTMODEL_AMBIENT.rgb; // Ambient Light
                float2 node_4027 = (i.uv0+(_scroll_def*node_9689.g)*float2(0,1));
                float4 _tex_defuse_var = tex2D(_tex_defuse,TRANSFORM_TEX(node_4027, _tex_defuse));
                float3 diffuse = (directDiffuse + indirectDiffuse) * _tex_defuse_var.rgb;
/// Final Color:
                float3 finalColor = diffuse + specular;
                float4 _tex_alpha_var = tex2D(_tex_alpha,TRANSFORM_TEX(node_1803, _tex_alpha));
                return fixed4(finalColor,_tex_alpha_var.r);
            }
            ENDCG
        }
        Pass {
            Name "ForwardAdd"
            Tags {
                "LightMode"="ForwardAdd"
            }
            Blend One One
            ZWrite Off
            
            Fog { Color (0,0,0,0) }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDADD
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #pragma multi_compile_fwdadd
            #pragma exclude_renderers flash d3d11_9x 
            #pragma target 3.0
            uniform float4 _LightColor0;
            uniform float4 _TimeEditor;
            uniform sampler2D _tex_defuse; uniform float4 _tex_defuse_ST;
            uniform sampler2D _tex_alpha; uniform float4 _tex_alpha_ST;
            uniform sampler2D _tex_spec; uniform float4 _tex_spec_ST;
            uniform float _gloss_val;
            uniform float _spec_val;
            uniform float _scroll_spec;
            uniform float _scroll_def;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                LIGHTING_COORDS(3,4)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.normalDir = mul(unity_ObjectToWorld, float4(v.normal,0)).xyz;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos(v.vertex);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
/////// Vectors:
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz,_WorldSpaceLightPos0.w));
                float3 lightColor = _LightColor0.rgb;
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
///////// Gloss:
                float gloss = _gloss_val;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                float NdotL = max(0, dot( normalDirection, lightDirection ));
                float4 node_9689 = _Time + _TimeEditor;
                float2 node_1803 = (i.uv0+(_scroll_spec*node_9689.g)*float2(0,1));
                float4 _tex_spec_var = tex2D(_tex_spec,TRANSFORM_TEX(node_1803, _tex_spec));
                float3 specularColor = (_spec_val*_tex_spec_var.rgb);
                float3 directSpecular = attenColor * pow(max(0,dot(halfDirection,normalDirection)),specPow);
                float3 specular = directSpecular * specularColor;
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                float3 directDiffuse = max( 0.0, NdotL) * attenColor;
                float2 node_4027 = (i.uv0+(_scroll_def*node_9689.g)*float2(0,1));
                float4 _tex_defuse_var = tex2D(_tex_defuse,TRANSFORM_TEX(node_4027, _tex_defuse));
                float3 diffuse = directDiffuse * _tex_defuse_var.rgb;
/// Final Color:
                float3 finalColor = diffuse + specular;
                float4 _tex_alpha_var = tex2D(_tex_alpha,TRANSFORM_TEX(node_1803, _tex_alpha));
                return fixed4(finalColor * _tex_alpha_var.r,0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
