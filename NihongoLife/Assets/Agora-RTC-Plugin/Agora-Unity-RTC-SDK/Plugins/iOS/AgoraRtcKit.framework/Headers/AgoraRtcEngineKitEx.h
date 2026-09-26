//
//  AgoraRtcEngineKitEx.h
//  AgoraRtcEngineKit
//
//  Copyright (c) 2020 Agora. All rights reserved.
//  Created by LLF on 2020/3/9.
//

#import "AgoraRtcEngineKit.h"
#import "AgoraObjects.h"


NS_ASSUME_NONNULL_BEGIN
@interface AgoraRtcEngineKit(Ex)

/**
 * @brief Join a channel using a token, and set the media options.
 *
 * @details
 * Before calling this method, if you have not called
 * `registerLocalUserAccountWithAppID:userAccount:` to register a user account, when you call this
 * method to join a channel, the SDK automatically creates a user account for you. Calling the
 * `registerLocalUserAccountWithAppID:userAccount:` method to register a user account, and then
 * calling this method to join a channel can shorten the time it takes to enter the channel.
 * Once a user joins the channel, the user subscribes to the audio and video streams of all the
 * other users in the channel by default, giving rise to usage and **billings**. If you want to stop
 * subscribing to the media stream of other users, you can set the `mediaOptions` parameter or call
 * the corresponding `mute` method.
 * Call timing: Call this method after `sharedEngineWithConfig:delegate:`.
 * Related callbacks: After the user successfully joins the channel, the SDK triggers the following
 * callbacks:
 * - The local client: `rtcEngine:didLocalUserRegisteredWithUserId:userAccount:`,
 * `rtcEngine:didJoinChannel:withUid:elapsed:` and `rtcEngine:connectionChangedToState:reason:`
 * callbacks.
 * - The remote client: The `rtcEngine:didJoinedOfUid:elapsed:` and
 * `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:` callbacks if a user joins the channel in the
 * COMMUNICATION profile, or if a host joins the channel in the LIVE_BROADCASTING profile.
 *
 * @note
 * - This method only supports users joining one channel at a time.
 * - Users with different App IDs cannot call each other.
 * - Before joining a channel, ensure that the App ID you use to generate a token is the same as
 * that you pass in the `sharedEngineWithConfig:delegate:` method; otherwise, you may fail to join
 * the channel with the token.
 * To ensure smooth communication, use the same parameter type to identify the user. For example, if
 * a user joins the channel with a UID, then ensure all the other users use the UID too. The same
 * applies to the user account. If a user joins the channel with the Agora Web SDK, ensure that the
 * ID of the user is set to the same parameter type.
 *
 * @param token The token generated on your server for authentication.Note:
 * - (Recommended) If your project has enabled the security mode (using APP ID and Token for
 * authentication), this parameter is required.
 * - If you have only enabled the testing mode (using APP ID for authentication), this parameter is
 * optional. You will automatically exit the channel 24 hours after successfully joining in.
 * - If you need to join different channels at the same time or switch between channels, Agora
 * recommends using a wildcard token so that you don't need to apply for a new token every time
 * joining a channel. See `Secure authentication with tokens`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 * @param delegate The callback class of `AgoraRtcEngine`. See `AgoraRtcEngineDelegate`. You can 
 * get the callback events of multiple channels through the `delegte` object passed in this parameter.
 * @param mediaOptions The channel media options. See `AgoraRtcChannelMediaOptions`.
 * @param joinSuccessBlock Occurs when a user joins a channel. `joinSuccessBlock` takes higher
 * priority than `rtcEngine:didJoinChannel:withUid:elapsed:`. When both are implemented,
 * `rtcEngine:didJoinChannel:withUid:elapsed:` does not take effect. Agora recommends setting
 * `joinSuccessBlock` as `nil` to use `rtcEngine:didJoinChannel:withUid:elapsed:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid. For example, the token is invalid, the `uid` parameter is not
 * set to an integer, or the value of a member in `AgoraRtcChannelMediaOptions` is invalid. You need
 * to pass in a valid parameter and join the channel again.
 *   - -3: Fails to initialize the `AgoraRtcEngineKit` object. You need to reinitialize the
 * `AgoraRtcEngineKit` object.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 *   - -8: The internal state of the `AgoraRtcEngineKit` object is wrong. The typical cause is that
 * after calling `startEchoTestWithConfig:` to start a call loop test, you call this method to join
 * the channel without calling `stopEchoTest` to stop the test. You need to call `stopEchoTest`
 * before calling this method.
 *   - -17: The request to join the channel is rejected. The typical cause is that the user is
 * already in the channel. Agora recommends that you use the
 * `rtcEngine:connectionChangedToState:reason:` callback to see whether the user is in the channel.
 * Do not call this method to join the channel unless you receive the
 * `AgoraConnectionStateDisconnected` (1) state.
 *   - -102: The channel name is invalid. You need to pass in a valid channel name in `channelId` to
 * rejoin the channel.
 *   - -121: The user ID is invalid. You need to pass in a valid user ID in `uid` to rejoin the
 * channel.
 */
- (int)joinChannelExByToken:(NSString* _Nullable)token
                 connection:(AgoraRtcConnection * _Nonnull)connection
                   delegate:(id<AgoraRtcEngineDelegate> _Nullable)delegate
               mediaOptions:(AgoraRtcChannelMediaOptions* _Nonnull)mediaOptions
                joinSuccess:(void(^ _Nullable)(NSString* _Nonnull channel, NSUInteger uid, NSInteger elapsed))joinSuccessBlock NS_SWIFT_NAME(joinChannelEx(byToken:connection:delegate:mediaOptions:joinSuccess:));

/**
 * @brief Join a channel using a user account and token, and set the media options.
 *
 * @details
 * Before calling this method, if you have not called
 * `registerLocalUserAccountWithAppID:userAccount:` to register a user account, when you call this
 * method to join a channel, the SDK automatically creates a user account for you. Calling the
 * `registerLocalUserAccountWithAppID:userAccount:` method to register a user account, and then
 * calling this method to join a channel can shorten the time it takes to enter the channel.
 * Once a user joins the channel, the user subscribes to the audio and video streams of all the
 * other users in the channel by default, giving rise to usage and **billings**. If you want to stop
 * subscribing to the media stream of other users, you can set the `mediaOptions` parameter or call
 * the corresponding `mute` method.
 * Call timing: Call this method after `sharedEngineWithConfig:delegate:`.
 * Related callbacks: After the user successfully joins the channel, the SDK triggers the following
 * callbacks:
 * - The local client: `rtcEngine:didLocalUserRegisteredWithUserId:userAccount:`,
 * `rtcEngine:didJoinChannel:withUid:elapsed:` and `rtcEngine:connectionChangedToState:reason:`
 * callbacks.
 * - The remote client: The `rtcEngine:didJoinedOfUid:elapsed:` and
 * `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:` callbacks if a user joins the channel in the
 * COMMUNICATION profile, or if a host joins the channel in the LIVE_BROADCASTING profile.
 *
 * @note
 * - This method only supports users joining one channel at a time.
 * - Users with different App IDs cannot call each other.
 * - Before joining a channel, ensure that the App ID you use to generate a token is the same as
 * that you pass in the `sharedEngineWithConfig:delegate:` method; otherwise, you may fail to join
 * the channel with the token.
 * To ensure smooth communication, use the same parameter type to identify the user. For example, if
 * a user joins the channel with a UID, then ensure all the other users use the UID too. The same
 * applies to the user account. If a user joins the channel with the Agora Web SDK, ensure that the
 * ID of the user is set to the same parameter type.
 *
 * @param token The token generated on your server for authentication.Note:
 * - (Recommended) If your project has enabled the security mode (using APP ID and Token for
 * authentication), this parameter is required.
 * - If you have only enabled the testing mode (using APP ID for authentication), this parameter is
 * optional. You will automatically exit the channel 24 hours after successfully joining in.
 * - If you need to join different channels at the same time or switch between channels, Agora
 * recommends using a wildcard token so that you don't need to apply for a new token every time
 * joining a channel. See `Secure authentication with tokens`.
 * @param channelId The channel name. This parameter signifies the channel in which users engage in
 * real-time audio and video interaction. Under the premise of the same App ID, users who fill in
 * the same channel ID enter the same channel for audio and video interaction. The string length
 * must be less than 64 bytes. Supported characters (89 characters in total):
 * - All lowercase English letters: a to z.
 * - All uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]",
 * "^", "_", "{", "}", "|", "~", ","
 * @param userAccount The user account. This parameter is used to identify the user in the channel
 * for real-time audio and video engagement. You need to set and manage user accounts yourself and
 * ensure that each user account in the same channel is unique. The maximum length of this parameter
 * is 255 bytes. Ensure that you set this parameter and do not set it as nil. Supported characters
 * are as follows(89 in total):
 * - The 26 lowercase English letters: a to z.
 * - The 26 uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - Space
 * - "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]",
 * "^", "_", "{", "}", "|", "~", ","
 * @param delegate The callback class of `AgoraRtcEngine`. See `AgoraRtcEngineDelegate`. You can 
 * get the callback events of multiple channels through the `delegte` object passed in this parameter.
 * @param mediaOptions The channel media options. See `AgoraRtcChannelMediaOptions`.
 * @param joinSuccessBlock Occurs when a user joins a channel. `joinSuccessBlock` takes higher
 * priority than `rtcEngine:didJoinChannel:withUid:elapsed:`. When both are implemented,
 * `rtcEngine:didJoinChannel:withUid:elapsed:` does not take effect. Agora recommends setting
 * `joinSuccessBlock` as `nil` to use `rtcEngine:didJoinChannel:withUid:elapsed:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid. For example, the token is invalid, the `uid` parameter is not
 * set to an integer, or the value of a member in `AgoraRtcChannelMediaOptions` is invalid. You need
 * to pass in a valid parameter and join the channel again.
 *   - -3: Fails to initialize the `AgoraRtcEngineKit` object. You need to reinitialize the
 * `AgoraRtcEngineKit` object.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 *   - -8: The internal state of the `AgoraRtcEngineKit` object is wrong. The typical cause is that
 * after calling `startEchoTestWithConfig:` to start a call loop test, you call this method to join
 * the channel without calling `stopEchoTest` to stop the test. You need to call `stopEchoTest`
 * before calling this method.
 *   - -17: The request to join the channel is rejected. The typical cause is that the user is
 * already in the channel. Agora recommends that you use the
 * `rtcEngine:connectionChangedToState:reason:` callback to see whether the user is in the channel.
 * Do not call this method to join the channel unless you receive the
 * `AgoraConnectionStateDisconnected` (1) state.
 *   - -102: The channel name is invalid. You need to pass in a valid channel name in `channelId` to
 * rejoin the channel.
 *   - -121: The user ID is invalid. You need to pass in a valid user ID in `uid` to rejoin the
 * channel.
 */
