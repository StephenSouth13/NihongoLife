//
//  AgoraRtcEngineKit.h
//  AgoraRtcEngineKit
//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraConstants.h"
#import "AgoraObjects.h"
#import "AgoraRtcMediaPlayerProtocol.h"
#import "AgoraH265TranscoderProtocol.h"

/** Agora provides ensured quality of experience (QoE) for worldwide Internet-based voice and video communications through a virtual global network that is especially optimized for real-time web and mobile-to-mobile applications.

 The AgoraRtcEngineKit class is the entry point of the Agora SDK that provides simple APIs for applications to easily start voice and video communication.
 */
@class AgoraRtcEngineKit;
@class AgoraMediaRecorder;

/**
 * The AgoraRtcEngineDelegate protocol enables callback event notifications to your application.

 The SDK uses delegate callbacks in the AgoraRtcEngineDelegate protocol to report runtime events to the application.
 From v1.1, some block callbacks in the SDK are replaced with delegate callbacks. The old block callbacks are therefore deprecated, but can still be used in the current version. However, Agora recommends replacing block callbacks with delegate callbacks. The SDK calls the block callback if a callback is defined in both the block and delegate callbacks.
 */
@protocol AgoraRtcEngineDelegate <NSObject>
@optional

#pragma mark Delegate Methods

/**-----------------------------------------------------------------------------
 * @name Delegate Methods

 The SDK uses delegate callbacks in the AgoraRtcEngineDelegate protocol to report runtime events to the application.
 From v1.1, some block callbacks in the SDK are replaced with delegate callbacks. The old block callbacks are therefore deprecated, but can still be used in the current version. However, Agora recommends replacing block callbacks with delegate callbacks. The SDK calls the block callback if a callback is defined in both the block and delegate callbacks.
 * -----------------------------------------------------------------------------
 */

#pragma mark Core Delegate Methods

/**
 * @brief Reports an error during SDK runtime.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param errorCode Error code. See `AgoraErrorCode`.
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didOccurError:(AgoraErrorCode)errorCode NS_SWIFT_NAME(rtcEngine(_:didOccurError:));

/**
 * @brief Occurs when the token expires.
 *
 * @details
 * The SDK triggers this callback if the token expires.
 * When receiving this callback, you need to generate a new token on your token server and you can
 * renew your token through one of the following ways:
 * - In scenarios involving one channel:
 *   - Call `renewToken:` to pass in the new token.
 *   - Call `leaveChannel:leaveChannelBlock:` to leave the current channel and then pass in the new
 * token when you call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join a
 * channel.
 * - In scenarios involving mutiple channels: Call `updateChannelExWithMediaOptions:connection:` to
 * pass in the new token.
 *
 * @param engine `AgoraRtcEngineKit` object.
 *
 */
- (void)rtcEngineRequestToken:(AgoraRtcEngineKit * _Nonnull)engine NS_SWIFT_NAME(rtcEngineRequestToken(_:));

/**
 * @brief Occurs when the token expires in 30 seconds.
 *
 * @details
 * When receiving this callback, you need to generate a new token on your token server and you can
 * renew your token through one of the following ways:
 * - In scenarios involving one channel:
 *   - Call `renewToken:` to pass in the new token.
 *   - Call `leaveChannel:leaveChannelBlock:` to leave the current channel and then pass in the new
 * token when you call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join a
 * channel.
 * - In scenarios involving mutiple channels: Call `updateChannelExWithMediaOptions:connection:` to
 * pass in the new token.
 * Call timing: The SDK triggers this callback 30 seconds before the token expires, reminding the
 * app to update the token.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param token The token that is about to expire.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine tokenPrivilegeWillExpire:(NSString *_Nonnull)token NS_SWIFT_NAME(rtcEngine(_:tokenPrivilegeWillExpire:));

/**
 * @brief Callback for `renewToken:` call result.
 *
 * @since v4.6.0
 *
 * @details
 * This callback is triggered after the user calls the `renewToken:` method to update the token, and
 * is used to notify the app of the result.
 *
 * @param token Token.
 * @param code Error code. See AgoraRenewTokenErrorCode.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine renewTokenResult:(NSString *_Nonnull)token code:(AgoraRenewTokenErrorCode)code NS_SWIFT_NAME(rtcEngine(_:renewTokenResult:code:));


/**
   * Occurs when connection license verification fails
   *
   * You can know the reason accordding to error code
   */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine licenseValidationFailure:(AgoraLicenseVerifyCode) error;

/**
 * @brief Occurs when the connection between the SDK and the server is interrupted.
 *
 * @details
 * The SDK triggers this callback when it loses connection with the server for more than four
 * seconds after the connection is established. After triggering this callback, the SDK tries to
 * reconnect to the server. You can use this callback to implement pop-up reminders. The differences
 * between this callback and `rtcEngineConnectionDidLost:` are as follow:
 * - The SDK triggers the `rtcEngineConnectionDidInterrupted:` callback when it loses connection
 * with the server for more than four seconds after it successfully joins the channel.
 * - The SDK triggers the `rtcEngineConnectionDidLost:` callback when it loses connection with the
 * server for more than 10 seconds, whether or not it joins the channel.
 * If the SDK fails to rejoin the channel 20 minutes after being disconnected from Agora's edge
 * server, the SDK stops rejoining the channel.
 *
 * @param engine `AgoraRtcEngineKit` object.
 *
 */
- (void)rtcEngineConnectionDidInterrupted:(AgoraRtcEngineKit * _Nonnull)engine NS_SWIFT_NAME(rtcEngineConnectionDidInterrupted(_:));


/**
 * @brief Occurs when the SDK cannot reconnect to Agora's edge server 10 seconds after its
 * connection to the server is interrupted.
 *
 * @details
 * The SDK triggers this callback when it cannot connect to the server 10 seconds after calling the
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` method, regardless of whether it is
 * in the channel. If the SDK fails to rejoin the channel 20 minutes after being disconnected from
 * Agora's edge server, the SDK stops rejoining the channel.
 *
 * @param engine `AgoraRtcEngineKit` object.
 *
 */
- (void)rtcEngineConnectionDidLost:(AgoraRtcEngineKit * _Nonnull)engine NS_SWIFT_NAME(rtcEngineConnectionDidLost(_:));


/**
 * @brief Occurs when the connection is banned by the Agora server.
 *
 * @param engine `AgoraRtcEngineKit` object.
 *
 */
- (void)rtcEngineConnectionDidBanned:(AgoraRtcEngineKit * _Nonnull)engine NS_SWIFT_NAME(rtcEngineConnectionDidBanned(_:));

/**
 * @brief Occurs when the local network type changes.
 *
 * @details
 * This callback occurs when the connection state of the local user changes. You can get the
 * connection state and reason for the state change in this callback. When the network connection is
 * interrupted, this callback indicates whether the interruption is caused by a network type change
 * or poor network conditions.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param type The type of the local network connection. See `AgoraNetworkType`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine networkTypeChanged:(AgoraNetworkType)type NS_SWIFT_NAME(rtcEngine(_:networkTypeChanged:));

/**
 * @brief Occurs when the SDK cannot get the device permission.
 *
 * @details
 * When the SDK fails to get the device permission, the SDK triggers this callback to report which
 * device permission cannot be got.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param type The type of the device permission. See `AgoraPermissionType`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine permissionError:(AgoraPermissionType)type NS_SWIFT_NAME(rtcEngine(_:permissionError:));

/**
 * @brief Occurs when the network connection state changes.
 *
 * @details
 * When the network connection state changes, the SDK triggers this callback and reports the current
 * connection state and the reason for the change.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param state The current connection state. See `AgoraConnectionState`.
 * @param reason The reason for a connection state change. See `AgoraConnectionChangedReason`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine connectionChangedToState:(AgoraConnectionState)state reason:(AgoraConnectionChangedReason)reason NS_SWIFT_NAME(rtcEngine(_:connectionChangedTo:reason:));

/**
 * @brief Reports the statistics about the current call.
 *
 * @details
 * Call timing: The SDK triggers this callback once every two seconds after the user joins the
 * channel.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param stats Statistics of the RTC engine. See `AgoraChannelStats`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine reportRtcStats:(AgoraChannelStats * _Nonnull)stats NS_SWIFT_NAME(rtcEngine(_:reportRtcStats:));


/**
 * @brief Reports the last-mile network quality of the local user.
 *
 * @details
 * This callback reports the last-mile network conditions of the local user before the user joins
 * the channel. Last mile refers to the connection between the local device and Agora's edge server.
 * Before the user joins the channel, this callback is triggered by the SDK once
 * `startLastmileProbeTest:` is called and reports the last-mile network conditions of the local
 * user.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param quality The last-mile network quality. See `AgoraNetworkQuality`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine lastmileQuality:(AgoraNetworkQuality)quality NS_SWIFT_NAME(rtcEngine(_:lastmileQuality:));


/**
 * @brief Reports the last mile network probe result.
 *
 * @details
 * The SDK triggers this callback within 30 seconds after the app calls `startLastmileProbeTest:`.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param result The uplink and downlink last-mile network probe test result. See
 * `AgoraLastmileProbeResult`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine lastmileProbeTestResult:(AgoraLastmileProbeResult * _Nonnull)result NS_SWIFT_NAME(rtcEngine(_:lastmileProbeTest:));


#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))

/** The specified device state.

 @note  This method applies to macOS only.

 @param engine     AgoraRtcEngineKit object
 @param deviceId   Device ID
 @param deviceType AgoraMediaDeviceType
 @param state      State of the device:

 * 0: Added.
 * 8: Removed.
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine device:(NSString * _Nonnull)deviceId type:(AgoraMediaDeviceType)deviceType stateChanged:(NSInteger)state NS_SWIFT_NAME(rtcEngine(_:device:type:stateChanged:));

#endif

/**
 * @brief Reports the built-in encryption errors.
 *
 * @details
 * When encryption is enabled by calling `enableEncryption:encryptionConfig:`, the SDK triggers this
 * callback if an error occurs in encryption or decryption on the sender or the receiver side.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param errorType Details about the error type. See `AgoraEncryptionErrorType`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didOccurEncryptionError:(AgoraEncryptionErrorType)errorType NS_SWIFT_NAME(rtcEngine(_:didOccur:));
/**
   * Reports the user log upload result
   * @param requestId RequestId of the upload
   * @param success Is upload success
   * @param reason Reason of the upload, 0: OK, 1 Network Error, 2 Server Error.
   */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine uploadLogResultRequestId:(NSString * _Nonnull)requestId success:(BOOL)success reason:(AgoraUploadErrorReason)reason NS_SWIFT_NAME(rtcEngine(_:uploadLogResultRequestId:success:reason:));

