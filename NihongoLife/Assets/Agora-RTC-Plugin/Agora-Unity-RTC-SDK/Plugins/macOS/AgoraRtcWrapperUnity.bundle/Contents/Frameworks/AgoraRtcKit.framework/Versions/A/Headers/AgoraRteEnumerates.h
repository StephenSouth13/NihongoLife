#import <Foundation/Foundation.h>

/**
 * @brief The state of the media player.
 *
 * @since v4.4.0
 */
typedef NS_ENUM(NSUInteger, AgoraRtePlayerState) {
  /**
   * 0: Idle link state.
   */
  AgoraRtePlayerStateIdle = 0,
  /**
   * 1: Opening the URL resource. This status code is reported after you call
   * `openWithUrl:startTime:cb:`.
   */
  AgoraRtePlayerStateOpening = 1,
  /**
   * 2: Successfully opens the URL resource. This status code is reported when the
   * `openWithUrl:startTime:cb:` method successfully opens the URL resource.
   */
  AgoraRtePlayerStateOpenCompleted = 2,
  /**
   * 3: Playing.
   */
  AgoraRtePlayerStatePlaying = 3,
  /**
   * 4: Pause playback. This status code is reported when you successfully call `pause:`.
   */
  AgoraRtePlayerStatePaused = 4,
  /**
   * 5: Playback completed.
   */
  AgoraRtePlayerStatePlaybackCompleted = 5,
  /**
   * 6: Playback stops. This status code is reported after you successfully call `pause:`.
   */
  AgoraRtePlayerStateStopped = 6,
  /**
   * 7: Failure state. This code is reported when an internal error occurs. If you receive this state,
   * you need to first call `pause:`, and then call `openWithUrl:startTime:cb:` to reopen the
   * resource.
   */
  AgoraRtePlayerStateFailed = 7
};

/**
 * @brief Media player event types.
 *
 * @since v4.4.0
 */
typedef NS_ENUM(NSUInteger, AgoraRtePlayerEvent) {
  /**
   * 0: Starts seeking to the specified position for playback.
   */
  AgoraRtePlayerEventSeekBegin = 0,
  /**
   * 1: Jump to the specified position completed.
   */
  AgoraRtePlayerEventSeekComplete = 1,
  /**
   * 2: An error occurs when seeking to the specified playback position.
   */
  AgoraRtePlayerEventSeekError = 2,
  /**
   * 3: The currently buffered data is insufficient to support playback.
   */
  AgoraRtePlayerEventBufferLow = 3,
  /**
   * 4: The currently buffered data is just enough to support playback.
   */
  AgoraRtePlayerEventBufferRecover = 4,
  /**
   * 5: Audio or video stutters.
   */
  AgoraRtePlayerEventFreezeStart = 5,
  /**
   * 6: Both audio and video stop freezing.
   */
  AgoraRtePlayerEventFreezeStop = 6,
  /**
   * 7: A single loop playback is completed.
   */
  AgoraRtePlayerEventOneLoopPlaybackCompleted = 7,
  /**
   * 8: Token is about to expire. After receiving this event, you need to generate a new Token and
   * renew the URL via `openWithUrl:startTime:cb:`.
   */
  AgoraRtePlayerEventAuthenticationWillExpire = 8,
  /**
   * 9: Due to network issues, falls back from receiving audio and video streams to receiving only the
   * remote audio stream.
   */
  AgoraRtePlayerEventAbrFallbackToAudioOnlyLayer = 9,
  /**
   * 10: After the network connection is restored, recovers from receiving only the remote audio
   * stream to receiving both audio and video streams.
   */
  AgoraRtePlayerEventAbrRecoverFromAudioOnlyLayer = 10,
  /**
   * 11: Starts switching to the new URL.
   */
  AgoraRtePlayerEventSwitchBegin = 11,
  /**
   * 12: Switch to the new URL is complete.
   */
  AgoraRtePlayerEventSwitchComplete = 12,
  /**
   * 13: An error occurs when switching to a new URL.
   */
  AgoraRtePlayerEventSwitchError = 13,
  /**
   * 14: The first video frame has been displayed.
   */
  AgoraRtePlayerEventFirstDisplayed = 14,
  /**
   * 15: The number of cached files reaches the limit.
   */
  AgoraRtePlayerEventReachCacheFileMaxCount = 15,
  /**
   * 16: The cache file size reaches the upper limit.
   */
  AgoraRtePlayerEventReachCacheFileMaxSize = 16,
  /**
   * 17: Start attempting to open a new URL.
   */
  AgoraRtePlayerEventTryOpenStart = 17,
  /**
   * 18: Successfully opens a new URL.
   */
  AgoraRtePlayerEventTryOpenSucceed = 18,
  /**
   * 19: Fails to open a new URL.
   */
  AgoraRtePlayerEventTryOpenFailed = 19,
  /**
   * 20: The current audio track changes.
   */
  AgoraRtePlayerEventAudioTrackChanged = 20
};

