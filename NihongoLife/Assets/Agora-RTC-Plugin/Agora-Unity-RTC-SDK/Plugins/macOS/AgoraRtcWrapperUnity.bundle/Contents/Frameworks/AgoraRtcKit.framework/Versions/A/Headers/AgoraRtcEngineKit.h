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
#import "AgoraRtcEngineDelegate.h"
#import "AgoraAudioFrameDelegate.h"
#import "AgoraAudioEncodedFrameDelegate.h"
#import "AgoraRtcAudioSpectrumDelegate.h"
#import "AgoraVideoFrameDelegate.h"
#import "AgoraFaceInfoDelegate.h"
#import "AgoraEncodedVideoFrameDelegate.h"
#import "AgoraRtcMediaPlayerDelegate.h"
#import "AgoraMediaRecorderDelegate.h"
#import "AgoraMediaMetadataDelegate.h"
#import "AgoraMediaMetadataDataSource.h"
#import "AgoraDirectCdnStreamingEventDelegate.h"
#import "AgoraRtcMediaPlayerCacheManagerProtocol.h"
#import "AgoraMediaRecorder.h"
#import "AgoraVideoEffectObject.h"

/** Agora provides ensured quality of experience (QoE) for worldwide Internet-based voice and video communications through a virtual global network that is especially optimized for real-time web and mobile-to-mobile applications.

 The AgoraRtcEngineKit class is the entry point of the Agora SDK that provides simple APIs for applications to easily start voice and video communication.
 */

#pragma mark - AgoraRtcEngineKit

/**
 * Provides all methods that can be invoked by your application.
 *
 * Agora provides ensured quality of experience (QoE) for worldwide
 * Internet-based voice and video communications through a virtual global
 * network that is especially optimized for real-time web and mobile-to-mobile
 * applications.
 *
 * `AgoraRtcEngineKit` is the basic interface class of Agora Native SDK. Creating an AgoraRtcEngineKit object and then calling the methods of this object enables the use of Agora Native SDK’s communication functionality.
*/

__attribute__((visibility("default"))) @interface AgoraRtcEngineKit : NSObject

#pragma mark Core Methods
/**
 * @brief Sets and retrieves `AgoraRtcEngineDelegate`.
 *
 * @details
 * The SDK uses `AgoraRtcEngineDelegate` to inform the app on engine runtime events. All methods
 * defined in the delegate are optional implementation methods.
 *
 */
@property(nonatomic, weak) id<AgoraRtcEngineDelegate> _Nullable delegate;

/**
 * @brief Creates and initializes `AgoraRtcEngineKit`.
 *
 * @details
 * All called methods provided by the `AgoraRtcEngineKit` class are executed asynchronously. Agora
 * recommends calling these methods in the same thread.
 *
 * @note
 * - Before calling other APIs, you must call this method to create the `AgoraRtcEngineKit` object.
 * - You can create the `AgoraRtcEngineKit` instance either by calling this method or by calling
 * `sharedEngineWithConfig:delegate:`. The difference between `sharedEngineWithConfig:delegate:` and
 * this method is that `sharedEngineWithConfig:delegate:` supports more configurations when creating
 * the `AgoraRtcEngineKit` instance, for example, specifying the region for connection and setting
 * the log files.
 * - The SDK supports creating only one `AgoraRtcEngineKit` instance for an app.
 *
 * @param appId The App ID issued by Agora for your project. Only users in apps with the same App ID
 * can join the same channel and communicate with each other. An App ID can only be used to create
 * one `AgoraRtcEngineKit` instance. To change your App ID, call `destroy` to destroy the current
 * `AgoraRtcEngineKit` instance, and then create a new one.
 * @param delegate The event handler for `AgoraRtcEngineKit`. See `AgoraRtcEngineDelegate`.
 *
 * @return
 * - Returns an initialized `AgoraRtcEngineKit` object if the method call succeeds.
 * - if the method call fails, returns an `AgoraRtcEngineKit` object whose internal engine field is
 * `nil`. You can get the error code via the `delegate`'s `rtcEngine:didOccurError:` callback.
 */
+ (instancetype _Nonnull)sharedEngineWithAppId:(NSString * _Nonnull)appId
                                      delegate:(id<AgoraRtcEngineDelegate> _Nullable)delegate NS_SWIFT_NAME(sharedEngine(withAppId:delegate:));

/**
 * @brief Creates and initializes `AgoraRtcEngineKit`.
 *
 * @details
 * You can create the `AgoraRtcEngineKit` instance either by calling this method or by calling
 * `sharedEngineWithAppId:delegate:`. The difference between `sharedEngineWithAppId:delegate:` and
 * this method is that this method supports more configurations when creating the
 * `AgoraRtcEngineKit` instance, for example, specifying the region for connection and setting the
 * log files.
 * Call timing: Before calling other APIs, you must call this method to create the
 * `AgoraRtcEngineKit` object.
 *
 * @note
 * The SDK supports creating only one `AgoraRtcEngineKit` instance for an app.
 * All called methods provided by the `AgoraRtcEngineKit` class are executed asynchronously. Agora
 * recommends calling these methods in the same thread.
 *
 * @param config Configurations for the `AgoraRtcEngineKit` instance. See `AgoraRtcEngineConfig`.
 * @param delegate The event handler for `AgoraRtcEngineKit`. See `AgoraRtcEngineDelegate`.
 *
 * @return
 * - Returns an initialized `AgoraRtcEngineKit` object if the method call succeeds.
 * - if the method call fails, returns an `AgoraRtcEngineKit` object whose internal engine field is
 * `nil`. You can get the error code via the `delegate`'s `rtcEngine:didOccurError:` callback.
 */
+ (instancetype _Nonnull)sharedEngineWithConfig:(AgoraRtcEngineConfig * _Nonnull)config
                                       delegate:(id<AgoraRtcEngineDelegate> _Nullable)delegate NS_SWIFT_NAME(sharedEngine(with:delegate:));

/**
 * @brief Preloads a channel with `token`, `channelId`, and `uid`.
 *
 * @details
 * When audience members need to switch between different channels frequently, calling the method
 * can help shortening the time of joining a channel, thus reducing the time it takes for audience
 * members to hear and see the host.
 * If you join a preloaded channel, leave it and want to rejoin the same channel, you do not need to
 * call this method unless the token for preloading the channel expires.
 * Call timing: To improve the user experience of preloading channels, Agora recommends that before
 * joining the channel, calling this method as early as possible once confirming the channel name
 * and user information.
 *
 * @note
 * - When calling this method, ensure you set the user role as audience and do not set the audio
 * scenario as `AgoraAudioScenarioChorus`, otherwise, this method does not take effect.
 * - You also need to make sure that the channel name, user ID and token passed in for preloading
 * are the same as the values passed in when joinning the channel, otherwise, this method does not
 * take effect.
 * - One `AgoraRtcEngineKit` instance supports preloading 20 channels at most. When exceeding this
 * limit, the latest 20 preloaded channels take effect.
 * Failing to preload a channel does not mean that you can't join a channel, nor will it increase
 * the time of joining a channel.
 *
 * @param token The token generated on your server for authentication. See .When the token for
 * preloading channels expires, you can update the token based on the number of channels you
 * preload.
 * - When preloading one channel, calling this method to pass in the new token.
 * - When preloading more than one channels:
 *   - If you use a wildcard token for all preloaded channels, call `updatePreloadChannelToken:` to
 * update the token.Note: When generating a wildcard token, ensure the user ID is not set as 0. See
 * `Secure authentication with tokens`.
 *   - If you use different tokens to preload different channels, call this method to pass in your
 * user ID, channel name and the new token.
 * @param channelId The channel name that you want to preload. This parameter signifies the channel
 * in which users engage in real-time audio and video interaction. Under the premise of the same App
 * ID, users who fill in the same channel ID enter the same channel for audio and video interaction.
 * The string length must be less than 64 bytes. Supported characters (89 characters in total):
 * - All lowercase English letters: a to z.
 * - All uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]",
 * "^", "_", "{", "}", "|", "~", ","
 * @param uid The user ID. This parameter is used to identify the user in the channel for real-time
 * audio and video interaction. You need to set and manage user IDs yourself, and ensure that each
 * user ID in the same channel is unique. This parameter is a 32-bit unsigned integer. The value
 * range is 1 to 2^32-1. If the user ID is not assigned (or set to 0), the SDK assigns a random user
 * ID and `rtcEngine:didJoinChannel:withUid:elapsed:` returns it in the callback. Your application
 * must record and maintain the returned user ID, because the SDK does not do so.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 *   - -102: The channel name is invalid. You need to pass in a valid channel name and join the
 * channel again.
 */
- (int)preloadChannelByToken:(NSString * _Nullable)token
                   channelId:(NSString * _Nonnull)channelId
                         uid:(NSUInteger)uid NS_SWIFT_NAME(preloadChannel(byToken:channelId:uid:));

/**
 * @brief Preloads a channel with `token`, `channelId`, and `userAccount`.
 *
 * @details
 * When audience members need to switch between different channels frequently, calling the method
 * can help shortening the time of joining a channel, thus reducing the time it takes for audience
 * members to hear and see the host.
 * If you join a preloaded channel, leave it and want to rejoin the same channel, you do not need to
 * call this method unless the token for preloading the channel expires.
 * Call timing: To improve the user experience of preloading channels, Agora recommends that before
 * joining the channel, calling this method as early as possible once confirming the channel name
 * and user information.
 *
 * @note
 * - When calling this method, ensure you set the user role as audience and do not set the audio
 * scenario as `AgoraAudioScenarioChorus`, otherwise, this method does not take effect.
 * - You also need to make sure that the User Account, channel ID and token passed in for preloading
 * are the same as the values passed in when joining the channel, otherwise, this method does not
 * take effect.
 * - One `AgoraRtcEngineKit` instance supports preloading 20 channels at most. When exceeding this
 * limit, the latest 20 preloaded channels take effect.
 * Failing to preload a channel does not mean that you can't join a channel, nor will it increase
 * the time of joining a channel.
 *
 * @param token The token generated on your server for authentication. See .When the token for
 * preloading channels expires, you can update the token based on the number of channels you
 * preload.
 * - When preloading one channel, calling this method to pass in the new token.
 * - When preloading more than one channels:
 *   - If you use a wildcard token for all preloaded channels, call `updatePreloadChannelToken:` to
 * update the token.Note: When generating a wildcard token, ensure the user ID is not set as 0. See
 * `Secure authentication with tokens`.
 *   - If you use different tokens to preload different channels, call this method to pass in your
 * user ID, channel name and the new token.
 * @param channelId The channel name that you want to preload. This parameter signifies the channel
 * in which users engage in real-time audio and video interaction. Under the premise of the same App
 * ID, users who fill in the same channel ID enter the same channel for audio and video interaction.
 * The string length must be less than 64 bytes. Supported characters (89 characters in total):
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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid. For example, the User Account is empty. You need to pass in a
 * valid parameter and join the channel again.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 *   - -102: The channel name is invalid. You need to pass in a valid channel name and join the
 * channel again.
 */
- (int)preloadChannelByTokenWithUserAccount:(NSString * _Nullable)token
                   channelId:(NSString * _Nonnull)channelId
                 userAccount:(NSString * _Nonnull)userAccount NS_SWIFT_NAME(preloadChannelWithUserAccount(byToken:channelId:userAccount:));

/**
 * @brief Updates the wildcard token for preloading channels.
 *
 * @details
 * You need to maintain the life cycle of the wildcard token by yourself. When the token expires,
 * you need to generate a new wildcard token and then call this method to pass in the new token.
 * Applicable scenarios: In scenarios involving multiple channels, such as switching between
 * different channels, using a wildcard token means users do not need to apply for a new token every
 * time joinning a new channel, which can save users time for switching channels and reduce the
 * pressure on your token server.
 *
 * @param token The new token.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid. For example, the token is invalid. You need to pass in a valid
 * parameter and join the channel again.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 */
- (int)updatePreloadChannelToken:(NSString * _Nonnull)token NS_SWIFT_NAME(updatePreloadChannelToken(_:));

/**
 * @brief Joins a channel.
 *
 * @details
 * By default, the user subscribes to the audio and video streams of all the other users in the
 * channel, giving rise to usage and **billings**. To stop subscribing to a specified stream or all
 * remote streams, call the corresponding `mute` methods.
 * Call timing: Call this method after `sharedEngineWithConfig:delegate:`.
 * Related callbacks: A successful call of this method triggers the following callbacks:
 * - The local client: The `rtcEngine:didJoinChannel:withUid:elapsed:` and
 * `rtcEngine:connectionChangedToState:reason:` callbacks.
 * - The remote client: The `rtcEngine:didJoinedOfUid:elapsed:` callback, if a user joining the
 * channel in the COMMUNICATION profile, or a host joining a channel in the LIVE_BROADCASTING
 * profile.
 * When the connection between the local client and Agora's server is interrupted due to poor
 * network conditions, the SDK tries reconnecting to the server. When the local client successfully
 * rejoins the channel, the SDK triggers the `rtcEngine:didRejoinChannel:withUid:elapsed:` callback
 * on the local client.
 *
 * @note
 * - This method only supports users joining one channel at a time.
 * - Users with different App IDs cannot call each other.
 * - Before joining a channel, ensure that the App ID you use to generate a token is the same as
 * that you pass in the `sharedEngineWithConfig:delegate:` method; otherwise, you may fail to join
 * the channel with the token.
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
 * @param info (Optional) Reserved for future use.
 * @param uid The user ID. This parameter is used to identify the user in the channel for real-time
 * audio and video interaction. You need to set and manage user IDs yourself, and ensure that each
 * user ID in the same channel is unique. This parameter is a 32-bit unsigned integer. The value
 * range is 1 to 2^32-1. If the user ID is not assigned (or set to 0), the SDK assigns a random user
 * ID and `rtcEngine:didJoinChannel:withUid:elapsed:` returns it in the callback. Your application
 * must record and maintain the returned user ID, because the SDK does not do so.
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
- (int)joinChannelByToken:(NSString * _Nullable)token
                channelId:(NSString * _Nonnull)channelId
                     info:(NSString * _Nullable)info
                      uid:(NSUInteger)uid
              joinSuccess:(void(^ _Nullable)(NSString * _Nonnull channel, NSUInteger uid, NSInteger elapsed))joinSuccessBlock NS_SWIFT_NAME(joinChannel(byToken:channelId:info:uid:joinSuccess:));

/**
 * @brief Joins a channel with media options.
 *
 * @details
 * Compared to `joinChannelByToken:channelId:info:uid:joinSuccess:`, this method has the `options`
 * parameter which is used to set media options, such as whether to publish audio and video streams
 * within a channel, or whether to automatically subscribe to the audio and video streams of all
 * remote users when joining a channel. By default, the user subscribes to the audio and video
 * streams of all the other users in the channel, giving rise to usage and **billings**. To stop
 * subscribing to other streams, set the `options` parameter or call the corresponding `mute`
 * methods.
 * Call timing: Call this method after `sharedEngineWithConfig:delegate:`.
 * Related callbacks: A successful call of this method triggers the following callbacks:
 * - The local client: The `rtcEngine:didJoinChannel:withUid:elapsed:` and
 * `rtcEngine:connectionChangedToState:reason:` callbacks.
 * - The remote client: The `rtcEngine:didJoinedOfUid:elapsed:` callback, if a user joining the
 * channel in the COMMUNICATION profile, or a host joining a channel in the LIVE_BROADCASTING
 * profile.
 * When the connection between the local client and Agora's server is interrupted due to poor
 * network conditions, the SDK tries reconnecting to the server. When the local client successfully
 * rejoins the channel, the SDK triggers the `rtcEngine:didRejoinChannel:withUid:elapsed:` callback
 * on the local client.
 *
 * @note
 * - This method only supports users joining one channel at a time.
 * - Users with different App IDs cannot call each other.
 * - Before joining a channel, ensure that the App ID you use to generate a token is the same as
 * that you pass in the `sharedEngineWithConfig:delegate:` method; otherwise, you may fail to join
 * the channel with the token.
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
 * @param uid The user ID. This parameter is used to identify the user in the channel for real-time
 * audio and video interaction. You need to set and manage user IDs yourself, and ensure that each
 * user ID in the same channel is unique. This parameter is a 32-bit unsigned integer. The value
 * range is 1 to 2^32-1. If the user ID is not assigned (or set to 0), the SDK assigns a random user
 * ID and `rtcEngine:didJoinChannel:withUid:elapsed:` returns it in the callback. Your application
 * must record and maintain the returned user ID, because the SDK does not do so.
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
- (int)joinChannelByToken:(NSString * _Nullable)token
                channelId:(NSString * _Nonnull)channelId
                      uid:(NSUInteger)uid
             mediaOptions:(AgoraRtcChannelMediaOptions * _Nonnull)mediaOptions
              joinSuccess:(void(^ _Nullable)(NSString * _Nonnull channel, NSUInteger uid, NSInteger elapsed))joinSuccessBlock NS_SWIFT_NAME(joinChannel(byToken:channelId:uid:mediaOptions:joinSuccess:));

/**
 * @brief Leaves a channel.
 *
 * @details
 * After calling this method, the SDK terminates the audio and video interaction, leaves the current
 * channel, and releases all resources related to the session.
 * After joining the channel, you must call this method to end the call; otherwise, the next call
 * cannot be started.
 * Call timing: Call this method after joining a channel.
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
 * - If you have called `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:` to join
 * multiple channels, calling this method will leave all the channels you joined.
 *
 * @param leaveChannelBlock This callback indicates that a user leaves a channel, and provides the
 * statistics of the call in `AgoraChannelStats`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 *   - -7: The SDK is not initialized.
 */
- (int)leaveChannel:(void(^ _Nullable)(AgoraChannelStats * _Nonnull stat))leaveChannelBlock NS_SWIFT_NAME(leaveChannel(_:));

/**
 * @brief Sets channel options and leaves the channel.
 *
 * @details
 * After calling this method, the SDK terminates the audio and video interaction, leaves the current
 * channel, and releases all resources related to the session.
 * After joining a channel, you must call this method or `leaveChannel:` to end the call, otherwise,
 * the next call cannot be started. If you have called
 * `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:` to join multiple channels,
 * calling this method will leave all the channels you joined.
 * Call timing: Call this method after joining a channel.
 * Related callbacks: A successful call of this method triggers the following callbacks:
 * - The local client: The `rtcEngine:didLeaveChannelWithStats:` callback will be triggered.
 * - The remote client: The `rtcEngine:didOfflineOfUid:reason:` callback will be triggered after the
 * remote host leaves the channel.
 *
 * @note
 * If you call `destroy` immediately after calling this method, the SDK does not trigger the
 * `rtcEngine:didLeaveChannelWithStats:` callback.
 * This method call is asynchronous. When this method returns, it does not necessarily mean that the
 * user has left the channel.
 *
 * @param options The options for leaving the channel. See `AgoraLeaveChannelOptions`.
 * @param leaveChannelBlock This callback indicates that a user leaves a channel, and provides the
 * statistics of the call in `AgoraChannelStats`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)leaveChannel:(AgoraLeaveChannelOptions * _Nonnull)options
leaveChannelBlock:(void (^ _Nullable)(AgoraChannelStats * _Nonnull))leaveChannelBlock NS_SWIFT_NAME(leaveChannel(_:leaveChannelBlock:));

/**
 * @brief Sets the channel profile.
 *
 * @details
 * You can call this method to set the channel profile. The SDK adopts different optimization
 * strategies for different channel profiles. For example, in a live streaming scenario, the SDK
 * prioritizes video quality. After initializing the SDK, the default channel profile is the live
 * streaming profile.
 * Call timing: Call this method before joining a channel.
 *
 * @note
 * - To ensure the quality of real-time communication, Agora recommends that all users in a channel
 * use the same channel profile.
 * - on iOS, the default audio routing of the SDK is different in different channel scenarios, See
 * `setDefaultAudioRouteToSpeakerphone:`.
 *
 * @param profile The channel profile. See `AgoraChannelProfile`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid.
 *   - -7: The SDK is not initialized.
 */
- (int)setChannelProfile:(AgoraChannelProfile)profile NS_SWIFT_NAME(setChannelProfile(_:));

/**
 * @brief Updates the channel media options after joining the channel.
 *
 * @param mediaOptions The channel media options. See `AgoraRtcChannelMediaOptions`.
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
- (int)updateChannelWithMediaOptions:(AgoraRtcChannelMediaOptions* _Nonnull)mediaOptions NS_SWIFT_NAME(updateChannel(with:));

/**
 * @brief Sets the client role.
 *
 * @details
 * By default,the SDK sets the user role as audience. You can call this method to set the user role
 * as host. The user role ( `roles` ) determines the users' permissions at the SDK level, including
 * whether they can publish audio and video streams in a channel.
 * Call timing: You can call this method either before or after joining a channel.
 * If you call this method to set the user role as the host before joining the channel and set the
 * local video property through the `setupLocalVideo:` method, the local video preview is
 * automatically enabled when the user joins the channel.
 * If you call this method to set the user role after joining a channel, the SDK will automatically
 * call the `muteLocalAudioStream:` and `muteLocalVideoStream:` method to change the state for
 * publishing audio and video streams.
 * Related callbacks: If you call this method to switch the user role after joining the channel, the
 * SDK triggers the following callbacks:
 * - Triggers `rtcEngine:didClientRoleChanged:newRole:newRoleOptions:` on the local client.Note:
 * Calling this method before joining a channel and set the `role` to `AUDIENCE` will trigger this
 * callback as well.
 * - Triggers `rtcEngine:didJoinedOfUid:elapsed:` or `rtcEngine:didOfflineOfUid:reason:` on the
 * remote client.
 * If you call this method to set the user role after joining a channel but encounter a failure, the
 * SDK trigger the `rtcEngine:didClientRoleChangeFailed:currentRole:` callback to report the reason
 * for the failure and the current user role.
 *
 * @note
 * When calling this method before joining a channel and setting the user role to `BROADCASTER`, the
 * `rtcEngine:didClientRoleChanged:newRole:newRoleOptions:` callback will not be triggered on the
 * local client.
 * Calling this method before joining a channel and set the `role` to `AUDIENCE` will trigger this
 * callback as well.
 *
 * @param role The user role. See `AgoraClientRole`.
 * Note: If you set the user role as an audience member, you cannot publish audio and video streams
 * in the channel. If you want to publish media streams in a channel during live streaming, ensure
 * you set the user role as broadcaster.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 *   - -7: The SDK is not initialized.
 */
- (int)setClientRole:(AgoraClientRole)role NS_SWIFT_NAME(setClientRole(_:));

/**
 * @brief Sets the user role and the audience latency level in a live streaming scenario.
 *
 * @details
 * By default,the SDK sets the user role as audience. You can call this method to set the user role
 * as host. The user role ( `roles` ) determines the users' permissions at the SDK level, including
 * whether they can publish audio and video streams in a channel.
 * The difference between this method and `setClientRole:` is that, this method supports setting the
 * `audienceLatencyLevel`. `audienceLatencyLevel` needs to be used together with `role` to determine
 * the level of service that users can enjoy within their permissions. For example, an audience
 * member can choose to receive remote streams with low latency or ultra-low latency.
 * Call timing: You can call this method either before or after joining a channel.
 * If you call this method to set the user role as the host before joining the channel and set the
 * local video property through the `setupLocalVideo:` method, the local video preview is
 * automatically enabled when the user joins the channel.
 * If you call this method to set the user role after joining a channel, the SDK will automatically
 * call the `muteLocalAudioStream:` and `muteLocalVideoStream:` method to change the state for
 * publishing audio and video streams.
 * Related callbacks: If you call this method to switch the user role after joining the channel, the
 * SDK triggers the following callbacks:
 * - Triggers `rtcEngine:didClientRoleChanged:newRole:newRoleOptions:` on the local client.Note:
 * Calling this method before joining a channel and set the `role` to `AUDIENCE` will trigger this
 * callback as well.
 * - Triggers `rtcEngine:didJoinedOfUid:elapsed:` or `rtcEngine:didOfflineOfUid:reason:` on the
 * remote client.
 * If you call this method to set the user role after joining a channel but encounter a failure, the
 * SDK trigger the `rtcEngine:didClientRoleChangeFailed:currentRole:` callback to report the reason
 * for the failure and the current user role.
 *
 * @note
 * When the user role is set to host, the audience latency level can only be set to
 * AgoraAudienceLatencyLevelUltraLowLatency.
 * When calling this method before joining a channel and setting the `role` to `BROADCASTER`, the
 * `rtcEngine:didClientRoleChanged:newRole:newRoleOptions:` callback will not be triggered on the
 * local client.
 * Calling this method before joining a channel and set the `role` to `AUDIENCE` will trigger this
 * callback as well.
 *
 * @param role The user role. See `AgoraClientRole`.
 * Note: If you set the user role as an audience member, you cannot publish audio and video streams
 * in the channel. If you want to publish media streams in a channel during live streaming, ensure
 * you set the user role as broadcaster.
 * @param options The detailed options of a user, including the user level. See
 * `AgoraClientRoleOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 *   - -5: The request is rejected.
 *   - -7: The SDK is not initialized.
 */
- (int)setClientRole:(AgoraClientRole)role options:(AgoraClientRoleOptions * _Nullable)options NS_SWIFT_NAME(setClientRole(_:options:));

/**
 * @brief Renews the token.
 *
 * @details
 * This method is used to update the token. After successfully calling this method, the SDK will
 * trigger the `rtcEngine:renewTokenResult:code:` callback. A token will expire after a certain
 * period of time, at which point the SDK will be unable to establish a connection with the server.
 * Call timing: In any of the following cases, Agora recommends that you generate a new token on
 * your server and then call this method to renew your token:
 * - Receiving the `rtcEngine:tokenPrivilegeWillExpire:` callback reporting the token is about to
 * expire.
 * - Receiving the `rtcEngineRequestToken:` callback reporting the token has expired.
 * - Receiving the `rtcEngine:connectionChangedToState:reason:` callback reporting
 * `AgoraConnectionChangedReasonTokenExpired` (9).
 *
 * @param token The new token.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid. For example, the token is empty.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 *   - 110: Invalid token. Ensure the following:
 *     - The user ID specified when generating the token is consistent with the user ID used when
 * joining the channel.
 *     - The generated token is the same as the token passed in to join the channel.
 */
- (int)renewToken:(NSString * _Nonnull)token NS_SWIFT_NAME(renewToken(_:));

/**
 * @brief Gets the current connection state of the SDK.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @return
 * The current connection state. See `AgoraConnectionState`.
 */
- (AgoraConnectionState)getConnectionState NS_SWIFT_NAME(getConnectionState());

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 *   - -8: Internal state error. Probably because the user is not a broadcaster.
 */
- (int)startOrUpdateChannelMediaRelay:(AgoraChannelMediaRelayConfiguration * _Nonnull)config NS_SWIFT_NAME(startOrUpdateChannelMediaRelay(_:));

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
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -5: The method call was rejected. There is no ongoing channel media relay.
 */
- (int)stopChannelMediaRelay NS_SWIFT_NAME(stopChannelMediaRelay());

/**
 * @brief Pauses the media stream relay to all target channels.
 *
 * @details
 * After the cross-channel media stream relay starts, you can call this method to pause relaying
 * media streams to all target channels; after the pause, if you want to resume the relay, call
 * `resumeAllChannelMediaRelay`.
 *
 * @note Call this method after `startOrUpdateChannelMediaRelay:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -5: The method call was rejected. There is no ongoing channel media relay.
 */
- (int)pauseAllChannelMediaRelay NS_SWIFT_NAME(pauseAllChannelMediaRelay());

/**
 * @brief Resumes the media stream relay to all target channels.
 *
 * @details
 * After calling the `pauseAllChannelMediaRelay` method, you can call this method to resume relaying
 * media streams to all destination channels.
 *
 * @note Call this method after `pauseAllChannelMediaRelay`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -5: The method call was rejected. There is no paused channel media relay.
 */
- (int)resumeAllChannelMediaRelay NS_SWIFT_NAME(resumeAllChannelMediaRelay());

/**
 * @brief Starts the local video mixing.
 *
 * @details
 * After calling this method, you can merge multiple video streams into one video stream locally.
 * For example, you can merge the video streams captured by the camera, screen sharing, media
 * player, remote video, video files, images, etc. into one video stream, and then publish the mixed
 * video stream to the channel.
 * Applicable scenarios: You can enable the local video mixing function in scenarios such as remote
 * conferences, live streaming, and online education, which allows users to view and manage multiple
 * videos more conveniently, and supports portrait-in-picture effect and other functions.
 * The following is a typical use case for implementing the portrait-in-picture effect:1. Call
 * `enableVirtualBackground:backData:segData:`, and set the custom background image to
 * `AgoraVirtualBackgroundNone`, that is, separate the portrait and the background in the video
 * captured by the camera.
 * 2. Call `startScreenCapture:` (iOS)/`startScreenCapture:config:` (macOS) to start capturing the
 * screen sharing video stream.
 * 3. Call this method and set the video source for capturing portraits as one of the video sources
 * participating in the local video mixing, picture-in-picture of the portrait can be achived in the
 * mixed video.
 * Call timing: - If you need to mix the locally collected video streams, you need to call this
 * method after `startCameraCapture:config:` or `startScreenCapture:`
 * (iOS)/`startScreenCapture:config:` (macOS).
 * - If you want to publish the mixed video stream to the channel, you need to set
 * `publishTranscodedVideoTrack` in `AgoraRtcChannelMediaOptions` to `YES` when calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` or `updateChannelWithMediaOptions:`.
 * Related callbacks: When you fail to call this method, the SDK triggers the
 * `rtcEngine:didLocalVideoTranscoderErrorWithStream:errorCode:` callback to report the reason.
 *
 * @note
 * - Local video mixing requires more CPU resources. Therefore, Agora recommends enabling this
 * function on devices with higher performance.
 * - If you need to mix locally captured video streams, the SDK supports the following capture
 * combinations:
 *   - On iOS, it supports up to 2 video streams captured by cameras (the device itself
 * needs to support dual cameras or support external cameras) + 1 screen sharing stream.
 *   - On macOS, it supports up to 4 video streams captured by cameras + 1 screen
 * sharing stream.
 * - When configuring the local video mixing, it is necessary to ensure that the layer number of the
 * video stream capturing the portrait is greater than the layer number of the screen sharing
 * stream. Otherwise, the portrait will be covered by the screen sharing and will not be displayed
 * in the final mixed video stream.
 *
 * @param config Configuration of the local video mixing, see
 * `AgoraLocalTranscoderConfiguration`.Attention:
 * - The maximum resolution of each video stream participating in the local video mixing is 4096 ×
 * 2160. If this limit is exceeded, video mixing does not take effect.
 * - The maximum resolution of the mixed video stream is 4096 × 2160.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startLocalVideoTranscoder:(AgoraLocalTranscoderConfiguration* _Nonnull)config NS_SWIFT_NAME(startLocalVideoTranscoder(_:));

/**
 * @brief Updates the local video mixing configuration.
 *
 * @details
 * After calling `startLocalVideoTranscoder:`, call this method if you want to update the local
 * video mixing configuration.
 *
 * @note If you want to update the video source type used for local video mixing, such as adding a
 * second camera or screen to capture video, you need to call this method after
 * `startCameraCapture:config:` or `startScreenCapture:` (iOS)/ `startScreenCapture:config:`
 * (macOS).
 *
 * @param config Configuration of the local video mixing, see `AgoraLocalTranscoderConfiguration`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateLocalTranscoderConfiguration:(AgoraLocalTranscoderConfiguration* _Nonnull)config NS_SWIFT_NAME(updateLocalTranscoderConfiguration(_:));

/**
 * @brief Stops the local video mixing.
 *
 * @details
 * After calling `startLocalVideoTranscoder:`, call this method if you want to stop the local video
 * mixing.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopLocalVideoTranscoder NS_SWIFT_NAME(stopLocalVideoTranscoder());

/**
 * @brief Starts local audio mixing.
 *
 * @details
 * This method supports merging multiple audio streams into one audio stream locally. For example,
 * merging the audio streams captured from the local microphone, and that from the media player, the
 * sound card, and the remote users into one audio stream, and then publish the merged audio stream
 * to the channel.
 * - If you want to mix the locally captured audio streams, you can set publishMixedAudioTrack in
 * `AgoraRtcChannelMediaOptions` to `YES`, and then publish the mixed audio stream to the channel.
 * - If you want to mix the remote audio stream, ensure that the remote audio stream has been
 * published in the channel and you have subcribed to the audio stream that you need to mix.
 * Applicable scenarios: You can enable this function in the following scenarios:
 * - By utilizing the local video mixing feature, the associated audio streams of the mixed video
 * streams can be simultaneously captured and published.
 * - In live streaming scenarios, users can receive audio streams within the channel, mix multiple
 * audio streams locally, and then forward the mixed audio stream to other channels.
 * - In online classes, teachers can mix the audio from interactions with students locally and then
 * forward the mixed audio stream to other channels.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @note To ensure audio quality, it is recommended that the number of audio streams to be mixed
 * does not exceed 10.
 *
 * @param config The configurations for mixing the lcoal audio. See
 * `AgoraLocalAudioMixerConfiguration`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 */
- (int)startLocalAudioMixer:(AgoraLocalAudioMixerConfiguration* _Nonnull)config NS_SWIFT_NAME(startLocalAudioMixer(_:));

/**
 * @brief Updates the configurations for mixing audio streams locally.
 *
 * @details
 * After calling `startLocalAudioMixer`, call this method if you want to update the local audio
 * mixing configuration.
 * Call timing: Call this method after `startLocalAudioMixer`.
 *
 * @note To ensure audio quality, it is recommended that the number of audio streams to be mixed
 * does not exceed 10.
 *
 * @param config The configurations for mixing the lcoal audio. See
 * `AgoraLocalAudioMixerConfiguration`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 */
- (int)updateLocalAudioMixerConfiguration:(AgoraLocalAudioMixerConfiguration* _Nonnull)config NS_SWIFT_NAME(updateLocalAudioMixerConfiguration(_:));

/**
 * @brief Stops the local audio mixing.
 *
 * @details
 * After calling `startLocalAudioMixer`, call this method if you want to stop the local audio
 * mixing.
 * Call timing: Call this method after `startLocalAudioMixer`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 */
- (int)stopLocalAudioMixer NS_SWIFT_NAME(stopLocalAudioMixer());

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
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -7: The method is called before `AgoraRtcEngineKit` is initialized.
 */
- (int)startMediaRenderingTracing NS_SWIFT_NAME(startMediaRenderingTracing());

/**
 * @brief Enables audio and video frame instant rendering.
 *
 * @since v4.1.1
 *
 * @details
 * After successfully calling this method, the SDK enables the instant frame rendering mode, which
 * can speed up the first frame rendering after the user joins the channel.
 * Applicable scenarios: Agora recommends that you enable this mode for the audience in a live
 * streaming scenario.
 * Call timing: Call this method before joining a channel.
 *
 * @note
 * Both the host and audience member need to call this method in order to experience instant
 * rendering of audio and video frames.
 * Once the method is successfully called, you can only cancel instant rendering by calling
 * `destroy` to destroy the `AgoraRtcEngineKit` object.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -7: The method is called before `AgoraRtcEngineKit` is initialized.
 */
- (int)enableInstantMediaRendering NS_SWIFT_NAME(enableInstantMediaRendering());

/**
 * @brief Releases the `AgoraRtcEngineKit` instance.
 *
 * @details
 * This method releases all resources used by the Agora SDK. Use this method for apps in which users
 * occasionally make voice or video calls. When users do not make calls, you can free up resources
 * for other operations.
 * After a successful method call, you can no longer use any method or callback in the SDK anymore.
 * If you want to use the real-time communication functions again, you must call
 * `sharedEngineWithConfig:delegate:` to create a new `AgoraRtcEngineKit` instance.
 *
 * @note - Agora does not recommend you calling `destroy` in any callback of the SDK. Otherwise, the
 * SDK cannot release the resources until the callbacks return results, which may result in a
 * deadlock.
 *
 */
+ (void)destroy NS_SWIFT_NAME(destroy());

/**
 * @brief Destroys the `AgoraRtcEngineKit` instance and releases related resources.
 *
 * @since v4.6.0
 *
 * @details
 * When you no longer need real-time communication, call this method to release the
 * `AgoraRtcEngineKit` instance and its related resources, so that the released resources can be
 * used for other operations. It is recommended for scenarios where users make voice or video
 * interactions.
 *
 * @param engineReleasedBlock When the `AgoraRtcEngineKit` instance is destroyed, the callback is
 * triggered to configure the synchronous or asynchronous destruction:
 * - Non-`nil`：Asynchronous destruction. The method returns immediately, while the engine resources
 * might not be fully released. The `engineReleasedBlock` callback is triggered after the engine is
 * destroyed.
 * - `nil`：Synchronous destruction. The method returns only after the engine resources are fully
 * released.
 *
 */
+ (void)destroy:(void (^ _Nullable)(void))engineReleasedBlock NS_SWIFT_NAME(destroy(_:));

#pragma mark Core Audio

/**
 * @brief Enables the audio module.
 *
 * @details
 * The audio module is enabled by default After calling `disableAudio` to disable the audio module,
 * you can call this method to re-enable it.
 * Call timing: This method can be called either before or after joining the channel. It is still
 * valid after one leaves channel.
 *
 * @note
 * - Calling this method will reset the entire engine, resulting in a slow response time. You can
 * use the following methods to independently control a specific function of the audio module based
 * on your actual needs:
 *   - `enableLocalAudio:`: Whether to enable the microphone to create the local audio stream.
 *   - `muteLocalAudioStream:`: Whether to publish the local audio stream.
 *   - `muteRemoteAudioStream:mute:`: Whether to subscribe and play the remote audio stream.
 *   - `muteAllRemoteAudioStreams:`: Whether to subscribe to and play all remote audio streams.
 * - A successful call of this method resets `enableLocalAudio:`, `muteRemoteAudioStream:mute:`, and
 * `muteAllRemoteAudioStreams:`. Proceed it with caution.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableAudio NS_SWIFT_NAME(enableAudio());

/**
 * @brief Disables the audio module.
 *
 * @details
 * The audio module is enabled by default, and you can call this method to disable the audio module.
 * Call timing: This method can be called either before or after joining the channel. It is still
 * valid after one leaves channel.
 *
 * @note
 * This method resets the internal engine and takes some time to take effect. Agora recommends using
 * the following API methods to control the audio modules separately:
 * - `enableLocalAudio:`: Whether to enable the microphone to create the local audio stream.
 * - (macOS only) `enableLoopbackRecording:deviceName:`: Whether to enable loopback audio capturing.
 * - `muteLocalAudioStream:`: Whether to publish the local audio stream.
 * - `muteRemoteAudioStream:mute:`: Whether to subscribe and play the remote audio stream.
 * - `muteAllRemoteAudioStreams:`: Whether to subscribe to and play all remote audio streams.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)disableAudio NS_SWIFT_NAME(disableAudio());

/**
 * @brief Enables or disables the local audio capture.
 *
 * @details
 * The audio function is enabled by default when users joining a channel. This method disables or
 * re-enables the local audio function to stop or restart local audio capturing.
 * The difference between this method and `muteLocalAudioStream:` are as follows:
 * - `enableLocalAudio:`: Disables or re-enables the local audio capturing and processing. If you
 * disable or re-enable local audio capturing using the `enableLocalAudio:` method, the local user
 * might hear a pause in the remote audio playback.
 * - `muteLocalAudioStream:`: Sends or stops sending the local audio streams without affecting the
 * audio capture status.
 * Applicable scenarios: This method does not affect receiving the remote audio streams.
 * `enableLocalAudio:` `(NO)` is suitable for scenarios where the user wants to receive remote audio
 * streams without sending locally captured audio.
 * Call timing: You can call this method either before or after joining a channel. Calling it before
 * joining a channel only sets the device state, and it takes effect immediately after you join the
 * channel.
 * Related callbacks: Once the local audio function is disabled or re-enabled, the SDK triggers the
 * `rtcEngine:localAudioStateChanged:reason:` callback, which reports `AgoraAudioLocalStateStopped`
 * (0) or `AgoraAudioLocalStateRecording` (1).
 *
 * @param enabled - `YES`: (Default) Re-enable the local audio function, that is, to start the local
 * audio capturing device (for example, the microphone).
 * - `NO`: Disable the local audio function, that is, to stop local audio capturing.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableLocalAudio:(BOOL)enabled NS_SWIFT_NAME(enableLocalAudio(_:));

/**
 * @brief Sets the audio profile.
 *
 * @details
 * If you need to set the audio scenario, you can either call `setAudioScenario:`, or
 * `sharedEngineWithConfig:delegate:` and set the `audioScenario` in `AgoraRtcEngineConfig`.
 * Applicable scenarios: This method is suitable for various audio scenarios. You can choose as
 * needed. For example, in scenarios with high audio quality requirements such as music teaching, it
 * is recommended to set `profile` to `AgoraAudioProfileMusicHighQuality`(4).
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @param profile The audio profile, including the sampling rate, bitrate, encoding mode, and the
 * number of channels. See `AgoraAudioProfile`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioProfile:(AgoraAudioProfile)profile NS_SWIFT_NAME(setAudioProfile(_:));

/**
 * @brief Sets the audio scenario.
 *
 * @details
 * Applicable scenarios: This method is suitable for various audio scenarios. You can choose as
 * needed. For example, in scenarios such as music teaching that require high sound quality, it is
 * recommended to set `scenario` to `AgoraAudioScenarioGameStreaming`(3).
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @note Due to iOS system restrictions, some audio routes cannot be recognized in call volume mode.
 * Therefore, if you need to use an external sound card, it is recommended to set the audio scenario
 * to `AgoraAudioScenarioGameStreaming`(3). In this scenario, the SDK will switch to media volume to
 * avoid this issue.
 *
 * @param scenario The audio scenarios. Under different audio scenarios, the device uses different
 * volume types. See `AgoraAudioScenario`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioScenario:(AgoraAudioScenario)scenario NS_SWIFT_NAME(setAudioScenario(_:));

/**
 * @brief Enables the reporting of users' volume indication.
 *
 * @details
 * This method enables the SDK to regularly report the volume information to the app of the local
 * user who sends a stream and remote users (three users at most) whose instantaneous volumes are
 * the highest.
 * Call timing: This method can be called either before or after joining the channel.
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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableAudioVolumeIndication:(NSInteger)interval
                            smooth:(NSInteger)smooth
                         reportVad:(BOOL)reportVad NS_SWIFT_NAME(enableAudioVolumeIndication(_:smooth:reportVad:));


#if TARGET_OS_IPHONE
/**
 * @brief Enables/Disables the audio route to the speakerphone.
 *
 * @details
 * Applicable scenarios: If the default audio route of the SDK or the setting in
 * `setDefaultAudioRouteToSpeakerphone:` cannot meet your requirements, you can call this method to
 * switch the current audio route.
 * Call timing: Call this method after joining a channel.
 * Related callbacks: After successfully calling this method, the SDK triggers the
 * `rtcEngine:didAudioRouteChanged:` callback to report the current audio route.
 *
 * @note
 * - This method only sets the audio route in the current channel and does not influence the default
 * audio route. If the user leaves the current channel and joins another channel, the default audio
 * route is used.
 * - If the user uses an external audio playback device such as a Bluetooth or wired headset, this
 * method does not take effect, and the SDK plays audio through the external device. When the user
 * uses multiple external devices, the SDK plays audio through the last connected device.
 *
 * @param enableSpeaker Sets whether to enable the speakerphone or earpiece:
 * - `YES`: Enable device state monitoring. The audio route is the speakerphone.
 * - `NO`: Disable device state monitoring. The audio route is the earpiece.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setEnableSpeakerphone:(BOOL)enableSpeaker NS_SWIFT_NAME(setEnableSpeakerphone(_:));

/**
 * @brief Checks whether the speakerphone is enabled.
 *
 * @details
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @return
 * - `YES`: The speakerphone is enabled, and the audio plays from the speakerphone.
 * - `NO`: The speakerphone is not enabled, and the audio plays from devices other than the
 * speakerphone. For example, the headset or earpiece.
 */
- (BOOL)isSpeakerphoneEnabled NS_SWIFT_NAME(isSpeakerphoneEnabled());

/**
 * @brief Sets the default audio playback route.
 *
 * @details
 * Most mobile phones have two audio routes: an earpiece at the top, and a speakerphone at the
 * bottom. The earpiece plays at a lower volume, and the speakerphone at a higher volume. When
 * setting the default audio route, you determine whether audio playback comes through the earpiece
 * or speakerphone when no external audio device is connected.
 * In different scenarios, the default audio routing of the system is also different. See the
 * following:
 * - Voice call: Earpiece.
 * - Audio broadcast: Speakerphone.
 * - Video call: Speakerphone.
 * - Video broadcast: Speakerphone.
 * You can call this method to change the default audio route.
 * Call timing: Call this method before joining a channel. If you need to change the audio route
 * after joining a channel, call `setEnableSpeakerphone:`.
 * Related callbacks: After successfully calling this method, the SDK triggers the
 * `rtcEngine:didAudioRouteChanged:` callback to report the current audio route.
 *
 * @note After calling this method to set the default audio route, the actual audio route of the
 * system will change with the connection of external audio devices (wired headphones or Bluetooth
 * headphones).
 *
 * @param defaultToSpeaker Whether to set the speakerphone as the default audio route:
 * - `YES`: Set the speakerphone as the default audio route.
 * - `NO`: Set the earpiece as the default audio route.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDefaultAudioRouteToSpeakerphone:(BOOL)defaultToSpeaker NS_SWIFT_NAME(setDefaultAudioRouteToSpeakerphone(_:));
#endif

/**
 * @brief Adjusts the capturing signal volume.
 *
 * @details
 * If you only need to mute the audio signal, Agora recommends that you use `muteRecordingSignal:`
 * instead.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param volume The volume of the user. The value range is [0,400].
 * - 0: Mute.
 * - 100: (Default) The original volume.
 * - 400: Four times the original volume (amplifying the audio signals by four times).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustRecordingSignalVolume:(NSInteger)volume NS_SWIFT_NAME(adjustRecordingSignalVolume(_:));

/**
 * @brief Adjusts the playback signal volume of all remote users.
 *
 * @details
 * This method is used to adjust the signal volume of all remote users mixed and played locally. If
 * you need to adjust the signal volume of a specified remote user played locally, it is recommended
 * that you call `adjustUserPlaybackSignalVolume:volume:` instead.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param volume The volume of the user. The value range is [0,400].
 * - 0: Mute.
 * - 100: (Default) The original volume.
 * - 400: Four times the original volume (amplifying the audio signals by four times).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustPlaybackSignalVolume:(NSInteger)volume NS_SWIFT_NAME(adjustPlaybackSignalVolume(_:));

/**
 * @brief Adjusts the playback signal volume of a specified remote user.
 *
 * @details
 * You can call this method to adjust the playback volume of a specified remote user. To adjust the
 * playback volume of different remote users, call the method as many times, once for each remote
 * user.
 * Call timing: Call this method after joining a channel.
 *
 * @param uid The user ID of the remote user.
 * @param volume The volume of the user. The value range is [0,400].
 * - 0: Mute.
 * - 100: (Default) The original volume.
 * - 400: Four times the original volume (amplifying the audio signals by four times).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustUserPlaybackSignalVolume:(NSUInteger)uid volume:(int)volume NS_SWIFT_NAME(adjustUserPlaybackSignalVolume(_:volume:));

/**
 * @brief Whether to mute the recording signal.
 *
 * @details
 * If you have already called `adjustRecordingSignalVolume:` to adjust the recording signal volume,
 * when you call this method and set it to `YES`, the SDK behaves as follows:1. Records the adjusted
 * volume.
 * 2. Mutes the recording signal.
 * When you call this method again and set it to `NO`, the recording signal volume will be restored
 * to the volume recorded by the SDK before muting.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param muted - `YES`: Mute the recording signal.
 * - `NO`: (Default) Do not mute the recording signal.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteRecordingSignal:(BOOL)muted NS_SWIFT_NAME(muteRecordingSignal(_:));

/**
 * @brief Stops or resumes publishing the local audio stream.
 *
 * @details
 * This method is used to control whether to publish the locally captured audio stream. If you call
 * this method to stop publishing locally captured audio streams, the audio capturing device will
 * still work and won't be affected.
 * Call timing: This method can be called either before or after joining the channel.
 * Related callbacks: After successfully calling this method, the local end triggers callback
 * `rtcEngine:didAudioPublishStateChange:oldState:newState:elapseSinceLastState:`; the remote end
 * triggers `rtcEngine:didAudioMuted:byUid:` and
 * `rtcEngine:remoteAudioStateChangedOfUid:state:reason:elapsed:` callbacks.
 *
 * @param mute Whether to stop publishing the local audio stream:
 * - `YES`: Stops publishing the local audio stream.
 * - `NO`: (Default) Resumes publishing the local audio stream.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteLocalAudioStream:(BOOL)mute NS_SWIFT_NAME(muteLocalAudioStream(_:));

/**
 * @brief Stops or resumes subscribing to the audio stream of a specified user.
 *
 * @details
 * Call timing: Call this method after joining a channel.
 * Related callbacks: After a successful method call, the SDK triggers the
 * `rtcEngine:didAudioSubscribeStateChange:uid:oldState:newState:elapseSinceLastState:` callback.
 *
 * @param uid The user ID of the specified user.
 * @param mute Whether to subscribe to the specified remote user's audio stream.
 * - `YES`: Stop subscribing to the audio stream of the specified user.
 * - `NO`: (Default) Subscribe to the audio stream of the specified user.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteRemoteAudioStream:(NSUInteger)uid mute:(BOOL)mute NS_SWIFT_NAME(muteRemoteAudioStream(_:mute:));

/**
 * @brief Stops or resumes subscribing to the audio streams of all remote users.
 *
 * @details
 * After successfully calling this method, the local user stops or resumes subscribing to the audio
 * streams of all remote users, including all subsequent users.
 * Call timing: Call this method after joining a channel.
 *
 * @note
 * If you call this method and then call `enableAudio` or `disableAudio`, the latest call will
 * prevail.
 * By default, the SDK subscribes to the audio streams of all remote users when joining a channel.
 * To modify this behavior, you can set `autoSubscribeAudio` to `NO` when calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel, which will
 * cancel the subscription to the audio streams of all users upon joining the channel.
 *
 * @param mute Whether to stop subscribing to the audio streams of all remote users:
 * - `YES`: Stops subscribing to the audio streams of all remote users.
 * - `NO`: (Default) Subscribes to the audio streams of all remote users by default.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteAllRemoteAudioStreams:(BOOL)mute NS_SWIFT_NAME(muteAllRemoteAudioStreams(_:));

/**
 * @brief Starts the audio capturing device test.
 *
 * @details
 * This method tests whether the audio capturing device works properly. After calling this method,
 * the SDK triggers the `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback at
 * the time interval set in this method, which reports `uid` = 0 and the volume information of the
 * capturing device.
 * The difference between this method and the `startEchoTestWithConfig:` method is that the former
 * checks if the local audio capturing device is working properly, while the latter can check the
 * audio and video devices and network conditions.
 *
 * @note Call this method before joining a channel. After the test is completed, call
 * `stopRecordingDeviceTest` to stop the test before joining a channel.
 *
 * @param indicationInterval The interval (ms) for triggering the
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback. This value should be set
 * to greater than 10, otherwise, you will not receive the
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback and the SDK returns the
 * error code `-2`. Agora recommends that you set this value to 100.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: Invalid parameters. Check your parameter settings.
 */
- (int)startRecordingDeviceTest:(int)indicationInterval NS_SWIFT_NAME(startRecordingDeviceTest(_:));

/**
 * @brief Stops the audio capturing device test.
 *
 * @details
 * This method stops the audio capturing device test. You must call this method to stop the test
 * after calling the `startRecordingDeviceTest:` method.
 *
 * @note Call this method before joining a channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopRecordingDeviceTest NS_SWIFT_NAME(stopRecordingDeviceTest());

/**
 * @brief Starts the audio playback device test.
 *
 * @details
 * This method tests whether the audio device for local playback works properly. Once a user starts
 * the test, the SDK plays an audio file specified by the user. If the user can hear the audio, the
 * playback device works properly.
 * After calling this method, the SDK triggers the
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback every 100 ms, reporting
 * `uid` = 1 and the volume information of the playback device.
 * The difference between this method and the `startEchoTestWithConfig:` method is that the former
 * checks if the local audio playback device is working properly, while the latter can check the
 * audio and video devices and network conditions.
 *
 * @note Call this method before joining a channel. After the test is completed, call
 * `stopPlaybackDeviceTest` to stop the test before joining a channel.
 *
 * @param audioFileName The path of the audio file. The data format is string in UTF-8.
 * - Supported file formats: wav, mp3, m4a, and aac.
 * - Supported file sample rates: 8000, 16000, 32000, 44100, and 48000 Hz.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startPlaybackDeviceTest:(NSString * _Nonnull)audioFileName NS_SWIFT_NAME(startPlaybackDeviceTest(_:));

/**
 * @brief Stops the audio playback device test.
 *
 * @details
 * This method stops the audio playback device test. You must call this method to stop the test
 * after calling the `startPlaybackDeviceTest:` method.
 *
 * @note Call this method before joining a channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopPlaybackDeviceTest NS_SWIFT_NAME(stopPlaybackDeviceTest());

/**
 * @brief Registers an extension.
 *
 * @details
 * For extensions external to the SDK (such as those from Extensions Marketplace and SDK
 * Extensions), you need to load them before calling this method. Extensions internal to the SDK
 * (those included in the full SDK package) are automatically loaded and registered after the
 * initialization of `AgoraRtcEngineKit`.
 * Call timing: - Agora recommends you call this method after the initialization of
 * `AgoraRtcEngineKit` and before joining a channel.
 * - For video extensions (such as the image enhancement extension), you need to call this method
 * after enabling the video module by calling `enableVideo` or `enableLocalVideo:`.
 * - Before calling this method, you need to link the extension dynamic library (.xcframework) to
 * your project in Xcode to load the extension.
 *
 * @note
 * - If you want to register multiple extensions, you need to call this method multiple times.
 * - The data processing order of different extensions in the SDK is determined by the order in
 * which the extensions are registered. That is, the extension that is registered first will process
 * the data first.
 *
 * @param provider The name of the extension provider.
 * @param extension The name of the extension.
 * @param sourceType Source type of the extension. See `AgoraMediaSourceType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -3: The extension library is not loaded. Agora recommends that you check the storage location
 * or the name of the dynamic library.
 */
- (int)registerExtensionWithVendor:(NSString * _Nonnull)provider
                         extension:(NSString * _Nonnull)extension
                        sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(registerExtension(withVendor:extension:sourceType:));

/**
 * Enable/Disable an extension.
 * By calling this function, you can dynamically enable/disable the extension without changing the pipeline.
 * For example, enabling/disabling Extension_A means the data will be adapted/bypassed by Extension_A.
 *
 * NOTE: For compatibility reasons, if you haven't call registerExtensionWithVendor,
 * enableExtension will automatically register the specified extension.
 * We suggest you call registerExtensionWithVendor explicitly.
 *
 * @param provider The name of the extension provider, e.g. agora.io.
 * @param extension The name of the extension, e.g. agora.beauty.
 * @param enabled Whether to enable the extension:
 * - true: (Default) Enable the extension.
 * - false: Disable the extension.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableExtensionWithVendor:(NSString * _Nonnull)provider extension:(NSString * _Nonnull)extension enabled:(BOOL)enabled NS_SWIFT_NAME(enableExtension(withVendor:extension:enabled:));

/**
 * @brief Enables or disables extensions.
 *
 * @details
 * Call timing: Agora recommends that you call this method after joining a channel.
 * Related callbacks: When this method is successfully called within the channel, it triggers
 * `onExtensionStartedWithContext:` or `onExtensionStoppedWithContext:`.
 *
 * @note
 * - If you want to enable multiple extensions, you need to call this method multiple times.
 * - After a successful call of this method, you cannot load other extensions.
 *
 * @param provider The name of the extension provider.
 * @param extension The name of the extension.
 * @param enabled Whether to enable the extension:
 * - `YES`: Enable the extension.
 * - `NO`: Disable the extension.
 * @param type Source type of the extension. See `AgoraMediaSourceType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -3: The extension library is not loaded. Agora recommends that you check the storage location
 * or the name of the dynamic library.
 */
- (int)enableExtensionWithVendor:(NSString * _Nonnull)provider
                       extension:(NSString * _Nonnull)extension
                         enabled:(BOOL)enabled
                      sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(enableExtension(withVendor:extension:enabled:sourceType:));

/**
 * Enable/Disable an extension.
 * By calling this function, you can dynamically enable/disable the extension without changing the pipeline.
 * For example, enabling/disabling Extension_A means the data will be adapted/bypassed by Extension_A.
 *
 * NOTE: For compatibility reasons, if you haven't call registerExtensionWithVendor,
 * enableExtension will automatically register the specified extension.
 * We suggest you call registerExtensionWithVendor explicitly.
 *
 * @param provider The name of the extension provider, e.g. agora.io.
 * @param extension The name of the extension, e.g. agora.beauty.
 * @param extensionInfo The information for extension definition see AgoraExtensionInfo
 * @param enabled Whether to enable the extension:
 * - true: (Default) Enable the extension.
 * - false: Disable the extension.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableExtensionWithVendor:(NSString * _Nonnull)provider
                       extension:(NSString * _Nonnull)extension
                   extensionInfo:(AgoraExtensionInfo * _Nonnull)extensionInfo
                         enabled:(BOOL)enabled NS_SWIFT_NAME(enableExtension(withVendor:extension:extensionInfo:enabled:));

/**
 * @brief Sets the properties of the extension provider.
 *
 * @details
 * You can call this method to set the attributes of the extension provider and initialize the
 * relevant parameters according to the type of the provider.
 * Call timing: Call this method before `enableExtensionWithVendor:extension:enabled:sourceType:`
 * and after `registerExtensionWithVendor:extension:sourceType:`.
 *
 * @note If you want to set the properties of the extension provider for multiple extensions, you
 * need to call this method multiple times.
 *
 * @param provider The name of the extension provider.
 * @param key The key of the extension.
 * @param value The value of the extension key.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setExtensionProviderPropertyWithVendor:(NSString * _Nonnull)provider
                                          key:(NSString * _Nonnull)key
                                        value:(NSString * _Nonnull)value NS_SWIFT_NAME(setExtensionProviderPropertyWithVendor(_:key:value:));

/**
 * @brief Sets the properties of the extension.
 *
 * @details
 * After enabling the extension, you can call this method to set the properties of the extension.
 * Call timing: Call this mehtod after calling
 * `enableExtensionWithVendor:extension:enabled:sourceType:`.
 * Related callbacks: After calling this method, it may trigger the `onEventWithContext:key:value:`
 * callback, and the specific triggering logic is related to the extension itself.
 *
 * @note If you want to set properties for multiple extensions, you need to call this method
 * multiple times.
 *
 * @param provider The name of the extension provider.
 * @param extension The name of the extension.
 * @param key The key of the extension.
 * @param value The value of the extension key.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setExtensionPropertyWithVendor:(NSString * _Nonnull)provider
                            extension:(NSString * _Nonnull)extension
                                  key:(NSString * _Nonnull)key
                                value:(NSString * _Nonnull)value NS_SWIFT_NAME(setExtensionPropertyWithVendor(_:extension:key:value:));

/**
 * Sets the properties of an extension.
 *
 * @param provider The name of the extension provider, e.g. agora.io.
 * @param extension The name of the extension, e.g. agora.beauty.
 * @param key The key of the extension.
 * @param value The JSON formatted value of the extension key.
 * @param sourceType The source type of the extension, e.g. AgoraMediaSourceTypePrimaryCamera. See AgoraMediaSourceType.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setExtensionPropertyWithVendor:(NSString * _Nonnull)provider
                            extension:(NSString * _Nonnull)extension
                                  key:(NSString * _Nonnull)key
                                value:(NSString * _Nonnull)value
                           sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(setExtensionPropertyWithVendor(_:extension:key:value:sourceType:));

/**
 * Sets the properties of an extension.
 *
 * @param provider The name of the extension provider, e.g. agora.io.
 * @param extension The name of the extension, e.g. agora.beauty.
 * @param extensionInfo The information for extension. See AgoraExtensionInfo.
 * @param key The key of the extension.
 * @param value The JSON formatted value of the extension key.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setExtensionPropertyWithVendor:(NSString * _Nonnull)provider
                            extension:(NSString * _Nonnull)extension
                        extensionInfo:(AgoraExtensionInfo * _Nonnull)extensionInfo
                                  key:(NSString * _Nonnull)key
                                value:(NSString * _Nonnull)value NS_SWIFT_NAME(setExtensionPropertyWithVendor(_:extension:extensionInfo:key:value:));

/**
 * @brief Gets detailed information on the extensions.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param provider The name of the extension provider.
 * @param extension The name of the extension.
 * @param key The key of the extension.
 *
 * @return
 * - The extension information, if the method call succeeds.
 * - An empty string, if the method call fails.
 */
- (NSString * _Nullable)getExtensionPropertyWithVendor:(NSString * _Nonnull)provider
                                             extension:(NSString * _Nonnull)extension
                                                   key:(NSString * _Nonnull)key NS_SWIFT_NAME(getExtensionProperty(withVendor:extension:key:));

/**
 * @brief Gets detailed information on the extensions.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param provider The name of the extension provider.
 * @param extension The name of the extension.
 * @param key The key of the extension.
 * @param sourceType Source type of the extension. See `AgoraMediaSourceType`.
 *
 * @return
 * - The extension information, if the method call succeeds.
 * - An empty string, if the method call fails.
 */
- (NSString * _Nullable)getExtensionPropertyWithVendor:(NSString * _Nonnull)provider
                                             extension:(NSString * _Nonnull)extension
                                                   key:(NSString * _Nonnull)key
                                            sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(getExtensionProperty(withVendor:extension:key:sourceType:));

/**
 * Gets the properties of an extension.
 *
 * @param provider The name of the extension provider, e.g. agora.io.
 * @param extension The name of the extension, e.g. agora.beauty.
 * @param extensionInfo The information for extension. See AgoraExtensionInfo.
 * @param key The key of the extension.
 *
 * @return
 * - JSON formatted string of property's value; return null if failed
 */
- (NSString * _Nullable)getExtensionPropertyWithVendor:(NSString * _Nonnull)provider
                                             extension:(NSString * _Nonnull)extension
                                         extensionInfo:(AgoraExtensionInfo * _Nonnull)extensionInfo
                                                   key:(NSString * _Nonnull)key NS_SWIFT_NAME(getExtensionProperty(withVendor:extension:extensionInfo:key:));

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
/**
 * @brief Enables loopback audio capturing.
 *
 * @details
 * If you enable loopback audio capturing, the output of the sound card is mixed into the audio
 * stream sent to the other end.
 *
 * @note
 * - The macOS system's default sound card does not support recording functionality. As of v4.5.0,
 * when you call this method for the first time, the SDK will automatically install the built-in
 * AgoraALD virtual sound card developed by Agora. After successful installation, the audio routing
 * will automatically switch to the virtual sound card and use it for audio capturing.
 * - You can call this method either before or after joining a channel.
 * - If you call the `disableAudio` method to disable the audio module, audio capturing will be
 * disabled as well. If you need to enable audio capturing, call the `enableAudio` method to enable
 * the audio module and then call the `enableLoopbackRecording:deviceName:` method.
 *
 * @param enabled Sets whether to enable loopback audio capturing.
 * - `YES`: Enable sound card capturing. You can find the name of the virtual sound card in your
 * system's**Audio Devices > Output**.
 * - `NO`: Disable sound card capturing. The name of the virtual sound card will not be shown in
 * your system's **Audio Devices > Output**.
 * @param deviceName The device name of the virtual sound card. The default value is set to NULL,
 * which means using AgoraALD for loopback audio capturing.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableLoopbackRecording:(BOOL)enabled deviceName:(NSString* _Nullable)deviceName NS_SWIFT_NAME(enableLoopbackRecording(_:deviceName:));

/**
 * @brief Adjusts the volume of the signal captured by the sound card.
 *
 * @details
 * After calling `enableLoopbackRecording:deviceName:` to enable loopback audio capturing, you can
 * call this method to adjust the volume of the signal captured by the sound card.
 *
 * @param volume Audio mixing volume. The value ranges between 0 and 100. The default value is 100,
 * which means the original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustLoopbackSignalVolume:(NSInteger)volume NS_SWIFT_NAME(adjustLoopbackSignalVolume(_:));

/** Retrieves the audio volume for recording loopback.
 * @note Call this method when you are in a channel.
 * @return
 * - >= 0: The audio volume for loopback, if this method call succeeds. The value range is [0, 100].
 * - < 0: Failure.
*/
- (int)getLoopbackRecordingVolume NS_SWIFT_NAME(getLoopbackRecordingVolume());
#endif

#pragma mark Core Video

/**
 * @brief Enables the video module.
 *
 * @details
 * The video module is disabled by default, call this method to enable it. If you need to disable
 * the video module later, you need to call `disableVideo`.
 * Call timing: This method can be called either before joining a channel or while in the channel:
 * - If called before joining a channel, it enables the video module.
 * - If called during an audio-only call, the audio call automatically switches to a video call.
 * Related callbacks: A successful call of this method triggers the
 * `rtcEngine:remoteVideoStateChangedOfUid:state:reason:elapsed:` callback on the remote client.
 *
 * @note
 * - This method enables the internal engine and is valid after leaving the channel.
 * - Calling this method will reset the entire engine, resulting in a slow response time. You can
 * use the following methods to independently control a specific function of the video module based
 * on your actual needs:
 *   - `enableLocalVideo:`: Whether to enable the camera to create the local video stream.
 *   - `muteLocalVideoStream:`: Whether to publish the local video stream.
 *   - `muteRemoteVideoStream:mute:`: Whether to subscribe to and play the remote video stream.
 *   - `muteAllRemoteVideoStreams:`: Whether to subscribe to and play all remote video streams.
 * - A successful call of this method resets `enableLocalVideo:`, `muteRemoteVideoStream:mute:`, and
 * `muteAllRemoteVideoStreams:`. Proceed it with caution.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableVideo NS_SWIFT_NAME(enableVideo());

/**
 * @brief Disables the video module.
 *
 * @details
 * This method is used to disable the video module.
 * Call timing: This method can be called either before or after joining the channel.
 * - If it is called before joining the channel, the audio-only mode is enabled.
 * - If it is called after joining the channel, it switches from video mode to audio-only mode.
 * Then, calling `enableVideo` can swithch to video mode again.
 * Related callbacks: A successful call of this method triggers the
 * `rtcEngine:didVideoEnabled:byUid:` (`NO`) callback on the remote client.
 *
 * @note
 * - This method affects the internal engine and can be called after leaving the channel.
 * - Calling this method will reset the entire engine, resulting in a slow response time. You can
 * use the following methods to independently control a specific function of the video module based
 * on your actual needs:
 *   - `enableLocalVideo:`: Whether to enable the camera to create the local video stream.
 *   - `muteLocalVideoStream:`: Whether to publish the local video stream.
 *   - `muteRemoteVideoStream:mute:`: Whether to subscribe to and play the remote video stream.
 *   - `muteAllRemoteVideoStreams:`: Whether to subscribe to and play all remote video streams.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)disableVideo NS_SWIFT_NAME(disableVideo());

/**
 * @brief Enables/Disables the local video capture.
 *
 * @details
 * This method disables or re-enables the local video capture, and does not affect receiving the
 * remote video stream.
 * After calling `enableVideo`, the local video capture is enabled by default.
 * If you call `enableLocalVideo:` (`NO`) to disable local video capture within the channel, it also
 * simultaneously stops publishing the video stream within the channel. If you want to restart video
 * catpure, you can call `enableLocalVideo:` (`YES`) and then call `updateChannelWithMediaOptions:`
 * to set the `options` parameter to publish the locally captured video stream in the channel.
 * After the local video capturer is successfully disabled or re-enabled, the SDK triggers the
 * `rtcEngine:remoteVideoStateChangedOfUid:state:reason:elapsed:` callback on the remote client.
 *
 * @note
 * - You can call this method either before or after joining a channel. However, if you call it
 * before joining, the settings will only take effect once you have joined the channel.
 * - This method enables the internal engine and is valid after leaving the channel.
 *
 * @param enabled Whether to enable the local video capture.
 * - `YES`: (Default) Enable the local video capture.
 * - `NO`: Disable the local video capture. Once the local video is disabled, the remote users
 * cannot receive the video stream of the local user, while the local user can still receive the
 * video streams of remote users. When set to `NO`, this method does not require a local camera.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableLocalVideo:(BOOL)enabled NS_SWIFT_NAME(enableLocalVideo(_:));

/**
 * @brief Queries the video codec capabilities of the SDK.
 *
 * @return
 * - One `AgoraVideoCodecCapInfo` array indicating the video encoding capability of the device, if
 * the method call succeeds.
 * - If the call timeouts, please modify the call logic and do not invoke the method in the main
 * thread.
 */
- (NSArray <AgoraVideoCodecCapInfo *> *_Nullable)queryCodecCapability;

/**
 * @brief Queries device score.
 *
 * @details
 * Applicable scenarios: In high-definition or ultra-high-definition video scenarios, you can first
 * call this method to query the device's score. If the returned score is low (for example, below
 * 60), you need to lower the video resolution to avoid affecting the video experience. The minimum
 * device score required for different business scenarios is varied. For specific score
 * recommendations, please `technical support`.
 *
 * @return
 * - > 0: The method call succeeeds, the value is the current device's score, the range is [0,100],
 * the larger the value, the stronger the device capability. Most devices are rated between 60 and
 * 100.
 * - < 0: Failure.
 */
- (int)queryDeviceScore NS_SWIFT_NAME(queryDeviceScore());

/**
 * Queries the HDR capability of the video module.
 *
 * @since v4.6.0
 *
 * @technical preview
 *
 * @return
 * - 0: Unsupported.
 * - 1: Supported.
 * - < 0: Unknown.
 */
- (AgoraHDRCapability)queryHDRCapability:(AgoraVideoModule)videoModule NS_SWIFT_NAME(queryHDRCapability(_:));

/**
 * @brief Sets the video encoder configuration.
 *
 * @details
 * Sets the encoder configuration for the local video. Each configuration profile corresponds to a
 * set of video parameters, including the resolution, frame rate, and bitrate.
 * Call timing: You can call this method either before or after joining a channel. If the user does
 * not need to reset the video encoding properties after joining the channel, Agora recommends
 * calling this method before `enableVideo` to reduce the time to render the first video frame.
 *
 * @note
 * - Both this method and the `getMirrorApplied` method support setting the mirroring effect. Agora
 * recommends that you only choose one method to set it up. Using both methods at the same time
 * causes the mirroring effect to overlap, and the mirroring settings fail.
 * - The `config` specified in this method is the maximum value under ideal network conditions. If
 * the video engine cannot render the video using the specified `config` due to unreliable network
 * conditions, the parameters further down the list are considered until a successful configuration
 * is found.
 *
 * @param config Video profile. See `AgoraVideoEncoderConfiguration`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVideoEncoderConfiguration:(AgoraVideoEncoderConfiguration * _Nonnull)config NS_SWIFT_NAME(setVideoEncoderConfiguration(_:));

/**
 * @brief Initializes the local video view.
 *
 * @details
 * This method initializes the video view of a local stream on the local device. It only affects the
 * video seen by the local user and does not impact the publishing of the local video. Call this
 * method to bind the local video stream to a video view ( `view` ) and to set the rendering and
 * mirror modes of the video view.
 * The binding remains valid after leaving the channel. To stop rendering or unbind the local video
 * from the view, set `view` as nil.
 * Applicable scenarios: After initialization, call this method to set the local video and then join
 * the channel.
 * In real-time interactive scenarios, if you need to simultaneously view multiple preview frames in
 * the local video preview, and each frame is at a different observation position along the video
 * link, you can repeatedly call this method to set different `view` s and set different observation
 * positions for each `view. ` For example, by setting the video source to the camera and then
 * configuring two `view` s with `position` setting to AgoraVideoModulePositionPostCaptureOrigin and
 * AgoraVideoModulePositionPostCapture, you can simultaneously preview the raw, unprocessed video
 * frame and the video frame that has undergone preprocessing (image enhancement effects, virtual
 * background, watermark) in the local video preview.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @note - To update only the rendering or mirror mode of the local video view during a call, call
 * `setLocalRenderMode:mirror:` instead.
 *
 * @param local The local video view and settings. See `AgoraRtcVideoCanvas`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setupLocalVideo:(AgoraRtcVideoCanvas * _Nullable)local NS_SWIFT_NAME(setupLocalVideo(_:));

/**
 * @brief Sets the maximum frame rate for rendering local video.
 *
 * @details
 * Applicable scenarios: In scenarios where the requirements for video rendering frame rate are not
 * high (such as screen sharing or online education), you can call this method to set the maximum
 * frame rate for local video rendering. The SDK will attempt to keep the actual frame rate of local
 * rendering close to this value, which helps to reduce CPU consumption and improving system
 * performance.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 * @param targetFps The capture frame rate (fps) of the local video. Sopported values are: 1, 7, 10,
 * 15, 24, 30, 60.CAUTION: Set this parameter to a value lower than the actual video frame rate;
 * otherwise, the settings do not take effect.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLocalRenderTargetFps:(AgoraVideoSourceType)sourceType
                               targetFps:(int)targetFps;
/**
 * @brief Sets the maximum frame rate for rendering remote video.
 *
 * @details
 * Applicable scenarios: In scenarios where the video rendering frame rate is not critical (e.g.,
 * screen sharing, online education) or when the remote users are using mid-to-low-end devices, you
 * can call this method to set the maximum frame rate for video rendering on the remote client. The
 * SDK will attempt to render the actual frame rate as close as possible to this value, which helps
 * to reduce CPU consumption and improve system performance.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @param targetFps The capture frame rate (fps) of the local video. Sopported values are: 1, 7, 10,
 * 15, 24, 30, 60.CAUTION: Set this parameter to a value lower than the actual video frame rate;
 * otherwise, the settings do not take effect.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteRenderTargetFps:(int)targetFps;

/**
 * @brief Updates the display mode of the local video view.
 *
 * @details
 * After initializing the local video view, you can call this method to update its rendering and
 * mirror modes. It affects only the video view that the local user sees and does not impact the
 * publishing of the local video.
 * Call timing: - Ensure that you have called the `setupLocalVideo:` method to initialize the local
 * video view before calling this method.
 * - During a call, you can call this method as many times as necessary to update the display mode
 * of the local video view.
 *
 * @note This method only takes effect on the primary camera `(AgoraMediaSourceTypePrimaryCamera)`.
 * In scenarios involving custom video capture or the use of alternative video sources, you need to
 * use `setupLocalVideo:` instead of this method.
 *
 * @param mode The local video display mode. See `AgoraVideoRenderMode`.
 * @param mirror The mirror mode of the local video view. See `AgoraVideoMirrorMode`.
 * Attention: If you use a front camera, the SDK enables the mirror mode by default; if you use a
 * rear camera, the SDK disables the mirror mode by default.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLocalRenderMode:(AgoraVideoRenderMode)mode
                   mirror:(AgoraVideoMirrorMode)mirror NS_SWIFT_NAME(setLocalRenderMode(_:mirror:));

/**
 * @brief Enables the local video preview.
 *
 * @details
 * You can call this method to enable local video preview.
 * Call timing: This method must be called after `enableVideo` and `setupLocalVideo:`.
 *
 * @note
 * - The local preview enables the mirror mode by default.
 * - After leaving the channel, local preview remains enabled. You need to call `stopPreview` to
 * disable local preview.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startPreview NS_SWIFT_NAME(startPreview());

/**
 * @brief Enables the local video preview and specifies the video source for the preview.
 *
 * @details
 * This method is used to start local video preview and specify the video source that appears in the
 * preview screen.
 * Call timing: This method must be called after `enableVideo` and `setupLocalVideo:`.
 *
 * @note
 * - The local preview enables the mirror mode by default.
 * - After leaving the channel, local preview remains enabled. You need to call `stopPreview` to
 * disable local preview.
 *
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startPreview: (AgoraVideoSourceType)sourceType NS_SWIFT_NAME(startPreview(_:));

/**
 * Stops the local video preview and the video.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopPreview NS_SWIFT_NAME(stopPreview());

/**
 * @brief Stops the local video preview.
 *
 * @details
 * Applicable scenarios: After calling `startPreview:` to start the preview, if you want to stop the
 * local video preview, call this method.
 * Call timing: Call this method before joining a channel or after leaving a channel.
 *
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopPreview: (AgoraVideoSourceType)sourceType NS_SWIFT_NAME(stopPreview(_:));

/**
 * @brief Initializes the video view of a remote user.
 *
 * @details
 * This method initializes the video view of a remote stream on the local device. It affects only
 * the video view that the local user sees. Call this method to bind the remote video stream to a
 * video view and to set the rendering and mirror modes of the video view.
 * You need to specify the ID of the remote user in this method. If the remote user ID is unknown to
 * the application, set it after the app receives the `rtcEngine:didJoinedOfUid:elapsed:` callback.
 * To unbind the remote user from the view, set the `view` parameter to NULL.
 * Once the remote user leaves the channel, the SDK unbinds the remote user.
 * In the scenarios of custom layout for mixed videos on the mobile end, you can call this method
 * and set a separate `view` for rendering each sub-video stream of the mixed video stream.
 *
 * @note
 * - To update the rendering or mirror mode of the remote video view during a call, use the
 * `setRemoteRenderMode:mode:mirror:` method.
 * - When using the recording service, the app does not need to bind a view, as it does not send a
 * video stream. If your app does not recognize the recording service, bind the remote user to the
 * view when the SDK triggers the `rtcEngine:firstRemoteVideoDecodedOfUid:size:elapsed:` callback.
 *
 * @param remote The remote video view and settings. See `AgoraRtcVideoCanvas`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setupRemoteVideo:(AgoraRtcVideoCanvas * _Nonnull)remote NS_SWIFT_NAME(setupRemoteVideo(_:));

/**
 * @brief Updates the display mode of the video view of a remote user.
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
 * @param mode The rendering mode of the remote user view. For details, see `AgoraVideoRenderMode`.
 * @param mirror The mirror mode of the remote user view. See `AgoraVideoMirrorMode`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteRenderMode:(NSUInteger)uid
                      mode:(AgoraVideoRenderMode)mode
                    mirror:(AgoraVideoMirrorMode)mirror NS_SWIFT_NAME(setRemoteRenderMode(_:mode:mirror:));

/**
 * @brief Stops or resumes publishing the local video stream.
 *
 * @details
 * This method is used to control whether to publish the locally captured video stream. If you call
 * this method to stop publishing locally captured video streams, the video capturing device will
 * still work and won't be affected.
 * Compared to `enableLocalVideo:` (`NO`), which can also cancel the publishing of local video
 * stream by turning off the local video stream capture, this method responds faster.
 * Call timing: This method can be called either before or after joining the channel.
 * Related callbacks: After successfully calling this method, the local end triggers callback
 * `rtcEngine:didVideoPublishStateChange:sourceType:oldState:newState:elapseSinceLastState:`; the
 * remote end triggers `rtcEngine:didVideoMuted:byUid:` and
 * `rtcEngine:remoteVideoStateChangedOfUid:state:reason:elapsed:` callbacks.
 *
 * @param mute Whether to stop publishing the local video stream.
 * - `YES`: Stop publishing the local video stream.
 * - `NO`: (Default) Publish the local video stream.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteLocalVideoStream:(BOOL)mute NS_SWIFT_NAME(muteLocalVideoStream(_:));

/**
 * @brief Stops or resumes subscribing to the video stream of a specified user.
 *
 * @details
 * Call timing: Call this method after joining a channel.
 * Related callbacks: After a successful method call, the SDK triggers the
 * `rtcEngine:didVideoSubscribeStateChange:uid:oldState:newState:elapseSinceLastState:` callback.
 *
 * @param uid The user ID of the specified user.
 * @param mute Whether to subscribe to the specified remote user's video stream.
 * - `YES`: Stop subscribing to the video streams of the specified user.
 * - `NO`: (Default) Subscribe to the video stream of the specified user.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteRemoteVideoStream:(NSUInteger)uid
                        mute:(BOOL)mute NS_SWIFT_NAME(muteRemoteVideoStream(_:mute:));

/**
 * @brief Stops or resumes subscribing to the video streams of all remote users.
 *
 * @details
 * After successfully calling this method, the local user stops or resumes subscribing to the video
 * streams of all remote users, including all subsequent users.
 * Call timing: Call this method after joining a channel.
 *
 * @note
 * If you call this method and then call `enableVideo` or `disableVideo`, the latest call will
 * prevail.
 * By default, the SDK subscribes to the video streams of all remote users when joining a channel.
 * To modify this behavior, you can set `autoSubscribeVideo` to`NO` when calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel, which will
 * cancel the subscription to the video streams of all users upon joining the channel.
 *
 * @param mute Whether to stop subscribing to the video streams of all remote users.
 * - `YES`: Stop subscribing to the video streams of all remote users.
 * - `NO`: (Default) Subscribe to the video streams of all remote users by default.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteAllRemoteVideoStreams:(BOOL)mute NS_SWIFT_NAME(muteAllRemoteVideoStreams(_:));

/**
 * @brief Sets the image enhancement options.
 *
 * @details
 * Enables or disables image enhancement, and sets the options.
 * Call timing: Call this method after calling `enableVideo` or `startPreview:`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device.
 *
 * @param enable Whether to enable the image enhancement function:
 * - `YES`: Enable the image enhancement function.
 * - `NO`: (Default) Disable the image enhancement function.
 * @param options The image enhancement options. See `AgoraBeautyOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -4: The current device does not support this feature. Possible reasons include:
 *     - The current device capabilities do not meet the requirements for image enhancement. Agora
 * recommends you replace it with a high-performance device.
 */
- (int)setBeautyEffectOptions:(BOOL)enable options:(AgoraBeautyOptions* _Nullable)options NS_SWIFT_NAME(setBeautyEffectOptions(_:options:));

/**
 * @brief Sets the image enhancement options and specifies the media source.
 *
 * @details
 * Enables or disables image enhancement, and sets the options.
 * Both this method and `setBeautyEffectOptions:options:` set image enhancement options, but this
 * method allows you to specify the media source to which the image enhancement is applied.
 * Call timing: Call this method after calling `enableVideo` or `startPreview:`.
 *
 * @param enable Whether to enable the image enhancement function:
 * - `YES`: Enable the image enhancement function.
 * - `NO`: (Default) Disable the image enhancement function.
 * @param options The image enhancement options. See `AgoraBeautyOptions`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - The default value is `AgoraMediaSourceTypePrimaryCamera`.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -4: The current device does not support this feature. Possible reasons include:
 *     - The current device capabilities do not meet the requirements for image enhancement. Agora
 * recommends you replace it with a high-performance device.
 */
- (int)setBeautyEffectOptions:(BOOL)enable options:(AgoraBeautyOptions* _Nullable)options sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(setBeautyEffectOptions(_:options:sourceType:));

/**
 * @brief Sets the face shape beauty options.
 *
 * @details
 * Calling this method allows for adjusting specific parts of the face, achieving effects such as
 * slimming the face, enlarging the eyes, and slimming the nose through minor cosmetic procedures.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device.
 *
 * @param enable Whether to enable the face shape effect:
 * - `YES`: Enable the face shape effect.
 * - `NO`: (Default) Disable the face shape effect.
 * @param options Face shaping style options, see `AgoraFaceShapeBeautyOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -4: The current device does not support this feature. Possible reasons include:
 *     - The current device capabilities do not meet the requirements for image enhancement. Agora
 * recommends you replace it with a high-performance device.
 */
- (int)setFaceShapeBeautyOptions:(BOOL)enable options:(AgoraFaceShapeBeautyOptions* _Nullable)options NS_SWIFT_NAME(setFaceShapeBeautyOptions(_:options:));

/**
 * @brief Sets the face shape options and specifies the media source.
 *
 * @details
 * Calling this method allows for modifying various parts of the face, achieving slimming, enlarging
 * eyes, slimming nose, and other minor cosmetic effects all at once using preset parameters,
 * supporting fine-tuning the overall modification intensity.
 * Both this methods and `setFaceShapeBeautyOptions:options:sourceType:` can be used to set beauty
 * effects options, the difference is that this method supports specifying the media source to apply
 * the beauty effects.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device.
 *
 * @param enable Whether to enable the face shape effect:
 * - `YES`: Enable the face shape effect.
 * - `NO`: (Default) Disable the face shape effect.
 * @param options Face shaping style options, see `AgoraFaceShapeBeautyOptions`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - Use the default value `AgoraMediaSourceTypePrimaryCamera` if you use camera to capture local
 * video.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -4: The current device does not support this feature. Possible reasons include:
 *     - The current device capabilities do not meet the requirements for image enhancement. Agora
 * recommends you replace it with a high-performance device.
 */
- (int)setFaceShapeBeautyOptions:(BOOL)enable options:(AgoraFaceShapeBeautyOptions* _Nullable)options sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(setFaceShapeBeautyOptions(_:options:sourceType:));

/**
 * @brief Gets the beauty effect options.
 *
 * @details
 * Calling this method can retrieve the current settings of the beauty effect.
 * Applicable scenarios: When the user opens the beauty style and style intensity menu in the app,
 * you can call this method to get the current beauty effect options, then refresh the menu in the
 * user interface according to the results, and update the UI.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @return
 * - A pointer to the `AgoraFaceShapeBeautyOptions` instance, if the method call succeeds.
 * - nil is returned, if the method call fails.
 */
- (AgoraFaceShapeBeautyOptions * _Nullable)getFaceShapeBeautyOptions NS_SWIFT_NAME(getFaceShapeBeautyOptions());

/**
 * @brief Gets the beauty effect options.
 *
 * @details
 * Calling this method can retrieve the current settings of the beauty effect.
 * Applicable scenarios: When the user opens the beauty style and style intensity menu in the app,
 * you can call this method to get the current beauty effect options, then refresh the menu in the
 * user interface according to the results, and update the UI.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - Use the default value `AgoraMediaSourceTypePrimaryCamera` if you use camera to capture local
 * video.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - A pointer to the `AgoraFaceShapeBeautyOptions` instance, if the method call succeeds.
 * - nil is returned, if the method call fails.
 */
- (AgoraFaceShapeBeautyOptions * _Nullable)getFaceShapeBeautyOptions:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(getFaceShapeBeautyOptions(_:));

/**
 * @brief Sets the options for beauty enhancement facial areas.
 *
 * @details
 * If the preset beauty effects implemented in the `setFaceShapeBeautyOptions:options:` method do
 * not meet expectations, you can use this method to set beauty area options, individually fine-tune
 * each part of the face, and achieve a more refined beauty effect.
 * Call timing: Call this method after calling `setFaceShapeBeautyOptions:options:` or
 * `setFaceShapeBeautyOptions:options:`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device.
 *
 * @param options Facial enhancement areas, see `AgoraFaceShapeAreaOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -4: The current device does not support this feature. Possible reasons include:
 *     - The current device capabilities do not meet the requirements for image enhancement. Agora
 * recommends you replace it with a high-performance device.
 */
- (int)setFaceShapeAreaOptions:(AgoraFaceShapeAreaOptions* _Nullable)options NS_SWIFT_NAME(setFaceShapeAreaOptions(_:));

/**
 * @brief Sets the options for beauty enhancement facial areas.
 *
 * @details
 * If the preset beauty effects implemented in the `setFaceShapeBeautyOptions:options:` method do
 * not meet expectations, you can use this method to set beauty area options, individually fine-tune
 * each part of the face, and achieve a more refined beauty effect.
 * Call timing: Call this method after calling `setFaceShapeBeautyOptions:options:` or
 * `setFaceShapeBeautyOptions:options:`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device.
 *
 * @param options Facial enhancement areas, see `AgoraFaceShapeAreaOptions`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - Use the default value `AgoraMediaSourceTypePrimaryCamera` if you use camera to capture local
 * video.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -4: The current device does not support this feature. Possible reasons include:
 *     - The current device capabilities do not meet the requirements for image enhancement. Agora
 * recommends you replace it with a high-performance device.
 */
- (int)setFaceShapeAreaOptions:(AgoraFaceShapeAreaOptions* _Nullable)options sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(setFaceShapeAreaOptions(_:sourceType:));

/**
 * @brief Gets the facial beauty area options.
 *
 * @details
 * Calling this method can retrieve the current settings of the beauty effect.
 * Applicable scenarios: When the user opens the facial beauty area and shaping intensity menu in
 * the app, you can call this method to get the current beauty effect options, then refresh the menu
 * in the user interface according to the results, and update the UI.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @param area Facial enhancement areas. See `AgoraFaceShapeArea`.
 *
 * @return
 * - A pointer to the `AgoraFaceShapeAreaOptions` instance, if the method call succeeds.
 * - nil is returned, if the method call fails.
 */
- (AgoraFaceShapeAreaOptions * _Nullable)getFaceShapeAreaOptions:(AgoraFaceShapeArea) area NS_SWIFT_NAME(getFaceShapeAreaOptions(_:));

/**
 * @brief Gets the facial beauty area options.
 *
 * @details
 * Calling this method can retrieve the current settings of the beauty effect.
 * Applicable scenarios: When the user opens the facial beauty area and shaping intensity menu in
 * the app, you can call this method to get the current beauty effect options, then refresh the menu
 * in the user interface according to the results, and update the UI.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @param area Facial enhancement areas. See `AgoraFaceShapeArea`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - Use the default value `AgoraMediaSourceTypePrimaryCamera` if you use camera to capture local
 * video.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - A pointer to the `AgoraFaceShapeAreaOptions` instance, if the method call succeeds.
 * - nil is returned, if the method call fails.
 */
- (AgoraFaceShapeAreaOptions * _Nullable)getFaceShapeAreaOptions:(AgoraFaceShapeArea) area sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(getFaceShapeAreaOptions(_:sourceType:));

/**
 * @brief Sets video noise reduction.
 *
 * @since v4.0.0
 *
 * @details
 * You can call this method to enable the video noise reduction feature and set the options of the
 * video noise reduction effect.
 * Applicable scenarios: dark environments and low-end video capture devices can cause video images
 * to contain significant noise, which affects video quality. In real-time interactive scenarios,
 * video noise also consumes bitstream resources and reduces encoding efficiency during encoding.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - Video noise reduction has certain requirements for equipment performance. If your device
 * overheats after you enable video noise reduction, Agora recommends modifying the video noise
 * reduction options to a less performance-consuming level or disabling video noise reduction
 * entirely.
 * If the noise reduction implemented by this method does not meet your needs, Agora recommends that
 * you call the `setBeautyEffectOptions:options:` method to enable the beauty and skin smoothing
 * function to achieve better video noise reduction effects. The recommended `AgoraBeautyOptions`
 * settings for intense noise reduction effect are as follows:
 * - `lighteningContrastLevel` AgoraLighteningContrastNormal
 * - `lighteningLevel`: 0.0
 * - `smoothnessLevel`: 0.5
 * - `rednessLevel`: 0.0
 * - `sharpnessLevel`: 0.1
 *
 * @param enable Whether to enable video noise reduction:
 * - `YES`: Enable video noise reduction.
 * - `NO`: (Default) Disable video noise reduction.
 * @param options The video noise reduction options. See `AgoraVideoDenoiserOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVideoDenoiserOptions:(BOOL)enable options:(AgoraVideoDenoiserOptions* _Nullable)options  NS_SWIFT_NAME(setVideoDenoiserOptions(_:options:));

/**
 * @brief Sets video noise reduction and specifies the media source.
 *
 * @since v4.0.0
 *
 * @details
 * You can call this method to enable the video noise reduction feature and set the options of the
 * video noise reduction effect.
 * Both this method and `setVideoDenoiserOptions:options:` set video noise reduction, but this
 * method allows you to specify the media source to which the noise reduction is applied.
 * Applicable scenarios: dark environments and low-end video capture devices can cause video images
 * to contain significant noise, which affects video quality. In real-time interactive scenarios,
 * video noise also consumes bitstream resources and reduces encoding efficiency during encoding.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - Video noise reduction has certain requirements for equipment performance. If your device
 * overheats after you enable video noise reduction, Agora recommends modifying the video noise
 * reduction options to a less performance-consuming level or disabling video noise reduction
 * entirely.
 * If the noise reduction implemented by this method does not meet your needs, Agora recommends that
 * you call the `setBeautyEffectOptions:options:` method to enable the beauty and skin smoothing
 * function to achieve better video noise reduction effects. The recommended `AgoraBeautyOptions`
 * settings for intense noise reduction effect are as follows:
 * - `lighteningContrastLevel` AgoraLighteningContrastNormal
 * - `lighteningLevel`: 0.0
 * - `smoothnessLevel`: 0.5
 * - `rednessLevel`: 0.0
 * - `sharpnessLevel`: 0.1
 *
 * @param enable Whether to enable video noise reduction:
 * - `YES`: Enable video noise reduction.
 * - `NO`: (Default) Disable video noise reduction.
 * @param options The video noise reduction options. See `AgoraVideoDenoiserOptions`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - The default value is `AgoraMediaSourceTypePrimaryCamera`.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVideoDenoiserOptions:(BOOL)enable options:(AgoraVideoDenoiserOptions* _Nullable)options sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(setVideoDenoiserOptions(_:options:sourceType:));


/**
 * @brief Sets the filter effect options.
 *
 * @since v4.4.1
 *
 * @details
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device.
 *
 * @param enable Whether to enable the filter effect:
 * - `YES`: Yes.
 * - `NO`: (Default) No.
 * @param options The filter effect options. See `AgoraFilterEffectOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setFilterEffectOptions:(BOOL)enable options:(AgoraFilterEffectOptions* _Nullable)options  NS_SWIFT_NAME(setFilterEffectOptions(_:options:));

/**
 * @brief Sets the filter effect options and specifies the media source.
 *
 * @since v4.4.1
 *
 * @details
 * Both this method and `setBeautyEffectOptions:options:sourceType:` set filter effect options. The
 * difference is that this method allows you to specify the media source to which the filter effect
 * option is applied.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device.
 *
 * @param enable Whether to enable the filter effect:
 * - `YES`: Yes.
 * - `NO`: (Default) No.
 * @param options The filter effect options. See `AgoraFilterEffectOptions`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - Use the default value `AgoraMediaSourceTypePrimaryCamera` if you use camera to capture local
 * video.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setFilterEffectOptions:(BOOL)enable options:(AgoraFilterEffectOptions* _Nullable)options sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(setFilterEffectOptions(_:options:sourceType:));

#pragma mark VideoEffectObject
/**
 * @brief Creates a video effect object.
 *
 * @since v4.6.0
 *
 * @details
 * Creates an `AgoraVideoEffectObject` video effect object and returns its pointer.
 *
 * @param bundlePath The path to the video effect bundle.
 * @param sourceType The media source type, such as AgoraMediaSourceTypePrimaryCamera. See
 * `AgoraMediaSourceType`.
 *
 * @return
 * Returns an `AgoraVideoEffectObject` object if the method call succeeds. Returns an empty pointer
 * if the method call fails.
 */
- (id<AgoraVideoEffectObject> _Nullable)createVideoEffectObjectWithBundlePath:(NSString * _Nonnull)bundlePath
                                                                   sourceType:(AgoraMediaSourceType)sourceType
    NS_SWIFT_NAME(createVideoEffectObject(bundlePath:sourceType:));

/**
 * @brief Destroys a video effect object.
 *
 * @since v4.6.0
 *
 * @param videoEffectObject Pointer to the `AgoraVideoEffectObject` instance to be destroyed. See
 * `AgoraVideoEffectObject`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)destroyVideoEffectObject:(id<AgoraVideoEffectObject> _Nullable)videoEffectObject
    NS_SWIFT_NAME(destroyVideoEffectObject(_:));


/**
 * @brief Sets low-light enhancement.
 *
 * @since v4.0.0
 *
 * @details
 * You can call this method to enable the color enhancement feature and set the options of the color
 * enhancement effect.
 * Applicable scenarios: The low-light enhancement feature can adaptively adjust the brightness
 * value of the video captured in situations with low or uneven lighting, such as backlit, cloudy,
 * or dark scenes. It restores or highlights the image details and improves the overall visual
 * effect of the video.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - Dark light enhancement has certain requirements for equipment performance. The low-light
 * enhancement feature has certain performance requirements on devices. If your device overheats
 * after you enable low-light enhancement, Agora recommends modifying the low-light enhancement
 * options to a less performance-consuming level or disabling low-light enhancement entirely.
 * - If you want to prioritize image quality ( AgoraLowlightEnhanceLevelQuality ) when using the
 * low-light enhancement function, you need to first call `setVideoDenoiserOptions:options:` to
 * achieve video noise reduction, the specific corresponding relationship is as follows:
 *   - When low light enhancement is set to automatic mode ( AgoraLowlightEnhanceModeAuto ), video
 * noise reduction needs to be set to prioritize image quality ( AgoraVideoDenoiserLevelHighQuality
 * ) and automatic mode ( AgoraVideoDenoiserModeAuto ).
 *   - When low-light enhancement is set to manual mode ( AgoraLowlightEnhanceModeManual ), video
 * noise reduction needs to be set to prioritize image quality ( AgoraVideoDenoiserLevelHighQuality
 * ) and manual mode ( AgoraVideoDenoiserModeManual ).
 *
 * @param enable Whether to enable low-light enhancement:
 * - `YES`: Enable low-light enhancement.
 * - `NO`: (Default) Disable low-light enhancement.
 * @param options The low-light enhancement options. See `AgoraLowlightEnhanceOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLowlightEnhanceOptions:(BOOL)enable options:(AgoraLowlightEnhanceOptions* _Nullable)options  NS_SWIFT_NAME(setLowlightEnhanceOptions(_:options:));

/**
 * @brief Sets low light enhance options and specifies the media source.
 *
 * @since v4.0.0
 *
 * @details
 * This method and `setLowlightEnhanceOptions:options:` both set low light enhance options, but this
 * method allows you to specify the media source to which the low light enhance options are applied.
 * Applicable scenarios: dark environments and low-end video capture devices can cause video images
 * to contain significant noise, which affects video quality. In real-time interactive scenarios,
 * video noise also consumes bitstream resources and reduces encoding efficiency during encoding.
 * Call timing: Call this method after calling `enableVideo`.
 *
 * @note
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 * - Video noise reduction has certain requirements for equipment performance. If your device
 * overheats after you enable video noise reduction, Agora recommends modifying the video noise
 * reduction options to a less performance-consuming level or disabling video noise reduction
 * entirely.
 *
 * @param enable Whether to enable low-light enhancement:
 * - `YES`: Enable low-light enhancement.
 * - `NO`: (Default) Disable low-light enhancement.
 * @param options The low-light enhancement options. See `AgoraLowlightEnhanceOptions`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - The default value is `AgoraMediaSourceTypePrimaryCamera`.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLowlightEnhanceOptions:(BOOL)enable options:(AgoraLowlightEnhanceOptions* _Nullable)options sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(setLowlightEnhanceOptions(_:options:sourceType:));

/**
 * @brief Sets color enhancement.
 *
 * @since v4.0.0
 *
 * @details
 * The video images captured by the camera can have color distortion. The color enhancement feature
 * intelligently adjusts video characteristics such as saturation and contrast to enhance the video
 * color richness and color reproduction, making the video more vivid.
 * You can call this method to enable the color enhancement feature and set the options of the color
 * enhancement effect.
 *
 * @note
 * - Call this method after calling `enableVideo`.
 * - The color enhancement feature has certain performance requirements on devices. With color
 * enhancement turned on, Agora recommends that you change the color enhancement level to one that
 * consumes less performance or turn off color enhancement if your device is experiencing severe
 * heat problems.
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 *
 * @param enable Whether to enable color enhancement:
 * - `YES` Enable color enhancement.
 * - `NO`: (Default) Disable color enhancement.
 * @param options The color enhancement options. See `AgoraColorEnhanceOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setColorEnhanceOptions:(BOOL)enable options:(AgoraColorEnhanceOptions* _Nullable)options  NS_SWIFT_NAME(setColorEnhanceOptions(_:options:));

/**
 * @brief Sets color enhance options and specifies the media source.
 *
 * @since v4.0.0
 *
 * @details
 * The video images captured by the camera can have color distortion. The color enhancement feature
 * intelligently adjusts video characteristics such as saturation and contrast to enhance the video
 * color richness and color reproduction, making the video more vivid.
 * Both this method and `setColorEnhanceOptions:options:` set color enhancement options, but this
 * method allows you to specify the media source to which the color enhance options are applied.
 *
 * @note
 * - Call this method after calling `enableVideo`.
 * - The color enhancement feature has certain performance requirements on devices. With color
 * enhancement turned on, Agora recommends that you change the color enhancement level to one that
 * consumes less performance or turn off color enhancement if your device is experiencing severe
 * heat problems.
 * - This method relies on the image enhancement dynamic library
 * `AgoraClearVisionExtension.xcframework` (iOS) or `AgoraClearVisionExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 *
 * @param enable Whether to enable color enhancement:
 * - `YES` Enable color enhancement.
 * - `NO`: (Default) Disable color enhancement.
 * @param options The color enhancement options. See `AgoraColorEnhanceOptions`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - The default value is `AgoraMediaSourceTypePrimaryCamera`.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setColorEnhanceOptions:(BOOL)enable options:(AgoraColorEnhanceOptions* _Nullable)options sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(setColorEnhanceOptions(_:options:sourceType:));

/**
 * @brief Enables/Disables the virtual background.
 *
 * @since v3.7.200
 *
 * @details
 * The virtual background feature enables the local user to replace their original background with a
 * static image, dynamic video, blurred background, or portrait-background segmentation to achieve
 * picture-in-picture effect. Once the virtual background feature is enabled, all users in the
 * channel can see the custom background.
 * Call this method after calling `enableVideo` or `startPreview:`.
 *
 * @note
 * - Using a video as a your virtual background will lead to continuous increase in memory usage,
 * which may cause issues such as app crashes. Therefore,it is recommended to reduce the resolution
 * and frame rate of the video when using it.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device. Agora recommends you use virtual
 * background on devices with the following processors:
 *   - On iOS: Devices with an A9 chip and better, as follows:
 *     - iPhone 6S and later
 *     - iPad Air 3rd generation and later
 *     - iPad 5th generation and later
 *     - iPad Pro 1st generation and later
 *     - iPad mini 5th generation and later
 *   - On macOS: Devices with an i5 CPU and better
 * - Agora recommends that you use this feature in scenarios that meet the following conditions:
 *   - A high-definition camera device is used, and the environment is uniformly lit.
 *   - There are few objects in the captured video. Portraits are half-length and unobstructed.
 * Ensure that the background is a solid color that is different from the color of the user's
 * clothing.
 * - This method relies on the virtual background dynamic library
 * `AgoraVideoSegmentationExtension.xcframework` (iOS) or
 * `AgoraVideoSegmentationExtension.framework` (macOS). If the dynamic library is deleted, the
 * function
 * cannot be enabled normally.
 *
 * @param enable Whether to enable virtual background:
 * - `YES`: Enable virtual background.
 * - `NO`: Disable virtual background.
 * @param backData The custom background. See `AgoraVirtualBackgroundSource`. To adapt the
 * resolution of the custom background image to that of the video captured by the SDK, the SDK
 * scales and crops the custom background image while ensuring that the content of the custom
 * background image is not distorted.
 * @param segData Processing properties for background images. See `AgoraSegmentationProperty`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -4: The device capabilities do not meet the requirements for the virtual background feature.
 * Agora recommends you try it on devices with higher performance.
 */
- (int)enableVirtualBackground:(BOOL)enable backData:(AgoraVirtualBackgroundSource* _Nullable)backData segData:(AgoraSegmentationProperty* _Nullable)segData NS_SWIFT_NAME(enableVirtualBackground(_:backData:segData:));

/**
 * @brief Enables virtual background and specify the media source, or disables virtual background.
 *
 * @since v3.7.200
 *
 * @details
 * The virtual background feature enables the local user to replace their original background with a
 * static image, dynamic video, blurred background, or portrait-background segmentation to achieve
 * picture-in-picture effect. Once the virtual background feature is enabled, all users in the
 * channel can see the custom background.
 * Both this method and `enableVirtualBackground:backData:segData:` enable/disable virtual
 * background, but this method allows you to specify the media source to which the virtual
 * background is applied.
 * Call this method after calling `enableVideo` or `startPreview:`.
 *
 * @note
 * - Using a video as a your virtual background will lead to continuous increase in memory usage,
 * which may cause issues such as app crashes. Therefore,it is recommended to reduce the resolution
 * and frame rate of the video when using it.
 * - This feature has high requirements on device performance. When calling this method, the SDK
 * automatically checks the capabilities of the current device. Agora recommends you use virtual
 * background on devices with the following processors:
 *   - On iOS: Devices with an A9 chip and better, as follows:
 *     - iPhone 6S and later
 *     - iPad Air 3rd generation and later
 *     - iPad 5th generation and later
 *     - iPad Pro 1st generation and later
 *     - iPad mini 5th generation and later
 *   - On macOS: Devices with an i5 CPU and better
 * - Agora recommends that you use this feature in scenarios that meet the following conditions:
 *   - A high-definition camera device is used, and the environment is uniformly lit.
 *   - There are few objects in the captured video. Portraits are half-length and unobstructed.
 * Ensure that the background is a solid color that is different from the color of the user's
 * clothing.
 * - This method relies on the virtual background dynamic library
 * `AgoraVideoSegmentationExtension.xcframework` (iOS) or
 * `AgoraVideoSegmentationExtension.framework` (macOS). If the dynamic library is deleted, the
 * function
 * cannot be enabled normally.
 *
 * @param enable Whether to enable virtual background:
 * - `YES`: Enable virtual background.
 * - `NO`: Disable virtual background.
 * @param backData The custom background. See `AgoraVirtualBackgroundSource`. To adapt the
 * resolution of the custom background image to that of the video captured by the SDK, the SDK
 * scales and crops the custom background image while ensuring that the content of the custom
 * background image is not distorted.
 * @param segData Processing properties for background images. See `AgoraSegmentationProperty`.
 * @param sourceType The type of the media source to which the filter effect is applied. See
 * `AgoraMediaSourceType`.Attention: In this method, this parameter supports only the following two
 * settings:
 * - The default value is `AgoraMediaSourceTypePrimaryCamera`.
 * - Set this parameter to `AgoraMediaSourceTypeCustomVideo` if you use custom video source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -4: The device capabilities do not meet the requirements for the virtual background feature.
 * Agora recommends you try it on devices with higher performance.
 */
- (int)enableVirtualBackground:(BOOL)enable backData:(AgoraVirtualBackgroundSource* _Nullable)backData segData:(AgoraSegmentationProperty* _Nullable)segData sourceType:(AgoraMediaSourceType)sourceType NS_SWIFT_NAME(enableVirtualBackground(_:backData:segData:sourceType:));

/**
 * @brief Sets video application scenarios.
 *
 * @since v4.2.0
 *
 * @details
 * After successfully calling this method, the SDK will automatically enable the best practice
 * strategies and adjust key performance metrics based on the specified scenario, to optimize the
 * video experience.
 *
 * @note Call this method before joining a channel.
 *
 * @param scenarioType The type of video application scenario. See
 * `AgoraApplicationScenarioType`.`AgoraApplicationMeetingScenario` (1) is suitable for meeting
 * scenarios. The SDK automatically enables the following strategies:
 * - In meeting scenarios where low-quality video streams are required to have a high bitrate, the
 * SDK automatically enables multiple technologies used to deal with network congestions, to enhance
 * the performance of the low-quality streams and to ensure the smooth reception by subscribers.
 * - The SDK monitors the number of subscribers to the high-quality video stream in real time and
 * dynamically adjusts its configuration based on the number of subscribers.
 *   - If nobody subscribers to the high-quality stream, the SDK automatically reduces its bitrate
 * and frame rate to save upstream bandwidth.
 *   - If someone subscribes to the high-quality stream, the SDK resets the high-quality stream to
 * the `AgoraVideoEncoderConfiguration` configuration used in the most recent calling of
 * `setVideoEncoderConfiguration:`. If no configuration has been set by the user previously, the
 * following values are used:
 *     - iOS:
 *       - Resolution: 960 × 540
 *       - Frame rate: 15 fps
 *       - Bitrate: 1000 Kbps
 *     - macOS:
 *       - Resolution: 1280 × 720
 *       - Frame rate: 15 fps
 *       - Bitrate: 1600 Kbps
 * - The SDK monitors the number of subscribers to the low-quality video stream in real time and
 * dynamically enables or disables it based on the number of subscribers.Note: If the user has
 * called `setDualStreamMode:streamConfig:` to set that never send low-quality video stream (
 * `AgoraDisableSimulcastStream` ), the dynamic adjustment of the low-quality stream in meeting
 * scenarios will not take effect.
 *   - If nobody subscribes to the low-quality stream, the SDK automatically disables it to save
 * upstream bandwidth.
 *   - If someone subscribes to the low-quality stream, the SDK enables the low-quality stream and
 * resets it to the `AgoraSimulcastStreamConfig` configuration used in the most recent calling of
 * `setDualStreamMode:streamConfig:`. If no configuration has been set by the user previously, the
 * following values are used:
 *     - Resolution: 480 × 272
 *     - Frame rate: 15 fps
 *     - Bitrate: 500 Kbps
 * `AgoraApplication1V1Scenario` (2) This is applicable to the `one to one live` scenario. To meet
 * the requirements for low latency and high-quality video in this scenario, the SDK optimizes its
 * strategies, improving performance in terms of video quality, first frame rendering, latency on
 * mid-to-low-end devices, and smoothness under weak network conditions.Attention: This enumeration
 * value is only applicable to the broadcaster vs. broadcaster scenario.
 * `AgoraApplicationLiveShowScenario` (3) This is applicable to the `show room` scenario. In this
 * scenario, fast video rendering and high image quality are crucial. The SDK implements several
 * performance optimizations, including automatically enabling accelerated audio and video frame
 * rendering to minimize first-frame latency (no need to call `enableInstantMediaRendering` ), and
 * B-frame encoding to achieve better image quality and bandwidth efficiency. The SDK also provides
 * enhanced video quality and smooth playback, even in poor network conditions or on lower-end
 * devices.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -4: Video application scenarios are not supported. Possible reasons include that you use the
 * Voice SDK instead of the Video SDK.
 *   - -7: The `AgoraRtcEngineKit` object has not been initialized. You need to initialize the
 * `AgoraRtcEngineKit` object before calling this method.
 */
- (int)setVideoScenario:(AgoraApplicationScenarioType)scenarioType  NS_SWIFT_NAME(setVideoScenario(_:));

/**  Sets the video qoe preference. You can call this method to set the expected QoE Preference. The SDK will optimize the video experience for each preference you set.

  @since v4.2.1

  @param preferenceType The qoe preference type. See #VIDEO_QOE_PREFERENCE_TYPE.
  @return * - 0: Success.
  * < 0: Failure.
    - -1(ERR_FAILED): A general error occurs (no specified reason).
    - -4(ERR_NOT_SUPPORTED): Unable to set video application scenario.
    - -7(ERR_NOT_INITIALIZED): The SDK is not initialized.
  */
- (int)setVideoQoEPreference:(AgoraQoEPreferenceType)preferenceType  NS_SWIFT_NAME(setVideoQoEPreference(_:));

/**
 * @brief Checks whether the device supports the specified advanced feature.
 *
 * @since v4.3.0
 *
 * @details
 * Checks whether the capabilities of the current device meet the requirements for advanced features
 * such as virtual background and image enhancement.
 * Applicable scenarios: Before using advanced features, you can check whether the current device
 * supports these features based on the call result. This helps to avoid performance degradation or
 * unavailable features when enabling advanced features on low-end devices. Based on the return
 * value of this method, you can decide whether to display or enable the corresponding feature
 * button, or notify the user when the device's capabilities are insufficient.
 *
 * @param type The type of the advanced feature, see `AgoraFeatureType`.
 *
 * @return
 * - `YES`: The current device supports the specified feature.
 * - `NO`: The current device does not support the specified feature.
 */
- (BOOL)isFeatureAvailableOnDevice:(AgoraFeatureType)type;

#pragma mark Audio Effect
/**
 * @brief Changes the voice pitch of the local speaker.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param pitch The local voice pitch. The value range is [0.5,2.0]. The lower the value, the lower
 * the pitch. The default value is 1.0 (no change to the pitch).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLocalVoicePitch:(double)pitch NS_SWIFT_NAME(setLocalVoicePitch(_:));

/**
 * @brief Sets the formant ratio to change the timbre of human voice.
 *
 * @details
 * Formant ratio affects the timbre of voice. The smaller the value, the deeper the sound will be,
 * and the larger, the sharper. After you set the formant ratio, all users in the channel can hear
 * the changed voice. If you want to change the timbre and pitch of voice at the same time, Agora
 * recommends using this method together with `setLocalVoicePitch:`.
 * Applicable scenarios: You can call this method to set the formant ratio of local audio to change
 * the timbre of human voice.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param formantRatio The formant ratio. The value range is [-1.0, 1.0]. The default value is 0.0,
 * which means do not change the timbre of the voice.Note: Agora recommends setting this value
 * within the range of [-0.4, 0.6]. Otherwise, the voice may be seriously distorted.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLocalVoiceFormant:(double)formantRatio;

/**
 * @brief Sets the local voice equalization effect.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param bandFrequency The band frequency. The value ranges between 0 and 9; representing the
 * respective 10-band center frequencies of the voice effects, including 31, 62, 125, 250, 500, 1k,
 * 2k, 4k, 8k, and 16k Hz. See `AgoraAudioEqualizationBandFrequency`.
 * @param gain The gain of each band in dB. The value ranges between -15 and 15. The default value
 * is 0.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLocalVoiceEqualizationOfBandFrequency:(AgoraAudioEqualizationBandFrequency)bandFrequency withGain:(NSInteger)gain NS_SWIFT_NAME(setLocalVoiceEqualizationOf(_:withGain:));

/**
 * @brief Sets the local voice reverberation.
 *
 * @details
 * The SDK provides an easier-to-use method, `setAudioEffectPreset:`, to directly implement preset
 * reverb effects for such as pop, R&B, and KTV.
 *
 * @note You can call this method either before or after joining a channel.
 *
 * @param reverbType The reverberation key. Agora provides five reverberation keys, see
 * `AgoraAudioReverbType`.
 * @param value The value of the reverberation key.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLocalVoiceReverbOfType:(AgoraAudioReverbType)reverbType withValue:(NSInteger)value NS_SWIFT_NAME(setLocalVoiceReverbOf(_:withValue:));

#pragma mark Audio Effect Playback

/**
 * @brief Preloads a specified audio effect file into the memory.
 *
 * @details
 * Ensure the size of all preloaded files does not exceed the limit.
 * For the audio file formats supported by this method, see `What formats of audio files does the
 * Agora RTC SDK support`.
 * Call timing: Agora recommends that you call this method before joining a channel.
 * 
 * @note
 * - If preloadEffect is called before playEffect is executed, the file resource will not be closed after playEffect. 
 * The next time playEffect is executed, it will directly seek to play at the beginning.
 * - If preloadEffect is not called before playEffectEx is executed, the resource will be destroyed after playEffect. 
 * The next time playEffect is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.
 * @param filePath File path:
 * - iOS or macOS: The absolute path or URL address (including the suffixes of the filename) of the
 * audio effect file. For example: `/var/mobile/Containers/Data/audio.mp4`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)preloadEffect:(int)soundId
            filePath:(NSString* _Nonnull)filePath NS_SWIFT_NAME(preloadEffect(_:filePath:));

/** @brief Preloads a specified audio effect file into the memory.
 *
 * @details
 * Ensure the size of all preloaded files does not exceed the limit.
 * For the audio file formats supported by this method, see `What formats of audio files does the
 * Agora RTC SDK support`.
 * Call timing: Agora recommends that you call this method before joining a channel.
 *
 * @note
 * - If preloadEffect is called before playEffect is executed, the file resource will not be closed after playEffect. 
 * The next time playEffect is executed, it will directly seek to play at the beginning.
 * - If preloadEffect is not called before playEffect is executed, the resource will be destroyed after playEffect. 
 * The next time playEffect is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.
 * @param filePath File path:
 * - iOS or macOS: The absolute path or URL address (including the suffixes of the filename) of the
 * audio effect file. For example: `/var/mobile/Containers/Data/audio.mp4`.
 * @param startPos The playback position (ms) of the audio effect file.

 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)preloadEffect:(int)soundId
            filePath:(NSString* _Nonnull)filePath
            startPos:(int)startPos NS_SWIFT_NAME(preloadEffect(_:filePath:startPos:));

/**
 * @brief Plays the specified local or online audio effect file.
 *
 * @details
 * To play multiple audio effect files at the same time, call this method multiple times with
 * different `soundId` and `filePath`. To achieve the optimal user experience, Agora recommends that
 * you do not playing more than three audio files at the same time.
 * Call timing: You can call this method either before or after joining a channel.
 * Related callbacks: After the playback of an audio effect file completes, the SDK triggers the
 * `rtcEngineDidAudioEffectFinish:soundId:` callback.
 *
 * @note 
 * - If you need to play an online audio effect file, Agora recommends that you cache the online
 * audio effect file to your local device, call `preloadEffect:filePath:` to preload the file into
 * memory, and then call this method to play the audio effect. Otherwise, you might encounter
 * playback failures or no sound during playback due to loading timeouts or failures.
 * - If preloadEffect is called before playEffect is executed, the file resource will not be closed after playEffect. 
 * The next time playEffect is executed, it will directly seek to play at the beginning.
 * - If preloadEffect is not called before playEffectEx is executed, the resource will be destroyed after playEffect. 
 * The next time playEffect is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.Attention: If you
 * have preloaded an audio effect into memory by calling `preloadEffect:filePath:`, ensure that the
 * value of this parameter is the same as that of `soundId` in `preloadEffect:filePath:`.
 * @param filePath The file path. The SDK supports file paths starting with `ipod-library//`, URLs
 * and absolute file paths of local files. The absolute path needs to be accurate to the file name
 * and extension. Supported audio formats include MP3, AAC, M4A, MP4, WAV, and 3GP. See `Supported
 * Audio Formats`.
 * Attention: If you have preloaded an audio effect into memory by calling
 * `preloadEffect:filePath:`, ensure that the value of this parameter is the same as that of
 * `filePath` in `preloadEffect:filePath:`.
 * @param loopCount The number of times the audio effect loops.
 * - ≥ 0: The number of playback times. For example, 1 means looping one time, which means playing
 * the audio effect two times in total.
 * - -1: Play the audio file in an infinite loop.
 * @param pitch The pitch of the audio effect. The value range is 0.5 to 2.0. The default value is
 * 1.0, which means the original pitch. The lower the value, the lower the pitch.
 * @param pan The spatial position of the audio effect. The value ranges between -1.0 and 1.0:
 * - -1.0: The audio effect is heard on the left of the user.
 * - 0.0: The audio effect is heard in front of the user.
 * - 1.0: The audio effect is heard on the right of the user.
 * @param gain The volume of the audio effect. The value range is 0.0 to 100.0. The default value is
 * 100.0, which means the original volume. The smaller the value, the lower the volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playEffect:(int)soundId
         filePath:(NSString* _Nonnull)filePath
        loopCount:(NSInteger)loopCount
            pitch:(double)pitch
              pan:(double)pan
             gain:(NSInteger)gain NS_SWIFT_NAME(playEffect(_:filePath:loopCount:pitch:pan:gain:));


/**
 * @brief Plays the specified local or online audio effect file.
 *
 * @details
 * To play multiple audio effect files at the same time, call this method multiple times with
 * different `soundId` and `filePath`. To achieve the optimal user experience, Agora recommends that
 * you do not playing more than three audio files at the same time.
 * Call timing: You can call this method either before or after joining a channel.
 * Related callbacks: After the playback of an audio effect file completes, the SDK triggers the
 * `rtcEngineDidAudioEffectFinish:soundId:` callback.
 *
 * @note 
 * - If you need to play an online audio effect file, Agora recommends that you cache the online
 * audio effect file to your local device, call `preloadEffect:filePath:` to preload the file into
 * memory, and then call this method to play the audio effect. Otherwise, you might encounter
 * playback failures or no sound during playback due to loading timeouts or failures.
 * - If preloadEffect is called before playEffect is executed, the file resource will not be closed after playEffect. 
 * The next time playEffect is executed, it will directly seek to play at the beginning.
 * - If preloadEffect is not called before playEffectEx is executed, the resource will be destroyed after playEffect. 
 * The next time playEffect is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.Attention: If you
 * have preloaded an audio effect into memory by calling `preloadEffect:filePath:`, ensure that the
 * value of this parameter is the same as that of `soundId` in `preloadEffect:filePath:`.
 * @param filePath The file path. The SDK supports file paths starting with `ipod-library//`, URLs
 * and absolute file paths of local files. The absolute path needs to be accurate to the file name
 * and extension. Supported audio formats include MP3, AAC, M4A, MP4, WAV, and 3GP. See `Supported
 * Audio Formats`.
 * Attention: If you have preloaded an audio effect into memory by calling
 * `preloadEffect:filePath:`, ensure that the value of this parameter is the same as that of
 * `filePath` in `preloadEffect:filePath:`.
 * @param loopCount The number of times the audio effect loops.
 * - ≥ 0: The number of playback times. For example, 1 means looping one time, which means playing
 * the audio effect two times in total.
 * - -1: Play the audio file in an infinite loop.
 * @param pitch The pitch of the audio effect. The value range is 0.5 to 2.0. The default value is
 * 1.0, which means the original pitch. The lower the value, the lower the pitch.
 * @param pan The spatial position of the audio effect. The value ranges between -1.0 and 1.0:
 * - -1.0: The audio effect is heard on the left of the user.
 * - 0.0: The audio effect is heard in front of the user.
 * - 1.0: The audio effect is heard on the right of the user.
 * @param gain The volume of the audio effect. The value range is 0.0 to 100.0. The default value is
 * 100.0, which means the original volume. The smaller the value, the lower the volume.
 * @param startPos The playback position (ms) of the audio effect file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playEffect:(int)soundId
         filePath:(NSString* _Nonnull)filePath
        loopCount:(NSInteger)loopCount
            pitch:(double)pitch
              pan:(double)pan
             gain:(NSInteger)gain
             startPos:(int)startPos NS_SWIFT_NAME(playEffect(_:filePath:loopCount:pitch:pan:gain:startPos:));

/**
 * @brief Plays the specified local or online audio effect file.
 *
 * @details
 * To play multiple audio effect files at the same time, call this method multiple times with
 * different `soundId` and `filePath`. To achieve the optimal user experience, Agora recommends that
 * you do not playing more than three audio files at the same time.
 * Call timing: You can call this method either before or after joining a channel.
 * Related callbacks: After the playback of an audio effect file completes, the SDK triggers the
 * `rtcEngineDidAudioEffectFinish:soundId:` callback.
 *
 * @note 
 * - If you need to play an online audio effect file, Agora recommends that you cache the online
 * audio effect file to your local device, call `preloadEffect:filePath:` to preload the file into
 * memory, and then call this method to play the audio effect. Otherwise, you might encounter
 * playback failures or no sound during playback due to loading timeouts or failures.
 * - If preloadEffect is called before playEffect is executed, the file resource will not be closed after playEffect. 
 * The next time playEffect is executed, it will directly seek to play at the beginning.
 * - If preloadEffect is not called before playEffectEx is executed, the resource will be destroyed after playEffect. 
 * The next time playEffect is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.Attention: If you
 * have preloaded an audio effect into memory by calling `preloadEffect:filePath:`, ensure that the
 * value of this parameter is the same as that of `soundId` in `preloadEffect:filePath:`.
 * @param filePath The file path. The SDK supports file paths starting with `ipod-library//`, URLs
 * and absolute file paths of local files. The absolute path needs to be accurate to the file name
 * and extension. Supported audio formats include MP3, AAC, M4A, MP4, WAV, and 3GP. See `Supported
 * Audio Formats`.
 * Attention: If you have preloaded an audio effect into memory by calling
 * `preloadEffect:filePath:`, ensure that the value of this parameter is the same as that of
 * `filePath` in `preloadEffect:filePath:`.
 * @param loopCount The number of times the audio effect loops.
 * - ≥ 0: The number of playback times. For example, 1 means looping one time, which means playing
 * the audio effect two times in total.
 * - -1: Play the audio file in an infinite loop.
 * @param pitch The pitch of the audio effect. The value range is 0.5 to 2.0. The default value is
 * 1.0, which means the original pitch. The lower the value, the lower the pitch.
 * @param pan The spatial position of the audio effect. The value ranges between -1.0 and 1.0:
 * - -1.0: The audio effect is heard on the left of the user.
 * - 0.0: The audio effect is heard in front of the user.
 * - 1.0: The audio effect is heard on the right of the user.
 * @param gain The volume of the audio effect. The value range is 0.0 to 100.0. The default value is
 * 100.0, which means the original volume. The smaller the value, the lower the volume.
 * @param publish Whether to publish the audio effect to the remote users:
 * - `YES`: Publish the audio effect to the remote users. Both the local user and remote users can
 * hear the audio effect.
 * - `NO`: Do not publish the audio effect to the remote users. Only the local user can hear the
 * audio effect.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playEffect:(int)soundId
         filePath:(NSString* _Nonnull)filePath
        loopCount:(NSInteger)loopCount
            pitch:(double)pitch
              pan:(double)pan
             gain:(NSInteger)gain
          publish:(BOOL)publish NS_SWIFT_NAME(playEffect(_:filePath:loopCount:pitch:pan:gain:publish:));

/**
 * @brief Plays the specified local or online audio effect file.
 *
 * @details
 * To play multiple audio effect files at the same time, call this method multiple times with
 * different `soundId` and `filePath`. To achieve the optimal user experience, Agora recommends that
 * you do not playing more than three audio files at the same time.
 * Call timing: You can call this method either before or after joining a channel.
 * Related callbacks: After the playback of an audio effect file completes, the SDK triggers the
 * `rtcEngineDidAudioEffectFinish:soundId:` callback.
 *
 * @note 
 * - If you need to play an online audio effect file, Agora recommends that you cache the online
 * audio effect file to your local device, call `preloadEffect:filePath:` to preload the file into
 * memory, and then call this method to play the audio effect. Otherwise, you might encounter
 * playback failures or no sound during playback due to loading timeouts or failures.
 * - If preloadEffect is called before playEffect is executed, the file resource will not be closed after playEffect. 
 * The next time playEffect is executed, it will directly seek to play at the beginning.
 * - If preloadEffect is not called before playEffect is executed, the resource will be destroyed after playEffect. 
 * The next time playEffect is executed, it will try to reopen the file and play it from the beginning.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.Attention: If you
 * have preloaded an audio effect into memory by calling `preloadEffect:filePath:`, ensure that the
 * value of this parameter is the same as that of `soundId` in `preloadEffect:filePath:`.
 * @param filePath The file path. The SDK supports file paths starting with `ipod-library//`, URLs
 * and absolute file paths of local files. The absolute path needs to be accurate to the file name
 * and extension. Supported audio formats include MP3, AAC, M4A, MP4, WAV, and 3GP. See `Supported
 * Audio Formats`.
 * Attention: If you have preloaded an audio effect into memory by calling
 * `preloadEffect:filePath:`, ensure that the value of this parameter is the same as that of
 * `filePath` in `preloadEffect:filePath:`.
 * @param loopCount The number of times the audio effect loops.
 * - ≥ 0: The number of playback times. For example, 1 means looping one time, which means playing
 * the audio effect two times in total.
 * - -1: Play the audio file in an infinite loop.
 * @param pitch The pitch of the audio effect. The value range is 0.5 to 2.0. The default value is
 * 1.0, which means the original pitch. The lower the value, the lower the pitch.
 * @param pan The spatial position of the audio effect. The value ranges between -1.0 and 1.0:
 * - -1.0: The audio effect is heard on the left of the user.
 * - 0.0: The audio effect is heard in front of the user.
 * - 1.0: The audio effect is heard on the right of the user.
 * @param gain The volume of the audio effect. The value range is 0.0 to 100.0. The default value is
 * 100.0, which means the original volume. The smaller the value, the lower the volume.
 * @param publish Whether to publish the audio effect to the remote users:
 * - `YES`: Publish the audio effect to the remote users. Both the local user and remote users can
 * hear the audio effect.
 * - `NO`: Do not publish the audio effect to the remote users. Only the local user can hear the
 * audio effect.
 * @param startPos The playback position (ms) of the audio effect file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playEffect:(int)soundId
         filePath:(NSString* _Nonnull)filePath
        loopCount:(NSInteger)loopCount
            pitch:(double)pitch
              pan:(double)pan
             gain:(NSInteger)gain
          publish:(BOOL)publish
         startPos:(int)startPos NS_SWIFT_NAME(playEffect(_:filePath:loopCount:pitch:pan:gain:publish:startPos:));

/** Plays all audio effects.
   *
   * After calling \ref preloadEffect:filePath: preloadEffect multiple times
   * to preload multiple audio effects into the memory, you can call this
   * method to play all the specified audio effects for all users in
   * the channel.
   *
   * @param loopCount The number of times the audio effect loops:
   * - `-1`: Play the audio effect in an indefinite loop until
   *  \ref AgoraRtcEngineKit.stopEffect: stopEffect or
   *  \ref AgoraRtcEngineKit.stopAllEffects stopAllEffects
   * - `0`: Play the audio effect once.
   * - `1`: Play the audio effect twice.
   * @param pitch The pitch of the audio effect. The value ranges between 0.5 and 2.0.
   * The default value is `1.0` (original pitch). The lower the value, the lower the pitch.
   * @param pan The spatial position of the audio effect. The value ranges between -1.0 and 1.0:
   * - `-1.0`: The audio effect displays to the left.
   * - `0.0`: The audio effect displays ahead.
   * - `1.0`: The audio effect displays to the right.
   * @param gain The volume of the audio effect. The value ranges between 0 and 100.
   * The default value is `100` (original volume). The lower the value, the lower
   * the volume of the audio effect.
   * @param publish Sets whether to publish the audio effect to the remote:
   * - true: Publish the audio effect to the remote.
   * - false: Do not publish the audio effect to the remote.
   *
   * @return
   * - 0: Success.
   * - < 0: Failure.
   */
- (int)playAllEffectsWithLoopCount:(NSInteger)loopCount
                         pitch:(double)pitch
                           pan:(double)pan
                          gain:(NSInteger)gain
                       publish:(BOOL)publish NS_SWIFT_NAME(playAllEffects(withLoopCount:pitch:pan:gain:publish:));

/**
 * @brief Retrieves the volume of the audio effects.
 *
 * @details
 * The volume is an integer ranging from 0 to 100. The default value is 100, which means the
 * original volume.
 *
 * @note Call this method after `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:`.
 *
 * @return
 * - Volume of the audio effects, if this method call succeeds.
 * - < 0: Failure.
 */
- (int)getEffectsVolume NS_SWIFT_NAME(getEffectsVolume());

/**
 * @brief Sets the volume of the audio effects.
 *
 * @details
 * Call timing: Call this method after
 * `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:`.
 *
 * @param volume The playback volume. The value range is [0, 100]. The default value is 100, which
 * represents the original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setEffectsVolume:(NSInteger)volume NS_SWIFT_NAME(setEffectsVolume(_:));

/**
 * @brief Gets the volume of a specified audio effect file.
 *
 * @details
 * Call timing: Call this method after
 * `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:`.
 *
 * @param soundId The ID of the audio effect. The unique ID of each audio effect file.
 * @param volume The playback volume. The value range is [0, 100]. The default value is 100, which
 * represents the original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVolumeOfEffect:(int)soundId
              withVolume:(int)volume NS_SWIFT_NAME(setVolumeOfEffect(_:withVolume:));

/**
 * @brief Gets the volume of a specified audio effect file.
 *
 * @param soundId The ID of the audio effect file.
 *
 * @return
 * - ≥ 0: Returns the volume of the specified audio effect, if the method call is successful. The
 * value ranges between 0 and 100. 100 represents the original volume.
 * - < 0: Failure.
 */
- (int)getVolumeOfEffect:(int)soundId NS_SWIFT_NAME(getVolumeOfEffect(_:));

/**
 * @brief Pauses a specified audio effect file.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)pauseEffect:(int)soundId NS_SWIFT_NAME(pauseEffect(_:));

/**
 * @brief Pauses all audio effects.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)pauseAllEffects NS_SWIFT_NAME(pauseAllEffects());

/**
 * @brief Resumes playing a specified audio effect.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)resumeEffect:(int)soundId NS_SWIFT_NAME(resumeEffect(_:));

/**
 * @brief Resumes playing all audio effect files.
 *
 * @details
 * After you call `pauseAllEffects` to pause the playback, you can call this method to resume the
 * playback.
 * Call timing: Call this method after `pauseAllEffects`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)resumeAllEffects NS_SWIFT_NAME(resumeAllEffects());

/**
 * @brief Stops playing a specified audio effect.
 *
 * @details
 * When you no longer need to play the audio effect, you can call this method to stop the playback.
 * If you only need to pause the playback, call `pauseEffect:`.
 * Call timing: Call this method after the `playEffect:filePath:loopCount:pitch:pan:gain:`,
 * `playEffect:filePath:loopCount:pitch:pan:gain:publish:` or
 * `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:`.
 *
 * @param soundId The ID of the audio effect. Each audio effect has a unique ID.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopEffect:(int)soundId NS_SWIFT_NAME(stopEffect(_:));

/**
 * @brief Stops playing all audio effects.
 *
 * @details
 * When you no longer need to play the audio effect, you can call this method to stop the playback.
 * If you only need to pause the playback, call `pauseAllEffects`.
 * Call timing: Call this method after the `playEffect:filePath:loopCount:pitch:pan:gain:`,
 * `playEffect:filePath:loopCount:pitch:pan:gain:publish:` or
 * `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopAllEffects NS_SWIFT_NAME(stopAllEffects());

/**
 * @brief Retrieves the duration of the audio effect file.
 *
 * @note Call this method after joining a channel.
 *
 * @param filePath File path:
 * - iOS or macOS: The absolute path or URL address (including the suffixes of the filename) of the
 * audio effect file. For example: `/var/mobile/Containers/Data/audio.mp4`.
 *
 * @return
 * - The total duration (ms) of the specified audio effect file, if the method call succeeds.
 * - < 0: Failure.
 */
- (int)getEffectDuration:(NSString* _Nonnull)filePath NS_SWIFT_NAME(getEffectDuration(_:));

/**
 * @brief Sets the playback position of an audio effect file.
 *
 * @details
 * After a successful setting, the local audio effect file starts playing at the specified position.
 *
 * @note Call this method after `playEffect`.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.
 * @param pos The playback position (ms) of the audio effect file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setEffectPosition:(int)soundId pos:(NSInteger)pos  NS_SWIFT_NAME(setEffectPosition(_:pos:));

/**
 * @brief Retrieves the playback position of the audio effect file.
 *
 * @note Call this method after `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:`.
 *
 * @param soundId The audio effect ID. The ID of each audio effect file is unique.
 *
 * @return
 * - The playback position (ms) of the specified audio effect file, if the method call succeeds.
 * - < 0: Failure.
 */
- (int)getEffectCurrentPosition:(int)soundId NS_SWIFT_NAME(getEffectCurrentPosition(_:));

/**
 * @brief Releases a specified preloaded audio effect from the memory.
 *
 * @details
 * After loading the audio effect file into memory using `preloadEffect:filePath:`, if you need to
 * release the audio effect file, call this method.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @param soundId The ID of the audio effect. Each audio effect has a unique ID.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)unloadEffect:(int)soundId NS_SWIFT_NAME(unloadEffect(_:));

/** Release all preloaded audio effects from the memory.

 *  @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)unloadAllEffects NS_SWIFT_NAME(unloadAllEffects());

#pragma mark beautifier effect
/**
 * @brief Sets a preset voice beautifier effect.
 *
 * @details
 * Call this method to set a preset voice beautifier effect for the local user who sends an audio
 * stream. After setting a voice beautifier effect, all users in the channel can hear the effect.
 * You can set different voice beautifier effects for different scenarios.
 * Call timing: This method can be called either before or after joining the channel.
 * To achieve better vocal effects, it is recommended that you call the following APIs before
 * calling this method:
 * - Call `setAudioScenario:` to set the audio scenario to high-quality audio scenario, namely
 * `AgoraAudioScenarioGameStreaming` (3).
 * - Call `setAudioProfile:` to set the `profile` parameter to `AgoraAudioProfileMusicHighQuality`
 * (4) or `AgoraAudioProfileMusicHighQualityStereo` (5).
 *
 * @note
 * - Do not set the `profile` parameter in `setAudioProfile:` to `AgoraAudioProfileSpeechStandard`
 * (1), or the method does not take effect.
 * - This method has the best effect on human voice processing, and Agora does not recommend calling
 * this method to process audio data containing music.
 * - After calling `setVoiceBeautifierPreset:`, Agora does not recommend calling the following
 * methods, otherwise the effect set by `setVoiceBeautifierPreset:` will be overwritten:
 *   - `setAudioEffectPreset:`
 *   - `setAudioEffectParameters:param1:param2:`
 *   - `setLocalVoicePitch:`
 *   - `setLocalVoiceEqualizationOfBandFrequency:withGain:`
 *   - `setLocalVoiceReverbOfType:withValue:`
 *   - `setVoiceBeautifierParameters:param1:param2:`
 *   - `setVoiceConversionPreset:`
 * - This method relies on the voice beautifier dynamic library
 * `AgoraAudioBeautyExtension.xcframework` (iOS) or `AgoraAudioBeautyExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot be enabled normally.
 *
 * @param preset The preset voice beautifier effect options: `AgoraVoiceBeautifierPreset`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVoiceBeautifierPreset:(AgoraVoiceBeautifierPreset)preset NS_SWIFT_NAME(setVoiceBeautifierPreset(_:));

/**
 * @brief Sets an SDK preset audio effect.
 *
 * @details
 * Call this method to set an SDK preset audio effect for the local user who sends an audio stream.
 * This audio effect does not change the gender characteristics of the original voice. After setting
 * an audio effect, all users in the channel can hear the effect.
 * Call timing: This method can be called either before or after joining the channel.
 * To achieve better vocal effects, it is recommended that you call the following APIs before
 * calling this method:
 * - Call `setAudioScenario:` to set the audio scenario to high-quality audio scenario, namely
 * `AgoraAudioScenarioGameStreaming` (3).
 * - Call `setAudioProfile:` to set the `profile` parameter to `AgoraAudioProfileMusicHighQuality`
 * (4) or `AgoraAudioProfileMusicHighQualityStereo` (5).
 *
 * @note
 * - Do not set the `profile` parameter in `setAudioProfile:` to `AgoraAudioProfileSpeechStandard`
 * (1), or the method does not take effect.
 * - If you call `setAudioEffectPreset:` and set enumerators except for
 * `AgoraAudioEffectPresetRoomAcous3DVoice` or `AgoraAudioEffectPresetPitchCorrection`, do not call
 * `setAudioEffectParameters:param1:param2:`; otherwise, `setAudioEffectPreset:` is overridden.
 * - After calling `setAudioEffectPreset:`, Agora does not recommend you to call the following
 * methods, otherwise the effect set by `setAudioEffectPreset:` will be overwritten:
 *   - `setVoiceBeautifierPreset:`
 *   - `setLocalVoicePitch:`
 *   - `setLocalVoiceEqualizationOfBandFrequency:withGain:`
 *   - `setLocalVoiceReverbOfType:withValue:`
 *   - `setVoiceBeautifierParameters:param1:param2:`
 *   - `setVoiceConversionPreset:`
 * - This method relies on the voice beautifier dynamic library
 * `AgoraAudioBeautyExtension.xcframework` (iOS) or `AgoraAudioBeautyExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot be enabled normally.
 *
 * @param preset The options for SDK preset audio effects. See `AgoraAudioEffectPreset`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioEffectPreset:(AgoraAudioEffectPreset)preset NS_SWIFT_NAME(setAudioEffectPreset(_:));

/**
 * @brief Sets a preset voice beautifier effect.
 *
 * @details
 * Call this method to set a preset voice changing effect for the local user who publishes an audio
 * stream in a channel. After setting the voice changing effect, all users in the channel can hear
 * the effect. You can set different voice changing effects for the user depending on different
 * scenarios.
 * Call timing: This method can be called either before or after joining the channel.
 * To achieve better vocal effects, it is recommended that you call the following APIs before
 * calling this method:
 * - Call `setAudioScenario:` to set the audio scenario to high-quality audio scenario, namely
 * `AgoraAudioScenarioGameStreaming` (3).
 * - Call `setAudioProfile:` to set the `profile` parameter to `AgoraAudioProfileMusicHighQuality`
 * (4) or `AgoraAudioProfileMusicHighQualityStereo` (5).
 *
 * @note
 * - Do not set the `profile` parameter in `setAudioProfile:` to `AgoraAudioProfileSpeechStandard`
 * (1), or the method does not take effect.
 * - This method has the best effect on human voice processing, and Agora does not recommend calling
 * this method to process audio data containing music.
 * - After calling `setVoiceConversionPreset:`, Agora does not recommend you to call the following
 * methods, otherwise the effect set by `setVoiceConversionPreset:` will be overwritten:
 *   - `setAudioEffectPreset:`
 *   - `setAudioEffectParameters:param1:param2:`
 *   - `setVoiceBeautifierPreset:`
 *   - `setVoiceBeautifierParameters:param1:param2:`
 *   - `setLocalVoicePitch:`
 *   - `setLocalVoiceFormant:`
 *   - `setLocalVoiceEqualizationOfBandFrequency:withGain:`
 *   - `setLocalVoiceReverbOfType:withValue:`
 * - This method relies on the voice beautifier dynamic library
 * `AgoraAudioBeautyExtension.xcframework` (iOS) or `AgoraAudioBeautyExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot be enabled normally.
 *
 * @param preset The options for the preset voice beautifier effects: `AgoraVoiceConversionPreset`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVoiceConversionPreset:(AgoraVoiceConversionPreset)preset NS_SWIFT_NAME(setVoiceConversionPreset(_:));

/**
 * @brief Sets parameters for SDK preset audio effects.
 *
 * @details
 * Call this method to set the following parameters for the local user who sends an audio stream:
 * - 3D voice effect: Sets the cycle period of the 3D voice effect.
 * - Pitch correction effect: Sets the basic mode and tonic pitch of the pitch correction effect.
 * Different songs have different modes and tonic pitches. Agora recommends bounding this method
 * with interface elements to enable users to adjust the pitch correction interactively.
 * After setting the audio parameters, all users in the channel can hear the effect.
 * To achieve better vocal effects, it is recommended that you call the following APIs before
 * calling this method:
 * - Call `setAudioScenario:` to set the audio scenario to high-quality audio scenario, namely
 * `AgoraAudioScenarioGameStreaming` (3).
 * - Call `setAudioProfile:` to set the `profile` parameter to `AgoraAudioProfileMusicHighQuality`
 * (4) or `AgoraAudioProfileMusicHighQualityStereo` (5).
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - Do not set the `profile` parameter in `setAudioProfile:` to `AgoraAudioProfileSpeechStandard`
 * (1), or the method does not take effect.
 * - This method has the best effect on human voice processing, and Agora does not recommend calling
 * this method to process audio data containing music.
 * - After calling `setAudioEffectParameters:param1:param2:`, Agora does not recommend you to call
 * the following methods, otherwise the effect set by `setAudioEffectParameters:param1:param2:` will
 * be overwritten:
 *   - `setAudioEffectPreset:`
 *   - `setVoiceBeautifierPreset:`
 *   - `setLocalVoicePitch:`
 *   - `setLocalVoiceEqualizationOfBandFrequency:withGain:`
 *   - `setLocalVoiceReverbOfType:withValue:`
 *   - `setVoiceBeautifierParameters:param1:param2:`
 *   - `setVoiceConversionPreset:`
 * - This method relies on the voice beautifier dynamic library,
 * `AgoraAudioBeautyExtension.xcframework` (iOS) or `AgoraAudioBeautyExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot be enabled normally.
 *
 * @param preset The options for SDK preset audio effects:
 * - `AgoraAudioEffectPresetRoomAcous3DVoice`, 3D voice effect:
 *   - You need to set the `profile` parameter in `setAudioProfile:` to
 * `AgoraAudioProfileMusicStandardStereo` (3) or `AgoraAudioProfileMusicHighQualityStereo` (5)
 * before setting this enumerator; otherwise, the enumerator setting does not take effect.
 *   - If the 3D voice effect is enabled, users need to use stereo audio playback devices to hear
 * the anticipated voice effect.
 * - `AgoraAudioEffectPresetPitchCorrection`, Pitch correction effect:
 * @param param1 - If you set `preset` to `AgoraAudioEffectPresetRoomAcous3DVoice`, `param1` sets
 * the cycle period of the 3D voice effect. The value range is [1,60] and the unit is seconds. The
 * default value is 10, indicating that the voice moves around you every 10 seconds.
 * - If you set `preset` to `AgoraAudioEffectPresetPitchCorrection`, `param1` indicates the basic
 * mode of the pitch correction effect:
 *   - `1`: (Default) Natural major scale.
 *   - `2`: Natural minor scale.
 *   - `3`: Japanese pentatonic scale.
 * @param param2 - If you set `preset` to `AgoraAudioEffectPresetRoomAcous3DVoice` , you need to set
 * `param2` to `0`.
 * - If you set `preset` to `AgoraAudioEffectPresetPitchCorrection`, `param2` indicates the tonic
 * pitch of the pitch correction effect:
 *   - `1`: A
 *   - `2`: A#
 *   - `3`: B
 *   - `4`: (Default) C
 *   - `5`: C#
 *   - `6`: D
 *   - `7`: D#
 *   - `8`: E
 *   - `9`: F
 *   - `10`: F#
 *   - `11`: G
 *   - `12`: G#
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioEffectParameters:(AgoraAudioEffectPreset)preset param1:(int)param1 param2:(int)param2 NS_SWIFT_NAME(setAudioEffectParameters(_:param1:param2:));

/**
 * @brief Sets parameters for the preset voice beautifier effects.
 *
 * @details
 * Call this method to set a gender characteristic and a reverberation effect for the singing
 * beautifier effect. This method sets parameters for the local user who sends an audio stream.
 * After setting the audio parameters, all users in the channel can hear the effect.
 * To achieve better vocal effects, it is recommended that you call the following APIs before
 * calling this method:
 * - Call `setAudioScenario:` to set the audio scenario to high-quality audio scenario, namely
 * `AgoraAudioScenarioGameStreaming` (3).
 * - Call `setAudioProfile:` to set the `profile` parameter to `AgoraAudioProfileMusicHighQuality`
 * (4) or `AgoraAudioProfileMusicHighQualityStereo` (5).
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - Do not set the `profile` parameter in `setAudioProfile:` to `AgoraAudioProfileSpeechStandard`
 * (1), or the method does not take effect.
 * - This method has the best effect on human voice processing, and Agora does not recommend calling
 * this method to process audio data containing music.
 * - After calling `setVoiceBeautifierParameters:param1:param2:`, Agora does not recommend calling
 * the following methods, otherwise the effect set by `setVoiceBeautifierParameters:param1:param2:`
 * will be overwritten:
 *   - `setAudioEffectPreset:`
 *   - `setAudioEffectParameters:param1:param2:`
 *   - `setVoiceBeautifierPreset:`
 *   - `setLocalVoicePitch:`
 *   - `setLocalVoiceEqualizationOfBandFrequency:withGain:`
 *   - `setLocalVoiceReverbOfType:withValue:`
 *   - `setVoiceConversionPreset:`
 * - This method relies on the voice beautifier dynamic library
 * `AgoraAudioBeautyExtension.xcframework` (iOS) or `AgoraAudioBeautyExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot be enabled normally.
 *
 * @param preset The option for the preset audio effect:
 * - `SINGING_BEAUTIFIER`: The singing beautifier effect.
 * @param param1 The gender characteristics options for the singing voice:
 * - `1`: A male-sounding voice.
 * - `2`: A female-sounding voice.
 * @param param2 The reverberation effect options for the singing voice:
 * - `1`: The reverberation effect sounds like singing in a small room.
 * - `2`: The reverberation effect sounds like singing in a large room.
 * - `3`: The reverberation effect sounds like singing in a hall.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVoiceBeautifierParameters:(AgoraVoiceBeautifierPreset)preset param1:(int)param1 param2:(int)param2 NS_SWIFT_NAME(setVoiceBeautifierParameters(_:param1:param2:));

/** Set parameters for SDK preset voice conversion.
 *
 * @note
 * - reserved interface
 *
 * @param preset The options for SDK preset audio effects. See #VOICE_CONVERSION_PRESET.
 * @param param1 reserved.
 * @param param2 reserved.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVoiceConversionParameters:(AgoraVoiceConversionPreset)preset param1:(int)param1 param2:(int)param2 NS_SWIFT_NAME(setVoiceConversionParameters(_:param1:param2:));

/**
 * @brief Sets the preset headphone equalization effect.
 *
 * @details
 * This method is mainly used in spatial audio effect scenarios. You can select the preset headphone
 * equalizer to listen to the audio to achieve the expected audio experience.
 *
 * @note If the headphones you use already have a good equalization effect, you may not get a
 * significant improvement when you call this method, and could even diminish the experience.
 *
 * @param preset The preset headphone equalization effect. See `AgoraHeadphoneEQPreset`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 */
- (int)setHeadphoneEQPreset:(AgoraHeadphoneEQPreset)preset NS_SWIFT_NAME(setHeadphoneEQPreset(_:));

/**
 * @brief Sets the low- and high-frequency parameters of the headphone equalizer.
 *
 * @details
 * In a spatial audio effect scenario, if the preset headphone equalization effect is not achieved
 * after calling the `setHeadphoneEQPreset:` method, you can further adjust the headphone
 * equalization effect by calling this method.
 *
 * @param lowGain The low-frequency parameters of the headphone equalizer. The value range is
 * [-10,10]. The larger the value, the deeper the sound.
 * @param highGain The high-frequency parameters of the headphone equalizer. The value range is
 * [-10,10]. The larger the value, the sharper the sound.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 */
- (int)setHeadphoneEQParameters:(int)lowGain highGain:(int)highGain NS_SWIFT_NAME(setHeadphoneEQParameters(_:highGain:));

/**
 * @brief Enables or disables the voice AI tuner.
 *
 * @details
 * The voice AI tuner supports enhancing sound quality and adjusting tone style.
 * Applicable scenarios: Social entertainment scenes including online KTV, online podcast and live
 * streaming in showrooms, where high sound quality is required.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param enabled Whether to enable the voice AI tuner:
 * - `YES`: Enables the voice AI tuner.
 * - `NO`: (Default) Disable the voice AI tuner.
 * @param type Voice AI tuner sound types, see `AgoraVoiceAITunerType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableVoiceAITuner:(BOOL)enabled type:(AgoraVoiceAITunerType)type NS_SWIFT_NAME(enableVoiceAITuner(_:type:));

#pragma mark Sound Position Indication
/**
 * @brief Enables or disables stereo panning for remote users.
 *
 * @details
 * Ensure that you call this method before joining a channel to enable stereo panning for remote
 * users so that the local user can track the position of a remote user by calling
 * `setRemoteVoicePosition:pan:gain:`.
 *
 * @param enabled Whether to enable stereo panning for remote users:
 * - `YES`: Enable stereo panning.
 * - `NO`: Disable stereo panning.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableSoundPositionIndication:(BOOL)enabled NS_SWIFT_NAME(enableSoundPositionIndication(_:));

/**
 * @brief Sets the 2D position (the position on the horizontal plane) of the remote user's voice.
 *
 * @details
 * This method sets the 2D position and volume of a remote user, so that the local user can easily
 * hear and identify the remote user's position.
 * When the local user calls this method to set the voice position of a remote user, the voice
 * difference between the left and right channels allows the local user to track the real-time
 * position of the remote user, creating a sense of space. This method applies to massive
 * multiplayer online games, such as Battle Royale games.
 *
 * @note
 * - For this method to work, enable stereo panning for remote users by calling the
 * `enableSoundPositionIndication:` method before joining a channel.
 * - For the best voice positioning, Agora recommends using a wired headset.
 * - Call this method after joining a channel.
 *
 * @param uid The user ID of the remote user.
 * @param pan The voice position of the remote user. The value ranges from -1.0 to 1.0:
 * - 0.0: (Default) The remote voice comes from the front.
 * - -1.0: The remote voice comes from the left.
 * - 1.0: The remote voice comes from the right.
 * @param gain The volume of the remote user. The value ranges from 0.0 to 100.0. The default value
 * is 100.0 (the original volume of the remote user). The smaller the value, the lower the volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteVoicePosition:(NSUInteger)uid pan:(double)pan gain:(double)gain NS_SWIFT_NAME(setRemoteVoicePosition(_:pan:gain:));

/**
 * @brief Enables or disables the spatial audio effect.
 *
 * @details
 * After enabling the spatial audio effect, you can call `setRemoteUserSpatialAudioParams:params:`
 * to set the spatial audio effect parameters of the remote user.
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - This method relies on the spatial audio dynamic library
 * `AgoraSpatialAudioExtension.xcframework` (iOS) or `AgoraSpatialAudioExtension.framework` (macOS).
 * If the dynamic library is deleted, the function cannot
 * be enabled normally.
 *
 * @param enabled Whether to enable the spatial audio effect:
 * - `YES`: Enable the spatial audio effect.
 * - `NO`: Disable the spatial audio effect.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableSpatialAudio:(BOOL)enabled NS_SWIFT_NAME(enableSpatialAudio(_:));

/**
 * @brief Sets the spatial audio effect parameters of the remote user.
 *
 * @details
 * Call this method after `enableSpatialAudio:`. After successfully setting the spatial audio effect
 * parameters of the remote user, the local user can hear the remote user with a sense of space.
 *
 * @param uid The user ID. This parameter must be the same as the user ID passed in when the user
 * joined the channel.
 * @param params The spatial audio parameters. See `AgoraSpatialAudioParams`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteUserSpatialAudioParams:(NSUInteger)uid
                                  params:(AgoraSpatialAudioParams* _Nonnull)params NS_SWIFT_NAME(setRemoteUserSpatialAudioParams(_:params:));

#pragma mark Audio Noise Suppression

/**
 * @brief Sets whether to enable the AI ​​noise suppression function and set the noise suppression
 * mode.
 *
 * @details
 * You can call this method to enable AI noise suppression function. Once enabled, the SDK
 * automatically detects and reduces stationary and non-stationary noise from your audio on the
 * premise of ensuring the quality of human voice. Stationary noise refers to noise signal with
 * constant average statistical properties and negligibly small fluctuations of level within the
 * period of observation. Common sources of stationary noises are:
 * - Television;
 * - Air conditioner;
 * - Machinery, etc.
 * Non-stationary noise refers to noise signal with huge fluctuations of level within the period of
 * observation; common sources of non-stationary noises are:
 * - Thunder;
 * - Explosion;
 * - Cracking, etc.
 * Applicable scenarios: In scenarios such as co-streaming, online education and video meeting, this
 * function can detect and reduce background noises to improve experience.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @note - This method relies on the AI noise suppression dynamic library
 * `AgoraAiNoiseSuppressionExtension.xcframework`. If the dynamic library is deleted, the function
 * cannot be enabled.
 *
 * @param enabled Whether to enable the AI noise suppression function:
 * - `YES`: Enable the AI noise suppression.
 * - `NO`: (Default) Disable the AI noise suppression.
 * @param mode The AI noise suppression modes. See `AUDIO_AINS_MODE`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAINSMode:(BOOL)enabled mode:(AUDIO_AINS_MODE)mode NS_SWIFT_NAME(setAINSMode(_:mode:));

#pragma mark Audio Mixing

/**
 * @brief Starts playing the music file.
 *
 * @details
 * For the audio file formats supported by this method, see `What formats of audio files does the
 * Agora RTC SDK support`. If the local music file does not exist, the SDK does not support the file
 * format, or the the SDK cannot access the music file URL, the SDK reports
 * AgoraAudioMixingReasonCanNotOpen.
 * Call timing: You can call this method either before or after joining a channel.
 * Related callbacks: A successful method call triggers the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` (`AgoraAudioMixingStateTypePlaying`) callback.
 * When the audio mixing file playback finishes, the SDK triggers the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` (`AgoraAudioMixingStateTypeStopped`) callback on
 * the local client.
 *
 * @note
 * - If you call this method to play short sound effect files, you may encounter playback failure.
 * Agora recommends using `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:` instead
 * to play such files.
 * - If you need to call this method multiple times, ensure that the time interval between calling
 * this method is more than 500 ms.
 *
 * @param filePath File path:
 * - iOS or macOS: The absolute path or URL address (including the suffixes of the filename) of the
 * audio effect file. For example: `/var/mobile/Containers/Data/audio.mp4`.
 * @param loopback Whether to only play music files on the local client:
 * - `YES`: Only play music files on the local client so that only the local user can hear the
 * music.
 * - `NO`: Publish music files to remote clients so that both the local user and remote users can
 * hear the music.
 * @param cycle The number of times the music file plays.
 * - > 0: The number of times for playback. For example, 1 represents playing 1 time.
 * - -1: Play the audio file in an infinite loop.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 *   - -3: The SDK is not ready.
 *     - The audio module is disabled.
 *     - The program is not complete.
 *     - The initialization of `AgoraRtcEngineKit` fails. Reinitialize the `AgoraRtcEngineKit`.
 */
- (int)startAudioMixing:(NSString *  _Nonnull)filePath
               loopback:(BOOL)loopback
                  cycle:(NSInteger)cycle NS_SWIFT_NAME(startAudioMixing(_:loopback:cycle:));

/**
 * @brief Starts playing the music file.
 *
 * @details
 * For the audio file formats supported by this method, see `What formats of audio files does the
 * Agora RTC SDK support`. If the local music file does not exist, the SDK does not support the file
 * format, or the the SDK cannot access the music file URL, the SDK reports
 * AgoraAudioMixingReasonCanNotOpen.
 * Call timing: You can call this method either before or after joining a channel.
 * Related callbacks: A successful method call triggers the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` (`AgoraAudioMixingStateTypePlaying`) callback.
 * When the audio mixing file playback finishes, the SDK triggers the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` (`AgoraAudioMixingStateTypeStopped`) callback on
 * the local client.
 *
 * @note
 * - If you call this method to play short sound effect files, you may encounter playback failure.
 * Agora recommends using `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:` instead
 * to play such files.
 * - If you need to call this method multiple times, ensure that the time interval between calling
 * this method is more than 500 ms.
 *
 * @param filePath File path:
 * - iOS or macOS: The absolute path or URL address (including the suffixes of the filename) of the
 * audio effect file. For example: `/var/mobile/Containers/Data/audio.mp4`.
 * @param loopback Whether to only play music files on the local client:
 * - `YES`: Only play music files on the local client so that only the local user can hear the
 * music.
 * - `NO`: Publish music files to remote clients so that both the local user and remote users can
 * hear the music.
 * @param cycle The number of times the music file plays.
 * - > 0: The number of times for playback. For example, 1 represents playing 1 time.
 * - -1: Play the audio file in an infinite loop.
 * @param startPos The playback position (ms) of the music file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 *   - -3: The SDK is not ready.
 *     - The audio module is disabled.
 *     - The program is not complete.
 *     - The initialization of `AgoraRtcEngineKit` fails. Reinitialize the `AgoraRtcEngineKit`.
 */
- (int)startAudioMixing:(NSString *  _Nonnull)filePath
               loopback:(BOOL)loopback
                  cycle:(NSInteger)cycle
               startPos:(NSInteger)startPos NS_SWIFT_NAME(startAudioMixing(_:loopback:cycle:startPos:));

/**
 * @brief Stops playing the music file.
 *
 * @details
 * After calling `startAudioMixing:loopback:cycle:startPos:` to play a music file, you can call this
 * method to stop the playing. If you only need to pause the playback, call `pauseAudioMixing`.
 * Call timing: Call this method after joining a channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopAudioMixing NS_SWIFT_NAME(stopAudioMixing());

/**
 * @brief Pauses playing and mixing the music file.
 *
 * @details
 * After calling `startAudioMixing:loopback:cycle:startPos:` to play a music file, you can call this
 * method to pause the playing. If you need to stop the playback, call `stopAudioMixing`.
 * Call timing: Call this method after joining a channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)pauseAudioMixing NS_SWIFT_NAME(pauseAudioMixing());

/**
 * @brief Resumes playing and mixing the music file.
 *
 * @details
 * After calling `pauseAudioMixing` to pause the playback, you can call this method to resume the
 * playback.
 * Call timing: Call this method after joining a channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)resumeAudioMixing NS_SWIFT_NAME(resumeAudioMixing());

/**
 * @brief Selects the audio track used during playback.
 *
 * @details
 * After getting the track index of the audio file, you can call this method to specify any track to
 * play. For example, if different tracks of a multi-track file store songs in different languages,
 * you can call this method to set the playback language.
 *
 * @note
 * - For the supported formats of audio files, see
 * `https://docs.agora.io/en/help/general-product-inquiry/audio_format#extended-audio-file-formats`.
 * - You need to call this method after calling `startAudioMixing:loopback:cycle:startPos:` and
 * receiving the `rtcEngine:audioMixingStateChanged:reasonCode:`
 * `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @param index The audio track you want to specify. The value should be greater than 0 and less
 * than that of returned by `getAudioTrackCount`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)selectAudioTrack:(NSInteger)index NS_SWIFT_NAME(selectAudioTrack(_:));

/**
 * @brief Gets the index of audio tracks of the current music file.
 *
 * @note - You need to call this method after calling `startAudioMixing:loopback:cycle:startPos:`
 * and receiving the `rtcEngine:audioMixingStateChanged:reasonCode:`
 * `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @return
 * - The SDK returns the index of the audio tracks if the method call succeeds.
 * - < 0: Failure.
 */
- (int)getAudioTrackCount NS_SWIFT_NAME(getAudioTrackCount());

/**
 * @brief Adjusts the volume during audio mixing.
 *
 * @details
 * This method adjusts the audio mixing volume on both the local client and remote clients.
 * Call timing: Call this method after `startAudioMixing:loopback:cycle:startPos:`.
 *
 * @note This method does not affect the volume of the audio file set in the
 * `playEffect:filePath:loopCount:pitch:pan:gain:publish:startPos:` method.
 *
 * @param volume Audio mixing volume. The value ranges between 0 and 100. The default value is 100,
 * which means the original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustAudioMixingVolume:(NSInteger)volume NS_SWIFT_NAME(adjustAudioMixingVolume(_:));

/**
 * @brief Adjusts the volume of audio mixing for publishing.
 *
 * @details
 * This method adjusts the volume of audio mixing for publishing (sending to other users).
 * Call timing: Call this method after calling `startAudioMixing:loopback:cycle:startPos:` and
 * receiving the `rtcEngine:audioMixingStateChanged:reasonCode:`
 * `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @param volume The volume of audio mixing for local playback. The value ranges between 0 and 100
 * (default). 100 represents the original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustAudioMixingPublishVolume:(NSInteger)volume NS_SWIFT_NAME(adjustAudioMixingPublishVolume(_:));

/**
 * @brief Retrieves the audio mixing volume for publishing.
 *
 * @details
 * This method helps troubleshoot audio volume‑related issues.
 *
 * @note You need to call this method after calling `startAudioMixing:loopback:cycle:startPos:` and
 * receiving the `rtcEngine:audioMixingStateChanged:reasonCode:`
 * `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @return
 * - ≥ 0: The audio mixing volume, if this method call succeeds. The value range is [0,100].
 * - < 0: Failure.
 */
- (int)getAudioMixingPublishVolume NS_SWIFT_NAME(getAudioMixingPublishVolume());

/**
 * @brief Adjusts the volume of audio mixing for local playback.
 *
 * @details
 * Call timing: You need to call this method after calling
 * `startAudioMixing:loopback:cycle:startPos:` and receiving the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @param volume The volume of audio mixing for local playback. The value ranges between 0 and 100
 * (default). 100 represents the original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustAudioMixingPlayoutVolume:(NSInteger)volume NS_SWIFT_NAME(adjustAudioMixingPlayoutVolume(_:));

/**
 * @brief Retrieves the audio mixing volume for local playback.
 *
 * @details
 * You can call this method to get the local playback volume of the mixed audio file, which helps in
 * troubleshooting volume‑related issues.
 * Call timing: Call this method after `startAudioMixing:loopback:cycle:startPos:` and receiving the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @return
 * - ≥ 0: The audio mixing volume, if this method call succeeds. The value range is [0,100].
 * - < 0: Failure.
 */
- (int)getAudioMixingPlayoutVolume NS_SWIFT_NAME(getAudioMixingPlayoutVolume());

/**
 * @brief Retrieves the duration (ms) of the music file.
 *
 * @details
 * Retrieves the total duration (ms) of the audio.
 * Call timing: Call this method after `startAudioMixing:loopback:cycle:startPos:` and receiving the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @return
 * - ≥ 0: The audio mixing duration, if this method call succeeds.
 * - < 0: Failure.
 */
- (int)getAudioMixingDuration NS_SWIFT_NAME(getAudioMixingDuration());

/**
 * @brief Retrieves the playback position (ms) of the music file.
 *
 * @details
 * Retrieves the playback position (ms) of the audio.
 *
 * @note
 * - You need to call this method after calling `startAudioMixing:loopback:cycle:startPos:` and
 * receiving the `rtcEngine:audioMixingStateChanged:reasonCode:`
 * `(AgoraAudioMixingStateTypePlaying)` callback.
 * - If you need to call `getAudioMixingCurrentPosition` multiple times, ensure that the time
 * interval between calling this method is more than 500 ms.
 *
 * @return
 * - ≥ 0: The current playback position (ms) of the audio mixing, if this method call succeeds. 0
 * represents that the current music file does not start playing.
 * - < 0: Failure.
 */
- (int)getAudioMixingCurrentPosition NS_SWIFT_NAME(getAudioMixingCurrentPosition());

/**
 * @brief Sets the audio mixing position.
 *
 * @details
 * Call this method to set the playback position of the music file to a different starting position
 * (the default plays from the beginning).
 * Call timing: Call this method after `startAudioMixing:loopback:cycle:startPos:` and receiving the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @param pos Integer. The playback position (ms).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioMixingPosition:(NSInteger)pos NS_SWIFT_NAME(setAudioMixingPosition(_:));

/**
 * @brief Sets the channel mode of the current audio file.
 *
 * @details
 * In a stereo music file, the left and right channels can store different audio data. According to
 * your needs, you can set the channel mode to original mode, left channel mode, right channel mode,
 * or mixed channel mode.
 * Applicable scenarios: For example, in the KTV scenario, the left channel of the music file stores
 * the musical accompaniment, and the right channel stores the original singer's vocals. You can set
 * according to actual needs:
 * - If you only want to hear the accompaniment, use this method to set the audio file's channel
 * mode to left channel mode.
 * - If you need to hear both the accompaniment and the original vocals simultaneously, call this
 * method to set the channel mode to mixed mode.
 * Call timing: Call this method after `startAudioMixing:loopback:cycle:startPos:` and receiving the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @note This method only applies to stereo audio files.
 *
 * @param mode The channel mode. See `AgoraAudioMixingDualMonoMode`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioMixingDualMonoMode:(AgoraAudioMixingDualMonoMode)mode NS_SWIFT_NAME(setAudioMixingDualMonoMode(_:));

/**
 * @brief Sets the pitch of the local music file.
 *
 * @details
 * When a local music file is mixed with a local human voice, call this method to set the pitch of
 * the local music file only.
 * Call timing: You need to call this method after calling
 * `startAudioMixing:loopback:cycle:startPos:` and receiving the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` `(AgoraAudioMixingStateTypePlaying)` callback.
 *
 * @param pitch Sets the pitch of the local music file by the chromatic scale. The default value is
 * 0, which means keeping the original pitch. The value ranges from -12 to 12, and the pitch value
 * between consecutive values is a chromatic value. The greater the absolute value of this
 * parameter, the higher or lower the pitch of the local music file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioMixingPitch:(NSInteger)pitch NS_SWIFT_NAME(setAudioMixingPitch(_:));

/**
 * @brief Sets the playback speed of the current audio file.
 *
 * @details
 * Ensure you call this method after calling `startAudioMixing:loopback:cycle:startPos:` receiving
 * the `rtcEngine:audioMixingStateChanged:reasonCode:` callback reporting the state as
 * `AgoraAudioMixingStateTypePlaying`.
 *
 * @param speed The playback speed. Agora recommends that you set this to a value between 50 and
 * 400, defined as follows:
 * - 50: Half the original speed.
 * - 100: The original speed.
 * - 400: 4 times the original speed.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioMixingPlaybackSpeed:(NSInteger)speed NS_SWIFT_NAME(setAudioMixingPlaybackSpeed(_:));

#pragma mark Audio Recording

/**
 * @brief Starts client-side recording.
 *
 * @details
 * The SDK supports recording on the client side during a call. This method records the audio of all
 * users in the channel and generates a recording file that contains the voices of all users. The
 * recording file only supports the following formats:
 * - `.wav`: Large file size with high audio fidelity.
 * - `.aac`: Small file size with some loss in audio fidelity.
 * 
 * Make sure the specified directory exists and is writable. You must call this method after
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`. If you call
 * `leaveChannel:leaveChannelBlock:` while recording is still in progress, the recording
 * automatically stops.
 *
 * @note When you call this method, the default recording sample rate is 32 kHz and cannot be
 * modified.
 *
 * @param filePath The absolute local path where the recording file is saved. The path must include
 * the file name and extension. For example: `/var/mobile/Containers/Data/audio.aac`.
 *                 Note:
 *                 Make sure the specified path exists and is writable.
 * @param quality The recording quality. See `AgoraAudioRecordingQuality` for details.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting.
 */
- (int)startAudioRecording:(NSString * _Nonnull)filePath
                   quality:(AgoraAudioRecordingQuality)quality NS_SWIFT_NAME(startAudioRecording(_:quality:));

/**
 * @brief Starts client-side recording and configures recording settings.
 *
 * @details
 * The SDK supports client-side recording during a call. After calling this method, you can record
 * the audio of users in the channel and generate a recording file. The recording file supports the
 * following formats only:
 * - WAV: Higher audio fidelity with larger file size. For example, a 10-minute recording at a
 * sample rate of 32000 Hz results in a file of approximately 73 MB.
 * - AAC: Lower audio fidelity with smaller file size. For example, a 10-minute recording at a
 * sample rate of 32000 Hz and a recording quality of `AgoraAudioRecordingQualityMedium` results in
 * a file of approximately 2 MB.
 * Recording automatically stops when the user leaves the channel.
 * Call timing: This method must be called after joining the channel.
 *
 * @param config Recording configuration. See `AgoraAudioRecordingConfiguration` for details.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting.
 */
- (int)startAudioRecordingWithConfig:(AgoraAudioRecordingConfiguration * _Nonnull)config NS_SWIFT_NAME(startAudioRecording(withConfig:));

/**
 * @brief Registers an encoded audio observer.
 *
 * @note
 * - Call this method after joining a channel.
 * - You can call this method or `startAudioRecordingWithConfig:` to set the recording type and
 * quality of audio files, but Agora does not recommend using this method and
 * `startAudioRecordingWithConfig:` at the same time. Only the method called later will take effect.
 *
 * @param config Observer settings for the encoded audio. See
 * `AgoraAudioEncodedFrameDelegateConfig`.
 * @param delegate The encoded audio observer. See `AgoraAudioEncodedFrameDelegate`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioEncodedFrameDelegate:(id<AgoraAudioEncodedFrameDelegate> _Nullable)delegate config:(AgoraAudioEncodedFrameDelegateConfig * _Nonnull)config NS_SWIFT_NAME(setAudioEncodedFrameDelegate(_:config:));

/**
 * @brief Stops client-side recording.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting suggestions.
 */
- (int)stopAudioRecording NS_SWIFT_NAME(stopAudioRecording());

#pragma mark Echo Test
/**
 * @brief Starts an audio device loopback test.
 *
 * @details
 * To test whether the user's local sending and receiving streams are normal, you can call this
 * method to perform an audio and video call loop test, which tests whether the audio and video
 * devices and the user's upstream and downstream networks are working properly.
 * After starting the test, the user needs to make a sound or face the camera. The audio or video is
 * output after about two seconds. If the audio playback is normal, the audio device and the user's
 * upstream and downstream networks are working properly; if the video playback is normal, the video
 * device and the user's upstream and downstream networks are working properly.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @note
 * - When calling in a channel, make sure that no audio or video stream is being published.
 * - After calling this method, call `stopEchoTest` to end the test; otherwise, the user cannot
 * perform the next audio and video call loop test and cannot join the channel.
 * - In live streaming scenarios, this method only applies to hosts.
 *
 * @param config The configuration of the audio and video call loop test. See
 * `AgoraEchoTestConfiguration`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */

- (int)startEchoTestWithConfig:(AgoraEchoTestConfiguration* _Nonnull)config NS_SWIFT_NAME(startEchoTest(withConfig:));

/**
 * @brief Stops the audio call test.
 *
 * @details
 * After calling `startEchoTestWithConfig:`, you must call this method to end the test; otherwise,
 * the user cannot perform the next audio and video call loop test and cannot join the channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -5(ERR_REFUSED): Failed to stop the echo test. The echo test may not be running.
 */
- (int)stopEchoTest NS_SWIFT_NAME(stopEchoTest());


#pragma mark Miscellaneous Audio Control
/**
 * @brief Enables in-ear monitoring.
 *
 * @details
 * This method enables or disables in-ear monitoring.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @note Users must use earphones (wired or Bluetooth) to hear the in-ear monitoring effect.
 *
 * @param enabled Enables or disables in-ear monitoring.
 * - `YES`: Enables in-ear monitoring.
 * - `NO`: (Default) Disables in-ear monitoring.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - - 8: Make sure the current audio routing is Bluetooth or headset.
 */
- (int)enableInEarMonitoring:(BOOL)enabled NS_SWIFT_NAME(enable(inEarMonitoring:));

/**
 * @brief Enables in-ear monitoring.
 *
 * @details
 * This method enables or disables in-ear monitoring.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @note Users must use earphones (wired or Bluetooth) to hear the in-ear monitoring effect.
 *
 * @param enabled Enables or disables in-ear monitoring.
 * - `YES`: Enables in-ear monitoring.
 * - `NO`: (Default) Disables in-ear monitoring.
 * @param includeAudioFilters The audio filter types of in-ear monitoring. See
 * `AgoraEarMonitoringFilterType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - - 8: Make sure the current audio routing is Bluetooth or headset.
 */
- (int)enableInEarMonitoring:(BOOL)enabled includeAudioFilters:(AgoraEarMonitoringFilterType)includeAudioFilters NS_SWIFT_NAME(enable(inEarMonitoring:includeAudioFilters:));

/**
 * @brief Sets the volume of the in-ear monitor.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param volume The volume of the user. The value range is [0,400].
 * - 0: Mute.
 * - 100: (Default) The original volume.
 * - 400: Four times the original volume (amplifying the audio signals by four times).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: Invalid parameter settings, such as in-ear monitoring volume exceeding the valid range (<
 * 0 or > 400).
 */
- (int)setInEarMonitoringVolume:(NSInteger)volume NS_SWIFT_NAME(setInEarMonitoringVolume(_:));

#if TARGET_OS_IPHONE
/**
 * @brief Sets the operational permission of the SDK on the audio session.
 *
 * @details
 * The SDK and the app can both configure the audio session by default. If you need to only use the
 * app to configure the audio session, this method restricts the operational permission of the SDK
 * on the audio session.
 * You can call this method either before or after joining a channel. Once you call this method to
 * restrict the operational permission of the SDK on the audio session, the restriction takes effect
 * when the SDK needs to change the audio session.
 *
 * @note - This method does not restrict the operational permission of the app on the audio session.
 *
 * @param restriction The operational permission of the SDK on the audio session. See
 * `AgoraAudioSessionOperationRestriction`. This parameter is in bit mask format, and each bit
 * corresponds to a permission.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (void)setAudioSessionOperationRestriction:(AgoraAudioSessionOperationRestriction)restriction NS_SWIFT_NAME(setAudioSessionOperationRestriction(_:));
#endif

#pragma mark Dual Video Mode
/**
 * @brief Enables or disables dual-stream mode on the sender side.
 *
 * @deprecated v4.2.0. This method is deprecated. Use setDualStreamMode instead.
 *
 * @details
 * Dual streams are a pairing of a high-quality video stream and a low-quality video stream:
 * - High-quality video stream: High bitrate, high resolution.
 * - Low-quality video stream: Low bitrate, low resolution.
 * After you enable dual-stream mode, you can call `setRemoteVideoStream:type:` to choose to receive
 * either the high-quality video stream or the low-quality video stream on the subscriber side.
 *
 * @note
 * - This method is applicable to all types of streams from the sender, including but not limited to
 * video streams collected from cameras, screen sharing streams, and custom-collected video streams.
 * - If you need to enable dual video streams in a multi-channel scenario, you can call the
 * `enableDualStreamModeEx:streamConfig:connection:` method.
 * - You can call this method either before or after joining a channel.
 *
 * @param enabled Whether to enable dual-stream mode:
 * - `YES`: Enable dual-stream mode.
 * - `NO`: (Default) Disable dual-stream mode.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableDualStreamMode:(BOOL)enabled NS_SWIFT_NAME(enableDualStreamMode(_:)) __deprecated_msg("use setDualStreamMode: instead.");

/**
 * @brief Sets the dual-stream mode on the sender side and the low-quality video stream.
 *
 * @deprecated v4.2.0. This method is deprecated. Use setDualStreamMode instead.
 *
 * @details
 * You can call this method to enable or disable the dual-stream mode on the publisher side. Dual
 * streams are a pairing of a high-quality video stream and a low-quality video stream:
 * - High-quality video stream: High bitrate, high resolution.
 * - Low-quality video stream: Low bitrate, low resolution.
 * After you enable dual-stream mode, you can call `setRemoteVideoStream:type:` to choose to receive
 * either the high-quality video stream or the low-quality video stream on the subscriber side.
 *
 * @note
 * - This method is applicable to all types of streams from the sender, including but not limited to
 * video streams collected from cameras, screen sharing streams, and custom-collected video streams.
 * - If you need to enable dual video streams in a multi-channel scenario, you can call the
 * `enableDualStreamModeEx:streamConfig:connection:` method.
 * - You can call this method either before or after joining a channel.
 *
 * @param enabled Whether to enable dual-stream mode:
 * - `YES`: Enable dual-stream mode.
 * - `NO`: (Default) Disable dual-stream mode.
 * @param streamConfig The configuration of the low-quality video stream. See
 * `AgoraSimulcastStreamConfig`.Note: When setting `mode` to `AgoraDisableSimulcastStream`, setting
 * `streamConfig` will not take effect.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableDualStreamMode:(BOOL)enabled
               streamConfig:(AgoraSimulcastStreamConfig* _Nonnull)streamConfig NS_SWIFT_NAME(enableDualStreamMode(_:streamConfig:)) __deprecated_msg("use setDualStreamMode: instead.");

/**
 * @brief Sets the dual-stream mode on the sender side.
 *
 * @details
 * The SDK defaults to enabling low-quality video stream adaptive mode ( `AgoraAutoSimulcastStream`
 * ) on the sender side, which means the sender does not actively send low-quality video stream. The
 * receiving end with the role of the **host** can initiate a low-quality video stream request by
 * calling `setRemoteVideoStream:type:`, and upon receiving the request, the sending end
 * automatically starts sending low-quality stream.
 * - If you want to modify this behavior, you can call this method and set `mode` to
 * `AgoraDisableSimulcastStream` (never send low-quality video streams) or
 * `AgoraEnableSimulcastStream` (always send low-quality video streams).
 * - If you want to restore the default behavior after making changes, you can call this method
 * again with `mode` set to `AgoraAutoSimulcastStream`.
 *
 * @note
 * The difference and connection between this method and `enableDualStreamMode:` is as follows:
 * - When calling this method and setting `mode` to `AgoraDisableSimulcastStream`, it has the same
 * effect as `enableDualStreamMode:` `(NO)`.
 * - When calling this method and setting `mode` to `AgoraEnableSimulcastStream`, it has the same
 * effect as `enableDualStreamMode:` `(YES)`.
 * - Both methods can be called before and after joining a channel. If both methods are used, the
 * settings in the method called later takes precedence.
 *
 * @param mode The mode in which the video stream is sent. See `AgoraSimulcastStreamMode`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDualStreamMode:(AgoraSimulcastStreamMode)mode NS_SWIFT_NAME(setDualStreamMode(_:));

/**
 * @brief Sets dual-stream mode configuration on the sender side.
 *
 * @details
 * The SDK defaults to enabling low-quality video stream adaptive mode ( `AgoraAutoSimulcastStream`
 * ) on the sender side, which means the sender does not actively send low-quality video stream. The
 * receiving end with the role of the **host** can initiate a low-quality video stream request by
 * calling `setRemoteVideoStream:type:`, and upon receiving the request, the sending end
 * automatically starts sending low-quality stream.
 * - If you want to modify this behavior, you can call this method and set `mode` to
 * `AgoraDisableSimulcastStream` (never send low-quality video streams) or
 * `AgoraEnableSimulcastStream` (always send low-quality video streams).
 * - If you want to restore the default behavior after making changes, you can call this method
 * again with `mode` set to `AgoraAutoSimulcastStream`.
 * The difference between this method and `setDualStreamMode:` is that this method can also
 * configure the low-quality video stream, and the SDK sends the stream according to the
 * configuration in `streamConfig`.
 *
 * @note
 * The difference and connection between this method and `enableDualStreamMode:streamConfig:` is as
 * follows:
 * - When calling this method and setting `mode` to `AgoraDisableSimulcastStream`, it has the same
 * effect as calling `enableDualStreamMode:streamConfig:` and setting `enabled` to `NO`.
 * - When calling this method and setting `mode` to `AgoraEnableSimulcastStream`, it has the same
 * effect as calling `enableDualStreamMode:streamConfig:` and setting `enabled` to `YES`.
 * - Both methods can be called before and after joining a channel. If both methods are used, the
 * settings in the method called later takes precedence.
 *
 * @param mode The mode in which the video stream is sent. See `AgoraSimulcastStreamMode`.
 * @param streamConfig The configuration of the low-quality video stream. See
 * `AgoraSimulcastStreamConfig`.Note: When setting `mode` to `AgoraDisableSimulcastStream`, setting
 * `streamConfig` will not take effect.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDualStreamMode:(AgoraSimulcastStreamMode)mode
            streamConfig:(AgoraSimulcastStreamConfig* _Nonnull)streamConfig NS_SWIFT_NAME(setDualStreamMode(_:streamConfig:));

/**
 * @brief Sets the simulcast video stream configuration.
 *
 * @since v4.6.0
 *
 * @details
 * You can call this method to set video streams with different resolutions for the same video
 * source. The subscribers can call `setRemoteVideoStream:type:` to select which stream layer to
 * receive. The broadcaster can publish up to four layers of video streams: one main stream (highest
 * resolution) and three additional streams of different quality levels.
 *
 * @param simulcastConfig Configuration for different video steam layers. This configuration
 * includes seven layers, from AgoraStreamLayer1 to AgoraStreamLow, with a maximum of three layers
 * enabled simultaneously. See `AgoraSimulcastConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSimulcastConfig:(AgoraSimulcastConfig* _Nonnull)simulcastConfig NS_SWIFT_NAME(setSimulcastConfig(_:));

/**
 * @brief Sets the default video stream type to subscribe to.
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
 * Call timing: Call this method before joining a channel. The SDK does not support changing the
 * default subscribed video stream type after joining a channel.
 *
 * @note If you call both this method and `setRemoteVideoStream:type:`, the setting of
 * `setRemoteVideoStream:type:` takes effect.
 *
 * @param streamType The default video-stream type. See `AgoraVideoStreamType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteDefaultVideoStreamType:(AgoraVideoStreamType)streamType NS_SWIFT_NAME(setRemoteDefaultVideoStreamType(_:));

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
 * @note
 * - You can call this method either before or after joining a channel.
 * - If you call both this method and `setRemoteDefaultVideoStreamType:`, the setting of this method
 * takes effect.
 *
 * @param uid The user ID.
 * @param streamType The video stream type, see `AgoraVideoStreamType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteVideoStream:(NSUInteger)uid
                       type:(AgoraVideoStreamType)streamType NS_SWIFT_NAME(setRemoteVideoStream(_:type:));

/**
 * @brief Options for subscribing to remote video streams.
 *
 * @details
 * When a remote user has enabled dual-stream mode, you can call this method to choose the option
 * for subscribing to the video streams sent by the remote user. The default subscription behavior
 * of the SDK for remote video streams depends on the type of registered video observer:
 * - If the `AgoraVideoFrameDelegate` observer is registered, the default is to subscribe to both
 * raw data and encoded data.
 * - If the `AgoraEncodedVideoFrameDelegate` observer is registered, the default is to subscribe
 * only to the encoded data.
 * - If both types of observers are registered, the default behavior follows the last registered
 * video observer. For example, if the last registered observer is the `AgoraVideoFrameDelegate`
 * observer, the default is to subscribe to both raw data and encoded data.
 * If you want to modify the default behavior, or set different subscription options for different
 * `uids`, you can call this method to set it.
 *
 * @param uid The user ID of the remote user.
 * @param options The video subscription options. See `AgoraVideoSubscriptionOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteVideoSubscriptionOptions:(NSUInteger)uid
                                 options:(AgoraVideoSubscriptionOptions* _Nonnull)options NS_SWIFT_NAME(setRemoteVideoSubscriptionOptions(_:options:));

#pragma mark Subscribe Blocklist / Allowlist

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSubscribeAudioBlocklist:(NSArray <NSNumber *> *_Nonnull)blocklist NS_SWIFT_NAME(setSubscribeAudioBlocklist(_:));

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSubscribeAudioAllowlist:(NSArray <NSNumber *> *_Nonnull)allowlist NS_SWIFT_NAME(setSubscribeAudioAllowlist(_:));

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSubscribeVideoBlocklist:(NSArray <NSNumber *> *_Nonnull)blocklist NS_SWIFT_NAME(setSubscribeVideoBlocklist(_:));

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
 *
  * @param allowlist The user ID list of users that you want to subscribe to.
 * If you want to specify the video streams of a user for subscription, add the user ID in this
 * list. If you want to remove a user from the allowlist, you need to call the
 * `setSubscribeVideoAllowlist:` method to update the user ID list; this means you only add the
 * `uid` of users that you want to subscribe to in the new user ID list.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSubscribeVideoAllowlist:(NSArray <NSNumber *> *_Nonnull)allowlist NS_SWIFT_NAME(setSubscribeVideoAllowlist(_:));

#pragma mark Stream Fallback

/**
 * @brief Sets the fallback option for the subscribed video stream based on the network conditions.
 *
 * @details
 * An unstable network affects the audio and video quality in a video call or interactive live video
 * streaming. If `option` is set as `AgoraStreamFallbackOptionVideoStreamLow` or
 * `AgoraStreamFallbackOptionAudioOnly`, the SDK automatically switches the video from a
 * high-quality stream to a low-quality stream or disables the video when the downlink network
 * conditions cannot support both audio and video to guarantee the quality of the audio. Meanwhile,
 * the SDK continuously monitors network quality and resumes subscribing to audio and video streams
 * when the network quality improves.
 * When the subscribed video stream falls back to an audio-only stream, or recovers from an
 * audio-only stream to an audio-video stream, the SDK triggers the
 * `rtcEngine:didRemoteSubscribeFallbackToAudioOnly:byUid:` callback.
 *
 * @param option Fallback options for the subscribed stream. See `AgoraStreamFallbackOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteSubscribeFallbackOption:(AgoraStreamFallbackOptions)option NS_SWIFT_NAME(setRemoteSubscribeFallbackOption(_:));

/** Sets high priority user list and related remote subscribe stream fallback option.

  This method sets the high priority user list and disables/enables the vips' remote subscribe stream fallback option according to the network conditions in NASA2.

 If you use the option as AgoraStreamFallbackOptionAudioOnly = 2, the AutVos will automatically switch the video from a high-stream to a low-stream, or disable the video when the downlink network conditions cannot support both audio and video to guarantee the quality of the audio. The AutVos monitors the network quality and re-enables the video stream when the network conditions improve.
 Once the subscribed stream falls back to audio only, or the audio stream switches back to the video stream, the [didRemoteSubscribeFallbackToAudioOnly]([AgoraRtcEngineDelegate  rtcEngine:didRemoteSubscribeFallbackToAudioOnly:byUid:]) callback will be triggered.

 @param option AgoraStreamFallbackOptions
 @return * 0: Success.
* <0: Failure.
 */
- (int)setHighPriorityUserList:(NSArray <NSNumber *> *_Nullable)uidList
                        option:(AgoraStreamFallbackOptions)option;


#pragma mark External Media Source

/**
 * @brief Configures the external video source.
 *
 * @details
 * After calling this method to enable an external video source, you can call
 * `pushExternalVideoFrame:videoTrackId:` to push external video data to the SDK.
 * Call timing: Call this method before joining a channel.
 *
 * @note Dynamic switching of video sources is not supported within the channel. To switch from an
 * external video source to an internal video source, you must first leave the channel, call this
 * method to disable the external video source, and then rejoin the channel.
 *
 * @param enable Whether to use the external video source:
 * - `YES`: Use the external video source. The SDK prepares to accept the external video frame.
 * - `NO`: (Default) Do not use the external video source.
 * @param useTexture Whether to use the external video frame in the Texture format.
 * - `YES`: Use the external video frame in the Texture format.
 * - `NO`: (Default) Do not use the external video frame in the Texture format.
 * @param sourceType Whether the external video frame is encoded. See
 * `AgoraExternalVideoSourceType`.
 *
 */
- (void)setExternalVideoSource:(BOOL)enable useTexture:(BOOL)useTexture sourceType:(AgoraExternalVideoSourceType)sourceType NS_SWIFT_NAME(setExternalVideoSource(_:useTexture:sourceType:));

/**
 * Sets the external video source.
 *
 * Call this API before `enableVideo` or `startPreview`.
 *
 * @param enable Determines whether to enable the external video source.
 * - `YES`: Use external video source.
 * - `NO`: Do not use external video source.
 *
 * The Agora SDK does not support switching video sources dynamically in the
 * channel. If an external video source is enabled and you are in a channel, if
 * you want to switch to an internal video source, you must exit the channel.
 * Then call this method to set enable as NO, and join the channel again.
 * @param useTexture Determines whether to use textured video data.
 * - `YES`: Use the texture as an input.
 * - `NO`: Do not use the texture as an input.
 * @param sourceType Determines the type of  the external video source.
 * @param encodedVideoTrackOption Determines encoded video track options.
 */
- (void)setExternalVideoSource:(BOOL)enable useTexture:(BOOL)useTexture
                                            sourceType:(AgoraExternalVideoSourceType)sourceType
                               encodedVideoTrackOption:(AgoraEncodedVideoTrackOptions* _Nonnull)encodedVideoTrackOption NS_SWIFT_NAME(setExternalVideoSource(_:useTexture:sourceType:encodedVideoTrackOption:));

/**
 * @brief Pushes the external raw video frame to the SDK.
 *
 * @details
 * After calling the `setExternalVideoSource:useTexture:sourceType:` method and setting the
 * `enabled` parameter to `YES`, and the `encodedFrame` parameter to `NO`, you can use this method
 * to push the raw external video frame to the SDK.
 *
 * @param frame The external raw video frame to be pushed. See `AgoraVideoFrame`.
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)pushExternalVideoFrame:(AgoraVideoFrame * _Nonnull)frame NS_SWIFT_NAME(pushExternalVideoFrame(_:)) __deprecated_msg("use pushExternalVideoFrame:frame,videoTrackId options instead.");

/**
 * @brief Pushes the external raw video frame to the SDK through video tracks.
 *
 * @details
 * To publish a custom video source, see the following steps:1. Call `createCustomVideoTrack` to
 * create a video track and get the video track ID.
 * 2. Call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel. In
 * `AgoraRtcChannelMediaOptions`, set `customVideoTrackId` to the video track ID that you want to
 * publish, and set `publishCustomVideoTrack` to `YES`.
 * 3. Call this method and specify `videoTrackId` as the video track ID set in step 2. You can then
 * publish the corresponding custom video source in the channel.
 *
 * @note
 * If you only need to push one custom video source to the channel, you can directly call the
 * `setExternalVideoSource:useTexture:sourceType:` method and the SDK will automatically create a
 * video track with the `videoTrackId` set to 0.
 * DANGER: After calling this method, even if you stop pushing external video frames to the SDK, the
 * custom video stream will still be counted as the video duration usage and incur charges. Agora
 * recommends that you take appropriate measures based on the actual situation to avoid such video
 * billing.
 * - If you no longer need to capture external video data, you can call `destroyCustomVideoTrack:`
 * to destroy the custom video track.
 * - If you only want to use the external video data for local preview and not publish it in the
 * channel, you can call `muteLocalVideoStream:` to cancel sending video stream or call
 * `updateChannelWithMediaOptions:` to set `publishCustomVideoTrack` to `NO`.
 *
 * @param frame The external raw video frame to be pushed. See `AgoraVideoFrame`.
 * @param videoTrackId The video track ID returned by calling the `createCustomVideoTrack`
 * method.Note: If you only need to push one custom video source, set `videoTrackId` to 0.
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)pushExternalVideoFrame:(AgoraVideoFrame * _Nonnull)frame videoTrackId:(NSUInteger)videoTrackId NS_SWIFT_NAME(pushExternalVideoFrame(_:videoTrackId:));

/**
 * Pushes the encoded external video frame to Agora SDK.
 *
 * @note
 * Ensure that you have configured encoded video source before calling this method.
 *
 * @param frame The encoded external video data, which must be the direct buffer.
 * @param info The encoded external video frame info: AgoraEncodedVideoFrameInfo.
 *
 * @return
 * - 0: Success, which means that the encoded external video frame is pushed successfully.
 * - < 0: Failure, which means that the encoded external video frame fails to be pushed.
 * @technical preview
 */
- (int)pushExternalEncodedVideoFrame:(NSData* _Nonnull)frame
                                info:(AgoraEncodedVideoFrameInfo * _Nonnull)info NS_SWIFT_NAME(pushExternalEncodedVideoFrame(_:info:));

/**
 * Pushes the encoded external video frame to specified connection in Agora SDK.
 *
 * @note
 * Ensure that you have configured encoded video source before calling this method.
 *
 * @param frame The encoded external video data, which must be the direct buffer.
 * @param info The encoded external video frame info: AgoraEncodedVideoFrameInfo.
 * @param videoTrackId The id of the video track.
 *
 * @return
 * - 0: Success, which means that the encoded external video frame is pushed successfully.
 * - < 0: Failure, which means that the encoded external video frame fails to be pushed.
 * @technical preview
 */
- (int)pushExternalEncodedVideoFrame:(NSData* _Nonnull)frame
                                  info:(AgoraEncodedVideoFrameInfo * _Nonnull)info
                            videoTrackId:(NSUInteger)videoTrackId NS_SWIFT_NAME(pushExternalEncodedVideoFrame(_:info:videoTrackId:));


/**
 * @brief Creates a custom video track.
 *
 * @details
 * To publish a custom video source, see the following steps:1. Call this method to create a video
 * track and get the video track ID.
 * 2. Call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel. In
 * `AgoraRtcChannelMediaOptions`, set `customVideoTrackId` to the video track ID that you want to
 * publish, and set `publishCustomVideoTrack` to `YES`.
 * 3. Call `pushExternalVideoFrame:videoTrackId:` and specify `videoTrackId` as the video track ID
 * set in step 2. You can then publish the corresponding custom video source in the channel.
 *
 * @return
 * - If the method call is successful, the video track ID is returned as the unique identifier of
 * the video track.
 * - If the method call fails, 0xffffffff is returned.
 */
- (unsigned int)createCustomVideoTrack NS_SWIFT_NAME(createCustomVideoTrack());

/**
 * Get an custom encoded video track id created by internal,which could used to publish or preview
 *
 * @return
 * - > 0: the useable video track id.
 * - < 0: Failure.
 */
- (unsigned int)createCustomEncodedVideoTrack:(AgoraEncodedVideoTrackOptions* _Nonnull)encodedVideoTrackOption NS_SWIFT_NAME(createCustomEncodedVideoTrack(_:));

/**
 * @brief Destroys the specified video track.
 *
 * @param videoTrackId The video track ID returned by calling the `createCustomVideoTrack` method.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)destroyCustomVideoTrack:(NSUInteger)videoTrackId NS_SWIFT_NAME(destroyCustomVideoTrack(_:));

/**
 * destroy a created custom encoded video track id
 *
 * @param videoTrackId The video track id which was created by createCustomEncodedVideoTrack
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)destroyCustomEncodedVideoTrack:(NSUInteger)videoTrackId NS_SWIFT_NAME(destroyCustomEncodedVideoTrack(_:));

#pragma mark External Audio Data

/**
 * @brief Sets the external audio sink.
 *
 * @details
 * After enabling the external audio sink, you can call
 * `pullPlaybackAudioFrameRawData:lengthInByte:` to pull remote audio frames. The app can process
 * the remote audio and play it with the audio effects that you want.
 * Applicable scenarios: This method applies to scenarios where you want to use external audio data
 * for playback.
 * Call timing: Call this method before joining a channel.
 *
 * @note Once you enable the external audio sink, the app will not retrieve any audio data from the
 * `onPlaybackAudioFrame:` callback.
 *
 * @param enabled Whether to enable or disable the external audio sink:
 * - `YES`: Enables the external audio sink.
 * - `NO`: (Default) Disables the external audio sink.
 * @param sampleRate The sample rate (Hz) of the external audio sink, which can be set as 16000,
 * 32000, 44100, or 48000.
 * @param channels The number of audio channels of the external audio sink:
 * - 1: Mono.
 * - 2: Stereo.
 *
 */
- (void)enableExternalAudioSink:(BOOL)enabled
                     sampleRate:(NSUInteger)sampleRate
                       channels:(NSUInteger)channels NS_SWIFT_NAME(enableExternalAudioSink(_:sampleRate:channels:));

/**
 * @brief Pulls the remote audio data.
 *
 * @details
 * After a successful call of this method, the app pulls the decoded and mixed audio data for
 * playback.
 * Call timing: Call this method after joining a channel.
 * Before calling this method, call `enableExternalAudioSink:sampleRate:channels:` `(enabled: YES)`
 * to notify the app to enable and set the external audio rendering.
 *
 * @note
 * Both this method and the `onPlaybackAudioFrame:` callback can be used to get audio data after
 * remote mixing. After calling `enableExternalAudioSink:sampleRate:channels:` to enable external
 * audio rendering, the app will no longer be able to obtain data from the `onPlaybackAudioFrame:`
 * callback. Therefore, you should choose between this method and the `onPlaybackAudioFrame:`
 * callback based on your actual business requirements. The specific distinctions between them are
 * as follows:
 * - After calling this method, the app automatically pulls the audio data from the SDK. By setting
 * the audio data parameters, the SDK adjusts the frame buffer to help the app handle latency,
 * effectively avoiding audio playback jitter.
 * - After registering the `onPlaybackAudioFrame:` callback, the SDK sends the audio data to the app
 * through the callback. Any delay in processing the audio frames may result in audio jitter.
 * This method is only used for retrieving audio data after remote mixing. If you need to get audio
 * data from different audio processing stages such as capture and playback, you can register the
 * corresponding callbacks by calling `setAudioFrameDelegate:`.
 *
 * @param data The remote audio data to be pulled. The data type is `byte[]`.
 * @param lengthInByte The data length (byte). The value of this parameter is related to the audio
 * duration, and the values of the `sampleRate` and `channels` parameters that you set in
 * `enableExternalAudioSink:sampleRate:channels:`. `lengthInByte` = `sampleRate`/1000 × 2 ×
 * `channels` × audio duration (ms).
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)pullPlaybackAudioFrameRawData:(void * _Nonnull)data
                              lengthInByte:(NSUInteger)lengthInByte NS_SWIFT_NAME(pullPlaybackAudioFrameRawData(_:lengthInByte:));

/**
 * @brief Pulls remote audio data in the SampleBuffer format.
 *
 * @details
 * After a successful call of this method, the app pulls the decoded and mixed audio data for
 * playback.
 *
 * @param lengthInByte The data length (byte). The value of this parameter is related to the audio
 * duration, and the values of the `sampleRate` and `channels` parameters that you set in
 * `enableExternalAudioSink:sampleRate:channels:`. `lengthInByte` = `sampleRate`/1000 × 2 ×
 * `channels` × audio duration (ms).
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (CMSampleBufferRef _Nullable)pullPlaybackAudioFrameSampleBufferByLengthInByte:(NSUInteger)lengthInByte NS_SWIFT_NAME(pullPlaybackAudioFrameSampleBufferBy(lengthInByte:));

/**
 * @brief Creates a custom audio track.
 *
 * @details
 * To publish a custom audio source, see the following steps:1. Call this method to create a custom
 * audio track and get the audio track ID.
 * 2. Call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel. In
 * `AgoraRtcChannelMediaOptions`, set `publishCustomAudioTrackId` to the audio track ID that you
 * want to publish, and set `publishCustomAudioTrack` to `YES`.
 * 3. Call `pushExternalAudioFrameRawData:samples:sampleRate:channels:trackId:timestamp:` and
 * specify `trackId` as the audio track ID set in step 2. You can then publish the corresponding
 * custom audio source in the channel.
 *
 * @note Call this method before joining a channel.
 *
 * @param trackType The type of the custom audio track. See `AgoraAudioTrackType`.Attention: If
 * `AgoraAudioTrackTypeDirect` is specified for this parameter, you must set
 * `publishMicrophoneTrack` to `NO` in `AgoraRtcChannelMediaOptions` when calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel; otherwise,
 * joining the channel fails and returns the error code -2.
 * @param config The configuration of the custom audio track. See `AgoraAudioTrackConfig`.
 *
 * @return
 * - If the method call is successful, the audio track ID is returned as the unique identifier of
 * the audio track.
 * - If the method call fails, 0xffffffff is returned.
 */
- (int)createCustomAudioTrack:(AgoraAudioTrackType)trackType
                       config:(AgoraAudioTrackConfig* _Nonnull)config;

/**
 * @brief Destroys the specified audio track.
 *
 * @param trackId The custom audio track ID returned in `createCustomAudioTrack:config:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)destroyCustomAudioTrack:(NSInteger)trackId;

/**
 * @brief Sets whether to enable the local playback of external audio source.
 *
 * @details
 * After calling this method to enable the local playback of external audio source, if you need to
 * stop local playback, you can call this method again and set `enabled` to `NO`.
 * You can call `adjustCustomAudioPlayoutVolume:volume:` to adjust the local playback volume of the
 * custom audio track.
 *
 * @note Ensure you have called the `createCustomAudioTrack:config:` method to create a custom audio
 * track before calling this method.
 *
 * @param trackId The audio track ID. Set this parameter to the custom audio track ID returned in
 * `createCustomAudioTrack:config:`.
 * @param enabled Whether to play the external audio source:
 * - `YES`: Play the external audio source.
 * - `NO`: (Default) Do not play the external source.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableCustomAudioLocalPlayback:(NSInteger)trackId enabled:(BOOL)enabled NS_SWIFT_NAME(enableCustomAudioLocalPlayback(_:enabled:));

/** Pushes the external audio frame to the Agora SDK for encoding.
 * @note
 *  Use `setExternalAudioSource` method to set sampleRate and channels.
 *
 * @param data      External audio data.
 * @param samples   Number of samples for the push.
 * @param trackId   The audio track ID. Use the createCustomAudioTrack method to get trackId.
 * @param timestamp Time stamp of the external audio frame to be synchronized with the external video source.
 * @return * 0: Success.
 * <0: Failure.
 */
- (int)pushExternalAudioFrameRawData:(void * _Nonnull)data
                             samples:(NSInteger)samples
                            trackId:(NSInteger)trackId
                           timestamp:(NSTimeInterval)timestamp NS_SWIFT_NAME(pushExternalAudioFrameRawData(_:samples:trackId:timestamp:)) __deprecated_msg("use pushExternalAudioFrameRawData:samples:sampleRate:channels:trackId:timestamp: instead.");

/**
 * @brief Pushes the external audio frame to the SDK.
 *
 * @details
 * Call this method to push external audio frames through the audio track.
 * Call timing: Before calling this method to push external audio data, perform the following
 * steps:1. Call `createCustomAudioTrack:config:` to create a custom audio track and get the audio
 * track ID.
 * 2. Call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel. In
 * `AgoraRtcChannelMediaOptions`, set `publishCustomAudioTrackId` to the audio track ID that you
 * want to publish, and set `publishCustomAudioTrack` to `YES`.
 *
 * @param data The external audio data.
 * @param samples The number of samples.
 * @param timestamp The timestamp (ms) of the external audio frame. This parameter is required. You
 * can use it to restore the order of the captured audio frames, or synchronize audio and video
 * frames in video-related scenarios (including scenarios where external video sources are used).
 * @param sampleRate The sample rate (Hz) of the external audio source which can be set as `8000`,
 * `16000`, `32000`, `44100`, or `48000`.
 * @param channels The number of channels of the external audio source, which can be set as `1`
 * (Mono) or `2` (Stereo).
 * @param trackId The audio track ID. Set this parameter to the custom audio track ID returned in
 * `createCustomAudioTrack:config:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)pushExternalAudioFrameRawData:(void* _Nonnull)data
                             samples:(NSInteger)samples
                          sampleRate:(NSInteger)sampleRate
                            channels:(NSInteger)channels
                            trackId:(NSInteger)trackId
                           timestamp:(NSTimeInterval)timestamp NS_SWIFT_NAME(pushExternalAudioFrameRawData(_:samples:sampleRate:channels:trackId:timestamp:));

/**
 * @brief Pushes external CMSampleBuffer audio frames.
 *
 * @param sampleBuffer The sample buffer.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)pushExternalAudioFrameSampleBuffer:(CMSampleBufferRef _Nonnull)sampleBuffer NS_SWIFT_NAME(pushExternalAudioFrameSampleBuffer(_:)) __deprecated_msg("use pushExternalAudioFrameSampleBuffer:sampleRate:channels:trackId: instead.");

/**
 * @brief Pushes the external CMSampleBuffer audio frames to the channel through a custom audio
 * track.
 *
 * @details
 * Before calling this method to push external audio data, perform the following steps:1. Call
 * `createCustomAudioTrack:config:` to create a custom audio track and get the audio track ID.
 * 2. Call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel. In
 * `AgoraRtcChannelMediaOptions`, set `publishCustomAudioTrackId` to the audio track ID that you
 * want to publish, and set `publishCustomAudioTrack` to `YES`.
 *
 * @param sampleBuffer The sample buffer.
 * @param sampleRate The sample rate (Hz) of the external audio source which can be set as `8000`,
 * `16000`, `32000`, `44100`, or `48000`.
 * @param channels The number of channels of the external audio source, which can be set as `1`
 * (Mono) or `2` (Stereo).
 * @param trackId The audio track ID. Set this parameter to the custom audio track ID returned in
 * `createCustomAudioTrack:config:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)pushExternalAudioFrameSampleBuffer:(CMSampleBufferRef _Nonnull)sampleBuffer
                               sampleRate:(NSInteger)sampleRate
                                 channels:(NSInteger)channels
                                  trackId:(NSInteger)trackId NS_SWIFT_NAME(pushExternalAudioFrameSampleBuffer(_:sampleRate:channels:trackId:));

/**
 * @brief Sets the format of the captured raw audio data.
 *
 * @details
 * The SDK calculates the sampling interval based on the `samplesPerCall`, `sampleRate` and
 * `channel` parameters set in this method.Sample interval (sec) = `samplePerCall` /( `sampleRate` ×
 * `channel` ). Ensure that the sample interval ≥ 0.01 (s). The SDK triggers the
 * `onRecordAudioFrame:channelId:` callback according to the sampling interval.
 * Call timing: Call this method before joining a channel.
 *
 * @param sampleRate The sample rate returned in the callback, which can be set as 8000, 16000,
 * 32000, 44100, or 48000 Hz.
 * @param channel The number of audio channels. You can set the value as 1 or 2.
 * - 1: Mono.
 * - 2: Stereo.
 * @param mode The use mode of the audio frame. See `AgoraAudioRawFrameOperationMode`.
 * @param samplesPerCall The number of data samples, such as 1024 for the Media Push.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRecordingAudioFrameParametersWithSampleRate:(NSInteger)sampleRate
                                              channel:(NSInteger)channel
                                                 mode:(AgoraAudioRawFrameOperationMode)mode
                                       samplesPerCall:(NSInteger)samplesPerCall NS_SWIFT_NAME(setRecordingAudioFrameParametersWithSampleRate(_:channel:mode:samplesPerCall:));

/**
 * @brief Sets the format of the raw audio playback data.
 *
 * @details
 * The SDK calculates the sampling interval based on the `samplesPerCall`, `sampleRate` and
 * `channel` parameters set in this method.Sample interval (sec) = `samplePerCall` /( `sampleRate` ×
 * `channel` ). Ensure that the sample interval ≥ 0.01 (s). The SDK triggers the
 * `onPlaybackAudioFrame:` callback according to the sampling interval.
 * Call timing: Call this method before joining a channel.
 *
 * @param sampleRate The sample rate returned in the callback, which can be set as 8000, 16000,
 * 24000, 32000, 44100, or 48000 Hz.
 * @param channel The number of audio channels. You can set the value as 1 or 2.
 * - 1: Mono.
 * - 2: Stereo.
 * @param mode The use mode of the audio frame. See `AgoraAudioRawFrameOperationMode`.
 * @param samplesPerCall The number of data samples, such as 1024 for the Media Push.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setPlaybackAudioFrameParametersWithSampleRate:(NSInteger)sampleRate
                                             channel:(NSInteger)channel
                                                mode:(AgoraAudioRawFrameOperationMode)mode
                                      samplesPerCall:(NSInteger)samplesPerCall NS_SWIFT_NAME(setPlaybackAudioFrameParametersWithSampleRate(_:channel:mode:samplesPerCall:));

/**
 * @brief Sets the format of the raw audio data after mixing for audio capture and playback.
 *
 * @details
 * The SDK calculates the sampling interval based on the `samplesPerCall`, `sampleRate` and
 * `channel` parameters set in this method.Sample interval (sec) = `samplePerCall` /( `sampleRate` ×
 * `channel` ). Ensure that the sample interval ≥ 0.01 (s). The SDK triggers the
 * `onMixedAudioFrame:channelId:` callback according to the sampling interval.
 * Call timing: Call this method before joining a channel.
 *
 * @param sampleRate The sample rate returned in the callback, which can be set as 8000, 16000,
 * 32000, 44100, or 48000 Hz.
 * @param channel The number of audio channels. You can set the value as 1 or 2.
 * - 1: Mono.
 * - 2: Stereo.
 * @param samplesPerCall The number of data samples, such as 1024 for the Media Push.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setMixedAudioFrameParametersWithSampleRate:(NSInteger)sampleRate
                                          channel:(NSInteger)channel
                                   samplesPerCall:(NSInteger)samplesPerCall NS_SWIFT_NAME(setMixedAudioFrameParametersWithSampleRate(_:channel:samplesPerCall:));

/**
 * @brief Sets the format of the in-ear monitoring raw audio data.
 *
 * @details
 * This method is used to set the in-ear monitoring audio data format reported by the
 * `onEarMonitoringAudioFrame:` callback.
 *
 * @note
 * - Before calling this method, you need to call `enableInEarMonitoring:includeAudioFilters:`, and
 * set `includeAudioFilters` to `AgoraEarMonitoringFilterBuiltInAudioFilters` or
 * `AgoraEarMonitoringFilterNoiseSuppression`.
 * - The SDK calculates the sampling interval based on the `samplesPerCall`, `sampleRate` and
 * `channel` parameters set in this method.Sample interval (sec) = `samplePerCall` /( `sampleRate` ×
 * `channel` ). Ensure that the sample interval ≥ 0.01 (s). The SDK triggers the
 * `onEarMonitoringAudioFrame:` callback according to the sampling interval.
 *
 * @param sampleRate The sample rate of the audio data reported in the `onEarMonitoringAudioFrame:`
 * callback, which can be set as 8,000, 16,000, 32,000, 44,100, or 48,000 Hz.
 * @param channel The number of audio channels reported in the `onEarMonitoringAudioFrame:`
 * callback.
 * - 1: Mono.
 * - 2: Stereo.
 * @param mode The use mode of the audio frame. See `AgoraAudioRawFrameOperationMode`.
 * @param samplesPerCall The number of data samples reported in the `onEarMonitoringAudioFrame:`
 * callback, such as 1,024 for the Media Push.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setEarMonitoringAudioFrameParametersWithSampleRate:(NSInteger)sampleRate
                                                  channel:(NSInteger)channel
                                                     mode:(AgoraAudioRawFrameOperationMode)mode
                                           samplesPerCall:(NSInteger)samplesPerCall NS_SWIFT_NAME(setEarMonitoringAudioFrameParametersWithSampleRate(_:channel:mode:samplesPerCall:));

/**
 * @brief Sets the format of the raw audio playback data before mixing.
 *
 * @details
 * The SDK triggers the `onPlaybackAudioFrameBeforeMixing:channelId:uid:` callback according to the
 * sampling interval.
 * Call timing: Call this method before joining a channel.
 *
 * @param sampleRate The sample rate returned in the callback, which can be set as 8000, 16000,
 * 32000, 44100, or 48000 Hz.
 * @param channel The number of audio channels. You can set the value as 1 or 2.
 * - 1: Mono.
 * - 2: Stereo.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setPlaybackAudioFrameBeforeMixingParametersWithSampleRate:(NSInteger)sampleRate
                                                         channel:(NSInteger)channel NS_SWIFT_NAME(setPlaybackAudioFrameBeforeMixingParametersWithSampleRate(_:channel:));

/**
 * @brief Sets the format of audio data in the `onPlaybackAudioFrameBeforeMixing:channelId:uid:`
 * callback.
 *
 * @details
 * Used to set the sample rate, number of channels, and number of samples per callback for the audio
 * data returned in the `onPlaybackAudioFrameBeforeMixing:channelId:uid:` callback.
 *
 * @param sampleRate Input parameter, sampling rate (Hz) returned in the
 * `onPlaybackAudioFrameBeforeMixing:channelId:uid:` callback. It can be set as the following
 * values: 8000、16000、32000、44100 or 48000.
 * @param channel Input parameter, the number of channels for the audio data returned in the
 * `onPlaybackAudioFrameBeforeMixing:channelId:uid:` callback.
 * - 1: Mono.
 * - 2: Stereo.
 * @param samplesPerCall Input parameter, the number of sample rate of the data returned in the
 * `onPlaybackAudioFrameBeforeMixing:channelId:uid:` callback. For example, in the RTMP streaming
 * scenario, it is recommended to set it to 1024.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setPlaybackAudioFrameBeforeMixingParametersWithSampleRate:(NSInteger)sampleRate
                                                         channel:(NSInteger)channel
                                                  samplesPerCall:(NSInteger)samplesPerCall NS_SWIFT_NAME(setPlaybackAudioFrameBeforeMixingParametersWithSampleRate(_:channel:samplesPerCall:));
/**
 * @brief Adjusts the volume of the custom audio track played remotely.
 *
 * @details
 * If you want to change the volume of the audio played remotely, you need to call this method
 * again.
 *
 * @note Ensure you have called the `createCustomAudioTrack:config:` method to create a custom audio
 * track before calling this method.
 *
 * @param trackId The audio track ID. Set this parameter to the custom audio track ID returned in
 * `createCustomAudioTrack:config:`.
 * @param volume The volume of the audio source. The value can range from 0 to 100. 0 means mute;
 * 100 means the original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustCustomAudioPublishVolume:(NSInteger)trackId volume:(NSInteger)volume NS_SWIFT_NAME(adjustCustomAudioPublishVolume(_:volume:));

/**
 * @brief Adjusts the volume of the custom audio track played locally.
 *
 * @details
 * If you want to change the volume of the audio to be played locally, you need to call this method
 * again.
 *
 * @note Ensure you have called the `createCustomAudioTrack:config:` method to create a custom audio
 * track before calling this method.
 *
 * @param trackId The audio track ID. Set this parameter to the custom audio track ID returned in
 * `createCustomAudioTrack:config:`.
 * @param volume The volume of the audio source. The value can range from 0 to 100. 0 means mute;
 * 100 means the original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustCustomAudioPlayoutVolume:(NSInteger)trackId volume:(NSInteger)volume NS_SWIFT_NAME(adjustCustomAudioPlayoutVolume(_:volume:));

/**
 * @brief Gets the current Monotonic Time of the SDK.
 *
 * @details
 * Monotonic Time refers to a monotonically increasing time series whose value increases over time.
 * The unit is milliseconds.
 * In custom video capture and custom audio capture scenarios, in order to ensure audio and video
 * synchronization, Agora recommends that you call this method to obtain the current Monotonic Time
 * of the SDK, and then pass this value into the timestamp parameter in the captured video frame (
 * `AgoraOutputVideoFrame` ) and audio frame ( `AgoraAudioFrame` ).
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @return
 * - ≥ 0: The method call is successful, and returns the current Monotonic Time of the SDK (in
 * milliseconds).
 * - < 0: Failure.
 */
- (int64_t)getCurrentMonotonicTimeInMs NS_SWIFT_NAME(getCurrentMonotonicTimeInMs());

#pragma mark Audio spectrum monitor

/**
 * @brief Registers an audio spectrum observer.
 *
 * @details
 * After successfully registering the audio spectrum observer and calling
 *  `enableAudioSpectrumMonitor:` to enable the audio spectrum monitoring, the SDK reports the
 * callback that you implement in the `AgoraAudioSpectrumDelegate` class according to the time
 * interval you set.
 *
 * @note You can call this method either before or after joining a channel.
 *
 * @param delegate The audio spectrum observer. See `AgoraAudioSpectrumDelegate`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)registerAudioSpectrumDelegate:(id<AgoraAudioSpectrumDelegate> _Nullable )delegate NS_SWIFT_NAME(registerAudioSpectrumDelegate(_:));

/**
 * @brief Unregisters the audio spectrum observer.
 *
 * @details
 * After calling `registerAudioSpectrumDelegate:`, if you want to disable audio spectrum monitoring,
 * you can call this method.
 *
 * @note You can call this method either before or after joining a channel.
 *
 * @param delegate The audio spectrum observer. See `AgoraAudioSpectrumDelegate`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)unregisterAudioSpectrumDelegate:(id<AgoraAudioSpectrumDelegate> _Nullable)delegate NS_SWIFT_NAME(unregisterAudioSpectrumDelegate(_:));

/**
 * @brief Turns on audio spectrum monitoring.
 *
 * @details
 * If you want to obtain the audio spectrum data of local or remote users, you can register the
 * audio spectrum observer and enable audio spectrum monitoring.
 *
 * @note You can call this method either before or after joining a channel.
 *
 * @param intervalInMS The interval (in milliseconds) at which the SDK triggers the
 * `onLocalAudioSpectrum:` and `onRemoteAudioSpectrum:` callbacks. The default value is 100. Do not
 * set this parameter to a value less than 10, otherwise calling this method would fail.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: Invalid parameters.
 */
- (int)enableAudioSpectrumMonitor:(int)intervalInMS NS_SWIFT_NAME(enableAudioSpectrumMonitor(_:));

/**
 * @brief Disables audio spectrum monitoring.
 *
 * @details
 * After calling `enableAudioSpectrumMonitor:`, if you want to disable audio spectrum monitoring,
 * you can call this method.
 *
 * @note You can call this method either before or after joining a channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)disableAudioSpectrumMonitor NS_SWIFT_NAME(disableAudioSpectrumMonitor());

#pragma mark Built-in Encryption

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: An invalid parameter is used. Set the parameter with a valid value.
 *   - -4: The built-in encryption mode is incorrect or the SDK fails to load the external
 * encryption library. Check the enumeration or reload the external encryption library.
 *   - -7: The SDK is not initialized. Initialize the `AgoraRtcEngineKit` instance before calling
 * this method.
 */
- (int)enableEncryption:(BOOL)enabled encryptionConfig:(AgoraEncryptionConfig * _Nonnull)config NS_SWIFT_NAME(enableEncryption(_:encryptionConfig:));

#pragma mark Data Stream

/**
 * @brief Creates a data stream.
 *
 * @details
 * You can call this method to create a data stream and improve the reliability and ordering of data
 * transmission.
 * Call timing: You can call this method either before or after joining a channel.
 * Related callbacks: After setting `reliable` to `YES`, if the recipient does not receive the data
 * within five seconds, the SDK triggers the
 * `rtcEngine:didOccurStreamMessageErrorFromUid:streamId:error:missed:cached:` callback and returns
 * an error code.
 *
 * @note
 * Each user can create up to five data streams during the lifecycle of `AgoraRtcEngineKit`. The
 * data stream will be destroyed when leaving the channel, and the data stream needs to be recreated
 * if needed.
 * If you need a more comprehensive solution for low-latency, high-concurrency, and scalable
 * real-time messaging and status synchronization, it is recommended to use `Signaling`.
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
 *
 * @return
 * - 0: The data stream is successfully created.
 * - < 0: Failure.
 */
- (int)createDataStream:(NSInteger * _Nonnull)streamId
               reliable:(BOOL)reliable
                ordered:(BOOL)ordered NS_SWIFT_NAME(createDataStream(_:reliable:ordered:));

/**
 * @brief Creates a data stream.
 *
 * @details
 * Compared to `createDataStream:reliable:ordered:`, this method does not guarantee the reliability
 * of data transmission. If a data packet is not received five seconds after it was sent, the SDK
 * directly discards the data.
 * Call timing: You can call this method either before or after joining a channel.
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
 *
 * @return
 * - 0: The data stream is successfully created.
 * - < 0: Failure.
 */
- (int)createDataStream:(NSInteger * _Nonnull)streamId
                  config:(AgoraDataStreamConfig * _Nonnull)config NS_SWIFT_NAME(createDataStream(_:config:));

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)sendStreamMessage:(NSInteger)streamId
                    data:(NSData * _Nonnull)data NS_SWIFT_NAME(sendStreamMessage(_:data:));

#pragma mark Common Data Transmission
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
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)sendRdtMessage:(NSInteger)uid
                 type:(AgoraRdtStreamType)type
                 data:(NSData * _Nonnull)data NS_SWIFT_NAME(sendRdtMessage(_:type:data:));

/**
 * @brief Send media control message
 *
 * @technical preview
 *
 * @param uid remote user id. In particular, if the uid is set to 0, it means broadcasting the message to the entire channel.
 * @param data The sent data.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)sendMediaControlMessage:(NSInteger)uid
                 data:(NSData * _Nonnull)data NS_SWIFT_NAME(sendMediaControlMessage(_:data:));

#pragma mark Stream Publish

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The URL or configuration of transcoding is invalid; check your URL and transcoding
 * configurations.
 *   - -7: The SDK is not initialized before calling this method.
 *   - -19: The Media Push URL is already in use; use another URL instead.
 */

- (int)startRtmpStreamWithoutTranscoding:(NSString* _Nonnull)url NS_SWIFT_NAME(startRtmpStreamWithoutTranscoding(_:));

/**
 * @brief Starts Media Push and sets the transcoding configuration.
 *
 * @details
 * Agora recommends that you use the server-side Media Push function. For details, see `Use RESTful
 * API`.
 * You can call this method to push a live audio-and-video stream to the specified CDN address and
 * set the transcoding configuration. This method can push media streams to only one CDN address at
 * a time, so if you need to push streams to multiple addresses, call this method multiple times.
 * Under one Agora project, the maximum number of concurrent tasks to push media streams is 200 by
 * default. If you need a higher quota, contact `technical support`.
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
 * @param transcoding The transcoding configuration for Media Push. See `AgoraLiveTranscoding`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The URL or configuration of transcoding is invalid; check your URL and transcoding
 * configurations.
 *   - -7: The SDK is not initialized before calling this method.
 *   - -19: The Media Push URL is already in use; use another URL instead.
 */
- (int)startRtmpStreamWithTranscoding:(NSString* _Nonnull)url transcoding:(AgoraLiveTranscoding* _Nullable)transcoding NS_SWIFT_NAME(startRtmpStream(withTranscoding:transcoding:));

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateRtmpTranscoding:(AgoraLiveTranscoding* _Nullable)transcoding NS_SWIFT_NAME(updateRtmpTranscoding(_:));

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
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopRtmpStream:(NSString* _Nonnull)url NS_SWIFT_NAME(stopRtmpStream(_:));

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
#pragma mark Screen Capture

/**-----------------------------------------------------------------------------
 * @name Screen Capture
 * -----------------------------------------------------------------------------
 */

/** Starts screen sharing.
 *
 * @param sourceType source type of screen. See #AgoraVideoSourceType.
 * @param parameters The configuration of the screen sharing.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startScreenCapture:(AgoraVideoSourceType)sourceType config:(AgoraScreenCaptureConfiguration * _Nullable)config NS_SWIFT_NAME(startScreenCapture(_:config:));

/**
 * @brief Stops screen capture from the specified video source.
 *
 * @details
 * Applicable scenarios: If you start screen capture from one or more screens by calling
 * `startScreenCapture:config:`, you need to call this method to stop screen capture, specifying the
 * screen through the `sourceType` parameter.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopScreenCapture:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(stopScreenCapture(_:));

/**
 * @brief Captures the screen by specifying the display ID.
 *
 * @details
 * Captures the video stream of a screen or a part of the screen area.
 * Applicable scenarios: In the screen sharing scenario, you need to call this method to start
 * capturing the screen video stream.
 * Call timing: You can call this method either before or after joining the channel, with the
 * following differences:
 * - Call this method before joining a channel, and then call
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join a channel and set
 * `publishScreenTrack` or `publishSecondaryScreenTrack` to `YES` to start screen sharing.
 * - Call this method after joining a channel, and then call `updateChannelWithMediaOptions:` to
 * join a channel and set `publishScreenTrack` or `publishSecondaryScreenTrack` to `YES` to start
 * screen sharing.
 *
 * @param displayId The display ID of the screen to be shared.
 * @param regionRect (Optional) The relative location of the region to the window. If you do not set
 * this parameter, the SDK shares the whole window. It consists of the following parameters:
 * - x: The horizontal offset from the top-left corner.
 * - y: The vertical offset from the top-left corner.
 * - width: The width of the region.
 * - height: The height of the region.
 * If the specified region overruns the window, the SDK shares only the region within it; if you set
 * width or height as 0, the SDK shares the whole window.
 * @param captureParams Screen sharing configurations. The default video dimension is 1920 x 1080,
 * that is, 2,073,600 pixels. Agora uses the value of this parameter to calculate the charges. See
 * `AgoraScreenCaptureParameters`.Attention: The video properties of the screen sharing stream only
 * need to be set through this parameter, and are unrelated to `setVideoEncoderConfiguration:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid.
 *   - -8: The screen sharing state is invalid. Probably because you have shared other screens or
 * windows. Try calling `stopScreenCapture` to stop the current sharing and start sharing the screen
 * again.
 */
- (int)startScreenCaptureByDisplayId:(UInt32)displayId
                          regionRect:(CGRect)regionRect
                       captureParams:(AgoraScreenCaptureParameters *_Nonnull)captureParams  NS_SWIFT_NAME(startScreenCapture(byDisplayId:regionRect:captureParams:));

/**
 * @brief Captures the whole or part of a window by specifying the window ID.
 *
 * @details
 * This method captures a window or part of the window. You need to specify the ID of the window to
 * be captured.
 * Applicable scenarios: In the screen sharing scenario, you need to call this method to start
 * capturing the screen video stream.
 * Call timing: You can call this method either before or after joining the channel, with the
 * following differences:
 * - Call this method before joining a channel, and then call
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join a channel and set
 * `publishScreenTrack` or `publishSecondaryScreenTrack` to `YES` to start screen sharing.
 * - Call this method after joining a channel, and then call `updateChannelWithMediaOptions:` to
 * join a channel and set `publishScreenTrack` or `publishSecondaryScreenTrack` to `YES` to start
 * screen sharing.
 *
 * @note This method applies to the macOS and Windows only.
 *
 * @param windowId The ID of the window to be shared.
 * @param regionRect (Optional) The relative location of the region to the window. If you do not set
 * this parameter, the SDK shares the whole window. It consists of the following parameters:
 * - x: The horizontal offset from the top-left corner.
 * - y: The vertical offset from the top-left corner.
 * - width: The width of the region.
 * - height: The height of the region.
 * If the specified region overruns the window, the SDK shares only the region within it; if you set
 * width or height as 0, the SDK shares the whole window.
 * @param captureParams Screen sharing configurations. The default video resolution is 1920 × 1080,
 * that is, 2,073,600 pixels. Agora uses the value of this parameter to calculate the charges. See
 * `AgoraScreenCaptureParameters`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid.
 *   - -8: The screen sharing state is invalid. Probably because you have shared other screens or
 * windows. Try calling `stopScreenCapture` to stop the current sharing and start sharing the screen
 * again.
 */
- (int)startScreenCaptureByWindowId:(UInt32)windowId
                         regionRect:(CGRect)regionRect
                      captureParams:(AgoraScreenCaptureParameters *_Nonnull)captureParams  NS_SWIFT_NAME(startScreenCapture(byWindowId:regionRect:captureParams:));

/**
 * @brief Updates the screen capturing region.
 *
 * @param rect The relative location of the region to the screen or window. If you do not set this
 * parameter, the SDK shares the whole screen. If the specified region overruns the screen or
 * window, the SDK shares only the region within it; if you set width or height as 0, the SDK shares
 * the whole screen or window.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateScreenCaptureRegion:(CGRect)rect  NS_SWIFT_NAME(updateScreenCaptureRegion(_:));

/**
 * @brief Updates the screen capturing parameters.
 *
 * @note - Call this method after starting screen sharing or window sharing.
 *
 * @param captureParams The screen sharing encoding parameters. See
 * `AgoraScreenCaptureParameters`.Attention: The video properties of the screen sharing stream only
 * need to be set through this parameter, and are unrelated to `setVideoEncoderConfiguration:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid.
 *   - -8: The screen sharing state is invalid. Probably because you have shared other screens or
 * windows. Try calling `stopScreenCapture` to stop the current sharing and start sharing the screen
 * again.
 */
- (int)updateScreenCaptureParameters:(AgoraScreenCaptureParameters * _Nonnull)captureParams  NS_SWIFT_NAME(updateScreenCaptureParameters(_:));

/**
 * @brief Gets a list of shareable screens and windows.
 *
 * @details
 * You can call this method before sharing a screen or window to get a list of shareable screens and
 * windows, which enables a user to use thumbnails in the list to easily choose a particular screen
 * or window to share. This list also contains important information such as window ID and screen
 * ID, with which you can call `startScreenCaptureByWindowId:regionRect:captureParams:` or
 * `startScreenCaptureByDisplayId:regionRect:captureParams:` to start the sharing.
 *
 * @param thumbSize The target size of the screen or window thumbnail (the width and height are in
 * pixels). The SDK scales the original image to make the length of the longest side of the image
 * the same as that of the target size without distorting the original image. For example, if the
 * original image is 400 × 300 and thumbSize is 100 × 100, the actual size of the `thumbnail` is 100
 * × 75. If the target size is larger than the original size, the thumbnail is the original image
 * and the SDK does not scale it.
 * @param iconSize The target size of the icon corresponding to the application program (the width
 * and height are in pixels). The SDK scales the original image to make the length of the longest
 * side of the image the same as that of the target size without distorting the original image. For
 * example, if the original image is 400 × 300 and iconSize is 100 × 100, the actual size of the `
 * icon` is 100 × 75. If the target size is larger than the original size, the icon is the original
 * image and the SDK does not scale it.
 * @param includeScreen Whether the SDK returns the screen information in addition to the window
 * information:
 * - `YES`: The SDK returns screen and window information.
 * - `NO`: The SDK returns window information only.
 *
 * @return
 * The `AgoraScreenCaptureSourceInfo` array.
 */
- (NSArray<AgoraScreenCaptureSourceInfo*>* _Nullable)getScreenCaptureSourcesWithThumbSize:(NSSize)thumbSize iconSize:(NSSize)iconSize includeScreen:(BOOL)includeScreen NS_SWIFT_NAME(getScreenCaptureSources(withThumbSize:iconSize:includeScreen:));

#endif

/**
 * @brief Sets the screen sharing scenario.
 *
 * @details
 * When you start screen sharing or window sharing, you can call this method to set the screen
 * sharing scenario. The SDK adjusts the video quality and experience of the sharing according to
 * the scenario.
 *
 * @note Agora recommends that you call this method before joining a channel.
 *
 * @param scenarioType The screen sharing scenario. See `AgoraScreenScenarioType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setScreenCaptureScenario:(AgoraScreenScenarioType)scenarioType  NS_SWIFT_NAME(setScreenCaptureScenario(_:));

/**
 * @brief Stops screen capture.
 *
 * @details
 * Applicable scenarios:
 * - iOS: If you start screen capture by calling `startScreenCapture:`, you need to
 * call this method to stop screen capture.
 * - macOS: If you start screen capture by calling
 * `startScreenCaptureByWindowId:regionRect:captureParams:` or
 * `startScreenCaptureByDisplayId:regionRect:captureParams:`, you need to call this method to stop
 * screen capture.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopScreenCapture NS_SWIFT_NAME(stopScreenCapture());

#if TARGET_OS_IPHONE
/**
 * @brief Starts screen capture.
 *
 * @details
 * Applicable scenarios: In the screen sharing scenario, you need to call this method to start
 * capturing the screen video stream.
 * Call timing: You can call this method either before or after joining the channel, with the
 * following differences:
 * - Call this method first and then call
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join channel and set
 * `publishScreenCaptureVideo` to `YES` to start screen sharing.
 * - Call this method after joining a channel, then call `updateChannelWithMediaOptions:` and set
 * `publishScreenCaptureVideo` to `YES` to start screen sharing.
 * Related callbacks: When the state of the screen sharing extension process changes, the SDK
 * triggers the `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback accordingly.
 * When the type of `sourceType` is `AgoraVideoSourceTypeScreen`, the `state` parameter indicates
 * the state of the screen sharing.
 *
 * @note
 * - On the iOS platform, screen sharing is only available on iOS 12.0 and later.
 * - If you are using the custom audio source instead of the SDK to capture audio, Agora recommends
 * you add the keep-alive processing logic to your application to avoid screen sharing stopping when
 * the application goes to the background.
 * - This feature requires high-performance device, and Agora recommends that you use it on iPhone X
 * and later models.
 * - This method relies on the iOS screen sharing dynamic library
 * `AgoraReplayKitExtension.xcframework`. If the dynamic library is deleted, screen sharing cannot
 * be enabled normally.
 * - To avoid system audio capture failure when sharing screen, Agora recommends that you set the
 * audio application scenario to `AgoraAudioScenarioGameStreaming` by using the `setAudioScenario:`
 * method before joining the channel.
 * - The billing for the screen sharing stream is based on the `dimensions` in
 * `AgoraScreenVideoParameters`:
 *   - When you do not pass in a value, Agora bills you at 1280 × 720.
 *   - When you pass in a value, Agora bills you at that value.
 *
 * @param parameters The screen sharing encoding parameters. See `AgoraScreenCaptureParameters2`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2 (iOS platform): Empty parameter.
 */
- (int)startScreenCapture:(AgoraScreenCaptureParameters2* _Nullable)parameters  NS_SWIFT_NAME(startScreenCapture(_:));

/**
 * Updates the screen sharing configuration.
 *
 * @param parameters The configuration of the screen sharing.
 * @note This method applies to iOS 11+ only.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateScreenCapture:(AgoraScreenCaptureParameters2* _Nullable)parameters  NS_SWIFT_NAME(updateScreenCapture(_:));

/**
 * @brief Queries the highest frame rate supported by the device during screen sharing.
 *
 * @since v4.2.0
 *
 * @details
 * Applicable scenarios: To ensure optimal screen sharing performance, particularly in enabling high
 * frame rates like 60 fps, Agora recommends you to query the device's maximum supported frame rate
 * using this method beforehand. This way, if the device cannot support such a high frame rate, you
 * can adjust the screen sharing stream accordingly to avoid any negative impact on the sharing
 * quality. If the device does not support high frame rate, you can reduce the frame rate of the
 * screen sharing stream appropriately when sharing the screen to ensure that the sharing effect
 * meets your expectation.
 *
 * @return
 * - The highest frame rate supported by the device, if the method is called successfully. See
 * `AgoraScreenCaptureFrameRateCapability`.
 * - If the call fails, returns `nil`.
 */
- (AgoraScreenCaptureFrameRateCapability)queryScreenCaptureCapability NS_SWIFT_NAME(queryScreenCaptureCapability());

/**
 * @brief Queries the focal length capability supported by the camera.
 *
 * @details
 * If you want to enable the wide-angle or ultra-wide-angle mode for camera capture, it is
 * recommended to start by calling this method to check whether the device supports the required
 * focal length capability. Then, adjust the camera's focal length configuration based on the query
 * result by calling `setCameraCapturerConfiguration:`, ensuring the best camera capture
 * performance.
 *
 * @return
 * Returns an array of `AgoraFocalLengthInfo` objects, which contain the camera's orientation and
 * focal length type.
 */
- (NSArray<AgoraFocalLengthInfo *>* _Nullable)queryCameraFocalLengthCapability NS_SWIFT_NAME(queryCameraFocalLengthCapability());

#pragma mark Camera Control
/**
 * @brief Checks whether the device supports camera zoom.
 *
 * @details
 * Call timing: This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @return
 * - `YES`: The device supports camera zoom.
 * - `NO`: The device does not support camera zoom.
 */
- (BOOL)isCameraZoomSupported NS_SWIFT_NAME(isCameraZoomSupported());

/**
 * @brief Gets the maximum zoom ratio supported by the camera.
 *
 * @note - This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @return
 * The maximum zoom ratio supported by the camera.
 */
- (CGFloat)cameraMaxZoomFactor NS_SWIFT_NAME(cameraMaxZoomFactor());

/**
 * @brief Sets the camera zoom factor.
 *
 * @details
 * For iOS devices equipped with multi-lens rear cameras, such as those featuring dual-camera
 * (wide-angle and ultra-wide-angle) or triple-camera (wide-angle, ultra-wide-angle, and telephoto),
 * you can call `setCameraCapturerConfiguration:` first to set the `cameraFocalLengthType` as
 * `AgoraFocalLengthDefault` (0) (standard lens). Then, adjust the camera zoom factor to a value
 * less than 1.0. This configuration allows you to capture video with an ultra-wide-angle
 * perspective.
 *
 * @note - You must call this method after `enableVideo`. The setting result will take effect after
 * the camera is successfully turned on, that is, after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @param zoomFactor Camera zoom factor. For devices that do not support ultra-wide-angle, the value
 * ranges from 1.0 to the maximum zoom factor; for devices that support ultra-wide-angle, the value
 * ranges from 0.5 to the maximum zoom factor. You can get the maximum zoom factor supported by the
 * device by calling the `cameraMaxZoomFactor` method.
 *
 * @return
 * - The camera zoom `factor` value, if successful.
 * - < 0: if the method if failed.
 */
- (CGFloat)setCameraZoomFactor:(CGFloat)zoomFactor NS_SWIFT_NAME(setCameraZoomFactor(_:));

/**
 * @brief Check whether the device supports the manual focus function.
 *
 * @note - This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @return
 * - `YES`: The device supports the manual focus function.
 * - `NO`: The device does not support the manual focus function.
 */
- (BOOL)isCameraFocusPositionInPreviewSupported NS_SWIFT_NAME(isCameraFocusPositionInPreviewSupported());

/**
 * @brief Sets the camera manual focus position.
 *
 * @note
 * - You must call this method after `enableVideo`. The setting result will take effect after the
 * camera is successfully turned on, that is, after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 * - After a successful method call, the SDK triggers the `rtcEngine:cameraFocusDidChangedToRect:`
 * callback.
 *
 * @param position The coordinate of the touchpoint in the view.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (BOOL)setCameraFocusPositionInPreview:(CGPoint)position NS_SWIFT_NAME(setCameraFocusPositionInPreview(_:));

/**
 * @brief Checks whether the device supports manual exposure.
 *
 * @note - This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @return
 * - `YES`: The device supports manual exposure.
 * - `NO`: The device does not support manual exposure.
 */
- (BOOL)isCameraExposurePositionSupported NS_SWIFT_NAME(isCameraExposurePositionSupported());

/**
 * @brief Sets the camera exposure position.
 *
 * @note
 * - You must call this method after `enableVideo`. The setting result will take effect after the
 * camera is successfully turned on, that is, after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 * - After a successful method call, the SDK triggers the
 * `rtcEngine:cameraExposureDidChangedToRect:` callback.
 *
 * @param positionInView The horizontal coordinate of the touchpoint in the view.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (BOOL)setCameraExposurePosition:(CGPoint)positionInView NS_SWIFT_NAME(setCameraExposurePosition(_:));

/**
 * @brief Queries whether the current camera supports adjusting exposure value.
 *
 * @since v4.2.2
 *
 * @note
 * - This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 * - Before calling `setCameraExposureFactor:`, Agora recoomends that you call this method to query
 * whether the current camera supports adjusting the exposure value.
 * - By calling this method, you adjust the exposure value of the currently active camera, that is,
 * the camera specified when calling `setCameraCapturerConfiguration:`.
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)isCameraExposureSupported NS_SWIFT_NAME(isCameraExposureSupported());

/**
 * @brief Sets the camera exposure value.
 *
 * @since v4.2.2
 *
 * @details
 * Insufficient or excessive lighting in the shooting environment can affect the image quality of
 * video capture. To achieve optimal video quality, you can use this method to adjust the camera's
 * exposure value.
 *
 * @note
 * - You must call this method after `enableVideo`. The setting result will take effect after the
 * camera is successfully turned on, that is, after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 * - Before calling this method, Agora recommends calling `isCameraExposureSupported` to check
 * whether the current camera supports adjusting the exposure value.
 * - By calling this method, you adjust the exposure value of the currently active camera, that is,
 * the camera specified when calling `setCameraCapturerConfiguration:`.
 *
 * @param exposureFactor The camera exposure value. The default value is 0, which means using the
 * default exposure of the camera. The larger the value, the greater the exposure. When the video
 * image is overexposed, you can reduce the exposure value; when the video image is underexposed and
 * the dark details are lost, you can increase the exposure value. If the exposure value you
 * specified is beyond the range supported by the device, the SDK will automatically adjust it to
 * the actual supported range of the device.
 * The value range is [-8.0, 8.0].
 *
 * @return
 * Without practical meaning.
 */
- (CGFloat)setCameraExposureFactor:(CGFloat)exposureFactor NS_SWIFT_NAME(setCameraExposureFactor(_:));

/**
 * @brief Checks whether the device supports camera flash.
 *
 * @note
 * - This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 * - The app enables the front camera by default. If your front camera does not support flash, this
 * method returns NO. If you want to check whether the rear camera supports the flash function, call
 * `switchCamera` before this method.
 * - On iPads with system version 15, even if `isCameraTorchSupported` returns `YES`, you might fail
 * to successfully enable the flash by calling `setCameraTorchOn:` due to system issues.
 *
 * @return
 * - `YES`: The device supports camera flash.
 * - `NO`: The device does not support camera flash.
 */
- (BOOL)isCameraTorchSupported NS_SWIFT_NAME(isCameraTorchSupported());

/**
 * @brief Enables the camera flash.
 *
 * @note - You must call this method after `enableVideo`. The setting result will take effect after
 * the camera is successfully turned on, that is, after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @param isOn Whether to turn on the camera flash:
 * - `YES`: Turn on the flash.
 * - `NO`: (Default) Turn off the flash.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (BOOL)setCameraTorchOn:(BOOL)isOn  NS_SWIFT_NAME(setCameraTorchOn(_:));

/**
 * @brief Checks whether the device supports the face auto-focus function.
 *
 * @note - This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @return
 * - `YES`: The device supports the face auto-focus function.
 * - `NO`: The device does not support the face auto-focus function.
 */
- (BOOL)isCameraAutoFocusFaceModeSupported NS_SWIFT_NAME(isCameraAutoFocusFaceModeSupported());

/**
 * @brief Enables the camera auto-face focus function.
 *
 * @details
 * The SDK enables face autofocus by default. To disable face autofocus, call this method.
 * Call timing: This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @param enable Whether to enable face autofocus:
 * - `YES`: Enable the camera auto-face focus function.
 * - `NO`: Disable face auto-focus.
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)setCameraAutoFocusFaceModeEnabled:(BOOL)enable NS_SWIFT_NAME(setCameraAutoFocusFaceModeEnabled(_:));

/**
 * @brief Checks whether the device supports auto exposure.
 *
 * @note - This method must be called after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @return
 * - `YES`: The device supports auto exposure.
 * - `NO`: The device does not support auto exposure.
 */
- (BOOL)isCameraAutoExposureFaceModeSupported NS_SWIFT_NAME(isCameraAutoExposureFaceModeSupported());

/**
 * @brief Sets whether to enable auto exposure.
 *
 * @note - You must call this method after `enableVideo`. The setting result will take effect after
 * the camera is successfully turned on, that is, after the SDK triggers the
 * `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and returns the local video
 * state as `AgoraVideoLocalStateCapturing` (1).
 *
 */
- (BOOL)setCameraAutoExposureFaceModeEnabled:(BOOL)enable NS_SWIFT_NAME(setCameraAutoExposureFaceModeEnabled(_:));

/**
 * @brief Switches between front and rear cameras.
 *
 * @details
 * You can call this method to dynamically switch cameras based on the actual camera availability
 * during the app's runtime, without having to restart the video stream or reconfigure the video
 * source.
 * Call timing: This method must be called after the camera is successfully enabled, that is, after
 * the SDK triggers the `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and
 * returns the local video state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @note This method only switches the camera for the video stream captured by the first camera,
 * that is, the video source set to `AgoraVideoSourceTypeCamera` (0) when calling
 * `startCameraCapture:config:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)switchCamera NS_SWIFT_NAME(switchCamera());

/**
 * @brief Enables or disables multi-camera capture.
 *
 * @details
 * In scenarios where there are existing cameras to capture video, Agora recommends that you use the
 * following steps to capture and publish video with multiple cameras:1. Call this method to enable
 * multi-channel camera capture.
 * 2. Call `startPreview:` to start the local video preview.
 * 3. Call `startCameraCapture:config:`, and set `sourceType` to start video capture with the second
 * camera.
 * 4. Call `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:`, and set
 * `publishSecondaryCameraTrack` to `YES` to publish the video stream captured by the second camera
 * in the channel.
 * If you want to disable multi-channel camera capture, use the following steps:1. Call
 * `stopCameraCapture:`.
 * 2. Call this method with `enabled` set to `NO`.
 * When using this function, ensure that the system version is 13.0 or later.
 * The minimum iOS device types that support multi-camera capture are as follows:
 * - iPhone XR
 * - iPhone XS
 * - iPhone XS Max
 * - iPad Pro 3rd generation and later
 *
 * @note
 * You can call this method before and after `startPreview:` to enable multi-camera capture:
 * - If it is enabled before `startPreview:`, the local video preview shows the image captured by
 * the two cameras at the same time.
 * - If it is enabled after `startPreview:`, the SDK stops the current camera capture first, and
 * then enables the primary camera and the second camera. The local video preview appears black for
 * a short time, and then automatically returns to normal.
 *
 * @param enabled Whether to enable multi-camera video capture mode:
 * - `YES`: Enable multi-camera capture mode; the SDK uses multiple cameras to capture video.
 * - `NO`: Disable multi-camera capture mode; the SDK uses a single camera to capture video.
 * @param config Capture configuration for the second camera. See
 * `AgoraCameraCapturerConfiguration`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableMultiCamera:(BOOL)enabled config:(AgoraCameraCapturerConfiguration* _Nullable)config NS_SWIFT_NAME(enableMultiCamera(_:config:));

 /**
  * @brief Sets the camera stabilization mode.
  *
  * @details
  * The camera stabilization mode is off by default. You need to call this method to turn it on and
  * set the appropriate stabilization mode.
  * Applicable scenarios: When shooting on the move, in low light conditions, or with mobile devices,
  * you can set the camera stabilization mode to reduce the impact of camera shake and get a more
  * stable, clear picture.
  * Call timing: This method must be called after the camera is successfully enabled, that is, after
  * the SDK triggers the `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and
  * returns the local video state as `AgoraVideoLocalStateCapturing` (1).
  *
  * @note
  * - Camera stabilization only works for scenarios with a video resolution greater than 1280 x 720.
  * - After enabling camera stabilization, the higher the camera stabilization level, the smaller the
  * camera's field of view and the greater the camera's latency. To improve user experience, it is
  * recommended that you set the `mode` parameter to `AgoraCameraStabilizationModeLevel1`.
  *
  * @param mode Camera stabilization mode. See `AgoraCameraStabilizationMode`.
  *
  * @return
  * - 0: Success.
  * - < 0: Failure.
  */
 - (int) setCameraStabilizationMode:(AgoraCameraStabilizationMode)mode NS_SWIFT_NAME(setCameraStabilizationMode(_:));
#endif
/**
 * @brief Checks if the camera supports portrait center stage.
 *
 * @details
 * Before calling `enableCameraCenterStage:` to enable portrait center stage, it is recommended to
 * call this method to check if the current device supports the feature.
 * Call timing: This method must be called after the camera is successfully enabled, that is, after
 * the SDK triggers the `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and
 * returns the local video state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @return
 * - `YES`: The current camera supports the portrait center stage.
 * - `NO`: The current camera supports the portrait center stage.
 */
- (BOOL)isCameraCenterStageSupported NS_SWIFT_NAME(isCameraCenterStageSupported());

/**
 * @brief Enables or disables portrait center stage.
 *
 * @details
 * The portrait center stage feature is off by default. You need to call this method to turn it on.
 * If you need to disable this feature, you need to call this method again and set `enabled` to
 * `NO`.
 * Applicable scenarios: The portrait center stage feature can be widely used in scenarios such as
 * online meetings, shows, online education, etc. The host can use this feature to ensure that they
 * are always in the center of the screen, whether they move or not, in order to achieve a good
 * display effect.
 * Call timing: This method must be called after the camera is successfully enabled, that is, after
 * the SDK triggers the `rtcEngine:localVideoStateChangedOfState:reason:sourceType:` callback and
 * returns the local video state as `AgoraVideoLocalStateCapturing` (1).
 *
 * @note
 * Due to the high performance requirements of this feature, you need to use it on the following
 * types of devices or devices with higher performance:
 * - iPad:
 *   - 12.9-inch iPad Pro (5th generation)
 *   - 11-inch iPad Pro (3rd generation)
 *   - iPad (9th generation)
 *   - iPad mini (6th generation)
 *   - iPad Air (5th generation)
 * - 2020 M1 MacBook Pro 13-inch + iPhone 11 (using iPhone as external camera for the MacBook)
 * Agora recommends that you call `isCameraCenterStageSupported` to check whether the current device
 * supports portrait center stage before enabling this feature.
 *
 * @param enabled Whether to enable the portrait center stage:
 * - `YES`: Enable portrait center stage.
 * - `NO`: Disable portrait center stage.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableCameraCenterStage:(BOOL)enabled NS_SWIFT_NAME(enableCameraCenterStage(_:));

/**
 * @brief Sets the camera capture configuration.
 *
 * @details
 * Call timing: Call this method before enabling local camera capture, such as before calling
 * `startPreview:` and `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`.
 *
 * @note To adjust the camera focal length configuration, It is recommended to call
 * `queryCameraFocalLengthCapability` first to check the device's focal length capabilities, and
 * then configure based on the query results.
 *
 * @param config The camera capture configuration. See `AgoraCameraCapturerConfiguration`.Attention:
 * In this method, you do not need to set the `deviceId` parameter.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setCameraCapturerConfiguration:(AgoraCameraCapturerConfiguration * _Nullable)config NS_SWIFT_NAME(setCameraCapturerConfiguration(_:));

/**
 * @brief Starts camera capture.
 *
 * @details
 * You can call this method to start capturing video from one or more cameras by specifying
 * `sourceType`.
 *
 * @note On iOS, if you want to enable multi-camera capture, you need to call
 * `enableMultiCamera:config:` and set `enabled` to `YES` before calling this method.
 *
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 * Note:
 * - On iOS, you can capture video from up to 2 cameras, provided the device has multiple
 * cameras or supports external cameras.
 * - On macOS, you can capture video from up to 4 cameras.
 * @param config The configuration of the video capture. See `AgoraCameraCapturerConfiguration`.
 * Note: On iOS, this parameter has no practical function. Use the `config` parameter
 * in `enableMultiCamera:config:` instead to set the video capture configuration.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startCameraCapture:(AgoraVideoSourceType)sourceType config:(AgoraCameraCapturerConfiguration * _Nullable)config NS_SWIFT_NAME(startCameraCapture(_:config:));

/**
 * @brief Stops camera capture.
 *
 * @details
 * After calling `startCameraCapture:config:` to start capturing video through one or more cameras,
 * you can call this method and set the `sourceType` parameter to stop the capture from the
 * specified cameras.
 *
 * @note
 * - If you are using the local video mixing function, calling this method can cause the local video
 * mixing to be interrupted.
 * - On iOS, if you want to disable multi-camera capture, you need to call
 * `enableMultiCamera:config:` after calling this method and set `enabled` to `NO`.
 *
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopCameraCapture:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(stopCameraCapture(_:));

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
#pragma mark macOS Device

/**
 * @brief Monitors the change of the device state.
 *
 * @details
 * This method can monitor the plugging and swapping of external audio or video devices, for
 * example, an external camera.
 *
 * @param enabled Whether to monitor the change of the device state:
 * - `YES`: Enable device state monitoring.
 * - `NO`: Disable device state monitoring.
 *
 */
- (void)monitorDeviceChange:(BOOL)enabled NS_SWIFT_NAME(monitorDeviceChange(_:));

/**
 * @brief Enumerates all the audio and video devices in the system.
 *
 * @details
 * This method returns an NSArray object that includes all audio and video devices in the system.
 * Apps can enumerate devices with the `AgoraRtcDeviceInfo` NSArray object.
 *
 * @note Do not call this method in the main thread.
 *
 * @param type The device type, which includes the audio capturing, audio playback, video capturing,
 * or video playback device. See `AgoraMediaDeviceType`.
 *
 * @return
 * An `AgoraRtcDeviceInfo` NSArray object that includes all audio and video devices if the method
 * succeeds.
 */
- (NSArray<AgoraRtcDeviceInfo *> * _Nullable)enumerateDevices:(AgoraMediaDeviceType)type NS_SWIFT_NAME(enumerateDevices(_:));

/**
 * @brief Retrieves the name of the current device.
 *
 * @details
 * This method retrieves the current audio and video capturing device based on your settings in the
 * `type` parameter.
 *
 * @param type The device type, which includes the audio capturing, audio playback, video capturing,
 * or video playback device. See `AgoraMediaDeviceType`.
 *
 * @return
 * - Returns `AgoraRtcDeviceInfo` when the method succeeds.
 * - Returns `nil` when the method fails.
 */
- (AgoraRtcDeviceInfo * _Nullable)getDeviceInfo:(AgoraMediaDeviceType)type NS_SWIFT_NAME(getDeviceInfo(_:));

/**
 * @brief Specifies a device with the device ID.
 *
 * @param type The device type, which includes the audio capturing, audio playback, video capturing,
 * or video playback device. See `AgoraMediaDeviceType`.
 * @param deviceId The device ID. You can get the Device ID by calling `enumerateDevices:`.
 * Connecting or disconnecting the audio device does not change the value of `deviceId`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDevice:(AgoraMediaDeviceType)type deviceId:(NSString * _Nonnull)deviceId NS_SWIFT_NAME(setDevice(_:deviceId:));

/**
 * @brief Gets the default audio device of the system.
 *
 * @param type Output parameter; the device type, see `AgoraMediaDeviceType`.
 *
 * @return
 * - Returns the `AgoraRtcDeviceInfo` object containing the default audio device information when
 * the call succeeds.
 * - Returns `nil` when the call fails.
 */
- (AgoraRtcDeviceInfo* _Nullable)getDefaultAudioDevice:(AgoraMediaDeviceType)type NS_SWIFT_NAME(getDefaultAudioDevice(_:));

/**
 * @brief Retrieves the volume of the current device.
 *
 * @param type The device type, which includes the audio capturing, audio playback, video capturing,
 * or video playback device. See `AgoraMediaDeviceType`.
 *
 * @return
 * - Returns the volume of the current device, if the method succeeds.
 * - < 0: Failure.
 */
- (int)getDeviceVolume:(AgoraMediaDeviceType)type NS_SWIFT_NAME(getDeviceVolume(_:));

/**
 * @brief Sets the volume of the specified device.
 *
 * @details
 * Sets the volume of audio or video capture or playback devices.
 *
 * @note Call this method after joining a channel.
 *
 * @param type The device type, which includes the audio capturing, audio playback, video capturing,
 * or video playback device. See `AgoraMediaDeviceType`.
 * @param volume The volume of the specified device. The value range is [0,255].
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDeviceVolume:(AgoraMediaDeviceType)type volume:(int)volume NS_SWIFT_NAME(setDeviceVolume(_:volume:));

/**
 * @brief Starts an audio device loopback test.
 *
 * @details
 * This method tests whether the local audio capture device and playback device are working
 * properly. After starting the test, the audio capture device records the local audio, and the
 * audio playback device plays the captured audio. The SDK triggers two independent
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callbacks at the time interval set
 * in this method, which reports the volume information of the capture device ( `uid` = 0) and the
 * volume information of the playback device ( `uid` = 1) respectively.
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - This method only takes effect when called by the host.
 * - This method tests local audio devices and does not report the network conditions.
 * - When you finished testing, call `stopAudioDeviceLoopbackTest` to stop the audio device loopback
 * test.
 *
 * @param indicationInterval The time interval (ms) at which the SDK triggers the
 * `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback. Agora recommends setting
 * a value greater than 200 ms. This value must not be less than 10 ms; otherwise, you can not
 * receive the `rtcEngine:reportAudioVolumeIndicationOfSpeakers:totalVolume:` callback.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startAudioDeviceLoopbackTest:(int)indicationInterval NS_SWIFT_NAME(startAudioDeviceLoopbackTest(_:));

/**
 * @brief Stops the audio device loopback test.
 *
 * @note
 * - You can call this method either before or after joining a channel.
 * - This method only takes effect when called by the host.
 * - Ensure that you call this method to stop the loopback test after calling the
 * `startAudioDeviceLoopbackTest:` method.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopAudioDeviceLoopbackTest NS_SWIFT_NAME(stopAudioDeviceLoopbackTest());

/**
 * @brief Sets the audio playback device used by the SDK to follow the system default audio playback
 * device.
 *
 * @param enable Whether to follow the system default audio playback device:
 * - `YES`: Follow the system default audio playback device. The SDK immediately switches the audio
 * playback device when the system default audio playback device changes.
 * - `NO`: Do not follow the system default audio playback device. The SDK switches the audio
 * playback device to the system default audio playback device only when the currently used audio
 * playback device is disconnected.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)followSystemPlaybackDevice:(BOOL)enable NS_SWIFT_NAME(followSystemPlaybackDevice(_:));

/**
 * @brief Sets the audio recording device used by the SDK to follow the system default audio
 * recording device.
 *
 * @param enable Whether to follow the system default audio recording device:
 * - `YES`: Follow the system default audio playback device. The SDK immediately switches the audio
 * recording device when the system default audio recording device changes.
 * - `NO`: Do not follow the system default audio playback device. The SDK switches the audio
 * recording device to the system default audio recording device only when the currently used audio
 * recording device is disconnected.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)followSystemRecordingDevice:(BOOL)enable NS_SWIFT_NAME(followSystemRecordingDevice(_:));

/** Starts the capture device test.

 @note  This method applies to macOS only, not to iOS.

 This method tests whether the current video capture device works properly. Ensure that you have called enableVideo before calling this method and that the parameter view window is valid.

 @param view Input parameter, for displaying the video window.
 */
- (int)startCaptureDeviceTest:(NSView * _Nonnull)view NS_SWIFT_NAME(startCaptureDeviceTest(_:));

/** Stops the capture device test.

 @note  This method applies to macOS only, not to iOS.

 This method stops testing the capture device. You must call this method to stop the test after calling [startCaptureDeviceTest](startCaptureDeviceTest:).
 */
- (int)stopCaptureDeviceTest NS_SWIFT_NAME(stopCaptureDeviceTest());
#endif

#pragma mark Face Detection

#if TARGET_OS_IPHONE
/**
 * @brief Enables or disables face detection for the local user.
 *
 * @details
 * Call timing: This method needs to be called after the camera is started (for example, by calling
 * `startPreview:` or `enableVideo` ).
 * Related callbacks: Once face detection is enabled, the SDK triggers the
 * `rtcEngine:facePositionDidChangeWidth:previewHeight:faces:` callback to report the face
 * information of the local user, which includes the following:
 * - The width and height of the local video.
 * - The position of the human face in the local view.
 * - The distance between the human face and the screen.
 *
 * @param enable Whether to enable face detection for the local user:
 * - `YES`: Enable face detection.
 * - `NO`: (Default) Disable face detection.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableFaceDetection:(BOOL)enable  NS_SWIFT_NAME(enableFaceDetection(_:));
#endif

#pragma mark Watermark

/**
 * @brief Adds a watermark image to the local video.
 *
 * @details
 * This method adds a PNG watermark image to the local video stream in a live streaming session.
 * Once the watermark image is added, all the users in the channel (CDN audience included) and the
 * video capturing device can see and capture it. If you only want to add a watermark to the CDN
 * live streaming, see `startRtmpStreamWithTranscoding:transcoding:`.
 *
 * @note
 * - The URL descriptions are different for the local video and CDN live streaming: In a local video
 * stream, URL refers to the absolute path of the added watermark image file in the local video
 * stream. In a CDN live stream, URL refers to the URL address of the added watermark image in the
 * CDN live streaming.
 * - The source file of the watermark image must be in the PNG file format. If the width and height
 * of the PNG file differ from your settings in this method, the PNG file will be cropped to conform
 * to your settings.
 * - The Agora SDK supports adding only one watermark image onto a local video or CDN live stream.
 * The newly added watermark image replaces the previous one.
 *
 * @param watermark The watermark image to be added to the local live streaming: `AgoraImage`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)addVideoWatermark:(AgoraImage * _Nonnull)watermark NS_SWIFT_NAME(addVideoWatermark(_:)) __deprecated_msg("use addVideoWatermarkWithConfig:config instead.");

/**
 * @brief Adds a watermark image to the local video.
 *
 * @details
 * This method adds a PNG watermark image to the local video in the live streaming. Once the
 * watermark image is added, all the audience in the channel (CDN audience included), and the
 * capturing device can see and capture it. The Agora SDK supports adding only one watermark image
 * onto a live video stream. The newly added watermark image replaces the previous one.
 * The watermark coordinates are dependent on the settings in the `setVideoEncoderConfiguration:`
 * method:
 * - If the orientation mode of the encoding video ( `AgoraVideoOutputOrientationMode` ) is fixed
 * landscape mode or the adaptive landscape mode, the watermark uses the landscape orientation.
 * - If the orientation mode of the encoding video ( `AgoraVideoOutputOrientationMode` ) is fixed
 * portrait mode or the adaptive portrait mode, the watermark uses the portrait orientation.
 * - When setting the watermark position, the region must be less than the dimensions set in the
 * `setVideoEncoderConfiguration:` method; otherwise, the watermark image will be cropped.
 * You can control the visibility of the watermark during preview by setting the `visibleInPreview`
 * parameter when calling this method. However, whether it ultimately takes effect also depends on
 * the position parameter you set when calling `setupLocalVideo:` (the ` position` of the video
 * frame in the video link). Refer to the table below for details.
 * | Observation position                            | visibleInPreview value | Watermark visibility |
 * | ----------------------------------------------- | ---------------------- | -------------------- |
 * | (Default) `AgoraVideoModulePositionPostCapture` | `YES`                  | Yes                  |
 * |                                                 | `NO`                   | No                   |
 * | `AgoraVideoModulePositionPreEncoder`            | `YES`                  | Yes                  |
 * |                                                 | `NO`                   | Yes                  |
 *
 * @note
 * - Ensure that calling this method after `enableVideo`.
 * - If you only want to add a watermark to the media push, you can call this method or the
 * `startRtmpStreamWithTranscoding:transcoding:` method.
 * - This method supports adding a watermark image in the PNG file format only. Supported pixel
 * formats of the PNG image are RGBA, RGB, Palette, Gray, and Alpha_gray.
 * - If the dimensions of the PNG image differ from your settings in this method, the image will be
 * cropped or zoomed to conform to your settings.
 * - If you have enabled the mirror mode for the local video, the watermark on the local video is
 * also mirrored. To avoid mirroring the watermark, Agora recommends that you do not use the mirror
 * and watermark functions for the local video at the same time. You can implement the watermark
 * function in your application layer.
 *
 * @param url The local file path of the watermark image to be added. This method supports adding a
 * watermark image from the local absolute or relative file path.
 * @param options The options of the watermark image to be added. See `WatermarkOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)addVideoWatermark:(NSURL* _Nonnull)url options:(WatermarkOptions* _Nonnull)options NS_SWIFT_NAME(addVideoWatermark(_:options:)) __deprecated_msg("use addVideoWatermarkWithConfig:config instead.");

/**
 * @brief Adds a watermark image to the local video.
 *
 * @since 4.6.0
 *
 * @details
 * You can use this method to overlay a watermark image on the local video stream, and configure the
 * watermark's position, size, and visibility in the preview using `WatermarkConfig`.
 *
 * @param config Configuration of the watermark image. See `WatermarkConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)addVideoWatermarkWithConfig:(WatermarkConfig* _Nonnull)config NS_SWIFT_NAME(addVideoWatermark(_:));

/**
 * @brief Removes the watermark image from the local video.
 *
 * @since 4.6.0
 *
 * @details
 * This method removes a previously added watermark image from the local video stream using the
 * specified unique ID.
 *
 * @param id The identifier of the watermark to be removed.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)removeVideoWatermark:(NSString* _Nonnull)id NS_SWIFT_NAME(removeVideoWatermark(_:));

/**
 * @brief Removes the watermark image from the video stream.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)clearVideoWatermarks NS_SWIFT_NAME(clearVideoWatermarks());

#pragma mark String UID
/**
 * @brief Registers a user account.
 *
 * @details
 * Once registered, the user account can be used to identify the local user when the user joins the
 * channel. After the registration is successful, the user account can identify the identity of the
 * local user, and the user can use it to join the channel.
 * This method is optional. If you want to join a channel using a user account, you can choose one
 * of the following methods:
 * - Call the `registerLocalUserAccountWithAppID:userAccount:` method to register a user account,
 * and then call the `joinChannelByToken:channelId:userAccount:joinSuccess:` or
 * `joinChannelByToken:channelId:userAccount:mediaOptions:joinSuccess:` method to join a channel,
 * which can shorten the time it takes to enter the channel.
 * - Call the `joinChannelByToken:channelId:userAccount:joinSuccess:` or
 * `joinChannelByToken:channelId:userAccount:mediaOptions:joinSuccess:` method to join a channel.
 * Related callbacks: After successfully calling this method, the
 * `rtcEngine:didLocalUserRegisteredWithUserId:userAccount:` callback will be triggered on the local
 * client to report the local user's UID and user account.
 *
 * @note
 * - Starting from v4.6.0, the SDK will no longer automatically map Int UID to the String
 * `userAccount` used when registering a User Account. If you want to join a channel with the
 * original String `userAccount` used during registration, call the
 * `joinChannelByToken:channelId:userAccount:mediaOptions:joinSuccess:` method to join the channel,
 * instead of calling `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` and pass in the
 * Int UID obtained through this method
 * - Ensure that the `userAccount` is unique in the channel.
 * - To ensure smooth communication, use the same parameter type to identify the user. For example,
 * if a user joins the channel with a UID, then ensure all the other users use the UID too. The same
 * applies to the user account. If a user joins the channel with the Agora Web SDK, ensure that the
 * ID of the user is set to the same parameter type.
 *
 * @param appID The App ID of your project on Agora Console.
 * @param userAccount The user account. This parameter is used to identify the user in the channel
 * for real-time audio and video engagement. You need to set and manage user accounts yourself and
 * ensure that each user account in the same channel is unique. The maximum length of this parameter
 * is 255 bytes. Ensure that you set this parameter and do not set it as nil. Supported characters
 * are as follow(89 in total):
 * - The 26 lowercase English letters: a to z.
 * - The 26 uppercase English letters: A to Z.
 * - All numeric characters: 0 to 9.
 * - Space
 * - "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]",
 * "^", "_", "{", "}", "|", "~", ","
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)registerLocalUserAccount:(NSString* _Nonnull)userAccount appId:(NSString* _Nonnull)appId  NS_SWIFT_NAME(registerLocalUserAccount(_:appId:));

/**
 * @brief Joins a channel with a User Account and Token.
 *
 * @details
 * Before calling this method, if you have not called
 * `registerLocalUserAccountWithAppID:userAccount:` to register a user account, when you call this
 * method to join a channel, the SDK automatically creates a user account for you. Calling the
 * `registerLocalUserAccountWithAppID:userAccount:` method to register a user account, and then
 * calling this method to join a channel can shorten the time it takes to enter the channel.
 * Once a user joins the channel, the user subscribes to the audio and video streams of all the
 * other users in the channel by default, giving rise to usage and billings. To stop subscribing to
 * a specified stream or all remote streams, call the corresponding `mute` methods.
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
- (int)joinChannelByToken:(NSString * _Nullable)token
                channelId:(NSString * _Nonnull)channelId
              userAccount:(NSString * _Nonnull)userAccount
              joinSuccess:(void(^ _Nullable)(NSString * _Nonnull channel, NSUInteger uid, NSInteger elapsed))joinSuccessBlock NS_SWIFT_NAME(joinChannel(byToken:channelId:userAccount:joinSuccess:));

/**
 * @brief Join a channel using a user account and token, and set the media options.
 *
 * @details
 * Before calling this method, if you have not called
 * `registerLocalUserAccountWithAppID:userAccount:` to register a user account, when you call this
 * method to join a channel, the SDK automatically creates a user account for you. Calling the
 * `registerLocalUserAccountWithAppID:userAccount:` method to register a user account, and then
 * calling this method to join a channel can shorten the time it takes to enter the channel.
 * Compared to `joinChannelByToken:channelId:userAccount:joinSuccess:`, this method has the
 * `options` parameter which is used to set media options, such as whether to publish audio and
 * video streams within a channel. By default, the user subscribes to the audio and video streams of
 * all the other users in the channel, giving rise to usage and **billings**. To stop subscribing to
 * other streams, set the `options` parameter or call the corresponding `mute` methods.
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
- (int)joinChannelByToken:(NSString * _Nullable)token
                channelId:(NSString * _Nonnull)channelId
              userAccount:(NSString * _Nonnull)userAccount
             mediaOptions:(AgoraRtcChannelMediaOptions * _Nonnull)mediaOptions
              joinSuccess:(void(^ _Nullable)(NSString * _Nonnull channel, NSUInteger uid, NSInteger elapsed))joinSuccessBlock NS_SWIFT_NAME(joinChannel(byToken:channelId:userAccount:mediaOptions:joinSuccess:));

/**
 * @brief Gets the user information by passing in the user account.
 *
 * @details
 * After a remote user joins the channel, the SDK gets the UID and user account of the remote user,
 * caches them in a mapping table object, and triggers the
 * `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:` callback on the local client. After receiving
 * the callback, you can call this method and pass in the user account to get the UID of the remote
 * user from the `AgoraUserInfo` object.
 * Call timing: Call this method after receiving the
 * `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:` callback.
 * Related callbacks: `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:`
 *
 * @param userAccount The user account.
 * @param error Error code.
 *
 * @return
 * - A pointer to the `AgoraUserInfo` instance, if the method call succeeds.
 * - If the call fails, returns nil.
 */
- (AgoraUserInfo* _Nullable)getUserInfoByUserAccount:(NSString* _Nonnull)userAccount withError:(AgoraErrorCode* _Nullable)error NS_SWIFT_NAME(getUserInfo(byUserAccount:withError:));

/**
 * @brief Gets the user information by passing in the user ID.
 *
 * @details
 * After a remote user joins the channel, the SDK gets the UID and user account of the remote user,
 * caches them in a mapping table object, and triggers the
 * `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:` callback on the local client. After receiving
 * the callback, you can call this method and pass in the UID to get the user account of the
 * specified user from the `AgoraUserInfo` object.
 * Call timing: Call this method after receiving the
 * `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:` callback.
 * Related callbacks: `rtcEngine:didUserInfoUpdatedWithUserId:userInfo:`
 *
 * @param uid The user ID.
 * @param error Error code.
 *
 * @return
 * - A pointer to the `AgoraUserInfo` instance, if the method call succeeds.
 * - If the call fails, returns nil.
 */
- (AgoraUserInfo* _Nullable)getUserInfoByUid:(NSUInteger)uid withError:(AgoraErrorCode* _Nullable)error NS_SWIFT_NAME(getUserInfo(byUid:withError:));

#pragma mark Custom Audio PCM Frame
/**
 * @brief Registers an audio frame observer object.
 *
 * @details
 * Call this method to register an audio frame observer object (register a callback). When you need
 * the SDK to trigger the `onMixedAudioFrame:channelId:`, `onRecordAudioFrame:channelId:`,
 * `onPlaybackAudioFrame:`, `onPlaybackAudioFrameBeforeMixing:channelId:uid:` or
 * `onEarMonitoringAudioFrame:` callback, you need to use this method to register the callbacks.
 * Call timing: Call this method before joining a channel.
 *
 * @param delegate The observer instance. See `AgoraAudioFrameDelegate`. Set the value as nil to
 * release the instance. Agora recommends calling this method after receiving
 * `rtcEngine:didLeaveChannelWithStats:` to release the audio observer object.
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)setAudioFrameDelegate:(id<AgoraAudioFrameDelegate> _Nullable)delegate NS_SWIFT_NAME(setAudioFrameDelegate(_:));

#pragma mark Custom Video Frame
/**
 * @brief Registers a raw video frame observer object.
 *
 * @details
 * If you want to observe raw video frames (such as YUV or RGBA format), Agora recommends that you
 * implement one `AgoraVideoFrameDelegate` class with this method.
 * When calling this method to register a video observer, you can register callbacks in the
 * `AgoraVideoFrameDelegate` class as needed. After you successfully register the video frame
 * observer, the SDK triggers the registered callbacks each time a video frame is received.
 * Applicable scenarios: After registering the raw video observer, you can use the obtained raw
 * video data in various video pre-processing scenarios, such as virtual backgrounds and image
 * enhacement by yourself.
 * Agora provides an open source iOS sample project `BeautyAPI` on GitHub for your reference.
 * Call timing: Call this method before joining a channel.
 *
 * @note
 * When handling the video data returned in the callbacks, pay attention to the changes in the
 * `width` and `height` parameters, which may be adapted under the following circumstances:
 * - When network conditions deteriorate, the video resolution decreases incrementally.
 * - If the user adjusts the video profile, the resolution of the video returned in the callbacks
 * also changes.
 *
 * @param delegate The observer instance. See `AgoraVideoFrameDelegate`. To release the instance,
 * set the value as nil.
 *
 * @return
 * - < 0: Failure.
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)setVideoFrameDelegate:(id<AgoraVideoFrameDelegate> _Nullable)delegate NS_SWIFT_NAME(setVideoFrameDelegate(_:));

#pragma mark Encoded Video Frame
/**
 * @brief Registers a receiver object for the encoded video image.
 *
 * @details
 * If you only want to observe encoded video frames (such as H.264 format) without decoding and
 * rendering the video, Agora recommends that you implement one `AgoraEncodedVideoFrameDelegate`
 * class through this method.
 *
 * @note Call this method before joining a channel.
 *
 * @param delegate The observer instance. See `AgoraEncodedVideoFrameDelegate`. Set the value as nil
 * to release the instance.
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)setEncodedVideoFrameDelegate:(id<AgoraEncodedVideoFrameDelegate> _Nullable)delegate NS_SWIFT_NAME(setEncodedVideoFrameDelegate(_:));

#pragma mark Custom Face Info

/**
 * @brief Registers or unregisters a facial information observer.
 *
 * @details
 * You can call this method to register the `onFaceInfo:` callback to receive the facial information
 * processed by Agora speech driven extension. When calling this method to register a facial
 * information observer, you can register callbacks in the `AgoraFaceInfoDelegate` class as needed.
 * After successfully registering the facial information observer, the SDK triggers the callback you
 * have registered when it captures the facial information converted by the speech driven extension.
 * Applicable scenarios: Facial information processed by the Agora speech driven extension is BS
 * (Blend Shape) data that complies with ARkit standards. You can further process the BS data using
 * third-party 3D rendering engines, such as driving avatar to make mouth movements corresponding to
 * speech.
 *
 * @note
 * - Call this method before joining a channel.
 * - Before calling this method, you need to make sure that the speech driven extension has been
 * enabled by calling `enableExtensionWithVendor:extension:enabled:sourceType:`.
 *
 * @param delegate Facial information observer, see `AgoraFaceInfoDelegate`. If you need to
 * unregister a facial information observer, pass in nil.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (BOOL)setFaceInfoDelegate:(id<AgoraFaceInfoDelegate> _Nullable)delegate NS_SWIFT_NAME(setFaceInfoDelegate(_:));

#pragma mark Custom Media Metadata
/**
 * @brief Sets the data source of the metadata.
 *
 * @details
 * You need to implement the `AgoraMediaMetadataDataSource` protocol in this method and specify the
 * data format of the metadata. After a successful method call, the SDK triggers the
 * `metadataMaxSize` callback.
 * This method can be used with `setMediaMetadataDelegate:withType:` to add synchronized metadata in
 * the video stream for more diversified live interactions, such as sending shopping links, digital
 * coupons, and online quizzes.
 *
 * @note Call this method before joining a channel.
 *
 * @param metadataDataSource The `AgoraMediaMetadataDataSource` protocol.
 * @param type The metadata type. The SDK currently only supports `AgoraMetadataTypeVideo`. See
 * `AgoraMetadataType`.
 *
 * @return
 * - `YES`: Success.
 * - `NO`: Failure.
 */
- (BOOL)setMediaMetadataDataSource:(id<AgoraMediaMetadataDataSource> _Nullable)metadataDataSource withType:(AgoraMetadataType)type NS_SWIFT_NAME(setMediaMetadataDataSource(_:with:));

/**
 * @brief Registers the metadata observer.
 *
 * @details
 * You need to implement the `AgoraMediaMetadataDelegate` class and specify the metadata type in
 * this method. This method enables you to add synchronized metadata in the video stream for more
 * diversified
 *  live interactive streaming, such as sending shopping links, digital coupons, and online quizzes.
 * A successful call of this method triggers the `metadataMaxSize` callback.
 *
 * @note Call this method before `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`.
 *
 * @param observer The metadata observer. See `AgoraMediaMetadataDelegate`.
 * @param type The metadata type. The SDK currently only supports `AgoraMetadataTypeVideo`. See
 * `AgoraMetadataType`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (BOOL)setMediaMetadataDelegate:(id<AgoraMediaMetadataDelegate> _Nullable)metadataDelegate withType:(AgoraMetadataType)type NS_SWIFT_NAME(setMediaMetadataDelegate(_:with:));

#pragma mark Miscellaneous Methods
/**
 * @brief Gets the SDK version.
 *
 * @return
 * The SDK version number. The format is a string.
 */
+ (NSString * _Nonnull)getSdkVersion NS_SWIFT_NAME(getSdkVersion());

/**
 * @brief Gets the warning or error description.
 *
 * @param error The error code reported by the SDK.
 *
 * @return
 * The specific error description.
 */
+ (NSString* _Nonnull)getErrorDescription:(NSInteger)error NS_SWIFT_NAME(getErrorDescription(_:));

/**
 * @brief Gets the C++ handle of the Native SDK.
 *
 * @details
 * This method retrieves the C++ handle of the SDK, which is used for registering the audio and
 * video frame observer.
 *
 * @return
 * The native handle of the SDK.
 */
- (void * _Nullable)getNativeHandle NS_SWIFT_NAME(getNativeHandle());

/**
 * @brief Sets the log file.
 *
 * @details
 * Specifies an SDK output log file. The log file records all log data for the SDK’s operation.
 * Call timing: This method needs to be called immediately after `sharedEngineWithConfig:delegate:`,
 * otherwise the output log may be incomplete.
 *
 * @note Ensure that the directory for the log file exists and is writable.
 *
 * @param filePath The complete path of the log files. These log files are encoded in UTF-8.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLogFile:(NSString * _Nonnull)filePath NS_SWIFT_NAME(setLogFile(_:));

/**
 * @brief Sets the log output level of the SDK.
 *
 * @details
 * This method sets the output log level of the SDK. You can use one or a combination of the log
 * filter levels. The log level follows the sequence of `AgoraLogFilterOff`,
 * `AgoraLogFilterCritical`, `AgoraLogFilterError`, `AgoraLogFilterWarning`, `AgoraLogFilterInfo`,
 * and `AgoraLogFilterDebug`. Choose a level to see the logs preceding that level.
 * If, for example, you set the log level to `AgoraLogFilterWarning`, you see the logs within levels
 * `AgoraLogFilterCritical`, `AgoraLogFilterError` and `AgoraLogFilterWarning`.
 *
 * @param filter The output log level of the SDK. See `AgoraLogFilter`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLogFilter:(NSUInteger)filter NS_SWIFT_NAME(setLogFilter(_:));

/**
 * @brief Sets the log file size.
 *
 * @details
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
 *
 * @note This method is used to set the size of the `agorasdk.log` file only and does not effect the
 * `agoraapi.log file`.
 *
 * @param fileSizeInKBytes The size (KB) of an `agorasdk.log` file. The value range is [128,20480].
 * The default value is 2,048 KB. If you set `fileSizeInKByte` smaller than 128 KB, the SDK
 * automatically adjusts it to 128 KB; if you set `fileSizeInKByte` greater than 20,480 KB, the SDK
 * automatically adjusts it to 20,480 KB.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLogFileSize:(NSUInteger)fileSizeInKBytes NS_SWIFT_NAME(setLogFileSize(_:));

/** Upload current log file immediately to server.
 *  only use this when an error occurs
 *  block before log file upload success or timeout.
 *
 *  @return
 *  requestID  the id of this upload.
 */
- (NSString * _Nullable)uploadLogFile NS_SWIFT_NAME(uploadLogFile());

/** * Write the log to SDK . @technical preview

 You can Write the log to SDK log files of the specified level

 @param level Log level: \ref AgoraLogLevel.

 @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)writeLog:(AgoraLogLevel)level content:(NSString * _Nonnull)content NS_SWIFT_NAME(writeLog(_:content:));

/**
 * @brief Retrieves the call ID.
 *
 * @details
 * When a user joins a channel on a client, a `callId` is generated to identify the call from the
 * client. You can call this method to get `callId`, and pass it in when calling methods such as
 * `rate:rating:description:` and `complain:description:`.
 * Call timing: Call this method after joining a channel.
 *
 * @return
 * The current call ID.
 */
- (NSString * _Nullable)getCallId NS_SWIFT_NAME(getCallId());

/**
 * @brief Allows a user to rate a call after the call ends.
 *
 * @note Ensure that you call this method after leaving a channel.
 *
 * @param callId The current call ID. You can get the call ID by calling `getCallId`.
 * @param rating The value is between 1 (the lowest score) and 5 (the highest score).
 * @param description (Optional) A description of the call. The string length should be less than
 * 800 bytes.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 */
- (int)rate:(NSString * _Nonnull)callId
     rating:(NSInteger)rating
description:(NSString * _Nullable)description NS_SWIFT_NAME(rate(_:rating:description:));

/**
 * @brief Allows a user to complain about the call quality after a call ends.
 *
 * @details
 * This method allows users to complain about the quality of the call. Call this method after the
 * user leaves the channel.
 *
 * @param callId The current call ID. You can get the call ID by calling `getCallId`.
 * @param description (Optional) A description of the call. The string length should be less than
 * 800 bytes.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -1: A general error occurs (no specified reason).
 *   - -2: The parameter is invalid.
 *   - -7: The method is called before `AgoraRtcEngineKit` is initialized.
 */
- (int)complain:(NSString * _Nonnull)callId
    description:(NSString * _Nullable)description NS_SWIFT_NAME(complain(_:description:));

/**
 * @brief Enables or disables dispatching delegate methods to the main queue.
 *
 * @details
 * If the callback methods are not dispatched to the main queue, the App should dispatch the UI
 * operations to the main queue.
 *
 * @param enabled - `YES`：Dispatch the callback methods to the main queue.
 * - `NO`：Do not dispatch the callback methods to the main queue.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails.
 */
- (int)enableMainQueueDispatch:(BOOL)enabled NS_SWIFT_NAME(enableMainQueueDispatch(_:));

/**
 * @brief Starts the last mile network probe test.
 *
 * @details
 * This method starts the last-mile network probe test before joining a channel to get the uplink
 * and downlink last mile network statistics, including the bandwidth, packet loss, jitter, and
 * round-trip time (RTT).
 * Call timing: Do not call other methods before receiving the `rtcEngine:lastmileQuality:` and
 * `rtcEngine:lastmileProbeTestResult:` callbacks. Otherwise, the callbacks may be interrupted.
 * Related callbacks: After successfully calling this method, the SDK sequentially triggers the
 * following 2 callbacks:
 * - `rtcEngine:lastmileQuality:`: The SDK triggers this callback within two seconds depending on
 * the network conditions. This callback rates the network conditions and is more closely linked to
 * the user experience.
 * - `rtcEngine:lastmileProbeTestResult:`: The SDK triggers this callback within 30 seconds
 * depending on the network conditions. This callback returns the real-time statistics of the
 * network conditions and is more objective.
 *
 * @param config The configurations of the last-mile network probe test. See
 * `AgoraLastmileProbeConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startLastmileProbeTest:(AgoraLastmileProbeConfig *_Nullable)config NS_SWIFT_NAME(startLastmileProbeTest(_:));

/**
 * @brief Stops the last mile network probe test.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopLastmileProbeTest NS_SWIFT_NAME(stopLastmileProbeTest());

/**
 * @brief Provides technical preview functionalities or special customizations by configuring the
 * SDK with JSON options.
 *
 * @param options Pointer to the set parameters in a JSON string.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setParameters:(NSString * _Nonnull)options NS_SWIFT_NAME(setParameters(_:));

/** Gets the Agora SDK's parameters for customization purposes.

 @note  This method is not public. Contact support@agora.io for more information.

 */
- (NSString * _Nullable)getParameter:(NSString * _Nonnull)parameter
                                args:(NSString * _Nullable)args NS_SWIFT_NAME(getParameter(_:args:));

/**
 * @brief Gets the current NTP (Network Time Protocol) time.
 *
 * @details
 * In the real-time chorus scenario, especially when the downlink connections are inconsistent due
 * to network issues among multiple receiving ends, you can call this method to obtain the current
 * NTP time as the reference time, in order to align the lyrics and music of multiple receiving ends
 * and achieve chorus synchronization.
 *
 * @return
 * The Unix timestamp (ms) of the current NTP time.
 */
- (uint64_t)getNtpWallTimeInMs;

#pragma mark MediaPlayer
/**
 * @brief Creates a media player instance.
 *
 * @details
 * Before calling any APIs in the `AgoraRtcMediaPlayerProtocol` class, you need to call this method
 * to create an instance of the media player. If you need to create multiple instances, you can call
 * this method multiple times.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @param delegate The event handler for `AgoraRtcEngineKit`. See `AgoraRtcEngineDelegate`.
 *
 * @return
 * - An `AgoraRtcMediaPlayerProtocol` object, if the method call succeeds.
 * - An empty pointer, if the method call fails.
 * An `AgoraRtcMediaPlayerProtocol` instance, if the method call succeeds.
 */
- (id<AgoraRtcMediaPlayerProtocol>_Nullable)createMediaPlayerWithDelegate:(id<AgoraRtcMediaPlayerDelegate>_Nullable)delegate NS_SWIFT_NAME(createMediaPlayer(with:));

/**
 * @brief Creates one `AgoraRtcMediaPlayerCacheManagerProtocol` instance.
 *
 * @details
 * Before calling any APIs in the `AgoraRtcMediaPlayerCacheManagerProtocol` class, you need to call
 * this method to get a cache manager instance of a media player.
 * Call timing: Make sure the `AgoraRtcEngineKit` is initialized before you call this method.
 *
 * @note The cache manager is a singleton pattern. Therefore, multiple calls to this method returns
 * the same instance.
 *
 * @return
 * The `AgoraRtcMediaPlayerCacheManagerProtocol` instance.
 */
- (id<AgoraRtcMediaPlayerCacheManagerProtocol> _Nullable)createMediaPlayerCacheManager NS_SWIFT_NAME(createMediaPlayerCacheManager());

/**
 * @brief Destroys the media player instance.
 *
 * @param mediaPlayer `AgoraRtcMediaPlayerProtocol` object.
 *
 * @return
 * - ≥ 0: Success. Returns the ID of media player instance.
 * - < 0: Failure.
 */
- (int)destroyMediaPlayer:(id<AgoraRtcMediaPlayerProtocol>_Nullable)mediaPlayer NS_SWIFT_NAME(destroyMediaPlayer(_:));

/**
 * getMediaPlayer
 * @param mediaPlayerId of the mediaPlayer.
 * @return id<AgoraMediaPlayerProtocol>
 */
- (id<AgoraRtcMediaPlayerProtocol> _Nullable)getMediaPlayer:(int)mediaPlayerId NS_SWIFT_NAME(getMediaPlayer(_:));

#pragma mark rhythm player

/**
 * @brief Enables the virtual metronome.
 *
 * @details
 * - After enabling the virtual metronome, the SDK plays the specified audio effect file from the
 * beginning, and controls the playback duration of each file according to `beatsPerMinute` you set
 * in `AgoraRhythmPlayerConfig`. For example, if you set `beatsPerMinute` as `60`, the SDK plays one
 * beat every second. If the file duration exceeds the beat duration, the SDK only plays the audio
 * within the beat duration.
 * - By default, the sound of the virtual metronome is published in the channel. If you want the
 * sound to be heard by the remote users, you can set `publishRhythmPlayerTrack` in
 * `AgoraRtcChannelMediaOptions` as `YES`.
 * Applicable scenarios: In music education, physical education and other scenarios, teachers
 * usually need to use a metronome so that students can practice with the correct beat. The meter is
 * composed of a downbeat and upbeats. The first beat of each measure is called a downbeat, and the
 * rest are called upbeats.
 * Call timing: This method can be called either before or after joining the channel.
 * Related callbacks: After successfully calling this method, the SDK triggers the
 * `rtcEngine:didRhythmPlayerStateChanged:reason:` callback locally to report the status of the
 * virtual metronome.
 *
 * @param sound1 The absolute path or URL address (including the filename extensions) of the file
 * for the downbeat. For example, `/var/mobile/Containers/Data/audio.mp4`. For the audio file
 * formats supported by this method, see `What formats of audio files does the Agora RTC SDK
 * support`.
 * @param sound2 The absolute path or URL address (including the filename extensions) of the file
 * for the upbeats. For example, `/var/mobile/Containers/Data/audio.mp4`. For the audio file formats
 * supported by this method, see `What formats of audio files does the Agora RTC SDK support`.
 * @param config The metronome configuration. See `AgoraRhythmPlayerConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -22: Cannot find audio effect files. Please set the correct paths for `sound1` and `sound2`.
 */
- (int)startRhythmPlayer:(NSString * _Nonnull)sound1 sound2:(NSString * _Nonnull)sound2 config:(AgoraRhythmPlayerConfig *  _Nullable)config NS_SWIFT_NAME(startRhythmPlayer(_:sound2:config:));

/**
 * @brief Disables the virtual metronome.
 *
 * @details
 * After calling `startRhythmPlayer:sound2:config:`, you can call this method to disable the virtual
 * metronome.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopRhythmPlayer NS_SWIFT_NAME(stopRhythmPlayer());

/**
 * @brief Configures the virtual metronome.
 *
 * @details
 * - After calling `startRhythmPlayer:sound2:config:`, you can call this method to reconfigure the
 * virtual metronome.
 * - After enabling the virtual metronome, the SDK plays the specified audio effect file from the
 * beginning, and controls the playback duration of each file according to `beatsPerMinute` you set
 * in `AgoraRhythmPlayerConfig`. For example, if you set `beatsPerMinute` as `60`, the SDK plays one
 * beat every second. If the file duration exceeds the beat duration, the SDK only plays the audio
 * within the beat duration.
 * - By default, the sound of the virtual metronome is published in the channel. If you want the
 * sound to be heard by the remote users, you can set `publishRhythmPlayerTrack` in
 * `AgoraRtcChannelMediaOptions` as `YES`.
 * Call timing: This method can be called either before or after joining the channel.
 * Related callbacks: After successfully calling this method, the SDK triggers the
 * `rtcEngine:didRhythmPlayerStateChanged:reason:` callback locally to report the status of the
 * virtual metronome.
 *
 * @param config The metronome configuration. See `AgoraRhythmPlayerConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)configRhythmPlayer:(AgoraRhythmPlayerConfig * _Nullable)config NS_SWIFT_NAME(configRhythmPlayer(_:));

#pragma mark Streaming Kit
/**
 * @brief Sets the audio profile of the audio streams directly pushed to the CDN by the host.
 *
 * @deprecated v4.6.0.
 *
 * @details
 * When you set the `publishMicrophoneTrack` or `publishCustomAudioTrack` in the
 * `AgoraDirectCdnStreamingMediaOptions` as `YES` to capture audios, you can call this method to set
 * the audio profile.
 *
 * @param profile The audio profile, including the sampling rate, bitrate, encoding mode, and the
 * number of channels. See `AgoraAudioProfile`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDirectCdnStreamingAudioConfiguration:(AgoraAudioProfile)profile NS_SWIFT_NAME(setDirectCdnStreamingAudioConfiguration(_:));

/**
 * @brief Sets the video profile of the media streams directly pushed to the CDN by the host.
 *
 * @deprecated v4.6.0.
 *
 * @details
 * This method only affects video streams captured by cameras or screens, or from custom video
 * capture sources. That is, when you set `publishCameraTrack` or `publishCustomVideoTrack` in
 * `AgoraDirectCdnStreamingMediaOptions` as `YES` to capture videos, you can call this method to set
 * the video profiles.
 * If your local camera does not support the video resolution you set,the SDK automatically adjusts
 * the video resolution to a value that is closest to your settings for capture, encoding or
 * streaming, with the same aspect ratio as the resolution you set. You can get the actual
 * resolution of the video streams through the `onDirectCdnStreamingStats:` callback.
 *
 * @param config Video profile. See `AgoraVideoEncoderConfiguration`.Note: During CDN live
 * streaming, Agora only supports setting `AgoraVideoOutputOrientationMode` as
 * `AgoraVideoOutputOrientationModeFixedLandscape` or
 * `AgoraVideoOutputOrientationModeFixedPortrait`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDirectCdnStreamingVideoConfiguration:(AgoraVideoEncoderConfiguration * _Nonnull)config NS_SWIFT_NAME(setDirectCdnStreamingVideoConfiguration(_:));

/**
 * @brief Starts pushing media streams to the CDN directly.
 *
 * @deprecated v4.6.0.
 *
 * @details
 * Aogra does not support pushing media streams to one URL repeatedly.
 * **Media options**
 * Agora does not support setting the value of `publishCameraTrack` and `publishCustomVideoTrack` as
 * `YES`, or the value of `publishMicrophoneTrack` and `publishCustomAudioTrack` as `YES` at the
 * same time. When choosing media setting options ( `AgoraDirectCdnStreamingMediaOptions` ), you can
 * refer to the following examples:
 * If you want to push audio and video streams captured by the host from a custom source, the media
 * setting options should be set as follows:
 * - `publishCustomAudioTrack` is set as `YES` and call the
 * `pushExternalAudioFrameSampleBuffer:sampleRate:channels:trackId:` or
 * `pushExternalAudioFrameRawData:samples:sampleRate:channels:trackId:timestamp:` method
 * - `publishCustomVideoTrack` is set as `YES` and call the `pushExternalVideoFrame:videoTrackId:`
 * method
 * - `publishCameraTrack` is set as `NO` (the default value)
 * - `publishMicrophoneTrack` is set as `NO` (the default value)
 * As of v4.2.0, Agora SDK supports audio-only live streaming. You can set `publishCustomAudioTrack`
 * or `publishMicrophoneTrack` in `AgoraDirectCdnStreamingMediaOptions` as `YES` and call
 * `pushExternalAudioFrameSampleBuffer:sampleRate:channels:trackId:` or
 * `pushExternalAudioFrameRawData:samples:sampleRate:channels:trackId:timestamp:` to push audio
 * streams.
 *
 * @note Agora only supports pushing one audio and video streams or one audio streams to CDN.
 *
 * @param delegate See `onDirectCdnStreamingStateChanged:reason:message:` and
 * `onDirectCdnStreamingStats:`.
 * @param publishUrl The CDN live streaming URL.
 * @param options The media setting options for the host. See `AgoraDirectCdnStreamingMediaOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)startDirectCdnStreaming:(id<AgoraDirectCdnStreamingEventDelegate> _Nonnull)delegate
                    publishUrl:(NSString * _Nonnull)publishUrl
                  mediaOptions:(AgoraDirectCdnStreamingMediaOptions * _Nonnull)options NS_SWIFT_NAME(startDirectCdnStreaming(_:publishUrl:mediaOptions:));

/**
 * @brief Stops pushing media streams to the CDN directly.
 *
 * @deprecated v4.6.0.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stopDirectCdnStreaming NS_SWIFT_NAME(stopDirectCdnStreaming());

/** Change the media source during the pushing
 *
 * @deprecated v4.6.0.
 *
 * @note
 * This method is synchronous.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateDirectCdnStreamingMediaOptions:(AgoraDirectCdnStreamingMediaOptions * _Nonnull)options NS_SWIFT_NAME(updateDirectCdnStreamingMediaOptions(_:));

/**
 * @brief Sets audio advanced options.
 *
 * @details
 * If you have advanced audio processing requirements, such as capturing and sending stereo audio,
 * you can call this method to set advanced audio options.
 *
 * @note Call this method after calling
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`, `enableAudio` and
 * `enableLocalAudio:`.
 *
 * @param options The advanced options for audio. See `AgoraAdvancedAudioOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAdvancedAudioOptions:(AgoraAdvancedAudioOptions * _Nonnull)options NS_SWIFT_NAME(setAdvancedAudioOptions(_:));

/**
 * @brief Reports customized messages.
 *
 * @details
 * Agora supports reporting and analyzing customized messages. This function is in the beta stage
 * with a free trial. The ability provided in its beta test version is reporting a maximum of 10
 * message pieces within 6 seconds, with each message piece not exceeding 256 bytes and each string
 * not exceeding 100 bytes. To try out this function, contact `support@agora.io` and discuss the
 * format of customized messages with us.
 *
 */
- (int)sendCustomReportMessage:(NSString * _Nullable)messageId
                      category:(NSString * _Nullable)category
                         event:(NSString * _Nullable)event
                         label:(NSString * _Nullable)label
                         value:(NSInteger)value NS_SWIFT_NAME(sendCustomReportMessage(_:category:event:label:value:));

/**
 * @brief Sets the output log level of the SDK.
 *
 * @details
 * Choose a level to see the logs preceding that level.
 *
 * @param level The log level. See `AgoraLogLevel`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLogLevel:(AgoraLogLevel)level NS_SWIFT_NAME(setLogLevel(_:));

/**
 * @brief Configures the connection to the Agora private media server access module.
 *
 * @details
 * After successfully deploying the Agora private media server and integrating the 4.x RTC SDK on an
 * intranet client, you can call this method to specify the Local Access Point and assign the access
 * module to the SDK.  
 * Call timing: You must call this method before joining a channel.
 *
 * @note This method only takes effect after deploying the Agora Hybrid Cloud solution. You can
 * `contact sales` to learn more about and deploy the Agora Hybrid Cloud.
 *
 * @param config The Local Access Point configuration. See `AgoraLocalAccessPointConfiguration` for
 * details.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting advice.
 */
- (int)setLocalAccessPoint:(AgoraLocalAccessPointConfiguration* _Nonnull)config NS_SWIFT_NAME(setLocalAccessPoint(withConfig:));

/**
 * @brief Sets up cloud proxy service.
 *
 * @since v3.3.0
 *
 * @details
 * When users' network access is restricted by a firewall, configure the firewall to allow specific
 * IP addresses and ports provided by Agora; then, call this method to enable the cloud `proxyType`
 * and set the cloud proxy type with the proxyType parameter.
 * After successfully connecting to the cloud proxy, the SDK triggers the
 * `rtcEngine:connectionChangedToState:reason:` ( AgoraConnectionStateConnecting,
 * AgoraConnectionChangedReasonSettingProxyServer ) callback.
 * To disable the cloud proxy that has been set, call the `setCloudProxy:(AgoraNoneProxy)`.
 * To change the cloud proxy type that has been set, call the `setCloudProxy:` `(AgoraNoneProxy) `
 * first, and then call the `setCloudProxy:` to set the `proxyType` you want.
 *
 * @note
 * - Agora recommends that you call this method before joining a channel.
 * - When a user is behind a firewall and uses the Force UDP cloud proxy, the services for Media
 * Push and cohosting across channels are not available.
 * - When you use the Force TCP cloud proxy, note that an error would occur when calling the
 * `startAudioMixing:loopback:cycle:startPos:` method to play online music files in the HTTP
 * protocol. The services for Media Push and cohosting across channels use the cloud proxy with the
 * TCP protocol.
 *
 * @param proxyType The type of the cloud proxy. See `AgoraCloudProxyType`.
 * This parameter is mandatory. The SDK reports an error if you do not pass in a value.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 *   - -2: The parameter is invalid.
 *   - -7: The SDK is not initialized.
 */
- (int)setCloudProxy:(AgoraCloudProxyType)proxyType  NS_SWIFT_NAME(setCloudProxy(_:));

/**
 * @brief Takes a snapshot of a video stream.
 *
 * @details
 * This method takes a snapshot of a video stream from the specified user, generates a JPG image,
 * and saves it to the specified path.
 * Call timing: Call this method after joining a channel.
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
- (NSInteger)takeSnapshot:(NSInteger)uid filePath:(NSString* _Nonnull)filePath NS_SWIFT_NAME(takeSnapshot(_:filePath:));

/**
 * @brief Takes a screenshot of the video at the specified observation point.
 *
 * @details
 * This method takes a snapshot of a video stream from the specified user, generates a JPG image,
 * and saves it to the specified path.
 * Call timing: Call this method after joining a channel.
 * Related callbacks: After a successful call of this method, the SDK triggers the
 * `rtcEngine:snapshotTaken:filePath:width:height:errCode:` callback to report whether the snapshot
 * is successfully taken, as well as the details for that snapshot.
 *
 * @note
 * - The method is asynchronous, and the SDK has not taken the snapshot when the method call
 * returns.
 * - When used for local video snapshots, this method takes a snapshot for the video streams
 * specified in `AgoraRtcChannelMediaOptions`.
 *
 * @param uid The user ID. Set uid as 0 if you want to take a snapshot of the local user's video.
 * @param config The configuration of the snaptshot. See `AgoraSnapshotConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (NSInteger)takeSnapshotWithConfig:(NSInteger)uid config:(AgoraSnapshotConfig* _Nonnull)config NS_SWIFT_NAME(takeSnapshotWithConfig(_:config:));

/**
 * @brief Enables or disables video screenshot and upload.
 *
 * @details
 * When video screenshot and upload function is enabled, the SDK takes screenshots and uploads
 * videos sent by local users based on the type and frequency of the module you set in
 * `AgoraContentInspectConfig`. After video screenshot and upload, the Agora server sends the
 * callback notification to your app server in HTTPS requests and sends all screenshots to the
 * third-party cloud storage service.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @note
 * - Before calling this method, make sure you have enabled video screenshot and upload on Agora
 * console.
 * - When the video moderation module is set to video moderation via Agora self-developed extension(
 * `AgoraContentInspectTypeSupervise` ), the video screenshot and upload dynamic library
 * `AgoraContentInspectExtension.xcframework` (iOS) or `AgoraContentInspectExtension.framework`
 * (macOS) is required. Deleting this library disables the
 * screenshot and upload feature.
 *
 * @param enabled Whether to enalbe video screenshot and upload:
 * - `YES`: Enables video screenshot and upload.
 * - `NO`: Disables video screenshot and upload.
 * @param config Screenshot and upload configuration. See `AgoraContentInspectConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableContentInspect:(BOOL)enabled config:(AgoraContentInspectConfig* _Nonnull)config NS_SWIFT_NAME(enableContentInspect(_:config:));

/**
 * @brief Sets audio-video synchronization on the publishing side.
 *
 * @details
 * A single user may use two separate devices to send audio and video streams respectively. To
 * ensure that the audio and video are synchronized in time on the receiving side, you can call this
 * method on the video publishing device and pass in the channel name and user ID of the audio
 * publishing device. The SDK automatically adjusts the video stream based on the timestamp of the
 * sent audio stream. This ensures audio-video synchronization on the receiving end even when the
 * two publishing devices are on different uplink networks (e.g., Wi-Fi and 4G).
 *
 * @note It is recommended that you call this method before joining the channel.
 *
 * @param channelId The channel name where the audio publishing device is located.
 * @param uid The user ID of the audio publishing device.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting advice.
 */
- (int)setAVSyncSource:(NSString* _Nonnull)channelId uid:(NSUInteger)uid NS_SWIFT_NAME(setAVSyncSource(_:uid:));

/**
   * send audio metadata
   * @since v4.3.1
   * @param metadata The NSData of metadata
   * @return
   * - 0: success
   * - <0: failure
   * @technical preview
  */
- (int)sendAudioMetadata:(NSData * _Nonnull)metadata NS_SWIFT_NAME(sendAudioMetadata(_:));
/**
 * @brief Sets whether to replace the current video feeds with images when publishing video streams.
 *
 * @details
 * When publishing video streams, you can call this method to replace the current video feeds with
 * custom images.
 * Once you enable this function, you can select images to replace the video feeds through the
 * `AgoraImageTrackOptions` parameter. If you disable this function, the remote users see the video
 * feeds that you publish.
 * Call timing: Call this method after joining a channel.
 *
 * @param enable Whether to replace the current video feeds with custom images:
 * - `YES`: Replace the current video feeds with custom images.
 * - `NO`: (Default) Do not replace the current video feeds with custom images.
 * @param options Image configurations. See `AgoraImageTrackOptions`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableVideoImageSource:(BOOL)enable
                       options:(AgoraImageTrackOptions *_Nullable)options NS_SWIFT_NAME(enableVideoImageSource(_:options:));

/**
 * @brief Gets the type of the local network connection.
 *
 * @details
 * You can use this method to get the type of network in use at any stage.
 *
 * @note You can call this method either before or after joining a channel.
 *
 * @return
 * - ≥ 0: The method call is successful, and the local network connection type is returned.
 *   - 0: The SDK disconnects from the network.
 *   - 1: The network type is LAN.
 *   - 2: The network type is Wi-Fi (including hotspots).
 *   - 3: The network type is mobile 2G.
 *   - 4: The network type is mobile 3G.
 *   - 5: The network type is mobile 4G.
 *   - 6: The network type is mobile 5G.
 * - < 0: The method call failed with an error code.
 *   - -1: The network type is unknown.
 */
- (int)getNetworkType NS_SWIFT_NAME(getNetworkType());

/**
 * @brief Creates a media recording object.
 *
 * @details
 * Before starting audio and video stream recording, you need to call this method to create a media
 * recording object. The SDK supports recording multiple audio and video streams from either local
 * or remote users. You can call this method multiple times to create multiple recording objects,
 * and use the `info` parameter to specify the channel name and the user ID of the stream to be
 * recorded.
 * 
 * After successfully creating the object, you need to call `setMediaRecorderDelegate:` to register
 * an observer for the recording object in order to listen for related callbacks, and then call
 * `startRecording:` to start recording.
 *
 * @param info Information about the audio and video stream to be recorded. See
 * `AgoraRecorderStreamInfo`.
 *
 * @return
 * - If the method call succeeds: Returns an `AgoraMediaRecorder` object.
 * - If the method call fails: Returns a null pointer.
 */
- (AgoraMediaRecorder * _Nullable)createMediaRecorder:(AgoraRecorderStreamInfo * _Nonnull)info NS_SWIFT_NAME(createMediaRecorder(withInfo:));

/**
 * @brief Destroys the audio and video recording object.
 *
 * @details
 * When you no longer need to record audio and video streams, you can call this method to destroy
 * the corresponding audio and video recording object. If recording is in progress, call
 * `stopRecording` to stop the recording first, and then call this method to destroy the audio and
 * video recording object.
 *
 * @param mediaRecorder The `AgoraMediaRecorder` object to be destroyed.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting suggestions.
 */
- (int)destroyMediaRecorder:(AgoraMediaRecorder * _Nullable)mediaRecorder;

#pragma mark Channel Transcoder

/** Get the H265Transcoder instance
 * @return id<AgoraH265TranscoderProtocol>
 *
*/
- (id<AgoraH265TranscoderProtocol>_Nullable)getH265Transcoder NS_SWIFT_NAME (getH265Transcoder());

#pragma mark Deprecated Methods

/**
 * @brief Sets the local video mirror mode.
 *
 * @deprecated
 *
 * @param mode The local video mirror mode. See `AgoraVideoMirrorMode`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLocalVideoMirrorMode:(AgoraVideoMirrorMode)mode NS_SWIFT_NAME(setLocalVideoMirrorMode(_:)) __deprecated;

/**
 * @brief Enables interoperability with the Agora Web SDK (applicable only in the live streaming
 *
 * @deprecated Web SDK interoperability is by default enabled.
 * scenarios).
 *
 * @details
 * You can call this method to enable or disable interoperability with the Agora Web SDK. If a
 * channel has Web SDK users, ensure that you call this method, or the video of the Native user will
 * be a black screen for the Web user.
 * This method is only applicable in live streaming scenarios, and interoperability is enabled by
 * default in communication scenarios.
 *
 * @param enabled Whether to enable interoperability:
 * - `YES`: Enable interoperability.
 * - `NO`: (Default) Disable interoperability.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableWebSdkInteroperability:(BOOL)enabled NS_SWIFT_NAME(enableWebSdkInteroperability(_:)) __deprecated;

/** The user who is talking and the speaker’s volume.
 @deprecated From v1.1

 By default it is disabled. If needed, use the [enableAudioVolumeIndication]([AgoraRtcEngineKit enableAudioVolumeIndication:smooth:]) method to configure it.

 @param audioVolumeIndicationBlock callback block, which contains the following
    speakers The speakers (array). Each speaker ():
     - uid: User ID of the speaker.
     - volume：Volume of the speaker, between 0 (lowest volume) to 255 (highest volume).
    totalVolume Total volume after audio mixing between 0 (lowest volume) to 255 (highest volume).
 */
- (void)audioVolumeIndicationBlock:(void(^ _Nullable)(NSArray * _Nonnull speakers, NSInteger totalVolume))audioVolumeIndicationBlock NS_SWIFT_NAME(audioVolumeIndicationBlock(_:)) __deprecated_msg("use delegate instead.");

/** The first local video frame is displayed on the screen.
 @deprecated From v1.1

 @param firstLocalVideoFrameBlock callback block, which contains the following
  width   Width (pixels) of the video stream.
  height  Height (pixels) of the video stream.
  elapsed Time elapsed (ms) from a user joining the channel until this callback is triggered.
 */
- (void)firstLocalVideoFrameBlock:(void(^ _Nullable)(NSInteger width, NSInteger height, NSInteger elapsed))firstLocalVideoFrameBlock NS_SWIFT_NAME(firstLocalVideoFrameBlock(_:)) __deprecated_msg("use delegate instead.");

/** Occurs when the first remote video frame is received and decoded.

 @deprecated This callback is deprecated.

 @param firstRemoteVideoDecodedBlock callback block, which contains the following
  uid     User ID of the user whose video streams are received.
  width   Width (pixels) of the video stream.
  height  Height (pixels) of the video stream.
  elapsed Time elapsed (ms) from the user joining the channel until this callback is triggered.
 */
- (void)firstRemoteVideoDecodedBlock:(void(^ _Nullable)(NSUInteger uid, NSInteger width, NSInteger height, NSInteger elapsed))firstRemoteVideoDecodedBlock NS_SWIFT_NAME(firstRemoteVideoDecodedBlock(_:)) __deprecated_msg("use delegate instead.");

/** The first remote video frame is received and decoded.
 @deprecated From v1.1

 @param firstRemoteVideoFrameBlock callback block, which contains the following
  uid     User ID of the user whose video streams are received.
  width   Width (pixels) of the video stream.
  height  Height (pixels) of the video stream.
  elapsed Time elapsed (ms) from the user joining the channel until this callback is triggered.
 */
- (void)firstRemoteVideoFrameBlock:(void(^ _Nullable)(NSUInteger uid, NSInteger width, NSInteger height, NSInteger elapsed))firstRemoteVideoFrameBlock NS_SWIFT_NAME(firstRemoteVideoFrameBlock(_:)) __deprecated_msg("use delegate instead.");

/** A user has successfully joined the channel.
 @deprecated From v1.1

 If there are other users in the channel when this user joins, the SDK also reports to the application on the existing users who are already in the channel.

 @param userJoinedBlock callback block, which contains the following:
   uid     User ID. If the uid is specified in the `joinChannelByToken` method, it returns the specified ID; if not, it returns an ID that is automatically assigned by the Agora server.
   elapsed Time elapsed (ms) from calling joinChannelByToken until this callback is triggered.
 */
- (void)userJoinedBlock:(void(^ _Nullable)(NSUInteger uid, NSInteger elapsed))userJoinedBlock NS_SWIFT_NAME(userJoinedBlock(_:)) __deprecated_msg("use delegate instead.");

/** A user has left the call or gone offline.
 @deprecated From v1.1

 The SDK reads the timeout data to determine if a user has left the channel (or has gone offline). If no data package is received from the user in 15 seconds, the SDK assumes the user is offline. Sometimes a weak network connection may lead to false detections; therefore, Agora recommends using signaling for reliable offline detection.

 @param userOfflineBlock callback block, which contains the following:
  uid User ID.
 */
- (void)userOfflineBlock:(void(^ _Nullable)(NSUInteger uid))userOfflineBlock NS_SWIFT_NAME(userOfflineBlock(_:)) __deprecated_msg("use delegate instead.");

/** A user's audio stream has muted/unmuted.
 @deprecated From v1.1

 @param userMuteAudioBlock callback block, which contains the following:
   uid   User ID.
   muted
     - YES: Muted.
     - NO: Unmuted.
*/
- (void)userMuteAudioBlock:(void(^ _Nullable)(NSUInteger uid, BOOL muted))userMuteAudioBlock NS_SWIFT_NAME(userMuteAudioBlock(_:)) __deprecated_msg("use delegate instead.");

/** Occurs when a remote user pauses or resumes sending the video stream.

  @deprecated This callback is deprecated. Use [remoteVideoStateChangedOfUid](remoteVideoStateChangedOfUid:state:reason:elapsed:)
   instead.

  @note This callback is invalid when the number of users or broadacasters in a
  channel exceeds 20.

 @param userMuteVideoBlock callback block, which contains the following:
   userId ID of the remote user.
   muted
  - YES: The remote user has paused sending the video stream.
  - NO: The remote user has resumed sending the video stream.
 */
- (void)userMuteVideoBlock:(void(^ _Nullable)(NSUInteger uid, BOOL muted))userMuteVideoBlock NS_SWIFT_NAME(userMuteVideoBlock(_:)) __deprecated_msg("use delegate instead.");

/** The SDK updates the application about the statistics of the uploading local video streams once every two seconds.
 @deprecated From v1.1

 @param localVideoStatBlock callback block, which contains the following:
  sentBytes  Total bytes sent since last count.
  sentFrames Total frames sent since last count.
 */
- (void)localVideoStatBlock:(void(^ _Nullable)(NSInteger sentBitrate, NSInteger sentFrameRate))localVideoStatBlock NS_SWIFT_NAME(localVideoStatBlock(_:)) __deprecated_msg("use delegate instead.");

/** The SDK updates the application about the statistics of receiving remote video streams once every two seconds.
 @deprecated From v1.1

 @param remoteVideoStatBlock callback block, which contains the following:
  uid           User ID that specifies whose video streams are received.
  rameCount     Total frames received since last count.
  delay         Time delay (ms)
  receivedBytes Total bytes received since last count.
  */
- (void)remoteVideoStatBlock:(void(^ _Nullable)(NSUInteger uid, NSInteger delay, NSInteger receivedBitrate, NSInteger receivedFrameRate))remoteVideoStatBlock NS_SWIFT_NAME(remoteVideoStatBlock(_:)) __deprecated_msg("use delegate instead.");

/** The camera is turned on and ready to capture the video.
 @deprecated From v1.1
 */
- (void)cameraReadyBlock:(void(^ _Nullable)(void))cameraReadyBlock NS_SWIFT_NAME(cameraReadyBlock(_:)) __deprecated_msg("use delegate instead.");

/** The SDK has lost network connection with the server.
 @deprecated From v1.1
 */
- (void)connectionLostBlock:(void(^ _Nullable)(void))connectionLostBlock NS_SWIFT_NAME(connectionLostBlock(_:)) __deprecated_msg("use delegate instead.");

/** A user has rejoined the channel with the reported channel ID and user ID.
 @deprecated From v1.1

 When the local machine loses connection with the server because of network problems, the SDK automatically attempts to reconnect, and then triggers this callback method upon reconnection.

 @param rejoinChannelSuccessBlock callback block, which contains the following:
  channel Channel name.
  uid     User ID.
  elapsed Time delay (ns) in rejoining the channel.
 */
- (void)rejoinChannelSuccessBlock:(void(^ _Nullable)(NSString * _Nonnull channel, NSUInteger uid, NSInteger elapsed))rejoinChannelSuccessBlock NS_SWIFT_NAME(rejoinChannelSuccessBlock(_:)) __deprecated_msg("use delegate instead.");

/** The RtcEngine runtime statistics reported once every two seconds.
 @deprecated From v1.1

 @param rtcStatsBlock callback block, which contains the following:
  stat AgoraChannelStats
 */
- (void)rtcStatsBlock:(void(^ _Nullable)(AgoraChannelStats * _Nonnull stat))rtcStatsBlock NS_SWIFT_NAME(rtcStatsBlock(_:)) __deprecated_msg("use delegate instead.");

/** The audio quality of the current call reported once every two seconds.
 @deprecated From v1.1

 @param audioQualityBlock callback block, which contains the following:
  uid     User ID of the speaker
  quality AgoraNetworkQuality
  delay   Time delay (ms)
  lost    The packet loss rate (%)
 */
- (void)audioQualityBlock:(void(^ _Nullable)(NSUInteger uid, AgoraNetworkQuality quality, NSUInteger delay, NSUInteger lost))audioQualityBlock NS_SWIFT_NAME(audioQualityBlock(_:)) __deprecated_msg("use delegate instead.");

/** The network quality of the specified user in a communication or live broadcast channel reported once every two seconds.
 @deprecated From v1.1

 @param networkQualityBlock callback block, which contains the following:
  uid       User ID. The network quality of the user with this UID will be reported. If uid is 0, the local network quality is reported.
  txQuality The transmission quality of the user: AgoraNetworkQuality
  rxQuality The receiving quality of the user: AgoraNetworkQuality
 */
- (void)networkQualityBlock:(void(^ _Nullable)(NSUInteger uid, AgoraNetworkQuality txQuality, AgoraNetworkQuality rxQuality))networkQualityBlock NS_SWIFT_NAME(networkQualityBlock(_:)) __deprecated_msg("use delegate instead.");

/** The network quality of the local user reported once after you have called [startLastmileProbeTest]([AgoraRtcEngineKit startLastmileProbeTest]).
 @deprecated From v1.1

 @param lastmileQualityBlock callback block, which contains the following:
  quality Quality of the last mile network: AgoraNetworkQuality
 */
- (void)lastmileQualityBlock:(void(^ _Nullable)(AgoraNetworkQuality quality))lastmileQualityBlock NS_SWIFT_NAME(lastmileQualityBlock(_:)) __deprecated_msg("use delegate instead.");

/** The media engine event.
 @deprecated From v1.1.
 */
- (void)mediaEngineEventBlock:(void(^ _Nullable)(NSInteger code))mediaEngineEventBlock NS_SWIFT_NAME(mediaEngineEventBlock(_:)) __deprecated_msg("use delegate instead.");

/** Disables the audio function in the channel.

 Replaced with the disableAudio method from v2.3.

 @deprecated  Replaced with the disableAudio method from v2.3.

 @return * 0: Success.
* <0: Failure.
 */
- (int)pauseAudio __deprecated_msg("use disableAudio instead.");

/** Enables the audio function in the channel.

 Replaced with the enableAudio method from v2.3.

 @deprecated Replaced with the enableAudio method from v2.3.

 @return * 0: Success.
* <0: Failure.
 */
- (int)resumeAudio __deprecated_msg("use enableAudio instead.");

/** Initializes the The AgoraRtcEngineKit object.

 Replaced with [sharedEngineWithAppId]([AgoraRtcEngineKit sharedEngineWithAppId:delegate:]).

 @deprecated  Replaced with sharedEngineWithAppId.
 */
+ (instancetype _Nonnull)sharedEngineWithAppId:(NSString * _Nonnull)AppId
                                         error:(void(^ _Nullable)(AgoraErrorCode errorCode))errorBlock NS_SWIFT_NAME(sharedEngine(withAppId:error:)) __deprecated_msg("use sharedEngineWithAppId:delegate: instead.");

/** Sets the high-quality audio parameters.
 @deprecated

 Replaced with [setAudioProfile](setAudioProfile:scenario:).
 */
- (int)setHighQualityAudioParametersWithFullband:(BOOL)fullband
                                          stereo:(BOOL)stereo
                                     fullBitrate:(BOOL)fullBitrate NS_SWIFT_NAME(setHighQualityAudioParametersWithFullband(_:stereo:fullBitrate:)) __deprecated_msg("use setAudioProfile:scenario: instead.");


/**
 * @brief Sets the audio profile and audio scenario.
 *
 * @deprecated This method is deprecated. You can use the
 * \ref AgoraRtcEngineKit.setAudioProfile: setAudioProfile
 * method instead.
 *
 * @details
 * Applicable scenarios: This method is suitable for various audio scenarios. You can choose as
 * needed. For example, in scenarios with high audio quality requirements such as music teaching, it
 * is recommended to set `profile` to `AgoraAudioProfileMusicHighQuality`(4) and `scenario` to
 * `AgoraAudioScenarioGameStreaming`(3).
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @note Due to iOS system restrictions, some audio routes cannot be recognized in call volume mode.
 * Therefore, if you need to use an external sound card, it is recommended to set the audio scenario
 * to `AgoraAudioScenarioGameStreaming`(3). In this scenario, the SDK will switch to media volume to
 * avoid this issue.
 *
 * @param profile The audio profile, including the sampling rate, bitrate, encoding mode, and the
 * number of channels. See `AgoraAudioProfile`.
 * @param scenario The audio scenarios. Under different audio scenarios, the device uses different
 * volume types. See `AgoraAudioScenario`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioProfile:(AgoraAudioProfile)profile scenario:(AgoraAudioScenario)scenario NS_SWIFT_NAME(setAudioProfile(_:scenario:)) __deprecated_msg("use setAudioProfile: instead.");

/** Gets the device type; such as a recording, playback, or video capture device.
 @note  This method applies to macOS only, not to iOS.

 @param type AgoraMediaDeviceType
 @return When called successfully, it returns the device ID of the device. Otherwise, it returns nil.
 */
- (NSString * _Nullable)getDeviceId:(AgoraMediaDeviceType)type NS_SWIFT_NAME(getDeviceId(_:)) __deprecated_msg("use getDeviceInfo: instead.");

/**
 * @brief Sets the external audio source.
 *
 * @deprecated This method is deprecated. Use createCustomAudioTrack:(AgoraAudioTrackType)trackType
 * config:(AgoraAudioTrackConfig* _Nonnull)config instead.
 *
 * @details
 * Call this method before calling `joinChannelByToken:channelId:info:uid:joinSuccess:` and
 * `startPreview`.
 *
 * @param enabled - `YES`: Enable the external audio source.
 * - `NO`: (Default) Disable the external audio source.
 * @param sampleRate The sample rate (Hz) of the external audio source, which can be set as 8000,
 * 16000, 32000, 44100, or 48000.
 * @param channels The number of audio channels of the external audio source:
 * - 1: Mono.
 * - 2: Stereo.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setExternalAudioSource:(BOOL)enabled
                   sampleRate:(NSInteger)sampleRate
                     channels:(NSInteger)channels NS_SWIFT_NAME(setExternalAudioSource(_:sampleRate:channels:)) __deprecated_msg("use createCustomAudioTrack:config: instead.");

/**
 * @brief Sets the external audio source parameters.
 *
 * @deprecated This method is deprecated. Use createCustomAudioTrack:(AgoraAudioTrackType)trackType
 * config:(AgoraAudioTrackConfig* _Nonnull)config instead.
 *
 * @details
 * Call timing: Call this method before joining a channel.
 *
 * @param enabled Whether to enable the external audio source:
 * - `YES`: Enable the external audio source.
 * - `NO`: (Default) Disable the external audio source.
 * @param sampleRate The sample rate (Hz) of the external audio source which can be set as `8000`,
 * `16000`, `32000`, `44100`, or `48000`.
 * @param channels The number of channels of the external audio source, which can be set as `1`
 * (Mono) or `2` (Stereo).
 * @param localPlayback Whether to play the external audio source:
 * - `YES`: Play the external audio source.
 * - `NO`: (Default) Do not play the external source.
 * @param publish Whether to publish audio to the remote users:
 * - `YES`: (Default) Publish audio to the remote users.
 * - `NO`: Do not publish audio to the remote users.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setExternalAudioSource:(BOOL)enabled
                   sampleRate:(NSInteger)sampleRate
                     channels:(NSInteger)channels
                localPlayback:(BOOL)localPlayback
                      publish:(BOOL)publish NS_SWIFT_NAME(setExternalAudioSource(_:sampleRate:channels:localPlayback:publish:)) __deprecated_msg("use createCustomAudioTrack:config: instead.");

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
/** Sets the speakerphone volume.

 @note  This method applies to macOS only, not to iOS.

 @param volume 0 (lowest volume) to 255 (highest volume).

 @return * 0: Success.
* <0: Failure.
 */
- (int)setSpeakerphoneVolume:(NSUInteger)volume NS_SWIFT_NAME(setSpeakerphoneVolume(_:)) __deprecated_msg("use setDeviceVolume:volume: instead.");

- (int)startScreenCapture:(NSUInteger)windowId
          withCaptureFreq:(NSInteger)captureFreq
                  bitRate:(NSInteger)bitRate
                  andRect:(CGRect)rect NS_SWIFT_NAME(startScreenCapture(_:withCaptureFreq:bitRate:andRect:)) __deprecated_msg("use startScreenCaptureByWindowId:regionRect:captureParams: instead.");
#endif

/** Returns the Media Engine version.
 @deprecated From v2.3

 @return string, Media engine version
 */
+ (NSString * _Nonnull)getMediaEngineVersion NS_SWIFT_NAME(getMediaEngineVersion()) __deprecated;


/** Sets the video encoding profile manually.
 @deprecated From v2.3

 @param size      Size of the video that you want to set. The highest value is 1280 x 720.
 @param frameRate Frame rate of the video that you want to set. The highest value is 30. You can set it to 5, 10, 15, 24, 30, and so on.
 @param bitrate   Bitrate of the video that you want to set. You need to manually work out the frame rate according to the width, height, and frame rate. With the same width and height, the bitrate varies with the change of the frame rate:

 * If the frame rate is 5 fps, divide the recommended bitrate by 2.
 * If the frame rate is 15 fps, use the recommended bitrate.
 * If the frame rate is 30 fps, multiply the recommended bitrate by 1.5.
 * Calculate your bitrate with the ratio if you choose other frame rates.

 For example, the resolution is 320 x 240 and the frame rate is 15 fps, hence, the bitrate is 200:

 * If the frame rate is 5 fps, the bitrate is 100.
 * If the frame rate is 30 fps, the bitrate is 300.
 * If the bitrate you set is beyond the proper range, the SDK will automatically adjust it to a value within the range.
 */
- (int)setVideoResolution:(CGSize)size andFrameRate:(NSInteger)frameRate bitrate:(NSInteger)bitrate NS_SWIFT_NAME(setVideoResolution(_:andFrameRate:bitrate:)) __deprecated_msg("use setVideoEncoderConfiguration: instead.");

/** A user has left the channel.
 @deprecated From v1.1

 This callback also provides call session statistics, including the duration of the call and the transmitting and receiving bytes.

 @param leaveChannelBlock callback block, which contains the following:
  stat AgoraChannelStats
 */
- (void)leaveChannelBlock:(void(^ _Nullable)(AgoraChannelStats * _Nonnull stat))leaveChannelBlock NS_SWIFT_NAME(leaveChannelBlock(_:)) __deprecated_msg("use delegate instead.");

/**
 * @brief Sets the local video display mode.
 *
 * @deprecated
 *
 * @details
 * Call this method to set the local video display mode. This method can be called multiple times
 * during a call to change the display mode.
 *
 * @param mode The local video display mode. See `AgoraVideoRenderMode`.
 * @param uid The user ID.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLocalRenderMode:(NSUInteger)uid
                     mode:(AgoraVideoRenderMode)mode NS_SWIFT_NAME(setLocalRenderMode(_:mode:)) __deprecated;

/**
 * Enable or disable the external audio source local playback.
 *
 * * @param enable Determines whether to enable the external audio source local playback:
 * - true: Enable the external audio source local playback.
 * - false: (default) Disable the external audio source local playback.
 * @return int
 * -  0: Success.
 * - <0: Failure.
 */
- (int)enableExternalAudioSourceLocalPlayback:(BOOL)enable NS_SWIFT_NAME(enableExternalAudioSourceLocalPlayback(_:)) __deprecated;;

- (int)startMultipleVideoStreams:(VIEW_CLASS * _Nonnull)view screen:(VIEW_CLASS * _Nonnull)screenView remotes:(NSArray * _Nullable)remoteViews NS_SWIFT_NAME(startMultipleVideoStreams(_:screen:remotes:)) __deprecated;;

/**
 * Adds multiple SDK delegate.
 *
 * @param delegate The AgoraRtcEngineDelegate object.
 */
- (void)addDelegate:(id<AgoraRtcEngineDelegate> _Nonnull)delegate NS_SWIFT_NAME(addDelegate(_:));

/**
 * Removes multiple SDK delegate.
 *
 * @param delegate The AgoraRtcEngineDelegate object.
 */
- (void)removeDelegate:(id<AgoraRtcEngineDelegate> _Nonnull)delegate NS_SWIFT_NAME(removeDelegate(_:));

@end