#pragma mark Local User Core Delegate Methods

/**
 * @brief Occurs when a user joins the channel.
 *
 * @details
 * This callback notifies the application that a user joins a specified channel.
 * Call timing: The SDK triggers this callback when you call `joinChannelByToken:channelId:info:uid:joinSuccess:`, 
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`, `joinChannelByToken:channelId:userAccount:joinSuccess:`,
 * `joinChannelByToken:channelId:userAccount:mediaOptions:joinSuccess:`, `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`
 * or `joinChannelExByToken:channelId:userAccount:delegate:mediaOptions:joinSuccess:` to join a channel.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param channel The channel name.
 * @param uid The ID of the user who rejoins the channel.
 * @param elapsed Time elapsed (ms) from the local user calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until the SDK triggers this
 * callback.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didJoinChannel:(NSString * _Nonnull)channel withUid:(NSUInteger)uid elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:didJoinChannel:withUid:elapsed:));

/**
 * @brief Occurs when a user rejoins the channel.
 *
 * @details
 * Call timing: When a user loses connection with the server because of network problems, the SDK
 * automatically tries to reconnect and triggers this callback upon reconnection.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param channel The channel name.
 * @param uid The ID of the user who rejoins the channel.
 * @param elapsed Time elapsed (ms) from the local user calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until the SDK triggers this
 * callback.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didRejoinChannel:(NSString * _Nonnull)channel withUid:(NSUInteger)uid elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:didRejoinChannel:withUid:elapsed:));

/**
 * @brief Occurs when the user role or the audience latency level changes.
 *
 * @details
 * Call timing: This callback will be triggered in any of the following situations:
 * - Calling `setClientRole:` or `setClientRole:options:` to set the user role or audience latency
 * level **after joining a channel**
 * - Calling `setClientRole:` or `setClientRole:options:` and set the user role to `AUDIENCE`
 * **before joining a channel**.
 *
 * @note This callback will not be triggered when you call `setClientRole:` or
 * `setClientRole:options:` and set the user role to `BROADCASTER` **before joining a channel**.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param oldRole Role that the user switches from: `AgoraClientRole`.
 * @param newRole Role that the user switches to: `AgoraClientRole`.
 * @param newRoleOptions Since
 * v4.1.0
 * Properties of the role that the user switches to. See `AgoraClientRoleOptions`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didClientRoleChanged:(AgoraClientRole)oldRole newRole:(AgoraClientRole)newRole newRoleOptions:(AgoraClientRoleOptions * _Nullable)newRoleOptions NS_SWIFT_NAME(rtcEngine(_:didClientRoleChanged:newRole:newRoleOptions:));

/**
 * @brief Occurs when switching a user role fails.
 *
 * @details
 * This callback informs you about the reason for failing to switching and your current user role.
 * Call timing: The SDK triggers this callback when the local user calls `setClientRole:` or
 * `setClientRole:options:` after joining a channel to switch the user role but the switching fails.
 *
 * @param reason The reason for a user role switch failure. See `AgoraClientRoleChangeFailedReason`.
 * @param currentRole Current user role. See `AgoraClientRole`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didClientRoleChangeFailed:(AgoraClientRoleChangeFailedReason)reason currentRole:(AgoraClientRole)currentRole NS_SWIFT_NAME(rtcEngine(_:didClientRoleChangeFailed:currentRole:));

/**
 * @brief Occurs when a user leaves a channel.
 *
 * @details
 * You can obtain information such as the total duration of a call, and the data traffic that the
 * SDK transmits and receives.
 * Call timing: The SDK triggers this callback after you call `leaveChannel:`,
 * `leaveChannel:leaveChannelBlock:`, `leaveChannelEx:leaveChannelBlock:`, or
 * `leaveChannelEx:options:leaveChannelBlock:` to leave a channel.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param stats Call statistics. See `AgoraChannelStats`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didLeaveChannelWithStats:(AgoraChannelStats * _Nonnull)stats NS_SWIFT_NAME(rtcEngine(_:didLeaveChannelWith:));


/**
 * @brief Reports the last mile network quality of each user in the channel.
 *
 * @details
 * This callback reports the last mile network conditions of each user in the channel. Last mile
 * refers to the connection between the local device and Agora's edge server.
 * The SDK triggers this callback once every two seconds. If a channel includes multiple users, the
 * SDK triggers this callback as many times.
 * This callback provides feedback on network quality through sending and receiving broadcast
 * packets within the channel. Excessive broadcast packets can lead to broadcast storms. To prevent
 * broadcast storms from causing a large amount of data transmission within the channel, this
 * callback supports feedback on the network quality of up to 4 remote hosts simultaneously by
 * default.
 *
 * @note `txQuality` is `Unknown` when the user is not sending a stream; `rxQuality` is `Unknown`
 * when the user is not receiving a stream.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID. The network quality of the user with this user ID is reported. If the uid
 * is 0, the local network quality is reported.
 * @param txQuality Uplink network quality rating of the user in terms of the transmission bit rate,
 * packet loss rate, average RTT (Round-Trip Time) and jitter of the uplink network. This parameter
 * is a quality rating helping you understand how well the current uplink network conditions can
 * support the selected video encoder configuration. For example, a 1000 Kbps uplink network may be
 * adequate for video frames with a resolution of 640 × 480 and a frame rate of 15 fps in the
 * LIVE_BROADCASTING profile, but might be inadequate for resolutions higher than 1280 × 720. See
 * `AgoraNetworkQuality`.
 * @param rxQuality Downlink network quality rating of the user in terms of packet loss rate,
 * average RTT, and jitter of the downlink network. See `AgoraNetworkQuality`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine networkQuality:(NSUInteger)uid txQuality:(AgoraNetworkQuality)txQuality rxQuality:(AgoraNetworkQuality)rxQuality NS_SWIFT_NAME(rtcEngine(_:networkQuality:txQuality:rxQuality:));


#pragma mark Local User Audio Delegate Methods

/**
 * @brief Occurs when the first audio frame is published.
 *
 * @details
 * The SDK triggers this callback under one of the following circumstances:
 * - The local client enables the audio module and calls
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` successfully.
 * - The local client calls `muteLocalAudioStream:` (`YES`) and `muteLocalAudioStream:` (`NO`) in
 * sequence.
 * - The local client calls `disableAudio` and `enableAudio` in sequence.
 * - The local client calls
 * `pushExternalAudioFrameRawData:samples:sampleRate:channels:trackId:timestamp:` to successfully
 * push the audio frame to the SDK.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param elapsed Time elapsed (ms) from the local user calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until the SDK triggers this
 * callback.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine firstLocalAudioFramePublished:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:firstLocalAudioFramePublished:));

/**
 * @brief Reports the statistics of the local audio stream.
 *
 * @details
 * The SDK triggers this callback once every two seconds.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param stats Local audio statistics. See `AgoraRtcLocalAudioStats`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localAudioStats:(AgoraRtcLocalAudioStats * _Nonnull)stats NS_SWIFT_NAME(rtcEngine(_:localAudioStats:));

/**
 * @brief Occurs when the local audio stream state changes.
 *
 * @details
 * When the state of the local audio stream changes (including the state of the audio capture and
 * encoding), the SDK triggers this callback to report the current state. This callback indicates
 * the state of the local audio stream, and allows you to troubleshoot issues when audio exceptions
 * occur.
 *
 * @note When the state is `AgoraAudioLocalStateFailed` (3), you can view the error information in
 * the `error` parameter.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param state The state of the local audio. See `AgoraAudioLocalState`.
 * @param reason Reasons for local audio state changes. See `AgoraAudioLocalReason`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localAudioStateChanged:(AgoraAudioLocalState)state reason:(AgoraAudioLocalReason)reason NS_SWIFT_NAME(rtcEngine(_:localAudioStateChanged:reason:));

/**
 * @brief Occurs when the local audio route changes.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param routing The current audio routing. See `AgoraAudioOutputRouting`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didAudioRouteChanged:(AgoraAudioOutputRouting)routing NS_SWIFT_NAME(rtcEngine(_:didAudioRouteChanged:));

/**
 * @brief Occurs when the playback of the local music file finishes.
 *
 * @deprecated Use [rtcEngine:audioMixingStateChanged:reasonCode:] instead.
 *
 * @details
 * After you call `startAudioMixing:loopback:cycle:startPos:` to play a local music file, this
 * callback occurs when the playback finishes. If the call of
 * `startAudioMixing:loopback:cycle:startPos:` fails, the error code `WARN_AUDIO_MIXING_OPEN_ERROR`
 * is returned.
 * 
 * @param engine `AgoraRtcEngineKit` object.
 */
