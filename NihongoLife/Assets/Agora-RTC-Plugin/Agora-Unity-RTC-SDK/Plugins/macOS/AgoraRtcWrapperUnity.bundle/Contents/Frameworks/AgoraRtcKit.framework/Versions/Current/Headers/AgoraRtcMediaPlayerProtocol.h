//
//  AgoraRtcMediaPlayerProtocol.h
//  AgoraRtcMediaPlayerProtocol
//
//  Copyright (c) 2020 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraObjects.h"
#if TARGET_OS_IPHONE
#import <UIKit/UIKit.h>
typedef UIView View;
#elif TARGET_OS_MAC
#import <AppKit/AppKit.h>
typedef NSView View;
#endif

@protocol AgoraRtcAudioSpectrumDelegateocol;
@protocol AgoraRtcMediaPlayerAudioFrameDelegate;
@protocol AgoraRtcMediaPlayerVideoFrameDelegate;
@protocol AgoraAudioSpectrumDelegate;

NS_ASSUME_NONNULL_BEGIN
@protocol AgoraRtcMediaPlayerProtocol <NSObject>

/**
 * @brief Gets the ID of the media player.
 *
 * @return
 * - Success. The ID of the media player.
 * - < 0: Failure.
 */
- (int)getMediaPlayerId NS_SWIFT_NAME(getMediaPlayerId());
/**
 * @brief Opens the media resource.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 * Related callbacks: After calling this method, the SDK triggers the
 * `AgoraRtcMediaPlayer:didChangedToState:reason:` callback. After receiving the report of the
 * playback status as `AgoraMediaPlayerStateOpenCompleted`, you can call the `play` method to play
 * the media file.
 *
 * @note This method is called asynchronously.
 *
 * @param url The path of the media file. Both local path and online path are supported.
 * @param startPos The starting position (ms) for playback. Default value is 0.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)open:(NSString *)url startPos:(NSInteger)startPos NS_SWIFT_NAME(open(_:startPos:));


/**
 * @brief Opens a media file and configures the playback scenarios.
 *
 * @details
 * This method supports opening different types of media files, including files starting with
 * `ipod-library://`, and setting the playback options.
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @note This method is called asynchronously. If you need to play a media file, make sure you
 * receive the `AgoraRtcMediaPlayer:didChangedToState:reason:` callback reporting
 * `AgoraMediaPlayerStateOpenCompleted` before calling the `play` method to play the file.
 *
 * @param source Media resources. See `AgoraMediaSource`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)openWithMediaSource:(AgoraMediaSource *)source NS_SWIFT_NAME(open(with:));

/**
 * Open the Agora CDN media source.
 * 
 * @deprecated 4.6.0
 * 
 * @param src The src of the media file that you want to play.
 * @param startPos The  playback position (ms).
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)openWithAgoraCDNSrc:(NSString *)src startPos:(NSInteger)startPos NS_SWIFT_NAME(open(withAgoraCDNSrc:startPos:));

/**
 * Gets the number of  Agora CDN lines.
 * 
 * @deprecated 4.6.0
 * 
 * @return
 * - > 0: number of CDN.
 * - <= 0: Failure.
 */
- (int)getAgoraCDNLineCount NS_SWIFT_NAME(getAgoraCDNLineCount());

/**
 * Switch Agora CDN lines.
 * 
 * @deprecated 4.6.0
 * 
 * @param index Specific line.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)switchAgoraCDNLineByIndex:(int)index NS_SWIFT_NAME(switchAgoraCDNLine(by:));

/**
 * Gets the line of the current CDN.
 * 
 * @deprecated 4.6.0
 * 
 * @return
 * - >= 0: Specific line.
 * - < 0: Failure.
 */
- (int)getCurrentAgoraCDNIndex NS_SWIFT_NAME(getCurrentAgoraCDNIndex());

