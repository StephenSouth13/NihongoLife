//
//  AgoraObjects.h
//  AgoraRtcEngineKit
//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <CoreMedia/CoreMedia.h>
#import <Foundation/Foundation.h>
#import "AgoraEnumerates.h"

#if TARGET_OS_IPHONE
#import <UIKit/UIKit.h>
typedef UIView VIEW_CLASS;
typedef UIColor COLOR_CLASS;
typedef UIImage IMAGE_CLASS;
#elif TARGET_OS_MAC
#import <AppKit/AppKit.h>
typedef NSView VIEW_CLASS;
typedef NSColor COLOR_CLASS;
typedef NSImage IMAGE_CLASS;
#endif

@protocol AgoraRtcMediaPlayerProtocol;
@protocol AgoraMediaFilterEventDelegate;

/**
 * @brief Occurs when the SDK reads the media resource data.
 *
 * @details
 * When you call the `openWithMediaSource:` method to open a media resource, the SDK triggers this
 * callback and request you to pass in the buffer of the media resource data.
 *
 * @param playerKit Media player protocol. See `AgoraRtcMediaPlayerProtocol`.
 * @param buffer An input parameter. Data buffer (bytes). Write the `bufferSize` data reported by
 * the SDK into this parameter.
 * @param bufferSize The length of the data buffer (bytes).
 *
 * @return
 * - If the data is read successfully, pass in the length of the data (bytes) you actually read in
 * the return value.
 * - If reading the data fails, pass in 0 in the return value.
 */
typedef int(^AgoraRtcMediaPlayerCustomSourceOnReadCallback)(id<AgoraRtcMediaPlayerProtocol> _Nonnull playerKit, unsigned char * _Nullable buffer, int bufferSize);

/**
 * @brief Occurs when the SDK seeks the media resource data.
 *
 * @details
 * When you call the `openWithMediaSource:` or `open:startPos:` method to open a custom media
 * resource, the SDK triggers this callback to request the specified location in the media resource.
 *
 * @param playerKit Media player protocol. See `AgoraRtcMediaPlayerProtocol`.
 * @param offset An input parameter. The offset of the target position relative to the starting
 * point, in bytes. The value can be positive or negative.
 * @param whence An input parameter. The starting point. You can set it as one of the following
 * values:
 * - 0: The starting point is the head of the data, and the actual data offset after seeking is
 * `offset`.
 * - 1: The starting point is the current position, and the actual data offset after seeking is the
 * current position plus `offset`.
 * - 2: The starting point is the end of the data, and the actual data offset after seeking is the
 * whole data length plus `offset`.
 * - 65536: Do not perform position seeking, return the file size. Agora recommends that you use
 * this parameter value when playing pure audio files such as MP3 and WAV.
 *
 * @return
 * - When `whence` is `65536`, the media file size is returned.
 * - When `whence` is `0`, `1`, or `2`, the actual data offset after the seeking is returned.
 * - -1: Seeking failed.
 */
typedef long long(^AgoraRtcMediaPlayerCustomSourceOnSeekCallback)(id<AgoraRtcMediaPlayerProtocol> _Nonnull playerKit, long long offset, int whence);

/**
 * @brief Information related to the media file to be played and the playback scenario
 * configurations.
 */
__attribute__((visibility("default"))) @interface AgoraMediaSource : NSObject
/**
 * The URL of the media file to be played.
 * @note If you open a common media resource, pass in the value to `url`. If you open a custom media
 * resource, pass in the value to `playerOnReadCallback` and `playerOnSeekCallback`. Agora
 * recommends that you do not pass in values to all these parameters in one call; otherwise, this
 * call may fail.
 */
@property(copy, nonatomic) NSString *_Nullable url;
/**
 * The URI (Uniform Resource Identifier) of the media file.
 */
@property(copy, nonatomic) NSString *_Nullable uri;
/**
 * The starting position (ms) for playback. The default value is 0.
 */
@property(assign, nonatomic) NSUInteger startPos;
/**
 * Whether to enable autoplay once the media file is opened:
 * - `YES`: (Default) Yes.
 * - `NO`: No.
 * @note If autoplay is disabled, you need to call the `play` method to play a media file after it
 * is opened.
 */
@property(assign, nonatomic) BOOL autoPlay;
/**
 * Whether to cache the media file when it is being played:
 * - `YES`: Enables caching.
 * - `NO`: (Default) Disables caching.
 * @note
 * - Agora only supports caching on-demand audio and video streams that are not transmitted in HLS
 * protocol.
 * - If you need to enable caching, pass in a value to `uri`; otherwise, caching is based on the
 * `url` of the media file.
 * - If you enable this function, the Media Player caches part of the media file being played on
 * your local device, and you can play the cached media file without internet connection. The
 * statistics about the media file being cached are updated every second after the media file is
 * played. See `AgoraMediaPlayerCacheStatistics`.
 */
@property(assign, nonatomic) BOOL enableCache;
/**
 * Whether to allow the selection of different audio tracks when playing this media file:
 * - `YES`: Allow to select different audio tracks.
 * - `NO`: (Default) Do not allow to select different audio tracks.
 * If you need to set different audio tracks for local playback and publishing to the channel, you
 * need to set this parameter to `YES`, and then call the `selectMultiAudioTrack:publishTrackIndex:`
 * method to select the audio track.
 */
@property(assign, nonatomic) BOOL enableMultiAudioTrack;
/**
 * Whether the media resource to be opened is a live stream or on-demand video distributed through
 * Media Broadcast service:
 * - `YES`: The media resource to be played is a live or on-demand video distributed through Media
 * Broadcast service.
 * - `NO`: (Default) The media resource is not a live stream or on-demand video distributed through
 * Media Broadcast service.
 * @note If you need to open a live stream or on-demand video distributed through Broadcast
 * Streaming service, pass in the URL of the media resource to `url`, and set `isAgoraSource` as
 * `YES`; otherwise, you don't need to set the `isAgoraSource` parameter.
 */
@property(assign, nonatomic) BOOL isAgoraSource;
/**
 * Whether the media resource to be opened is a live stream:
 * - `YES`: The media resource is a live stream.
 * - `NO`: (Default) The media resource is not a live stream.
 * If the media resource you want to open is a live stream, Agora recommends that you set this
 * parameter as `YES` so that the live stream can be loaded more quickly.
 * @note If the media resource you open is not a live stream, but you set `isLiveSource` as `YES`,
 * the media resource is not to be loaded more quickly.
 */
@property(assign, nonatomic) BOOL isLiveSource;
/**
 * Occurs when the SDK reads the media resource data. See
 * `AgoraRtcMediaPlayerCustomSourceOnReadCallback`.
 * @note If you open a common media resource, pass in the value to `url`. If you open a custom media
 * resource, pass in the value to `playerOnReadCallback` and `playerOnSeekCallback`. Agora
 * recommends that you do not pass in values to all these parameters in one call; otherwise, this
 * call may fail.
 */
@property(copy, nonatomic) AgoraRtcMediaPlayerCustomSourceOnReadCallback _Nonnull playerOnReadCallback;
/**
 * Occurs when the SDK seeks the media resource data. See
 * `AgoraRtcMediaPlayerCustomSourceOnSeekCallback`.
 * @note If you open a common media resource, pass in the value to `url`. If you open a custom media
 * resource, pass in the value to `playerOnReadCallback` and `playerOnSeekCallback`. Agora
 * recommends that you do not pass in values to all these parameters in one call; otherwise, this
 * call may fail.
 */
@property(copy, nonatomic) AgoraRtcMediaPlayerCustomSourceOnSeekCallback _Nonnull playerOnSeekCallback;

@end

/**
 * @brief Statistics about the media files being cached.
 */
__attribute__((visibility("default"))) @interface AgoraMediaPlayerCacheStatistics : NSObject

/**
 * The size (bytes) of the media file being played.
 */
@property(assign, nonatomic) NSInteger fileSize;
/**
 * The size (bytes) of the media file that you want to cache.
 */
@property(assign, nonatomic) NSInteger cacheSize;
/**
 * The size (bytes) of the media file that has been downloaded.
 */
@property(assign, nonatomic) NSInteger downloadSize;

@end

/**
 * @brief The information of the media file being played.
 */
__attribute__((visibility("default"))) @interface AgoraMediaPlayerPlaybackStats : NSObject
/**
 * The frame rate (fps) of the video.
 */
@property(assign, nonatomic) NSInteger videoFps;
/**
 * The bitrate (kbps) of the video.
 */
@property(assign, nonatomic) NSInteger videoBitrateInKbps;
/**
 * The bitrate (kbps) of the audio.
 */
@property(assign, nonatomic) NSInteger audioBitrateInKbps;
/**
 * The total bitrate (kbps) of the media stream.
 */
@property(assign, nonatomic) NSInteger totalBitrateInKbps;
@end

/**
 * @brief Information related to the media player.
 */
__attribute__((visibility("default"))) @interface AgoraMediaPlayerUpdatedInfo : NSObject
/**
 * @technical preview
 */
@property(copy, nonatomic) NSString *_Nullable internalPlayerUuid;
/**
 * The ID of a deivce.
 */
@property(copy, nonatomic) NSString *_Nullable deviceId;
/**
 * Height (pixel) of the video.
 */
@property(assign, nonatomic) NSInteger videoHeight;
/**
 * Width (pixel) of the video.
 */
@property(assign, nonatomic) NSInteger videoWidth;
/**
 * Audio sample rate (Hz).
 */
@property(assign, nonatomic) NSInteger audioSampleRate;
/**
 * The number of audio channels.
 */
@property(assign, nonatomic) NSInteger audioChannels;
/**
 * The number of bits per audio sample point.
 */
@property(assign, nonatomic) NSInteger audioBitsPerSample;
@end

/**
 * @brief Information about the video bitrate of the media resource being played.
 */
__attribute__((visibility("default"))) @interface AgoraMediaPlayerSrcInfo : NSObject
/**
 * The video bitrate (Kbps) of the media resource being played.
 */
@property(assign, nonatomic) int bitrateInKbps;
/**
 * The name of the media resource.
 */
@property(copy, nonatomic) NSString *_Nullable name;

@end


/**
 * @brief The statistics of the current CDN streaming.
 *
 * @deprecated v4.6.0.
 */
__attribute__((visibility("default"))) @interface AgoraDirectCdnStreamingStats : NSObject
/**
 * The width (px) of the video frame.
 */
@property(assign, nonatomic) NSUInteger videoWidth;
/**
 * The height (px) of the video frame.
 */
@property(assign, nonatomic) NSUInteger videoHeight;
/**
 * The frame rate (fps) of the current video frame.
 */
@property(assign, nonatomic) NSUInteger fps;
/**
 * The bitrate (bps) of the current video frame.
 */
@property(assign, nonatomic) NSUInteger videoBitrate;
/**
 * The bitrate (bps) of the current audio frame.
 */
@property(assign, nonatomic) NSUInteger audioBitrate;

@end

/**
 * @brief The media setting options for the host.
 *
 * @deprecated v4.6.0.
 */
__attribute__((visibility("default"))) @interface AgoraDirectCdnStreamingMediaOptions : NSObject
/**
 * Sets whether to publish the video captured by the camera:
 * - `YES`: Publish the video captured by the camera.
 * - `NO`: (Default) Do not publish the video captured by the camera.
 */
@property(assign, nonatomic) BOOL publishCameraTrack;
/**
 * Sets whether to publish the audio captured by the microphone:
 * - `YES`: Publish the audio captured by the microphone.
 * - `NO`: (Default) Do not publish the audio captured by the microphone.
 */
@property(assign, nonatomic) BOOL publishMicrophoneTrack;
/**
 * Sets whether to publish the captured audio from a custom source:
 * - `YES`: Publish the captured audio from a custom source.
 * - `NO`: (Default) Do not publish the captured audio from the custom source.
 */
@property(assign, nonatomic) BOOL publishCustomAudioTrack;
/**
 * Sets whether to publish the captured video from a custom source:
 * - `YES`: Publish the captured video from a custom source.
 * - `NO`: (Default) Do not publish the captured video from the custom source.
 */
@property(assign, nonatomic) BOOL publishCustomVideoTrack;
/**
 * Determines whether to publish the audio track of media player source.
 * - `YES`: Publish the audio track of media player source.
 * - `NO`: (Default) Do not publish the audio track of media player source.
*/
@property(assign, nonatomic) BOOL publishMediaPlayerAudioTrack;
/**
 * Determines which media player source should be published.
 * This parameter get from function getMediaPlayerId() of AgoraMediaPlayer.
*/
@property(assign, nonatomic) NSInteger publishMediaPlayerId;

/**
 * The video track ID returned by calling the `createCustomVideoTrack` method. The default value is
 * 0.
 */
@property(assign, nonatomic) NSInteger customVideoTrackId;

@end

/**
 * The video encoded track options.
 */
__attribute__((visibility("default"))) @interface AgoraEncodedVideoTrackOptions : NSObject
/**
 * Whether to enable CC mode.
 * - TCC_ENABLED = 0: (Default) enable cc.
 * - TCC_DISABLED = 1: disable cc.
 */
@property(assign, nonatomic) int ccMode;
/**
 * The codec type used for the encoded images.
 * - VIDEO_CODEC_VP8 = 1: VP8.
 * - VIDEO_CODEC_H264 = 2: H.264.
 * - VIDEO_CODEC_H265 = 3: (Default) H.265.
 * - VIDEO_CODEC_VP9 = 13: VP9.
 * - VIDEO_CODEC_GENERIC = 6: GENERIC.
 * - VIDEO_CODEC_GENERIC_JPEG = 20: GENERIC_JPEG.
 */
@property(assign, nonatomic) int codecType;
/**
 * Target bitrate (Kbps) for sending encoded video frame.
 */
@property(assign, nonatomic) int targetBitrate;
@end

/**
 * @brief The channel media options.
 *
 * @details
 * Agora supports publishing multiple audio streams and one video stream at the same time and in the
 * same `AgoraRtcConnection`. For example, `publishMicrophoneTrack`, `publishCustomAudioTrack`, and
 * `publishMediaPlayerAudioTrack` can be set as `YES` at the same time, but only one of
 * `publishCameraTrack`, `publishScreenCaptureVideo` (iOS)/`publishScreenTrack` (macOS),
 * `publishCustomVideoTrack`, or
 * `publishEncodedVideoTrack` can be set as `YES`.
 *
 * @note Agora recommends that you set member parameter values yourself according to your business
 * scenario, otherwise the SDK will automatically assign values to member parameters.
 *
 */
__attribute__((visibility("default"))) @interface AgoraRtcChannelMediaOptions : NSObject
/**
 * Whether to publish the video captured by the camera:
 * - `YES`: Publish the video captured by the camera.
 * - `NO`: Do not publish the video captured by the camera.
 */
@property(assign, nonatomic) BOOL publishCameraTrack;
/**
 * Whether to publish the video captured by the second camera:
 * - `YES`: Publish the video captured by the second camera.
 * - `NO`: Do not publish the video captured by the second camera.
 */
@property(assign, nonatomic) BOOL publishSecondaryCameraTrack;
/**
 * Whether to publish the audio captured by the microphone:
 * - `YES`: Publish the audio captured by the microphone.
 * - `NO`: Do not publish the audio captured by the microphone.
 */
@property(assign, nonatomic) BOOL publishMicrophoneTrack;

/**
 * Determines whether to publish the audio of the screen capturer.
 * - `YES`: Publish the audio track of the screen capturer.
 * - `NO`: (Default) Do not publish the audio track of the screen capturer.
 */
@property(assign, nonatomic) BOOL publishScreenCaptureAudio;
#if TARGET_OS_IPHONE
/**
 * (iOS only) Whether to publish the video captured from the screen:
 * - `YES`: Publish the video captured from the screen.
 * - `NO`: Do not publish the video captured from the screen.
 * @note As of v4.0.0, the parameter name is changed from `publishScreenTrack` to
 * `publishScreenCaptureVideo`.
 */
@property(assign, nonatomic) BOOL publishScreenCaptureVideo;

#elif TARGET_OS_MAC
/**
 * (macOS only) Whether to publish the video captured by the third camera:
 * - `YES`: Publish the video captured by the third camera.
 * - `NO`: Do not publish the video captured by the third camera.
 */
@property(assign, nonatomic) BOOL publishThirdCameraTrack;
/**
 * (macOS only) Whether to publish the video captured by the fourth camera:
 * - `YES`: Publish the video captured by the fourth camera.
 * - `NO`: Do not publish the video captured by the fourth camera.
 */
@property(assign, nonatomic) BOOL publishFourthCameraTrack;
/**
 * (macOS only) Whether to publish the video captured from the screen:
 * - `YES`: Publish the video captured from the screen.
 * - `NO`: Do not publish the video captured from the screen.
 */
@property(assign, nonatomic) BOOL publishScreenTrack;
/**
 * (macOS only) Whether to publish the video captured from the second screen:
 * - `YES`: Publish the video captured from the second screen.
 * - `NO`: Do not publish the video captured from the second screen.
 */
@property(assign, nonatomic) BOOL publishSecondaryScreenTrack;
/**
 * (macOS only) Whether to publish the video captured from the third screen:
 * - `YES`: Publish the captured video from the third screen.
 * - `NO`: Do not publish the video captured from the third screen.
 */
@property(assign, nonatomic) BOOL publishThirdScreenTrack;
/**
 * (macOS only) Whether to publish the video captured from the fourth screen:
 * - `YES`: Publish the captured video from the fourth screen.
 * - `NO`: Do not publish the video captured from the fourth screen.
 */
@property(assign, nonatomic) BOOL publishFourthScreenTrack;
#endif
/**
 * Whether to publish the audio captured from a custom source:
 * - `YES`: Publish the audio captured from the custom source.
 * - `NO`: Do not publish the captured audio from a custom source.
 */
@property(assign, nonatomic) BOOL publishCustomAudioTrack;
/**
 * The ID of the custom audio track to be published. The default value is 0. You can obtain the
 * custom audio track ID through the `createCustomAudioTrack:config:` method.
 */
@property(assign, nonatomic) NSInteger publishCustomAudioTrackId;
/**
 * Whether to publish the video captured from a custom source:
 * - `YES`: Publish the video captured from the custom source.
 * - `NO`: Do not publish the captured video from a custom source.
 */
@property(assign, nonatomic) BOOL publishCustomVideoTrack;
/**
 * Whether to publish the encoded video:
 * - `YES`: Publish the encoded video.
 * - `NO`: Do not publish the encoded video.
 */
@property(assign, nonatomic) BOOL publishEncodedVideoTrack;
/**
 * Whether to publish the audio from the media player:
 * - `YES`: Publish the audio from the media player.
 * - `NO`: Do not publish the audio from the media player.
 */
@property(assign, nonatomic) BOOL publishMediaPlayerAudioTrack;
/**
 * Whether to publish the video from the media player:
 * - `YES`: Publish the video from the media player.
 * - `NO`: Do not publish the video from the media player.
 */
@property(assign, nonatomic) BOOL publishMediaPlayerVideoTrack;
/**
* Determines whether to publish the local transcoded video track.
* - `YES`: Publish the video track of local transcoded video track.
* - `NO`: (Default) Do not publish the local transcoded video track.
*/
@property(assign, nonatomic) BOOL publishTranscodedVideoTrack;
/**
 * Whether to publish the mixed audio track:
 * - `YES`: Publish the mixed audio track.
 * - `NO`: Do not publish the mixed audio track.
 */
@property(assign, nonatomic) BOOL publishMixedAudioTrack;
/**
 * Whether to publish the video track processed by the speech driven extension:
 * - `YES`: Publish the video track processed by the speech driven extension.
 * - `NO`: (Default) Do not publish the video track processed by the speech driven extension.
 */
@property(assign, nonatomic) BOOL publishLipSyncTrack;
/**
 * Whether to automatically subscribe to all remote audio streams when the user joins a channel:
 * - `YES`: Subscribe to all remote audio streams.
 * - `NO`: Do not automatically subscribe to any remote audio streams.
 */
@property(assign, nonatomic) BOOL autoSubscribeAudio;
/**
 * Whether to automatically subscribe to all remote video streams when the user joins the channel:
 * - `YES`: Subscribe to all remote video streams.
 * - `NO`: Do not automatically subscribe to any remote video streams.
 */
@property(assign, nonatomic) BOOL autoSubscribeVideo;
/**
 * Whether to enable audio capturing or playback:
 * - `YES`: Enable audio capturing or playback.
 * - `NO`: Do not enable audio capturing or playback.
 * @note If you need to publish the audio streams captured by your microphone, ensure this parameter
 * is set as `YES`.
 */
@property(assign, nonatomic) BOOL enableAudioRecordingOrPlayout;
/**
 * The ID of the media player to be published. The default value is 0.
 */
@property(assign, nonatomic) NSInteger publishMediaPlayerId;
/**
 * The user role. See `AgoraClientRole`.
 * @note If you set the user role as an audience member, you cannot publish audio and video streams
 * in the channel. If you want to publish media streams in a channel during live streaming, ensure
 * you set the user role as broadcaster.
 */
@property(assign, nonatomic) AgoraClientRole clientRoleType;
/**
 * The latency level of an audience member in interactive live streaming. See
 * `AgoraAudienceLatencyLevelType`.
 */
@property(assign, nonatomic) AgoraAudienceLatencyLevelType audienceLatencyLevel;
/**
 * The default video-stream type. See `AgoraVideoStreamType`.
 */
@property(assign, nonatomic) AgoraVideoStreamType defaultVideoStreamType;
/**
 * The channel profile. See `AgoraChannelProfile`.
 */
@property(assign, nonatomic) AgoraChannelProfile channelProfile;
/**
 * Delay (in milliseconds) for sending audio frames. You can use this parameter to set the delay of
 * the audio frames that need to be sent, to ensure audio and video synchronization.
 * To switch off the delay, set the value to 0.
 */
@property(assign, nonatomic) NSInteger audioDelayMs;
/**
 * The delay in ms for sending media player audio frames. This is used for explicit control of A/V sync.
 * To switch off the delay, set the value to zero.
 */
@property(assign, nonatomic) NSInteger mediaPlayerAudioDelayMs;
/**
 * (Optional) The token generated on your server for authentication.
 * @note
 * - This parameter takes effect only when calling `updateChannelWithMediaOptions:` or
 * `updateChannelExWithMediaOptions:connection:`.
 * - Ensure that the App ID, channel name, and user name used for creating the token are the same as
 * those used by the `sharedEngineWithConfig:delegate:` method for initializing the RTC engine, and
 * those used by the `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` and
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:` methods for joining the
 * channel.
 */