- (int)joinChannelExByToken:(NSString* _Nullable)token
                  channelId:(NSString* _Nonnull)channelId
                userAccount:(NSString* _Nonnull)userAccount
                   delegate:(id<AgoraRtcEngineDelegate> _Nullable)delegate
               mediaOptions:(AgoraRtcChannelMediaOptions* _Nonnull)mediaOptions
                joinSuccess:(void(^ _Nullable)(NSString* _Nonnull channel, NSUInteger uid, NSInteger elapsed))joinSuccessBlock NS_SWIFT_NAME(joinChannelEx(byToken:channelId:userAccount:delegate:mediaOptions:joinSuccess:));

/**
 * @brief Updates the channel media options after joining the channel.
 *
 * @param options The channel media options. See `AgoraRtcChannelMediaOptions`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The value of a member in `AgoraRtcChannelMediaOptions` is invalid. For example, the token
 * or the user ID is invalid. You need to fill in a valid parameter.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 *   - -8: The internal state of the `AgoraRtcEngineKit` object is wrong. The possible reason is
 * that the user is not in the channel. Agora recommends that you use the
 * `rtcEngine:connectionChangedToState:reason:` callback to see whether the user is in the channel.
 * If you receive the `AgoraConnectionStateDisconnected` (1) or `AgoraConnectionStateFailed` (5)
 * state, the user is not in the channel. You need to call
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join a channel before calling
 * this method.
 */
- (int)updateChannelExWithMediaOptions:(AgoraRtcChannelMediaOptions* _Nonnull)mediaOptions
                            connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(updateChannelEx(with:connection:));

/**
 * @brief Leaves a channel.
 *
 * @details
 * After calling this method, the SDK terminates the audio and video interaction, leaves the current
 * channel, and releases all resources related to the session.
 * After calling `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:` to join a
 * channel, you must call this method or `leaveChannelEx:options:leaveChannelBlock:` to end the
 * call, otherwise, the next call cannot be started.
 * Applicable scenarios: This method can be called in scenarios involving multiple channels.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 * Related callbacks: A successful call of this method triggers the following callbacks:
 * - The local client: The `rtcEngine:didLeaveChannelWithStats:` callback will be triggered.
 * - The remote client: The `rtcEngine:didOfflineOfUid:reason:` callback will be triggered after the
 * remote host leaves the channel.
 *
 * @note
 * If you call `destroy` immediately after calling this method, the SDK does not trigger the
 * `rtcEngine:didLeaveChannelWithStats:` callback.
 * - This method call is asynchronous. When this method returns, it does not necessarily mean that
 * the user has left the channel.
 * - If you call `leaveChannel:` or `leaveChannel:leaveChannelBlock:`, you will leave all the
 * channels you have joined by calling `joinChannelByToken:channelId:info:uid:joinSuccess:`,
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`, or
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 * @param leaveChannelBlock This callback indicates that a user leaves a channel, and provides the
 * statistics of the call in `AgoraChannelStats`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)leaveChannelEx:(AgoraRtcConnection * _Nonnull)connection
    leaveChannelBlock:(void(^ _Nullable)(AgoraChannelStats* _Nonnull stat))leaveChannelBlock NS_SWIFT_NAME(leaveChannelEx(_:leaveChannelBlock:));

/**
 * Leaves a channel with the channel ID and user account.
 *
 * @param channelId The channel name. The maximum length of this parameter is 64 bytes. Supported character scopes are:
 * - All lowercase English letters: a to z.
 * - All uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - The space character.
 * - Punctuation characters and other symbols, including: "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]", "^", "_", " {", "}", "|", "~", ",".
 * @param userAccount The user account. The maximum length of this parameter is 255 bytes. Ensure that you set this parameter and do not set it as null. Supported character scopes are:
 * - All lowercase English letters: a to z.
 * - All uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - The space character.
 * - Punctuation characters and other symbols, including: "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]", "^", "_", " {", "}", "|", "~", ",".
 * @param leaveChannelBlock This callback indicates that a user leaves a channel, and provides the statistics of the call in #AgoraChannelStats.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)leaveChannelEx:(NSString * _Nonnull)channelId
              userAccount:(NSString * _Nonnull)userAccount
    leaveChannelBlock:(void(^ _Nullable)(AgoraChannelStats* _Nonnull stat))leaveChannelBlock NS_SWIFT_NAME(leaveChannelEx(_:userAccount:leaveChannelBlock:));

/**
 * Resets the SDK delegate.
 *
 * The SDK uses the delegate to inform the app on engine runtime events. All methods defined in the
 * delegate are optional implementation methods.
 *
 * @param delegate The AgoraRtcEngineDelegate protocol.
 * @param connection The AgoraRtcConnection object.
 */
- (int)setDelegateEx:(id<AgoraRtcEngineDelegate> _Nullable)delegate connection:(AgoraRtcConnection * _Nonnull)connection;