- (void)rtcEngineLocalAudioMixingDidFinish:(AgoraRtcEngineKit * _Nonnull)engine NS_SWIFT_NAME(rtcEngineLocalAudioMixingDidFinish(_:)) __deprecated_msg("Use rtcEngine:audioMixingStateChanged:reasonCode: instead");

/**
 * @brief Occurs when the playback of the local music file finishes.
 *
 * @details
 * This callback occurs when the local audio effect file finishes playing.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param soundId The ID of the audio effect. The unique ID of each audio effect file.
 *
 */
- (void)rtcEngineDidAudioEffectFinish:(AgoraRtcEngineKit * _Nonnull)engine soundId:(int)soundId NS_SWIFT_NAME(rtcEngineDidAudioEffectFinish(_:soundId:));


#pragma mark Local User Video Delegate Methods

/**
 * @brief Occurs when the camera turns on and is ready to capture the video.
 *
 * @deprecated Use [rtcEngine:localVideoStateChangedOfState:reason:sourceType:] instead.
 *
 * @details
 * This callback indicates that the camera has been successfully turned on and you can start to
 * capture video.
 *
 * @param engine `AgoraRtcEngineKit` object.
 */
- (void)rtcEngineCameraDidReady:(AgoraRtcEngineKit * _Nonnull)engine NS_SWIFT_NAME(rtcEngineCameraDidReady(_:)) __deprecated_msg("Use rtcEngine:localVideoStateChangedOfState:reason:sourceType: instead");

#if TARGET_OS_IPHONE

/**
 * @brief Occurs when the camera focus area changes.
 *
 * @details
 * The SDK triggers this callback when the local user changes the camera focus position by calling
 * `setCameraFocusPositionInPreview:`.
 *
 * @param rect The focus rectangle in the local preview. See `CGRect`.
 * @param engine `AgoraRtcEngineKit` object.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine cameraFocusDidChangedToRect:(CGRect)rect NS_SWIFT_NAME(rtcEngine(_:cameraFocusDidChangedTo:));

/**
 * @brief Occurs when the camera exposure area changes.
 *
 * @details
 * The SDK triggers this callback when the local user changes the camera exposure position by
 * calling `setCameraExposurePosition:`.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param rect The focus rectangle in the local preview. See `CGRect`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine cameraExposureDidChangedToRect:(CGRect)rect NS_SWIFT_NAME(rtcEngine(_:cameraExposureDidChangedTo:));

#endif

/**
 * @brief Occurs when the video stops playing.
 *
 * @deprecated Use [rtcEngine:localVideoStateChangedOfState:reason:sourceType:] instead.
 *
 * @details
 * The application can use this callback to change the configuration of the `view` (for example,
 * displaying other pictures in the view) after the video stops playing.
 *
 * @param engine `AgoraRtcEngineKit` object.
 *
 */
- (void)rtcEngineVideoDidStop:(AgoraRtcEngineKit * _Nonnull)engine NS_SWIFT_NAME(rtcEngineVideoDidStop(_:)) __deprecated_msg("Use rtcEngine:localVideoStateChangedOfState:reason:sourceType: instead");

/**
 * @brief Occurs when the first video frame is published.
 *
 * @details
 * The SDK triggers this callback under one of the following circumstances:
 * - The local client enables the video module and calls
 * `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel successfully.
 * - The local client calls `muteLocalVideoStream:` (`YES`) and `muteLocalVideoStream:` (`NO`) in
 * sequence.
 * - The local client calls `disableVideo` and `enableVideo` in sequence.
 * - The local client calls `pushExternalVideoFrame:videoTrackId:` to successfully push the video
 * frame to the SDK.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 * @param elapsed Time elapsed (ms) from the local user calling
 * `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until this callback is triggered.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine firstLocalVideoFramePublishedWithElapsed:(NSInteger)elapsed sourceType:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(rtcEngine(_:firstLocalVideoFramePublishedWithElapsed:sourceType:));

/**
 * @brief Occurs when the first local video frame is displayed on the local video view.
 *
 * @details
 * The SDK triggers this callback when the first local video frame is displayed on the local video
 * view.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 * @param size The size of the first local video frame.
 * @param elapsed The time elapsed (ms) from the local user calling
 * `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel to when the SDK
 * triggers this callback. If `startPreview` / `startPreview:` is called before joining the channel,
 * this parameter indicates the time elapsed from calling `startPreview` or `startPreview:` to when
 * this event occurred.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine firstLocalVideoFrameWithSize:(CGSize)size elapsed:(NSInteger)elapsed sourceType:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(rtcEngine(_:firstLocalVideoFrameWith:elapsed:sourceType:));

/**
 * @brief Reports the statistics of the local video stream.
 *
 * @details
 * The SDK triggers this callback once every two seconds to report the statistics of the local video
 * stream.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 * @param stats The statistics of the local video stream. See `AgoraRtcLocalVideoStats`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localVideoStats:(AgoraRtcLocalVideoStats * _Nonnull)stats sourceType:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(rtcEngine(_:localVideoStats:sourceType:));

/**
 * @brief Video frame rendering event callback.
 *
 * @details
 * After calling the `startMediaRenderingTracing` method or joining a channel, the SDK triggers this
 * callback to report the events of video frame rendering and the indicators during the rendering
 * process. Developers can optimize the indicators to improve the efficiency of the first video
 * frame rendering.
 *
 * @param uid The user ID.
 * @param currentEvent The current video frame rendering event. See `AgoraMediaTraceEvent`.
 * @param tracingInfo The indicators during the video frame rendering process. Developers need to
 * reduce the value of indicators as much as possible in order to improve the efficiency of the
 * first video frame rendering. See `AgoraVideoRenderingTracingInfo`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine videoRenderingTracingResultOfUid:(NSUInteger)uid currentEvent:(AgoraMediaTraceEvent)currentEvent tracingInfo:(AgoraVideoRenderingTracingInfo * _Nonnull)tracingInfo NS_SWIFT_NAME(rtcEngine(_:videoRenderingTracingResultOfUid:currentEvent:tracingInfo:));



#pragma mark Remote User Core Delegate Methods

/**
 * @brief Occurs when a remote user (in the communication profile)/ host (in the live streaming
 * profile) joins the channel.
 *
 * @details
 * - In a communication channel, this callback indicates that a remote user joins the channel. The
 * SDK also triggers this callback to report the existing users in the channel when a user joins the
 * channel.
 * - In a live-broadcast channel, this callback indicates that a host joins the channel. The SDK
 * also triggers this callback to report the existing hosts in the channel when a host joins the
 * channel. Agora recommends limiting the number of co-hosts to 32, with a maximum of 17 video
 * hosts.
 * Call timing: The SDK triggers this callback under one of the following circumstances:
 * - A remote user/host joins the channel.
 * - A remote user switches the user role to the host after joining the channel.
 * - A remote user/host rejoins the channel after a network interruption.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the user or host who joins the channel.
 * @param elapsed Time delay (ms) from the local user calling
 * `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until this callback is triggered.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didJoinedOfUid:(NSUInteger)uid elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:didJoinedOfUid:elapsed:));

/**
 * @brief Occurs when a remote user (in the communication profile)/ host (in the live streaming
 * profile) leaves the channel.
 *
 * @details
 * There are generally two reasons for users to become offline:
 * - Leave the channel: When a user/host leaves the channel, the user/host sends a goodbye message.
 * - Drop offline: When no data packet of the user or host is received for a certain period of time
 * (20 seconds for the communication profile, and more for the live broadcast profile), the SDK
 * assumes that the user/host drops offline. A poor network connection may lead to false detections.
 * It is recommended to use the Agora RTM SDK for reliable offline detection.
 * Call timing: This callback is triggered when a remote user (in the communication profile) or host
 * (in the live streaming profile) leaves a channel.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the user who leaves the channel or goes offline.
 * @param reason Reasons why a remote user (in the communication profile) or host (in the live
 * streaming profile) goes offline. See `AgoraUserOfflineReason`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didOfflineOfUid:(NSUInteger)uid reason:(AgoraUserOfflineReason)reason NS_SWIFT_NAME(rtcEngine(_:didOfflineOfUid:reason:));


/**
 * @brief Occurs when the local user receives the data stream from the remote user.
 *
 * @details
 * The SDK triggers this callback when the local user receives the stream message that the remote
 * user sends by calling the `sendStreamMessage:data:` method.
 *
 * @note If you need a more comprehensive solution for low-latency, high-concurrency, and scalable
 * real-time messaging and status synchronization, it is recommended to use `Signaling`.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the remote user sending the message.
 * @param streamId The stream ID of the received message.
 * @param data The data received.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine receiveStreamMessageFromUid:(NSUInteger)uid streamId:(NSInteger)streamId data:(NSData * _Nonnull)data NS_SWIFT_NAME(rtcEngine(_:receiveStreamMessageFromUid:streamId:data:));

/**
 * @brief Occurs when the local user does not receive the data stream from the remote user.
 *
 * @details
 * The SDK triggers this callback when the local user fails to receive the stream message that the
 * remote user sends by calling the `sendStreamMessage:data:` method.
 *
 * @note If you need a more comprehensive solution for low-latency, high-concurrency, and scalable
 * real-time messaging and status synchronization, it is recommended to use `Signaling`.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the remote user sending the message.
 * @param streamId The stream ID of the received message.
 * @param error Error code.
 * @param missed The number of lost messages.
 * @param cached Number of incoming cached messages when the data stream is interrupted.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didOccurStreamMessageErrorFromUid:(NSUInteger)uid streamId:(NSInteger)streamId error:(NSInteger)error missed:(NSInteger)missed cached:(NSInteger)cached NS_SWIFT_NAME(rtcEngine(_:didOccurStreamMessageErrorFromUid:streamId:error:missed:cached:));


/**
 * @brief Occurs when the local user receives data via Reliable Data Transmission (RDT) from a remote user.
 *
 * @technical preview
 *
 *  @param engine The engine kit
 *  @param uid The remote user id
 *  @param type The stream type. See AgoraRdtStreamType.
 *  @param data The data received.
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine receiveRdtMessageFromUid:(NSUInteger)uid type:(AgoraRdtStreamType)type data:(NSData * _Nonnull)data NS_SWIFT_NAME(rtcEngine(_:receiveRdtMessageFromUid:type:data:));

/**
 * @brief Occurs when the RDT tunnel state changed
 *
 * @technical preview
 *
 *  @param engine The engine kit
 *  @param uid The remote user id
 *  @param state  The RDT state. See AgoraRdtState.
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didOccurRdtMessageStateFromUid:(NSUInteger)uid state:(AgoraRdtState)state NS_SWIFT_NAME(rtcEngine(_:didOccurRdtMessageStateFromUid:state:));

/**
 * @brief Occurs when the local user receives media control message sent by a remote user.
 *
 * @technical preview
 *
 *  @param engine The engine kit
 *  @param uid The remote user id
 *  @param data The data received.
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine receiveMediaControlMessageFromUid:(NSUInteger)uid data:(NSData * _Nonnull)data NS_SWIFT_NAME(rtcEngine(_:receiveMediaControlMessageFromUid:data:));


#pragma mark Remote User Audio Delegate Methods

/**-----------------------------------------------------------------------------
 * @name Remote User Audio Delegate Methods
 * -----------------------------------------------------------------------------
 */


