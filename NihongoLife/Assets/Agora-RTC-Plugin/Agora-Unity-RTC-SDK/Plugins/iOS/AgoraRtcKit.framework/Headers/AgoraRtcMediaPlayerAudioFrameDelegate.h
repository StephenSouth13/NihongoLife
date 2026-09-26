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

@protocol AgoraRtcMediaPlayerAudioFrameDelegate <NSObject>

/**
 * @brief Occurs each time the player receives an audio frame.
 *
 * @details
 * After registering the audio frame observer, the callback occurs every time the player receives an
 * audio frame, reporting the detailed information of the audio frame.
 *
 * @param audioFrame The audio frame information. See AgoraAudioFrame.
 * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
       didReceiveAudioFrame:(AgoraAudioFrame* _Nonnull)audioFrame NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didReceiveAudioFrame:));

@end