/**
 * @brief Stops or resumes publishing the local audio stream.
 *
 * @details
 * A successful call of this method triggers the `rtcEngine:didAudioMuted:byUid:` and
 * `rtcEngine:remoteAudioStateChangedOfUid:state:reason:elapsed:` callbacks on the remote client.
 *
 * @note This method does not affect any ongoing audio recording, because it does not disable the
 * audio capture device.
 *
 * @param mute Whether to stop publishing the local audio stream:
 * - `YES`: Stops publishing the local audio stream.
 * - `NO`: (Default) Resumes publishing the local audio stream.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteLocalAudioStreamEx:(BOOL)mute connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(muteLocalAudioStreamEx(_:connection:));
/**
 * @brief Stops or resumes publishing the local video stream.
 *
 * @details
 * A successful call of this method triggers the `rtcEngine:didVideoMuted:byUid:` callback on the
 * remote client.
 *
 * @note - This method does not affect any ongoing video recording, because it does not disable the
 * camera.
 *
 * @param mute Whether to stop publishing the local video stream.
 * - `YES`: Stop publishing the local video stream.
 * - `NO`: (Default) Publish the local video stream.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteLocalVideoStreamEx:(BOOL)mute connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(muteLocalVideoStreamEx(_:connection:));

/**
 * @brief Sets channel options and leaves the channel.
 *
 * @details
 * After calling this method, the SDK terminates the audio and video interaction, leaves the current
 * channel, and releases all resources related to the session.
 * After calling `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:` to join a
 * channel, you must call this method or `leaveChannelEx:leaveChannelBlock:` to end the call,
 * otherwise, the next call cannot be started.
 * Applicable scenarios: This method can be called in scenarios involving multiple channels.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 * Related callbacks: A successful call of this method triggers the following callbacks:
 * - The local client: The `rtcEngine:didLeaveChannelWithStats:` callback will be triggered.
 * - The remote client: The `rtcEngine:didOfflineOfUid:reason:` callback will be triggered after the
 * remote host leaves the channel.
 *
 * @note
 * If you call `destroy` immediately after calling this method, the SDK does not trigger the
 * `rtcEngine:didLeaveChannelWithStats:` callback.
 * - This method call is asynchronous. When this method returns, it does not necessarily mean that
 * the user has left the channel.
 * - If you call `leaveChannel:` or `leaveChannel:leaveChannelBlock:`, you will leave all the
 * channels you have joined by calling `joinChannelByToken:channelId:info:uid:joinSuccess:`,
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`, or
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 * @param options Since
 * v4.1.0
 * The options for leaving the channel. See `AgoraLeaveChannelOptions`.
 * Note: This parameter only supports the `stopMicrophoneRecording` member in the
 * `AgoraLeaveChannelOptions` settings; setting other members does not take effect.
 * @param leaveChannelBlock This callback indicates that a user leaves a channel, and provides the
 * statistics of the call in `AgoraChannelStats`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)leaveChannelEx:(AgoraRtcConnection * _Nonnull)connection
              options:(AgoraLeaveChannelOptions * _Nonnull)options
    leaveChannelBlock:(void(^ _Nullable)(AgoraChannelStats* _Nonnull stat))leaveChannelBlock;

/**
 * Leaves a channel with the channel ID and user account and sets the options for leaving.
 *
 * @param channelId The channel name. The maximum length of this parameter is 64 bytes. Supported character scopes are:
 * - All lowercase English letters: a to z.
 * - All uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - The space character.
 * - Punctuation characters and other symbols, including: "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]", "^", "_", " {", "}", "|", "~", ",".
 * @param userAccount The user account. The maximum length of this parameter is 255 bytes. Ensure that you set this parameter and do not set it as null. Supported character scopes are:
 * - All lowercase English letters: a to z.
 * - All uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - The space character.
 * - Punctuation characters and other symbols, including: "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]", "^", "_", " {", "}", "|", "~", ",".
 * @param options The options for leaving the channel. See {@link LeaveChannelOptions}.
 * @param leaveChannelBlock This callback indicates that a user leaves a channel, and provides the statistics of the call in #AgoraChannelStats.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)leaveChannelEx:(NSString * _Nonnull)channelId
              userAccount:(NSString * _Nonnull)userAccount
              options:(AgoraLeaveChannelOptions * _Nonnull)options
    leaveChannelBlock:(void(^ _Nullable)(AgoraChannelStats* _Nonnull stat))leaveChannelBlock NS_SWIFT_NAME(leaveChannelEx(_:userAccount:options:leaveChannelBlock:));

/**
 * @brief Stops or resumes receiving the audio stream of a specified user.
 *
 * @details
 *             This method is used to stops or resumes receiving the audio stream of a specified
 * user. You can call this method before or after joining a channel. If a user leaves a channel, the
 * settings in this method become invalid.
 *
 * @param uid The ID of the specified user.
 * @param mute Whether to stop receiving the audio stream of the specified user:
 * - `YES`: Stop receiving the audio stream of the specified user.
 * - `NO`: (Default) Resume receiving the audio stream of the specified user.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteRemoteAudioStreamEx:(NSUInteger)uid
                          mute:(BOOL)mute
                    connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(muteRemoteAudioStreamEx(_:mute:connection:));

/**
 * @brief Stops or resumes subscribing to the audio streams of all remote users.
 *
 * @details
 * After successfully calling this method, the local user stops or resumes subscribing to the audio
 * streams of all remote users, including the ones join the channel subsequent to this call.
 *
 * @note
 * - Call this method after joining a channel.
 * - If you do not want to subscribe the audio streams of remote users before joining a channel, you
 * can set `autoSubscribeAudio` as `NO` when calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`.
 *
 * @param mute Whether to stop subscribing to the audio streams of all remote users:
 * - `YES`: Stops subscribing to the audio streams of all remote users.
 * - `NO`: (Default) Subscribes to the audio streams of all remote users by default.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteAllRemoteAudioStreamsEx:(BOOL)mute
                        connection:(AgoraRtcConnection * _Nonnull)connection  NS_SWIFT_NAME(muteAllRemoteAudioStreamsEx(_:connection:));

/**
 * @brief Sets the video encoder configuration.
 *
 * @details
 * Sets the encoder configuration for the local video. Each configuration profile corresponds to a
 * set of video parameters, including the resolution, frame rate, and bitrate.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 *
 * @note The `config` specified in this method is the maximum value under ideal network conditions.
 * If the video engine cannot render the video using the specified `config` due to unreliable
 * network conditions, the parameters further down the list are considered until a successful
 * configuration is found.
 *
 * @param config Video profile. See `AgoraVideoEncoderConfiguration`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVideoEncoderConfigurationEx:(AgoraVideoEncoderConfiguration* _Nonnull)config
                           connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setVideoEncoderConfigurationEx(_:connection:));

/**
 * @brief Initializes the video view of a remote user.
 *
 * @details
 * This method initializes the video view of a remote stream on the local device. It affects only
 * the video view that the local user sees. Call this method to bind the remote video stream to a
 * video view and to set the rendering and mirror modes of the video view.
 * The application specifies the uid of the remote video in the `AgoraRtcVideoCanvas` method before
 * the remote user joins the channel.
 * If the remote uid is unknown to the application, set it after the application receives the
 * `rtcEngine:didJoinedOfUid:elapsed:` callback. If the Video Recording function is enabled, the
 * Video Recording Service joins the channel as a dummy client, causing other clients to also
 * receive the `onUserJoined` callback. Do not bind the dummy client to the application view because
 * the dummy client does not send any video streams.
 * To unbind the remote user from the view, set the `view` parameter to NULL.
 * Once the remote user leaves the channel, the SDK unbinds the remote user.
 *
 * @note
 * - Call this method after `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 * - To update the rendering or mirror mode of the remote video view during a call, use the
 * `setRemoteRenderModeEx:mode:mirror:connection:` method.
 *
 * @param remote The remote video view settings. See `AgoraRtcVideoCanvas`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setupRemoteVideoEx:(AgoraRtcVideoCanvas* _Nonnull)remote
               connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setupRemoteVideoEx(_:connection:));

/**
 * @brief Sets the video display mode of a specified remote user.
 *
 * @details
 * After initializing the video view of a remote user, you can call this method to update its
 * rendering and mirror modes. This method affects only the video view that the local user sees.
 *
 * @note
 * - Call this method after initializing the remote view by calling the `setupRemoteVideo:` method.
 * - During a call, you can call this method as many times as necessary to update the display mode
 * of the video view of a remote user.
 *
 * @param uid The user ID of the remote user.
 * @param mode The video display mode of the remote user. See `AgoraVideoRenderMode`.
 * @param mirror The mirror mode of the remote user view. See `AgoraVideoMirrorMode`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteRenderModeEx:(NSUInteger)uid
                        mode:(AgoraVideoRenderMode)mode
                      mirror:(AgoraVideoMirrorMode)mirror
                  connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setRemoteRenderModeEx(_:mode:mirror:connection:));

/**
 * @brief Stops or resumes receiving the video stream of a specified user.
 *
 * @details
 * This method is used to stop or resume receiving the video stream of a specified user. You can
 * call this method before or after joining a channel. If a user leaves a channel, the settings in
 * this method become invalid.
 *
 * @param uid The user ID of the remote user.
 * @param mute Whether to stop receiving the video stream of the specified user:
 * - `YES`: Stop receiving the video stream of the specified user.
 * - `NO`: (Default) Resume receiving the video stream of the specified user.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteRemoteVideoStreamEx:(NSUInteger)uid
                          mute:(BOOL)mute
                    connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(muteRemoteVideoStreamEx(_:mute:connection:));

/**
 * @brief Stops or resumes subscribing to the video streams of all remote users.
 *
 * @details
 * After successfully calling this method, the local user stops or resumes subscribing to the video
 * streams of all remote users, including all subsequent users.
 *
 * @param mute Whether to stop subscribing to the video streams of all remote users.
 * - `YES`: Stop subscribing to the video streams of all remote users.
 * - `NO`: (Default) Subscribe to the video streams of all remote users by default.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteAllRemoteVideoStreamsEx:(BOOL)mute
                        connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(muteAllRemoteVideoStreamsEx(_:connection:));

/**
 * @brief Enables or disables dual-stream mode on the sender side.
 *
 * @deprecated v4.2.0. This method is deprecated. Use setDualStreamModeEx instead.
 *
 * @details
 * You can call this method to enable or disable the dual-stream mode on the publisher side. Dual
 * streams are a pairing of a high-quality video stream and a low-quality video stream:
 * - High-quality video stream: High bitrate, high resolution.
 * - Low-quality video stream: Low bitrate, low resolution.
 * After you enable dual-stream mode, you can call `setRemoteVideoStream:type:` to choose to receive
 * either the high-quality video stream or the low-quality video stream on the subscriber side.
 *
 * @note This method is applicable to all types of streams from the sender, including but not
 * limited to video streams collected from cameras, screen sharing streams, and custom-collected
 * video streams.
 *
 * @param enabled Whether to enable dual-stream mode:
 * - `YES`: Enable dual-stream mode.
 * - `NO`: (Default) Disable dual-stream mode.
 * @param streamConfig The configuration of the low-quality video stream. See
 * `AgoraSimulcastStreamConfig`.Note: When setting `mode` to `AgoraDisableSimulcastStream`, setting
 * `streamConfig` will not take effect.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableDualStreamModeEx:(BOOL)enabled
                 streamConfig:(AgoraSimulcastStreamConfig*)streamConfig
                   connection:(AgoraRtcConnection* _Nonnull)connection NS_SWIFT_NAME(enableDualStreamModeEx(_:streamConfig:connection:)) __deprecated_msg("use setDualStreamModeEx: instead.");

/**
 * @brief Sets the dual-stream mode on the sender side.
 *
 * @details
 * The SDK defaults to enabling low-quality video stream adaptive mode ( `AgoraAutoSimulcastStream`
 * ) on the sender side, which means the sender does not actively send low-quality video stream. The
 * receiving end with the role of the **host** can initiate a low-quality video stream request by
 * calling `setRemoteVideoStreamEx:type:connection:`, and upon receiving the request, the sending
 * end automatically starts sending low-quality stream.
 * - If you want to modify this behavior, you can call this method and set `mode` to
 * `AgoraDisableSimulcastStream` (never send low-quality video streams) or
 * `AgoraEnableSimulcastStream` (always send low-quality video streams).
 * - If you want to restore the default behavior after making changes, you can call this method
 * again with `mode` set to `AgoraAutoSimulcastStream`.
 *
 * @note
 * The difference and connection between this method and
 * `enableDualStreamModeEx:streamConfig:connection:` is as follows:
 * - When calling this method and setting `mode` to AgoraDisableSimulcastStream, it has the same
 * effect as `enableDualStreamModeEx:streamConfig:connection:` `(NO)`.
 * - When calling this method and setting `mode` to AgoraEnableSimulcastStream, it has the same
 * effect as `enableDualStreamModeEx:streamConfig:connection:` `(YES)`.
 * - Both methods can be called before and after joining a channel. If both methods are used, the
 * settings in the method called later takes precedence.
 *
 * @param mode The mode in which the video stream is sent. See `AgoraSimulcastStreamMode`.
 * @param streamConfig The configuration of the low-quality video stream. See
 * `AgoraSimulcastStreamConfig`.Note: When setting `mode` to `AgoraDisableSimulcastStream`, setting
 * `streamConfig` will not take effect.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDualStreamModeEx:(AgoraSimulcastStreamMode)mode
              streamConfig:(AgoraSimulcastStreamConfig*)streamConfig
                connection:(AgoraRtcConnection* _Nonnull)connection NS_SWIFT_NAME(setDualStreamModeEx(_:streamConfig:connection:));

/**
 * @brief Sets the simulcast video stream configuration.
 *
 * @since v4.6.0
 *
 * @details
 * This method can be called in scenarios involving multiple channels. You can call the
 * `setSimulcastConfig` method to set video streams with different resolutions for the same video
 * source. The subscribers can call to select which stream layer to receive. The broadcaster can
 * publish up to four layers of video streams: one main stream (highest resolution) and three
 * additional streams of different quality levels. `setRemoteVideoStream:type:`
 * Applicable scenarios: This method can be called in scenarios involving multiple channels.
 *
 * @param simulcastConfig Configuration for different video steam layers. This configuration
 * includes seven layers, from AgoraStreamLayer1 to AgoraStreamLow, with a maximum of three layers
 * enabled simultaneously. See `AgoraSimulcastConfig`.
 * @param connection One `AgoraRtcConnection` object. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSimulcastConfigEx:(AgoraSimulcastConfig*)simulcastConfig
                 connection:(AgoraRtcConnection* _Nonnull)connection NS_SWIFT_NAME(setDualStreamModeEx(_:connection:));

/**
 * @brief Sets the video stream type to subscribe to.
 *
 * @details
 * Depending on the default behavior of the sender and the specific settings when calling
 * `setDualStreamMode:streamConfig:`, the scenarios for the receiver calling this method are as
 * follows:
 * - The SDK enables low-quality video stream adaptive mode ( `AgoraAutoSimulcastStream` ) on the
 * sender side by default, meaning only the high-quality video stream is transmitted. Only the
 * receiver with the role of the **host**can call this method to initiate a low-quality video stream
 * request. Once the sender receives the request, it starts automatically sending the low-quality
 * video stream. At this point, all users in the channel can call this method to switch to
 * low-quality video stream subscription mode.
 * - If the sender calls `setDualStreamMode:streamConfig:` and sets `mode` to
 * `AgoraDisableSimulcastStream` (never send low-quality video stream), then calling this method
 * will have no effect.
 * - If the sender calls `setDualStreamMode:streamConfig:` and sets `mode` to
 * `AgoraEnableSimulcastStream` (always send low-quality video stream), both the host and audience
 * receivers can call this method to switch to low-quality video stream subscription mode.
 * The SDK will dynamically adjust the size of the corresponding video stream based on the size of
 * the video window to save bandwidth and computing resources. The default aspect ratio of the
 * low-quality video stream is the same as that of the high-quality video stream. According to the
 * current aspect ratio of the high-quality video stream, the system will automatically allocate the
 * resolution, frame rate, and bitrate of the low-quality video stream.
 *
 * @note If the publisher has already called `setDualStreamModeEx:streamConfig:connection:` and set
 * `mode` to `AgoraDisableSimulcastStream` (never send low-quality video stream), calling this
 * method will not take effect, you should call `setDualStreamModeEx:streamConfig:connection:` again
 * on the sending end and adjust the settings.
 *
 * @param uid The user ID.
 * @param streamType The video stream type, see `AgoraVideoStreamType`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteVideoStreamEx:(NSUInteger)uid
                         type:(AgoraVideoStreamType)streamType
                   connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setRemoteVideoStreamEx(_:type:connection:));
/**
 * @brief Sets options for subscribing to remote video streams.
 *
 * @details
 * When a remote user has enabled dual-stream mode, you can call this method to choose the option
 * for subscribing to the video streams sent by the remote user.
 *
 * @param uid The user ID of the remote user.
 * @param options The video subscription options. See `AgoraVideoSubscriptionOptions`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteVideoSubscriptionOptionsEx:(NSUInteger)uid
                                   options:(AgoraVideoSubscriptionOptions* _Nonnull)options
                                connection:(AgoraRtcConnection* _Nonnull)connection NS_SWIFT_NAME(setRemoteVideoSubscriptionOptionsEx(_:options:connection:));

/**
 * @brief Gets the user information by passing in the user account.
 *
 * @details
 * After a remote user joins the channel, the SDK gets the UID and user account of the remote user,
 * caches them in a mapping table object, and triggers the
 * `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:` callback on the local client. After receiving
 * the callback, you can call this method and pass in the UID to get the user account of the
 * specified user from the `AgoraUserInfo` object.
 *
 * @param userAccount The user account.
 * @param connection The connection information. See `AgoraRtcConnection`.
 * @param error Error code.
 *
 */
