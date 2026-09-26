//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraRtcMediaPlayerProtocol.h"

@protocol AgoraRtcMediaPlayerProtocol;
@class AgoraOutputVideoFrame;

@protocol AgoraRtcMediaPlayerVideoFrameDelegate <NSObject>
@optional
/**
 * @brief Occurs each time the player receives a video frame.
 *
 * @details
 * After registering the video frame observer, the callback occurs every time the player receives a
 * video frame, reporting the detailed information of the video frame.
 *
 * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
 * @param videoFrame Video frame information. See `AgoraOutputVideoFrame`.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
       didReceiveVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didReceiveVideoFrame:));

/**
 * @brief Occurs when the SDK gets the pixel buffer of the video source.
 *
 * @details
 * When the media player receives the pixel buffer of the video source, this callback is triggered
 * to report the detailed information about the pixel buffer of type CVPixelBufferRef. You can use
 * this information for image processing and analysis, custom rendering, etc.
 *
 * @param playerKit The `AgoraRtcMediaPlayerProtocol` instance.
 * @param pixelBuffer A CVPixelBufferRef object that contains detailed information about the pixel
 * buffer.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
       didReceivePixelBuffer:(CVPixelBufferRef _Nonnull)pixelBuffer NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didReceivePixelBuffer:));

/**
 * @brief Sets the format of raw video data.
 *
 * @details
 * You need to register the callback when calling the `setVideoFrameDelegate:` method. This callback
 * is triggered when the media player receives every video frame. You can set the desired video data
 * format in the return value of this callback.
 *
 * @return
 * The desired format you set for the raw video data. See `AgoraVideoFormat`.
 * By default, the original video pixel format ( AgoraVideoFormatDefault ) may be I420 or
 * CVPixelBufferRef.
 */
- (AgoraVideoFormat)AgoraRtcMediaPlayerGetVideoPixelFormat NS_SWIFT_NAME(AgoraRtcMediaPlayerGetVideoPixelFormat());

@end