/**
 * Enable automatic CDN line switching.
 * 
 * @deprecated 4.6.0
 * 
 * @param enable Whether enable.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)enableAutoSwitchAgoraCDN:(BOOL)enable NS_SWIFT_NAME(enableAutoSwitchAgoraCDN(_:));

/**
 * Update the CDN source token and timestamp.
 * 
 * @deprecated 4.6.0
 * 
 * @param token token.
 * @param ts ts.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)renewAgoraCDNSrcToken:(NSString *)token ts:(NSInteger)ts NS_SWIFT_NAME(renewAgoraCDNSrcToken(_:ts:));

/**
 * Switch the CDN source.
 * 
 * @deprecated 4.6.0
 * 
 * @param src Specific line.
 * @param syncPts Live streaming must be set to false.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)switchAgoraCDNSrc:(NSString *)src syncPts:(BOOL)syncPts NS_SWIFT_NAME(switchAgoraCDNSrc(_:syncPts:));

/**
 * @brief Plays the media file.
 *
 * @details
 * Call timing: - Call this method after calling `open:startPos:` or `openWithMediaSource:` opening
 * a media file and receiving a `AgoraRtcMediaPlayer:didChangedToState:reason:` callback reporting
 * the status as AgoraMediaPlayerStateOpenCompleted.
 * - Call the method after calling `seekToPosition:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)play NS_SWIFT_NAME(play());

/**
 * @brief Pauses the playback.
 *
 * @details
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)pause NS_SWIFT_NAME(pause());

/**
 * @brief Stops playing the media track.
 *
 * @details
 * After calling this method to stop playback, if you want to play again, you need to call
 * `open:startPos:` or `openWithMediaSource:` to open the media resource.
 * Call timing: Call this method after play.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)stop NS_SWIFT_NAME(stop());

/**
 * @brief Resumes playing the media file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)resume NS_SWIFT_NAME(resume());

/**
 * @brief Seeks to a new playback position.
 *
 * @details
 * - If you call `seekToPosition:` after the playback has completed (upon receiving callback
 * `AgoraRtcMediaPlayer:didChangedToState:reason:` reporting playback status as
 * AgoraMediaPlayerStatePlayBackCompleted or AgoraMediaPlayerStatePlayBackAllLoopsCompleted ), the
 * SDK will play the media file from the specified position. At this point, you will receive
 * callback `AgoraRtcMediaPlayer:didChangedToState:reason:` reporting playback status as
 * AgoraMediaPlayerStatePlaying.
 * - If you call `seekToPosition:` while the playback is paused, upon successful call of this
 * method, the SDK will seek to the specified position. To resume playback, call `resume` or `play`
 * .
 * Call timing: You can call this method either before or after joining a channel.
 * Related callbacks: After successfully calling this method, you will receive the
 * `AgoraRtcMediaPlayer:didOccurEvent:elapsedTime:message:` callback, reporting the result of the
 * seek operation to the new playback position.
 *
 * @param position The new playback position (ms).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)seekToPosition:(NSInteger)position NS_SWIFT_NAME(seek(toPosition:));

/**
 * @brief Sets the pitch of the current media resource.
 *
 * @note Call this method after calling `open:startPos:`.
 *
 * @param pitch Sets the pitch of the local music file by the chromatic scale. The default value is
 * 0, which means keeping the original pitch. The value ranges from -12 to 12, and the pitch value
 * between consecutive values is a chromatic value. The greater the absolute value of this
 * parameter, the higher or lower the pitch of the local music file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioPitch:(NSInteger)pitch NS_SWIFT_NAME(setAudioPitch(_:));

/**
 * @brief Gets the duration of the media resource.
 *
 * @return
 * - If the method call succeeds, the SDK returns the total duration(ms) of the media file.
 * - < 0: Failure.
 */
- (NSInteger)getDuration NS_SWIFT_NAME(getDuration());

/**
 * @brief Gets the audio buffer delay when playing the media file.
 * @return The audio buffer delay (ms), if the method call succeeds.
 *         < 0: An error occurs. You can use MediaPlayerReason to check the reason.
 */
- (NSInteger)getAudioBufferDelay NS_SWIFT_NAME(getAudioBufferDelay());

