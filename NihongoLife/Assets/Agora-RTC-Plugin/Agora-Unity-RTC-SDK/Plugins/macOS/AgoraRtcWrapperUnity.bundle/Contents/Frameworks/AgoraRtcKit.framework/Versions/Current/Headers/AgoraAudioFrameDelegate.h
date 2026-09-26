//
//  AgoraRtcEngineKit.h
//  AgoraRtcEngineKit
//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraEnumerates.h"

@class AgoraAudioFrame;
@class AgoraAudioParams;

/**
 * The AgoraAudioFrameDelegate protocol enables audio frame callback event notifications to your application.
 */
@protocol AgoraAudioFrameDelegate <NSObject>
@optional

/**
 * @brief Gets the captured audio frame.
 *
 * @details
 * To ensure that the format of the cpatured audio frame is as expected, you can choose one of the
 * following two methods to set the audio data format:
 * - Method 1: After calling
 * `setRecordingAudioFrameParametersWithSampleRate:channel:mode:samplesPerCall:` to set the audio
 * data format and `setAudioFrameDelegate:` to register the audio frame observer object, the SDK
 * calculates the sampling interval according to the parameters set in the methods, and triggers the
 * `onRecordAudioFrame:channelId:` callback according to the sampling interval.
 * - Method 2: After calling `setAudioFrameDelegate:` to register the audio frame observer object,
 * set the audio data format in the return value of the `getObservedAudioFramePosition` callback.
 * The SDK then calculates the sampling interval according to the return value of the
 * `getRecordAudioParams` callback, and triggers the `onRecordAudioFrame:channelId:` callback
 * according to the sampling interval.
 *
 * @note - The priority of method 1 is higher than that of method 2. If method 1 is used to set the
 * audio data format, the setting of method 2 is invalid.
 *
 * @param frame The raw audio data. See `AgoraAudioFrame`.
 * @param channelId The channel ID.
 *
 * @return
 * Without practical meaning.
 */
- (BOOL)onRecordAudioFrame:(AgoraAudioFrame* _Nonnull)frame channelId:(NSString * _Nonnull)channelId  NS_SWIFT_NAME(onRecordAudioFrame(_:channelId:));

/**
 * @brief Gets the raw audio frame for playback.
 *
 * @details
 * To ensure that the data format of audio frame for playback is as expected, Agora recommends that
 * you choose one of the following two methods to set the audio data format:
 * - Method 1: After calling
 * `setPlaybackAudioFrameParametersWithSampleRate:channel:mode:samplesPerCall:` to set the audio
 * data format and `setAudioFrameDelegate:` to register the audio frame observer object, the SDK
 * calculates the sampling interval according to the parameters set in the methods, and triggers the
 * `onPlaybackAudioFrame:` callback according to the sampling interval.
 * - Method 2: After calling `setAudioFrameDelegate:` to register the audio frame observer object,
 * set the audio data format in the return value of the `getObservedAudioFramePosition` callback.
 * The SDK then calculates the sampling interval according to the return value of the
 * `getPlaybackAudioParams` callback, and triggers the `onPlaybackAudioFrame:` callback according to
 * the sampling interval.
 *
 * @note - The priority of method 1 is higher than that of method 2. If method 1 is used to set the
 * audio data format, the setting of method 2 is invalid.
 *
 * @param frame The raw audio data. See `AgoraAudioFrame`.
 * @param channelId The channel ID.
 *
 * @return
 * Without practical meaning.
 */
- (BOOL)onPlaybackAudioFrame:(AgoraAudioFrame* _Nonnull)frame channelId:(NSString * _Nonnull)channelId  NS_SWIFT_NAME(onPlaybackAudioFrame(_:channelId:));