/** The first audio frame received and decoded from the remote user.

 @param engine  The AgoraRtcEngineKit object.
 @param uid     Remote user ID.
 @param elapsed Time elapsed (ms) from calling `joinChannelByToken` until this callback is triggered.
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine firstRemoteAudioFrameOfUid:(NSUInteger)uid elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:firstRemoteAudioFrameOfUid:elapsed:));


/**
 * @brief Occurs when the SDK receives the first audio frame from a specific remote user.
 *
 * **Deprecated**  from v3.0.0. Use `AgoraAudioRemoteStateDecoding(2)` in the 
 * [remoteAudioStateChangedOfUid]([AgoraRtcEngineDelegate rtcEngine:remoteAudioStateChangedOfUid:state:reason:elapsed:]) 
 * callback instead.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID of the remote user.
 * @param elapsed The time elapsed (ms) from the local user calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until the SDK triggers this
 * callback.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine firstRemoteAudioFrameDecodedOfUid:(NSUInteger)uid elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:firstRemoteAudioFrameDecodedOfUid:elapsed:)) __deprecated_msg("use rtcEngine:remoteAudioStateChangedOfUid:state:reason:elapsed: instead.");

/**
 * @brief Reports the transport-layer statistics of each remote audio stream.
 *
 * @details
 * The SDK triggers this callback once every two seconds for each remote user who is sending audio
 * streams. If a channel includes multiple remote users, the SDK triggers this callback as many
 * times.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param stats The statistics of the received remote audio streams. See `AgoraRtcRemoteAudioStats`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine remoteAudioStats:(AgoraRtcRemoteAudioStats * _Nonnull)stats NS_SWIFT_NAME(rtcEngine(_:remoteAudioStats:));

/**
 * @brief Occurs when a remote user (in the communication profile) or a host (in the live streaming
 * profile) stops/resumes sending the audio stream.
 *
 * @details
 * The SDK triggers this callback when the remote user stops or resumes sending the audio stream by
 * calling the `muteLocalAudioStream:` method.
 *
 * @note This callback does not work properly when the number of users (in the communication
 * profile) or hosts (in the live streaming channel) in a channel exceeds 32.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID.
 * @param muted Whether the remote user's audio stream is muted:
 * - `YES`: User's audio stream is muted.
 * - `NO`: User's audio stream is unmuted.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didAudioMuted:(BOOL)muted byUid:(NSUInteger)uid NS_SWIFT_NAME(rtcEngine(_:didAudioMuted:byUid:));


/**
 * @brief Reports the volume information of users.
 *
 * @details
 * By default, this callback is disabled. You can enable it by calling
 * `enableAudioVolumeIndication:smooth:reportVad:`. Once this callback is enabled and users send
 * streams in the channel, the SDK triggers the
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback according to the time
 * interval set in `enableAudioVolumeIndication:smooth:reportVad:`. The SDK triggers two independent
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callbacks simultaneously, which
 * separately report the volume information of the local user who sends a stream and the remote
 * users (up to three) whose instantaneous volume is the highest.
 *
 * @note
 * After receiving this callback, if you need to update the UI interface, ensure that the execution
 * thread is switched to the UI thread.
 * Once this callback is enabled, if the local user calls the `muteLocalAudioStream:` method to
 * mute, the SDK continues to report the volume indication of the local user.
 * If a remote user whose volume is one of the three highest in the channel stops publishing the
 * audio stream for 20 seconds, the callback excludes this user's information; if all remote users
 * stop publishing audio streams for 20 seconds, the SDK stops triggering the callback for remote
 * users.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param speakers The volume information of the users. See `AgoraRtcAudioVolumeInfo`. An empty
 * `speakers` array in the callback indicates that no remote user is in the channel or is sending a
 * stream.
 * @param totalVolume The volume of the speaker. The value range is [0,255].
 * - In the callback for the local user, `totalVolume` is the volume of the local user who sends a
 * stream.
 * - In the callback for remote users, `totalVolume` is the sum of the volume of all remote users
 * (up to three) whose instantaneous volume is the highest. If the user calls
 * `startAudioMixing:loopback:cycle:startPos:`, then `totalVolume` is the volume after audio mixing.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine reportAudioVolumeIndicationOfSpeakers:(NSArray<AgoraRtcAudioVolumeInfo *> * _Nonnull)speakers totalVolume:(NSInteger)totalVolume NS_SWIFT_NAME(rtcEngine(_:reportAudioVolumeIndicationOfSpeakers:totalVolume:));


/**
 * @brief Occurs when the most active remote speaker is detected.
 *
 * @details
 * After a successful call of `enableAudioVolumeIndication:smooth:reportVad:`, the SDK continuously
 * detects which remote user has the loudest volume. During the current period, the remote user
 * whose volume is detected as the loudest for the most times, is the most active user.
 * When the number of users is no less than two and an active remote speaker exists, the SDK
 * triggers this callback and reports the `uid` of the most active remote speaker.
 * - If the most active remote speaker is always the same user, the SDK triggers the
 * `rtcEngine:activeSpeaker:` callback only once.
 * - If the most active remote speaker changes to another user, the SDK triggers this callback again
 * and reports the `uid` of the new active remote speaker.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param speakerUid The user ID of the most active speaker.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine activeSpeaker:(NSUInteger)speakerUid NS_SWIFT_NAME(rtcEngine(_:activeSpeaker:));

 /**
 * @brief Reports the statistics of the audio stream sent by each remote user.
 *
 * @deprecated Use [rtcEngine:remoteAudioStats:] instead.
 *
 * @details
 * The SDK triggers this callback once every two seconds to report the audio quality of each remote
 * user who is sending an audio stream. If a channel has multiple users sending audio streams, the
 * SDK triggers this callback as many times.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID of the remote user sending the audio stream.
 * @param quality Audio quality of the user. See `AgoraNetworkQuality`.
 * @param delay The network delay (ms) from the sender to the receiver, including the delay caused
 * by audio sampling pre-processing, network transmission, and network jitter buffering.
 * @param lost The packet loss rate (%) of the audio packet sent from the remote user to the
 * receiver.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine audioQualityOfUid:(NSUInteger)uid quality:(AgoraNetworkQuality)quality delay:(NSUInteger)delay lost:(NSUInteger)lost NS_SWIFT_NAME(rtcEngine(_:audioQualityOfUid:quality:delay:lost:)) __deprecated_msg("use rtcEngine:remoteAudioStats: instead");

/**
 * @brief Reports the transport-layer statistics of each remote audio stream.
 *
 * @deprecated Use [rtcEngine:remoteAudioStats:] instead.
 *
 * @details
 * This callback reports the transport-layer statistics, such as the packet loss rate and network
 * time delay after the local user receives an audio packet from a remote user. During a call, when
 * the user receives the audio packet sent by the remote user, the callback is triggered every 2
 * seconds.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the remote user sending the audio streams.
 * @param delay The network delay (ms) from the remote user to the receiver.
 * @param lost The packet loss rate (%) of the audio packet sent from the remote user to the
 * receiver.
 * @param rxKBitrate The bitrate of the received audio (Kbps).
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine audioTransportStatsOfUid:(NSUInteger)uid delay:(NSUInteger)delay lost:(NSUInteger)lost rxKBitRate:(NSUInteger)rxKBitRate NS_SWIFT_NAME(rtcEngine(_:audioTransportStatsOfUid:delay:lost:rxKBitRate:)) __deprecated_msg("use rtcEngine:remoteAudioStats: instead");


/** Intra request received.
 * @param engine  The AgoraRtcEngineKit object.
*/
- (void)rtcEngineIntraRequestReceived:(AgoraRtcEngineKit *_Nonnull)engine NS_SWIFT_NAME(rtcEngineIntraRequestReceived(_:));

