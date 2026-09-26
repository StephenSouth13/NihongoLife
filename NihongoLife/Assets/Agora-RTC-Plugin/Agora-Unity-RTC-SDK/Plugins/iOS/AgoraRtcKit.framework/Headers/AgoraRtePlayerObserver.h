/**
 *
 * Agora Real Time Engagement
 * Copyright (c) 2024 Agora IO. All rights reserved.
 *
 */


#import <Foundation/Foundation.h>
#import "AgoraRtePlayer.h"
#import "AgoraRteEnumerates.h"

/**
 * Player Observer, an interface for receiving player event callbacks.
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRtePlayerObserver : NSObject

- (instancetype _Nonnull)init;

/**
 * @brief Callback for when the player state changes.
 *
 * @since v4.4.0
 *
 * @details
 * If you need to monitor changes in the player state, you must first call `registerObserver:error:`
 * to register the player observer object.
 * Call timing: The SDK triggers this callback when the player state changes to report the current
 * and previous states.
 *
 * @param oldState The state of the player before the change. See `AgoraRtePlayerState`.
 * @param newState The current state of the player. See `AgoraRtePlayerState`. If the current state
 * is `AgoraRtePlayerStateFailed`, you can get detailed error information through the `error`
 * parameter.
 * @param error The state or error information. See `AgoraRteError`.
 *
 */
- (void)onStateChanged:(AgoraRtePlayerState)oldState newState:(AgoraRtePlayerState)newState error:(AgoraRteError * _Nullable)error;

/**
 * @brief Reports the current playback progress of the media resource.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: This callback is triggered once per second during media resource playback.
 *
 * @param currentTime The current playback progress in milliseconds.
 * @param utcTime The current NTP (Network Time Protocol) time in milliseconds.
 *
 */
- (void)onPositionChanged:(uint64_t)currentTime utcTime:(uint64_t)utcTime;

/**
 * @brief Callback for video resolution change.
 *
 * @since v4.4.0
 *
 * @details
 * If you need to monitor changes in the resolution of the video stream being played by the player,
 * you must first call `registerObserver:error:` to register the player observer object.
 * Call timing: The SDK triggers this callback to report the current video width and height when the
 * resolution of the video stream changes.
 *
 * @param width The width of the video frame (px).
 * @param height The height of the video frame (px).
 *
 */
- (void)onResolutionChanged:(int)width height:(int)height;

/**
 * @brief Player event callback.
 *
 * @since v4.4.0
 *
 * @details
 * If you need to monitor player events, you must first call `registerObserver:error:` to register
 * the player observer object.
 * Call timing: The SDK triggers this callback when a player event changes.
 *
 * @param event Player event. See `AgoraRtePlayerEvent` for details.
 *
 */
- (void)onEvent:(AgoraRtePlayerEvent)event;

/**
 * @brief Callback for received media metadata.
 *
 * @since v4.4.0
 *
 * @details
 * If you need to obtain metadata from the Media Pull, you must first call `registerObserver:error:`
 * to register a player observer object.
 * Call timing: This callback is triggered after the SDK parses the metadata from the Media Pull,
 * reporting the type and content of the data.
 *
 * @param type The type of metadata. See `AgoraRtePlayerMetadataType`.
 * @param data The parsed metadata content.
 *
 */
- (void)onMetadata:(AgoraRtePlayerMetadataType)type data:(NSData * _Nonnull)data;

/**
 * @brief Callback triggered when the player or media stream information changes.
 *
 * @since v4.4.0
 *
 * @details
 * If you want to obtain information about the player or media stream, you need to first call
 * `registerObserver:error:` to register a player observer object. You can also retrieve the
 * information directly using the `getInfo:error:` method.
 * Call timing: The SDK triggers this callback when the player or media stream information changes.
 *
 * @param info Information about the player and media stream. See `AgoraRtePlayerInfo` for details.
 *
 */
- (void)onPlayerInfoUpdated:(AgoraRtePlayerInfo * _Nonnull)info;

/**
 * @brief Player volume indication callback.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: The SDK triggers this callback when the player's volume changes.
 *
 * @param volume The current volume of the player, ranging from [0,225].
 *
 */
- (void)onAudioVolumeIndication:(int32_t)volume;

@end