/**
 * @brief Retrieves the mixed captured and playback audio frame.
 *
 * @details
 * To ensure that the data format of mixed captured and playback audio frame meets the expectations,
 * Agora recommends that you choose one of the following two ways to set the data format:
 * - Method 1: After calling `setMixedAudioFrameParametersWithSampleRate:channel:samplesPerCall:` to
 * set the audio data format and `setAudioFrameDelegate:` to register the audio frame observer
 * object, the SDK calculates the sampling interval according to the parameters set in the methods,
 * and triggers the `onMixedAudioFrame:channelId:` callback according to the sampling interval.
 * - Method 2: After calling `setAudioFrameDelegate:` to register the audio frame observer object,
 * set the audio data format in the return value of the `getObservedAudioFramePosition` callback.
 * The SDK then calculates the sampling interval according to the return value of the
 * `getMixedAudioParams` callback, and triggers the `onMixedAudioFrame:channelId:` callback
 * according to the sampling interval.
 *
 * @note - The priority of method 1 is higher than that of method 2. If method 1 is used to set the
 * audio data format, the setting of method 2 is invalid.
 *
 * @param frame The raw audio data. See `AgoraAudioFrame`.
 * @param channelId The channel ID.
 *
 * @return
 * Without practical meaning.
 */
- (BOOL)onMixedAudioFrame:(AgoraAudioFrame* _Nonnull)frame channelId:(NSString * _Nonnull)channelId  NS_SWIFT_NAME(onMixedAudioFrame(_:channelId:));

/**
 * @brief Gets the in-ear monitoring audio frame.
 *
 * @details
 * In order to ensure that the obtained in-ear audio data meets the expectations, Agora recommends
 * that you choose one of the following two methods to set the in-ear monitoring-ear audio data
 * format:
 * - Method 1: After calling
 * `setEarMonitoringAudioFrameParametersWithSampleRate:channel:mode:samplesPerCall:` to set the
 * audio data format and `setAudioFrameDelegate:` to register the audio frame observer object, the
 * SDK calculates the sampling interval according to the parameters set in the methods, and triggers
 * the `onEarMonitoringAudioFrame:` callback according to the sampling interval.
 * - Method 2: After calling `setAudioFrameDelegate:` to register the audio frame observer object,
 * set the audio data format in the return value of the `getObservedAudioFramePosition` callback.
 * The SDK then calculates the sampling interval according to the return value of the
 * `getEarMonitoringAudioParams` callback, and triggers the `onEarMonitoringAudioFrame:` callback
 * according to the sampling interval.
 *
 * @note - The priority of method 1 is higher than that of method 2. If method 1 is used to set the
 * audio data format, the setting of method 2 is invalid.
 *
 * @param frame The raw audio data. See `AgoraAudioFrame`.
 *
 * @return
 * Without practical meaning.
 */
- (BOOL)onEarMonitoringAudioFrame:(AgoraAudioFrame* _Nonnull)frame NS_SWIFT_NAME(onEarMonitoringAudioFrame(_:));

/**
 * @brief Sets the frame position for the video observer.
 *
 * @details
 * After successfully registering the audio data observer, the SDK uses this callback for each
 * specific audio frame processing node to determine whether to trigger the following callbacks:
 * - `onRecordAudioFrame:channelId:`
 * - `onPlaybackAudioFrame:`
 * - `onPlaybackAudioFrameBeforeMixing:channelId:uid:`
 * - `onMixedAudioFrame:channelId:`
 * - `onEarMonitoringAudioFrame:`
 * You can set one or more positions you need to observe by modifying the return value of
 * `getObservedAudioFramePosition` based on your scenario requirements:
 * When the annotation observes multiple locations, the | (or operator) is required. To conserve
 * system resources, you can reduce the number of frame positions that you want to observe.
 *
 * @return
 * Returns a bitmask that sets the observation position, with the following values:
 * - AgoraAudioFramePositionPlayback (0x0001): This position can observe the playback audio mixed by
 * all remote users, corresponding to the `onPlaybackAudioFrame:` callback.
 * - AgoraAudioFramePositionRecord (0x0002): This position can observe the collected local user's
 * audio, corresponding to the `onRecordAudioFrame:channelId:` callback.
 * - AgoraAudioFramePositionMixed (0x0004): This position can observe the playback audio mixed by
 * the loacl user and all remote users, corresponding to the `onMixedAudioFrame:channelId:`
 * callback.
 * - AgoraAudioFramePositionBeforeMixing (0x0008): This position can observe the audio of a single
 * remote user before mixing, corresponding to the `onPlaybackAudioFrameBeforeMixing:channelId:uid:`
 * callback.
 * - AgoraAudioFramePositionEarMonitoring (0x0010): This position can observe the in-ear monitoring
 * audio of the local user, corresponding to the `onEarMonitoringAudioFrame:` callback.
 */