@property(copy, nonatomic) NSString * _Nullable token;
/**
 * Enable media packet encryption.
 * It will be ignored when calling function updateChannelMediaOptions().
 * - `YES`:  Enable media packet encryption.
 * - `NO`: (Default) Do not Enable media packet encryption.
 */
@property(assign, nonatomic) BOOL enableBuiltInMediaEncryption;

/**
 * Whether to publish the sound of a metronome to remote users:
 * - `YES`: Publish processed audio frames. Both the local user and remote users can hear the
 * metronome.
 * - `NO`: Do not publish the sound of the metronome. Only the local user can hear the metronome.
 */
@property(assign, nonatomic) BOOL publishRhythmPlayerTrack;
/**
 * Whether to enable interactive mode:
 * - `YES`: Enable interactive mode. Once this mode is enabled and the user role is set as audience,
 * the user can receive remote video streams with low latency.
 * - `NO`:Do not enable interactive mode. If this mode is disabled, the user receives the remote
 * video streams in default settings.
 * @note
 * - This parameter only applies to co-streaming scenarios. The cohosts need to call the
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:` method to join the other
 * host's channel as an audience member, and set `isInteractiveAudience` to `YES`.
 * - This parameter takes effect only when the user role is `AgoraClientRoleAudience`.
 */
@property(assign, nonatomic) BOOL isInteractiveAudience;

/**
 * The video track ID returned by calling the `createCustomVideoTrack` method. The default value is
 * 0.
 */
@property(assign, nonatomic) NSInteger customVideoTrackId;

/**
 * Whether the audio stream being published is filtered according to the volume algorithm:
 * - `YES`: The audio stream is filtered. If the audio stream filter is not enabled, this setting
 * does not takes effect.
 * - `NO`: The audio stream is not filtered.
 * @note If you need to enable this function, contact `support@agora.io`.
 */
@property(assign, nonatomic) BOOL isAudioFilterable;

/** Provides the technical preview functionalities or special customizations by configuring the SDK with JSON options.
 *  @technical preview
 */
@property(copy, nonatomic) NSString * _Nullable parameters;

/**
 * Whether to enable multiple transmisssion paths:
 * - `YES`: Enable multiple transmisssion path.
 * - `NO`: Dsiable multiple transmisssion path.
 * @note Permissions and system requirements:
 * - iOS: iOS 12.0 or later.
 * - macOS: 10.14 or later.
 * @since 4.6.0
 */
@property(assign, nonatomic) BOOL enableMultipath;

/**
 * Uplink transmission mode. See `AgoraMultipathMode`.
 * @note When using this parameter, make sure that `enableMultipath` is set to `YES`.
 * @since 4.6.0
 */
@property(assign, nonatomic) AgoraMultipathMode uplinkMultipathMode;

/**
 * Downlink transmission mode. See `AgoraMultipathMode`.
 * @note When using this parameter, make sure that `enableMultipath` is set to `YES`.
 * @since 4.6.0
 */
@property(assign, nonatomic) AgoraMultipathMode downlinkMultipathMode;

/**
 * Preferred type of transmission path. See `AgoraMultipathType`.
 * @note When using this parameter, make sure that `enableMultipath` is set to `YES`.
 * @since 4.6.0
 */
@property(assign, nonatomic) AgoraMultipathType preferMultipathType;

@end

/**
 * @brief Attributes of the video canvas object.
 */
__attribute__((visibility("default"))) @interface AgoraRtcVideoCanvas : NSObject
/**
 * User ID that publishes the video source.
 */
@property(assign, nonatomic) NSUInteger uid;

/**
 * The video display window.
 * @note In one `AgoraRtcVideoCanvas`, you can only choose to set either `view` or `surfaceTexture`.
 * If both are set, only the settings in `view` take effect.
 */
@property(assign, nonatomic) NSUInteger subviewUid;

/**
 * The video display window.
 * In one `AgoraRtcVideoCanvas`, you can only choose to set either `view` or `surfaceTexture`.
 * If both are set, only the settings in `view` take effect.
 */
@property(strong, nonatomic) VIEW_CLASS *_Nullable view;
/**
 * The rendering mode of the video. See `AgoraVideoRenderMode`.
 */
@property(assign, nonatomic) AgoraVideoRenderMode renderMode;
/**
 * The mirror mode of the view. See `AgoraVideoMirrorMode`.
 * @note
 * - For the mirror mode of the local video view: If you use a front camera, the SDK enables the
 * mirror mode by default; if you use a rear camera, the SDK disables the mirror mode by default.
 * - For the remote user: The mirror mode is disabled by default.
 */
@property(assign, nonatomic) AgoraVideoMirrorMode mirrorMode;
/**
 * Setting mode of the view. See `AgoraVideoViewSetupMode`.
 */
@property(assign, nonatomic) AgoraVideoViewSetupMode setupMode;
/**
 * The type of the video source. See `AgoraVideoSourceType`.
 */
@property(nonatomic, assign) AgoraVideoSourceType sourceType;
/**
 * The ID of the media player. You can get the Device ID by calling `getMediaPlayerId`.
 */
@property(nonatomic, assign) int mediaPlayerId;
/**
 * (Optional) Display area of the video frame, see `CGRect`. `width` and `height` represent the
 * video pixel width and height of the area. The default value is null (width or height is 0), which
 * means that the actual resolution of the video frame is displayed.
 */
@property(assign, nonatomic) CGRect cropArea;

/**
 * The background color of the video canvas in RGBA format. The default value is 0x00000000, which
 * represents black.
 */
@property(assign, nonatomic) UInt32 backgroundColor;

/**
 * (Optional) Whether to enable alpha mask rendering:
 * - `YES`: Enable alpha mask rendering.
 * - `NO`: (Default) Disable alpha mask rendering.
 * Alpha mask rendering can create images with transparent effects and extract portraits from
 * videos. When used in combination with other methods, you can implement effects such as
 * portrait-in-picture and watermarking.
 * @note
 * - The receiver can render alpha channel information only when the sender enables alpha
 * transmission.
 * - To enable alpha transmission, `technical support`.
 */
@property(assign, nonatomic) BOOL enableAlphaMask;

/**
 * The observation position of the video frame in the video link. See `AgoraVideoModulePosition`.
 */
@property(assign, nonatomic) AgoraVideoModulePosition position;
@end

/**
 * @brief Configurations of the last-mile network test.
 */
__attribute__((visibility("default"))) @interface AgoraLastmileProbeConfig : NSObject
/**
 * Sets whether to test the uplink network. Some users, for example, the audience members in a
 * LIVE_BROADCASTING channel, do not need such a test.
 * - `YES`: Test the uplink network.
 * - `NO`: Do not test the uplink network.
 */
@property (assign, nonatomic) BOOL probeUplink;
/**
 * Sets whether to test the downlink network:
 * - `YES`: Test the downlink network.
 * - `NO`: Do not test the downlink network.
 */
@property (assign, nonatomic) BOOL probeDownlink;
/**
 * The expected maximum uplink bitrate (bps) of the local user. The value range is [100000,
 * 5000000]. Agora recommends referring to `setVideoEncoderConfiguration:` to set the value.
 */
@property (assign, nonatomic) NSUInteger expectedUplinkBitrate;
/**
 * The expected maximum downlink bitrate (bps) of the local user. The value range is
 * [100000,5000000].
 */
@property (assign, nonatomic) NSUInteger expectedDownlinkBitrate;
@end

/**
 * @brief Results of the uplink or downlink last-mile network test.
 */
__attribute__((visibility("default"))) @interface AgoraLastmileProbeOneWayResult : NSObject
/**
 * The packet loss rate (%).
 */
@property (assign, nonatomic) NSUInteger packetLossRate;
/**
 * The network jitter (ms).
 */
@property (assign, nonatomic) NSUInteger jitter;
/**
 * The estimated available bandwidth (bps).
 */
@property (assign, nonatomic) NSUInteger availableBandwidth;
@end

/**
 * @brief Results of the uplink and downlink last-mile network tests.
 */
__attribute__((visibility("default"))) @interface AgoraLastmileProbeResult : NSObject
/**
 * The status of the last-mile network tests. See `AgoraLastmileProbeResultState`.
 */
@property (assign, nonatomic) AgoraLastmileProbeResultState state;
/**
 * The round-trip time (ms).
 */
@property (assign, nonatomic) NSUInteger rtt;
/**
 * Results of the uplink last-mile network test. See `AgoraLastmileProbeOneWayResult`.
 */
@property (strong, nonatomic) AgoraLastmileProbeOneWayResult *_Nonnull uplinkReport;
/**
 * Results of the downlink last-mile network test. See `AgoraLastmileProbeOneWayResult`.
 */
@property (strong, nonatomic) AgoraLastmileProbeOneWayResult *_Nonnull downlinkReport;
@end
/**
 * @brief The statistics of the local video stream.
 */
__attribute__((visibility("default"))) @interface AgoraRtcLocalVideoStats : NSObject
/**
 * The ID of the local user.
 */
@property(assign, nonatomic) NSUInteger uid;

/**
 * The actual bitrate (Kbps) while sending the local video stream.
 * @note This value does not include the bitrate for resending the video after packet loss.
 */
@property(assign, nonatomic) NSUInteger sentBitrate;
/**
 * The actual frame rate (fps) while sending the local video stream.
 * @note This value does not include the frame rate for resending the video after packet loss.
 */
@property(assign, nonatomic) NSUInteger sentFrameRate;
/**
 * The frame rate (fps) for capturing the local video stream.
 */
@property(assign, nonatomic) NSInteger captureFrameRate;
/**
 * The width (px) for capturing the local video stream.
 */
@property(assign, nonatomic) NSInteger captureFrameWidth;
/**
 * The height (px) for capturing the local video stream.
 */
@property(assign, nonatomic) NSInteger captureFrameHeight;
/**
 * The frame rate (fps) adjusted by the built-in video capture adapter (regulator) of the SDK for
 * capturing the local video stream. The regulator adjusts the frame rate of the video captured by
 * the camera according to the video encoding configuration.
 */
@property(assign, nonatomic) NSInteger regulatedCaptureFrameRate;
/**
 * The width (px) adjusted by the built-in video capture adapter (regulator) of the SDK for
 * capturing the local video stream. The regulator adjusts the height and width of the video
 * captured by the camera according to the video encoding configuration.
 */
@property(assign, nonatomic) NSInteger regulatedCaptureFrameWidth;
/**
 * The height (px) adjusted by the built-in video capture adapter (regulator) of the SDK for
 * capturing the local video stream. The regulator adjusts the height and width of the video
 * captured by the camera according to the video encoding configuration.
 */
@property(assign, nonatomic) NSInteger regulatedCaptureFrameHeight;

/**
 * The output frame rate (fps) of the local video encoder.
 */
@property(assign, nonatomic) NSInteger encoderOutputFrameRate;
/**
 * The output frame rate (fps) of the local video renderer.
 */
@property(assign, nonatomic) NSInteger rendererOutputFrameRate;
/**
 * The target frame rate (fps) of the current encoder.
 */
@property(assign, nonatomic) NSInteger targetFrameRate;
/**
 * The quality adaptation of the local video stream in the reported interval (based on the target
 * frame rate and target bitrate). See `AgoraVideoQualityAdaptIndication`.
 */
@property(assign, nonatomic) AgoraVideoQualityAdaptIndication qualityAdaptIndication;
/**
 * The target bitrate (Kbps) of the current encoder. This is an estimate made by the SDK based on
 * the current network conditions.
 */
@property(assign, nonatomic) NSInteger targetBitrate;
/**
 * The bitrate (Kbps) while encoding the local video stream.
 * @note This value does not include the bitrate for resending the video after packet loss.
 */
@property(assign, nonatomic) NSInteger encodedBitrate;
/**
 * The width of the encoded video (px).
 */
@property(assign, nonatomic) NSInteger encodedFrameWidth;
/**
 * The height of the encoded video (px).
 */
@property(assign, nonatomic) NSInteger encodedFrameHeight;
/**
 * The number of the sent video frames, represented by an aggregate value.
 */
@property(assign, nonatomic) NSInteger encodedFrameCount;
/**
 * The codec type of the local video. See `AgoraVideoCodecType`.
 */
@property(assign, nonatomic) AgoraVideoCodecType codecType;
/**
 * @technical preview
 * The encodedFrameDepth of the local video:
 * - AgoraVideoEncodedFrameOfSDR = 8
 * - AgoraVideoEncodedFrameOfHDR = 10
 */
@property(assign, nonatomic) AgoraVideoEncodedFrameDepth encodedFrameDepth;
/**
 * The local video encoding acceleration type. See `AgoraVideoHwEncoderAccelerating`.
 */
@property(assign, nonatomic) AgoraVideoHwEncoderAccelerating hwEncoderAccelerating;
/**
 * The video packet loss rate (%) from the local client to the Agora server before applying the
 * anti-packet loss strategies.
 */
@property(assign, nonatomic) NSInteger txPacketLossRate;
/**
 * The brightness level of the video image captured by the local camera. See
 * `AgoraCaptureBrightnessLevelType`.
 */
@property(assign, nonatomic) AgoraCaptureBrightnessLevelType captureBrightnessLevel NS_SWIFT_NAME(captureBrightnessLevel);
/**
 * Whether we send dual stream now.
 */
@property(assign, nonatomic) BOOL dualStreamEnabled;
@end

/**
 * @brief Statistics of the remote video stream.
 */
__attribute__((visibility("default"))) @interface AgoraRtcRemoteVideoStats : NSObject
/**
 * The user ID of the remote user sending the video stream.
 */
@property(assign, nonatomic) NSUInteger uid;

/**
 * Deprecated:
 * In scenarios where audio and video are synchronized, you can get the video delay data from
 * `networkTransportDelay` and `jitterBufferDelay` in `AgoraRtcRemoteAudioStats`.
 * The video delay (ms).
 */
@property(assign, nonatomic) NSUInteger delay __deprecated;
/**
 * End-to-end video latency (ms). That is, the time elapsed from the video capturing on the remote
 * user's end to the receiving and rendering of the video on the local user's end.
 */
@property(assign, nonatomic) NSUInteger e2eDelay;
/**
 * The width (pixels) of the video.
 */
@property(assign, nonatomic) NSUInteger width;
/**
 * The height (pixels) of the video.
 */
@property(assign, nonatomic) NSUInteger height;
/**
 * The bitrate (Kbps) of the remote video received since the last count.
 */
@property(assign, nonatomic) NSUInteger receivedBitrate;
/**
 * The type of the video stream. See `AgoraVideoStreamType`.
 */
@property(assign, nonatomic) AgoraVideoStreamType rxStreamType;

/**
 * The frame rate (fps) of decoding the remote video.
 */
@property(assign, nonatomic) NSInteger decoderOutputFrameRate;
/**
 * The frame rate (fps) of rendering the remote video.
 */
@property(assign, nonatomic) NSInteger rendererOutputFrameRate;
/**
 * The packet loss rate (%) of the remote video.
 */
@property(assign, nonatomic) NSInteger frameLossRate;
/**
 * The packet loss rate (%) of the remote video after using the anti-packet-loss technology.
 */
@property(assign, nonatomic) NSInteger packetLossRate;
/**
 * The total freeze time (ms) of the remote video stream after the remote user joins the channel. In
 * a video session where the frame rate is set to no less than 5 fps, video freeze occurs when the
 * time interval between two adjacent renderable video frames is more than 500 ms.
 */
@property(assign, nonatomic) NSInteger totalFrozenTime;
/**
 * The total active time (ms) of the video.
 * As long as the remote user or host neither stops sending the video stream nor disables the video
 * module after joining the channel, the video is available.
 */
@property(assign, nonatomic) NSUInteger totalActiveTime;
/**
 * The total duration (ms) of the remote video stream.
 */
@property(assign, nonatomic) NSInteger publishDuration;
/**
 * The total video freeze time as a percentage (%) of the total time the video is available. The
 * video is considered available as long as that the remote user neither stops sending the video
 * stream nor disables the video module after joining the channel.
 */
@property(assign, nonatomic) NSInteger frozenRate;
/**
 * The amount of time (ms) that the audio is ahead of the video.
 * @note If this value is negative, the audio is lagging behind the video.
 */
@property(assign, nonatomic) NSInteger avSyncTimeMs;
/**
 * The quality of the remote video stream in the reported interval.
 * The quality is determined by the Agora real-time video MOS (Mean Opinion Score) measurement method.
 * The return value range is [0, 500].
 * Dividing the return value by 100 gets the MOS score, which ranges from 0 to 5. The higher the score, the better the video quality.
 * @note For textured video data, this parameter always returns 0.
 */
@property(assign, nonatomic) NSInteger mosValue;
/**
 * Total number of video bytes received (bytes), represented by an aggregate value.
 */
@property(assign, nonatomic) NSUInteger rxVideoBytes;

@end

/**
 * @brief Local audio statistics.
 */
__attribute__((visibility("default"))) @interface AgoraRtcLocalAudioStats : NSObject
/**
 * The number of audio channels.
 */
@property(assign, nonatomic) NSUInteger numChannels;
/**
 * The sampling rate (Hz) of sending the local user's audio stream.
 */
@property(assign, nonatomic) NSUInteger sentSampleRate;
/**
 * The average bitrate (Kbps) of sending the local user's audio stream.
 */
@property(assign, nonatomic) NSUInteger sentBitrate;
/**
 * The internal payload codec.
 */
@property(assign, nonatomic) NSUInteger internalCodec;
/**
 * The packet loss rate (%) from the local client to the Agora server before applying the
 * anti-packet loss strategies.
 */
@property(assign, nonatomic) NSUInteger txPacketLossRate;
/**
 * The audio device module delay (ms) when playing or recording audio.
 */
@property(assign, nonatomic) NSUInteger audioDeviceDelay;
/**
 * The audio playout delay of the device
 */
@property(assign, nonatomic) NSUInteger audioPlayoutDelay;
/**
 * The ear monitor delay (ms), which is the delay from microphone input to headphone output.
 */
@property(assign, nonatomic) NSUInteger earMonitorDelay;
/**
 * Acoustic echo cancellation (AEC) module estimated delay (ms), which is the signal delay between
 * when audio is played locally before being locally captured.
 */
@property(assign, nonatomic) NSUInteger aecEstimatedDelay;

@end

/**
 * @brief Audio statistics of the remote user.
 */
__attribute__((visibility("default"))) @interface AgoraRtcRemoteAudioStats : NSObject
/**
 * The user ID of the remote user.
 */
@property(assign, nonatomic) NSUInteger uid;

/**
 * The quality of the audio stream sent by the user. See `AgoraNetworkQuality`.
 */
@property(assign, nonatomic) NSUInteger quality;
/**
 * The network delay (ms) from the sender to the receiver.
 */
@property(assign, nonatomic) NSUInteger networkTransportDelay;
/**
 * The network delay (ms) from the audio receiver to the jitter buffer.
 * @note When the receiving end is an audience member and `audienceLatencyLevel` of
 * `AgoraClientRoleOptions` is 1, this parameter does not take effect.
 */
@property(assign, nonatomic) NSUInteger jitterBufferDelay;
/**
 * The frame loss rate (%) of the remote audio stream in the reported interval.
 */
@property(assign, nonatomic) NSUInteger audioLossRate;
/**
 * The number of audio channels.
 */
@property(assign, nonatomic) NSUInteger numChannels;
/**
 * The sampling rate of the received audio stream in the reported interval.
 */
@property(assign, nonatomic) NSUInteger receivedSampleRate;
/**
 * The average bitrate (Kbps) of the received audio stream in the reported interval.
 */
@property(assign, nonatomic) NSUInteger receivedBitrate;
/**
 * The total freeze time (ms) of the remote audio stream after the remote user joins the channel. In
 * a session, audio freeze occurs when the audio frame loss rate reaches 4%.
 */
@property(assign, nonatomic) NSUInteger totalFrozenTime;
/**
 * The total active time (ms) between the start of the audio call and the callback of the remote
 * user.
 * The active time refers to the total duration of the remote user without the mute state.
 */
@property(assign, nonatomic) NSUInteger totalActiveTime;
/**
 * The total duration (ms) of the remote audio stream.
 */
@property(assign, nonatomic) NSInteger publishDuration;
/**
 * The total audio freeze time as a percentage (%) of the total time when the audio is available.
 * The audio is considered available when the remote user neither stops sending the audio stream nor
 * disables the audio module after joining the channel.
 */
@property(assign, nonatomic) NSUInteger frozenRate;
/**
 * The quality of the remote audio stream in the reported interval. The quality is determined by the
 * Agora real-time audio MOS (Mean Opinion Score) measurement method. The return value range is [0,
 * 500]. Dividing the return value by 100 gets the MOS score, which ranges from 0 to 5. The higher
 * the score, the better the audio quality.
 * The subjective perception of audio quality corresponding to the Agora real-time audio MOS scores
 * is as follows:
 * | MOS score      | Perception of audio quality                                                                                                                                  |
 * | -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
 * | Greater than 4 | Excellent. The audio sounds clear and smooth.                                                                                                                |
 * | From 3.5 to 4  | Good. The audio has some perceptible impairment but still sounds clear.                                                                                      |
 * | From 3 to 3.5  | Fair. The audio freezes occasionally and requires attentive listening.                                                                                       |
 * | From 2.5 to 3  | Poor. The audio sounds choppy and requires considerable effort to understand.                                                                                |
 * | From 2 to 2.5  | Bad. The audio has occasional noise. Consecutive audio dropouts occur, resulting in some information loss. The users can communicate only with difficulty.   |
 * | Less than 2    | Very bad. The audio has persistent noise. Consecutive audio dropouts are frequent, resulting in severe information loss. Communication is nearly impossible. |
 */
@property(assign, nonatomic) NSUInteger mosValue;
/**
 * If the packet loss concealment (PLC) occurs for N consecutive times, freeze is considered as PLC occurring for M consecutive times.
 * freeze cnt = (n_plc - n) / m
 */
@property (assign, nonatomic) NSUInteger frozenRateByCustomPlcCount;
/**
 * The number of audio packet loss concealment
 */
@property (assign, nonatomic) NSUInteger plcCount;

/**
 * @technical preview
 * The number of times the remote audio stream has experienced freezing.
*/
@property (assign, nonatomic) NSUInteger frozenCntByCustom;

