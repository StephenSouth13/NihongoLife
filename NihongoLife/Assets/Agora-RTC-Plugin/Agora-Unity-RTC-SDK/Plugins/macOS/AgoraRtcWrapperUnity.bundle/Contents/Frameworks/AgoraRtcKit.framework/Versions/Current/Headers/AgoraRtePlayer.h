/**
 *
 * Agora Real Time Engagement
 * Copyright (c) 2024 Agora IO. All rights reserved.
 *
 */

#import <Foundation/Foundation.h>

#import "AgoraRteEnumerates.h"

@class AgoraRteError;
@class AgoraRte;
@class AgoraRtePlayerCustomSourceProvider;
@class AgoraRteStream;
@class AgoraRteCanvas;
@class AgoraRtePlayerObserver;

/**
 * The PlayerInitialConfig class is used to initialize the Player object.
 * @since v4.4.0
 * @technical preview
 */
__attribute__((visibility("default"))) @interface AgoraRtePlayerInitialConfig : NSObject

-(instancetype _Nonnull)init;

@end

/**
 * @brief This class provides methods related to Media player options.
 *
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRtePlayerConfig : NSObject

-(instancetype _Nonnull)init;

/**
 * @brief Sets whether to enable autoplay.
 *
 * @since v4.4.0
 *
 * @details
 * You can call this method to set whether to enable autoplay before calling
 * `openWithUrl:startTime:cb:` to open a Media Pull stream. If not set, autoplay is enabled by
 * default.  
 * Call timing: This method must be called before `openWithUrl:startTime:cb:`.
 *
 * @param autoPlay Whether to enable autoplay:
 * - `YES`: (Default) Enable autoplay.
 * - `NO`: Disable autoplay.
 * @param error Status or error information. See `AgoraRteError` for details.
 *
 */
- (void)setAutoPlay:(BOOL)autoPlay error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the autoplay setting.
 *
 * @since v4.4.0
 *
 * @details
 * You can call this method to retrieve the current autoplay setting of the media player.
 * Call timing: This method must be called after `getConfigs:error:`.
 *
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the autoplay setting was successfully retrieved:
 * - `YES`: Retrieval succeeded.
 * - `NO`: Retrieval failed.
 */
- (BOOL)autoPlay:(AgoraRteError * _Nullable)error;

/**
 * @brief Sets the playback speed parameter.
 *
 * @since v4.5.1
 *
 * @note You can call this method after calling the `openWithUrl:startTime:cb:` method.
 *
 * @param speed Playback speed, with a valid range of [50, 400].
 * @param error Output parameter used to return error information. See `AgoraRteError` for details.
 *
 */
- (void)setPlaybackSpeed:(int32_t)speed error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the playback speed parameter.
 *
 * @since v4.5.1
 *
 * @param error Output parameter that indicates error information. See `AgoraRteError`.
 *
 * @return
 * If the method call succeeds, returns the value of the playback speed.
 */
- (int32_t)playbackSpeed:(AgoraRteError * _Nullable)error;

- (void)setPlayoutAudioTrackIdx:(int)idx error:(AgoraRteError * _Nullable)error;
- (int)playoutAudioTrackIdx:(AgoraRteError * _Nullable)error;

- (void)setPublishAudioTrackIdx:(int)idx error:(AgoraRteError * _Nullable)error;
- (int)publishAudioTrackIdx:(AgoraRteError * _Nullable)error;

- (void)setAudioTrackIdx:(int)idx error:(AgoraRteError * _Nullable)error;
- (int)audioTrackIdx:(AgoraRteError * _Nullable)error;

- (void)setSubtitleTrackIdx:(int)idx error:(AgoraRteError * _Nullable)error;
- (int)subtitleTrackIdx:(AgoraRteError * _Nullable)error;

- (void)setExternalSubtitleTrackIdx:(int)idx error:(AgoraRteError * _Nullable)error;
- (int)externalSubtitleTrackIdx:(AgoraRteError * _Nullable)error;

- (void)setAudioPitch:(int32_t)pitch error:(AgoraRteError * _Nullable)error;
- (int32_t)audioPitch:(AgoraRteError * _Nullable)error;

