//
//  AgoraRtcEngineKit.h
//  AgoraRtcEngineKit
//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraEnumerates.h"

@class AgoraOutputVideoFrame;
@protocol AgoraVideoFrameDelegate <NSObject>
@optional

/**
 * @brief Occurs each time the SDK receives a video frame captured by local devices.
 *
 * @details
 * You can get raw video data collected by the local device through this callback and preprocess it
 * as needed. Once the preprocessing is complete, you can directly modify `videoFrame` in this
 * callback, and set the return value to `YES` to send the modified video data to the SDK.
 * If you need to send the preprocessed data to the SDK, you need to call `getVideoFrameProcessMode`
 * first to set the video processing mode to read and write mode (
 * AgoraVideoFrameProcessModeReadWrite ).
 * Applicable scenarios: - Preprocess the locally collected video data before it is processed by the
 * SDK. For example, get video data through this callback and process it with filters, watermarks,
 * cropping, rotation, etc.
 * - Get information about the locally collected video data before it is processed by the SDK. For
 * example, the original width, height, frame rate of the video frame, etc.
 * Call timing: After the successful registration of the video data observer, each time the SDK
 * captures a video frame.
 *
 * @note
 * - If the video data type you get is RGBA, the SDK does not support processing the data of the
 * alpha channel.
 * - It is recommended that you ensure the modified parameters in `videoFrame` are consistent with
 * the actual situation of the video frames in the video frame buffer. Otherwise, it may cause
 * unexpected rotation, distortion, and other issues in the local preview and remote video display.
 * The default video format that you get from this callback is I420 or CVPixelBufferRef. If you need
 * other formats, you can set the expected data format in the `getVideoFormatPreference` callback.
 *
 * @param sourceType Video source types, including cameras, screens, or media player. See
 * `AgoraVideoSourceType`.
 * @param videoFrame The video frame. See `AgoraOutputVideoFrame`.Note: The default value of the
 * video frame data format obtained through this callback is as follows:
 * - iOS/macOS: I420 or CVPixelBufferRef
 *
 * @return
 * - When the video processing mode is `AgoraVideoFrameProcessModeReadOnly`:
 *   - `YES`: Reserved for future use.
 *   - `NO`: Reserved for future use.
 * - When the video processing mode is `AgoraVideoFrameProcessModeReadWrite`:
 *   - `YES`: Sets the SDK to receive the video frame.
 *   - `NO`: Sets the SDK to discard the video frame.
 */
- (BOOL)onCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame sourceType:(AgoraVideoSourceType)sourceType;

/**
 * @brief Occurs each time the SDK receives a video frame before encoding.
 *
 * @details
 * After you successfully register the video frame observer, the SDK triggers this callback each
 * time it receives a video frame. In this callback, you can get the video data before encoding and
 * then process the data according to your particular scenarios.
 * After processing, you can send the processed video data back to the SDK in this callback.
 *
 * @note
 * - If you need to send the preprocessed data to the SDK, you need to call
 * `getVideoFrameProcessMode` first to set the video processing mode to read and write mode (
 * AgoraVideoFrameProcessModeReadWrite ).
 * - To get the video data captured from the second screen before encoding, you need to set
 * `AgoraVideoModulePositionPreEncoder` (1 << 2) as a frame position through
 * `getObservedFramePosition`.
 * - The video data that this callback gets has been preprocessed, with its content cropped and
 * rotated, and the image enhanced.
 * - It is recommended that you ensure the modified parameters in `videoFrame` are consistent with
 * the actual situation of the video frames in the video frame buffer. Otherwise, it may cause
 * unexpected rotation, distortion, and other issues in the local preview and remote video display.
 *
 * @param sourceType The type of the video source. See `AgoraVideoSourceType`.
 * @param videoFrame The video frame. See `AgoraOutputVideoFrame`.Note: The default value of the
 * video frame data format obtained through this callback is as follows:
 * - iOS/macOS: I420 or CVPixelBufferRef
 *
 * @return
 * - When the video processing mode is `AgoraVideoFrameProcessModeReadOnly`:
 *   - `YES`: Reserved for future use.
 *   - `NO`: Reserved for future use.
 * - When the video processing mode is `AgoraVideoFrameProcessModeReadWrite`:
 *   - `YES`: Sets the SDK to receive the video frame.
 *   - `NO`: Sets the SDK to discard the video frame.
 */