/**
 * @technical preview
 * The total duration (ms) that the remote audio stream has been in a frozen state.
*/
@property (assign, nonatomic) NSUInteger frozenTimeByCustom;

/**
 * The Quality of Experience (QoE) of the local user when receiving a remote audio stream. See
 * `AgoraExperienceQuality`.
 */
@property(assign, nonatomic) AgoraExperienceQuality qoeQuality;
/**
 * Reasons why the QoE of the local user when receiving a remote audio stream is poor. See
 * `AgoraExperiencePoorReason`.
 */
@property(assign, nonatomic) AgoraExperiencePoorReason qualityChangedReason;
/**
 * Total number of audio bytes received (bytes) before network countermeasures, represented by
 * an aggregate value.
 */
@property(assign, nonatomic) NSUInteger rxAudioBytes;
/**
 * End-to-end audio delay (in milliseconds), which refers to the time from when the audio is
 * captured by the remote user to when it is played by the local user.
 */
@property(assign, nonatomic) NSInteger e2eDelay;
@end

/**
 * @brief The volume information of users.
 */
__attribute__((visibility("default"))) @interface AgoraRtcAudioVolumeInfo : NSObject
/**
 * The user ID.
 * - In the local user's callback, `uid` is 0.
 * - In the remote users' callback, `uid` is the user ID of a remote user whose instantaneous volume
 * is the highest.
 */
@property(assign, nonatomic) NSUInteger uid;

/**
 * The volume of the user. The value ranges between 0 (the lowest volume) and 255 (the highest
 * volume). If the local user enables audio capturing and calls `muteLocalAudioStream:` and set it
 * as `YES` to mute, the value of `volume` indicates the volume of locally captured audio signal. If
 * the user calls `startAudioMixing:loopback:cycle:startPos:`, the value of `volume` indicates the
 * volume after audio mixing.
 */
@property(assign, nonatomic) NSUInteger volume;

/**
 * Voice activity status of the local user.
 * - 0: The local user is not speaking.
 * - 1: The local user is speaking.
 * @note
 * - The `vad` parameter does not report the voice activity status of remote users. In a remote
 * user's callback, the value of `vad` is always 1.
 * - To use this parameter, you must set `reportVad` to `YES` when calling
 * `enableAudioVolumeIndication:smooth:reportVad:`.
 */
@property(assign, nonatomic) NSUInteger vad;

/**
 * The voice pitch of the local user. The value ranges between 0.0 and 4000.0.
 * @note The `voicePitch` parameter does not report the voice pitch of remote users. In the remote
 * users' callback, the value of `voicePitch` is always 0.0.
 */
@property (assign, nonatomic) double voicePitch;

@end

/**
 * @brief Statistics of a call session.
 */
__attribute__((visibility("default"))) @interface AgoraChannelStats : NSObject
/**
 * Call duration of the local user in seconds, represented by an aggregate value.
 */
@property(assign, nonatomic) NSUInteger duration;
/**
 * The number of bytes sent.
 */
@property(assign, nonatomic) NSUInteger txBytes;
/**
 * The number of bytes received.
 */
@property(assign, nonatomic) NSUInteger rxBytes;
/**
 * The bitrate (Kbps) of sending the audio packet.
 */
@property(assign, nonatomic) NSUInteger txAudioKBitrate;
/**
 * The bitrate (Kbps) of receiving the audio.
 */
@property(assign, nonatomic) NSUInteger rxAudioKBitrate;
/**
 * The bitrate (Kbps) of sending the video.
 */
@property(assign, nonatomic) NSUInteger txVideoKBitrate;
/**
 * The bitrate (Kbps) of receiving the video.
 */
@property(assign, nonatomic) NSUInteger rxVideoKBitrate;
/**
 * The total number of audio bytes sent, represented by an aggregate value.
 */
@property(assign, nonatomic) NSUInteger txAudioBytes;
/**
 * The total number of video bytes sent, represented by an aggregate value.
 */
@property(assign, nonatomic) NSUInteger txVideoBytes;
/**
 * The total number of audio bytes received, represented by an aggregate value.
 */
@property(assign, nonatomic) NSUInteger rxAudioBytes;
/**
 * The total number of video bytes received, represented by an aggregate value.
 */
@property(assign, nonatomic) NSUInteger rxVideoBytes;
/**
 * The client-to-server delay (milliseconds).
 */
@property(assign, nonatomic) NSUInteger lastmileDelay;
/**
 * The number of users in the channel.
 */
@property(assign, nonatomic) NSUInteger userCount;
/**
 * Application CPU usage (%).
 * @note - The value of `cpuAppUsage` is always reported as 0 in the
 * `rtcEngine:didLeaveChannelWithStats:` callback.
 */
@property(assign, nonatomic) double cpuAppUsage;
/**
 * The system CPU usage (%).
 * @note - The value of `cpuTotalUsage` is always reported as 0 in the
 * `rtcEngine:didLeaveChannelWithStats:` callback.
 */
@property(assign, nonatomic) double cpuTotalUsage;
/**
 * The round-trip time delay (ms) from the client to the local router.
 * @note
 * This property is disabled on devices running iOS 14 or later, and enabled on devices running
 * versions earlier than iOS 14 by default.
 * To enable this property on devices running iOS 14 or later, `technical support`.
 */
@property(assign, nonatomic) NSInteger gatewayRtt;
/**
 * The memory ratio occupied by the app (%).
 * @note This value is for reference only. Due to system limitations, you may not get this value.
 */
@property(assign, nonatomic) double memoryAppUsageRatio;
/**
 * The memory occupied by the system (%).
 * @note This value is for reference only. Due to system limitations, you may not get this value.
 */
@property(assign, nonatomic) double memoryTotalUsageRatio;
/**
 * The memory size occupied by the app (KB).
 * @note This value is for reference only. Due to system limitations, you may not get this value.
 */
@property(assign, nonatomic) NSInteger memoryAppUsageInKbytes;
/**
 * The duration (ms) between the SDK starts connecting and the connection is established. If the
 * value reported is 0, it means invalid.
 */
@property(assign, nonatomic) NSInteger connectTimeMs;

/**
 * The actual bitrate (Kbps) while sending the local video stream.
 */
@property(assign, nonatomic) NSUInteger txKBitrate;
/**
 * The receiving bitrate (Kbps).
 */
@property(assign, nonatomic) NSUInteger rxKBitrate;

/**The duration(ms) between first audio packet received and connection start, 0 if not valid
 */
@property(assign, nonatomic) NSInteger firstAudioPacketDuration;
/** The duration(ms) between first video packet received and connection start, 0 if not valid
 */
@property(assign, nonatomic) NSInteger firstVideoPacketDuration;
/** The duration(ms) between first video key frame received and connection start, 0 if not valid
 */
@property(assign, nonatomic) NSInteger firstVideoKeyFramePacketDuration;
/** Video packet number before first video key frame received, 0 if not valid
 */
@property(assign, nonatomic) NSInteger packetsBeforeFirstKeyFramePacket;
/**
 * The packet loss rate (%) from the client to the Agora server before applying the anti-packet-loss
 * algorithm.
 */
@property(assign, nonatomic) NSInteger txPacketLossRate;
/**
 * The packet loss rate (%) from the Agora server to the client before using the anti-packet-loss
 * method.
 */
@property(assign, nonatomic) NSInteger rxPacketLossRate;
/**
 * The local network acceleration state.
 * A value of 1 indicates that local network acceleration is active, while 0 indicates it is inactive.
 * @technical preview
 */
@property(assign, nonatomic) NSInteger lanAccelerateState;

@end
/**
 * @brief Advanced options for video encoding.
 */
__attribute__((visibility("default"))) @interface AgoraAdvancedVideoOptions : NSObject
/**
 * Video encoder preference. See `AgoraEncodingPreference`.
 */
@property(assign, nonatomic) AgoraEncodingPreference encodingPreference;

/**
 * Compression preference for video encoding. See `AgoraCompressionPreference`.
 */
@property(assign, nonatomic) AgoraCompressionPreference compressionPreference;

/**
 * Whether to encode and send the Alpha data present in the video frame to the remote end:
 * - `YES`: Encode and send Alpha data.
 * - `NO`: (Default) Do not encode and send Alpha data.
 */
@property(assign, nonatomic) BOOL encodeAlpha;

@end

/**
 * @brief The level of the codec capability.
 */
__attribute__((visibility("default"))) @interface AgoraVideoCodecCapLevels : NSObject

/**
 * Hardware decoding capability level, which represents the device's ability to perform hardware
 * decoding on videos of different quality. See `AgoraVideoCodecCapabilityLevel`.
 */
@property(assign, nonatomic) AgoraVideoCodecCapabilityLevel hwDecodingLevel;
/**
 * Software decoding capability level, which represents the device's ability to perform software
 * decoding on videos of different quality. See `AgoraVideoCodecCapabilityLevel`.
 */
@property(assign, nonatomic) AgoraVideoCodecCapabilityLevel swDecodingLevel;

@end

/**
 * @brief Focal length information supported by the camera, including the camera direction and focal
 * length type.
 */
__attribute__((visibility("default"))) @interface AgoraFocalLengthInfo : NSObject

/**
 * The camera direction. See `AgoraCameraDirection`.
 */
@property(assign, nonatomic) int cameraDirection;

/**
 * The focal length type. See `AgoraFocalLength`.
 */
@property(assign, nonatomic) AgoraFocalLength focalLengthType;

@end

/**
 * @brief The codec capability of the SDK.
 */
__attribute__((visibility("default"))) @interface AgoraVideoCodecCapInfo : NSObject

/**
 * The video codec types. See `AgoraVideoCodecType`.
 */
@property(assign, nonatomic) AgoraVideoCodecType codecType;
/**
 * Bit mask of the codec types in SDK. See `AgoraCodecCapMask`.
 */
@property(assign, nonatomic) NSUInteger codecCapMask;

/**
 * Codec capability of the SDK. See `AgoraVideoCodecCapLevels`.
 */
@property(strong, nonatomic) AgoraVideoCodecCapLevels *_Nonnull codecCapLevels;

@end
/**
 * @brief Video encoder configurations.
 */
__attribute__((visibility("default"))) @interface AgoraVideoEncoderConfiguration : NSObject
/**
 * Users can set the resolution by themselves, or directly select the desired resolution from the
 * following list:
 * - AgoraVideoDimension120x120: The video resolution is 120 × 120.
 * - AgoraVideoDimension160x120: The video resolution is 160 × 120.
 * - AgoraVideoDimension180x180: The video resolution is 180 × 180.
 * - AgoraVideoDimension240x180: The video resolution is 240 × 180.
 * - AgoraVideoDimension320x180: The video resolution is 320 × 180.
 * - AgoraVideoDimension240x240: The video resolution is 240 × 240.
 * - AgoraVideoDimension320x240: The video resolution is 320 × 240.
 * - AgoraVideoDimension424x240: The video resolution is 424 × 240.
 * - AgoraVideoDimension360x360: The video resolution is 360 × 360.
 * - AgoraVideoDimension480x360: The video resolution is 480 × 360.
 * - AgoraVideoDimension640x360: The video resolution is 640 × 360.
 * - AgoraVideoDimension480x480: The video resolution is 480 × 480.
 * - AgoraVideoDimension640x480: The video resolution is 640 × 480.
 * - AgoraVideoDimension840x480: The video resolution is 840 × 480.
 * - AgoraVideoDimension960x540: The video resolution is 960 × 540.
 * - AgoraVideoDimension960x720: The video resolution is 960 × 720.
 * - AgoraVideoDimension1280x720: The video resolution is 1280 × 720.
 * - AgoraVideoDimension1920x1080: The video resolution is 1920 × 1080.
 * - AgoraVideoDimension2540x1440: The video resolution is 2540 × 1440.
 * - AgoraVideoDimension3840x2160: The video resolution is 3840 × 2160.
 * @note
 * - Whether the 720p resolution or above can be supported depends on the device. If the device
 * cannot support 720p, the frame rate will be lower than the set value.
 * - The default value is 960 × 540.
 */
@property(assign, nonatomic) CGSize dimensions;

/**
 * The codec type of the local video stream. See `AgoraVideoCodecType`.
 */
@property(assign, nonatomic) AgoraVideoCodecType codecType;

/**
 * The frame rate (fps) of the encoding video frame. The default value is 15. See
 * `AgoraVideoFrameRate`.
 */
@property(assign, nonatomic) NSInteger frameRate;

/**
 * The encoding bitrate (Kbps) of the video. This parameter does not need to be set; keeping the
 * default value `AgoraVideoBitrateStandard` is sufficient. The SDK automatically matches the most
 * suitable bitrate based on the video resolution and frame rate you have set. For the
 * correspondence between video resolution and frame rate, see `Video profile`.
 * - AgoraVideoBitrateStandard (0): (Recommended) Standard bitrate mode.
 * - AgoraVideoBitrateCompatible (-1): Adaptive bitrate mode. In general, Agora suggests that you do
 * not use this value.
 */
@property(assign, nonatomic) NSInteger bitrate;

/**
 * The minimum encoding bitrate (Kbps) of the video.
 * The SDK automatically adjusts the encoding bitrate to adapt to the network conditions. Using a
 * value greater than the default value forces the video encoder to output high-quality images but
 * may cause more packet loss and sacrifice the smoothness of the video transmission. Unless you
 * have special requirements for image quality, Agora does not recommend changing this value.
 * @note This parameter only applies to the interactive streaming profile.
 */
@property(assign, nonatomic) NSInteger minBitrate;

/**
 * The orientation mode of the encoded video. See `AgoraVideoOutputOrientationMode`.
 */
@property(assign, nonatomic) AgoraVideoOutputOrientationMode orientationMode;

/**
 * Sets the mirror mode of the published local video stream. It only affects the video that the
 * remote user sees. See `AgoraVideoMirrorMode`.
 * @note By default, the video is not mirrored.
 */
@property(assign, nonatomic) AgoraVideoMirrorMode mirrorMode;

/**
 * Video degradation preference under limited bandwidth. See `AgoraDegradationPreference`.
 * @note When this parameter is set to AgoraDegradationMaintainFramerate (1) or
 * AgoraDegradationBalanced (2), `orientationMode` needs to be set to
 * AgoraVideoOutputOrientationModeAdaptative (0) at the same time, otherwise the setting will not
 * take effect.
 */
@property (assign, nonatomic) AgoraDegradationPreference degradationPreference;

/**
 * Advanced options for video encoding. See `AgoraAdvancedVideoOptions`.
 */
@property (strong, nonatomic) AgoraAdvancedVideoOptions *_Nullable advancedVideoOptions;

/** 
 * @brief Specifies the video resolution and initializes the `AgoraVideoEncoderConfiguration` object.
 *
 * @param size The video resolution.
 * @param frameRate The video frame rate. See `AgoraVideoFrameRate`.
 * @param bitrate The bitrate of the encoding video.
 * @param orientationMode The orientation of the encoding video. See `AgoraVideoOutputOrientationMode`.
 * @param mirrorMode Sets the mirror mode of the published local video stream. It only affects the video that
 * the remote user sees. See `AgoraVideoMirrorMode`.
 * @return The initialized `AgoraVideoEncoderConfiguration` object.
 */
- (instancetype _Nonnull)initWithSize:(CGSize)size
                            frameRate:(NSInteger)frameRate
                              bitrate:(NSInteger)bitrate
                      orientationMode:(AgoraVideoOutputOrientationMode)orientationMode
                           mirrorMode:(AgoraVideoMirrorMode)mirrorMode NS_SWIFT_NAME(init(size:frameRate:bitrate:orientationMode:mirrorMode:));

/** 
 * @brief Specifies the video width and height, and initializes the `AgoraVideoEncoderConfiguration` object.
 *
 * @param width The width of the video.
 * @param height The height of the video.
 * @param frameRate The video frame rate. See `AgoraVideoFrameRate`.
 * @param bitrate The bitrate of the encoding video.
 * @param orientationMode The orientation of the encoding video. See `AgoraVideoOutputOrientationMode`.
 * @param mirrorMode Sets the mirror mode of the published local video stream. It only affects the video that
 * the remote user sees. See `AgoraVideoMirrorMode`.
 * @return The initialized `AgoraVideoEncoderConfiguration` object.
 */
- (instancetype _Nonnull)initWithWidth:(NSInteger)width
                                height:(NSInteger)height
                             frameRate:(NSInteger)frameRate
                               bitrate:(NSInteger)bitrate
                       orientationMode:(AgoraVideoOutputOrientationMode)orientationMode
                            mirrorMode:(AgoraVideoMirrorMode)mirrorMode NS_SWIFT_NAME(init(width:height:frameRate:bitrate:orientationMode:mirrorMode:));
@end

/**
 * @brief Transcoding configurations of each host.
 */
__attribute__((visibility("default"))) @interface AgoraLiveTranscodingUser : NSObject
/**
 * The user ID of the host.
 */
@property(assign, nonatomic) NSUInteger uid;
/**
 * The position and size of the host's video in the output image. The data size is CGRect.
 */
@property(assign, nonatomic) CGRect rect;
/**
 * The layer index number of the host's video. The value range is [0, 100].
 * - 0: (Default) The host's video is the bottom layer.
 * - 100: The host's video is the top layer.
 * @note
 * - If the value is less than 0 or greater than 100, `AgoraErrorCodeInvalidArgument` error is
 * returned.
 * - Setting zOrder to 0 is supported.
 */
@property(assign, nonatomic) NSInteger zOrder;
/**
 * The transparency of the host's video. The value range is [0.0,1.0].
 * - 0.0: Completely transparent.
 * - 1.0: (Default) Opaque.
 */
@property(assign, nonatomic) double alpha;
/**
 * The audio channel used by the host's audio in the output audio. The default value is 0, and the
 * value range is [0, 5].
 * - `0`: (Recommended) The defaut setting, which supports dual channels at most and depends on the
 * upstream of the host.
 * - `1`: The host's audio uses the FL audio channel. If the host's upstream uses multiple audio
 * channels, the Agora server mixes them into mono first.
 * - `2`: The host's audio uses the FC audio channel. If the host's upstream uses multiple audio
 * channels, the Agora server mixes them into mono first.
 * - `3`: The host's audio uses the FR audio channel. If the host's upstream uses multiple audio
 * channels, the Agora server mixes them into mono first.
 * - `4`: The host's audio uses the BL audio channel. If the host's upstream uses multiple audio
 * channels, the Agora server mixes them into mono first.
 * - `5`: The host's audio uses the BR audio channel. If the host's upstream uses multiple audio
 * channels, the Agora server mixes them into mono first.
 * - `0xFF` or a value greater than `5`: The host's audio is muted, and the Agora server removes the
 * host's audio.
 * @note If the value is not `0`, a special player is required.
 */
@property(assign, nonatomic) NSInteger audioChannel;
@end

/**
 * @brief The configuration for advanced features of the RTMP or RTMPS streaming with transcoding.
 *
 * @details
 * If you want to enable the advanced features of streaming with transcoding, contact
 * `support@agora.io`.
 *
 */
__attribute__((visibility("default"))) @interface AgoraLiveStreamAdvancedFeature : NSObject

/**
 * The feature names, including LBHQ (high-quality video with a lower bitrate) and VEO (optimized
 * video encoder).
 */
@property(copy, nonatomic) NSString* _Nullable featureName;

/**
 * Whether to enable the advanced features of streaming with transcoding:
 * - `YES`: Enable the advanced features.
 * - `NO`: (Default) Do not enable the advanced features.
 */
@property(assign, nonatomic) BOOL opened;
@end

/**
 * @brief Image properties.
 *
 * @details
 * This class sets the properties of the watermark and background images in the live video.
 *
 */
__attribute__((visibility("default"))) @interface AgoraImage : NSObject
/**
 * The HTTP/HTTPS URL address of the image in the live video. The maximum length of this parameter
 * is 1024 bytes.
 */
@property(strong, nonatomic) NSURL *_Nonnull url;
/**
 * The position and size of the image on the video frame. The data type is CGRect.
 */
@property(assign, nonatomic) CGRect rect;
/**
 * The layer index of the watermark or background image. When you use the watermark array to add a
 * watermark or multiple watermarks, you must pass a value to `zOrder` in the range [1,255];
 * otherwise, the SDK reports an error. In other cases, `zOrder` can optionally be passed in the
 * range [0,255], with 0 being the default value. 0 means the bottom layer and 255 means the top
 * layer.
 */
@property (assign, nonatomic) NSInteger zOrder;
/**
 * The transparency of the watermark or background image. The range of the value is [0.0,1.0]:
 * - 0.0: Completely transparent.
 * - 1.0: (Default) Opaque.
 */
@property(assign, nonatomic) double alpha;
@end

/**
 * @brief Watermark image configurations.
 *
 * @details
 * Configuration options for setting the watermark image to be added.
 *
 */
__attribute__((visibility("default"))) @interface WatermarkOptions : NSObject
/**
 * Whether the watermark is visible in the local preview view:
 * - `YES`: (Default) The watermark is visible in the local preview view.
 * - `NO`: The watermark is not visible in the local preview view.
 */
@property(assign, nonatomic) BOOL visibleInPreview;
/**
 * The area to display the watermark image in landscape mode. This parameter contains the following
 * members:
 * - x: The horizontal position of the watermark relative to the upper left corner.
 * - y: The vertical position of the watermark relative to the upper left corner.
 * - width: The width of the watermark area (px).
 * - height: The height of the watermark area (px).
 */
@property(assign, nonatomic) CGRect positionInLandscapeMode;
/**
 * The area to display the watermark image in portrait mode. This parameter contains the following
 * members:
 * - x: The horizontal position of the watermark relative to the upper left corner.
 * - y: The vertical position of the watermark relative to the upper left corner.
 * - width: The width of the watermark area (px).
 * - height: The height of the watermark area (px).
 */
@property(assign, nonatomic) CGRect positionInPortraitMode;
/**
 * Layer order of the watermark image. The default value is 0.
 */
@property(assign, nonatomic)  int zOrder;
@end

/**
 * @brief Configures the format, size, and pixel buffer of the watermark image.
 *
 * @since 4.6.0
 *
 * @details
 * Defines the buffer data structure of the watermark image, including image width, height, format,
 * length, and image data buffer.
 *
 */
__attribute__((visibility("default"))) @interface WatermarkBuffer : NSObject
 /**
  * Width of the watermark image, in pixels.
  */
 @property(assign, nonatomic)  int width;
 /**
  * Height of the watermark image, in pixels.
  */
 @property(assign, nonatomic)  int height;
 /**
  * Length of the buffer, in bytes.
  */
 @property(assign, nonatomic)  int length;
 /**
  * Raw image data buffer.
  */
 @property(strong, nonatomic) NSData* _Nullable buffer;