- (AgoraUserInfo* _Nullable)getUserInfoByUserAccountEx:(NSString* _Nonnull)userAccount
                                            connection:(AgoraRtcConnection * _Nonnull)connection
                                             withError:(AgoraErrorCode* _Nullable)error NS_SWIFT_NAME(getUserInfo(byUserAccountEx:connection:withError:));

/**
 * @brief Gets the user information by passing in the user ID.
 *
 * @details
 * After a remote user joins the channel, the SDK gets the UID and user account of the remote user,
 * caches them in a mapping table object, and triggers the
 * `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:` callback on the local client. After receiving
 * the callback, you can call this method and pass in the UID to get the user account of the
 * specified user from the `AgoraUserInfo` object.
 *
 * @param uid The user ID.
 * @param error Error code.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - A `AgoraUserInfo` object, if the method call succeeds.
 * - nil, if the method call fails.
 */
- (AgoraUserInfo* _Nullable)getUserInfoByUidEx:(NSUInteger)uid
                                    connection:(AgoraRtcConnection * _Nonnull)connection
                                     withError:(AgoraErrorCode* _Nullable)error NS_SWIFT_NAME(getUserInfo(byUidEx:connection:withError:));

/**
 * @brief Gets the current connection state of the SDK.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * The current connection state. See `AgoraConnectionState`.
 */
- (AgoraConnectionState)getConnectionStateEx:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(getConnectionStateEx(_:));

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
/**
 * @brief Enables loopback audio capturing.
 *
 * @details
 * If you enable loopback audio capturing, the output of the sound card is mixed into the audio
 * stream sent to the other end.
 *
 * @note - This method only supports using one sound card for audio capturing.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 * @param enabled Sets whether to enable loopback audio capture:
 * - `YES`: Enable loopback audio capturing.
 * - `NO`: (Default) Disable loopback audio capturing.
 * @param deviceName The device name of the virtual sound card. The default value is set to NULL,
 * which means using AgoraALD for loopback audio capturing.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableLoopbackRecordingEx:(BOOL)enabled
                      deviceName:(NSString* _Nullable)deviceName
                      connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(enableLoopbackRecordingEx(_:deviceName:connection:));
#endif

/**
 * Adjusts the recording volume.
 *
 * @param volume The recording volume, which ranges from 0 to 400:
 * - 0  : Mute the recording volume.
 * - 100: The original volume.
 * - 400: (Maximum) Four times the original volume with signal clipping protection.
 *
 * @param connection  \ref AgoraRtcConnection by channelId and uid combine
 *
 * @return
 * - 0  : Success.
 * - < 0: Failure.
 */
- (int)adjustRecordingSignalVolumeEx:(NSInteger)volume
                          connection:(AgoraRtcConnection* _Nonnull)connection;

/**
 * Mutes or resume recording signal volume.
 *
 * @param mute Determines whether to mute or resume the recording signal volume.
 * - YES: Mute the recording signal volume.
 * -  NO: (Default) Resume the recording signal volume.
 *
 * @param connection  \ref AgoraRtcConnection by channelId and uid combine
 *
 * @return
 * - 0  : Success.
 * - < 0: Failure.
 */
- (int)muteRecordingSignalEx:(BOOL)mute
                  connection:(AgoraRtcConnection* _Nonnull)connection;

/**
 * @brief Adjusts the playback signal volume of a specified remote user.
 *
 * @details
 * You can call this method to adjust the playback volume of a specified remote user. To adjust the
 * playback volume of different remote users, call the method as many times, once for each remote
 * user.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 *
 * @param uid The user ID of the remote user.
 * @param volume The volume of the user. The value range is [0,400].
 * - 0: Mute.
 * - 100: (Default) The original volume.
 * - 400: Four times the original volume (amplifying the audio signals by four times).
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustUserPlaybackSignalVolumeEx:(NSUInteger)uid
                              volume:(NSInteger)volume
                          connection:(AgoraRtcConnection* _Nonnull)connection;

/**
 * @brief Agora supports reporting and analyzing customized messages.
 *
 * @details
 * Agora supports reporting and analyzing customized messages. This function is in the beta stage
 * with a free trial. The ability provided in its beta test version is reporting a maximum of 10
 * message pieces within 6 seconds, with each message piece not exceeding 256 bytes and each string
 * not exceeding 100 bytes. To try out this function, contact `support@agora.io` and discuss the
 * format of customized messages with us.
 *
 */
- (int)sendCustomReportMessageEx:(NSString * _Nullable)messageId
                        category:(NSString * _Nullable)category
                           event:(NSString * _Nullable)event
                           label:(NSString * _Nullable)label
                           value:(NSInteger)value
                      connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(sendCustomReportMessageEx(_:category:event:label:value:connection:));

/**
 * @brief Enables the reporting of users' volume indication.
 *
 * @details
 * This method enables the SDK to regularly report the volume information to the app of the local
 * user who sends a stream and remote users (three users at most) whose instantaneous volumes are
 * the highest.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 * Related callbacks: The SDK triggers the
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback according to the interval
 * you set if this method is successfully called and there are users publishing streams in the
 * channel.
 *
 * @param interval Sets the time interval between two consecutive volume indications:
 * - ≤ 0: Disables the volume indication.
 * - > 0: Time interval (ms) between two consecutive volume indications. Ensure this parameter is
 * set to a value greater than 10, otherwise you will not receive the
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback. Agora recommends that
 * this value is set as greater than 100.
 * @param smooth The smoothing factor that sets the sensitivity of the audio volume indicator. The
 * value ranges between 0 and 10. The recommended value is 3. The greater the value, the more
 * sensitive the indicator.
 * @param reportVad - `YES`: Enables the voice activity detection of the local user. Once it is
 * enabled, the `vad` parameter of the
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback reports the voice
 * activity status of the local user.
 * - `NO`: (Default) Disables the voice activity detection of the local user. Once it is disabled,
 * the `vad` parameter of the `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:`
 * callback does not report the voice activity status of the local user, except for the scenario
 * where the engine automatically detects the voice activity of the local user.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableAudioVolumeIndicationEx:(NSInteger)interval
                              smooth:(NSInteger)smooth
                           reportVad:(BOOL)reportVad
                          connection:(AgoraRtcConnection* _Nonnull)connection NS_SWIFT_NAME(enableAudioVolumeIndicationEx(_:smooth:reportVad:connection:));

 /**
  * @brief Sets the 2D position (the position on the horizontal plane) of the remote user's voice.
  *
  * @details
  * This method sets the voice position and volume of a remote user.
  * When the local user calls this method to set the voice position of a remote user, the voice
  * difference between the left and right channels allows the local user to track the real-time
  * position of the remote user, creating a sense of space. This method applies to massive
  * multiplayer online games, such as Battle Royale games.
  *
  * @note
  * - For the best voice positioning, Agora recommends using a wired headset.
  * - Call this method after joining a channel.
  *
  * @param uid The user ID of the remote user.
  * @param pan The voice position of the remote user. The value ranges from -1.0 to 1.0:
  * - -1.0: The remote voice comes from the left.
  * - 0.0: (Default) The remote voice comes from the front.
  * - 1.0: The remote voice comes from the right.
  * @param gain The volume of the remote user. The value ranges from 0.0 to 100.0. The default value
  * is 100.0 (the original volume of the remote user). The smaller the value, the lower the volume.
  * @param connection The connection information. See `AgoraRtcConnection`.
  *
  * @return
  * - 0: Success.
  * - < 0: Failure.
  */
- (int)setRemoteVoicePositionEx:(NSUInteger)uid
                          pan:(double)pan
                         gain:(double)gain
                   connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setRemoteVoicePositionEx(_:pan:gain:connection:));

/** Sets spatial audio parameters of a remote user.

 When the local user calls this method to set the spatial audio parameters  of a remote user, the sound difference between the left and right channels allows the local user to track the real-time position of the remote user, creating a real sense of spatial.

**Note:**

- For the best effect, we recommend using the following audio output devices:
  - (iOS) A stereo headset.
  - (macOS) A stereo loudspeaker.
 @param uid The ID of the remote user.
 @param params The spatial audio parameters of the remote user. 
 @param connection  \ref AgoraRtcConnection by channelId and uid combine
 
 @return * 0: Success.
* < 0: Failure.
 */
- (int)setRemoteUserSpatialAudioParamsEx:(NSUInteger)uid
                                  params:(AgoraSpatialAudioParams* _Nonnull)params
                              connection:(AgoraRtcConnection* _Nonnull)connection NS_SWIFT_NAME(setRemoteUserSpatialAudioParamsEx(_:params:connection:));