- (BOOL)onPreEncodeVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame sourceType:(AgoraVideoSourceType)sourceType;


- (BOOL)onTranscodedVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onTranscodedVideoFrame(_:));

/**
 * @brief Occurs each time the SDK receives a video frame sent by the remote user.
 *
 * @details
 * After you successfully register the video frame observer, the SDK triggers this callback each
 * time it receives a video frame. In this callback, you can get the video data sent from the remote
 * end before rendering, and then process it according to the particular scenarios.
 * The default video format that you get from this callback is I420 or CVPixelBufferRef. If you need
 * other formats, you can set the expected data format in the `getVideoFormatPreference` callback.
 *
 * @note
 * - If you need to send the preprocessed data to the SDK, you need to call
 * `getVideoFrameProcessMode` first to set the video processing mode to read and write mode (
 * AgoraVideoFrameProcessModeReadWrite ).
 * - If the video data type you get is RGBA, the SDK does not support processing the data of the
 * alpha channel.
 * - It is recommended that you ensure the modified parameters in `videoFrame` are consistent with
 * the actual situation of the video frames in the video frame buffer. Otherwise, it may cause
 * unexpected rotation, distortion, and other issues in the local preview and remote video display.
 *
 * @param uid The user ID of the remote user who sends the current video frame.
 * @param videoFrame The video frame. See `AgoraOutputVideoFrame`.Note: The default value of the
 * video frame data format obtained through this callback is as follows:
 * - iOS/macOS: I420 or CVPixelBufferRef
 * @param channelId The channel ID.
 *
 * @return
 * - When the video processing mode is `AgoraVideoFrameProcessModeReadOnly`:
 *   - `YES`: Reserved for future use.
 *   - `NO`: Reserved for future use.
 * - When the video processing mode is `AgoraVideoFrameProcessModeReadWrite`:
 *   - `YES`: Sets the SDK to receive the video frame.
 *   - `NO`: Sets the SDK to discard the video frame.
 */
- (BOOL)onRenderVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame uid:(NSUInteger)uid channelId:(NSString * _Nonnull)channelId NS_SWIFT_NAME(onRenderVideoFrame(_:uid:channelId:));

/**
 * Occurs each time the SDK receives a video frame decoded by the MediaPlayer.
 *
 * After you successfully register the video frame observer, the SDK triggers this callback each
 * time a video frame is decoded. In this callback, you can get the video data decoded by the
 * MediaPlayer. You can then pre-process the data according to your scenarios.
 *
 * After pre-processing, you can send the processed video data back to the SDK by setting the
 * `videoFrame` parameter in this callback.
 * 
 * @note
 * - This callback will not be affected by the return values of \ref getVideoFrameProcessMode "getVideoFrameProcessMode", \ref getRotationApplied "getRotationApplied", \ref getMirrorApplied "getMirrorApplied", \ref getObservedFramePosition "getObservedFramePosition".
 *
 * @param videoFrame A pointer to the video frame: AgoraOutputVideoFrame
 * @param mediaPlayerId of the mediaPlayer.
 * @return Determines whether to ignore the current video frame if the pre-processing fails:
 * - true: Do not ignore.
 * - false: Ignore, in which case this method does not sent the current video frame to the SDK.
 */
- (BOOL)onMediaPlayerVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame mediaPlayerId:(NSInteger)mediaPlayerId NS_SWIFT_NAME(onMediaPlayerVideoFrame(_:mediaPlayerId:));