/**
 * Format of the watermark buffer. See `WatermarkBufferFormat`.
 */
@property(assign, nonatomic) WatermarkBufferFormat format;
@end

/**
 * @brief Used to configure watermark-related information.
 *
 * @since 4.6.0
 */
__attribute__((visibility("default"))) @interface WatermarkConfig : NSObject
/**
 * Unique identifier for the watermark.
 */
@property(copy, nonatomic) NSString* _Nonnull id;
/**
 * Type of the watermark. See `WatermarkType`.
 */
@property(assign, nonatomic) WatermarkType type;
/**
 * Buffer data of the watermark image. See `WatermarkBuffer`.
 */
@property(strong, nonatomic) WatermarkBuffer* _Nullable buffer;
/**
 * URL of the watermark image.
 */
@property(strong, nonatomic) NSURL *_Nullable imageUrl;
/**
 * Display options for the watermark. See `WatermarkOptions`.
 */
@property(strong, nonatomic) WatermarkOptions* _Nonnull options;
@end

/**
 * @brief Transcoding configurations for Media Push.
 */
__attribute__((visibility("default"))) @interface AgoraLiveTranscoding : NSObject
/**
 * Manages the user layout configuration in the Media Push. Agora supports a maximum of 17
 * transcoding users in a Media Push channel. See `AgoraLiveTranscodingUser`.
 */
@property(copy, nonatomic) NSArray<AgoraLiveTranscodingUser*>* _Nullable transcodingUsers;

/**
 * The size of the video (width and height in pixels).
 * - When pushing video streams to the CDN, note the following:
 *   - The value range of the width is [64,1920]. If the value is less than 64, Agora server
 * automatically adjusts it to 64; if the value is greater than 1920, Agora server automatically
 * adjusts it to 1920.
 *   - The value range of the height is [64,1080]. If the value is less than 64, Agora server
 * automatically adjusts it to 64; if the value is greater than 1080, Agora server automatically
 * adjusts it to 1080.
 * - When pushing audio streams to the CDN, set the width and height as 0.
 */
@property(assign, nonatomic) CGSize size;
/**
 * The encoding bitrate (Kbps) of the video. This parameter does not need to be set; keeping the
 * default value `AgoraVideoBitrateStandard` is sufficient. The SDK automatically matches the most
 * suitable bitrate based on the video resolution and frame rate you have set. For the
 * correspondence between video resolution and frame rate, see `Video profile`.
 */
@property(assign, nonatomic) NSInteger videoBitrate;
/**
 * Frame rate (fps) of the output video stream set for Media Push. The default value is 15. The
 * value range is (0,30].
 * @note The Agora server adjusts any value over 30 to 30.
 */
@property(assign, nonatomic) NSInteger videoFramerate;
/**
 * Deprecated
 * This member is deprecated.
 * Latency mode:
 * - `YES`: Low latency with unassured quality.
 * - `NO`: (Default) High latency with assured quality.
 */
@property(assign, nonatomic) BOOL lowLatency;
/**
 * GOP (Group of Pictures) in fps of the video frames for Media Push. The default value is 30.
 */
@property(assign, nonatomic) NSInteger videoGop;
/**
 * Video codec profile type for Media Push. Set it as 66, 77, or 100 (default). See
 * `AgoraVideoCodecProfileType` for details.
 * @note If you set this parameter to any other value, Agora adjusts it to the default value.
 */
@property(assign, nonatomic) AgoraVideoCodecProfileType videoCodecProfile;

/**
 * Video codec profile types for Media Push. See `AgoraVideoCodecTypeForStream`.
 * @since v3.2.0
 */
@property(assign, nonatomic) AgoraVideoCodecTypeForStream videoCodecType;

/**
 * Reserved property. Extra user-defined information to send SEI for the H.264/H.265 video stream to
 * the CDN live client. Maximum length: 4096 bytes. For more information on SEI, see SEI-related
 * questions.
 */
@property(copy, nonatomic) NSString* _Nullable transcodingExtraInfo;

/**
 * The array of watermarks on the live video. See `AgoraImage`. You can add one or more `watermarks`
 * using `watermarkArray`. The image format needs to be PNG. The total number of watermarks and
 * background images on a live video must be greater than or equal to 0 and less than or equal to
 * 10.
 */
@property(copy, nonatomic) NSArray<AgoraImage*>* _Nullable watermarkArray;

/**
 * An array of background images on the live video. See `AgoraImage`. You can use
 * `backgroundImageArray` to add one or more background images. The image format needs to be PNG.
 * The total number of watermarks and background images on a live video must be greater than or
 * equal to 0 and less than or equal to 10.
 */
@property(copy, nonatomic) NSArray<AgoraImage*>* _Nullable backgroundImageArray;

/**
 * The background color in for Media Push.
 * The color of the custom background image. The format is a hexadecimal integer defined by RGB,
 * without the # sign, such as 0xFFB6C1 for light pink. The default value is 0x000000(black).
 * COLOR_CLASS is a general name for the type:
 * - iOS: UIColor
 * - macOS: NSColor
 */
@property(strong, nonatomic) COLOR_CLASS* _Nullable backgroundColor;

/**
 * The audio sampling rate (Hz) of the output media stream. See `AgoraAudioSampleRateType`.
 */
@property(assign, nonatomic) AgoraAudioSampleRateType audioSampleRate;
/**
 * Bitrate (Kbps) of the audio output stream for Media Push. The default value is 48, and the
 * highest value is 128.
 */
@property(assign, nonatomic) NSInteger audioBitrate;
/**
 * The number of audio channels for Media Push. Agora recommends choosing 1 (mono), or 2 (stereo)
 * audio channels. Special players are required if you choose 3, 4, or 5.
 * - 1: (Default) Mono.
 * - 2: Stereo.
 * - 3: Three audio channels.
 * - 4: Four audio channels.
 * - 5: Five audio channels.
 */
@property(assign, nonatomic) NSInteger audioChannels;
/**
 * Audio codec profile type for Media Push. See `AgoraAudioCodecProfileType`.
 */
@property(assign, nonatomic) AgoraAudioCodecProfileType audioCodecProfile;

/** Creates a default transcoding object.

 @return Default AgoraLiveTranscoding object.
 */
+ (AgoraLiveTranscoding* _Nonnull)defaultTranscoding NS_SWIFT_NAME(default());

/** Adds a user displaying the video in CDN live.

 @param user The transcoding user. See AgoraLiveTranscodingUser.

 @return - 0: Success.
 - < 0: Failure.
 */
- (int)addUser:(AgoraLiveTranscodingUser* _Nonnull)user NS_SWIFT_NAME(add(_:));

/** Removes a user from CDN live.

 @param uid The user ID of the user to remove from CDN live.

 @return - 0: Success.
 - < 0: Failure.
 */
- (int)removeUser:(NSUInteger)uid NS_SWIFT_NAME(removeUser(_:));

/** Enables/Disables advanced features of the RTMP or RTMPS streaming with transcoding.

 @param featureName The name of the advanced feature, including the following:
 <li>LBHQ: The advanced feature for high-quality video with a lower bitrate.</li>
 <li>VEO: The advanced feature for the optimized video encoder.</li>
 @param opened Whether to enable the advanced feature:
 <li>YES: Enable the advanced feature.</li>
 <li>NO: (Default) Disable the advanced feature.</li>
 */
- (void)setAdvancedFeatures:(NSString* _Nonnull)featureName opened:(BOOL)opened NS_SWIFT_NAME(setAdvancedFeatures(_:opened:));

/** Checks whether advanced features of the RTMP or RTMPS streaming with transcoding are enabled.
 @return The name of each advanced feature and whether the advanced feature is enabled.
 */
- (NSArray<AgoraLiveStreamAdvancedFeature*>* _Nullable)getAdvancedFeatures NS_SWIFT_NAME(getAdvancedFeatures());

@end

/** Live broadcast import stream configuration.
 */
__attribute__((visibility("default"))) @interface AgoraLiveInjectStreamConfig : NSObject
/** Size of the stream to be imported into the live broadcast. The default value is 0; same
 * size as the original stream.
 */
@property(assign, nonatomic) CGSize size;
/** Video GOP of the stream to be added into the broadcast. The default value is 30.
 */
@property(assign, nonatomic) NSInteger videoGop;
/** Video frame rate of the stream to be added into the broadcast. The default value is 15 fps.
 */
@property(assign, nonatomic) NSInteger videoFramerate;
/** Video bitrate of the stream to be added into the broadcast. The default value is 400 Kbps.
 */
@property(assign, nonatomic) NSInteger videoBitrate;

/** Audio sampling rate of the stream to be added into the broadcast. The default value is 48000.
 */
@property(assign, nonatomic) AgoraAudioSampleRateType audioSampleRate;
/** Audio bitrate of the stream to be added into the broadcast. The default value is 48 Kbps.
 */
@property(assign, nonatomic) NSInteger audioBitrate;
/** Audio channels to be added into the broadcast. The default value is 1.
 */
@property(assign, nonatomic) NSInteger audioChannels;

/** Create a default stream config

 @return default stream config
 */
+ (AgoraLiveInjectStreamConfig *_Nonnull)defaultConfig NS_SWIFT_NAME(defaultConfig());
@end

    __deprecated

    /** AgoraRtcVideoCompositingRegion array.
     */
    __attribute__((visibility("default"))) @interface AgoraRtcVideoCompositingRegion
    : NSObject
      /** User ID of the user with the video to be displayed in the region.
       */
      @property(assign, nonatomic) NSUInteger uid;
/** Horizontal position of the region on the screen (0.0 to 1.0).
 */
@property(assign, nonatomic) CGFloat x;
/** Vertical position of the region on the screen (0.0 to 1.0).
 */
@property(assign, nonatomic) CGFloat y;
/** Actual width of the region (0.0 to 1.0).
 */
@property(assign, nonatomic) CGFloat width;
/** Actual height of the region (0.0 to 1.0).
 */
@property(assign, nonatomic) CGFloat height;
/** 0 means the region is at the bottom, and 100 means the region is at the top.
 */
@property(assign, nonatomic) NSInteger zOrder;
/** 0 means the region is transparent, and 1 means the region is opaque. The default value is 1.0.
 */
@property(assign, nonatomic) CGFloat alpha;
/** Render mode: AgoraVideoRenderMode
 */
@property(assign, nonatomic) AgoraVideoRenderMode renderMode;
@end

    __deprecated
    /** Rtc video compositing layout.
     */
    __attribute__((visibility("default"))) @interface AgoraRtcVideoCompositingLayout
    : NSObject
      /** Width of the entire canvas (display window or screen).
       */
      @property(assign, nonatomic) NSInteger canvasWidth;
/** Height of the entire canvas (display window or screen).
 */
@property(assign, nonatomic) NSInteger canvasHeight;
/** Enter any of the 6-digit symbols defined in RGB. For example, "#c0c0c0"
 */
@property(copy, nonatomic) NSString *_Nullable backgroundColor;
/** AgoraRtcVideoCompositingRegion array.
 */
@property(copy, nonatomic) NSArray<AgoraRtcVideoCompositingRegion *> *_Nullable regions;
/** Application defined data.
 */
@property(copy, nonatomic) NSString *_Nullable appData;
@end

    /**
     @deprecated

     Sets whether the current host is the RTMP stream owner.
     */
    __deprecated __attribute__((visibility("default"))) @interface AgoraPublisherConfiguration
    : NSObject
      /**
       - YES: The current host is the RTMP stream owner and the push-stream configuration is enabled
       (default). - NO: The current host is not the RTMP stream owner and the push-stream
       configuration is disabled.
       */
      @property(assign, nonatomic) BOOL owner;

/** Width of the output data stream set for CDN Live. 360 is the default value
 */
@property(assign, nonatomic) NSInteger width;
/** Height of the output data stream set for CDN Live. 640 is the default value
 */
@property(assign, nonatomic) NSInteger height;
/** Frame rate of the output data stream set for CDN Live. 15 fps is the default value
 */
@property(assign, nonatomic) NSInteger framerate;
/** Bitrate of the output data stream set for CDN Live. 500 kbit/s is the default value
 */
@property(assign, nonatomic) NSInteger bitrate;
/** Audio sample rate of the output data stream set for CDN Live. The default value is 48000.
 */
@property(assign, nonatomic) NSInteger audiosamplerate;
/** Audio bitrate of the output data stream set for CDN Live.  The default value is 48.
 */
@property(assign, nonatomic) NSInteger audiobitrate;
/** Audio channels of the output data stream set for CDN Live. The default value is 1.
 */
@property(assign, nonatomic) NSInteger audiochannels;

/**

* 0: Tile Horizontally
* 1: Layered Windows
* 2: Tile Vertically
 */
@property(assign, nonatomic) NSInteger defaultLayout;
/** Video stream lifecycle of CDN Live: AgoraRtmpStreamLifeCycle
 */
@property(assign, nonatomic) AgoraRtmpStreamLifeCycle lifeCycle;

/** Width of the stream to be injected. Set it as 0.
 */
@property(assign, nonatomic) NSInteger injectStreamWidth;

/** Height of the stream to be injected. Set it as 0.
 */
@property(assign, nonatomic) NSInteger injectStreamHeight;

/** Address of the stream to be injected to the channel.
 */
@property(copy, nonatomic) NSString *_Nullable injectStreamUrl;

/** The push-stream address for the picture-in-picture layouts. The default value is *NULL*.
 */
@property(copy, nonatomic) NSString *_Nullable publishUrl;

/** The push-stream address of the original stream which does not require picture-blending. The
 * default value is NULL.
 */
@property(copy, nonatomic) NSString *_Nullable rawStreamUrl;

/** Reserved field. The default value is NULL.
 */
@property(copy, nonatomic) NSString *_Nullable extraInfo;

/** Check if configuration is validate
 */
- (BOOL)validate NS_SWIFT_NAME(validate());

- (NSString * _Nullable)toJsonString NS_SWIFT_NAME(toJsonString());
@end

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))

/**
 * @brief The class that provides device information.
 */
__attribute__((visibility("default"))) @interface AgoraRtcDeviceInfo : NSObject
@property (assign, nonatomic) int __deprecated index;

/**
 * The device type. See `AgoraMediaDeviceType`.
 */
@property(assign, nonatomic) AgoraMediaDeviceType type;

/**
 * The device ID.
 */
@property(copy, nonatomic) NSString *_Nullable deviceId;

/**
 * The device name.
 */
@property(copy, nonatomic) NSString *_Nullable deviceName;

/**
 * The type of audio devices, such as built-in, USB and HDMI.
 */
@property(copy, nonatomic) NSString *_Nullable deviceTypeName;
@end
#endif

/** Properties of the AgoraColorSpace object.
 * Default value is (BT709, BT709, BT709, Full).
 */
__attribute__((visibility("default"))) @interface AgoraColorSpace : NSObject
/** Primary id of color space
 */
@property(assign, nonatomic) AgoraColorPrimaryID primaryID;
/** Transfer function id of color space
 */
@property(assign, nonatomic) AgoraColorTransferID transferID;
/** Color matrix id of yuv
 */
@property(assign, nonatomic) AgoraColorMatrixID matrixID;
/** Color range id of yuv
 */
@property(assign, nonatomic) AgoraColorRangeID rangeID;
@end

/**
 * @brief The external video frame.
 */
__attribute__((visibility("default"))) @interface AgoraVideoFrame : NSObject
/**
 * The format of the incoming video frame. This parameter must be specified as one of the following
 * values:
 * - 1: I420
 * - 2: BGRA
 * - 3: NV21
 * - 4: RGBA
 * - 5: IMC2
 * - 7: ARGB
 * - 8: NV12
 * - 12: iOS texture (CVPixelBufferRef)
 * - 16: I422
 */
@property(assign, nonatomic) NSInteger format;

/**
 * Timestamp (ms) of the incoming video frame. An incorrect timestamp results in frame loss or
 * unsynchronized audio and video.
 */
@property(assign, nonatomic) CMTime time;  // Time for this frame.

/**
 * The line span of the frame, that is, the number of pixels between two adjacent lines of video
 * frames.
 * @note
 * - The unit of this parameter is pixels, not bytes.
 * - If the video is in Texture format, set this parameter to the width of Texture.
 * - If the video frame format is set to 12, do not use this field.
 * - When dealing with video data, it is necessary to process the offset between each line of pixel
 * data based on this parameter, otherwise it may result in image distortion.
 * @deprecated Use strideInPixels instead.
 */
@property(assign, nonatomic) int stride DEPRECATED_MSG_ATTRIBUTE("use strideInPixels instead");

/** Line spacing of the incoming video frame, which must be in pixels instead of bytes. For
 * textures, it is the width of the texture.
 */
@property(assign, nonatomic) int strideInPixels;  // Number of pixels between two consecutive rows.
                                                  // Note: in pixel, not byte. Not used for iOS
                                                  // textures.

/**
 * Height of the incoming video frame.
 */
@property(assign, nonatomic) int height;  // Number of rows of pixels. Not used for iOS textures.

/**
 * Buffer of iOS texture.
 */
@property(assign, nonatomic) CVPixelBufferRef _Nullable textureBuf;

/** Still Image (UIImage for iPhone, NSImage for Mac)
 */
@property(strong, nonatomic) IMAGE_CLASS * _Nullable image;

/**
 * Raw data buffer. This parameter does not apply to iOS textures.
 */
@property(strong, nonatomic) NSData *_Nullable dataBuf;  // Raw data buffer. Not used for iOS textures.

/**
 *  Indicates the alpha channel of current frame, which is consistent with the dimension of the video frame.
 *  The value range of each pixel is [0,255], where 0 represents the background; 255 represents the foreground.
 *  The default value is nill.
 */
@property(strong, nonatomic) NSData *_Nullable alphaBuf;
/**
 * When the video frame contains alpha channel data, it represents the relative position of
 * `alphaBuffer` and the video frame. See `AgoraAlphaStitchMode`.
 */
@property(assign, nonatomic) AgoraAlphaStitchMode alphaStitchMode;

/**
 * Raw data related parameter. The number of pixels trimmed from the left. The default value is 0.
 */
@property(assign, nonatomic) int cropLeft;  // Number of pixels to crop on the left boundary.
/**
 * Raw data related parameter. The number of pixels trimmed from the top. The default value is 0.
 */
@property(assign, nonatomic) int cropTop;  // Number of pixels to crop on the top boundary.
/**
 * Raw data related parameter. The number of pixels trimmed from the right. The default value is 0.
 */
@property(assign, nonatomic) int cropRight;  // Number of pixels to crop on the right boundary.
/**
 * Raw data related parameter. The number of pixels trimmed from the bottom. The default value is 0.
 */
@property(assign, nonatomic) int cropBottom;  // Number of pixels to crop on the bottom boundary.
/**
 * Raw data related parameter. The clockwise rotation of the video frame. You can set the rotation
 * angle as 0, 90, 180, or 270. The default value is 0.
 */
@property(assign, nonatomic) int rotation;  // 0, 90, 180, 270. See document for rotation calculation.

/**
 * By default, the color space properties of video frames will apply the Full Range and BT.709
 * standard configurations.
 */
@property(strong, nonatomic) AgoraColorSpace *_Nullable colorSpace;
/* Note
 * 1. strideInPixels
 *    Stride is in pixels, not bytes
 * 2. About the frame width and height
 *    No field is defined for the width. However, it can be deduced by:
 *       croppedWidth = (strideInPixels - cropLeft - cropRight)
 *    And
 *       croppedHeight = (height - cropTop - cropBottom)
 * 3. About crop
 *    _________________________________________________________________.....
 *    |                        ^                                      |  ^
 *    |                        |                                      |  |
 *    |                     cropTop                                   |  |
 *    |                        |                                      |  |
 *    |                        v                                      |  |
 *    |                ________________________________               |  |
 *    |                |                              |               |  |
 *    |                |                              |               |  |
 *    |<-- cropLeft -->|          valid region        |<- cropRight ->|
 *    |                |                              |               | height
 *    |                |                              |               |
 *    |                |_____________________________ |               |  |
 *    |                        ^                                      |  |
 *    |                        |                                      |  |
 *    |                     cropBottom                                |  |
 *    |                        |                                      |  |
 *    |                        v                                      |  v
 *    _________________________________________________________________......
 *    |                                                               |
 *    |<---------------- strideInPixels ----------------------------->|
 *
 *    If your buffer contains garbage data, you can crop them. For example, the frame size is
 *    360 x 640, often the buffer stride is 368, that is, there extra 8 pixels on the
 *    right are for padding, and should be removed. In this case, you can set:
 *    strideInPixels = 368;
 *    height = 640;
 *    cropRight = 8;
 *    // cropLeft, cropTop, cropBottom are set to a default of 0
 */

/** If data format is BGRA or RGBA and alphaBuf is nill, it is required to call fillAlphaData to fill alphaBuf.
 */
- (void)fillAlphaData;
@end

/**
 * @brief Configuration of Agora SDK log files.
 */
__attribute__((visibility("default"))) @interface AgoraLogConfig: NSObject
/**
 * The complete path of the log files. Agora recommends using the default log directory. If you need
 * to modify the default directory, ensure that the directory you specify exists and is writable.
 * The default log directory is:
 * - iOS: AppSandbox/Library/Logs/agorasdk.log
 * - macOS:
 *   - If Sandbox is enabled: AppSandbox/Library/Logs/agorasdk.log
 *   - If Sandbox is disabled: ~/Library/Logs/agorasdk.log
 */
@property (copy, nonatomic) NSString * _Nullable filePath;
/**
 * The size (KB) of an `agorasdk.log` file. The value range is [128,20480]. The default value is
 * 2,048 KB. If you set `fileSizeInKByte` smaller than 128 KB, the SDK automatically adjusts it to
 * 128 KB; if you set `fileSizeInKByte` greater than 20,480 KB, the SDK automatically adjusts it to
 * 20,480 KB.
 */
@property (assign, nonatomic) NSInteger fileSizeInKB;
/**
 * The output level of the SDK log file. See `AgoraLogLevel`.
 * For example, if you set the log level to WARN, the SDK outputs the logs within levels FATAL,
 * ERROR, and WARN.
 */
@property (assign, nonatomic) AgoraLogLevel level;
@end

