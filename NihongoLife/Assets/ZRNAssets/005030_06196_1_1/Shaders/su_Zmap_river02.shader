// Upgrade NOTE: commented out 'float4 unity_LightmapST', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_Lightmap', a built-in variable
// Upgrade NOTE: commented out 'sampler2D unity_LightmapInd', a built-in variable
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Upgrade NOTE: replaced tex2D unity_Lightmap with UNITY_SAMPLE_TEX2D
// Upgrade NOTE: replaced tex2D unity_LightmapInd with UNITY_SAMPLE_TEX2D_SAMPLER

// Shader created with Shader Forge v1.04 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.04;sub:START;pass:START;ps:flbk:,lico:1,lgpr:1,nrmq:1,limd:1,uamb:True,mssp:True,lmpd:True,lprd:False,rprd:False,enco:False,frtr:True,vitr:True,dbil:False,rmgx:True,rpth:0,hqsc:True,hqlp:False,tesm:0,blpr:0,bsrc:0,bdst:1,culm:0,dpts:2,wrdp:True,dith:2,ufog:True,aust:True,igpj:False,qofs:0,qpre:1,rntp:1,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:3068,x:34121,y:32714,varname:node_3068,prsc:2|diff-1663-OUT,diffpow-1084-OUT,spec-3140-OUT,gloss-812-OUT;n:type:ShaderForge.SFN_Color,id:5442,x:32834,y:32599,ptovrint:False,ptlb:near_color,ptin:_near_color,varname:node_5442,prsc:2,glob:False,c1:0.345098,c2:0.4156863,c3:0.4627451,c4:1;n:type:ShaderForge.SFN_Tex2d,id:8605,x:32607,y:32818,varname:node_8605,prsc:2,tex:2d1aa6f28e9894b4395e59af116e7eca,ntxv:3,isnm:False|UVIN-4812-UVOUT,TEX-1911-TEX;n:type:ShaderForge.SFN_Slider,id:812,x:33190,y:32812,ptovrint:False,ptlb:Gloss,ptin:_Gloss,varname:_node_4390_copy,prsc:2,min:0,cur:0.3,max:1;n:type:ShaderForge.SFN_Tex2dAsset,id:1911,x:31886,y:32891,ptovrint:False,ptlb:nise_texture,ptin:_nise_texture,varname:node_1911,tex:2d1aa6f28e9894b4395e59af116e7eca,ntxv:0,isnm:False;n:type:ShaderForge.SFN_Panner,id:4812,x:32392,y:32575,varname:node_4812,prsc:2,spu:1,spv:0.25|DIST-2522-OUT;n:type:ShaderForge.SFN_ValueProperty,id:8227,x:32007,y:32721,ptovrint:False,ptlb:tex_scroll_A,ptin:_tex_scroll_A,varname:node_8227,prsc:2,glob:False,v1:0.07;n:type:ShaderForge.SFN_Time,id:5963,x:32007,y:32575,varname:node_5963,prsc:2;n:type:ShaderForge.SFN_Multiply,id:2522,x:32187,y:32575,varname:node_2522,prsc:2|A-5963-TSL,B-8227-OUT;n:type:ShaderForge.SFN_Tex2d,id:4862,x:32607,y:32935,varname:node_4862,prsc:2,tex:2d1aa6f28e9894b4395e59af116e7eca,ntxv:0,isnm:False|UVIN-4252-UVOUT,TEX-1911-TEX;n:type:ShaderForge.SFN_Panner,id:4252,x:32417,y:33033,varname:node_4252,prsc:2,spu:1,spv:0.1|DIST-4457-OUT;n:type:ShaderForge.SFN_ValueProperty,id:819,x:32040,y:33179,ptovrint:False,ptlb:tex_scroll_B,ptin:_tex_scroll_B,varname:_node_8227_copy,prsc:2,glob:False,v1:0.05;n:type:ShaderForge.SFN_Time,id:1455,x:32040,y:33033,varname:node_1455,prsc:2;n:type:ShaderForge.SFN_Multiply,id:4457,x:32220,y:33033,varname:node_4457,prsc:2|A-1455-TSL,B-819-OUT;n:type:ShaderForge.SFN_Multiply,id:5537,x:32834,y:32818,varname:node_5537,prsc:2|A-8605-RGB,B-4862-RGB;n:type:ShaderForge.SFN_Tex2dAsset,id:841,x:32326,y:33322,ptovrint:False,ptlb:hamon_texture,ptin:_hamon_texture,varname:node_841,tex:311ca53f1614dd94d910ebdb85441def,ntxv:3,isnm:False;n:type:ShaderForge.SFN_Tex2d,id:7400,x:32685,y:33384,varname:node_26,prsc:2,tex:311ca53f1614dd94d910ebdb85441def,ntxv:2,isnm:False|UVIN-4252-UVOUT,TEX-841-TEX;n:type:ShaderForge.SFN_Multiply,id:9742,x:32878,y:33244,varname:node_9742,prsc:2|A-2803-RGB,B-7400-RGB;n:type:ShaderForge.SFN_Tex2d,id:2803,x:32685,y:33244,varname:node_2803,prsc:2,tex:311ca53f1614dd94d910ebdb85441def,ntxv:0,isnm:False|TEX-841-TEX;n:type:ShaderForge.SFN_Add,id:1084,x:33171,y:32615,varname:node_1084,prsc:2|A-5442-RGB,B-9742-OUT;n:type:ShaderForge.SFN_Add,id:5989,x:33607,y:32965,varname:node_5989,prsc:2|A-413-OUT,B-9742-OUT;n:type:ShaderForge.SFN_Color,id:8282,x:32834,y:32349,ptovrint:False,ptlb:far_color,ptin:_far_color,varname:node_8282,prsc:2,glob:False,c1:0.2313726,c2:0.2666667,c3:0.2588235,c4:1;n:type:ShaderForge.SFN_Add,id:2311,x:33171,y:32466,varname:node_2311,prsc:2|A-8282-RGB,B-9742-OUT;n:type:ShaderForge.SFN_Depth,id:7693,x:33165,y:32300,varname:node_7693,prsc:2;n:type:ShaderForge.SFN_ValueProperty,id:2754,x:33165,y:32241,ptovrint:False,ptlb:far_pos,ptin:_far_pos,varname:node_2754,prsc:2,glob:False,v1:1.5;n:type:ShaderForge.SFN_Lerp,id:2789,x:33546,y:32463,varname:node_2789,prsc:2|A-2311-OUT,B-1084-OUT,T-3496-OUT;n:type:ShaderForge.SFN_Subtract,id:9655,x:33351,y:32241,varname:node_9655,prsc:2|A-2754-OUT,B-7693-OUT;n:type:ShaderForge.SFN_ConstantClamp,id:3496,x:33535,y:32241,varname:node_3496,prsc:2,min:0,max:2|IN-9655-OUT;n:type:ShaderForge.SFN_Multiply,id:1663,x:33774,y:32577,varname:node_1663,prsc:2|A-2789-OUT,B-469-RGB;n:type:ShaderForge.SFN_VertexColor,id:469,x:33546,y:32635,varname:node_469,prsc:2;n:type:ShaderForge.SFN_Multiply,id:7506,x:33133,y:32971,varname:node_7506,prsc:2|A-5537-OUT,B-7137-OUT;n:type:ShaderForge.SFN_Vector1,id:7137,x:32834,y:32947,varname:node_7137,prsc:2,v1:2;n:type:ShaderForge.SFN_Multiply,id:3140,x:33809,y:32965,varname:node_3140,prsc:2|A-5989-OUT,B-9765-OUT;n:type:ShaderForge.SFN_Vector1,id:9765,x:33607,y:33092,varname:node_9765,prsc:2,v1:3;n:type:ShaderForge.SFN_Add,id:413,x:33347,y:32971,varname:node_413,prsc:2|A-7506-OUT,B-3978-OUT;n:type:ShaderForge.SFN_Vector1,id:3978,x:33133,y:33094,varname:node_3978,prsc:2,v1:0.2;proporder:5442-8282-2754-812-1911-841-8227-819;pass:END;sub:END;*/