/**
 * @brief Occurs when the uplink network information changes.
 *
 * @details
 * The SDK triggers this callback when the uplink network information changes.
 *
 * @note This callback only applies to scenarios where you push externally encoded video data in
 * H.264 format to the SDK.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param networkInfo The uplink network information. See `AgoraUplinkNetworkInfo`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine uplinkNetworkInfoUpdate:(AgoraUplinkNetworkInfo *_Nonnull)networkInfo NS_SWIFT_NAME(rtcEngine(_:uplinkNetworkInfoUpdate:));

/**
 * @brief Occurs when the audio subscribing state changes.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param channelId The channel name.
 * @param uid The user ID of the remote user.
 * @param oldState The previous subscribing status. See `AgoraStreamSubscribeState`.
 * @param newState The current subscribing status. See `AgoraStreamSubscribeState`.
 * @param elapseSinceLastState The time elapsed (ms) from the previous state to the current state.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine didAudioSubscribeStateChange:(NSString * _Nonnull)channelId
                uid:(unsigned int)uid
           oldState:(AgoraStreamSubscribeState)oldState
           newState:(AgoraStreamSubscribeState)newState
elapseSinceLastState:(int)elapseSinceLastState NS_SWIFT_NAME(rtcEngine(_:didAudioSubscribeStateChange:uid:oldState:newState:elapseSinceLastState:));

/**
 * @brief Occurs when the audio publishing state changes.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param channelId The channel name.
 * @param oldState The previous publishing state. See `AgoraStreamPublishState`.
 * @param newState The current publishing stat. See `AgoraStreamPublishState`.
 * @param elapseSinceLastState The time elapsed (ms) from the previous state to the current state.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine didAudioPublishStateChange:(NSString * _Nonnull)channelId
           oldState:(AgoraStreamPublishState)oldState
           newState:(AgoraStreamPublishState)newState
elapseSinceLastState:(int)elapseSinceLastState NS_SWIFT_NAME(rtcEngine(_:didAudioPublishStateChange:oldState:newState:elapseSinceLastState:));

#pragma mark String UID

/**
 * @brief Occurs when the local user registers a user account.
 *
 * @details
 * After the local user successfully calls `registerLocalUserAccountWithAppID:userAccount:` to
 * register the user account or calls
 * `joinChannelByToken:channelId:userAccount:mediaOptions:joinSuccess:` to join a channel, the SDK
 * triggers the callback and informs the local user's UID and User Account.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the local user.
 * @param userAccount The user account of the local user.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didLocalUserRegisteredWithUserId:(NSUInteger)uid userAccount:(NSString * _Nonnull)userAccount NS_SWIFT_NAME(rtcEngine(_:didLocalUserRegisteredWithUserId:userAccount:));

/**
 * @brief Occurs when the SDK gets the user ID and user account of the remote user.
 *
 * @details
 * After a remote user joins the channel, the SDK gets the UID and user account of the remote user,
 * caches them in a mapping table object, and triggers this callback on the local client.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID of the remote user.
 * @param userInfo The UserInfo object that contains the user ID and user account of the remote
 * user. See `AgoraUserInfo` for details.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didUserInfoUpdatedWithUserId:(NSUInteger)uid userInfo:(AgoraUserInfo* _Nonnull)userInfo NS_SWIFT_NAME(rtcEngine(_:didUserInfoUpdatedWithUserId:userInfo:));

#pragma mark Rhythm Player Delegates Methods
/**
 * @brief Occurs when the state of virtual metronome changes.
 *
 * @details
 * When the state of the virtual metronome changes, the SDK triggers this callback to report the
 * current state of the virtual metronome. This callback indicates the state of the local audio
 * stream and enables you to troubleshoot issues when audio exceptions occur.
 *
 * @param state For the current virtual metronome status, see `AgoraRhythmPlayerState`.
 * @param reason For the error codes and error messages related to virtual metronome errors, see
 * `AgoraRhythmPlayerReason`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didRhythmPlayerStateChanged:(AgoraRhythmPlayerState)state
        reason:(AgoraRhythmPlayerReason)reason NS_SWIFT_NAME(rtcEngine(_:didRhythmPlayerStateChanged:reason:));

#pragma mark  Local Video Transcoder Delegates Methods
/**
 * @brief Occurs when there's an error during the local video mixing.
 *
 * @details
 * When you fail to call `startLocalVideoTranscoder:` or `updateLocalTranscoderConfiguration:`, the
 * SDK triggers this callback to report the reason.
 *
 * @param stream The video streams that cannot be mixed during video mixing. See
 * `AgoraTranscodingVideoStream`.
 * @param errorCode The reason for local video mixing error. See `AgoraVideoTranscoderError`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didLocalVideoTranscoderErrorWithStream:(AgoraTranscodingVideoStream * _Nonnull )stream
        errorCode:(AgoraVideoTranscoderError)errorCode NS_SWIFT_NAME(rtcEngine(_:didLocalVideoTranscoderErrorWithStream:errorCode:));

#pragma mark Remote User Video Delegates Methods

/**
 * @brief Occurs when the first remote video frame is received and decoded.
 *
 *  @deprecated This callback is deprecated. Use [remoteVideoStateChangedOfUid](remoteVideoStateChangedOfUid:state:reason:elapsed:)
 * instead.
 *
 * @details
 * The SDK triggers this callback under one of the following circumstances:
 * - The remote user joins the channel and sends the video stream.
 * - The remote user stops sending the video stream and re-sends it after 15 seconds. Reasons for
 * such an interruption include:
 *   - The remote user leaves the channel.
 *   - The remote user drops offline.
 *   - The remote user calls `disableVideo` to disable video.
 *
 * @param uid The user ID of the remote user sending the video stream.
 * @param engine `AgoraRtcEngineKit` object.
 * @param size Video dimensions.
 * @param elapsed The time elapsed (ms) from the local user calling
 * `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until the SDK triggers this
 * callback.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine firstRemoteVideoDecodedOfUid:(NSUInteger)uid size:(CGSize)size elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:firstRemoteVideoDecodedOfUid:size:elapsed:)) __deprecated_msg("use rtcEngine:remoteVideoStateChangedOfUid:state:reason: instead.");

/**
 * @brief Occurs when the renderer receives the first frame of the remote video.
 *
 * @note This callback is only triggered when the video frame is rendered by the SDK; it will not be
 * triggered if the user employs custom video rendering.You need to implement this independently
 * using methods outside the SDK.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID of the remote user sending the video stream.
 * @param size Video dimensions.
 * @param elapsed The time elapsed (ms) from the local user calling
 * `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until the SDK triggers this
 * callback.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine firstRemoteVideoFrameOfUid:(NSUInteger)uid size:(CGSize)size elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:firstRemoteVideoFrameOfUid:size:elapsed:));

  /** @brief Occurs when the video size or rotation of a specified user changes.
  *
  * @param engine `AgoraRtcEngineKit` object.
  * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
  * @param uid The ID of the user whose video size or rotation changes. (The `uid`for the local user is 0. 
  * The video is the local user's video preview).
  * @param size Video dimensions.
  * @param rotation The rotation information. The value range is [0,360). 
  * Attention: On iOS, The parameter value is always 0.
  */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine videoSizeChangedOfSourceType:(AgoraVideoSourceType)sourceType uid:(NSUInteger)uid size:(CGSize)size rotation:(NSInteger)rotation NS_SWIFT_NAME(rtcEngine(_:videoSizeChangedOf:uid:size:rotation:));
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine contentInspectResult:(AgoraContentInspectResult)result NS_SWIFT_NAME(rtcEngine(_:contentInspectResult:));
/**
 * @brief Reports the result of taking a video snapshot.
 *
 * @details
 * After a successful `takeSnapshot:filePath:` method call, the SDK triggers this callback to report
 * whether the snapshot is successfully taken as well as the details for the snapshot taken.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID. One `uid` of 0 indicates the local user.
 * @param filePath The local path of the snapshot.
 * @param width The width (px) of the snapshot.
 * @param height The height (px) of the snapshot.
 * @param errCode The message that confirms success or gives the reason why the snapshot is not
 * successfully taken:
 * - 0: Success.
 * - < 0: Failure:
 *   - -1: The SDK fails to write data to a file or encode a JPEG image.
 *   - -2: The SDK does not find the video stream of the specified user within one second after the
 * `takeSnapshot:filePath:` method call succeeds. The possible reasons are: local capture stops,
 * remote end stops publishing, or video data processing is blocked.
 *   - -3: Calling the `takeSnapshot:filePath:` method too frequently.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine snapshotTaken:(NSUInteger)uid filePath:(NSString* _Nonnull)filePath width:(NSInteger)width height:(NSInteger)height errCode:(NSInteger)errCode NS_SWIFT_NAME(rtcEngine(_:snapshotTaken:filePath:width:height:errCode:));
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine audioMetadataReceived:(NSUInteger)uid metadata:(NSData * _Nonnull)metadata NS_SWIFT_NAME(rtcEngine(_:audioMetadataReceived:metadata:));
/**
 * @brief Occurs when the local video stream state changes.
 *
 * @details
 * When the status of the local video changes, the SDK triggers this callback to report the current
 * local video state and the reason for the state change.
 * Applicable scenarios: You can use this callback to stay updated on the state changes of the local
 * video stream, and take corresponding measures based on the reasons for the state changes, to
 * better manage and debug issues related to the video stream.
 * Call timing: - The SDK triggeres this callback under the following circumstances, with the
 * `state` as AgoraVideoLocalStateFailed, and the `reason` as
 * `AgoraLocalVideoStreamReasonCaptureFailure`:
 *   - The app switches to the background, and the system revokes the camera resource.
 *   - The camera starts normally, but does not output video frames for four consecutive seconds.
 * - When the camera outputs captured video frames, if the SDK detects 15 consecutive duplicate
 * video frames, it triggers this callback, with the `state` as `AgoraVideoLocalStateCapturing` and
 * the `reason` as `AgoraLocalVideoStreamReasonCaptureFailure`.Note:
 * - Note that the video frame duplication detection is only available for video frames with a
 * resolution greater than 200 × 200, a frame rate greater than or equal to 10 fps, and a bitrate
 * less than 20 Kbps.
 * - Normally, if there is an error in video capturing, the issue can be troubleshooted through the
 * `reason` parameter in this callback. However, on some devices, when there is an issue with
 * capturing (such as freezing), the Android system will not throw any error callbacks, so the SDK
 * cannot report the reason for the change in local video status. In this case, you can determine if
 * there is no video frame being captured by checking the following: this callback reports the
 * `state` as `AgoraVideoLocalStateCapturing` or `AgoraVideoLocalStateEncoding`, and the
 * `captureFrameRate` in the `rtcEngine:localVideoStats:sourceType:` callback is 0.
 *
 * @note
 * - Note that the video frame duplication detection is only available for video frames with a
 * resolution greater than 200 × 200, a frame rate greater than or equal to 10 fps, and a bitrate
 * less than 20 Kbps.
 * - Normally, if there is an error in video capturing, the issue can be troubleshooted through the
 * `reason` parameter in this callback. However, on some devices, when there is an issue with
 * capturing (such as freezing), the Android system will not throw any error callbacks, so the SDK
 * cannot report the reason for the change in local video status. In this case, you can determine if
 * there is no video frame being captured by checking the following: this callback reports the
 * `state` as `AgoraVideoLocalStateCapturing` or `AgoraVideoLocalStateEncoding`, and the
 * `captureFrameRate` in the `rtcEngine:localVideoStats:sourceType:` callback is 0.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 * @param state The state of the local video, see `AgoraVideoLocalState`.
 * @param reason The reasons for changes in local video state. See `AgoraLocalVideoStreamReason`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localVideoStateChangedOfState:(AgoraVideoLocalState)state reason:(AgoraLocalVideoStreamReason)reason sourceType:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(rtcEngine(_:localVideoStateChangedOf:reason:sourceType:));

/**
 * @brief Occurs when the remote video stream state changes.
 *
 * @note This callback does not work properly when the number of users (in the communication
 * profile) or hosts (in the live streaming channel) in a channel exceeds 32.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the remote user whose video state changes.
 * @param state The state of the remote video. See `AgoraVideoRemoteState`.
 * @param reason The reason for the remote video state change. See `AgoraVideoRemoteReason`.
 * @param elapsed Time elapsed (ms) from the local user calling the
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` method until the SDK triggers this
 * callback.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine remoteVideoStateChangedOfUid:(NSUInteger)uid state:(AgoraVideoRemoteState)state reason:(AgoraVideoRemoteReason)reason elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:remoteVideoStateChangedOfUid:state:reason:elapsed:));

/**
 * @brief Occurs when the remote audio state changes.
 *
 * @details
 * When the audio state of a remote user (in a voice/video call channel) or host (in a live
 * streaming channel) changes, the SDK triggers this callback to report the current state of the
 * remote audio stream.
 *
 * @note This callback does not work properly when the number of users (in the communication
 * profile) or hosts (in the live streaming channel) in a channel exceeds 32.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the remote user whose audio state changes.
 * @param state The state of the remote audio. See `AgoraAudioRemoteState`.
 * @param reason The reason of the remote audio state change. See `AgoraAudioRemoteReason`.
 * @param elapsed Time elapsed (ms) from the local user calling the
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` method until the SDK triggers this
 * callback.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine remoteAudioStateChangedOfUid:(NSUInteger)uid state:(AgoraAudioRemoteState)state reason:(AgoraAudioRemoteReason)reason elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:remoteAudioStateChangedOfUid:state:reason:elapsed:));

/**
 * @brief Occurs when the state of the media stream relay changes.
 *
 * @details
 * The SDK returns the state of the current media relay with any error message.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param state The state code. See `AgoraChannelMediaRelayState`.
 * @param code The error code of the channel media relay. See `AgoraChannelMediaRelayError`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine
channelMediaRelayStateDidChange:(AgoraChannelMediaRelayState)state
            error:(AgoraChannelMediaRelayError)error NS_SWIFT_NAME(rtcEngine(_:channelMediaRelayStateDidChange:error:));

/**
 * @brief Occurs when a remote user stops or resumes publishing the video stream.
 *
 * @details
 * When a remote user calls `muteLocalVideoStream:` to stop or resume publishing the video stream,
 * the SDK triggers this callback to report to the local user the state of the streams published by
 * the remote user.
 *
 * @note This callback can be inaccurate when the number of users (in the communication profile) or
 * hosts (in the live streaming profile) in a channel exceeds 32.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID of the remote user.
 * @param muted Whether the remote user stops publishing the video stream:
 * - `YES`: The remote user stops publishing the video stream.
 * - `NO`: The remote user resumes publishing the video stream.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didVideoMuted:(BOOL)muted byUid:(NSUInteger)uid NS_SWIFT_NAME(rtcEngine(_:didVideoMuted:byUid:));

/**
 * @brief Occurs when a remote user enables or disables the video module.
 *
 * @details
 * Once the video module is disabled, the user can only use a voice call. The user cannot send or
 * receive any video.
 * The SDK triggers this callback when a remote user enables or disables the video module by calling
 * the `enableVideo` or `disableVideo` method.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID of the remote user.
 * @param enabled - `YES`: The video module is enabled.
 * - `NO`: The video module is disabled.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didVideoEnabled:(BOOL)enabled byUid:(NSUInteger)uid NS_SWIFT_NAME(rtcEngine(_:didVideoEnabled:byUid:));

/**
 * @brief Occurs when a specific remote user enables/disables the local video capturing function.
 *
 * @deprecated
 *
 * @details
 * The SDK triggers this callback when the remote user resumes or stops capturing the video stream
 * by calling the `enableLocalVideo:` method.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID of the remote user.
 * @param enabled Whether the specified remote user enables/disables local video capturing:
 * - `YES`: The video module is enabled. Other users in the channel can see the video of this remote
 * user.
 * - `NO`: The video module is disabled. Other users in the channel can no longer receive the video
 * stream from this remote user, while this remote user can still receive the video streams from
 * other users.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didLocalVideoEnabled:(BOOL)enabled byUid:(NSUInteger)uid NS_SWIFT_NAME(rtcEngine(_:didLocalVideoEnabled:byUid:)) __deprecated_msg("use rtcEngine:remoteVideoStateChangedOfUid:state:reason: instead.");

/**
 * @brief Reports the proxy connection state.
 *
 * @details
 * You can use this callback to listen for the state of the SDK connecting to a proxy. For example,
 * when a user calls `setCloudProxy:` and joins a channel successfully, the SDK triggers this
 * callback to report the user ID, the proxy type connected, and the time elapsed fromthe user
 * calling `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until this callback is
 * triggered.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param channel The channel name.
 * @param uid The user ID.
 * @param proxyType The proxy type connected. See `AgoraProxyType`.
 * @param localProxyIp Reserved for future use.
 * @param elapsed The time elapsed (ms) from the user calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` until this callback is triggered.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine didProxyConnected:(NSString* _Nonnull)channel withUid:(NSUInteger)uid proxyType:(AgoraProxyType)proxyType localProxyIp:(NSString* _Nonnull)localProxyIp elapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:didProxyConnected:withUid:proxyType:localProxyIp:elapsed:));

/**
 *  Occurs when the remote user state is updated.
 *
 *  @param engine  The AgoraRtcEngineKit object.
 *  @param uid     Remote user ID.
 *  @param state   The remote user state.
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine remoteUserStateChangedOfUid:(NSUInteger)uid state:(NSUInteger)state NS_SWIFT_NAME(rtcEngine(_:remoteUserStateChangedOfUid:state:));

/**
 * @brief Reports the statistics of the video stream sent by each remote users.
 *
 * @details
 * Reports the statistics of the video stream from the remote users. The SDK triggers this callback
 * once every two seconds for each remote user. If a channel has multiple users/hosts sending video
 * streams, the SDK triggers this callback as many times.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param stats Statistics of the remote video stream. See `AgoraRtcRemoteVideoStats`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine remoteVideoStats:(AgoraRtcRemoteVideoStats * _Nonnull)stats NS_SWIFT_NAME(rtcEngine(_:remoteVideoStats:));


/**
 * @brief Occurs when the remote media stream falls back to the audio-only stream due to poor
 * network conditions or switches back to the video stream after the network conditions improve.
 *
 * @details
 * If you call `setRemoteSubscribeFallbackOption:` and set `option` to
 * `AgoraStreamFallbackOptionAudioOnly`, the SDK triggers this callback in the following situations:
 * - The downstream network condition is poor, and the subscribed video stream is downgraded to
 * audio-only stream.
 * - The downstream network condition has improved, and the subscribed stream has been restored to
 * video stream.
 *
 * @note Once the remote media stream switches to the low-quality video stream due to weak network
 * conditions, you can monitor the stream switch between a high-quality and low-quality stream in
 * the `rtcEngine:remoteVideoStats:` callback.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The user ID of the remote user.
 * @param isFallbackOrRecover - `YES`: The subscribed media stream falls back to audio-only due to
 * poor network conditions.
 * - `NO`: The subscribed media stream switches back to the video stream after the network
 * conditions improve.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didRemoteSubscribeFallbackToAudioOnly:(BOOL)isFallbackOrRecover byUid:(NSUInteger)uid NS_SWIFT_NAME(rtcEngine(_:didRemoteSubscribeFallbackToAudioOnly:byUid:));

/**
 * @brief Reports the transport-layer statistics of each remote video stream.
 *
 * @deprecated Use [rtcEngine:remoteVideoStats:] instead.
 *
 * @details
 * This callback reports the transport-layer statistics, such as the packet loss rate and network
 * time delay after the local user receives a video packet from a remote user.
 * During a call, when the user receives the video packet sent by the remote user/host, the callback
 * is triggered every 2 seconds.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid The ID of the remote user sending the video packets.
 * @param delay The network delay (ms) from the sender to the receiver.
 * @param lost The packet loss rate (%) of the video packet sent from the remote user.
 * @param rxKBitRate The bitrate of the received video (Kbps).
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine videoTransportStatsOfUid:(NSUInteger)uid delay:(NSUInteger)delay lost:(NSUInteger)lost rxKBitRate:(NSUInteger)rxKBitRate NS_SWIFT_NAME(rtcEngine(_:videoTransportStatsOfUid:delay:lost:rxKBitRate:)) __deprecated_msg("use rtcEngine:remoteVideoStats: instead");

/**
 * @brief Occurs when the video subscribing state changes.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param channel The channel name.
 * @param uid The user ID of the remote user.
 * @param oldState The previous subscribing status. See `AgoraStreamSubscribeState`.
 * @param newState The current subscribing status. See `AgoraStreamSubscribeState`.
 * @param elapseSinceLastState The time elapsed (ms) from the previous state to the current state.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine didVideoSubscribeStateChange:(NSString * _Nonnull)channelId
                uid:(unsigned int)uid
           oldState:(AgoraStreamSubscribeState)oldState
           newState:(AgoraStreamSubscribeState)newState
elapseSinceLastState:(int)elapseSinceLastState NS_SWIFT_NAME(rtcEngine(_:didVideoSubscribeStateChange:uid:oldState:newState:elapseSinceLastState:));

/**
 * @brief Occurs when the video publishing state changes.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param channelId The channel name.
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 * @param oldState The previous publishing state. See `AgoraStreamPublishState`.
 * @param newState The current publishing stat. See `AgoraStreamPublishState`.
 * @param elapseSinceLastState The time elapsed (ms) from the previous state to the current state.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine didVideoPublishStateChange:(NSString * _Nonnull)channelId
         sourceType:(AgoraVideoSourceType)sourceType
           oldState:(AgoraStreamPublishState)oldState
           newState:(AgoraStreamPublishState)newState
elapseSinceLastState:(int)elapseSinceLastState NS_SWIFT_NAME(rtcEngine(_:didVideoPublishStateChange:sourceType:oldState:newState:elapseSinceLastState:));

#pragma mark Stream Publish Delegate Methods

/**-----------------------------------------------------------------------------
 * @name Stream Publish Delegate Methods
 * -----------------------------------------------------------------------------
 */