/**
 * @brief Adds a watermark image to the local video.
 *
 * @details
 * This method adds a PNG watermark image to the local video in the live streaming. Once the
 * watermark image is added, all the audience in the channel (CDN audience included), and the
 * capturing device can see and capture it. The Agora SDK supports adding only one watermark image
 * onto a live video stream. The newly added watermark image replaces the previous one.
 * The watermark coordinates are dependent on the settings in the
 * `setVideoEncoderConfigurationEx:connection:` method:
 * - If the orientation mode of the encoding video ( `AgoraVideoOutputOrientationMode` ) is fixed
 * landscape mode or the adaptive landscape mode, the watermark uses the landscape orientation.
 * - If the orientation mode of the encoding video ( `AgoraVideoOutputOrientationMode` ) is fixed
 * portrait mode or the adaptive portrait mode, the watermark uses the portrait orientation.
 * - When setting the watermark position, the region must be less than the dimensions set in the
 * `setVideoEncoderConfigurationEx:connection:` method; otherwise, the watermark image will be
 * cropped.
 *
 * @note
 * - Ensure that you have called `enableVideo` before calling this method.
 * - This method supports adding a watermark image in the PNG file format only. Supported pixel
 * formats of the PNG image are RGBA, RGB, Palette, Gray, and Alpha_gray.
 * - If the dimensions of the PNG image differ from your settings in this method, the image will be
 * cropped or zoomed to conform to your settings.
 * - If you have enabled the local video preview by calling the `startPreview:` method, you can use
 * the `visibleInPreview` member to set whether or not the watermark is visible in the preview.
 * - If you have enabled the mirror mode for the local video, the watermark on the local video is
 * also mirrored. To avoid mirroring the watermark, Agora recommends that you do not use the mirror
 * and watermark functions for the local video at the same time. You can implement the watermark
 * function in your application layer.
 *
 * @param url The local file path of the watermark image to be added. This method supports adding a
 * watermark image from the local absolute or relative file path.
 * @param options The options of the watermark image to be added. See `WatermarkOptions`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)addVideoWatermarkEx:(NSURL* _Nonnull)url options:(WatermarkOptions* _Nonnull)options connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(addVideoWatermarkEx(_:options:connection:)) __deprecated_msg("use addVideoWatermark:config connection instead.");

/**
 * @brief Adds a watermark image to the local video.
 *
 * @since 4.6.0
 *
 * @details
 * Applicable scenarios: This method applies to multi-channel scenarios.
 *
 * @param configs Watermark configuration. See `WatermarkConfig`.
 * @param connection `AgoraRtcConnection` object. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)addVideoWatermarkEx:(WatermarkConfig* _Nonnull)configs connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(addVideoWatermarkEx(_:connection:));

/**
 * @brief Removes the specified watermark image from the local or remote video stream.
 *
 * @since 4.6.0
 *
 * @details
 * Applicable scenarios: This method applies to multi-channel scenarios.
 *
 * @param id Identifier of the watermark to be removed.
 * @param connection Connection information, including channel ID and user ID. See
 * `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)removeVideoWatermarkEx:(NSString* _Nonnull)id connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(removeVideoWatermarkEx(_:connection:));


/**
 * @brief Removes the watermark image from the video stream.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)clearVideoWatermarkEx:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(clearVideoWatermarkEx(_:));

/**
 * @brief Creates a data stream.
 *
 * @details
 * You can call this method to create a data stream and improve the reliability and ordering of data
 * transmission.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 * Related callbacks: After setting `reliable` to `YES`, if the recipient does not receive the data
 * within five seconds, the SDK triggers the
 * `rtcEngine:didOccurStreamMessageErrorFromUid:streamId:error:missed:cached:` callback and returns
 * an error code.
 *
 * @note Each user can create up to five data streams during the lifecycle of `AgoraRtcEngineKit`.
 * The data stream will be destroyed when leaving the channel, and the data stream needs to be
 * recreated if needed.
 *
 * @param streamId An output parameter; the ID of the data stream created.
 * @param reliable Sets whether the recipients are guaranteed to receive the data stream within five
 * seconds:
 * - `YES`: The recipients receive the data from the sender within five seconds. If the recipient
 * does not receive the data within five seconds, the SDK triggers the
 * `rtcEngine:didOccurStreamMessageErrorFromUid:streamId:error:missed:cached:` callback and returns
 * an error code.
 * - `NO`: There is no guarantee that the recipients receive the data stream within five seconds and
 * no error message is reported for any delay or missing data stream.
 * Attention: Please ensure that `reliable` and `ordered` are either both set to`YES` or both set to
 * `NO`.
 * @param ordered Sets whether the recipients receive the data stream in the sent order:
 * - `YES`: The recipients receive the data in the sent order.
 * - `NO`: The recipients do not receive the data in the sent order.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: The data stream is successfully created.
 * - < 0: Failure.
 */
- (int)createDataStreamEx:(NSInteger * _Nonnull)streamId
                 reliable:(BOOL)reliable
                  ordered:(BOOL)ordered
               connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(createDataStreamEx(_:reliable:ordered:connection:));
/**
 * @brief Creates a data stream.
 *
 * @details
 * Compared to `createDataStreamEx:reliable:ordered:connection:`, this method does not guarantee the
 * reliability of data transmission. If a data packet is not received five seconds after it was
 * sent, the SDK directly discards the data.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 *
 * @note
 * Each user can create up to five data streams during the lifecycle of `AgoraRtcEngineKit`. The
 * data stream will be destroyed when leaving the channel, and the data stream needs to be recreated
 * if needed.
 * If you need a more comprehensive solution for low-latency, high-concurrency, and scalable
 * real-time messaging and status synchronization, it is recommended to use `Signaling`.
 *
 * @param streamId An output parameter; the ID of the data stream created.
 * @param config The configurations for the data stream. See `AgoraDataStreamConfig`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: The data stream is successfully created.
 * - < 0: Failure.
 */
- (int)createDataStreamEx:(NSInteger * _Nonnull)streamId
                   config:(AgoraDataStreamConfig * _Nonnull)config
               connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(createDataStreamEx(_:config:connection:));

/**
 * @brief Sends data stream messages.
 *
 * @details
 * After calling `createDataStream:config:`, you can call this method to send data stream messages
 * to all users in the channel.
 * The SDK has the following restrictions on this method:
 * - Each client within the channel can have up to 5 data channels simultaneously, with a total
 * shared packet bitrate limit of 30 KB/s for all data channels.
 * - Each data channel can send up to 60 packets per second, with each packet being a maximum of 1
 * KB.
 * A successful method call triggers the `rtcEngine:receiveStreamMessageFromUid:streamId:data:`
 * callback on the remote client, from which the remote user gets the stream message. A failed
 * method call triggers the
 * `rtcEngine:didOccurStreamMessageErrorFromUid:streamId:error:missed:cached:` callback on the
 * remote client.
 *
 * @note
 * If you need a more comprehensive solution for low-latency, high-concurrency, and scalable
 * real-time messaging and status synchronization, it is recommended to use `Signaling`.
 * - This method needs to be called after `createDataStream:config:` and joining the channel.
 * - This method applies to broadcasters only.
 *
 * @param streamId The data stream ID. You can get the data stream ID by calling
 * `createDataStream:config:`.
 * @param data The message to be sent.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)sendStreamMessageEx:(NSInteger)streamId
                      data:(NSData * _Nonnull)data
                connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(sendStreamMessageEx(_:data:connection:));

/**-----------------------------------------------------------------------------
 * @name Common Data Transmission
 * -----------------------------------------------------------------------------
 */

/**
 * @brief Send Reliable message to remote uid in channel.
 *
 * @technical preview
 *
 * @param uid remote user id.
 * @param type Reliable Data Transmission tunnel message type. See AgoraRdtStreamType.
 * @param data The sent data.
 * @param connection AgoraRtcConnection by channelId and uid combine.

 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)sendRdtMessageEx:(NSInteger)uid
                   type:(AgoraRdtStreamType)type
                   data:(NSData * _Nonnull)data
             connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(sendRdtMessageEx(_:type:data:connection:));

/**
 * @brief Send media control message
 *
 * @technical preview
 *
 * @param uid Remote user id. In particular, if the uid is set to 0, it means broadcasting the message to the entire channel.
 * @param data The sent data.
 * @param connection AgoraRtcConnection by channelId and uid combine.

 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)sendMediaControlMessageEx:(NSInteger)uid
                   data:(NSData * _Nonnull)data
             connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(sendMediaControlMessageEx(_:data:connection:));

/**-----------------------------------------------------------------------------
 * @name Stream Fallback
 * -----------------------------------------------------------------------------
 */

 /**
 * Sets the high priority user list and related remote subscribe fallback option.
 *
 * @param uidList The uid list of high priority users.
 * @param option The remote subscribe fallback option of high priority users.
 * @param connection  \ref AgoraRtcConnection by channelId and uid combine.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */

- (int)setHighPriorityUserListEx:(NSArray <NSNumber *> *_Nullable)uidList
                          option:(AgoraStreamFallbackOptions)option
                      connection:(AgoraRtcConnection* _Nonnull)connection;

