//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>

@class AgoraMediaRecorder;
@class AgoraMediaRecorderInfo;
typedef NS_ENUM(NSInteger, AgoraMediaRecorderState);
typedef NS_ENUM(NSInteger, AgoraMediaRecorderReasonCode);

@protocol AgoraMediaRecorderDelegate <NSObject>
#pragma mark Media Recorder Delegate Methods
/**
 * @brief Callback for recording state changes.
 *
 * @since v4.0.0
 *
 * @details
 * The SDK triggers this callback when the recording state of the audio and video stream changes. It
 * reports the current recording state and the reason for the change.
 *
 * @param recorder The `AgoraMediaRecorder` instance.
 * @param channelId The channel name.
 * @param uid The user ID.
 * @param state The current recording state. See `AgoraMediaRecorderState`.
 * @param reason The reason for the recording state change. See `AgoraMediaRecorderReasonCode`.
 *
 */
- (void)mediaRecorder:(AgoraMediaRecorder* _Nonnull)recorder stateDidChanged:(NSString * _Nonnull)channelId uid:(NSUInteger)uid state:(AgoraMediaRecorderState)state reason:(AgoraMediaRecorderReasonCode)reason;

/**
 * @brief Recording information update callback.
 *
 * @since v4.0.0
 *
 * @details
 * After you successfully register this callback and start audio and video stream recording, 
 * the SDK periodically triggers this callback based on the value of `recorderInfoUpdateInterval` 
 * that you set in `AgoraMediaRecorderConfiguration`, reporting the current recording file's name,
 * duration, and size.
 *
 * @param channelId The channel name.
 * @param uid The user ID.
 * @param info The recording file information. See `AgoraMediaRecorderInfo`.
 * @param recorder The `AgoraMediaRecorder` object.
 *
 */
- (void)mediaRecorder:(AgoraMediaRecorder* _Nonnull)recorder informationDidUpdated:(NSString * _Nonnull)channelId uid:(NSUInteger)uid info:(AgoraMediaRecorderInfo* _Nonnull)info;

@end