/**
 * @brief Configurations for the `AgoraRtcEngineConfig` instance.
 */
__attribute__((visibility("default"))) @interface AgoraRtcEngineConfig: NSObject

 /**
  * The App ID issued by Agora for your project. Only users in apps with the same App ID can join the
  * same channel and communicate with each other. An App ID can only be used to create one
  * `AgoraRtcEngineKit` instance. To change your App ID, call `destroy` to destroy the current
  * `AgoraRtcEngineKit` instance, and then create a new one.
  */
 @property (copy, nonatomic) NSString * _Nullable appId;

 /**
  * The channel profile. See `AgoraChannelProfile`.
  */
 @property (assign, nonatomic) AgoraChannelProfile channelProfile;

  /**
   * The license used for verification when connectting channel. Charge according to the license
   */
  @property (copy, nonatomic) NSString * _Nullable license;

 /**
  * The audio scenarios. Under different audio scenarios, the device uses different volume types. See
  * `AgoraAudioScenario`.
  */
 @property (assign, nonatomic) AgoraAudioScenario audioScenario;
 /**
  * The region for connection. This is an advanced feature and applies to scenarios that have
  * regional restrictions. For details on supported regions, see `AgoraAreaCodeType`. The area codes
  * support bitwise operation.
  */
 @property (assign, nonatomic) AgoraAreaCodeType areaCode;
 /**
  * Sets the log file size. See `AgoraLogConfig`.
  * By default, the SDK generates five SDK log files and five API call log files with the following
  * rules:
  * - The SDK log files are: `agorasdk.log`, `agorasdk.1.log`, `agorasdk.2.log`, `agorasdk.3.log`,
  * and `agorasdk.4.log`.
  * - The API call log files are: `agoraapi.log`, `agoraapi.1.log`, `agoraapi.2.log`,
  * `agoraapi.3.log`, and `agoraapi.4.log`.
  * - The default size of each SDK log file and API log file is 2,048 KB. These log files are encoded
  * in UTF-8.
  * - The SDK writes the latest logs in `agorasdk.log` or `agoraapi.log`.
  * - When `agorasdk.log` is full, the SDK processes the log files in the following order:1. Delete
  * the `agorasdk.4.log` file (if any).
  * 2. Rename `agorasdk.3.log` to `agorasdk.4.log`.
  * 3. Rename `agorasdk.2.log` to `agorasdk.3.log`.
  * 4. Rename `agorasdk.1.log` to `agorasdk.2.log`.
  * 5. Create a new `agorasdk.log` file.
  * - The overwrite rules for the `agoraapi.log` file are the same as for `agorasdk.log`.
  */
 @property (strong, nonatomic) AgoraLogConfig * _Nullable logConfig;
 /**
   * Thread priority for SDK common threads
   *
   * See \ref AgoraThreadPriorityType.
   */
 @property (assign, nonatomic) AgoraThreadPriorityType threadPriority;
 /**
  * The `AgoraMediaFilterEventDelegate` instance.
  */
 @property (weak, nonatomic) id<AgoraMediaFilterEventDelegate> _Nullable eventDelegate;
 /**
  * Whether to enable domain name restriction:
  * - `YES`: Enables the domain name restriction. This value is suitable for scenarios where IoT
  * devices use IoT cards for network access. The SDK will only connect to servers in the domain name
  * or IP whitelist that has been reported to the operator.
  * - `NO`: (Default) Disables the domain name restriction. This value is suitable for most common
  * scenarios.
  */
 @property (assign, nonatomic) BOOL domainLimit;

 /**
  * Whether to automatically register the Agora extensions when initializing `AgoraRtcEngineKit`:
  * - `YES`: (Default) Automatically register the Agora extensions when initializing
  * `AgoraRtcEngineKit`.
  * - `NO`: Do not register the Agora extensions when initializing `AgoraRtcEngineKit`. You need to
  * call `enableExtensionWithVendor:extension:enabled:sourceType:` to register the Agora extensions.
  */
 @property (assign, nonatomic) BOOL autoRegisterAgoraExtensions;
@end

/**
 * @brief Raw audio data.
 */
__attribute__((visibility("default"))) @interface AgoraAudioFrame : NSObject
/**
 * The number of samples per channel in the audio frame.
 */
@property(assign, nonatomic) NSInteger samplesPerChannel;
/**
 * The number of bytes per sample. For PCM, this parameter is generally set to 16 bits (2 bytes).
 */
@property(assign, nonatomic) NSInteger bytesPerSample;
/**
 * The number of audio channels (the data are interleaved if it is stereo).
 * - 1: Mono.
 * - 2: Stereo.
 */
@property(assign, nonatomic) NSInteger channels;
/**
 * The number of samples per channel in the audio frame.
 */
@property(assign, nonatomic) NSInteger samplesPerSec;
/**
 * The data buffer of the audio frame. When the audio frame uses a stereo channel, the data buffer
 * is interleaved.
 * The size of the data buffer is as follows: `buffer` = `samples` × `channels` × `bytesPerSample`.
 */
@property(assign, nonatomic) void* _Nullable buffer;
/**
 * The timestamp (ms) of the external audio frame.
 * You can use this timestamp to restore the order of the captured audio frame, and synchronize
 * audio and video frames in video scenarios, including scenarios where external video sources are
 * used.
 */
@property(assign, nonatomic) int64_t renderTimeMs;
/**
 */
@property(assign, nonatomic) int64_t presentationMs;
/**
 * Reserved for future use.
 */
@property(assign, nonatomic) NSInteger avSyncType;
/**
 * RTP timestamp of the first sample in the audio frame
 */
@property(assign, nonatomic) uint32_t rtpTimestamp;
@end

/**
 * @brief Audio data format.
 *
 * @details
 * You can pass the `AgoraAudioParams` object in the following APIs to set the audio data format for
 * the corresponding callback:
 * - `getRecordAudioParams`: Sets the audio data format for the `onRecordAudioFrame:channelId:`
 * callback.
 * - `getPlaybackAudioParams`: Sets the audio data format for the `onPlaybackAudioFrame:` callback.
 * - `getMixedAudioParams`: Sets the audio data format for the `onMixedAudioFrame:channelId:`
 * callback.
 * - `getEarMonitoringAudioParams`: Sets the audio data format for the `onEarMonitoringAudioFrame:`
 * callback.
 *
 * @note
 * - The SDK calculates the sampling interval through the `samplesPerCall`, `sampleRate`, and
 * `channel` parameters in `AgoraAudioParams`, and triggers the `onRecordAudioFrame:channelId:`,
 * `onPlaybackAudioFrame:`, `onMixedAudioFrame:channelId:`, and `onEarMonitoringAudioFrame:`
 * callbacks according to the sampling interval.
 * - Sample interval (sec) = `samplePerCall` /( `sampleRate` × `channel` ).
 * - Ensure that the sample interval ≥ 0.01 (s).
 *
 */
__attribute__((visibility("default"))) @interface AgoraAudioParams : NSObject

/**
 * The audio sample rate (Hz), which can be set as one of the following values:
 * - 8000.
 * - (Default) 16000.
 * - 32000.
 * - 44100
 * - 48000
 */
@property (assign, nonatomic) NSInteger sampleRate;

/**
 * The number of audio channels, which can be set as either of the following values:
 * - 1: (Default) Mono.
 * - 2: Stereo.
 */
@property (assign, nonatomic) NSInteger channel;

/**
 * The use mode of the audio data. See `AgoraAudioRawFrameOperationMode`.
 */
@property (assign, nonatomic) AgoraAudioRawFrameOperationMode mode;

/**
 * The number of samples, such as 1024 for the media push.
 */
@property (assign, nonatomic) NSInteger samplesPerCall;

@end

/**
 * @brief Audio information after encoding.
 */
__attribute__((visibility("default"))) @interface AgoraEncodedAudioFrameInfo: NSObject
 /**
  * The number of audio samples per channel.
  */
 @property (assign, nonatomic) NSInteger samplesPerChannel;
 /**
  * The number of audio channels.
  */
 @property (assign, nonatomic) NSInteger channels;
 /**
  * Audio sample rate (Hz).
  */
 @property (assign, nonatomic) NSInteger samplesPerSec;
 /**
  * Audio Codec type: `AgoraAudioCodecType`.
  */
 @property (assign, nonatomic) AgoraAudioCodecType codecType;
@end

/**
 * @brief The uplink network information.
 */
__attribute__((visibility("default"))) @interface AgoraUplinkNetworkInfo : NSObject
/**
 * The target video encoder bitrate (bps).
 */
@property(nonatomic, assign) int videoEncoderTargetBitrateBps;
@end

/**
 * The collections of downlink network info.
 */
__attribute__((visibility("default"))) @interface AgoraDownlinkNetworkInfo : NSObject
/**
* The lastmile buffer delay time in ms.
*/
@property(nonatomic, assign) int lastmileBufferDelayTimeMs;
/**
* The downlink bandwidth estimation bitrate bps.
*/
@property(nonatomic, assign) int bandwidthEstimationBps;
@end

/**
 * @brief The options for leaving a channel.
 */
__attribute__((visibility("default"))) @interface AgoraLeaveChannelOptions : NSObject
/**
 * Whether to stop playing and mixing the music file when a user leaves the channel.
 * - `YES`: (Default) Stop playing and mixing the music file.
 * - `NO`: Do not stop playing and mixing the music file.
 */
@property(nonatomic, assign) BOOL stopAudioMixing;

/**
 * Whether to stop playing all audio effects when a user leaves the channel.
 * - `YES`: (Default) Stop playing all audio effects.
 * - `NO`: Do not stop playing any audio effect.
 */
@property(nonatomic, assign) BOOL stopAllEffect;

/**
 * Whether to stop microphone recording when a user leaves the channel.
 * - `YES`: (Default) Stop microphone recording.
 * - `NO`: Do not stop microphone recording.
 */
@property(nonatomic, assign) BOOL stopMicrophoneRecording;

@end

/**
 * @brief Configurations of the video frame.
 *
 * @details
 * Note that the buffer provides a pointer to a pointer. This interface cannot modify the pointer of
 * the buffer, but it can modify the content of the buffer.
 *
 */
__attribute__((visibility("default"))) @interface AgoraOutputVideoFrame : NSObject

/**
 * The format of the incoming video frame. This parameter must be specified as one of the following
 * values:
 * - 1: I420
 * - 2: BGRA
 * - 3: NV21
 * - 4: RGBA
 * - 5: IMC2
 * - 7: ARGB
 * - 8: NV12
 * - 12: iOS texture (CVPixelBufferRef)
 * - 16: I422
 */
@property (nonatomic, assign) NSInteger type;
/**
 * The width of the video, in pixels.
 */
@property (nonatomic, assign) int width;
/**
 * The height of the video, in pixels.
 */
@property (nonatomic, assign) int height;
/**
 * For YUV data, the line span of the Y buffer; for RGBA data, the total data length.
 * @note When dealing with video data, it is necessary to process the offset between each line of
 * pixel data based on this parameter, otherwise it may result in image distortion.
 */
@property (nonatomic, assign) int yStride;
/**
 * For YUV data, the line span of the U buffer; for RGBA data, the value is 0.
 * @note When dealing with video data, it is necessary to process the offset between each line of
 * pixel data based on this parameter, otherwise it may result in image distortion.
 */
@property (nonatomic, assign) int uStride;
/**
 * For YUV data, the line span of the V buffer; for RGBA data, the value is 0.
 * @note When dealing with video data, it is necessary to process the offset between each line of
 * pixel data based on this parameter, otherwise it may result in image distortion.
 */
@property (nonatomic, assign) int vStride;
/**
 * For YUV data, the pointer to the Y buffer; for RGBA data, the data buffer.
 */
@property (nonatomic, assign) uint8_t* _Nullable yBuffer;
/**
 * For YUV data, the pointer to the U buffer; for RGBA data, the value is 0.
 */
@property (nonatomic, assign) uint8_t* _Nullable uBuffer;
/**
 * For YUV data, the pointer to the V buffer; for RGBA data, the value is 0.
 */
@property (nonatomic, assign) uint8_t* _Nullable vBuffer;
/**
 * The clockwise rotation of the video frame before rendering. Supported values include 0, 90, 180,
 * and 270 degrees.
 */
@property (nonatomic, assign) int rotation;
/**
 * The Unix timestamp (ms) when the video frame is rendered. This timestamp can be used to guide the
 * rendering of the video frame. This parameter is required.
 */
@property (nonatomic, assign) int64_t renderTimeMs;
/**
 * Reserved for future use.
 */
@property (nonatomic, assign) int avSyncType;

/**
 * Fills the data to CVPixelBuffer.
 */
@property(assign, nonatomic) CVPixelBufferRef _Nullable pixelBuffer;
/**
 * The alpha channel data output by using portrait segmentation algorithm. This data matches the
 * size of the video frame, with each pixel value ranging from [0,255], where 0 represents the
 * background and 255 represents the foreground (portrait).
 * By setting this parameter, you can render the video background into various effects, such as
 * transparent, solid color, image, video, etc.
 * @note - Make sure that `alphaBuf` is exactly the same size as the video frame (width × height),
 * otherwise it may cause the app to crash.
 */
@property (nonatomic, assign) uint8_t* _Nullable alphaBuffer;
/**
 * When the video frame contains alpha channel data, it represents the relative position of
 * `alphaBuffer` and the video frame. See `AgoraAlphaStitchMode`.
 */
@property (nonatomic, assign) AgoraAlphaStitchMode alphaStitchMode;

/**
 * The meta information in the video frame. To use this parameter, contact `technical support`.
 */
@property(nonatomic, strong) NSDictionary *_Nonnull metaInfo;

/**
 * By default, the color space properties of video frames will apply the Full Range and BT.709
 * standard configurations.
 */
@property(nonatomic, strong) AgoraColorSpace* _Nullable colorSpace;

@end

/**
 * @brief Built-in encryption configurations.
 */
__attribute__((visibility("default"))) @interface AgoraEncryptionConfig: NSObject

 /**
  * The built-in encryption mode. See `AgoraEncryptionMode`. Agora recommends using
  * `AgoraEncryptionModeAES128GCM2` or `AgoraEncryptionModeAES256GCM2` encrypted mode. These two
  * modes support the use of salt for higher security.
  */
 @property (assign, nonatomic) AgoraEncryptionMode encryptionMode;

 /**
  * Encryption key in string type with unlimited length. Agora recommends using a 32-byte key.
  * @note If you do not set an encryption key or set it as `nil`, you cannot use the built-in
  * encryption, and the SDK returns `-2`.
  */
 @property (copy, nonatomic) NSString * _Nullable encryptionKey;
 /**
  * Salt, 32 bytes in length. Agora recommends that you use OpenSSL to generate salt on the server
  * side. See Media Stream Encryption for details.
  * @note This parameter takes effect only in `AgoraEncryptionModeAES128GCM2` or
  * `AgoraEncryptionModeAES256GCM2` encrypted mode. In this case, ensure that this parameter is not
  * `0`.
  */
 @property (strong, nonatomic) NSData * _Nullable encryptionKdfSalt;
 /**
  * Whether to enable data stream encryption:
  * - `YES`: Enable data stream encryption.
  * - `NO`: (Default) Disable data stream encryption.
  */
 @property (assign, nonatomic) BOOL  datastreamEncryptionEnabled;
 @end

/**
 * @brief The information of the user.
 */
__attribute__((visibility("default"))) @interface AgoraUserInfo: NSObject

/**
 * The user ID.
 */
@property(assign, nonatomic) NSUInteger uid;

 /**
  * User account.
  */
 @property (copy, nonatomic) NSString * _Nullable userAccount;
 @end

/**
 * @brief Setting of user role properties.
 */
__attribute__((visibility("default"))) @interface AgoraClientRoleOptions: NSObject

/**
 * The latency level of an audience member in interactive live streaming. See
 * `AgoraAudienceLatencyLevelType`.
 */
@property (assign, nonatomic) AgoraAudienceLatencyLevelType audienceLatencyLevel;

@end

/**
 * @brief The camera capturer preference.
 */
__attribute__((visibility("default"))) @interface AgoraCameraCapturerConfiguration: NSObject
/** The camera direction. See AgoraCameraDirection:

 - AgoraCameraDirectionRear: The rear camera.
 - AgoraCameraDirectionFront: The front camera.
 */
#if TARGET_OS_IOS
/**
 * (iOS only and optional) The camera direction. See `AgoraCameraDirection`.
 */
@property (assign, nonatomic) AgoraCameraDirection cameraDirection;
/**
 * (iOS only and optional) The camera focal length type. See `AgoraFocalLength`.
 * @note
 * - For iOS devices equipped with multi-lens rear cameras, such as those featuring dual-camera
 * (wide-angle and ultra-wide-angle) or triple-camera (wide-angle, ultra-wide-angle, and telephoto),
 * you can use one of the following methods to capture video with an ultra-wide-angle perspective:
 *   - Method one: Set this parameter to `AgoraFocalLengthUltraWide` (2) (ultra-wide lens).
 *   - Method two: Set this parameter to `AgoraFocalLengthDefault` (0) (standard lens), then call
 * `setCameraZoomFactor:` to set the camera's zoom factor to a value less than 1.0, with the minimum
 * setting being 0.5.
 * The difference is that the size of the ultra-wide angle in method one is not adjustable, whereas
 * method two supports adjusting the camera's zoom factor freely.
 */
@property(assign, nonatomic) AgoraFocalLength cameraFocalLengthType;
#elif TARGET_OS_MAC
/**
 * (macOS only) The camera ID.
 */
@property (copy, nonatomic) NSString * _Nullable deviceId;
#endif

/**
 * (Optional) The dimension (px) of the video frame. The default value is 960 × 540.
 */
@property(assign, nonatomic) CGSize dimensions;

/**
 * (Optional) The frame rate (fps) of the video frame. The default value is 15.
 */
@property(assign, nonatomic) int frameRate;

/**
 * (Optional) Whether to follow the video aspect ratio set in `setVideoEncoderConfiguration:`:
 * - `YES`: (Default) Follow the set video aspect ratio. The SDK crops the captured video according
 * to the set video aspect ratio and synchronously changes the local preview screen and the video
 * frame in `onCaptureVideoFrame:sourceType:` and `onPreEncodeVideoFrame:sourceType:`.
 * - `NO`: Do not follow the system default audio playback device. The SDK does not change the
 * aspect ratio of the captured video frame.
 */
@property(assign, nonatomic) BOOL followEncodeDimensionRatio;

@end

/**
 * @brief The configurations for the data stream.
 *
 * @details
 * The following table shows the SDK behaviors under different parameter settings:
 * | `syncWithAudio` | `ordered` | SDK behaviors                                                                                                                                                                                                                                                                                                                                                         |
 * | --------------- | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
 * | `NO`            | `NO`      | The SDK triggers the `rtcEngine:receiveStreamMessageFromUid:streamId:data:` callback immediately after the receiver receives a data packet.                                                                                                                                                                                                                           |
 * | `YES`           | `NO`      | If the data packet delay is within the audio delay, the SDK triggers the rtcEngine:receiveStreamMessageFromUid:streamId:data: callback when the synchronized audio packet is played out. If the data packet delay exceeds the audio delay, the SDK triggers the rtcEngine:receiveStreamMessageFromUid:streamId:data: callback as soon as the data packet is received. |
 * | `NO`            | `YES`     | If the delay of a data packet is less than five seconds, the SDK corrects the order of the data packet. If the delay of a data packet exceeds five seconds, the SDK discards the data packet.                                                                                                                                                                         |
 * | `YES`           | `YES`     | If the delay of the data packet is within the range of the audio delay, the SDK corrects the order of the data packet. If the delay of a data packet exceeds the audio delay, the SDK discards this data packet.                                                                                                                                                      |
 *
 */
__attribute__((visibility("default"))) @interface AgoraDataStreamConfig: NSObject

/**
 * Whether the SDK guarantees that the receiver receives the data in the sent order.
 * - `YES`: Guarantee that the receiver receives the data in the sent order.
 * - `NO`: Do not guarantee that the receiver receives the data in the sent order.
 * Do not set this parameter as `YES` if you need the receiver to receive the data packet
 * immediately.
 */
@property (assign, nonatomic) BOOL ordered;

/**
 * Whether to synchronize the data packet with the published audio packet.
 * - `YES`: Synchronize the data packet with the audio packet. This setting is suitable for special
 * scenarios such as lyrics synchronization.
 * - `NO`: Do not synchronize the data packet with the audio packet. This setting is suitable for
 * scenarios where data packets need to arrive at the receiving end immediately.
 * When you set the data packet to synchronize with the audio, then if the data packet delay is
 * within the audio delay, the SDK triggers the
 * `rtcEngine:receiveStreamMessageFromUid:streamId:data:` callback when the synchronized audio
 * packet is played out.
 */
@property (assign, nonatomic) BOOL syncWithAudio;
@end


/**
 * @brief Channel media information.
 */
__attribute__((visibility("default"))) @interface AgoraChannelMediaRelayInfo: NSObject
/**
 * The token that enables the user to join the channel.
 */
@property (copy, nonatomic) NSString * _Nullable token;
/**
 * The channel name.
 */
@property (copy, nonatomic) NSString * _Nullable channelName;
/**
 * The user ID.
 */
@property (assign, nonatomic) NSUInteger uid;
/** Initializes the AgoraChannelMediaRelayInfo object

 @param token The token that enables the user to join the channel.
 */
- (instancetype _Nonnull)initWithToken:(NSString *_Nullable)token NS_SWIFT_NAME(init(token:));
@end

/**
 * @brief Configuration of cross channel media relay.
 */
__attribute__((visibility("default"))) @interface AgoraChannelMediaRelayConfiguration: NSObject
/**
 * The information of the target channel `AgoraChannelMediaRelayInfo`. It contains the following
 * members:
 * - `channelName`: The name of the target channel.
 * - `token`: The `token` for joining the target channel. It is generated with the `channelName` and
 * `uid` you set in `destInfos`.
 *   - If you have not enabled the App Certificate, set this parameter as the default value `nil`,
 * which means the SDK applies the App ID.
 *   - If you have enabled the App Certificate, you must use the `token` generated with the
 * `channelName` and `uid`.
 * - `uid`: The unique user ID to identify the relay stream in the target channel. The value ranges
 * from 0 to (2 32-1). To avoid user ID conflicts, this user ID must be different from any other
 * user ID in the target channel. The default value is 0, which means the SDK generates a random
 * UID.
 * @note If the token of any target channel expires, the whole media relay stops; hence Agora
 * recommends that you specify the same expiration time for the tokens of all the target channels.
 */
