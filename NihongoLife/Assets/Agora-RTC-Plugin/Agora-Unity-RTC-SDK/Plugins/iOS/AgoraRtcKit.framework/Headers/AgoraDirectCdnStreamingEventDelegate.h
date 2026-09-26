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

/**
 * The event handler for direct cdn streaming
 * 
 * @deprecated v4.6.0.
 *
 */
@protocol AgoraDirectCdnStreamingEventDelegate <NSObject>
@optional

/**
 * @brief Occurs when the CDN streaming state changes.
 *
 * @details
 * When the host directly pushes streams to the CDN, if the streaming state changes, the SDK
 * triggers this callback to report the changed streaming state, error codes, and other information.
 * You can troubleshoot issues by referring to this callback.
 *
 * @param state The current CDN streaming state. See `AgoraDirectCdnStreamingState`.
 * @param reason Reasons for changes in the status of CDN streaming. See
 * `AgoraDirectCdnStreamingReason`.
 * @param message The information about the changed streaming state.
 *
 */
- (void)onDirectCdnStreamingStateChanged:(AgoraDirectCdnStreamingState)state
                                   reason:(AgoraDirectCdnStreamingReason)reason
                                 message:(NSString *_Nullable)message NS_SWIFT_NAME(onDirectCdnStreamingStateChanged(_:reason:message:));

/**
 * @brief Reports the CDN streaming statistics.
 *
 * @details
 * When the host directly pushes media streams to the CDN, the SDK triggers this callback every one
 * second.
 *
 * @param stats The statistics of the current CDN streaming. See `AgoraDirectCdnStreamingStats`.
 *
 */
- (void)onDirectCdnStreamingStats:(AgoraDirectCdnStreamingStats *_Nonnull)stats NS_SWIFT_NAME(onDirectCdnStreamingStats(_:));

@end