/**
 * @brief Gets current local playback progress.
 *
 * @return
 * - Returns the current playback progress (ms) if the call succeeds.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (NSInteger)getPosition NS_SWIFT_NAME(getPosition());
/**
 * @brief Gets the number of the media streams in the media resource.
 *
 * @note Call this method after you call `open:startPos:` and receive the
 * `AgoraRtcMediaPlayer:didChangedToState:reason:` callback reporting the state
 * `AgoraMediaPlayerStateOpenCompleted`.
 *
 * @return
 * - The number of the media streams in the media resource if the method call succeeds.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (NSInteger)getStreamCount NS_SWIFT_NAME(getStreamCount());

/**
 * @brief Gets the detailed information of the media stream.
 *
 * @details
 * Call timing: Call this method after calling `getStreamCount`.
 *
 * @param index The index of the media stream. This parameter must be less than the return value of
 * `getStreamCount`.
 *
 * @return
 * - If the call succeeds, returns the detailed information of the media stream. See
 * `AgoraRtcMediaStreamInfo`.
 * - `nil` is returned, if the method call fails.
 */
- (AgoraRtcMediaStreamInfo *_Nullable)getStreamByIndex:(int)index NS_SWIFT_NAME(getStreamBy(_:));

/**
 * @brief Sets the loop playback.
 *
 * @details
 * If you want to loop, call this method and set the number of the loops.
 * When the loop finishes, the SDK triggers `AgoraRtcMediaPlayer:didChangedToState:reason:` and
 * reports the playback state as AgoraMediaPlayerStatePlayBackAllLoopsCompleted.
 *
 * @param loopCount The number of times the audio effect loops:
 * - ≥0: Number of times for playing. For example, setting it to 0 means no loop playback, playing
 * only once; setting it to 1 means loop playback once, playing a total of twice.
 * - -1: Play the audio file in an infinite loop.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setLoopCount:(int)loopCount NS_SWIFT_NAME(setLoopCount(_:));

/**
 * @brief Sets the channel mode of the current audio file.
 *
 * @details
 * Call this method after calling `open:startPos:`.
 *
 * @param speed The playback speed. Agora recommends that you set this to a value between 30 and
 * 400, defined as follows:
 * - 30: 0.3 times the original speed.
 * - 100: The original speed.
 * - 400: 4 times the original speed.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setPlaybackSpeed:(int)speed NS_SWIFT_NAME(setPlaybackSpeed(_:));

/**
 * @brief Selects the audio track used during playback.
 *
 * @details
 * After getting the track index of the audio file, you can call this method to specify any track to
 * play. For example, if different tracks of a multi-track file store songs in different languages,
 * you can call this method to set the playback language.
 *
 * @note You need to call this method after calling `getStreamByIndex:` to get the audio stream
 * index value.
 *
 * @param index The index of the audio track.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)selectAudioTrack:(int)index NS_SWIFT_NAME(selectAudioTrack(_:));

/**
 * @brief Selects the audio tracks that you want to play on your local device and publish to the
 * channel respectively.
 *
 * @details
 * You can call this method to determine the audio track to be played on your local device and
 * published to the channel.
 * Before calling this method, you need to open the media file with the `openWithMediaSource:`
 * method and set `enableMultiAudioTrack` in `AgoraMediaSource` as `YES`.
 * Applicable scenarios: For example, in KTV scenarios, the host can choose to play the original
 * sound locally and publish the accompaniment track to the channel.
 *
 * @param playoutTrackIndex The index of audio tracks for local playback. You can obtain the index
 * through `getStreamByIndex:`.
 * @param publishTrackIndex The index of audio tracks to be published in the channel. You can obtain
 * the index through `getStreamByIndex:`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)selectMultiAudioTrack:(NSInteger)playoutTrackIndex publishTrackIndex:(NSInteger)publishTrackIndex NS_SWIFT_NAME(selectMultiAudioTrack(_:publishTrackIndex:));

/**
 * take screenshot while playing  video
 * @param filename the filename of screenshot file
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)takeScreenshot:(NSString *)filename NS_SWIFT_NAME(takeScreenshot(_:));

/**
 * select internal subtitles in video
 * @param index the index of the internal subtitles
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)selectInternalSubtitle:(int)index NS_SWIFT_NAME(selectInternalSubtitle(_:));

/**
 * set an external subtitle for video
 * @param url The URL of the subtitle file that you want to load.
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setExternalSubtitle:(NSString *)url NS_SWIFT_NAME(setExternalSubtitle(_:));

/**
 * @brief Gets current playback state.
 *
 * @return
 * The current playback state. See `AgoraMediaPlayerState`.
 */