/**
 * @brief Occurs each time the SDK receives a video frame, and prompts you whether to rotate the
 * captured video.
 *
 * @details
 * If the video has been rotated according to `rotation` in `AgoraOutputVideoFrame` on the capture
 * device, you do not need to call this method to set the video rotation.
 *
 * @note
 * - On iOS, the supported video data formats for this callback are: I420, RGBA,
 * and CVPixelBuffer.
 * - On macOS, the supported video data formats for this callback are: I420 and
 * RGBA.
 *
 * @return
 * Sets whether to rotate the captured video:
 * - `YES`: Rotate the captured video.
 * - `NO`: (Default) Do not rotate the captured video.
 */
- (BOOL)getRotationApplied NS_SWIFT_NAME(getRotationApplied());

/**
 * @brief Occurs each time the SDK receives a video frame and prompts you whether or not to mirror
 * the captured video.
 *
 * @details
 * If the video data you want to obtain is a mirror image of the original video, you need to
 * register this callback when calling `setVideoFrameDelegate:`. After you successfully register the
 * video frame observer, the SDK triggers this callback each time it receives a video frame. You
 * need to set whether or not to mirror the video frame in the return value of this callback.
 *
 * @note
 * - On iOS, the supported video data formats for this callback are: I420, RGBA, and
 * CVPixelBuffer.
 * - On macOS, the supported video data formats for this callback are: I420 and RGBA.
 * - Both this method and the `setVideoEncoderConfiguration:` method support setting the mirroring
 * effect. Agora recommends that you only choose one method to set it up. Using both methods at the
 * same time causes the mirroring effect to overlap, and the mirroring settings fail.
 *
 * @return
 * Sets whether or not to mirror the captured video:
 * - `YES`: Mirror the captured video.
 * - `NO`: (Default) Do not mirror the captured video.
 */
- (BOOL)getMirrorApplied NS_SWIFT_NAME(getMirrorApplied());

/**
 * @brief Occurs each time the SDK receives a video frame and prompts you to set the process mode of
 * the video frame.
 *
 * @details
 * After you successfully register the video frame observer, the SDK triggers this callback each
 * time it receives a video frame. You need to set your preferred process mode in the return value
 * of this callback.
 *
 * @return
 * See `AgoraVideoFrameProcessMode`.
 */
- (AgoraVideoFrameProcessMode)getVideoFrameProcessMode NS_SWIFT_NAME(getVideoFrameProcessMode());

/**
 * @brief Sets the format of the raw video data output by the SDK.
 *
 * @details
 * You need to register the callback when calling the `setVideoFrameDelegate:` method. After you
 * successfully register the video frame observer, the SDK triggers this callback each time it
 * receives a video frame. You need to set your preferred video data in the return value of this
 * callback.
 *
 * @note
 * The default types of pixel format ( AgoraVideoFormatDefault ) for the raw video are as follows:
 * - The default video frame type may be I420 or CVPixelBufferRef.
 *
 * @return
 * Sets the raw data format of the SDK output. See `AgoraVideoFormat`.
 */
- (AgoraVideoFormat)getVideoFormatPreference NS_SWIFT_NAME(getVideoFormatPreference());

