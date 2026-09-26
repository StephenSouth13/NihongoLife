//
//  AgoraRtcEngineKit.h
//  AgoraRtcEngineKit
//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraConstants.h"
#import "AgoraObjects.h"

/** Agora provides ensured quality of experience (QoE) for worldwide Internet-based voice and video communications through a virtual global network that is especially optimized for real-time web and mobile-to-mobile applications.

 The AgoraRtcEngineKit class is the entry point of the Agora SDK that provides simple APIs for applications to easily start voice and video communication.
 */
@class AgoraRtcEngineKit;
@class AgoraMediaRecorder;

@protocol AgoraAudioEncodedFrameDelegate <NSObject>
@required

/**
 * @brief Gets the encoded audio data of the local user.
 *
 * @details
 * After calling `setAudioEncodedFrameDelegate:config:` and setting the encoded audio as
 * AgoraAudioEncodedFrameObserverPositionRecord, you can get the encoded audio data of the local
 * user from this callback.
 *
 * @param frameData The audio buffer.
 * @param info Audio information after encoding. See `AgoraEncodedAudioFrameInfo`.
 *
 */
- (void)onRecordEncodedAudioFrame:(NSData* _Nonnull)frameData info:(AgoraEncodedAudioFrameInfo* _Nonnull)info NS_SWIFT_NAME(onRecordEncodedAudioFrame(_:info:));

/**
 * @brief Gets the encoded audio data of all remote users.
 *
 * @details
 * After calling `setAudioEncodedFrameDelegate:config:` and setting the encoded audio as
 * AgoraAudioEncodedFrameObserverPositionPlayback, you can get encoded audio data of all remote
 * users through this callback.
 *
 * @param frameData The audio buffer.
 * @param info Audio information after encoding. See `AgoraEncodedAudioFrameInfo`.
 *
 */
- (void)onPlaybackEncodedAudioFrame:(NSData* _Nonnull)frameData info:(AgoraEncodedAudioFrameInfo* _Nonnull)info NS_SWIFT_NAME(onPlaybackEncodedAudioFrame(_:info:));

/**
 * @brief Gets the mixed and encoded audio data of the local and all remote users.
 *
 * @details
 * After calling `setAudioEncodedFrameDelegate:config:` and setting the audio profile as
 * AgoraAudioEncodedFrameObserverPositionMixed, you can get the mixed and encoded audio data of the
 * local and all remote users through this callback.
 *
 * @param frameData The audio buffer.
 * @param info Audio information after encoding. See `AgoraEncodedAudioFrameInfo`.
 *
 */
- (void)onMixedEncodedAudioFrame:(NSData* _Nonnull)frameData info:(AgoraEncodedAudioFrameInfo* _Nonnull)info NS_SWIFT_NAME(onMixedEncodedAudioFrame(_:info:));
@end