- (AgoraMediaPlayerState)getPlayerState NS_SWIFT_NAME(getPlayerState());

/**
 * @brief Sets whether to mute the media file.
 *
 * @details
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @param isMute Whether to mute the media file:
 * - `YES`: Mute the media file.
 * - `NO`: (Default) Unmute the media file.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)mute:(BOOL)isMute NS_SWIFT_NAME(mute(_:));

/**
 * @brief Reports whether the media resource is muted.
 *
 * @return
 * - `YES`: Reports whether the media resource is muted.
 * - `NO`: Reports whether the media resource is muted.
 */
- (BOOL)getMute NS_SWIFT_NAME(getMute());

/**
 * @brief Adjusts the local playback volume.
 *
 * @details
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param volume The local playback volume, which ranges from 0 to 100:
 * - 0: Mute.
 * - 100: (Default) The original volume.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustPlayoutVolume:(int)volume NS_SWIFT_NAME(adjustPlayoutVolume(_:));

/**
 * @brief Gets the local playback volume.
 *
 * @return
 * The local playback volume, which ranges from 0 to 100.
 * - 0: Mute.
 * - 100: (Default) The original volume.
 */
- (int)getPlayoutVolume NS_SWIFT_NAME(getPlayoutVolume());

/**
 * @brief Adjusts the volume of the media file for publishing.
 *
 * @details
 * After connected to the Agora server, you can call this method to adjust the volume of the media
 * file heard by the remote user.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param volume The volume, which ranges from 0 to 400:
 * - 0: Mute.
 * - 100: (Default) The original volume.
 * - 400: Four times the original volume (amplifying the audio signals by four times).
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)adjustPublishSignalVolume:(int)volume NS_SWIFT_NAME(adjustPublishSignalVolume(_:));

/**
 * @brief Gets the volume of the media file for publishing.
 *
 * @return
 * - ≥ 0: The remote playback volume.
 * - < 0: Failure.
 */
- (int)getPublishSignalVolume NS_SWIFT_NAME(getPublishSignalVolume());

/**
 * @brief Sets media player options.
 *
 * @details
 * The media player supports setting options through `key` and `value`.
 * The difference between this method and `setPlayerOptionString:value:` is that the `value`
 * parameter of this method is of type Int, while the `value` of `setPlayerOptionString:value:` is
 * of type String. These two methods cannot be used together.
 * Applicable scenarios: Scenarios that require technical previews or special customization
 * features. In general, you do not need to call this method; you can simply use the default options
 * provided by the media player.
 * Call timing: Call this method before the `open:startPos:` or `openWithMediaSource:` method.
 *
 * @param key The key of the option.
 * @param value The value of the key.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setPlayerOption:(NSString *)key value:(NSInteger)value NS_SWIFT_NAME(setPlayerOption(_:value:));

/**
 * @brief Sets media player options.
 *
 * @details
 * The media player supports setting options through `key` and `value`.
 * The difference between this method and `setPlayerOption:value:` is that the `value` parameter of
 * this method is of type String, while the `value` of `setPlayerOption:value:` is of type String.
 * These two methods cannot be used together.
 * Applicable scenarios: Scenarios that require technical previews or special customization
 * features. In general, you do not need to call this method; you can simply use the default options
 * provided by the media player.
 * Call timing: Call this method before the `open:startPos:` or `openWithMediaSource:` method.
 *
 * @param key The key of the option.
 * @param value The value of the key.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setPlayerOptionString:(NSString *)key value:(NSString *)value NS_SWIFT_NAME(setPlayerOptionString(_:value:));

/**
 * @brief Sets the view.
 *
 * @details
 * Call timing: You can call this method either before or after joining a channel.
 *
 * @param view The render view.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setView:(View *_Nullable)view NS_SWIFT_NAME(setView(_:));

/**
 * @brief Sets the render mode of the media player.
 *
 * @param mode Sets the render mode of the view. See `AgoraVideoRenderMode`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRenderMode:(AgoraMediaPlayerRenderMode)mode NS_SWIFT_NAME(setRenderMode(_:));

/**
 * @brief Gets the path of the media resource being played.
 *
 * @return
 * The path of the media resource being played.
 */