Shader "su/su_Zmap_river02" {
    Properties {
        _near_color ("near_color", Color) = (0.345098,0.4156863,0.4627451,1)
        _far_color ("far_color", Color) = (0.2313726,0.2666667,0.2588235,1)
        _far_pos ("far_pos", Float ) = 1.5
        _Gloss ("Gloss", Range(0, 1)) = 0.3
        _nise_texture ("nise_texture", 2D) = "white" {}
        _hamon_texture ("hamon_texture", 2D) = "bump" {}
        _tex_scroll_A ("tex_scroll_A", Float ) = 0.07
        _tex_scroll_B ("tex_scroll_B", Float ) = 0.05
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
            Name "ForwardBase"
            Tags {
                "LightMode"="ForwardBase"
            }
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdbase_fullshadows
            #pragma exclude_renderers flash d3d11_9x 
            #pragma target 3.0
			#pragma glsl
            uniform float4 _TimeEditor;
            #ifndef LIGHTMAP_OFF
                // float4 unity_LightmapST;
                // sampler2D unity_Lightmap;
                #ifndef DIRLIGHTMAP_OFF
                    // sampler2D unity_LightmapInd;
                #endif
            #endif
            uniform float4 _near_color;
            uniform float _Gloss;
            uniform sampler2D _nise_texture; uniform float4 _nise_texture_ST;
            uniform float _tex_scroll_A;
            uniform float _tex_scroll_B;
            uniform sampler2D _hamon_texture; uniform float4 _hamon_texture_ST;
            uniform float4 _far_color;
            uniform float _far_pos;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD5;
                LIGHTING_COORDS(6,7)
                #ifndef LIGHTMAP_OFF
                    float2 uvLM : TEXCOORD8;
                #endif
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(unity_ObjectToWorld, float4(v.normal,0)).xyz;
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                #ifndef LIGHTMAP_OFF
                    o.uvLM = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float partZ = max(0,i.projPos.z - _ProjectionParams.g);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
/////// Vectors:
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
                #ifndef LIGHTMAP_OFF
                    float4 lmtex = UNITY_SAMPLE_TEX2D(unity_Lightmap,i.uvLM);
                    #ifndef DIRLIGHTMAP_OFF
                        float3 lightmap = DecodeLightmap(lmtex);
                        float3 scalePerBasisVector = DecodeLightmap(UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd,unity_Lightmap,i.uvLM));
                        UNITY_DIRBASIS
                        half3 normalInRnmBasis = saturate (mul (unity_DirBasis, float3(0,0,1)));
                        lightmap *= dot (normalInRnmBasis, scalePerBasisVector);
                    #else
                        float3 lightmap = DecodeLightmap(lmtex);
                    #endif
                #endif
                #ifndef LIGHTMAP_OFF
                    #ifdef DIRLIGHTMAP_OFF
                        float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                    #else
                        float3 lightDirection = normalize (scalePerBasisVector.x * unity_DirBasis[0] + scalePerBasisVector.y * unity_DirBasis[1] + scalePerBasisVector.z * unity_DirBasis[2]);
                        lightDirection = mul(lightDirection,tangentTransform); // Tangent to world
                    #endif
                #else
                    float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
                #endif
                float3 lightColor = _LightColor0.rgb;
                float3 halfDirection = normalize(viewDirection+lightDirection);
////// Lighting:
                float attenuation = LIGHT_ATTENUATION(i);
                float3 attenColor = attenuation * _LightColor0.xyz;
///////// Gloss:
                float gloss = _Gloss;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                float NdotL = max(0, dot( normalDirection, lightDirection ));
                float4 node_5963 = _Time + _TimeEditor;
                float2 node_4812 = (i.uv0+(node_5963.r*_tex_scroll_A)*float2(1,0.25));
                float4 node_8605 = tex2D(_nise_texture,TRANSFORM_TEX(node_4812, _nise_texture));
                float4 node_1455 = _Time + _TimeEditor;
                float2 node_4252 = (i.uv0+(node_1455.r*_tex_scroll_B)*float2(1,0.1));
                float4 node_4862 = tex2D(_nise_texture,TRANSFORM_TEX(node_4252, _nise_texture));
                float4 node_2803 = tex2D(_hamon_texture,TRANSFORM_TEX(i.uv0, _hamon_texture));
                float4 node_26 = tex2D(_hamon_texture,TRANSFORM_TEX(node_4252, _hamon_texture));
                float3 node_9742 = (node_2803.rgb*node_26.rgb);
                float3 specularColor = (((((node_8605.rgb*node_4862.rgb)*2.0)+0.2)+node_9742)*3.0);
                #if !defined(LIGHTMAP_OFF) && defined(DIRLIGHTMAP_OFF)
                    float3 directSpecular = float3(0,0,0);
                #else
                    float3 directSpecular = 1 * pow(max(0,dot(halfDirection,normalDirection)),specPow);
                #endif
                float3 specular = directSpecular * specularColor;
                #ifndef LIGHTMAP_OFF
                    #ifndef DIRLIGHTMAP_OFF
                        specular *= lightmap;
                    #else
                        specular *= (floor(attenuation) * _LightColor0.xyz);
                    #endif
                #else
                    specular *= (floor(attenuation) * _LightColor0.xyz);
                #endif
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                float3 node_1084 = (_near_color.rgb+node_9742);
                float3 indirectDiffuse = float3(0,0,0);
                #ifndef LIGHTMAP_OFF
                    float3 directDiffuse = float3(0,0,0);
                #else
                    float3 directDiffuse = pow(max( 0.0, NdotL), node_1084) * attenColor;
                #endif
                #ifndef LIGHTMAP_OFF
                    #ifdef SHADOWS_SCREEN
                        #if (defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)) && defined(SHADER_API_MOBILE)
                            directDiffuse += min(lightmap.rgb, attenuation);
                        #else
                            directDiffuse += max(min(lightmap.rgb,attenuation*lmtex.rgb), lightmap.rgb*attenuation*0.5);
                        #endif
                    #else
                        directDiffuse += lightmap.rgb;
                    #endif
                #endif
                indirectDiffuse += UNITY_LIGHTMODEL_AMBIENT.rgb; // Ambient Light
                float3 diffuse = (directDiffuse + indirectDiffuse) * (lerp((_far_color.rgb+node_9742),node_1084,clamp((_far_pos-partZ),0,2))*i.vertexColor.rgb);
/// Final Color:
                float3 finalColor = diffuse + specular;
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
            
            
            Fog { Color (0,0,0,0) }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDADD
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            #pragma multi_compile_fwdadd_fullshadows
            #pragma exclude_renderers flash d3d11_9x 
            #pragma target 3.0
			#pragma glsl
            uniform float4 _TimeEditor;
            uniform float4 _near_color;
            uniform float _Gloss;
            uniform sampler2D _nise_texture; uniform float4 _nise_texture_ST;
            uniform float _tex_scroll_A;
            uniform float _tex_scroll_B;
            uniform sampler2D _hamon_texture; uniform float4 _hamon_texture_ST;
            uniform float4 _far_color;
            uniform float _far_pos;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float3 tangentDir : TEXCOORD3;
                float3 binormalDir : TEXCOORD4;
                float4 vertexColor : COLOR;
                float4 projPos : TEXCOORD5;
                LIGHTING_COORDS(6,7)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = mul(unity_ObjectToWorld, float4(v.normal,0)).xyz;
                o.tangentDir = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz, 0.0 ) ).xyz );
                o.binormalDir = normalize(cross(o.normalDir, o.tangentDir) * v.tangent.w);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                float3 lightColor = _LightColor0.rgb;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos (o.pos);
                COMPUTE_EYEDEPTH(o.projPos.z);
                TRANSFER_VERTEX_TO_FRAGMENT(o)
                return o;
            }
            fixed4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
                float partZ = max(0,i.projPos.z - _ProjectionParams.g);
                float3x3 tangentTransform = float3x3( i.tangentDir, i.binormalDir, i.normalDir);
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
                float gloss = _Gloss;
                float specPow = exp2( gloss * 10.0+1.0);