/**
 * @brief Sets the blocklist of subscriptions for audio streams.
 *
 * @details
 * You can call this method to specify the audio streams of a user that you do not want to subscribe
 * to.
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - The blocklist is not affected by the setting in `muteRemoteAudioStream:mute:`,
 * `muteAllRemoteAudioStreams:`, and `autoSubscribeAudio` in `AgoraRtcChannelMediaOptions`.
 * - Once the blocklist of subscriptions is set, it is effective even if you leave the current
 * channel and rejoin the channel.
 * - If a user is added in the allowlist and blocklist at the same time, only the blocklist takes
 * effect.
 *
 * @param blocklist The user ID list of users that you do not want to subscribe to.
 * If you want to specify the audio streams of a user that you do not want to subscribe to, add the
 * user ID in this list. If you want to remove a user from the blocklist, you need to call the
 * `setSubscribeAudioBlocklist:` method to update the user ID list; this means you only add the
 * `uid` of users that you do not want to subscribe to in the new user ID list.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSubscribeAudioBlocklistEx:(NSArray <NSNumber *> *_Nonnull)blocklist connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setSubscribeAudioBlocklistEx(_:connection:));

/**
 * @brief Sets the allowlist of subscriptions for audio streams.
 *
 * @details
 * You can call this method to specify the audio streams of a user that you want to subscribe to.
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - The allowlist is not affected by the setting in `muteRemoteAudioStream:mute:`,
 * `muteAllRemoteAudioStreams:` and `autoSubscribeAudio` in `AgoraRtcChannelMediaOptions`.
 * - Once the allowlist of subscriptions is set, it is effective even if you leave the current
 * channel and rejoin the channel.
 * - If a user is added in the allowlist and blocklist at the same time, only the blocklist takes
 * effect.
 *
 * @param allowlist The user ID list of users that you want to subscribe to.
 * If you want to specify the audio streams of a user for subscription, add the user ID in this
 * list. If you want to remove a user from the allowlist, you need to call the
 * `setSubscribeAudioAllowlist:` method to update the user ID list; this means you only add the
 * `uid` of users that you want to subscribe to in the new user ID list.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSubscribeAudioAllowlistEx:(NSArray <NSNumber *> *_Nonnull)allowlist connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setSubscribeAudioAllowlistEx(_:connection:));

/**
 * @brief Sets the blocklist of subscriptions for video streams.
 *
 * @details
 * You can call this method to specify the video streams of a user that you do not want to subscribe
 * to.
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - The blocklist is not affected by the setting in `muteRemoteVideoStream:mute:`,
 * `muteAllRemoteVideoStreams:` and `autoSubscribeAudio` in `AgoraRtcChannelMediaOptions`.
 * - Once the blocklist of subscriptions is set, it is effective even if you leave the current
 * channel and rejoin the channel.
 * - If a user is added in the allowlist and blocklist at the same time, only the blocklist takes
 * effect.
 *
 * @param blocklist The user ID list of users that you do not want to subscribe to.
 * If you want to specify the video streams of a user that you do not want to subscribe to, add the
 * user ID of that user in this list. If you want to remove a user from the blocklist, you need to
 * call the `setSubscribeVideoBlocklist:` method to update the user ID list; this means you only add
 * the `uid` of users that you do not want to subscribe to in the new user ID list.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSubscribeVideoBlocklistEx:(NSArray <NSNumber *> *_Nonnull)blocklist connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setSubscribeVideoBlocklistEx(_:connection:));

/**
 * @brief Sets the allowlist of subscriptions for video streams.
 *
 * @details
 * You can call this method to specify the video streams of a user that you want to subscribe to.
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - The allowlist is not affected by the setting in `muteRemoteVideoStream:mute:`,
 * `muteAllRemoteVideoStreams:` and `autoSubscribeAudio` in `AgoraRtcChannelMediaOptions`.
 * - Once the allowlist of subscriptions is set, it is effective even if you leave the current
 * channel and rejoin the channel.
 * - If a user is added in the allowlist and blocklist at the same time, only the blocklist takes
 * effect.
 * @param allowlist The user ID list of users that you want to subscribe to.
 * If you want to specify the video streams of a user for subscription, add the user ID in this
 * list. If you want to remove a user from the allowlist, you need to call the
 * `setSubscribeVideoAllowlistEx:` method to update the user ID list; this means you only add the
 * `uid` of users that you want to subscribe to in the new user ID list.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSubscribeVideoAllowlistEx:(NSArray <NSNumber *> *_Nonnull)allowlist connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setSubscribeVideoAllowlistEx(_:connection:));

/**
 * @brief Takes a snapshot of a video stream using connection ID.
 *
 * @details
 * This method takes a snapshot of a video stream from the specified user, generates a JPG image,
 * and saves it to the specified path.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 * Related callbacks: After a successful call of this method, the SDK triggers the
 * `rtcEngine:snapshotTaken:filePath:width:height:errCode:` callback to report whether the snapshot
 * is successfully taken, as well as the details for that snapshot.
 *
 * @note
 * - The method is asynchronous, and the SDK has not taken the snapshot when the method call
 * returns.
 * - When used for local video snapshots, this method takes a snapshot for the video streams
 * specified in `AgoraRtcChannelMediaOptions`.
 * - If the user's video has been preprocessed, for example, watermarked or beautified, the
 * resulting snapshot includes the pre-processing effect.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 * @param uid The user ID. Set uid as 0 if you want to take a snapshot of the local user's video.
 * @param filePath The local path (including filename extensions) of the snapshot. For example:
 * - iOS: `/App Sandbox/Library/Caches/example.jpg`
 * - macOS: `～/Library/Logs/example.jpg`
 * Attention: Ensure that the path you specify exists and is writable.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (NSInteger)takeSnapshotEx:(AgoraRtcConnection * _Nonnull)connection uid:(NSInteger)uid filePath:(NSString* _Nonnull)filePath NS_SWIFT_NAME(takeSnapshotEx(_:uid:filePath:));

/**
 * @brief Gets a video screenshot of the specified observation point using the connection ID.
 *
 * @details
 * This method takes a snapshot of a video stream from the specified user, generates a JPG image,
 * and saves it to the specified path.
 * Call timing: Call this method after
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`.
 * Related callbacks: After a successful call of this method, the SDK triggers the
 * `rtcEngine:snapshotTaken:filePath:width:height:errCode:` callback to report whether the snapshot
 * is successfully taken, as well as the details for that snapshot.
 *
 * @note
 * - The method is asynchronous, and the SDK has not taken the snapshot when the method call
 * returns.
 * - When used for local video snapshots, this method takes a snapshot for the video streams
 * specified in `AgoraRtcChannelMediaOptions`.
 * - If the user's video has been preprocessed, for example, watermarked or beautified, the
 * resulting snapshot includes the pre-processing effect.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 * @param uid The user ID. Set uid as 0 if you want to take a snapshot of the local user's video.
 * @param config The configuration of the snaptshot. See `AgoraSnapshotConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (NSInteger)takeSnapshotExWithConfig:(AgoraRtcConnection * _Nonnull)connection uid:(NSInteger)uid config:(AgoraSnapshotConfig* _Nonnull)config NS_SWIFT_NAME(takeSnapshotExWithConfig(_:uid:config:));

/**
   * send audio metadata
   * @since v4.3.1
   * @param metadata The NSData of metadata
   * @param connection AgoraRtcConnection.
   * @return
   * - 0: success
   * - <0: failure
   * @technical preview
*/
- (int)sendAudioMetadataEx:(AgoraRtcConnection * _Nonnull)connection metadata:(NSData * _Nonnull)metadata NS_SWIFT_NAME(sendAudioMetadataEx(_:metadata:));

/**
 * @brief Enables or disables video screenshot and upload.
 *
 * @details
 * This method can take screenshots for multiple video streams and upload them. When video
 * screenshot and upload function is enabled, the SDK takes screenshots and uploads videos sent by
 * local users based on the type and frequency of the module you set in `AgoraContentInspectConfig`.
 * After video screenshot and upload, the Agora server sends the callback notification to your app
 * server in HTTPS requests and sends all screenshots to the third-party cloud storage service.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @note Before calling this method, ensure that you have contacted `technical support` to activate
 * the video screenshot upload service.
 *
 * @param enabled Whether to enalbe video screenshot and upload:
 * - `YES`: Enables video screenshot and upload.
 * - `NO`: Disables video screenshot and upload.
 * @param config Screenshot and upload configuration. See `AgoraContentInspectConfig`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableContentInspectEx:(BOOL)enabled config:(AgoraContentInspectConfig* _Nonnull)config connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(enableContentInspectEx(_:config:connection:));

/**
 * @brief Starts pushing media streams to a CDN without transcoding.
 *
 * @details
 * Agora recommends that you use the server-side Media Push function. For details, see `Use RESTful
 * API`.
 * You can call this method to push an audio or video stream to the specified CDN address. This
 * method can push media streams to only one CDN address at a time, so if you need to push streams
 * to multiple addresses, call this method multiple times.
 * After you call this method, the SDK triggers the
 * `rtcEngine:rtmpStreamingChangedToState:state:reason:` callback on the local client to report the
 * state of the streaming.
 *
 * @note
 * - Call this method after joining a channel.
 * - Only hosts in the LIVE_BROADCASTING profile can call this method.
 * - If you want to retry pushing streams after a failed push, make sure to call `stopRtmpStream:`
 * first, then call this method to retry pushing streams; otherwise, the SDK returns the same error
 * code as the last failed push.
 *
 * @param url The address of Media Push. The format is RTMP or RTMPS. The character length cannot
 * exceed 1024 bytes. Special characters such as Chinese characters are not supported.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The URL or configuration of transcoding is invalid; check your URL and transcoding
 * configurations.
 *   - -7: The SDK is not initialized before calling this method.
 *   - -19: The Media Push URL is already in use; use another URL instead.
 */
- (int)startRtmpStreamWithoutTranscodingEx:(NSString* _Nonnull)url
                                connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(startRtmpStreamWithoutTranscodingEx(_:connection:));

/**
 * @brief Starts Media Push and sets the transcoding configuration.
 *
 * @details
 * Agora recommends that you use the server-side Media Push function. For details, see `Use RESTful
 * API`.
 * You can call this method to push a live audio-and-video stream to the specified CDN address and
 * set the transcoding configuration. This method can push media streams to only one CDN address at
 * a time, so if you need to push streams to multiple addresses, call this method multiple times.
 * After you call this method, the SDK triggers the
 * `rtcEngine:rtmpStreamingChangedToState:state:reason:` callback on the local client to report the
 * state of the streaming.
 *
 * @note
 * - Ensure that you enable the Media Push service before using this function.
 * - Call this method after joining a channel.
 * - Only hosts in the LIVE_BROADCASTING profile can call this method.
 * - If you want to retry pushing streams after a failed push, make sure to call
 * `stopRtmpStreamEx:connection:` first, then call this method to retry pushing streams; otherwise,
 * the SDK returns the same error code as the last failed push.
 *
 * @param url The address of Media Push. The format is RTMP or RTMPS. The character length cannot
 * exceed 1024 bytes. Special characters such as Chinese characters are not supported.
 * @param transcoding The transcoding configuration for Media Push. See `AgoraLiveTranscoding`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The URL or configuration of transcoding is invalid; check your URL and transcoding
 * configurations.
 *   - -7: The SDK is not initialized before calling this method.
 *   - -19: The Media Push URL is already in use; use another URL instead.
 */
- (int)startRtmpStreamWithTranscodingEx:(NSString* _Nonnull)url
                            transcoding:(AgoraLiveTranscoding* _Nullable)transcoding
                             connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(startRtmpStreamWithTranscodingEx(_:transcoding:connection:));

/**
 * @brief Updates the transcoding configuration.
 *
 * @details
 * Agora recommends that you use the server-side Media Push function. For details, see `Use RESTful
 * API`.
 * After you start pushing media streams to CDN with transcoding, you can dynamically update the
 * transcoding configuration according to the scenario. The SDK triggers the
 * `rtcEngineTranscodingUpdated:` callback after the transcoding configuration is updated.
 *
 * @param transcoding The transcoding configuration for Media Push. See `AgoraLiveTranscoding`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateRtmpTranscodingEx:(AgoraLiveTranscoding* _Nullable)transcoding
                    connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(updateRtmpTranscodingEx(_:connection:));

/**
 * @brief Stops pushing media streams to a CDN.
 *
 * @details
 * Agora recommends that you use the server-side Media Push function. For details, see `Use RESTful
 * API`.
 * You can call this method to stop the live stream on the specified CDN address. This method can
 * stop pushing media streams to only one CDN address at a time, so if you need to stop pushing
 * streams to multiple addresses, call this method multiple times.
 * After you call this method, the SDK triggers the
 * `rtcEngine:rtmpStreamingChangedToState:state:reason:` callback on the local client to report the
 * state of the streaming.
 *
 * @param url The address of Media Push. The format is RTMP or RTMPS. The character length cannot
 * exceed 1024 bytes. Special characters such as Chinese characters are not supported.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopRtmpStreamEx:(NSString* _Nonnull)url
             connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(stopRtmpStreamEx(_:connection:));

/**
 * @brief Starts relaying media streams across channels or updates channels for media relay.
 *
 * @since v4.2.0
 *
 * @details
 * The first successful call to this method starts relaying media streams from the source channel to
 * the destination channels. To relay the media stream to other channels, or exit one of the current
 * media relays, you can call this method again to update the destination channels. This feature
 * supports relaying media streams to a maximum of six destination channels.
 * After a successful method call, the SDK triggers the
 * `rtcEngine:channelMediaRelayStateDidChange:error:` callback, and this callback returns the state
 * of the media stream relay. Common states are as follows:
 * - If the `rtcEngine:channelMediaRelayStateDidChange:error:` callback returns
 * `AgoraChannelMediaRelayStateRunning` (2) and `AgoraChannelMediaRelayErrorNone` (0), it means that
 * the SDK starts relaying media streams from the source channel to the destination channel.
 * - If the `rtcEngine:channelMediaRelayStateDidChange:error:` callback returns
 * `AgoraChannelMediaRelayStateFailure` (3), an exception occurs during the media stream relay.
 *
 * @note
 * - Call this method after joining the channel.
 * - This method takes effect only when you are a host in a live streaming channel.
 * - The relaying media streams across channels function needs to be enabled by contacting
 * `technical support`.
 * - Agora does not support string user accounts in this API.
 *
 * @param config The configuration of the media stream relay. See
 * `AgoraChannelMediaRelayConfiguration`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 *   - -8: Internal state error. Probably because the user is not a broadcaster.
 */