- (NSString *)getPlaySrc NS_SWIFT_NAME(getPlaySrc());

/**
 * @brief Switches the media resource being played.
 *
 * @details
 * You can call this method to switch the media resource to be played according to the current
 * network status. For example:
 * - When the network is poor, the media resource to be played is switched to a media resource
 * address with a lower bitrate.
 * - When the network is good, the media resource to be played is switched to a media resource
 * address with a higher bitrate.
 * After calling this method, if you receive the
 * `AgoraRtcMediaPlayer:didOccurEvent:elapsedTime:message:` callback report the
 * `AgoraMediaPlayerEventSwitchComplete` event, the switching is successful. If the switching fails,
 * the SDK will automatically retry 3 times. If it still fails, you will receive the
 * `AgoraRtcMediaPlayer:didOccurEvent:elapsedTime:message:` callback reporting the
 * `AgoraMediaPlayerEventSwitchError` event indicating an error occurred during media resource
 * switching.
 *
 * @note
 * - Ensure that you call this method after `open:startPos:`.
 * - To ensure normal playback, pay attention to the following when calling this method:
 *   - Do not call this method when playback is paused.
 *   - Do not call the `seekToPosition:` method during switching.
 *   - Before switching the media resource, make sure that the playback position does not exceed the
 * total duration of the media resource to be switched.
 *
 * @param src The URL of the media resource.
 * @param syncPts Whether to synchronize the playback position (ms) before and after the switch:
 * - `YES`: Synchronize the playback position before and after the switch.
 * - `NO`: (Default) Do not synchronize the playback position before and after the switch.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)switchSrc:(NSString *)src syncPts:(BOOL)syncPts NS_SWIFT_NAME(switchSrc(_:syncPts:));

/**
 * @brief Preloads a media resource.
 *
 * @details
 * You can call this method to preload a media resource into the playlist. If you need to preload
 * multiple media resources, you can call this method multiple times.
 * After calling this method, if you receive the `AgoraMediaPlayerPreloadEventComplete` event in the
 * `AgoraRtcMediaPlayer:didPreloadEvent:` callback, the preload is successful; If you receive the
 * `AgoraMediaPlayerPreloadEventError` event in the `AgoraRtcMediaPlayer:didPreloadEvent:` callback,
 * the preload fails.
 * If the preload is successful and you want to play the media resource, call `playPreloadedSrc:`;
 * if you want to clear the playlist, call `stop`.
 *
 * @note
 * - Before calling this method, ensure that you have called `open:startPos:` or
 * `openWithMediaSource:` to open the media resource successfully.
 * - Agora does not support preloading duplicate media resources to the playlist. However, you can
 * preload the media resources that are being played to the playlist again.
 *
 * @param src The URL of the media resource.
 * @param startPos The starting position (ms) for playing after the media resource is preloaded to
 * the playlist. When preloading a live stream, set this parameter to 0.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)preloadSrc:(NSString *)src startPos:(int)startPos NS_SWIFT_NAME(preloadSrc(_:startPos:));

/**
 * @brief Unloads media resources that are preloaded.
 *
 * @note This method cannot release the media resource being played.
 *
 * @param src The URL of the media resource.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)unloadSrc:(NSString *)src NS_SWIFT_NAME(unloadSrc(_:));

/**
 * @brief Plays preloaded media resources.
 *
 * @details
 * After calling the `preloadSrc:startPos:` method to preload the media resource into the playlist,
 * you can call this method to play the preloaded media resource. After calling this method, if you
 * receive the `AgoraRtcMediaPlayer:didChangedToState:reason:` callback which reports the
 * `AgoraMediaPlayerStatePlaying` state, the playback is successful.
 * If you want to change the preloaded media resource to be played, you can call this method again
 * and specify the URL of the new media resource that you want to preload. If you want to replay the
 * media resource, you need to call `preloadSrc:startPos:` to preload the media resource to the
 * playlist again before playing. If you want to clear the playlist, call the `stop` method.
 *
 * @note If you call this method when playback is paused, this method does not take effect until
 * playback is resumed.
 *
 * @param src The URL of the media resource in the playlist must be consistent with the `src` set by
 * the `preloadSrc:startPos:` method; otherwise, the media resource cannot be played.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)playPreloadedSrc:(NSString *)src NS_SWIFT_NAME(playPreloadedSrc(_:));

/**
 * @brief Sets the channel mode of the current audio file.
 *
 * @details
 * In a stereo music file, the left and right channels can store different audio data. According to
 * your needs, you can set the channel mode to original mode, left channel mode, right channel mode,
 * or mixed channel mode. For example, in the KTV scenario, the left channel of the music file
 * stores the musical accompaniment, and the right channel stores the singing voice. If you only
 * need to listen to the accompaniment, call this method to set the channel mode of the music file
 * to left channel mode; if you need to listen to the accompaniment and the singing voice at the
 * same time, call this method to set the channel mode to mixed channel mode.
 *
 * @note
 * - Call this method after calling `open:startPos:`.
 * - This method only applies to stereo audio files.
 *
 * @param mode The channel mode. See `AgoraAudioDualMonoMode`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioDualMonoMode:(AgoraAudioDualMonoMode)mode NS_SWIFT_NAME(setAudioDualMonoMode(_:));

/**
 * @brief Enables or disables the spatial audio effect for the media player.
 *
 * @details
 * After successfully setting the spatial audio effect parameters of the media player, the SDK
 * enables the spatial audio effect for the media player, and the local user can hear the media
 * resources with a sense of space.
 * If you need to disable the spatial audio effect for the media player, set the `params` parameter
 * to null.
 *
 * @param params The spatial audio effect parameters of the media player. See
 * `AgoraSpatialAudioParams`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setSpatialAudioParams:(AgoraSpatialAudioParams* _Nonnull)params NS_SWIFT_NAME(setSpatialAudioParams(_:));

#pragma mark Callback Audio PCM Frame

/**
 * Registers & unregister the player audio observer
 *
 * @param delegate observer object, pass nil to unregister
 * @return
 * - YES: Success.
 * - NO: Failure.
 */
