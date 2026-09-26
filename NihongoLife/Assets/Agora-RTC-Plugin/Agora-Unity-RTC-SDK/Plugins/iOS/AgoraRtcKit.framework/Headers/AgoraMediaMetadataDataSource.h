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

/** The definition of the AgoraMediaMetadataDataSource protocol.
 * @note Implement all the callbacks in this protocol in the critical thread. We recommend avoiding any time-consuming
 * operation in the critical thread.
*/
@protocol AgoraMediaMetadataDataSource <NSObject>
@required

/**
 * @brief Occurs when the SDK requests the maximum size of the metadata.
 *
 * @details
 * After successfully complete the registration by calling `setMediaMetadataDelegate:withType:`, the
 * SDK triggers this callback once every video frame is sent. You need to specify the maximum size
 * of the metadata in the return value of this callback.
 *
 * @return
 * The maximum size of the `buffer` of the metadata that you want to use. The highest value is 1024
 * bytes. Ensure that you set the return value.
 */
- (NSInteger)metadataMaxSize NS_SWIFT_NAME(metadataMaxSize());

/**
 * @brief Occurs when the SDK is ready to send metadata.
 *
 * @details
 * This callback is triggered when the SDK is ready to send metadata.
 *
 * @note Ensure that the size of the metadata does not exceed the value set in the `metadataMaxSize`
 * callback.
 *
 * @param timestamp The timestamp.
 * @param sourceType Video data type. See `AgoraVideoSourceType`.
 *
 * @return
 * The metadata that you want to send in the format of NSData, which includes the following
 * parameters:
 * - `uid`: The ID of the remote user who sends the metadata.
 * - `size`: The size of the metadata.
 * - `buffer`: The metadata data.
 * - `timeStampMs`: The NTP timestamp (ms) when the metadata is sent.
 */
- (NSData * _Nullable)readyToSendMetadataAtTimestamp:(NSTimeInterval)timestamp sourceType:(AgoraVideoSourceType)sourceType NS_SWIFT_NAME(readyToSendMetadata(atTimestamp:sourceType:));

@end