/**
 * @brief Sets the playback volume parameter.
 *
 * @since v4.5.1
 *
 * @details
 * You can use this method to set the playback volume. The valid range is [0, 400].
 *
 * @param volume The playback volume to set. The valid range is [0, 400].
 * @param error Output parameter that returns error information. See `AgoraRteError` for details.
 * - AgoraRteOk: The setting is successful.
 * - AgoraRteErrorInvalidArgument: The volume parameter is invalid.
 *
 */
- (void)setPlayoutVolume:(int32_t)volume error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the playback volume of the media player.
 *
 * @since v4.5.1
 *
 * @param error Output parameter that returns the error information. See `AgoraRteError`.
 *
 * @return
 * Returns the playback volume of the media player if the method call succeeds.
 */
- (int32_t)playoutVolume:(AgoraRteError * _Nullable)error;

- (void)setAudioPlaybackDelay:(int32_t)delay error:(AgoraRteError * _Nullable)error;
- (int32_t)audioPlaybackDelay:(AgoraRteError * _Nullable)error;

- (void)setAudioDualMonoMode:(int)mode error:(AgoraRteError * _Nullable)error;
- (int)audioDualMonoMode:(AgoraRteError * _Nullable)error;

- (void)setPublishVolume:(int32_t)volume error:(AgoraRteError * _Nullable)error;
- (int32_t)publishVolume:(AgoraRteError * _Nullable)error;

/**
 * @brief Sets the loop count for playing a media file.
 *
 * @since v4.5.1
 *
 * @param count The number of times the media file is played:
 * - `1`: Plays once.
 * - `2`: Plays twice.
 * - `-1`: Plays in an infinite loop until `pause:` is called.
 * @param error Output parameter that indicates error information. May return the following
 * `AgoraRteErrorCode`:
 * - AgoraRteOk: The call succeeds.
 * - AgoraRteErrorInvalidArgument: The `count` parameter is set to an invalid value. See
 * `AgoraRteError` for details.
 *
 */
- (void)setLoopCount:(int32_t)count error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the loop count parameter of the media file.
 *
 * @since v4.5.1
 *
 * @param error Output parameter that indicates error information. See `AgoraRteError`.
 *
 * @return
 * If the method call succeeds, returns the loop count of the media file.
 */
- (int32_t)loopCount:(AgoraRteError * _Nullable)error;

/**
  * Set player private parameters. This parameter setting can be done according to actual needs, referring to the suggestions of Agora SA.
  * @since v4.4.0
  * @param jsonParameter JSON formatted string
  * @param error AgoraRteError object may return the following AgoraRteErrorCode
  *  - AgoraRteOk: Success
  *  - AgoraRteErrorInvalidArgument: Indicates that the jsonParameter parameter is empty.
  * @return void
  */
- (void)setJsonParameter:(NSString * _Nonnull)jsonParameter error:(AgoraRteError * _Nullable)error;

/**
  * Get the currently configured private parameters of the AgoraRtePlayer.
  * @since v4.4.0
  * @param error AgoraRteError object may return the following AgoraRteErrorCode
  *  - AgoraRteOk: Success
  * @return NSString
  */
- (NSString * _Nullable)jsonParameter:(AgoraRteError * _Nullable)error;

/**
 * @brief Sets the quality layer of the subscribed media stream.
 *
 * @since v4.4.0
 *
 * @details
 * You can switch between different video quality layers of a multi-layer video stream based on the
 * actual network conditions, each corresponding to a different resolution.
 * By default, after calling this method, the audience can only switch between the high and low
 * video streams, that is, between `AgoraRteAbrSubscriptionHigh` and `AgoraRteAbrSubscriptionLow`.
 * If you require more advanced resolution switching, you can contact technical support to enable
 * the ABR (Adaptive Bitrate) feature. Once enabled, you can customize the resolution for each video
 * quality layer, and the audience can switch between all available quality layers of the video
 * stream as needed. For implementation details, see Media Pull on the audience side.
 * Applicable scenarios: In unstable network environments, the audience can select an appropriate
 * video quality level based on the current network conditions to ensure a smooth viewing
 * experience.
 * Call timing: You must call this method before `setConfigs:error:`.
 *
 * @note If you have not enabled the ABR feature when calling this method, the subscribed video
 * quality layer can only be set to `AgoraRteAbrSubscriptionHigh` or `AgoraRteAbrSubscriptionLow`.
 *
 * @param layer The video quality layer to subscribe to. See `AgoraRteAbrSubscriptionLayer`.
 *                     When customizing the resolution for `AgoraRteAbrSubscriptionLayer1` to
 * `AgoraRteAbrSubscriptionLayer6`,
 *                     you can refer to the table below or define them based on your actual
 * requirements:
 *                     Note: When customizing resolutions, make sure to sort the video quality
 * layers from highest to lowest resolution. If the resolutions are the same, sort them by frame
 * rate from highest to lowest.
 * | Video Quality Layer                    | Resolution   |
 * | -------------------------------------- | ------------ |
 * | `AgoraRteAbrSubscriptionLayer1`        | 2160p (4K)   |
 * | `AgoraRteAbrSubscriptionLayer2`        | 1440p (2K)   |
 * | `AgoraRteAbrSubscriptionLayer3`        | 1080p (HD)   |
 * | `AgoraRteAbrSubscriptionLayer4`        | 720p (HD)    |
 * | `AgoraRteAbrSubscriptionLayer5`        | 540p (SD)    |
 * | `AgoraRteAbrSubscriptionLayer6`           | 480p (SD)    |
 * @param error Status or error information. See `AgoraRteError`.
 *
 */