/**-----------------------------------------------------------------------------
 * @name CDN Live Streaming Delegate Methods
 * -----------------------------------------------------------------------------
 */

/** Occurs when the state of the RTMP or RTMPS streaming changes.

The SDK triggers this callback to report the result of the local user calling the [addPublishStreamUrl](addPublishStreamUrl:transcodingEnabled:) or [removePublishStreamUrl](removePublishStreamUrl:) method.

This callback returns the URL and its current streaming state.

This callback indicates the state of the RTMP or RTMPS streaming. When exceptions occur, you can troubleshoot issues by referring to the detailed error descriptions in the `errorCode` parameter.

@param engine AgoraRtcEngineKit object.
@param url The CDN streaming URL.
@param state The RTMP or RTMPS streaming state: AgoraRtmpStreamingState.
@param reason The detailed error information for streaming: AgoraRtmpStreamingReason.
 */
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine rtmpStreamingChangedToState:(NSString* _Nonnull)url state:(AgoraRtmpStreamingState)state reason:(AgoraRtmpStreamingReason)reason NS_SWIFT_NAME(rtcEngine(_:rtmpStreamingChangedToState:state:reason:));

/**
 * @brief Reports events during the Media Push.
 *
 * @since v3.1.0
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param url The URL for Media Push.
 * @param eventCode The event code of Media Push. See `AgoraRtmpStreamingEvent`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine rtmpStreamingEventWithUrl:(NSString* _Nonnull)url eventCode:(AgoraRtmpStreamingEvent)eventCode NS_SWIFT_NAME(rtcEngine(_:rtmpStreamingEventWithUrl:eventCode:));

/**
 * @brief Occurs when the publisher's transcoding is updated.
 *
 * @details
 * When the `AgoraLiveTranscoding` class in the `startRtmpStreamWithTranscoding:transcoding:` method
 * updates, the SDK triggers the `rtcEngineTranscodingUpdated:` callback to report the update
 * information.
 *
 * @note If you call the `startRtmpStreamWithTranscoding:transcoding:` method to set the
 * `AgoraLiveTranscoding` class for the first time, the SDK does not trigger this callback.
 *
 * @param engine `AgoraRtcEngineKit` object.
 *
 */