////// Specular:
                float NdotL = max(0, dot( normalDirection, lightDirection ));
                float4 node_5963 = _Time + _TimeEditor;
                float2 node_4812 = (i.uv0+(node_5963.r*_tex_scroll_A)*float2(1,0.25));
                float4 node_8605 = tex2D(_nise_texture,TRANSFORM_TEX(node_4812, _nise_texture));
                float4 node_1455 = _Time + _TimeEditor;
                float2 node_4252 = (i.uv0+(node_1455.r*_tex_scroll_B)*float2(1,0.1));
                float4 node_4862 = tex2D(_nise_texture,TRANSFORM_TEX(node_4252, _nise_texture));
                float4 node_2803 = tex2D(_hamon_texture,TRANSFORM_TEX(i.uv0, _hamon_texture));
                float4 node_26 = tex2D(_hamon_texture,TRANSFORM_TEX(node_4252, _hamon_texture));
                float3 node_9742 = (node_2803.rgb*node_26.rgb);
                float3 specularColor = (((((node_8605.rgb*node_4862.rgb)*2.0)+0.2)+node_9742)*3.0);
                float3 directSpecular = attenColor * pow(max(0,dot(halfDirection,normalDirection)),specPow);
                float3 specular = directSpecular * specularColor;
/////// Diffuse:
                NdotL = max(0.0,dot( normalDirection, lightDirection ));
                float3 node_1084 = (_near_color.rgb+node_9742);
                float3 directDiffuse = pow(max( 0.0, NdotL), node_1084) * attenColor;
                float3 diffuse = directDiffuse * (lerp((_far_color.rgb+node_9742),node_1084,clamp((_far_pos-partZ),0,2))*i.vertexColor.rgb);
/// Final Color:
                float3 finalColor = diffuse + specular;
                return fixed4(finalColor * 1,0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
