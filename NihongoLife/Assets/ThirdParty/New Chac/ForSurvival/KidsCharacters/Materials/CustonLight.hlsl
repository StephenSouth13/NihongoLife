void MainLight_half(float3 WorldPos, out half3 Direction, out half3 Color, out half DistanceAtten, out half ShadowAtten)
{
	#ifdef SHADERGRAPH_PREVIEW
		Direction = half3(0.5, 0.5, 0);
		Color = 1;
		DistanceAtten = 1;
		ShadowAtten = 1;

	#else
		#if SHADOWS_SCREEN
			half4 clipPos = TransformWorldToClip(WorldPos);
			half4 shadowCoord = ComputeScreenPos(clipPos);
		#else
			half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
		#endif

		Light mainLight = GetMainLight(shadowCoord);
		Direction = mainLight.direction;
		Color = mainLight.color;
		DistanceAtten = mainLight.distanceAttenuation;

		#if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
			ShadowAtten = 1.0h;
		#endif

		// NihongoLife: this graph is Unlit, so URP never compiles a shadow variant of it. On WebGL / GLES the shadow
		// map is then not a depth texture, and a shader that still samples it with a shadow sampler is refused by the
		// browser ("Mismatch between texture format and sampler type") — the character is simply not drawn (the
		// empty character-select preview on the web). There, only sample when a shadow keyword is really on.
		#if (defined(SHADER_API_GLES3) || defined(SHADER_API_GLES) || defined(SHADER_API_WEBGPU)) && !defined(_MAIN_LIGHT_SHADOWS) && !defined(_MAIN_LIGHT_SHADOWS_CASCADE) && !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
			ShadowAtten = 1.0h;
		#elif SHADOWS_SCREEN
			ShadowAtten = SampleScreenSpaceShadowmap(shadowCoord);
		#else
			ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
			half4 shadowParams = GetMainLightShadowParams();
			ShadowAtten = SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture),TransformWorldToShadowCoord(WorldPos), shadowSamplingData, shadowParams, false);
		#endif
	#endif
}