@property (strong, nonatomic, readonly) NSDictionary<NSString *, AgoraChannelMediaRelayInfo *> *_Nullable destinationInfos;
/**
 * The information of the source channel. See `AgoraChannelMediaRelayInfo`. It contains the
 * following members:
 * - `channelName`: The name of the source channel. The default value is `nil`, which means the SDK
 * applies the name of the current channel.
 * - `token`: The `token` for joining the source channel. This token is generated with the
 * `channelName` and `uid` you set in `srcInfo`.
 *   - If you have not enabled the App Certificate, set this parameter as the default value `nil`,
 * which means the SDK applies the App ID.
 *   - If you have enabled the App Certificate, you must use the `token` generated with the
 * `channelName` and `uid`, and the `uid` must be set as 0.
 * - `uid`: The unique user ID to identify the relay stream in the source channel. Agora recommends
 * leaving the default value of 0 unchanged.
 */
@property (strong, nonatomic) AgoraChannelMediaRelayInfo *_Nonnull sourceInfo;
/** 
 * @brief Sets the information of the target channel.
 *
 * @param destinationInfo The information of the target channel. See `AgoraChannelMediaRelayInfo`. 
 * It contains the following members:
 * - `channelName`: The name of the destination channel.
 * - `uid`: The unique user ID to identify the relay stream in the target channel. The value ranges from 0 
 * to (2 <sup>32</sup>-1). To avoid user ID conflicts, this user ID must be different from any other user 
 * ID in the target channel. The default value is 0, which means the SDK generates a random UID.
 * - `token`: The token for joining the destination channel. It is generated with the `channelName` and `uid` you set in `destinationInfo`.
 *   - If you have not enabled the App Certificate, set this parameter as the default value `nil`, which means the SDK applies the App ID.
 *   - If you have enabled the App Certificate, you must use the `token` generated with the `channelName` and `uid`.
 *   Attention: If the token of any target channel expires, the whole media relay stops; hence Agora recommends that you specify the 
 * same expiration time for the tokens of all the target channels.
 * @param channelName The channel name of the target channel.
 * 
 * @return 
 * - YES: Success.
 * - NO: Failure.
 */
- (BOOL)setDestinationInfo:(AgoraChannelMediaRelayInfo *_Nonnull)destinationInfo forChannelName:(NSString *_Nonnull)channelName NS_SWIFT_NAME(setDestinationInfo(_:forChannelName:));
/** 
 * @brief Deletes the information of target channel.
 *
 * @param channelName The channel name of the target channel.
 *
 * @return 
 * - YES: Success.
 * - NO: Failure.
 */
- (BOOL)removeDestinationInfoForChannelName:(NSString *_Nonnull)channelName NS_SWIFT_NAME(removeDestinationInfo(forChannelName:));
@end


/**
 * @brief Image enhancement options.
 */
__attribute__((visibility("default"))) @interface AgoraBeautyOptions : NSObject

/**
 * The contrast level, used with the `lighteningLevel` parameter. The larger the value, the greater
 * the contrast between light and dark. See `AgoraLighteningContrastLevel`.
 */
@property(nonatomic, assign) AgoraLighteningContrastLevel lighteningContrastLevel;

/**
 * The brightening level, in the range [0.0,1.0], where 0.0 means the original brightening. The
 * default value is 0.7. The higher the value, the greater the degree of brightening.
 */
@property(nonatomic, assign) float lighteningLevel;

/**
 * The smoothness level, in the range [0.0,1.0], where 0.0 means the original smoothness. The
 * default value is 0.5. The greater the value, the greater the smoothness level.
 */
@property(nonatomic, assign) float smoothnessLevel;

/**
 * The redness level, in the range [0.0,1.0], where 0.0 means the original redness. The default
 * value is 0.1. The larger the value, the greater the redness level.
 */
@property(nonatomic, assign) float rednessLevel;

/**
 * The sharpness level, in the range [0.0,1.0], where 0.0 means the original sharpness. The default
 * value is 0.1. The larger the value, the greater the sharpness level.
 */
@property(nonatomic, assign) float sharpnessLevel;

@end

/**
 * @brief Filter effect options.
 */
__attribute__((visibility("default"))) @interface AgoraFaceShapeAreaOptions : NSObject
/**
 * Facial enhancement areas: `AgoraFaceShapeArea`
 */
@property(nonatomic, assign) AgoraFaceShapeArea shapeArea;
/**
 * The intensity of the enhancement. The definition of enhancement intensity varies according to the
 * different face areas, such as its orientation, range, and preset value. See `AgoraFaceShapeArea`.
 */
@property(nonatomic, assign) int shapeIntensity;

@end

/**
 * @brief The facial enhancement style options.
 */
__attribute__((visibility("default"))) @interface AgoraFaceShapeBeautyOptions: NSObject
/**
 * Facial enhancement style options: `AgoraFaceShapeStyle`.
 */
@property(nonatomic, assign) AgoraFaceShapeStyle shapeStyle;
/**
 * The intensity of the facial enhancement style, with a value range oof [0.0,1.0]. The default
 * value is 0.0, which means no face enhancement effect. The higher the value, the more obvious the
 * facial enhancement effect.
 */
@property(nonatomic, assign) int styleIntensity;

@end

/**
 * @brief Filter effect options.
 */
__attribute__((visibility("default"))) @interface AgoraFilterEffectOptions: NSObject
/**
 * The absolute path to the local cube map texture file, which can be used to customize the filter
 * effect. The specified .cude file should strictly follow the Cube LUT Format Specification;
 * otherwise, the filter options do not take effect. The following is a sample of the .cude file:
 * ```
 * LUT_3D_SIZE 32
 * 0.0039215689 0 0.0039215682
 * 0.0086021447 0.0037950677 0
 * ...
 * 0.0728652592 0.0039215689 0
 * ```
 * @note
 * - The identifier `LUT_3D_SIZE` on the first line of the cube map file represents the size of the
 * three-dimensional lookup table. The LUT size for filter effect can only be set to 32.
 * - The SDK provides a built-in `built_in_whiten_filter.cube` file. You can pass the absolute path
 * of this file to get the whitening filter effect.
 */
@property(nonatomic, copy) NSString* _Nullable path NS_SWIFT_NAME(path);

/**
 * The intensity of the filter effect, with a range value of [0.0,1.0], in which 0.0 represents no
 * filter effect. The default value is 0.5. The higher the value, the stronger the filter effect.
 */
@property(nonatomic, assign) float strength;

@end

/**
 * @brief Video noise reduction options.
 */
__attribute__((visibility("default"))) @interface AgoraVideoDenoiserOptions : NSObject

/**
 * Video noise reduction mode. See `AgoraVideoDenoiserMode`.
 */
@property(nonatomic, assign) AgoraVideoDenoiserMode mode;

/**
 * Video noise reduction level. See `AgoraVideoDenoiserLevel`.
 */
@property(nonatomic, assign) AgoraVideoDenoiserLevel level;

@end

/**
 * @brief The low-light enhancement options.
 */
__attribute__((visibility("default"))) @interface AgoraLowlightEnhanceOptions : NSObject

/**
 * The low-light enhancement mode. See `AgoraLowlightEnhanceMode`.
 */
@property(nonatomic, assign) AgoraLowlightEnhanceMode mode;

/**
 * The low-light enhancement level. See `AgoraLowlightEnhanceLevel`.
 */
@property(nonatomic, assign) AgoraLowlightEnhanceLevel level;

@end

/**
 * @brief The color enhancement options.
 */
__attribute__((visibility("default"))) @interface AgoraColorEnhanceOptions : NSObject

/**
 * The level of color enhancement. The value range is [0.0, 1.0]. `0.0` is the default value, which
 * means no color enhancement is applied to the video. The higher the value, the higher the level of
 * color enhancement. The default value is `0.5`.
 */
@property(nonatomic, assign) float strengthLevel;

/**
 * The level of skin tone protection. The value range is [0.0, 1.0]. `0.0` means no skin tone
 * protection. The higher the value, the higher the level of skin tone protection. The default value
 * is `1.0`.
 * - When the level of color enhancement is higher, the portrait skin tone can be significantly
 * distorted, so you need to set the level of skin tone protection.
 * - When the level of skin tone protection is higher, the color enhancement effect can be slightly
 * reduced.
 * Therefore, to get the best color enhancement effect, Agora recommends that you adjust
 * `strengthLevel` and `skinProtectLevel` to get the most appropriate values.
 */
@property(nonatomic, assign) float skinProtectLevel;

@end


/**
 * @brief The custom background.
 */
__attribute__((visibility("default"))) @interface AgoraVirtualBackgroundSource : NSObject

/**
 * The custom background. See `AgoraVirtualBackgroundSourceType`.
 */
@property(nonatomic, assign) AgoraVirtualBackgroundSourceType backgroundSourceType NS_SWIFT_NAME(backgroundSourceType);

/**
 * The type of the custom background image. The color of the custom background image. The format is
 * a hexadecimal integer defined by RGB, without the # sign, such as 0xFFB6C1 for light pink. The
 * default value is 0xFFFFFF, which signifies white. The value range is [0x000000, 0xffffff]. If the
 * value is invalid, the SDK replaces the original background image with a white background image.
 * @note
 * This parameter is only applicable to custom backgrounds of the following types:
 * - AgoraVirtualBackgroundColor: The background image is a solid-colored image of the color passed
 * in by the parameter.
 * - AgoraVirtualBackgroundImg: If the image in `source` has a transparent background, the
 * transparent background will be filled with the color passed in by the parameter.
 */
@property(nonatomic, assign) NSUInteger color NS_SWIFT_NAME(color);

/**
 * The local absolute path of the custom background image. Supports PNG, JPG, MP4, AVI, MKV, and FLV
 * formats. If the path is invalid, the SDK will use either the original background image or the
 * solid color image specified by `color`.
 * @note This parameter takes effect only when the type of the custom background image is
 * AgoraVirtualBackgroundImg or AgoraVirtualBackgroundVideo.
 */
@property(nonatomic, copy) NSString* _Nullable source NS_SWIFT_NAME(source);

/**
 * The degree of blurring applied to the custom background image. See `AgoraBlurDegree`.
 * @note This parameter takes effect only when the type of the custom background image is
 * AgoraVirtualBackgroundBlur.
 */
@property(nonatomic, assign) AgoraBlurDegree blurDegree;

@end

/**
 * @brief Processing properties for background images.
 */
__attribute__((visibility("default"))) @interface AgoraSegmentationProperty: NSObject

/**
 * The type of algorithms to user for background processing. See `SegModelType`.
 */
@property(nonatomic, assign) SegModelType modelType;

/**
 * The accuracy range for recognizing background colors in the image. The value range is [0,1], and
 * the default value is 0.5. The larger the value, the wider the range of identifiable shades of
 * pure color. When the value of this parameter is too large, the edge of the portrait and the pure
 * color in the portrait range are also detected. Agora recommends that you dynamically adjust the
 * value of this parameter according to the actual effect.
 * @note This parameter only takes effect when `modelType` is set to `SegModelAgoraGreen`.
 */
@property(nonatomic, assign) float greenCapacity;

/**
 * The screen color. See `ScreenColorType`.
 */
@property(nonatomic, assign) ScreenColorType screenColorType;

@end
/**
 * @brief The video streams for local video mixing.
 */
__attribute__((visibility("default"))) @interface AgoraTranscodingVideoStream: NSObject
/**
 * The user ID of the remote user.
 * @note Use this parameter only when the source type is `AgoraVideoSourceTypeRemote` for local
 * video mixing.
 */
@property (assign, nonatomic) NSUInteger remoteUserUid;

/**
 * The video source type for local video mixing. See `AgoraVideoSourceType`.
 */
@property (assign, nonatomic) AgoraVideoSourceType sourceType;
/**
 * The file path of local images.
 * Examples:
 * - iOS: `/var/mobile/Containers/Data/Application/<APP-UUID>/Documents/image.png`
 * - macOS: `~/Pictures/image.png`
 * @note Use this parameter only when the source type is the image for local video mixing.
 */
@property (copy, nonatomic) NSString * _Nullable imageUrl;
/**
 * (Optional) Media player ID. Use the parameter only when you set `sourceType` to
 * `AgoraVideoSourceTypeMediaPlayer`.
 */
@property(assign, nonatomic) NSUInteger mediaPlayerId;
/**
 * The relative position and size of the video for local video mixing to the origin (the top left
 * corner of the canvas). The data type is CGRect.
 */
@property (assign, nonatomic) CGRect rect;
/**
 * The number of the layer to which the video for the local video mixing belongs. The value range is
 * [0, 100].
 * - 0: (Default) The layer is at the bottom.
 * - 100: The layer is at the top.
 */
@property (assign, nonatomic) NSInteger zOrder;
/**
 * The transparency of the video for local video mixing. The value range is [0.0, 1.0]. 0.0
 * indicates that the video is completely transparent, and 1.0 indicates that it is opaque.
 */
@property(assign, nonatomic) double alpha;
/**
 * Whether to mirror the video for the local video mixing.
 * - `YES`: Mirror the video for the local video mixing.
 * - `NO`: (Default) Do not mirror the video for the local video mixing.
 * @note This parameter only takes effect on video source types that are cameras.
 */
@property(assign, nonatomic) BOOL mirror;

@end

/**
 * @brief The source of the audio streams that are mixed locally.
 */
__attribute__((visibility("default"))) @interface AgoraMixedAudioStream: NSObject
/**
 * The type of the audio source. See `AgoraAudioSourceType`.
 */
@property (assign, nonatomic) AgoraAudioSourceType sourceType;
/**
 * The user ID of the remote user.
 * @note Set this parameter if the source type of the locally mixed audio steams is
 * AgoraAudioSourceRemoteUser.
 */
@property (assign, nonatomic) NSUInteger remoteUserUid;
/**
 * The channel name. This parameter signifies the channel in which users engage in real-time audio
 * and video interaction. Under the premise of the same App ID, users who fill in the same channel
 * ID enter the same channel for audio and video interaction. The string length must be less than 64
 * bytes. Supported characters (89 characters in total):
 * - All lowercase English letters: a to z.
 * - All uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]",
 * "^", "_", "{", "}", "|", "~", ","
 * @note Set this parameter if the source type of the locally mixed audio streams is
 * AgoraAudioSourceRemoteChannel or AgoraAudioSourceRemoteUser.
 */
@property (copy, nonatomic) NSString * _Nullable channelId;
/**
 * The audio track ID. Set this parameter to the custom audio track ID returned in
 * `createCustomAudioTrack:config:`.
 * @note Set this parameter if the source type of the locally mixed audio steams is
 * AgoraAudioSourceCustom.
 */
@property (assign, nonatomic) NSUInteger trackId;

@end

/**
 * @brief The configurations for mixing the local audio.
 */
__attribute__((visibility("default"))) @interface AgoraLocalAudioMixerConfiguration: NSObject

/**
 * The source of the audio streams that are mixed locally. See `AgoraMixedAudioStream`.
 */
@property(copy, nonatomic) NSArray<AgoraMixedAudioStream *> *_Nullable audioInputStreams;

/**
 * Whether the mxied audio stream uses the timestamp of the audio frames captured by the local
 * microphone.
 * - `YES`: (Default) Yes. Set to this value if you want all locally captured audio streams
 * synchronized.
 * - `NO`: No. The SDK uses the timestamp of the audio frames at the time when they are mixed.
 */
@property(assign, nonatomic) BOOL syncWithLocalMic;

@end

/**
 * @brief The configuration of the video mixing on the local client.
 */
__attribute__((visibility("default"))) @interface AgoraLocalTranscoderConfiguration: NSObject

/**
 * The video streams for local video mixing. See `AgoraTranscodingVideoStream`.
 */
@property(copy, nonatomic) NSArray<AgoraTranscodingVideoStream *> *_Nullable videoInputStreams;

/**
 * The encoding configuration of the mixed video stream after the local video mixing. See
 * `AgoraVideoEncoderConfiguration`.
 */
@property (strong, nonatomic) AgoraVideoEncoderConfiguration *_Nonnull videoOutputConfiguration;

/**
 * Whether to use the timestamp when the primary camera captures the video frame as the timestamp of the mixed video frame.
 * - true: (Default) Use the timestamp of the captured video frame as the timestamp of the mixed video frame.
 * - false: Do not use the timestamp of the captured video frame as the timestamp of the mixed video frame. use the timestamp when the mixed video frame is constructed Instead.
 */
@property(assign, nonatomic) BOOL syncWithPrimaryCamera;

@end

/**
 * @brief Screen sharing configurations.
 */
__attribute__((visibility("default"))) @interface AgoraScreenCaptureParameters: NSObject
/**
 * The video encoding resolution of the screen sharing stream.
 * If the screen dimensions are different from the value of this parameter, Agora applies the
 * following strategies for encoding. Suppose `dimensions` is set to 1920 × 1080:
 * - If the value of the screen dimensions is lower than that of `dimensions`, for example, 1000 ×
 * 1000 pixels, the SDK uses the screen dimensions, that is, 1000 × 1000 pixels, for encoding.
 * - If the value of the screen dimensions is higher than that of `dimensions`, for example, 2000 ×
 * 1500, the SDK uses the maximum value under `dimensions` with the aspect ratio of the screen
 * dimension (4:3) for encoding, that is, 1440 × 1080.
 * @note
 * When setting the encoding resolution in the scenario of sharing documents (
 * AgoraScreenScenarioDocument ), choose one of the following two methods:
 * - If you require the best image quality, it is recommended to set the encoding resolution to be
 * the same as the capture resolution.
 * - If you wish to achieve a relative balance between image quality, bandwidth, and system
 * performance, then:
 *   - When the capture resolution is greater than 1920 × 1080, it is recommended that the encoding
 * resolution is not less than 1920 × 1080.
 *   - When the capture resolution is less than 1920 × 1080, it is recommended that the encoding
 * resolution is not less than 1280 × 720.
 */
@property (assign, nonatomic) CGSize dimensions;
/**
 * The frame rate (fps) of the shared region. The default value is 5. Agora does not recommend
 * setting this to a value greater than 15.
 */
@property (assign, nonatomic) NSInteger frameRate;
/**
 * The bitrate (Kbps) of the shared region. The default value is 0 (the SDK works out a bitrate
 * according to the dimensions of the current screen).
 */
@property (assign, nonatomic) NSInteger bitrate;

/**
 * Whether to capture the mouse in screen sharing:
 * - `YES`: (Default) Capture the mouse.
 * - `NO`: Do not capture the mouse.
 * @note Due to macOS system restrictions, setting this parameter to `NO` is ineffective during
 * screen sharing (it has no impact when sharing a window).
 */
@property(assign, nonatomic) BOOL captureMouseCursor;

/**
 * Whether to bring the window to the front when calling the
 * `startScreenCaptureByWindowId:regionRect:captureParams:` method to share it:
 * - `YES`: Bring the window to the front.
 * - `NO`: (Default) Do not bring the window to the front.
 * @note Due to macOS system limitations, when setting this member to bring the window to the front,
 * if the current app has multiple windows, only the main window will be brought to the front.
 */
@property(assign, nonatomic) BOOL windowFocus;

/**
 * The ID list of the windows to be blocked. When calling
 * `startScreenCaptureByDisplayId:regionRect:captureParams:` to start screen sharing, you can use
 * this parameter to block a specified window. When calling `updateScreenCaptureParameters:` to
 * update screen sharing configurations, you can use this parameter to dynamically block a specified
 * window.
 */
@property(copy, nonatomic) NSArray* _Nullable excludeWindowList;

/**
 * Whether to place a border around the shared window or screen:
 * - `YES`: Place a border.
 * - `NO`: (Default) Do not place a border.
 * @note When you share a part of a window or screen, the SDK places a border around the entire
 * window or screen if you set this parameter to `YES`.
 */
@property(assign, nonatomic) BOOL highLighted;
/**
 * On macOS, `COLOR_CLASS` refers to `NSColor`.
 */
@property(strong, nonatomic) COLOR_CLASS* _Nullable highLightColor;
/**
 * The width (px) of the border. The default value is 5, and the value range is (0, 50].
 * @note This parameter only takes effect when `highLighted` is set to `YES`.
 */
@property(assign, nonatomic) NSUInteger highLightWidth;

@property(assign, nonatomic) BOOL captureAudio;

@end

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
/**
 * @brief The configuration of the captured screen.
 */
__attribute__((visibility("default"))) @interface AgoraScreenCaptureConfiguration: NSObject
/**
 * Whether to capture the window on the screen:
 * - `YES`: Capture the window.
 * - `NO`: (Default) Capture the screen, not the window.
 */
@property(assign, nonatomic) BOOL isCaptureWindow;
/**
 *  The display ID of the screen.
 * @note This parameter takes effect only when you want to capture the screen on macOS.
 */
@property(assign, nonatomic) UInt32 displayId;
/**
 *  Window ID.
 * @note This parameter takes effect only when you want to capture the window.
 */
@property(assign, nonatomic) UInt32 windowId;

/**
 * The screen capture configuration. See `AgoraScreenCaptureParameters`.
 */
@property(strong, nonatomic) AgoraScreenCaptureParameters* _Nonnull params;
/**
 * The relative position of the shared region to the whole screen. The data type is CGRect.
 */
@property(assign, nonatomic) CGRect regionRect;

@end
#endif

/**
 * @brief The video configuration for the shared screen stream.
 */
__attribute__((visibility("default"))) @interface AgoraScreenVideoParameters : NSObject

/**
 * The video encoding dimension. The default value is 1280 × 720.
 */
@property(assign, nonatomic) CGSize dimensions;
/**
 * The video encoding frame rate (fps). The default value is 15.
 */
@property(assign, nonatomic) NSInteger frameRate;
/**
 * The video encoding bitrate (Kbps).
 */
@property(assign, nonatomic) NSInteger bitrate;

/**
 * The content hint for screen sharing. See `AgoraVideoContentHint`.
 */
@property(assign, nonatomic) AgoraVideoContentHint contentHint;

@end

/**
 * @brief The audio configuration for the shared screen stream.
 *
 * @details
 * Only available where `captureAudio` is `YES`.
 *
 */
__attribute__((visibility("default"))) @interface AgoraScreenAudioParameters : NSObject

/**
 * The volume of the captured system audio. The value range is [0, 100]. The default value is 100.
 */
@property(assign, nonatomic) NSInteger captureSignalVolume;