/**
 * @brief Sets the frame position for the video observer.
 *
 * @details
 * After successfully registering the video data observer, the SDK uses this callback to determine
 * whether to trigger `onCaptureVideoFrame:sourceType:`, `onRenderVideoFrame:uid:channelId:` and
 * `onPreEncodeVideoFrame:sourceType:` callback at each specific video frame processing position, so
 * that you can observe the locally collected video data, the video data sent by the remote end, and
 * the video data before encoding. You can set one or more positions you need to observe by
 * modifying the return value according to your scenario:
 * - `AgoraVideoModulePositionPostCapture` (1 << 0): The position after capturing the video data,
 * which corresponds to the `onCaptureVideoFrame:sourceType:` callback.
 * - `AgoraVideoModulePositionPreRenderer` (1 << 1): The position of the received remote video data
 * before rendering, which corresponds to the `onRenderVideoFrame:uid:channelId:` callback.
 * - `AgoraVideoModulePositionPreEncoder` (1 << 2): The position before encoding the video data,
 * which corresponds to the `onPreEncodeVideoFrame:sourceType:` callback.
 *
 * @note
 * - Use '|' (the OR operator) to observe multiple frame positions.
 * - This callback observes `AgoraVideoModulePositionPostCapture` (1 << 0) and
 * `AgoraVideoModulePositionPreRenderer` (1 << 1) by default.
 * - To conserve system resources, you can reduce the number of frame positions that you want to
 * observe.
 * - When the video processing mode is `AgoraVideoFrameProcessModeReadWrite` and the observation
 * position is set to `AgoraVideoModulePositionPreEncoder` | `AgoraVideoModulePositionPostCapture`,
 * the `getMirrorApplied` does not take effect; you need to modify the video processing mode or the
 * position of the observer.
 *
 * @return
 * A bit mask that controls the frame position of the video observer. See `AgoraVideoFramePosition`.
 */
- (AgoraVideoFramePosition)getObservedFramePosition NS_SWIFT_NAME(getObservedFramePosition());

/* Unavailable Delegate Methods */
#if TARGET_OS_IPHONE
- (BOOL)onCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onCapture(_:)) __attribute__((availability(ios,deprecated=7_0,message="Use onCaptureVideoFrame:sourceType: instead.")));
- (BOOL)onSecondaryCameraCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onSecondaryCameraCapture(_:)) __attribute__((availability(ios,deprecated=7_0,message="Use onCaptureVideoFrame:sourceType: instead.")));
- (BOOL)onScreenCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onScreenCapture(_:)) __attribute__((availability(ios,deprecated=7_0,message="Use onCaptureVideoFrame:sourceType: instead.")));
- (BOOL)onSecondaryScreenCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onSecondaryScreenCapture(_:)) __attribute__((availability(ios,deprecated=7_0,message="Use onCaptureVideoFrame:sourceType: instead.")));
- (BOOL)onPreEncodeVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onPreEncode(_:)) __attribute__((availability(ios,deprecated=7_0,message="Use onPreEncodeVideoFrame:sourceType: instead.")));
- (BOOL)onPreEncodeScreenVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onPreEncodeScreenVideoFrame(_:)) __attribute__((availability(ios,deprecated=7_0,message="Use onPreEncodeVideoFrame:sourceType: instead.")));
#endif

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
- (BOOL)onCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onCapture(_:)) __attribute__((availability(macos,deprecated=10_9,message="Use onCaptureVideoFrame:sourceType: instead.")));
- (BOOL)onSecondaryCameraCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onSecondaryCameraCapture(_:)) __attribute__((availability(macos,deprecated=10_9,message="Use onCaptureVideoFrame:sourceType: instead.")));
- (BOOL)onScreenCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onScreenCapture(_:)) __attribute__((availability(macos,deprecated=10_9,message="Use onCaptureVideoFrame:sourceType: instead.")));
- (BOOL)onSecondaryScreenCaptureVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onSecondaryScreenCapture(_:)) __attribute__((availability(macos,deprecated=10_9,message="Use onCaptureVideoFrame:sourceType: instead.")));
- (BOOL)onPreEncodeVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onPreEncode(_:)) __attribute__((availability(macos,deprecated=10_9,message="Use onPreEncodeVideoFrame:sourceType: instead.")));
- (BOOL)onPreEncodeScreenVideoFrame:(AgoraOutputVideoFrame * _Nonnull)videoFrame NS_SWIFT_NAME(onPreEncodeScreenVideoFrame(_:)) __attribute__((availability(macos,deprecated=10_9,message="Use onPreEncodeVideoFrame:sourceType: instead.")));
#endif

@end