- (BOOL)setAudioFrameDelegate:(id<AgoraRtcMediaPlayerAudioFrameDelegate> _Nullable)delegate NS_SWIFT_NAME(setAudioFrameDelegate(_:));

#pragma mark Callback Video Frame

/**
 * @brief Registers the video observer object of the media player.
 *
 * @details
 * When calling this method to register a video observer, you can register callbacks in the
 * `AgoraRtcMediaPlayerVideoFrameDelegate` class as needed. After you successfully register the
 * video frame observer, the SDK triggers the registered callbacks each time a video frame is
 * received.
 *
 * @param delegate The delegate instance. See `AgoraRtcMediaPlayerVideoFrameDelegate`. Set the value
 * as nil to release the instance.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (BOOL)setVideoFrameDelegate:(id<AgoraRtcMediaPlayerVideoFrameDelegate> _Nullable)delegate NS_SWIFT_NAME(setVideoFrameDelegate(_:));

- (int)registerMediaPlayerAudioSpectrumDelegate:(id<AgoraAudioSpectrumDelegate> _Nullable)delegate
                                   intervalInMS:(NSUInteger)intervalInMS NS_SWIFT_NAME(registerMediaPlayerAudioSpectrumDelegate(_:intervalInMS:));

- (int)unregisterMediaPlayerAudioSpectrumDelegate:(id<AgoraAudioSpectrumDelegate> _Nullable)delegate NS_SWIFT_NAME(unregisterMediaPlayerAudioSpectrumDelegate(_:));
@end


NS_ASSUME_NONNULL_END