@end

/**
 * @brief Screen sharing configurations.
 */
__attribute__((visibility("default"))) @interface AgoraScreenCaptureParameters2 : NSObject
/**
 * Determines whether to capture system audio during screen sharing:
 * - `YES`: Capture system audio.
 * - `NO`: (Default) Do not capture system audio.
 * @note - To improve the success rate of capturing system audio during screen sharing, ensure that
 * you have called the `setAudioScenario:` method and set the audio scenario to
 * `AgoraAudioScenarioGameStreaming`.
 */
@property(assign, nonatomic) BOOL captureAudio;
/**
 * The audio configuration for the shared screen stream. See `AgoraScreenAudioParameters`.
 * @note This parameter only takes effect when `captureAudio` is `YES`.
 */
@property(strong, nonatomic) AgoraScreenAudioParameters* _Nonnull audioParams;
/**
 * Whether to capture the screen when screen sharing:
 * - `YES`: (Default) Capture the screen.
 * - `NO`: Do not capture the screen.
 */
@property(assign, nonatomic) BOOL captureVideo;
/**
 * The video configuration for the shared screen stream. See `AgoraScreenVideoParameters`.
 * @note This parameter only takes effect when `captureVideo` is `YES`.
 */
@property(strong, nonatomic) AgoraScreenVideoParameters* _Nonnull videoParams;
@end

/**
 * @brief Recording configurations.
 */
__attribute__((visibility("default"))) @interface AgoraAudioRecordingConfiguration: NSObject
/**
 * The absolute path (including the filename extensions) of the recording file. For example:
 * `/var/mobile/Containers/Data/audio.aac`.
 * @note Ensure that the directory for the log files exists and is writable.
 */
@property (copy, nonatomic) NSString * _Nullable filePath;
/**
 * Recording sample rate (Hz).
 * - 16000
 * - (Default) 32000
 * - 44100
 * - 48000
 * @note If you set this parameter to 44100 or 48000, Agora recommends recording WAV files, or AAC
 * files with `quality` set as AgoraAudioRecordingQualityMedium or AgoraAudioRecordingQualityHigh
 * for better recording quality.
 */
@property (assign, nonatomic) NSUInteger sampleRate;
/**
 * The recording content. See `AgoraAudioFileRecordingType`.
 */
@property (assign, nonatomic) AgoraAudioFileRecordingType fileRecordOption;
/**
 * Recording quality. See `AgoraAudioRecordingQuality`.
 * @note This parameter applies to AAC files only.
 */
@property (assign, nonatomic) AgoraAudioRecordingQuality quality;

/**
 * The audio channel of recording: The parameter supports the following values:
 * - 1: (Default) Mono.
 * - 2: Stereo.
 * @note
 * The actual recorded audio channel is related to the audio channel that you capture.
 * - If the captured audio is mono and `recordingChannel` is `2`, the recorded audio is the
 * dual-channel data that is copied from mono data, not stereo.
 * - If the captured audio is dual channel and `recordingChannel` is `1`, the recorded audio is the
 * mono data that is mixed by dual-channel data.
 * The integration scheme also affects the final recorded audio channel. If you need to record in
 * stereo, contact `technical support`.
 */
@property (assign, nonatomic) NSInteger recordingChannel;

@end

/**
 * @brief The configuration of the low-quality video stream.
 */
__attribute__((visibility("default"))) @interface AgoraSimulcastStreamConfig: NSObject

 /**
  * Video bitrate (Kbps). The default value is -1. This parameter does not need to be set. The SDK
  * automatically matches the most suitable bitrate based on the video resolution and frame rate you
  * set.
  */
 @property (assign, nonatomic) int kBitrate;
 /**
  * The frame rate (fps) of the local video. The default value is 5.
  */
 @property (assign, nonatomic) int framerate;
 /**
  * The video dimension. The default value is 50% of the high-quality video stream.
  */
 @property (assign, nonatomic) CGSize dimensions;
 @end

/**
 * @brief Configures the parameters of a specific layer in multi-quality video streams.
 *
 * @since v4.6.0
 *
 * @details
 * Used to configure the resolution, frame rate, and enable status of a specific layer in
 * multi-quality video streams.
 *
 */
__attribute__((visibility("default"))) @interface AgoraStreamLayerConfig: NSObject
 /**
  * Video frame size. Default is 0.
  */
 @property (assign, nonatomic) CGSize dimensions;
 /**
  * Frame rate (fps) of the local video capture. Default is 0.
  */
 @property (assign, nonatomic) int framerate;
 /**
  * Whether to enable the video stream for the corresponding layer.
  * - `YES`: Enables the video stream for the corresponding layer.
  * - `NO`: (Default) Disables the video stream for the corresponding layer.
  */
 @property (assign, nonatomic) BOOL enable;
@end

/**
 * @brief Configure video streams of different quality levels.
 *
 * @since v4.6.0
 */
__attribute__((visibility("default"))) @interface AgoraSimulcastConfig: NSObject
 /**
  * Configurations for multi-layer streaming: `AgoraStreamLayerConfig`.
  */
 @property (copy, nonatomic, readonly) NSArray<AgoraStreamLayerConfig*>* _Nonnull configs;
 /**
  * Whether to enable fallback publishing:
  * - `YES`: Enable fallback publishing. When the device performance or network is poor at the
  * publishing end, the SDK will dynamically disable multiple video streams of different quality
  * levels, from layer1 to layer6. At least the video streams of the highest and lowest quality are
  * retained to maintain basic video continuity.
  * - `NO`: (Default) Disable fallback publishing.
  */
 @property (assign, nonatomic) BOOL publishFallbackEnable;
@end

/**
 * @brief The detailed information of the media stream.
 */
__attribute__((visibility("default"))) @interface AgoraRtcMediaStreamInfo : NSObject
/**
 * The index of the media stream.
 */
@property(nonatomic, assign) NSInteger streamIndex;
/**
 * The type of the media stream. See `AgoraMediaStreamType`.
 */
@property(nonatomic, assign) AgoraMediaStreamType streamType;
/**
 * The codec of the media stream.
 */
@property(nonatomic, copy) NSString *_Nonnull codecName;
/**
 * The language of the media stream.
 */
@property(nonatomic, copy) NSString *_Nullable language;
/**
 * This parameter only takes effect for video streams, and indicates the video frame rate (fps).
 */
@property(nonatomic, assign) NSInteger videoFrameRate;
/**
 * This parameter only takes effect for video streams, and indicates the video bitrate (bps).
 */
@property(nonatomic, assign) NSInteger videoBitRate;
/**
 * This parameter only takes effect for video streams, and indicates the video width (pixel).
 */
@property(nonatomic, assign) NSInteger videoWidth;
/**
 * This parameter only takes effect for video streams, and indicates the video height (pixel).
 */
@property(nonatomic, assign) NSInteger videoHeight;
/**
 * This parameter only takes effect for audio streams, and indicates the audio sample rate (Hz).
 */
@property(nonatomic, assign) NSInteger audioSampleRate;
/**
 * This parameter only takes effect for audio streams, and indicates the audio channel number.
 */
@property(nonatomic, assign) NSInteger audioChannels;
/**
 * The total duration (ms) of the media stream.
 */
@property(nonatomic, assign) NSInteger duration;
/**
 * This parameter only takes effect for video streams, and indicates the video rotation angle.
 */
@property(nonatomic, assign) NSInteger rotation;

@end

/**
 * @brief Audio spectrum information of the remote user.
 */
__attribute__((visibility("default"))) @interface AgoraAudioSpectrumInfo : NSObject

/**
 * The user ID of the remote user.
 */
@property(nonatomic, assign) NSInteger uid;
/**
 * Audio spectrum information of the remote user.
 */
@property(nonatomic, strong) NSArray<NSNumber *> * _Nullable audioSpectrumData;

@end

/**
 * @brief Observer settings for the encoded audio.
 */
__attribute__((visibility("default"))) @interface AgoraAudioEncodedFrameDelegateConfig: NSObject
/**
 * Audio observer position. See `AgoraAudioEncodedFrameDelegatePosition`.
 */
@property (assign, nonatomic) AgoraAudioEncodedFrameDelegatePosition postionType;
/**
 * Audio encoding type. See `AgoraAudioEncodingType`.
 */
@property (assign, nonatomic) AgoraAudioEncodingType encodingType;
@end

/**
 * @brief `AgoraContentInspectModule` class, a structure used to configure the frequency of video
 * screenshot and upload.
 */
__attribute__((visibility("default"))) @interface AgoraContentInspectModule: NSObject
/**
 * Types of functional module. See `AgoraContentInspectType`.
 */
@property (assign, nonatomic) AgoraContentInspectType type;

/**
 * The frequency (s) of video screenshot and upload. The value should be set as larger than 0. The
 * default value is 0, the SDK does not take screenshots. Agora recommends that you set the value as
 * 10; you can also adjust it according to your business needs.
 */
@property (assign, nonatomic) NSInteger interval;

/**
 * The position of the video observer. See `AgoraVideoModulePosition`.
 */
@property (assign, nonatomic) AgoraVideoModulePosition position;
@end

/**
 * @brief Screenshot and upload configuration.
 */
__attribute__((visibility("default"))) @interface AgoraContentInspectConfig: NSObject
/**
 * Additional information on the video content (maximum length: 1024 Bytes).
 * The SDK sends the screenshots and additional information on the video content to the Agora
 * server. Once the video screenshot and upload process is completed, the Agora server sends the
 * additional information and the callback notification to your server.
 */
@property (nonatomic, copy) NSString* _Nullable extraInfo;
/**
 * (Optional) Server configuration related to uploading video screenshots via extensions from Agora
 * Extensions Marketplace. This parameter only takes effect when `type` in
 * `AgoraContentInspectModule` is set to `AgoraContentInspectTypeImageModeration`. If you want to
 * use it, contact `technical support`.
 */
@property (nonatomic, copy) NSString* _Nullable serverConfig;
/**
 * Functional module. See `AgoraContentInspectModule`.
 * A maximum of 32 `AgoraContentInspectModule` instances can be configured, and the value range of
 * `MAX_CONTENT_INSPECT_MODULE_COUNT` is an integer in [1,32].
 * @note A function module can only be configured with one instance at most. Currently only the
 * video screenshot and upload function is supported.
 */
@property(copy, nonatomic) NSArray<AgoraContentInspectModule*>* _Nullable modules;
@end

/**
 * @brief The snapshot configuration.
 */
__attribute__((visibility("default"))) @interface AgoraSnapshotConfig: NSObject
/**
 * The local path (including filename extensions) of the snapshot. For example:
 * - iOS: `/App Sandbox/Library/Caches/example.jpg`
 * @note Ensure that the path you specify exists and is writable.
 */
@property (copy, nonatomic) NSString * _Nullable filePath;
/**
 * The position of the snapshot video frame in the video pipeline. See `AgoraVideoModulePosition`.
 */
@property(assign, nonatomic) AgoraVideoModulePosition position;
@end

/**
 * @brief Contains connection information.
 */
__attribute__((visibility("default"))) @interface AgoraRtcConnection: NSObject <NSCopying>

/**
 * Init AgoraRtcConnection with channelId and localUid
 *
 * @param channelId Unique channel name for the AgoraRTC session in the string.
 * @param localUid Local User ID. A 32-bit unsigned integer.
 * @return AgoraRtcConnection.
 */
- (instancetype _Nonnull)initWithChannelId:(NSString *_Nonnull)channelId localUid:(NSInteger)localUid;

/**
 * The channel name.
 */
@property (nonatomic, copy) NSString *_Nonnull channelId;

/**
 * The ID of the local user.
 */
@property (nonatomic, assign) NSUInteger localUid;

@end

/**
 * @brief Video subscription options.
 */
__attribute__((visibility("default"))) @interface AgoraVideoSubscriptionOptions: NSObject

/**
 * The video stream type that you want to subscribe to. The default value is
 * AgoraVideoStreamTypeHigh, indicating that the high-quality video streams are subscribed. See
 * `AgoraVideoStreamType`.
 */
@property (nonatomic, assign) AgoraVideoStreamType type;
/**
 * Whether to subscribe to encoded video frames only:
 * - `YES`: Subscribe to the encoded video data (structured data) only; the SDK does not decode or
 * render raw video data.
 * - `NO`: (Default) Subscribe to both raw video data and encoded video data.
 */
@property (nonatomic, assign) BOOL encodedFrameOnly;

@end

/**
 * @brief Information about the audio and video streams to be recorded.
 */
__attribute__((visibility("default"))) @interface AgoraRecorderStreamInfo: NSObject
/**
 * The user ID of the remote user to be recorded.
 */
@property (nonatomic, nonatomic) NSUInteger uid;

/**
 * Name of the destination channel to be recorded.
 */
@property (nonatomic, copy) NSString *_Nonnull channelId;
/**
 * The type of video stream to be recorded. See `AgoraRecorderStreamType` for details.
 */
@property (assign, nonatomic) AgoraRecorderStreamType type;
@end

/**
 * @brief Information about externally encoded video frames.
 */
__attribute__((visibility("default"))) @interface AgoraEncodedVideoFrameInfo: NSObject

/**
 * The codec type of the local video stream. See `AgoraVideoCodecType`. The default value is
 * `AgoraVideoCodecTypeH264 (2)`.
 */
@property (assign, nonatomic) AgoraVideoCodecType codecType;
/**
 * Width (pixel) of the video frame.
 */
@property (assign, nonatomic) NSInteger width;
/**
 * Height (pixel) of the video frame.
 */
@property (assign, nonatomic) NSInteger height;
/**
 * The number of video frames per second.
 * When this parameter is not `0`, you can use it to calculate the Unix timestamp of externally
 * encoded video frames.
 */
@property (assign, nonatomic) NSInteger framesPerSecond;
/**
 * The video frame type. See `AgoraVideoFrameType`.
 */
@property (assign, nonatomic) AgoraVideoFrameType frameType;
/**
 * The rotation information of the video frame. See `AgoraVideoRotation`.
 */
@property (assign, nonatomic) NSInteger rotation;
/**
 * Reserved for future use.
 */
@property (assign, nonatomic) NSInteger trackId;  // This can be reserved for multiple video tracks, we need to create different ssrc
              // and additional payload for later implementation.
/**
 * The Unix timestamp (ms) for capturing the external encoded video frames.
 */
@property (assign, nonatomic) NSInteger captureTimeMs;
/**
  * This is a output parameter which means the timestamp for decoding the video.
  */
@property (assign, nonatomic) NSInteger decodeTimeMs;
/**
 * The type of video streams. See `AgoraVideoStreamType`.
 */
@property (assign, nonatomic) AgoraVideoStreamType streamType;
@end

/**
 * @brief Configuration information of the log server.
 */
NS_SWIFT_NAME(LogUploadServerInfo) __attribute__((visibility("default"))) @interface LogUploadServerInfo : NSObject
/**
 * The domain name of the log server.
 */
@property(copy, nonatomic) NSString* _Nullable serverDomain;
/**
 * The storage path of logs on the server.
 */
@property(copy, nonatomic) NSString* _Nullable serverPath;
/**
 * The port of the log server.
 */
@property(assign, nonatomic) NSInteger serverPort;
/**
 * Whether the log server uses the HTTPS protocol:
 * - `YES`: Uses the HTTPS protocol.
 * - `NO`: Uses the HTTP protocol.
 */
@property(assign, nonatomic) BOOL serverHttps;
@end

/**
 * @brief Advanced options for the Local Access Point.
 */
NS_SWIFT_NAME(AdvancedConfigInfo) __attribute__((visibility("default"))) @interface AdvancedConfigInfo : NSObject
/**
 * Custom log upload server. By default, the SDK uploads logs to the Agora log server. You can use
 * this parameter to change the log upload server. See `LogUploadServerInfo`.
 */
@property(strong, nonatomic) LogUploadServerInfo* _Nullable logUploadServer;
@end

/**
 * @brief Local Access Point configuration.
 */
NS_SWIFT_NAME(AgoraLocalAccessPointConfiguration) __attribute__((visibility("default"))) @interface AgoraLocalAccessPointConfiguration : NSObject
/**
 * The internal IP address list of the Local Access Point. Either `ipList` or `domainList` must be
 * provided.
 */
@property(copy, nonatomic) NSArray* _Nullable ipList NS_SWIFT_NAME(ipList);
/**
 * The domain name list of the Local Access Point. The SDK resolves the IP addresses of the Local
 * Access Point based on the domain names you provide. The domain name resolution timeout is 10
 * seconds. You must specify at least one of `ipList` or `domainList`. If you specify both IP
 * addresses and domain names, the SDK merges and deduplicates the IP addresses resolved from the
 * domain names and the ones you provide, then randomly connects to one IP address to achieve load
 * balancing.
 */
@property(copy, nonatomic) NSArray* _Nullable domainList NS_SWIFT_NAME(domainList);
/**
 * Intranet certificate verification domain. If the value is empty, the SDK uses the default
 * certificate verification domain `secure-edge.local`.
 */
@property(copy, nonatomic) NSString* _Nullable verifyDomainName NS_SWIFT_NAME(verifyDomainName);
/**
 * Connection mode. See `AgoraLocalProxyMode` for details.
 */
@property(assign, nonatomic) AgoraLocalProxyMode mode NS_SWIFT_NAME(mode);
/**
 * Advanced options for the Local Access Point. See `AdvancedConfigInfo` for details.
 */
@property(strong, nonatomic) AdvancedConfigInfo* _Nullable advancedConfig NS_SWIFT_NAME(advancedConfig);
/**
  * Whether to disable vos-aut:
  - true: (Default)disable vos-aut.
  - false: not disable vos-aut
*/
@property(assign, nonatomic) BOOL disableAut;
@end

/**
 * @brief The metronome configuration.
 */
__attribute__((visibility("default"))) @interface AgoraRhythmPlayerConfig: NSObject
/**
 * The number of beats per measure, which ranges from 1 to 9. The default value is 4, which means
 * that each measure contains one downbeat and three upbeats.
 */
@property (assign, nonatomic) int beatsPerMeasure;
/**
 * The beat speed (beats/minute), which ranges from 60 to 360. The default value is 60, which means
 * that the metronome plays 60 beats in one minute.
 */
@property (assign, nonatomic) int beatsPerMinute;

@end

/**
 * @brief The information of the detected human face.
 */
NS_SWIFT_NAME(AgoraFacePositionInfo) __attribute__((visibility("default"))) @interface AgoraFacePositionInfo : NSObject

/**
 * The x-coordinate (px) of the human face in the local video. The horizontal position relative to
 * the origin, where the upper left corner of the captured video is the origin, and the x-coordinate
 * is the upper left corner of the watermark.
 */
@property(assign, nonatomic) NSInteger x NS_SWIFT_NAME(x);

/**
 * The y-coordinate (px) of the human face in the local video. Taking the top left corner of the
 * captured video as the origin, the y coordinate represents the relative longitudinal displacement
 * of the top left corner of the human face to the origin.
 */
@property(assign, nonatomic) NSInteger y NS_SWIFT_NAME(y);

/**
 * The width (px) of the human face in the captured video.
 */
@property(assign, nonatomic) NSInteger width NS_SWIFT_NAME(width);

/**
 * The height (px) of the human face in the captured video.
 */
@property(assign, nonatomic) NSInteger height NS_SWIFT_NAME(height);

/**
 * The distance between the human face and the device screen (cm).
 */
@property(assign, nonatomic) NSInteger distance NS_SWIFT_NAME(distance);
@end

/**
 * @brief The advanced options for audio.
 */
__attribute__((visibility("default"))) @interface AgoraAdvancedAudioOptions: NSObject

/**
 * The number of channels for audio preprocessing. See `AgoraAudioProcessChannels`.
 */
@property(assign, nonatomic) AgoraAudioProcessChannels audioProcessingChannels;

@end

/**
 * @brief Image configurations.
 */
__attribute__((visibility("default"))) @interface AgoraImageTrackOptions : NSObject
/**
 * The image URL. Supported formats of images include JPEG, JPG, PNG and GIF. This method supports
 * adding an image from the local absolute or relative file path.
 */
@property(copy, nonatomic) NSString *_Nullable imageUrl;
/**
 * The frame rate of the video streams being published. The value range is [1,30]. The default value
 * is 1.
 */
@property(assign, nonatomic) int fps;
@property(assign, nonatomic) AgoraVideoMirrorMode mirrorMode;
@end

/**
 * @brief The spatial audio parameters.
 */
__attribute__((visibility("default"))) @interface AgoraSpatialAudioParams : NSObject
/**
 * The azimuth angle of the remote user or media player relative to the local user. The value range
 * is [0,360], and the unit is degrees, The values are as follows:
 * - 0: (Default) 0 degrees, which means directly in front on the horizontal plane.
 * - 90: 90 degrees, which means directly to the left on the horizontal plane.
 * - 180: 180 degrees, which means directly behind on the horizontal plane.
 * - 270: 270 degrees, which means directly to the right on the horizontal plane.
 * - 360: 360 degrees, which means directly in front on the horizontal plane.
 */
@property(assign, nonatomic) double speaker_azimuth;
/**
 * The elevation angle of the remote user or media player relative to the local user. The value
 * range is [-90,90], and the unit is degrees, The values are as follows:
 * - 0: (Default) 0 degrees, which means that the horizontal plane is not rotated.
 * - -90: -90 degrees, which means that the horizontal plane is rotated 90 degrees downwards.
 * - 90: 90 degrees, which means that the horizontal plane is rotated 90 degrees upwards.
 */
@property(assign, nonatomic) double speaker_elevation;
/**
 * The distance of the remote user or media player relative to the local user. The value range is
 * [1,50], and the unit is meters. The default value is 1 meter.
 */
@property(assign, nonatomic) double speaker_distance;
/**
 * The orientation of the remote user or media player relative to the local user. The value range is
 * [0,180], and the unit is degrees, The values are as follows:
 * - 0: (Default) 0 degrees, which means that the sound source and listener face the same direction.
 * - 180: 180 degrees, which means that the sound source and listener face each other.
 */
@property(assign, nonatomic) NSInteger speaker_orientation;
/**
 * Whether to enable audio blurring:
 * - `YES`: Enable audio blurring.
 * - `NO`: (Default) Disable audio blurring.
 */