/**
 * @brief Video rendering mode.
 *
 * @since v4.4.0
 */
typedef NS_ENUM(NSUInteger, AgoraRteVideoRenderMode) {
  /**
   * 0: Scale the video dimensions proportionally. Prioritize filling the viewport. Any excess video
   * content resulting from a mismatch between the video dimensions and the display viewport will be
   * cropped.
   */
  AgoraRteVideoRenderModeHidden = 0,
  /**
   * 1: Scale the video dimensions proportionally, prioritizing full display of the video content. 
   * Any area of the display window not filled due to a mismatch between the video dimensions and the
   * display window size is filled with black.
   */
  AgoraRteVideoRenderModeFit = 1
};

/**
 * @brief Video mirroring mode.
 *
 * @since v4.4.0
 */
typedef NS_ENUM(NSUInteger, AgoraRteVideoMirrorMode) {
  /**
   * 0: The SDK determines the mirror mode.
   * - Local video view mirror mode: If you use the front-facing camera, the SDK enables the local
   * video view mirror mode by default; if you use the rear-facing camera, the SDK disables the local
   * video view mirror mode by default.
   * - Remote user view mirror mode: The mirror mode for remote users is disabled by default.
   */
  AgoraRteVideoMirrorModeAuto = 0,
  /**
   * 1: Enables mirror mode.
   */
  AgoraRteVideoMirrorModeEnabled = 1,
  /**
   * 2: Disable mirror mode.
   */
  AgoraRteVideoMirrorModeDisabled = 2
};

/**
 * @brief Type of metadata associated with the media stream.
 *
 * @since v4.4.0
 */
typedef NS_ENUM(NSUInteger, AgoraRtePlayerMetadataType) {
  /**
   * 0: SEI (Supplemental Enhancement Information) type.
   */
  AgoraRtePlayerMetadataTypeSei = 0,
};

/**
 * @brief RTE-related status codes and error codes.
 *
 * @since v4.4.0
 */
typedef NS_ENUM(NSUInteger, AgoraRteErrorCode) {
  /**
   * 0: Operation succeeds.
   */
  AgoraRteOk = 0,
  /**
   * 1: General error (the cause of the error is not clearly classified).
   */
  AgoraRteErrorDefault = 1,
  /**
   * 2: An invalid parameter is set in the method. For example, the parameter contains illegal
   * characters, or the passed-in object is null. Please reset the parameter.
   */
  AgoraRteErrorInvalidArgument = 2,
  /**
   * 3: The current state does not support this operation, for example, the object has been destroyed
   * or is invalid.
   */
  AgoraRteErrorInvalidOperation = 3,
  /**
   * 4: Network error. Please check your network.
   */
  AgoraRteErrorNetworkError = 4,
  /**
   * 5: Token authentication failed. Possible reasons include an invalid Token or an expired Token.
   * Please check your Token.
   */
  AgoraRteErrorAuthenticationFailed = 5,
  /**
   * 6: Media stream not found. For example, after joining the channel, no media stream is received
   * from the host for more than 10 seconds.
   */
  AgoraRteErrorStreamNotFound = 6,
};


/**
 * @brief The quality layer of the subscribed video stream.
 *
 * @since v4.4.0
 *
 * @note If you need to customize the video resolution of layers from
 * `AgoraRteAbrSubscriptionLayer1` to `AgoraRteAbrSubscriptionLayer6` and subscribe to them, please
 * contact technical support to enable the ABR feature.
 *
 */
