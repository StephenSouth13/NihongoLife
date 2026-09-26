//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraConstants.h"
#import "AgoraObjects.h"

@protocol AgoraRtcMediaPlayerProtocol;

@protocol AgoraRtcMediaPlayerDelegate <NSObject>
@optional

/**
 * @brief Reports the changes of playback state.
 *
 * @details
 * When the state of the media player changes, the SDK triggers this callback to report the current
 * playback state.
 *
 * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
 * @param state The playback state. See `AgoraMediaPlayerState`.
 * @param reason The reason for the changes in the media player status. See
 * `AgoraMediaPlayerReason`.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
          didChangedToState:(AgoraMediaPlayerState)state
                      reason:(AgoraMediaPlayerReason)reason NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didChangedTo:reason:));

/** Reports current playback progress.

 The callback occurs once every one second during the playback and reports
 current playback progress.

 @param playerKit AgoraMediaPlayer

@param positionMs Current playback progress (ms).
 @param timestampMs The NTP timestamp (ms) when the position is sent.
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
       didChangedToPosition:(NSInteger)positionMs
                atTimestamp:(NSTimeInterval)timestampMs NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didChangedTo:atTimestamp:));

 /**
  * @brief Reports the player events.
  *
  * @details
  * - After calling the `seekToPosition:` method, the SDK triggers the callback to report the results
  * of the seek operation.
  *
  * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
  * @param eventCode The player event. See `AgoraMediaPlayerEvent`.
  * @param elapsedTime The time (ms) when the event occurs.
  * @param message Information about the event.
  *
  */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
              didOccurEvent:(AgoraMediaPlayerEvent)eventCode
                elapsedTime:(NSInteger)elapsedTime
                    message:(NSString *_Nullable)message NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didOccur:elapsedTime:message:));

/**
 * @brief Occurs when the media metadata is received.
 *
 * @details
 * The callback occurs when the player receives the media metadata and reports the detailed
 * information of the media metadata.
 *
 * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
 * @param data The detailed data of the media metadata.
 * @param length The data length (bytes).
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
             didReceiveData:(NSString *_Nullable)data
                     length:(NSInteger)length NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didReceiveData:length:));

/**
 * @brief Reports the playback duration that the buffered data can support.
 *
 * @details
 * When playing online media resources, the SDK triggers this callback every two seconds to report
 * the playback duration that the currently buffered data can support.
 * - When the playback duration supported by the buffered data is less than the threshold (0 by
 * default), the SDK returns `AgoraMediaPlayerEventBufferLow` (6).
 * - When the playback duration supported by the buffered data is greater than the threshold (0 by
 * default), the SDK returns `AgoraMediaPlayerEventBufferRecover` (7).
 *
 * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
 * @param playCachedBuffer The playback duration (ms) that the buffered data can support.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
             didPlayBufferUpdated:(NSInteger)playCachedBuffer NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didPlayBufferUpdated:));

/**
 * @brief Reports the events of preloaded media resources.
 *
 * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
 * @param event Events that occur when media resources are preloaded. See
 * `AgoraMediaPlayerPreloadEvent`.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
            didPreloadEvent:(AgoraMediaPlayerPreloadEvent)event NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:didPreloadEvent:));

/**
 * @brief Reports the playback progress of the media file.
 *
 * @details
 * When playing media files, the SDK triggers this callback every two second to report current
 * playback progress.
 *
 * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
 * @param to The media player source information after the change. See `AgoraMediaPlayerSrcInfo`.
 * @param from The media player source information before the change. See `AgoraMediaPlayerSrcInfo`.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit playerSrcInfoDidChange:(AgoraMediaPlayerSrcInfo *_Nonnull)to from:(AgoraMediaPlayerSrcInfo *_Nonnull)from NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:playerSrcInfoDidChange:from:));

/**
 * @brief Occurs when information related to the media player changes.
 *
 * @details
 * When the information about the media player changes, the SDK triggers this callback. You can use
 * this callback for troubleshooting.
 *
 * @param playerKit See `AgoraRtcMediaPlayerProtocol`.
 * @param info Information related to the media player. See `AgoraMediaPlayerUpdatedInfo`.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit infoUpdated:(AgoraMediaPlayerUpdatedInfo *_Nonnull)info NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:infoUpdated:));

/**
 * @brief Reports the statistics of the media file being cached.
 *
 * @details
 * After you call the `openWithMediaSource:` method and set `enableCache` as `YES`, the SDK triggers
 * this callback once per second to report the statistics of the media file being cached.
 *
 * @param info The statistics of the media file being cached. See `AgoraMediaPlayerCacheStatistics`.
 * @param playerKit The `AgoraRtcMediaPlayerProtocol` instance.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit cacheStats:(AgoraMediaPlayerCacheStatistics *_Nonnull)info NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:cacheStats:));

/**
 * @brief Reports the statistics of the media file being played.
 *
 * @details
 * The SDK triggers this callback once per second to report the statistics of the media file being
 * played.
 *
 * @param info The statistics of the media file. See `AgoraMediaPlayerPlaybackStats`.
 * @param playerKit The `AgoraRtcMediaPlayerProtocol` instance.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit playbackStats:(AgoraMediaPlayerPlaybackStats *_Nonnull)info NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:playbackStats:));

/**
 * @brief AgoraCDN Token has expired and needs to be set up with renewAgoraCDNSrcToken(const char*
 * src).
 * 
 * @deprecated 4.6.0
 * 
 */
- (void)onAgoraCDNTokenWillExpire NS_SWIFT_NAME(onAgoraCDNTokenWillExpire());

/**
 * @brief Reports the volume of the media player.
 *
 * @details
 * The SDK triggers this callback every 200 milliseconds to report the current volume of the media
 * player.
 *
 * @param volume The volume of the media player. The value ranges from 0 to 255.
 * @param playerKit The `AgoraRtcMediaPlayerProtocol` instance.
 *
 */
- (void)AgoraRtcMediaPlayer:(id<AgoraRtcMediaPlayerProtocol> _Nonnull)playerKit
             volumeIndicationDidReceive:(NSInteger)volume NS_SWIFT_NAME(AgoraRtcMediaPlayer(_:volumeIndicationDidReceive:));
@end