- (void)setAbrSubscriptionLayer:(AgoraRteAbrSubscriptionLayer)layer error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the quality layer of the subscribed video stream.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method after `getConfigs:error:`.
 *
 * @param error The status or error information. See `AgoraRteError`.
 *
 * @return
 * The currently set video quality layer. See `AgoraRteAbrSubscriptionLayer`.
 */
- (AgoraRteAbrSubscriptionLayer)abrSubscriptionLayer:(AgoraRteError * _Nullable)error;

/**
 * @brief Sets the fallback option for subscribed remote video streams.
 *
 * @since v4.4.0
 *
 * @details
 * In poor network conditions, the quality of real-time audio and video communication may degrade.
 * After calling this method, the SDK falls back the resolution of the remote video streams to the
 * specified video quality layer when network conditions deteriorate. Each video quality layer
 * corresponds to a specific resolution and bitrate. The SDK continuously monitors network quality
 * and restores the subscribed video stream to the original quality layer when network conditions
 * improve.
 * 
 * By default, after calling this method, you can only choose to fall back to
 * `AgoraRteAbrFallbackLow` or `AgoraRteAbrFallbackAudioOnly`. If you require higher video
 * experience, you can contact technical support to enable the ABR feature. Once enabled, you can
 * fall back to all video quality layers and customize the resolution for each layer.
 * 
 * Applicable scenarios:
 * - In typical scenarios, you can call this method and set the fallback layer to
 * `AgoraRteAbrFallbackLow` or `AgoraRteAbrFallbackAudioOnly`. The SDK then falls back the remote
 * video streams to a lower-quality stream or to audio-only when the network is poor.
 * - If you have higher requirements for the fallback resolution of remote video streams, you can
 * contact technical support to enable the ABR feature. After enabling it, you can customize the
 * resolution for each layer. The SDK uses the resolution of the lowest quality layer you specify as
 * the lower bound and dynamically adjusts the resolution within this range based on network
 * conditions. For detailed implementation, see Viewer-side URL Streaming.
 * 
 * Call timing: You must call this method before `setConfigs:error:`.
 *
 * @note If the ABR feature is not enabled, the fallback quality layer for remote video streams can
 * only be set to `AgoraRteAbrFallbackLow` or `AgoraRteAbrFallbackAudioOnly`.
 *
 * @param layer The fallback video quality layer. See `AgoraRteAbrFallbackLayer`. When customizing
 * the resolution for `AgoraRteAbrFallbackLayer1` to `AgoraRteAbrFallbackLayer6`, you can refer to
 * the table below for guidance or define them based on your actual needs:
 * | Video Quality Layer                  | Resolution   |
 * | ----------------------------------- | ------------ |
 * | `AgoraRteAbrSubscriptionLayer1`     | 2160p (4K)   |
 * | `AgoraRteAbrSubscriptionLayer2`     | 1440p (2K)   |
 * | `AgoraRteAbrSubscriptionLayer3`     | 1080p (HD)   |
 * | `AgoraRteAbrSubscriptionLayer4`     | 720p (HD)    |
 * | `AgoraRteAbrSubscriptionLayer5`     | 540p (SD)    |
 * | `AgoraRteAbrSubscriptionLayer6`     | 480p (SD)    |
 * 
 * Note: When customizing resolutions, you must sort the video quality layers from highest to lowest
 * resolution. If two layers have the same resolution, sort them by frame rate in descending order.
 * 
 * @param error The status or error information. See `AgoraRteError`.
 *
 */