- (AgoraAudioFramePosition)getObservedAudioFramePosition NS_SWIFT_NAME(getObservedAudioFramePosition());

/**
 * @brief Sets the audio format for the `onMixedAudioFrame:channelId:` callback.
 *
 * @details
 * You need to register the callback when calling the `setAudioFrameDelegate:` method. After you
 * successfully register the audio observer, the SDK triggers this callback, and you can set the
 * audio format in the return value of this callback.
 *
 * @note
 * The SDK triggers the `onMixedAudioFrame:channelId:` callback with the `AgoraAudioParams`
 * calculated sampling interval you set in the return value. The calculation formula is Sample
 * interval (sec) = `samplePerCall` /( `sampleRate` × `channel` ).
 * Ensure that the sample interval ≥ 0.01 (s).
 *
 * @return
 * The mixed captured and playback audio data. See `AgoraAudioParams`.
 */
- (AgoraAudioParams* _Nonnull)getMixedAudioParams NS_SWIFT_NAME(getMixedAudioParams());

/**
 * @brief Sets the audio format for the `onRecordAudioFrame:channelId:` callback.
 *
 * @details
 * You need to register the callback when calling the `setAudioFrameDelegate:` method. After you
 * successfully register the audio observer, the SDK triggers this callback, and you can set the
 * audio format in the return value of this callback.
 *
 * @note
 * The SDK triggers the `onRecordAudioFrame:channelId:` callback with the `AgoraAudioParams`
 * calculated sampling interval you set in the return value. The calculation formula is Sample
 * interval (sec) = `samplePerCall` /( `sampleRate` × `channel` ).
 * Ensure that the sample interval ≥ 0.01 (s).
 *
 * @return
 * The captured audio data, see `AgoraAudioParams`.
 */
- (AgoraAudioParams* _Nonnull)getRecordAudioParams NS_SWIFT_NAME(getRecordAudioParams());

/**
 * @brief Sets the audio format for the `onPlaybackAudioFrame:` callback.
 *
 * @details
 * You need to register the callback when calling the `setAudioFrameDelegate:` method. After you
 * successfully register the audio observer, the SDK triggers this callback, and you can set the
 * audio format in the return value of this callback.
 *
 * @note
 * The SDK triggers the `onPlaybackAudioFrame:` callback with the `AgoraAudioParams` calculated
 * sampling interval you set in the return value. The calculation formula is Sample interval (sec) =
 * `samplePerCall` /( `sampleRate` × `channel` ).
 * Ensure that the sample interval ≥ 0.01 (s).
 *
 * @return
 * The audio data for playback, see `AgoraAudioParams`.
 */
- (AgoraAudioParams* _Nonnull)getPlaybackAudioParams NS_SWIFT_NAME(getPlaybackAudioParams());

/**
 * @brief Sets the audio format for the `onEarMonitoringAudioFrame:` callback.
 *
 * @details
 * You need to register the callback when calling the `setAudioFrameDelegate:` method. After you
 * successfully register the audio observer, the SDK triggers this callback, and you can set the
 * audio format in the return value of this callback.
 *
 * @note
 * The SDK triggers the `onEarMonitoringAudioFrame:` callback with the `AgoraAudioParams` calculated
 * sampling interval you set in the return value. The calculation formula is `Sample` interval (
 * `sec` ) = `samplePerCall` /( `sampleRate` × `channel` ).
 * Ensure that the sample interval ≥ 0.01 (s).
 *
 * @return
 * The audio data of in-ear monitoring, see `AgoraAudioParams`.
 */
- (AgoraAudioParams* _Nonnull)getEarMonitoringAudioParams NS_SWIFT_NAME(getEarMonitoringAudioParams());

/**
 * @brief Retrieves the audio frame before mixing of subscribed remote users.
 *
 * @param channelId The channel ID.
 * @param uid The ID of subscribed remote users.
 * @param frame The raw audio data. See `AgoraAudioFrame`.
 *
 * @return
 * Without practical meaning.
 */
- (BOOL)onPlaybackAudioFrameBeforeMixing:(AgoraAudioFrame* _Nonnull)frame channelId:(NSString * _Nonnull)channelId uid:(NSUInteger)uid  NS_SWIFT_NAME(onPlaybackAudioFrame(beforeMixing:channelId:uid:));

@end
