//
//  AgoraRtcEngineKit.h
//  AgoraRtcEngineKit
//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>

@class AgoraEncodedVideoFrameInfo;

@protocol AgoraEncodedVideoFrameDelegate <NSObject>
@optional

/**
 * @brief Reports that the receiver has received the to-be-decoded video frame sent by the remote
 * end.
 *
 * @details
 * If you call the `setRemoteVideoSubscriptionOptions:options:` method and set `encodedFrameOnly` to
 * `YES`, the SDK triggers this callback locally to report the received encoded video frame
 * information.
 *
 * @since 4.6.0
 * @param videoData The encoded video image data.
 * @param length The data length of the video image.
 * @param videoFrameInfo For the information of the encoded video frame, see
 * `AgoraEncodedVideoFrameInfo`.
 * @param channelId The channel name.
 * @param uid The user id of remote user.
 *
 * @return
 * Without practical meaning.
 */
- (BOOL)onEncodedVideoFrameReceived:(NSData * _Nonnull )videoData length:(size_t)length info:(AgoraEncodedVideoFrameInfo * _Nonnull)videoFrameInfo channelId:(NSString * _Nonnull)channelId uid:(NSUInteger)uid NS_SWIFT_NAME(onEncodedVideoFrameReceived(_:length:info:channelId:uid:));
@end