- (void)setAbrFallbackLayer:(AgoraRteAbrFallbackLayer)layer error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the configured video stream fallback option.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method after `getConfigs:error:`.
 *
 * @param error Status or error information. See `AgoraRteError` for details.
 *
 * @return
 * The configured video stream fallback option. See `AgoraRteAbrFallbackLayer` for details.
 */
- (AgoraRteAbrFallbackLayer)abrFallbackLayer:(AgoraRteError * _Nullable)error;

@end

/**
 * @brief Statistics of the media resource being played.
 *
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRtePlayerStats : NSObject

- (instancetype _Nonnull)init;

/**
 * Video decoding frame rate (fps).
 */
- (int)videoDecodeFrameRate;

/**
 * Video rendering frame rate (fps).
 */
- (int)videoRenderFrameRate;

/**
 * Video bitrate (Kbps).
 */
- (int)videoBitrate;

/**
 * Audio bitrate (Kbps).
 */
- (int)audioBitrate;

@end

/**
 * @brief Information about the player and media stream.
 *
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRtePlayerInfo : NSObject
- (instancetype _Nonnull)init;

/**
 * The current state of the player. See `AgoraRtePlayerState` for details.
 */
- (int)state;

/**
 * Duration of the media resource (ms).
 * @since v4.5.1
 */
- (long)duration;

/**
 * The stream count.
 */
- (int)streamCount;

/**
 * Whether the URL Media Pull contains a remote audio stream:
 * - `YES: Contains a remote audio stream.`
 * - `NO: Does not contain a remote audio stream.`
 */
- (BOOL)hasAudio;

/**
 * Whether the URL media stream contains remote video streams:
 * - `YES: Contains remote video streams.`
 * - `NO: Does not contain remote video streams.`
 */
- (BOOL)hasVideo;

/**
 * Whether the player stops receiving the remote audio stream:
 * - `YES: Stops receiving the remote audio stream.`
 * - `NO: Continues receiving the remote audio stream.`
 * @since v4.4.0
 */
- (BOOL)isAudioMuted;

/**
 * Whether the player stops receiving remote video streams:
 * - `YES: Stops receiving remote video streams.`
 * - `NO: Continues receiving remote video streams.`
 * @since v4.4.0
 */
- (BOOL)isVideoMuted;

/**
 * Height of the video frame in pixels (px).
 * @since v4.5.1
 */
- (int)videoHeight;

/**
 * Width of the video frame (px).
 * @since v4.5.1
 */
- (int)videoWidth;

/**
 * The quality level of the subscribed video stream. In adaptive bitrate (ABR) multi-layer video
 * stream, different quality levels correspond to different resolutions and bitrates. See
 * `AgoraRteAbrSubscriptionLayer` for details.
 */
- (AgoraRteAbrSubscriptionLayer)abrSubscriptionLayer;

/**
 * Audio sample rate (Hz).
 * @since v4.5.1
 */
- (int)audioSampleRate;

/**
 * Number of audio channels.
 */
- (int)audioChannels;

/**
 * The number of bits per raw audio data sample.
 * @since v4.5.1
 */
- (int)audioBitsPerSample;

/**
 * The URL that is currently being played.
 */
- (NSString * _Nullable)currentUrl;

@end


/*
 * Player class, can be used to play URL resources.
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRtePlayer : NSObject

/**
 * @brief Preloads a URL resource.
 *
 * @since v4.4.0
 *
 * @details
 * Successfully preloading a URL resource can speed up the opening of the resource via
 * `openWithUrl:startTime:cb:`. When you need to use these resources, they can be accessed more
 * quickly, reducing wait time.
 * Applicable scenarios: Preloading before opening media resources can reduce user wait time and
 * provide a smoother audiovisual experience.
 * Call timing: This method must be called before `openWithUrl:startTime:cb:`.
 *
 * @note This method currently only takes effect for URLs with the `rte://` prefix. You can preload
 * up to 20 URLs. If the limit is exceeded, the newly preloaded URL will replace the earliest one.
 *
 * @param url A URL with the `rte://` prefix. For detailed field descriptions, see `Audience-side
 * URL Media Pull`.
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the preload is successful:
 * - `YES`: Preload succeeded.
 * - `NO`: Preload failed.
 */