@property(assign, nonatomic) BOOL enable_blur;
/**
 * Whether to enable air absorption, that is, to simulate the sound attenuation effect of sound
 * transmitting in the air; under a certain transmission distance, the attenuation speed of
 * high-frequency sound is fast, and the attenuation speed of low-frequency sound is slow.
 * - `YES`: (Default) Enable air absorption. Make sure that the value of `speaker_attenuation` is
 * not `0`; otherwise, this setting does not take effect.
 * - `NO`: Disable air absorption.
 */
@property(assign, nonatomic) BOOL enable_air_absorb;
/**
 * The sound attenuation coefficient of the remote user or media player. The value range is [0,1].
 * The values are as follows:
 * - 0: Broadcast mode, where the volume and timbre are not attenuated with distance, and the volume
 * and timbre heard by local users do not change regardless of distance.
 * - (0,0.5): Weak attenuation mode, where the volume and timbre only have a weak attenuation during
 * the propagation, and the sound can travel farther than that in a real environment.
 * `enable_air_absorb` needs to be enabled at the same time.
 * - 0.5: (Default) Simulates the attenuation of the volume in the real environment; the effect is
 * equivalent to not setting the `speaker_attenuation` parameter.
 * - (0.5,1]: Strong attenuation mode, where volume and timbre attenuate rapidly during the
 * propagation. `enable_air_absorb` needs to be enabled at the same time.
 */
@property(assign, nonatomic) double speaker_attenuation;
/**
 * Whether to enable the Doppler effect: When there is a relative displacement between the sound
 * source and the receiver of the sound source, the tone heard by the receiver changes.
 * - `YES`: Enable the Doppler effect.
 * - `NO`: (Default) Disable the Doppler effect.
 * @note
 * - This parameter is suitable for scenarios where the sound source is moving at high speed (for
 * example, racing games). It is not recommended for common audio and video interactive scenarios
 * (for example, voice chat, co-streaming, or online KTV).
 * - When this parameter is enabled, Agora recommends that you set a regular period (such as 30 ms),
 * and then call the `updatePlayerPositionInfo:positionInfo:`,
 * `updateSelfPosition:axisForward:axisRight:axisUp:`, and `updateRemotePosition:positionInfo:`
 * methods to continuously update the relative distance between the sound source and the receiver.
 * The following factors can cause the Doppler effect to be unpredictable or the sound to be
 * jittery: the period of updating the distance is too long, the updating period is irregular, or
 * the distance information is lost due to network packet loss or delay.
 */
@property(assign, nonatomic) BOOL enable_doppler;
@end

NS_SWIFT_NAME(AgoraEchoTestConfiguration)
/**
 * @brief The configuration of the audio and video call loop test.
 */
__attribute__((visibility("default"))) @interface AgoraEchoTestConfiguration : NSObject
/**
 * The view used to render the local user's video. This parameter is only applicable to scenarios
 * testing video devices, that is, when `enableVideo` is YES.
 */
@property(strong, nonatomic) VIEW_CLASS* _Nullable view NS_SWIFT_NAME(view);
/**
 * Whether to enable the audio device for the loop test:
 * - `YES`: (Default) Enable the audio device. To test the audio device, set this parameter as YES.
 * - `NO`: Disable the audio device.
 */
@property(assign, nonatomic) BOOL enableAudio NS_SWIFT_NAME(enableAudio);
/**
 * Whether to enable the video device for the loop test:
 * - `YES`: (Default) Enable the video device. To test the video device, set this parameter as YES.
 * - `NO`: Disable the video device.
 */
@property(assign, nonatomic) BOOL enableVideo NS_SWIFT_NAME(enableVideo);
/**
 * The token used to secure the audio and video call loop test. If you do not enable App Certificate
 * in Agora Console, you do not need to pass a value in this parameter; if you have enabled App
 * Certificate in Agora Console, you must pass a token in this parameter; the `uid` used when you
 * generate the token must be 0xFFFFFFFF, and the channel name used must be the channel name that
 * identifies each audio and video call loop tested. For server-side token generation, see .
 */
@property(copy, nonatomic) NSString* _Nullable token NS_SWIFT_NAME(token);
/**
 * The channel name that identifies each audio and video call loop. To ensure proper loop test
 * functionality, the channel name passed in to identify each loop test cannot be the same when
 * users of the same project (App ID) perform audio and video call loop tests on different devices.
 */
@property(copy, nonatomic) NSString* _Nonnull channelId NS_SWIFT_NAME(channelId);
/**
 * Set the time interval or delay for returning the results of the audio and video loop test. The
 * value range is [2,10], in seconds, with the default value being 2 seconds.
 * - For audio loop tests, the test results will be returned according to the time interval you set.
 * - For video loop tests, the video will be displayed in a short time, after which the delay will
 * gradually increase until it reaches the delay you set.
 */
@property(assign, nonatomic) NSInteger intervalInSeconds NS_SWIFT_NAME(intervalInSeconds);
@end

/**
 * @brief Recording file information.
 */
__attribute__((visibility("default"))) @interface AgoraMediaRecorderInfo : NSObject

/**
 * The absolute storage path of the recording file.
 */
@property(copy, nonatomic) NSString* _Nonnull recorderFileName;
/**
 * Duration of the recorded file, in milliseconds.
 */
@property(assign, nonatomic) NSUInteger durationMs;
/**
 * Size of the recording file in bytes.
 */
@property(assign, nonatomic) NSUInteger fileSize;

@end

/**
 * @brief Audio and video stream recording configuration.
 */
__attribute__((visibility("default"))) @interface AgoraMediaRecorderConfiguration : NSObject

/**
 * The absolute path where the recording file is saved locally. The path must include the file name
 * and extension. For example:
 * - iOS: `/App Sandbox/Library/Caches/example.mp4`
 * - macOS: `/Library/Logs/example.mp4`
 * @note Make sure the specified path exists and is writable.
 */
@property(copy, nonatomic) NSString* _Nonnull storagePath;
/**
 * The format of the recorded file. See `AgoraMediaRecorderContainerFormat`.
 */
@property(assign, nonatomic) AgoraMediaRecorderContainerFormat containerFormat;
/**
 * Recording content. See `AgoraMediaRecorderStreamType` for details.
 */
@property(assign, nonatomic) AgoraMediaRecorderStreamType streamType;
/**
 * Maximum recording duration in milliseconds. The default value is 120000.
 */
@property(assign, nonatomic) NSUInteger maxDurationMs;
/**
 * Interval for updating recording information, in milliseconds. The value must be within [1000,
 * 10000]. The SDK triggers the `mediaRecorder:informationDidUpdated:uid:info:` 
 * callback based on this setting to report the updated recording information.
 */
@property(assign, nonatomic) NSUInteger recorderInfoUpdateInterval;
/**
 * The width (in px) of the recorded video. The maximum value of width × height must not exceed 3840
 * × 2160.
 * You need to set this parameter only when calling `createMediaRecorder:` and setting the `type` in
 * `AgoraRecorderStreamInfo` to `AgoraRecorderStreamTypePreview`.
 */
@property(assign, nonatomic) NSUInteger width;
/**
 * Height (in px) of the recorded video. The maximum value of width × height must not exceed 3840 ×
 * 2160.
 *                     You need to set this parameter only when calling `createMediaRecorder:` and
 * setting the `type` in `AgoraRecorderStreamInfo` to
 * `AgoraRecorderStreamTypePreview`.
 */
@property(assign, nonatomic) NSUInteger height;
/**
 * The frame rate for recording video, with a maximum value of 30. For example: 5, 10, 15, 24, 30,
 * etc.
 *                     You need to set this parameter only when calling `createMediaRecorder:` and
 * setting the `type` field in `AgoraRecorderStreamInfo` to
 * `AgoraRecorderStreamTypePreview`.
 */
@property(assign, nonatomic) NSUInteger fps;
/**
 * Sampling rate (Hz) for recording audio. You can set it to 16000, 32000, 44100, or 48000.
 *                     This parameter is required only when you call `createMediaRecorder:` and set
 * the `type` field in `AgoraRecorderStreamInfo` to
 * `AgoraRecorderStreamTypePreview`.
 */
@property(assign, nonatomic) NSUInteger sample_rate;
/**
 * Number of channels for recording audio:
 * - 1: Mono
 * - 2: Stereo
 * You need to set this parameter only when you call `createMediaRecorder:` and set the `type` in
 * `AgoraRecorderStreamInfo` to `AgoraRecorderStreamTypePreview`.
 */
@property(assign, nonatomic) NSUInteger channel_num;
/**
 * The type of the video source for recording. See `AgoraVideoSourceType`.
 *                     You need to set this parameter only when calling `createMediaRecorder:` and
 * setting the `type` in `AgoraRecorderStreamInfo` to `AgoraRecorderStreamTypePreview`.
 */
@property(assign, nonatomic) AgoraVideoSourceType videoSourceType;
@end

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))

/**
 * @brief The information about the specified shareable window or screen.
 */
__attribute__((visibility("default"))) @interface AgoraScreenCaptureSourceInfo : NSObject
/**
 * The type of the shared target. See `AgoraScreenCaptureSourceType`.
 */
@property(assign, nonatomic) AgoraScreenCaptureSourceType type;
/**
 * The window ID for a window or the display ID for a screen.
 */
@property(assign, nonatomic) CGWindowID sourceId;
/**
 * The name of the window or screen. UTF-8 encoding.
 */
@property(copy, nonatomic) NSString* _Nonnull sourceName;
/**
 * The image content of the thumbnail. See `NSImage`.
 */
@property(strong, nonatomic) NSImage* _Nonnull thumbImage;
/**
 * The image content of the icon. See `NSImage`.
 */
@property(strong, nonatomic) NSImage* _Nullable iconImage;
/**
 * The process to which the window belongs. UTF-8 encoding.
 */
@property(copy, nonatomic) NSString* _Nonnull processPath;
/**
 * The title of the window. UTF-8 encoding.
 */
@property(copy, nonatomic) NSString* _Nonnull sourceTitle;
/**
 * The position of a window relative to the entire screen space (including all shareable screens).
 * See `CGRect`.
 */
@property(assign, nonatomic) CGRect position;
/**
 * Determines whether the screen is the primary display:
 * - `YES`: The screen is the primary display.
 * - `NO`: The screen is not the primary display.
 */
@property(assign, nonatomic) BOOL primaryMonitor;

@end

#endif

NS_SWIFT_NAME(AgoraExtensionInfo) __attribute__((visibility("default"))) @interface AgoraExtensionInfo : NSObject

/**
 * The id of the remote user on which the extension works.
 *
 * @note remoteUid = 0 means that the extension works on all remote streams.
 */
@property (assign, nonatomic) NSUInteger remoteUid NS_SWIFT_NAME(remoteUid);
/**
 * User ID: A 32-bit unsigned integer ranging from 1 to (2^32-1). It must be unique.
 */
@property (assign, nonatomic) NSUInteger localUid NS_SWIFT_NAME(localUid);

/**
 * The type of media device.
 */
@property (assign, nonatomic) AgoraMediaSourceType sourceType NS_SWIFT_NAME(sourceType);

/**
 *  The unique channel name for the AgoraRTC session in the string format. The string
 * length must be less than 64 bytes. Supported character scopes are:
 * - All lowercase English letters: a to z.
 * - All uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - The space character.
 * - Punctuation characters and other symbols, including: "!", "#", "$", "%", "&", "(", ")", "+",
 * "-",
 * ":", ";", "<", "=", ".", ">", "?", "@", "[", "]", "^", "_", " {", "}", "|", "~", ",".
 */
@property (copy, nonatomic) NSString *_Nonnull channelId NS_SWIFT_NAME(channelId);

@end

/**
 * @brief The configuration of custom audio tracks.
 */
NS_SWIFT_NAME(AgoraAudioTrackConfig) __attribute__((visibility("default"))) @interface AgoraAudioTrackConfig : NSObject
/**
 * Whether to enable the local audio-playback device:
 * - `YES`: (Default) Enable the local audio-playback device.
 * - `NO`: Do not enable the local audio-playback device.
 */
@property (assign, nonatomic) BOOL enableLocalPlayback NS_SWIFT_NAME(enableLocalPlayback);
/**
 * Whether to enable audio processing module:
 * - `YES`: Enable the audio processing module to apply the Automatic Echo Cancellation (AEC),
 * Automatic Noise Suppression (ANS), and Automatic Gain Control (AGC) effects.
 * - `NO`: (Default) Do not enable the audio processing module.
 * @note This parameter only takes effect on AgoraAudioTrackTypeDirect in custom audio capturing.
 */
@property (assign, nonatomic) BOOL enableAudioProcessing NS_SWIFT_NAME(enableAudioProcessing);
@end

/**
 * @brief Layout information of a specific sub-video stream within the mixed stream.
 */
__attribute__((visibility("default"))) @interface AgoraVideoLayout: NSObject
/**
 * The channel name to which the sub-video stream belongs.
 */
@property(copy, nonatomic) NSString* _Nonnull channelId NS_SWIFT_NAME(channelId);
/**
 * User ID who published this sub-video stream.
 */
@property (assign, nonatomic) NSUInteger uid NS_SWIFT_NAME(uid);
/**
 * Reserved for future use.
 */
@property(copy, nonatomic) NSString* _Nullable strUid NS_SWIFT_NAME(strUid);
/**
 * Status of the sub-video stream on the video mixing canvas.
 * - 0: Normal. The sub-video stream has been rendered onto the mixing canvas.
 * - 1: Placeholder image. The sub-video stream has no video frames and is displayed as a
 * placeholder on the mixing canvas.
 * - 2: Black image. The sub-video stream is replaced by a black image.
 */
@property (assign, nonatomic) NSUInteger videoState NS_SWIFT_NAME(videoState);
/**
 * X-coordinate (px) of the sub-video stream on the mixing canvas. The relative lateral displacement
 * of the top left corner of the video for video mixing to the origin (the top left corner of the
 * canvas).
 */
@property (assign, nonatomic) NSUInteger x NS_SWIFT_NAME(x);
/**
 * Y-coordinate (px) of the sub-video stream on the mixing canvas. The relative longitudinal
 * displacement of the top left corner of the captured video to the origin (the top left corner of
 * the canvas).
 */
@property (assign, nonatomic) NSUInteger y NS_SWIFT_NAME(y);
/**
 * Width (px) of the sub-video stream.
 */
@property (assign, nonatomic) NSUInteger width NS_SWIFT_NAME(width);
/**
 * Heitht (px) of the sub-video stream.
 */
@property (assign, nonatomic) NSUInteger height NS_SWIFT_NAME(height);
@end

/**
 * @brief Detailed layout information of a mixed video stream.
 */
__attribute__((visibility("default"))) @interface AgoraVideoLayoutInfo: NSObject
/**
 * The width of the mixed video stream (px).
 */
@property (assign, nonatomic) NSUInteger width NS_SWIFT_NAME(width);
/**
 * The height of the mixed video stream (px).
 */
@property (assign, nonatomic) NSUInteger height NS_SWIFT_NAME(height);
/**
 * The number of layout information in the mixed video stream.
 */
@property (assign, nonatomic) NSUInteger layoutCount NS_SWIFT_NAME(layoutCount);
/**
 * The detailed layout information of a mixed video stream. See `AgoraVideoLayout`.
 */
@property(copy, nonatomic) NSArray<AgoraVideoLayout *> *_Nullable layoutList NS_SWIFT_NAME(layoutList);
@end

/**
 * @brief Indicators during video frame rendering progress.
 */
NS_SWIFT_NAME(AgoraVideoRenderingTracingInfo) __attribute__((visibility("default"))) @interface AgoraVideoRenderingTracingInfo : NSObject
/**
 * The time interval (ms) from `startMediaRenderingTracing` to SDK triggering the
 * `rtcEngine:videoRenderingTracingResultOfUid:currentEvent:tracingInfo:` callback. Agora recommends
 * you call `startMediaRenderingTracing` before joining a channel.
 */
@property (assign, nonatomic) NSInteger elapsedTime NS_SWIFT_NAME(elapsedTime);
/**
 * The time interval (ms) from `startMediaRenderingTracing` to
 * `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`. A negative number indicates that
 * `startMediaRenderingTracing` is called after calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`.
 */
@property (assign, nonatomic) NSInteger start2JoinChannel NS_SWIFT_NAME(start2JoinChannel);
/**
 * The time interval (ms) from `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to successfully joining the channel.
 */
@property (assign, nonatomic) NSInteger join2JoinSuccess NS_SWIFT_NAME(join2JoinSuccess);
/**
 * - If the local user calls `startMediaRenderingTracing` before successfully joining the channel,
 * this value is the time interval (ms) from the local user successfully joining the channel to the
 * remote user joining the channel.
 * - If the local user calls `startMediaRenderingTracing` after successfully joining the channel,
 * the value is the time interval (ms) from `startMediaRenderingTracing` to when the remote user
 * joins the channel.
 * @note
 * - If the local user calls `startMediaRenderingTracing` after the remote user joins the channel,
 * the value is 0 and meaningless.
 * - In order to reduce the time of rendering the first frame for remote users, Agora recommends
 * that the local user joins the channel when the remote user is in the channel to reduce this
 * value.
 */
@property (assign, nonatomic) NSInteger joinSuccess2RemoteJoined NS_SWIFT_NAME(joinSuccess2RemoteJoined);
/**
 * - If the local user calls `startMediaRenderingTracing` before the remote user joins the channel,
 * this value is the time interval (ms) from when the remote user joins the channel to when the
 * local user sets the remote view.
 * - If the local user calls `startMediaRenderingTracing` after the remote user joins the channel,
 * this value is the time interval (ms) from calling `startMediaRenderingTracing` to setting the
 * remote view.
 * @note
 * - If the local user calls `startMediaRenderingTracing` after setting the remote view, the value
 * is 0 and has no effect.
 * - In order to reduce the time of rendering the first frame for remote users, Agora recommends
 * that the local user sets the remote view before the remote user joins the channel, or sets the
 * remote view immediately after the remote user joins the channel to reduce this value.
 */
@property (assign, nonatomic) NSInteger remoteJoined2SetView NS_SWIFT_NAME(remoteJoined2SetView);
/**
 * - If the local user calls `startMediaRenderingTracing` before the remote user joins the channel,
 * this value is the time interval (ms) from the remote user joining the channel to subscribing to
 * the remote video stream.
 * - If the local user calls `startMediaRenderingTracing` after the remote user joins the channel,
 * this value is the time interval (ms) from `startMediaRenderingTracing` to subscribing to the
 * remote video stream.
 * @note
 * - If the local user calls `startMediaRenderingTracing` after subscribing to the remote video
 * stream, the value is 0 and has no effect.
 * - In order to reduce the time of rendering the first frame for remote users, Agora recommends
 * that after the remote user joins the channel, the local user immediately subscribes to the remote
 * video stream to reduce this value.
 */
@property (assign, nonatomic) NSInteger remoteJoined2UnmuteVideo NS_SWIFT_NAME(remoteJoined2UnmuteVideo);
/**
 * - If the local user calls `startMediaRenderingTracing` before the remote user joins the channel,
 * this value is the time interval (ms) from when the remote user joins the channel to when the
 * local user receives the remote video stream.
 * - If the local user calls `startMediaRenderingTracing` after the remote user joins the channel,
 * this value is the time interval (ms) from `startMediaRenderingTracing` to receiving the remote
 * video stream.
 * @note
 * - If the local user calls `startMediaRenderingTracing` after receiving the remote video stream,
 * the value is 0 and has no effect.
 * - In order to reduce the time of rendering the first frame for remote users, Agora recommends
 * that the remote user publishes video streams immediately after joining the channel, and the local
 * user immediately subscribes to remote video streams to reduce this value.
 */
@property (assign, nonatomic) NSInteger remoteJoined2PacketReceived NS_SWIFT_NAME(remoteJoined2PacketReceived);

@end

/**
 * @brief Media metadata.
 */
NS_SWIFT_NAME(AgoraMetadata) __attribute__((visibility("default"))) @interface AgoraMetadata : NSObject
/**
 * The channel name.
 */
@property (copy, nonatomic) NSString* _Nonnull channelId NS_SWIFT_NAME(channelId);
/**
 * The user ID.
 * - For the recipient: The ID of the remote user who sent the `AgoraMetadata`.
 * - For the sender: Ignore it.
 */
@property (assign, nonatomic) NSInteger uid NS_SWIFT_NAME(uid);
/**
 * The received metadata.
 */
@property (strong, nonatomic) NSData* _Nonnull data NS_SWIFT_NAME(data);
/**
 * The timestamp (ms) of when the `AgoraMetadata` is sent.
 */
@property (assign, nonatomic) NSTimeInterval timestamp NS_SWIFT_NAME(timestamp);
@end

/**
 * @brief Statistical information about a specific network path.
 *
 * @since 4.6.0
 */
__attribute__((visibility("default"))) @interface AgoraPathStats : NSObject

/**
 * Types of network path. See `AgoraMultipathType`.
 * @since 4.6.0
 */
@property (assign, nonatomic) AgoraMultipathType pathType;

/**
 * The transmission bitrate of the path in Kbps.
 * @since 4.6.0
 */
@property (assign, nonatomic) NSUInteger txKBitRate;

/**
 * The receiving bitrate of the path in Kbps.
 * @since 4.6.0
 */
@property (assign, nonatomic) NSUInteger rxKBitRate;
@end

/**
 * @brief Aggregates statistics of each network path in multipath transmission.
 *
 * @since 4.6.0
 */
__attribute__((visibility("default"))) @interface AgoraMultipathStats : NSObject
/**
 * The total number of bytes received over the LAN path.
 */
@property (assign, nonatomic) NSUInteger lanRxBytes;
/**
 * The total number of bytes sent over the LAN path.
 */
@property (assign, nonatomic) NSUInteger lanTxBytes;
/**
 * The total number of bytes received over the Wi-Fi path.
 */
@property (assign, nonatomic) NSUInteger wifiRxBytes;
/**
 * The total number of bytes sent over the Wi-Fi path.
 */
@property (assign, nonatomic) NSUInteger wifiTxBytes;
/**
 * The total number of bytes received over the mobile network path.
 */
@property (assign, nonatomic) NSUInteger mobileRxBytes;
/**
 * The total number of bytes sent over the mobile network path.
 */
@property (assign, nonatomic) NSUInteger mobileTxBytes;
/**
 * The number of currently active transmission paths.
 */
@property (assign, nonatomic) NSUInteger activePathNum;
/**
 * Statistics for each active transmission path. See `AgoraPathStats`.
 */
@property (copy, nonatomic) NSArray<AgoraPathStats *> *_Nullable pathStats NS_SWIFT_NAME(pathStats);
@end