- (void)rtcEngineTranscodingUpdated:(AgoraRtcEngineKit * _Nonnull)engine NS_SWIFT_NAME(rtcEngineTranscodingUpdated(_:));

/**
 * @brief Occurs when the playback state of the music file changes.
 *
 * @details
 * This callback occurs when the playback state of the music file changes, and reports the current
 * state and error code.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param state The playback state of the music file. See `AgoraAudioMixingStateType`.
 * @param reasonCode Error code. See `AgoraAudioMixingReasonCode`.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine audioMixingStateChanged:(AgoraAudioMixingStateType)state
                                                                    reasonCode:(AgoraAudioMixingReasonCode)reasonCode NS_SWIFT_NAME(rtcEngine(_:audioMixingStateChanged:reasonCode:));

/**
 * @brief Reports the playback progress of a music file.
 *
 * @details
 * After you called the `startAudioMixing:loopback:cycle:startPos:` method to play a music file, the
 * SDK triggers this callback every two seconds to report the playback progress.
 *
 * @param position The playback progress (ms).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine audioMixingPositionChanged:(NSInteger)position NS_SWIFT_NAME(rtcEngine(_:audioMixingPositionChanged:));

#if TARGET_OS_IPHONE
#pragma mark Face Detection Delegate Methods
/**
 * @brief Reports the face detection result of the local user.
 *
 * **Since:** v3.0.1.
 * @details
 * Once you enable face detection by calling `enableFaceDetection:` `(YES)`, you can get the
 * following information on the local user in real-time:
 * - The width and height of the local video.
 * - The position of the human face in the local view.
 * - The distance between the human face and the screen.
 * This value is based on the fitting calculation of the local video size and the position of the
 * human face.
 *
 * @note
 * - When it is detected that the face in front of the camera disappears, the callback will be
 * triggered immediately. When no human face is detected, the frequency of this callback to be
 * triggered wil be decreased to reduce power consumption on the local device.
 * - The SDK stops triggering this callback when a human face is in close proximity to the screen.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param width The width (px) of the video image captured by the local camera.
 * @param height The height (px) of the video image captured by the local camera.
 * @param faces Information of the detected face. See `AgoraFacePositionInfo`. The number of
 * `AgoraFacePositionInfo` array reported in this callback is based on the faces detected. The
 * length of the array can be 0, which means that no human face is detected in front of the camera.
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine facePositionDidChangeWidth:(int)width previewHeight:(int)height faces:(NSArray<AgoraFacePositionInfo*>* _Nullable)faces  NS_SWIFT_NAME(rtcEngine(_:facePositionDidChangeWidth:previewHeight:faces:));
#endif

#pragma mark  Remote Video Layout Info Delegates Methods
/**
 * @brief Occurs when the local user receives a mixed video stream carrying layout information.
 *
 * @details
 * When the local user receives a mixed video stream sent by the video mixing server for the first
 * time, or when there is a change in the layout information of the mixed stream, the SDK triggers
 * this callback, reporting the layout information of each sub-video stream within the mixed video
 * stream.
 *
 * @param engine `AgoraRtcEngineKit` object.
 * @param uid User ID who published this mixed video stream.
 * @param videoLayoutInfo Layout information of a specific sub-video stream within the mixed stream.
 * See . `AgoraVideoLayoutInfo`
 *
 */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didTranscodedStreamLayoutInfoUpdatedWithUserId:(NSUInteger)uid videoLayoutInfo:(AgoraVideoLayoutInfo* _Nonnull)videoLayoutInfo NS_SWIFT_NAME(rtcEngine(_:didTranscodedStreamLayoutInfoUpdatedWithUserId:videoLayoutInfo:));