+ (BOOL)preloadWithUrl:(NSString * _Nonnull)url error:(AgoraRteError * _Nullable)error;

/**
 * @brief Constructs a player object.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method after `initMediaEngine:error:`.
 *
 * @param rte An `AgoraRte` object.
 * @param config Initialization settings for the player object. You can pass a null pointer.
 *
 */
- (instancetype _Nonnull)initWithRte:(AgoraRte * _Nonnull)rte initialConfig:(AgoraRtePlayerInitialConfig * _Nullable)config;

/**
 * @brief Opens a URL resource.
 *
 * @since v4.4.0
 *
 * @details
 * This method supports opening real-time streaming media via a URL. If you want to speed up the
 * process of opening the URL resource, you can call `preloadWithUrl:error:` to preload the resource
 * before calling this method.
 * If the resource fails to open, you will receive the `onStateChanged:newState:error:` callback
 * reporting the link state as `AgoraRtePlayerStateFailed`. In this case, you need to first call
 * `pause:`, then call `openWithUrl:startTime:cb:` again to reopen the URL resource. If you have
 * disabled autoplay, you can call `play:` to start playback after the resource is opened.
 * Call timing: This method must be called after `initWithRte:initialConfig:`.
 * Related callbacks: After calling this method, the `onStateChanged:newState:error:` callback is
 * triggered, reporting the player link state as `AgoraRtePlayerStateOpening`, indicating that the
 * URL is being opened. Once successfully opened, the player link state is reported as
 * `AgoraRtePlayerStateOpenCompleted`.
 *
 * @note This method currently only supports opening URLs that start with `rte://`, direct CDN
 * streaming URLs, and local media files.
 *
 * @param url The URL to open. It can be a direct CDN streaming URL, a local media file, or a URL
 * prefixed with `rte://`. For detailed information on each field of the RTE URL, see "Audience-side
 * URL streaming".
 * @param startTime The starting playback position, in milliseconds.
 * @param cb An asynchronous callback function used to notify the result of opening the URL
 * resource. If an error occurs during the process, you can retrieve detailed error information from
 * the `err` parameter in the callback. See `AgoraRteError` for details.
 *
 */
- (void)openWithUrl:(NSString * _Nonnull)url startTime:(uint64_t)startTime cb:(void (^_Nullable)(AgoraRteError* _Nullable err))cb;

- (void)openWithCustomSourceProvider:(AgoraRtePlayerCustomSourceProvider * _Nonnull)provider startTime:(uint64_t)startTime cb:(void (^_Nullable)(AgoraRteError* _Nullable err))cb;

- (void)openWithStream:(AgoraRteStream * _Nonnull)stream cb:(void (^_Nullable)(AgoraRteError* _Nullable err))cb;

/**
 * @brief Switches to a new URL during playback.
 *
 * @since v4.5.1
 *
 * @note This method is only effective when the player is opened with a non-RTE URL. You must call
 * this method after the SDK returns the player state as `AgoraRtePlayerStateOpenCompleted`.
 *
 * @param url The target URL to switch to.
 * @param syncPts - `YES`: Synchronizes the playback position after switching (in milliseconds).
 * - `NO`: (Default) Does not synchronize the playback position after switching.
 * @param cb The callback for asynchronously notifying the result of the switch operation. You can
 * check the `err` parameter in the callback to determine the result or error code, as defined in
 * `AgoraRteError`.
 *
 */
- (void)switchWithUrl:(NSString * _Nonnull)url syncPts:(BOOL)syncPts cb:(void (^_Nullable)(AgoraRteError* _Nullable err))cb;

/**
 * @brief Retrieves statistics of the media resource currently being played by the player.
 *
 * @since v4.4.0
 *
 * @details
 * This method retrieves the player's statistics, including decoding and rendering frame rates,
 * audio and video bitrate, etc., and returns the result asynchronously through a callback function.
 * Call timing: You must call this method after `openWithUrl:startTime:cb:`.
 *
 * @param cb An asynchronous callback function that notifies you of the result of retrieving the
 * player's media resource statistics and any error information. The function includes the following
 * parameters:
 * - `stats`: The statistics of the media resource currently being played by the player. See
 * `AgoraRtePlayerStats`.
 * - `err`: Error information. See `AgoraRteError`.
 *
 */