- (int)startOrUpdateChannelMediaRelayEx:(AgoraChannelMediaRelayConfiguration * _Nonnull)config connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(startOrUpdateChannelMediaRelayEx(_:connection:));

/**
 * @brief Stops the media stream relay. Once the relay stops, the host quits all the target
 * channels.
 *
 * @details
 * After a successful method call, the SDK triggers the
 * `rtcEngine:channelMediaRelayStateDidChange:error:` callback. If the callback reports
 * `AgoraChannelMediaRelayStateIdle` (0) and `AgoraChannelMediaRelayErrorNone` (0), the host
 * successfully stops the relay.
 *
 * @note If the method call fails, the SDK triggers the
 * `rtcEngine:channelMediaRelayStateDidChange:error:` callback with the
 * `AgoraChannelMediaRelayErrorServerNoResponse` (2) or
 * `AgoraChannelMediaRelayErrorServerConnectionLost` (8) status code. You can call the
 * `leaveChannel:leaveChannelBlock:` method to leave the channel, and the media stream relay
 * automatically stops.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -5: The method call was rejected. There is no ongoing channel media relay.
 */
- (int)stopChannelMediaRelayEx:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(stopChannelMediaRelayEx(_:));

/**
 * @brief Pauses the media stream relay to all target channels.
 *
 * @details
 * After the cross-channel media stream relay starts, you can call this method to pause relaying
 * media streams to all target channels; after the pause, if you want to resume the relay, call
 * `resumeAllChannelMediaRelay`.
 *
 * @note Call this method after `startOrUpdateChannelMediaRelayEx:connection:`.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -5: The method call was rejected. There is no ongoing channel media relay.
 */
- (int)pauseAllChannelMediaRelayEx:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(pauseAllChannelMediaRelayEx(_:));

/**
 * @brief Resumes the media stream relay to all target channels.
 *
 * @details
 * After calling the `pauseAllChannelMediaRelayEx:` method, you can call this method to resume
 * relaying media streams to all destination channels.
 *
 * @note Call this method after `pauseAllChannelMediaRelayEx:`.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -5: The method call was rejected. There is no paused channel media relay.
 */
- (int)resumeAllChannelMediaRelayEx:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(resumeAllChannelMediaRelayEx(_:));

/**
 * @brief Enables tracing the video frame rendering process.
 *
 * @since v4.1.1
 *
 * @details
 * The SDK starts tracing the rendering status of the video frames in the channel from the moment
 * this method is successfully called and reports information about the event through the
 * `rtcEngine:videoRenderingTracingResultOfUid:currentEvent:tracingInfo:` callback.
 * Applicable scenarios: Agora recommends that you use this method in conjunction with the UI
 * settings (such as buttons and sliders) in your app to improve the user experience. For example,
 * call this method when the user clicks the Join Channel button, and then get the time spent during
 * the video frame rendering process through the
 * `rtcEngine:videoRenderingTracingResultOfUid:currentEvent:tracingInfo:` callback, so as to
 * optimize the indicators accordingly.
 *
 * @note
 * - If you have not called this method, the SDK tracks the rendering events of the video frames
 * from the moment you call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the
 * channel. You can call this method at an appropriate time according to the actual application
 * scenario to set the starting position for tracking video rendering events.
 * - After the local user leaves the current channel, the SDK automatically tracks the video
 * rendering events from the moment you join a channel.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startMediaRenderingTracingEx:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(startMediaRenderingTracingEx(_:));

/** Provides the technical preview functionalities or special customizations by configuring the SDK with JSON options.

 @note  The JSON options are not public by default. Agora is working on making commonly used JSON options public in a standard way. Contact support@agora.io for more information.

 @param options SDK options in JSON format.
 @param connection AgoraRtcConnection.
 */
- (int)setParametersEx:(NSString * _Nonnull)options connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(setParametersEx(_:connection:));

/**
 * @brief Gets the call ID with the connection ID.
 *
 * @details
 * When a user joins a channel on a client, a `callId` is generated to identify the call from the
 * client. You can call this method to get `callId`, and pass it in when calling methods such as
 * `rate:rating:description:` and `complain:description:`.
 * Call timing: Call this method after joining a channel.
 *
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 */
- (NSString * _Nullable)getCallIdEx:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(getCallIdEx(_:));


 /**
  * @brief Preloads a specified sound effect to the channel.
  * @since 4.6.0
  * 
  * @details
  * Each time you call this method, you can only preload one sound effect file into memory. If you
  * need to preload multiple sound files, please call this method multiple times. After preloading is
  * complete, you can call `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:` to play
  * the preloaded sound effects, or call `playAllEffects:loopCount:pitch:pan:gain:publish:` to play
  * all preloaded sound effects.
  * Applicable scenarios: This method can be called in scenarios involving multiple channels.
  *
  * @note
  * - To ensure a smooth experience, the size of sound effect files should not exceed the limit.
  * - Agora recommends that you call this method before joining a channel.
  * - If preloadEffectEx is called before playEffectEx is executed, the file resource will not be closed after playEffectEx. 
  * The next time playEffectEx is executed, it will directly seek to play at the beginning.
  * - If preloadEffectEx is not called before playEffectEx is executed, the resource will be destroyed after playEffectEx. 
  * The next time playEffectEx is executed, it will try to reopen the file and play it from the beginning.
  *
  * @param connection Connection object. See `AgoraRtcConnection`.
  * @param soundId The audio effect ID.
  * @param filePath The absolute path of the local file or the URL of the online file. Supported
  * audio formats include: mp3, mp4, m4a, aac, 3gp, mkv and wav.
  *
  * @return
  * - 0: Success.
  * - < 0: Failure.
  */
- (int)preloadEffectEx:(AgoraRtcConnection* _Nonnull)connection
             soundId:(int)soundId
            filePath:(NSString* _Nonnull)filePath NS_SWIFT_NAME(preloadEffectEx(_:soundId:filePath:));

/**
  * @brief Preloads a specified sound effect to the channel.
  * @since 4.6.0
  * 
  * @details
  * Each time you call this method, you can only preload one sound effect file into memory. If you
  * need to preload multiple sound files, please call this method multiple times. After preloading is
  * complete, you can call `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:` to play
  * the preloaded sound effects, or call `playAllEffects:loopCount:pitch:pan:gain:publish:` to play
  * all preloaded sound effects.
  * Applicable scenarios: This method can be called in scenarios involving multiple channels.
  *
  * @note
  * - To ensure a smooth experience, the size of sound effect files should not exceed the limit.
  * - Agora recommends that you call this method before joining a channel.
  * - If preloadEffectEx is called before playEffectEx is executed, the file resource will not be closed after playEffectEx. 
  * The next time playEffectEx is executed, it will directly seek to play at the beginning.
  * - If preloadEffectEx is not called before playEffectEx is executed, the resource will be destroyed after playEffectEx. 
  * The next time playEffectEx is executed, it will try to reopen the file and play it from the beginning.
  *
  * @param connection Connection object. See `AgoraRtcConnection`.
  * @param soundId The audio effect ID.
  * @param filePath The absolute path of the local file or the URL of the online file. Supported
  * audio formats include: mp3, mp4, m4a, aac, 3gp, mkv and wav.
  * @param startPos The playback position (ms) of the audio effect file.
  *
  * @return
  * - 0: Success.
  * - < 0: Failure.
  */
- (int)preloadEffectEx:(AgoraRtcConnection* _Nonnull)connection
             soundId:(int)soundId
            filePath:(NSString* _Nonnull)filePath
            startPos:(int)startPos NS_SWIFT_NAME(preloadEffectEx(_:soundId:filePath:startPos:));