/**
  * @brief Report the multipath transmission statistics
  *
  * @post This callback is triggered after you set `enableMultipath` to `true` to enable multipath transmission.
  *
  * @since 4.6.0
  *
  * @param stats The multipath statistics. See the MultipathStats structure for details.
  */
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine multiPathStats:(AgoraMultipathStats * _Nonnull)stats NS_SWIFT_NAME(rtcEngine(_:multiPathStats:));

#pragma mark - Unavailable Delegate Methods
typedef NSUInteger AgoraLocalVideoStreamError;
typedef NSUInteger AgoraAudioLocalError;
typedef NSUInteger AgoraRhythmPlayerError;
typedef NSUInteger AgoraRtmpStreamingErrorCode;

#if TARGET_OS_IPHONE
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine rtmpStreamingChangedToState:(NSString* _Nonnull)url state:(AgoraRtmpStreamingState)state errCode:(AgoraRtmpStreamingErrorCode)errCode NS_SWIFT_NAME(rtcEngine(_:rtmpStreamingChangedToState:state:errCode:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:rtmpStreamingChangedToState:state:reason: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didRhythmPlayerStateChanged:(AgoraRhythmPlayerState)state errorCode:(AgoraRhythmPlayerError)errorCode NS_SWIFT_NAME(rtcEngine(_:didRhythmPlayerStateChanged:errorCode:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:didRhythmPlayerStateChanged:reason: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localAudioStateChanged:(AgoraAudioLocalState)state error:(AgoraAudioLocalError)error NS_SWIFT_NAME(rtcEngine(_:localAudioStateChanged:error:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:localAudioStateChanged:reason: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localVideoStateChangedOfState:(AgoraVideoLocalState)state error:(AgoraLocalVideoStreamError)error NS_SWIFT_NAME(rtcEngine(_:localVideoStateChangedOf:error:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:localVideoStateChangedOfState:reason:sourceType: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localVideoStateChangedOfState:(AgoraVideoLocalState)state error:(AgoraLocalVideoStreamError)error sourceType:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(rtcEngine(_:localVideoStateChangedOf:error:sourceType:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:localVideoStateChangedOfState:reason:sourceType: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localVideoStats:(AgoraRtcLocalVideoStats * _Nonnull)stats NS_SWIFT_NAME(rtcEngine(_:localVideoStats:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:localVideoStats:sourceType instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine didVideoPublishStateChange:(NSString * _Nonnull)channelId
         oldState:(AgoraStreamPublishState)oldState
         newState:(AgoraStreamPublishState)newState
elapseSinceLastState:(int)elapseSinceLastState NS_SWIFT_NAME(rtcEngine(_:didVideoPublishStateChange:oldState:newState:elapseSinceLastState:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:didVideoPublishStateChange:sourceType:oldState:newState:elapseSinceLastState: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine firstLocalVideoFramePublishedWithElapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:firstLocalVideoFramePublishedWithElapsed:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:firstLocalVideoFramePublishedWithElapsed:sourceType: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine videoSizeChangedOfUid:(NSUInteger)uid size:(CGSize)size rotation:(NSInteger)rotation NS_SWIFT_NAME(rtcEngine(_:videoSizeChangedOfUid:size:rotation:)) __attribute__((availability(ios,deprecated=7_0,message="Use rtcEngine:videoSizeChangedOfSourceType:uid:size:rotation instead.")));
#endif

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
- (void)rtcEngine:(AgoraRtcEngineKit* _Nonnull)engine rtmpStreamingChangedToState:(NSString* _Nonnull)url state:(AgoraRtmpStreamingState)state errCode:(AgoraRtmpStreamingErrorCode)errCode NS_SWIFT_NAME(rtcEngine(_:rtmpStreamingChangedToState:state:errCode:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:rtmpStreamingChangedToState:state:reason: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine didRhythmPlayerStateChanged:(AgoraRhythmPlayerState)state errorCode:(AgoraRhythmPlayerError)errorCode NS_SWIFT_NAME(rtcEngine(_:didRhythmPlayerStateChanged:errorCode:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:didRhythmPlayerStateChanged:reason: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localAudioStateChanged:(AgoraAudioLocalState)state error:(AgoraAudioLocalError)error NS_SWIFT_NAME(rtcEngine(_:localAudioStateChanged:error:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:localAudioStateChanged:reason: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localVideoStateChangedOfState:(AgoraVideoLocalState)state error:(AgoraLocalVideoStreamError)error NS_SWIFT_NAME(rtcEngine(_:localVideoStateChangedOf:error:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:localVideoStateChangedOfState:reason:sourceType: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localVideoStateChangedOfState:(AgoraVideoLocalState)state error:(AgoraLocalVideoStreamError)error sourceType:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(rtcEngine(_:localVideoStateChangedOf:error:sourceType:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:localVideoStateChangedOfState:reason:sourceType: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine localVideoStats:(AgoraRtcLocalVideoStats * _Nonnull)stats NS_SWIFT_NAME(rtcEngine(_:localVideoStats:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:localVideoStats:sourceType instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit *_Nonnull)engine didVideoPublishStateChange:(NSString * _Nonnull)channelId
         oldState:(AgoraStreamPublishState)oldState
         newState:(AgoraStreamPublishState)newState
elapseSinceLastState:(int)elapseSinceLastState NS_SWIFT_NAME(rtcEngine(_:didVideoPublishStateChange:oldState:newState:elapseSinceLastState:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:didVideoPublishStateChange:sourceType:oldState:newState:elapseSinceLastState: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine firstLocalVideoFramePublishedWithElapsed:(NSInteger)elapsed NS_SWIFT_NAME(rtcEngine(_:firstLocalVideoFramePublishedWithElapsed:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:firstLocalVideoFramePublishedWithElapsed:sourceType: instead.")));
- (void)rtcEngine:(AgoraRtcEngineKit * _Nonnull)engine videoSizeChangedOfUid:(NSUInteger)uid size:(CGSize)size rotation:(NSInteger)rotation NS_SWIFT_NAME(rtcEngine(_:videoSizeChangedOfUid:size:rotation:)) __attribute__((availability(macos,deprecated=10_9,message="Use rtcEngine:videoSizeChangedOfSourceType:uid:size:rotation instead.")));
#endif
@end