- (void)getStats:(void (^_Nonnull)(AgoraRtePlayerStats* _Nonnull stats, AgoraRteError* _Nullable err))cb;

/**
 * @brief Sets the view window to display video.
 *
 * @since v4.4.0
 *
 * @details
 * This method specifies a Canvas object to display video. After the video stream starts playing
 * successfully, the video will be rendered on the specified Canvas.
 * Call timing: You must call this method after `initWithRte:initialConfig:`.
 *
 * @param canvas The Canvas object used to render video frames. See `AgoraRteCanvas` for details.
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the setting is successful:
 * - `YES`: The setting is successful.
 * - `NO`: The setting fails. You can get detailed information from the error code returned via the
 * `error` parameter.
 */
- (BOOL)setCanvas:(AgoraRteCanvas *_Nonnull)canvas error:(AgoraRteError * _Nullable)error;

/**
 * @brief Plays a URL resource.
 *
 * @since v4.4.0
 *
 * @details
 * If you have disabled autoplay, after successfully opening a live stream using
 * `openWithUrl:startTime:cb:`, you can call this method to start playback.
 * Call timing: Call this method only after receiving the `onStateChanged:newState:error:` callback
 * reporting the state as `AgoraRtePlayerStateOpenCompleted`.
 * Related callbacks: After this method is successfully called, the `onStateChanged:newState:error:`
 * callback is triggered to report the player state.
 *
 * @note This method currently only supports URLs prefixed with `rte://`.
 *
 * @param error The link state or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether playback is successful:
 * - `YES`: Playback succeeded.
 * - `NO`: Playback failed.
 */
- (BOOL)play:(AgoraRteError * _Nullable)error;

/**
 * Stop playback.
 * @since v4.4.0
 * @param error AgoraRteError object may return the following AgoraRteErrorCode
 *  - AgoraRteOk: Success
 *  - AgoraRteErrorInvalidOperation: The corresponding internal AgoraRtePlayer object has been destroyed or is invalid.
 * @return BOOL The result of the stop operation. If it fails, you can check the specific error through err.
 *  - YES: Successfully stop.
 *  - NO: Failed to stop.
 */
- (BOOL)stop:(AgoraRteError * _Nullable)error;

/**
 * @brief Pauses playback.
 *
 * @since v4.4.0
 *
 * @details
 * After you call `play:` to play a URL resource, you can call this method if you want to pause the
 * playback. If you want to stop the playback, call `pause:` instead.  
 * Call timing: You must call this method after `play:` has been called.  
 * Related callbacks: After this method is successfully called, the `onStateChanged:newState:error:`
 * callback is triggered to report the player state as `AgoraRtePlayerStatePaused`.
 *
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the playback is paused successfully:
 * - `YES`: Playback paused successfully.
 * - `NO`: Failed to pause playback.
 */
- (BOOL)pause:(AgoraRteError * _Nullable)error;

/**
 * @brief Seeks to a specific playback position.
 *
 * @since v4.5.1
 *
 * @param newTime The playback position you want to seek to.
 * @param error Output parameter used to return error information if the seek operation fails. See
 * `AgoraRteError`.
 *
 * @return
 * Whether the seeking operation is successful:
 * - `YES`: Operation succeeds.
 * - `NO`: Operation fails.
 */
- (BOOL)seek:(uint64_t)newTime error:(AgoraRteError * _Nullable)error;

/**
 * @brief Starts or stops receiving the audio stream.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method after `openWithUrl:startTime:cb:`.
 * Related callbacks: After this method is called successfully, the `onPlayerInfoUpdated:` callback
 * is triggered to report the latest player and media stream information.
 *
 * @param mute Whether to receive the audio stream:
 * - `YES`: Do not receive the audio stream.
 * - `NO`: Receive the audio stream.
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the subscribe or unsubscribe operation is successful:
 * - `YES`: The operation succeeds.
 * - `NO`: The operation fails.
 */
- (BOOL)muteAudio:(BOOL)mute error:(AgoraRteError * _Nullable)error;