typedef NS_ENUM(NSUInteger, AgoraRteAbrSubscriptionLayer) {
  /**
   * 0: The high-quality video stream. This quality level has the highest resolution.
   */
  AgoraRteAbrSubscriptionHigh = 0,
  /**
   * 1: (Default) The lowest-quality video stream. This quality level has the lowest resolution.
   */
  AgoraRteAbrSubscriptionLow = 1,
  /**
   * 2: Video quality level 1. The resolution of this quality level is only lower than
   * `AgoraRteAbrSubscriptionHigh`.
   */
  AgoraRteAbrSubscriptionLayer1 = 2,
  /**
   * 3: Video quality level 2. The resolution of this quality level is lower than that of
   * `AgoraRteAbrSubscriptionLayer1`.
   */
  AgoraRteAbrSubscriptionLayer2 = 3,
  /**
   * 4: Video quality level 3. The resolution of this quality level is lower than that of
   * `AgoraRteAbrSubscriptionLayer2`.
   */
  AgoraRteAbrSubscriptionLayer3 = 4,
  /**
   * 5: Video quality level 4. The resolution of this quality level is lower than that of
   * `AgoraRteAbrSubscriptionLayer3`.
   */
  AgoraRteAbrSubscriptionLayer4 = 5,
  /**
   * 6: Video quality level 5. The resolution of this quality level is lower than that of
   * `AgoraRteAbrSubscriptionLayer4`.
   */
  AgoraRteAbrSubscriptionLayer5 = 6,
  /**
   * 7: Video quality level 6. The resolution of this quality level is lower than
   * `AgoraRteAbrSubscriptionLayer5`.
   */
  AgoraRteAbrSubscriptionLayer6 = 7,
};


/**
 * @brief The quality layers for video stream fallback.
 *
 * @since v4.4.0
 *
 * @note
 * - To use `AgoraRteAbrFallbackLayer1` through `AgoraRteAbrFallbackLayer6`, you need to `contact
 * technical support` to enable the ABR feature. Once enabled, you can customize the resolution for
 * each layer and choose any of them as the **lowest resolution** for video stream fallback. When
 * the network condition is poor, the SDK uses this as the lower limit and dynamically adjusts the
 * resolution within this range based on network conditions.
 * - When customizing resolutions, make sure to sort the video quality layers (Layer) in descending
 * order of resolution. If the resolutions are the same, sort them in descending order of frame
 * rate.
 *
 */
typedef NS_ENUM(NSUInteger, AgoraRteAbrFallbackLayer) {
  /**
   * 0: Does not perform fallback processing on the audio and video streams, but cannot guarantee the
   * quality of the audio and video streams.
   */
  AgoraRteAbrFallbackDisabled = 0,
  /**
   * 1: (Default) Falls back to the low-quality video stream. This layer has the lowest resolution.
   */
  AgoraRteAbrFallbackLow = 1,
  /**
   * 2: First attempts to receive only the low-quality video stream; if the video cannot be displayed
   * due to poor network conditions, then falls back to receiving only the subscribed remote audio
   * stream.
   */
  AgoraRteAbrFallbackAudioOnly = 2,
  /**
   * 3: Falls back to video quality level 1. The resolution and bitrate of this level are only lower
   * than those of the highest-quality multi-layer video stream you subscribed to.
   */
  AgoraRteAbrFallbackLayer1 = 3,
  /**
   * 4: Falls back to video quality level 2. The resolution of this level is only lower than level 1.
   */
  AgoraRteAbrFallbackLayer2 = 4,
  /**
   * 5: Falls back to video quality level 3. The resolution of this level is only lower than that of
   * level 2.
   */
  AgoraRteAbrFallbackLayer3 = 5,
  /**
   * 6: Falls back to video quality level 4. The resolution at this level is only lower than that of
   * level 3.
   */
  AgoraRteAbrFallbackLayer4 = 6,
  /**
   * 7: Falls back to video quality level 5. The resolution of this level is only lower than that of
   * level 4.
   */
  AgoraRteAbrFallbackLayer5 = 7,
  /**
   * 8: Falls back to video quality level 6. The resolution of this level is only lower than level 5.
   */
  AgoraRteAbrFallbackLayer6 = 8,
};