/**
 * @brief Plays a specified sound effect in the channel.
 *
 * @since v4.6.0
 *
 * @details
 * This method allows you to set whether to publish sound effects in a channel. In order to play
 * multiple sound files simultaneously, simply call the method multiple times with different
 * `soundId` and `filePath` parameters. After calling `preloadEffect:filePath:` to preload the sound
 * effect, you can call this method to play the specified sound effect to all users in the channel.
 * Each call to this method can only play one sound effect. To play multiple sound effects
 * simultaneously, please call this method multiple times.
 * Applicable scenarios: This method can be called in scenarios involving multiple channels.
 *
 * @note
 * - Agora recommends not playing more than three sound effects at the same time.
 * - The sound effect ID and file path in this method must be consistent with those in the
 * `preloadEffectEx:filePath:` method.
 * - If preloadEffectEx is called before playEffectEx is executed, the file resource will not be closed after playEffectEx. 
 * The next time playEffectEx is executed, it will directly seek to play at the beginning.
 * - If preloadEffectEx is not called before playEffectEx is executed, the resource will be destroyed after playEffectEx. 
 * The next time playEffectEx is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param connection `AgoraRtcConnection` object. See `AgoraRtcConnection`.
 * @param soundId The ID of the audio effect. The unique ID of each audio effect file. If preloading
 * sound effects into memory through the `preloadEffect:filePath:` method, make sure that this
 * parameter value is consistent with the value in the `preloadEffect:filePath:` method.
 * @param filePath The absolute path of the local sound effect file (including the file name
 * extension) or the URL of the online sound effect file, for example
 * /var/mobile/Containers/Data/audio.mp4. Supported audio formats include: mp3, mp4, m4a, aac, 3gp,
 * mkv and wav.
 * @param loopCount Set the number of times the sound effect to be looped:
 * - 0: Play once.
 * - 1: Play twice.
 * - -1: Loop infinitely until calling `stopEffect:` or `stopAllEffects`.
 * @param pitch The pitch of the audio effect. The range is from 0.5 to 2.0, with a default value of
 * 1.0 (original pitch). The lower the value, the lower the pitch.
 * @param pan The spatial position of the audio effect. The range of values is from -1.0 to 1.0:
 * - 0.0: The audio effect is heard in front of the user.
 * - 1.0: The audio effect is heard on the right of the user.
 * - -1.0: The audio effect is heard on the left of the user.
 * @param gain The volume of the audio effect. The value range is 0 to 100, and the default value is
 * 100. The smaller the value, the lower the volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playEffectEx:(AgoraRtcConnection* _Nonnull)connection
            soundId:(int)soundId
           filePath:(NSString* _Nonnull)filePath
          loopCount:(NSInteger)loopCount
              pitch:(double)pitch
                pan:(double)pan
               gain:(NSInteger)gain NS_SWIFT_NAME(playEffectEx(_:soundId:filePath:loopCount:pitch:pan:gain:));

/**
 * @brief Plays a specified sound effect in the channel.
 *
 * @since v4.6.0
 *
 * @details
 * This method allows you to set whether to publish sound effects in a channel. In order to play
 * multiple sound files simultaneously, simply call the method multiple times with different
 * `soundId` and `filePath` parameters. After calling `preloadEffect:filePath:` to preload the sound
 * effect, you can call this method to play the specified sound effect to all users in the channel.
 * Each call to this method can only play one sound effect. To play multiple sound effects
 * simultaneously, please call this method multiple times.
 * Applicable scenarios: This method can be called in scenarios involving multiple channels.
 *
 * @note
 * - Agora recommends not playing more than three sound effects at the same time.
 * - The sound effect ID and file path in this method must be consistent with those in the
 * `preloadEffectEx:filePath:` method.
 * - If preloadEffectEx is called before playEffectEx is executed, the file resource will not be closed after playEffectEx. 
 * The next time playEffectEx is executed, it will directly seek to play at the beginning.
 * - If preloadEffectEx is not called before playEffectEx is executed, the resource will be destroyed after playEffectEx. 
 * The next time playEffectEx is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param connection `AgoraRtcConnection` object. See `AgoraRtcConnection`.
 * @param soundId The ID of the audio effect. The unique ID of each audio effect file. If preloading
 * sound effects into memory through the `preloadEffect:filePath:` method, make sure that this
 * parameter value is consistent with the value in the `preloadEffect:filePath:` method.
 * @param filePath The absolute path of the local sound effect file (including the file name
 * extension) or the URL of the online sound effect file, for example
 * /var/mobile/Containers/Data/audio.mp4. Supported audio formats include: mp3, mp4, m4a, aac, 3gp,
 * mkv and wav.
 * @param loopCount Set the number of times the sound effect to be looped:
 * - 0: Play once.
 * - 1: Play twice.
 * - -1: Loop infinitely until calling `stopEffect:` or `stopAllEffects`.
 * @param pitch The pitch of the audio effect. The range is from 0.5 to 2.0, with a default value of
 * 1.0 (original pitch). The lower the value, the lower the pitch.
 * @param pan The spatial position of the audio effect. The range of values is from -1.0 to 1.0:
 * - 0.0: The audio effect is heard in front of the user.
 * - 1.0: The audio effect is heard on the right of the user.
 * - -1.0: The audio effect is heard on the left of the user.
 * @param gain The volume of the audio effect. The value range is 0 to 100, and the default value is
 * 100. The smaller the value, the lower the volume.
 * @param startPos The playback position (ms) of the audio effect file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playEffectEx:(AgoraRtcConnection* _Nonnull)connection
            soundId:(int)soundId
           filePath:(NSString* _Nonnull)filePath
          loopCount:(NSInteger)loopCount
              pitch:(double)pitch
                pan:(double)pan
               gain:(NSInteger)gain
           startPos:(int)startPos NS_SWIFT_NAME(playEffectEx(_:soundId:filePath:loopCount:pitch:pan:gain:startPos:));


/**
 * @brief Plays a specified sound effect in the channel.
 *
 * @since v4.6.0
 *
 * @details
 * This method allows you to set whether to publish sound effects in a channel. In order to play
 * multiple sound files simultaneously, simply call the method multiple times with different
 * `soundId` and `filePath` parameters. After calling `preloadEffect:filePath:` to preload the sound
 * effect, you can call this method to play the specified sound effect to all users in the channel.
 * Each call to this method can only play one sound effect. To play multiple sound effects
 * simultaneously, please call this method multiple times.
 * Applicable scenarios: This method can be called in scenarios involving multiple channels.
 *
 * @note
 * - Agora recommends not playing more than three sound effects at the same time.
 * - The sound effect ID and file path in this method must be consistent with those in the
 * `preloadEffectEx:filePath:` method.
 * - If preloadEffectEx is called before playEffectEx is executed, the file resource will not be closed after playEffectEx. 
 * The next time playEffectEx is executed, it will directly seek to play at the beginning.
 * - If preloadEffectEx is not called before playEffectEx is executed, the resource will be destroyed after playEffectEx. 
 * The next time playEffectEx is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param connection `AgoraRtcConnection` object. See `AgoraRtcConnection`.
 * @param soundId The ID of the audio effect. The unique ID of each audio effect file. If preloading
 * sound effects into memory through the `preloadEffect:filePath:` method, make sure that this
 * parameter value is consistent with the value in the `preloadEffect:filePath:` method.
 * @param filePath The absolute path of the local sound effect file (including the file name
 * extension) or the URL of the online sound effect file, for example
 * /var/mobile/Containers/Data/audio.mp4. Supported audio formats include: mp3, mp4, m4a, aac, 3gp,
 * mkv and wav.
 * @param loopCount Set the number of times the sound effect to be looped:
 * - 0: Play once.
 * - 1: Play twice.
 * - -1: Loop infinitely until calling `stopEffect:` or `stopAllEffects`.
 * @param pitch The pitch of the audio effect. The range is from 0.5 to 2.0, with a default value of
 * 1.0 (original pitch). The lower the value, the lower the pitch.
 * @param pan The spatial position of the audio effect. The range of values is from -1.0 to 1.0:
 * - 0.0: The audio effect is heard in front of the user.
 * - 1.0: The audio effect is heard on the right of the user.
 * - -1.0: The audio effect is heard on the left of the user.
 * @param gain The volume of the audio effect. The value range is 0 to 100, and the default value is
 * 100. The smaller the value, the lower the volume.
 * @param publish Sets whether to publish the audio effect in a channel:
 * - YES: Publish the audio effect in the channel so that remote user can hear it.
 * - NO: Do not publish the audio effect in the channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playEffectEx:(AgoraRtcConnection* _Nonnull)connection
            soundId:(int)soundId
           filePath:(NSString* _Nonnull)filePath
          loopCount:(NSInteger)loopCount
              pitch:(double)pitch
                pan:(double)pan
               gain:(NSInteger)gain
           publish:(BOOL)publish NS_SWIFT_NAME(playEffectEx(_:soundId:filePath:loopCount:pitch:pan:gain:publish:));

/**
 * @brief Plays a specified sound effect in the channel.
 *
 * @since v4.6.0
 *
 * @details
 * This method allows you to set whether to publish sound effects in a channel. In order to play
 * multiple sound files simultaneously, simply call the method multiple times with different
 * `soundId` and `filePath` parameters. After calling `preloadEffect:filePath:` to preload the sound
 * effect, you can call this method to play the specified sound effect to all users in the channel.
 * Each call to this method can only play one sound effect. To play multiple sound effects
 * simultaneously, please call this method multiple times.
 * Applicable scenarios: This method can be called in scenarios involving multiple channels.
 *
 * @note
 * - Agora recommends not playing more than three sound effects at the same time.
 * - The sound effect ID and file path in this method must be consistent with those in the
 * `preloadEffectEx:filePath:` method.
 * - If preloadEffectEx is called before playEffectEx is executed, the file resource will not be closed after playEffectEx. 
 * The next time playEffectEx is executed, it will directly seek to play at the beginning.
 * - If preloadEffectEx is not called before playEffectEx is executed, the resource will be destroyed after playEffectEx. 
 * The next time playEffectEx is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param connection `AgoraRtcConnection` object. See `AgoraRtcConnection`.
 * @param soundId The ID of the audio effect. The unique ID of each audio effect file. If preloading
 * sound effects into memory through the `preloadEffect:filePath:` method, make sure that this
 * parameter value is consistent with the value in the `preloadEffect:filePath:` method.
 * @param filePath The absolute path of the local sound effect file (including the file name
 * extension) or the URL of the online sound effect file, for example
 * /var/mobile/Containers/Data/audio.mp4. Supported audio formats include: mp3, mp4, m4a, aac, 3gp,
 * mkv and wav.
 * @param loopCount Set the number of times the sound effect to be looped:
 * - 0: Play once.
 * - 1: Play twice.
 * - -1: Loop infinitely until calling `stopEffect:` or `stopAllEffects`.
 * @param pitch The pitch of the audio effect. The range is from 0.5 to 2.0, with a default value of
 * 1.0 (original pitch). The lower the value, the lower the pitch.
 * @param pan The spatial position of the audio effect. The range of values is from -1.0 to 1.0:
 * - 0.0: The audio effect is heard in front of the user.
 * - 1.0: The audio effect is heard on the right of the user.
 * - -1.0: The audio effect is heard on the left of the user.
 * @param gain The volume of the audio effect. The value range is 0 to 100, and the default value is
 * 100. The smaller the value, the lower the volume. 
 * @param startPos The playback position (ms) of the audio effect file.
 * @param publish Sets whether to publish the audio effect in a channel:
 * - YES: Publish the audio effect in the channel so that remote user can hear it.
 * - NO: Do not publish the audio effect in the channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playEffectEx:(AgoraRtcConnection* _Nonnull)connection
          soundId:(int)soundId
         filePath:(NSString* _Nonnull)filePath
        loopCount:(NSInteger)loopCount
            pitch:(double)pitch
              pan:(double)pan
             gain:(NSInteger)gain
          publish:(BOOL)publish
         startPos:(int)startPos NS_SWIFT_NAME(playEffectEx(_:soundId:filePath:loopCount:pitch:pan:gain:publish:startPos:));

/** 
 * Adds multiple SDK delegate.
 * 
 * @param delegate The AgoraRtcEngineDelegate object. 
 * @param connection The AgoraRtcConnection object.
 */
- (void)addDelegateEx:(id<AgoraRtcEngineDelegate> _Nonnull)delegate connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(addDelegateEx(_:connection:));

/** 
 * Removes multiple SDK delegate.
 * 
 * @param delegate The AgoraRtcEngineDelegate object. 
 * @param connection The AgoraRtcConnection object.
 */
- (void)removeDelegateEx:(id<AgoraRtcEngineDelegate> _Nonnull)delegate connection:(AgoraRtcConnection * _Nonnull)connection NS_SWIFT_NAME(removeDelegateEx(_:connection:));

/**
 * @brief Enables or disables the built-in encryption.
 *
 * @details
 * After the user leaves the channel, the SDK automatically disables the built-in encryption. To
 * enable the built-in encryption, call this method before the user joins the channel again.
 * Applicable scenarios: Scenarios with higher security requirements.
 * Call timing: Call this method before joining a channel.
 *
 * @note
 * - All users within the same channel must set the same encryption configurations when calling this
 * method.
 * - If you enable the built-in encryption, you cannot use the Media Push function.
 *
 * @param enabled Whether to enable built-in encryption:
 * - YES: Enable the built-in encryption.
 * - NO: (Default) Disable the built-in encryption.
 * @param config Built-in encryption configurations. See `AgoraEncryptionConfig`.
 * @param connection The connection information. See `AgoraRtcConnection`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableEncryptionEx:(BOOL)enabled encryptionConfig:(AgoraEncryptionConfig *_Nonnull)config connection:(AgoraRtcConnection* _Nonnull)connection NS_SWIFT_NAME(enableEncryptionEx(_:encryptionConfig:connection:));
@end

NS_ASSUME_NONNULL_END