/**
 * @brief Starts or stops receiving remote video streams.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method after `openWithUrl:startTime:cb:`.
 * Related callbacks: After this method is successfully called, the `onPlayerInfoUpdated:` callback
 * is triggered to report the latest player and media stream information.
 *
 * @param mute Whether to receive the remote video streams:
 * - `YES`: Do not receive remote video streams.
 * - `NO`: Receive remote video streams.
 * @param error Status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the subscribe or unsubscribe operation succeeds:
 * - `YES`: Operation succeeds.
 * - `NO`: Operation fails.
 */
- (BOOL)muteVideo:(BOOL)mute error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the current playback position.
 *
 * @since v4.5.1
 *
 * @details
 * This method retrieves the current playback time position in milliseconds.
 *
 * @param error Output parameter that indicates error information. Possible return values include:
 * - `AgoraRteOk`: The call succeeds.
 * - `AgoraRteErrorInvalidOperation`:
 *   - The corresponding internal `AgoraRtePlayer` object has been destroyed or is invalid.
 *   - The opened URL is an RTE URL, and calling `getPosition` is not supported. See `AgoraRteError`
 * for details.
 *
 * @return
 * If the method call succeeds, returns the current playback position in milliseconds.
 */
- (uint64_t)getPosition:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets information related to the player playback and media stream.
 *
 * @since v4.4.0
 *
 * @details
 * You can use this method to get information related to the player and the media stream, such as
 * audio sample rate and video frame dimensions.
 * Call timing: You must call this method after `initWithRte:initialConfig:`.
 *
 * @param info Information related to the player and the media stream. See `AgoraRtePlayerInfo`.
 * @param error Status or error information. See `AgoraRteError`.
 *
 * @return
 * Whether the information is retrieved successfully:
 * - `YES`: The information is retrieved successfully.
 * - `NO`: Failed to retrieve the information.
 */
- (BOOL)getInfo:(AgoraRtePlayerInfo * _Nonnull)info error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the current media player options.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method after `initWithRte:initialConfig:`.
 *
 * @param config The object that contains the media player options. See `AgoraRtePlayerConfig`.
 * @param error The status or error information. See `AgoraRteError`.
 *
 * @return
 * Whether the options are retrieved successfully:
 * - `YES`: Retrieved successfully.
 * - `NO`: Failed to retrieve.
 */
- (BOOL)getConfigs:(AgoraRtePlayerConfig * _Nonnull)config error:(AgoraRteError * _Nullable)error;

/**
 * @brief Player configuration.
 *
 * @since v4.4.0
 *
 * @details
 * You can call this method to configure the player, such as enabling autoplay, subscribing to video
 * streams of different resolutions and bitrate.
 * Call timing: This method must be called after `initWithRte:initialConfig:`.
 *
 * @param config The player configuration object. See `AgoraRtePlayerConfig`.
 * @param error Status or error information. See `AgoraRteError`.
 *
 * @return
 * Whether the player configuration is successful:
 * - `YES`: Configuration succeeded.
 * - `NO`: Configuration failed.
 */
- (BOOL)setConfigs:(AgoraRtePlayerConfig * _Nonnull)config error:(AgoraRteError * _Nullable)error;

/**
 * @brief Registers a player observer object.
 *
 * @since v4.4.0
 *
 * @details
 * Before calling this method, you need to implement an interface class that inherits from
 * `AgoraRtePlayerObserver`.
 * Call timing: You must call this method after `initWithRte:initialConfig:`.
 *
 * @param observer The interface object instance. See `AgoraRtePlayerObserver` for details.
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the registration is successful:
 * - `YES`: Registration succeeded.
 * - `NO`: Registration failed.
 */
- (BOOL)registerObserver:(AgoraRtePlayerObserver *_Nonnull)observer error:(AgoraRteError * _Nullable)error;

/**
 * @brief Unregisters the media player event observer object.
 *
 * @since v4.4.0
 *
 * @details
 * After you call `registerObserver:error:` to register a media player event observer, call this
 * method if you need to unregister it.
 * Call timing: This method must be called after `registerObserver:error:`.
 *
 * @param observer The interface object instance. See `AgoraRtePlayerObserver`.
 * @param error The status or error information. See `AgoraRteError`.
 *
 * @return
 * Whether the unregistration is successful:
 * - `YES`: Unregistration succeeded.
 * - `NO`: Unregistration failed.
 */
- (BOOL)unregisterObserver:(AgoraRtePlayerObserver * _Nullable)observer error:(AgoraRteError * _Nullable)error;

@end
