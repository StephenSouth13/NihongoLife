//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraEnumerates.h"
#import "AgoraObjects.h"
#import <simd/simd.h>

@class AgoraRtcEngineKit;
@class AgoraRtcConnection;

/**
 * @brief The spatial position of the remote user or the media player.
 */
__attribute__((visibility("default"))) @interface AgoraRemoteVoicePositionInfo : NSObject
/**
 * The coordinates in the world coordinate system. This parameter is an array of length 3, and the
 * three values represent the front, right, and top coordinates in turn.
 */
@property(assign, nonatomic) simd_float3 position;
/**
 * The unit vector of the x axis in the coordinate system. This parameter is an array of length 3,
 * and the three values represent the front, right, and top coordinates in turn.
 */
@property(assign, nonatomic) simd_float3 forward;
@end


/**
 * @brief Sound insulation area settings.
 */
__attribute__((visibility("default"))) @interface AgoraSpatialAudioZone : NSObject
//the zone id
/**
 * The ID of the sound insulation area.
 */
@property(assign, nonatomic) NSInteger zoneSetId;
//zone center point
/**
 * The spatial center point of the sound insulation area. This parameter is an array of length 3,
 * and the three values represent the front, right, and top coordinates in turn.
 */
@property(nonatomic) simd_float3 position;
//forward direction 
/**
 * Starting at `position`, the forward unit vector. This parameter is an array of length 3, and the
 * three values represent the front, right, and top coordinates in turn.
 */
@property(nonatomic) simd_float3 forward;
//right direction
/**
 * Starting at `position`, the right unit vector. This parameter is an array of length 3, and the
 * three values represent the front, right, and top coordinates in turn.
 */
@property(nonatomic) simd_float3 right;
//up direction
/**
 * Starting at `position`, the up unit vector. This parameter is an array of length 3, and the three
 * values represent the front, right, and top coordinates in turn.
 */
@property(nonatomic) simd_float3 up;
//the forward side length of the zone
/**
 * The entire sound insulation area is regarded as a cube; this represents the length of the forward
 * side in the unit length of the game engine.
 */
@property(assign, nonatomic) float forwardLength;
//tehe right side length of the zone
/**
 * The entire sound insulation area is regarded as a cube; this represents the length of the right
 * side in the unit length of the game engine.
 */
@property(assign, nonatomic) float rightLength;
//the up side length of the zone
/**
 * The entire sound insulation area is regarded as a cube; this represents the length of the up side
 * in the unit length of the game engine.
 */
@property(assign, nonatomic) float upLength;
//the audio attenuation of zone
/**
 * The sound attenuation coefficient when users within the sound insulation area communicate with
 * external users. The value range is [0,1]. The values are as follows:
 * - 0: Broadcast mode, where the volume and timbre are not attenuated with distance, and the volume
 * and timbre heard by local users do not change regardless of distance.
 * - (0,0.5): Weak attenuation mode, that is, the volume and timbre are only weakly attenuated
 * during the propagation process, and the sound can travel farther than the real environment.
 * - 0.5: (Default) simulates the attenuation of the volume in the real environment; the effect is
 * equivalent to not setting the `audioAttenuation` parameter.
 * - (0.5,1]: Strong attenuation mode (default value is 1), that is, the volume and timbre attenuate
 * rapidly during propagation.
 */
@property(assign, nonatomic) float audioAttenuation;
@end

/**
 * @brief The configuration of `AgoraLocalSpatialAudioKit`.
 */
__attribute__((visibility("default"))) @interface AgoraLocalSpatialAudioConfig : NSObject
/**
 * `AgoraRtcEngineKit`.
 */
@property(assign, nonatomic) AgoraRtcEngineKit* _Nullable rtcEngine;
@end


NS_ASSUME_NONNULL_BEGIN

__attribute__((visibility("default"))) @interface AgoraSpatialAudioKitBase : NSObject

/**
 * @brief Sets the maximum number of streams that a user can receive in a specified audio reception
 * range.
 *
 * @details
 * If the number of receivable streams exceeds the set value, the local user receives the `maxCount`
 * streams that are closest to the local user.
 *
 * @param maxCount The maximum number of streams that a user can receive within a specified audio
 * reception range. The value of this parameter should be ≤ 16, and the default value is 10.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setMaxAudioRecvCount:(NSUInteger)maxCount NS_SWIFT_NAME(setMaxAudioRecvCount(_:));

/**
 * @brief Sets the audio reception range of the local user.
 *
 * @details
 * After the setting is successful, the local user can only hear the remote users within the setting
 * range or belonging to the same team. You can call this method at any time to update the audio
 * reception range.
 *
 * @param range The maximum audio reception range. The unit is meters. The value of this parameter
 * must be greater than 0, and the default value is 20.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setAudioRecvRange:(float)range NS_SWIFT_NAME(setAudioRecvRange(_:));

/**
 * @brief Sets the length (in meters) of the game engine distance per unit.
 *
 * @details
 * In a game engine, the unit of distance is customized, while in the Agora spatial audio algorithm,
 * distance is measured in meters. By default, the SDK converts the game engine distance per unit to
 * one meter. You can call this method to convert the game engine distance per unit to a specified
 * number of meters.
 *
 * @param unit The number of meters that the game engine distance per unit is equal to. The value of
 * this parameter must be greater than 0.00, and the default value is 1.00. For example, setting
 * unit as 2.00 means the game engine distance per unit equals 2 meters.The larger the value is, the
 * faster the sound heard by the local user attenuates when the remote user moves far away from the
 * local user.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setDistanceUnit:(float)unit NS_SWIFT_NAME(setDistanceUnit(_:));

/**
 * @brief Updates the spatial position of the media player.
 *
 * @details
 * After a successful update, the local user can hear the change in the spatial position of the
 * media player.
 * Call timing: This method can be called either before or after joining the channel.
 *
 * @param playerId The ID of the media player. You can get the Device ID by calling
 * `getMediaPlayerId`.
 * @param positionInfo The spatial position of the media player. See `AgoraRemoteVoicePositionInfo`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updatePlayerPositionInfo:(NSInteger)playerId positionInfo:(AgoraRemoteVoicePositionInfo* _Nonnull)positionInfo NS_SWIFT_NAME(updatePlayerPositionInfo(_:positionInfo:));

/**
 * @brief Updates the spatial position of the local user.
 *
 * @details
 * - Under the `AgoraLocalSpatialAudioKit` class, this method needs to be used with
 * `updateRemotePosition:positionInfo:`. The SDK calculates the relative position between the local
 * and remote users according to this method and the parameter settings in
 * `updateRemotePosition:positionInfo:`, and then calculates the user's spatial audio effect
 * parameters.
 *
 * @param position The coordinates in the world coordinate system. This parameter is an array of
 * length 3, and the three values represent the front, right, and top coordinates in turn.
 * @param axisForward The unit vector of the x axis in the coordinate system. This parameter is an
 * array of length 3, and the three values represent the front, right, and top coordinates in turn.
 * @param axisRight The unit vector of the y axis in the coordinate system. This parameter is an
 * array of length 3, and the three values represent the front, right, and top coordinates in turn.
 * @param axisUp The unit vector of the z axis in the coordinate system. This parameter is an array
 * of length 3, and the three values represent the front, right, and top coordinates in turn.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateSelfPosition:(simd_float3)position axisForward:(simd_float3)axisForward axisRight:(simd_float3)axisRight axisUp:(simd_float3)axisUp NS_SWIFT_NAME(updateSelfPosition(_:axisForward:axisRight:axisUp:));

/**
 * @brief Update coordinate conversion.
 *
 * @details
 * Applicable scenarios: When using native iOS frameworks like SceneKit or RealityKit, you can use
 * this method to directly pass the position vector into the mentioned frameworks for rendering.
 *
 * @param transform Convert parameters. It's a 4 × 4 matrix.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateSelfTransform:(simd_float4x4)transform NS_SWIFT_NAME(updateSelfTransform(_:));

/**
 * @brief Stops or resumes publishing the local audio stream.
 *
 * @note
 * - This method does not affect any ongoing audio recording, because it does not disable the audio
 * capture device.
 * - Call this method after the `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` method.
 * - When using the spatial audio effect, if you need to set whether to stop subscribing to the
 * audio stream of a specified user, Agora recommends calling this method instead of the
 * `muteLocalAudioStream:` method in `AgoraRtcEngineKit`.
 * - A successful call of this method triggers the `rtcEngine:didAudioMuted:byUid:` and
 * `rtcEngine:remoteAudioStateChangedOfUid:state:reason:elapsed:` callbacks on the remote client.
 *
 * @param mute Whether to stop publishing the local audio stream:
 * - `YES`: Stop publishing the local audio stream.
 * - `NO`: Publish the local audio stream.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteLocalAudioStream:(BOOL)mute NS_SWIFT_NAME(muteLocalAudioStream(_:));

/**
 * @brief Stops or resumes subscribing to the audio streams of all remote users.
 *
 * @details
 * After successfully calling this method, the local user stops or resumes subscribing to the audio
 * streams of all remote users, including all subsequent users.
 *
 * @note
 * - Call this method after the `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` method.
 * - When using the spatial audio effect, if you need to set whether to stop subscribing to the
 * audio streams of all remote users, Agora recommends calling this method instead of the
 * `muteAllRemoteAudioStreams:` method in `AgoraRtcEngineKit`.
 * - After calling this method, you need to call `updateSelfPosition:axisForward:axisRight:axisUp:`
 * and `updateRemotePosition:positionInfo:` to update the spatial location of the local user and the
 * remote user; otherwise, the settings in this method do not take effect.
 *
 * @param mute Whether to stop subscribing to the audio streams of all remote users:
 * - `YES`: Stop subscribing to the audio streams of all remote users.
 * - `NO`: Subscribe to the audio streams of all remote users.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteAllRemoteAudioStreams:(BOOL)mute NS_SWIFT_NAME(muteAllRemoteAudioStreams(_:));

/**
 * @brief Sets the sound insulation area.
 *
 * @details
 * In virtual interactive scenarios, you can use this method to set the sound insulation area and
 * sound attenuation coefficient. When the sound source (which can be the user or the media player)
 * and the listener belong to the inside and outside of the sound insulation area, they can
 * experience the attenuation effect of sound similar to the real environment when it encounters a
 * building partition.
 * - When the sound source and the listener belong to the inside and outside of the sound insulation
 * area, the sound attenuation effect is determined by the sound attenuation coefficient in
 * `AgoraSpatialAudioZone`.
 * - If the user or media player is in the same sound insulation area, it is not affected by
 * `AgoraSpatialAudioZone`, and the sound attenuation effect is determined by the `attenuation`
 * parameter in `setPlayerAttenuation:playerId:forceSet:` or
 * `setRemoteAudioAttenuation:userId:forceSet:`. If you do not call
 * `setPlayerAttenuation:playerId:forceSet:` or `setRemoteAudioAttenuation:userId:forceSet:`, the
 * default sound attenuation coefficient of the SDK is 0.5, which simulates the attenuation of the
 * sound in the real environment.
 * - If the sound source and the receiver belong to two sound insulation areas, the receiver cannot
 * hear the sound source.
 *
 * @note If this method is called multiple times, the last sound insulation area set takes effect.
 *
 * @param zones Sound insulation area settings. See `AgoraSpatialAudioZone`. When you set this
 * parameter to `nil`, it means clearing all sound insulation zones.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setZones:(NSArray<AgoraSpatialAudioZone*> * _Nullable)zones NS_SWIFT_NAME(setZones(_:));

/**
 * @brief Sets the sound attenuation properties of the media player.
 *
 * @param playerId The ID of the media player. You can get the Device ID by calling
 * `getMediaPlayerId`.
 * @param attenuation The sound attenuation coefficient of the remote user or media player. The
 * value range is [0,1]. The values are as follows:
 * - 0: Broadcast mode, where the volume and timbre are not attenuated with distance, and the volume
 * and timbre heard by local users do not change regardless of distance.
 * - (0,0.5): Weak attenuation mode, that is, the volume and timbre are only weakly attenuated
 * during the propagation process, and the sound can travel farther than the real environment.
 * - 0.5: (Default) simulates the attenuation of the volume in the real environment; the effect is
 * equivalent to not setting the `speaker_attenuation` parameter.
 * - (0.5,1]: Strong attenuation mode, that is, the volume and timbre attenuate rapidly during the
 * propagation process.
 * @param forceSet Whether to force the sound attenuation effect of the media player:
 * - `YES`: Force `attenuation` to set the attenuation of the media player. At this time, the
 * attenuation coefficient of the sound insulation are set in the `audioAttenuation` in the
 * `AgoraSpatialAudioZone` does not take effect for the media player.
 * - `NO`: Do not force `attenuation` to set the sound attenuation effect of the media player, as
 * shown in the following two cases.
 *   - If the sound source and listener are inside and outside the sound isolation area, the sound
 * attenuation effect is determined by the `audioAttenuation` in `AgoraSpatialAudioZone`.
 *   - If the sound source and the listener are in the same sound insulation area or outside the
 * same sound insulation area, the sound attenuation effect is determined by `attenuation` in this
 * method.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setPlayerAttenuation:(double)attenuation playerId:(NSUInteger)playerId forceSet:(BOOL)forceSet NS_SWIFT_NAME(setPlayerAttenuation(_:playerId:forceSet:));

/**
 * @brief Stops or resumes subscribing to the audio stream of a specified user.
 *
 * @note
 * - Call this method after the `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` method.
 * - When using the spatial audio effect, if you need to set whether to stop subscribing to the
 * audio stream of a specified user, Agora recommends calling this method instead of the
 * `muteRemoteAudioStream:mute:` method in `AgoraRtcEngineKit`.
 *
 * @param uid The user ID. This parameter must be the same as the user ID passed in when the user
 * joined the channel.
 * @param mute Whether to subscribe to the specified remote user's audio stream.
 * - `YES`: Stop subscribing to the audio stream of the specified user.
 * - `NO`: (Default) Subscribe to the audio stream of the specified user. The SDK decides whether to
 * subscribe according to the distance between the local user and the remote user.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)muteRemoteAudioStream:(NSUInteger)uid mute:(BOOL)mute NS_SWIFT_NAME(muteRemoteAudioStream(_:mute:));

- (int)clearRemotePositions NS_SWIFT_NAME(clearRemotePositions());

@end

__attribute__((visibility("default"))) @interface AgoraLocalSpatialAudioKit : AgoraSpatialAudioKitBase

/**
 * @brief Initializes `AgoraLocalSpatialAudioKit`.
 *
 * @note
 * - Before calling other methods of the `AgoraLocalSpatialAudioKit` class, you need to call this
 * method to initialize `AgoraLocalSpatialAudioKit`.
 * - The SDK supports creating only one `AgoraLocalSpatialAudioKit` instance for an app.
 *
 * @param config The configuration of `AgoraLocalSpatialAudioKit`. See
 * `AgoraLocalSpatialAudioConfig`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
+ (instancetype _Nonnull)sharedLocalSpatialAudioWithConfig:(AgoraLocalSpatialAudioConfig*)config NS_SWIFT_NAME(sharedLocalSpatialAudio(with:));

/**
 * @brief Destroys `AgoraLocalSpatialAudioKit`.
 *
 * @details
 * This method releases all resources under `AgoraLocalSpatialAudioKit`. When the user does not need
 * to use the spatial audio effect, you can call this method to release resources for other
 * operations.
 * After calling this method, you can no longer use any of the APIs under
 * `AgoraLocalSpatialAudioKit`. To use the spatial audio effect again, you need to wait until the
 * `destroy` method execution to complete before calling `sharedLocalSpatialAudioWithConfig:` to
 * create a new `AgoraLocalSpatialAudioKit`.
 *
 * @note Call this method before the `destroy` method under `AgoraRtcEngineKit`.
 *
 */
+ (void)destroy NS_SWIFT_NAME(destroy());

/**
 * @brief Updates the spatial position of the specified remote user.
 *
 * @details
 * After successfully calling this method, the SDK calculates the spatial audio parameters based on
 * the relative position of the local and remote user.
 *
 * @note Call this method after the `joinChannelByToken:channelId:info:uid:joinSuccess:` or
 * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` method.
 *
 * @param uid The user ID. This parameter must be the same as the user ID passed in when the user
 * joined the channel.
 * @param posInfo The spatial position of the remote user. See `AgoraRemoteVoicePositionInfo`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)updateRemotePosition:(NSUInteger)uid positionInfo:(AgoraRemoteVoicePositionInfo*)posInfo NS_SWIFT_NAME(updateRemotePosition(_:positionInfo:));

- (int)updateRemotePositionEx:(NSUInteger)uid positionInfo:(AgoraRemoteVoicePositionInfo*)posInfo connection:(AgoraRtcConnection *)connection NS_SWIFT_NAME(updateRemotePositionEx(_:positionInfo:connection:));

/**
 * @brief Removes the spatial position of the specified remote user.
 *
 * @details
 * After successfully calling this method, the local user no longer hears the specified remote user.
 * After leaving the channel, to avoid wasting computing resources, call this method to delete the
 * spatial position information of the specified remote user. Otherwise, the user's spatial position
 * information will be saved continuously. When the number of remote users exceeds the number of
 * audio streams that can be received as set in `setMaxAudioRecvCount:`, the system automatically
 * unsubscribes from the audio stream of the user who is furthest away based on relative distance.
 *
 * @param uid The user ID. This parameter must be the same as the user ID passed in when the user
 * joined the channel.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)removeRemotePosition:(NSUInteger)uid NS_SWIFT_NAME(removeRemotePosition(_:));

- (int)removeRemotePositionEx:(NSUInteger)uid connection:(AgoraRtcConnection *)connection NS_SWIFT_NAME(removeRemotePositionEx(_:connection:));

/**
 * @brief Removes the spatial positions of all remote users.
 *
 * @details
 * After successfully calling this method, the local user no longer hears any remote users.
 * After leaving the channel, to avoid wasting resources, you can also call this method to delete
 * the spatial positions of all remote users.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)clearRemotePositionsEx:(AgoraRtcConnection *)connection NS_SWIFT_NAME(clearRemotePositionsEx(_:));

/**
 * @brief Sets the sound attenuation effect for the specified user.
 *
 * @param uid The user ID. This parameter must be the same as the user ID passed in when the user
 * joined the channel.
 * @param attenuation For the user's sound attenuation coefficient, the value range is [0,1]. The
 * values are as follows:
 * - 0: Broadcast mode, where the volume and timbre are not attenuated with distance, and the volume
 * and timbre heard by local users do not change regardless of distance.
 * - (0,0.5): Weak attenuation mode, that is, the volume and timbre are only weakly attenuated
 * during the propagation process, and the sound can travel farther than the real environment.
 * - 0.5: (Default) simulates the attenuation of the volume in the real environment; the effect is
 * equivalent to not setting the `speaker_attenuation` parameter.
 * - (0.5,1]: Strong attenuation mode, that is, the volume and timbre attenuate rapidly during the
 * propagation process.
 * @param forceSet Whether to force the user's sound attenuation effect:
 * - `YES`: Force `attenuation` to set the sound attenuation of the user. At this time, the
 * `attenuation` coefficient of the sound insulation area set in the `audioAttenuation` of the
 * `AgoraSpatialAudioZone` does not take effect for the user.
 * - `NO`: Do not force `attenuation` to set the user's sound attenuation effect, as shown in the
 * following two cases.
 *   - If the sound source and listener are inside and outside the sound isolation area, the sound
 * attenuation effect is determined by the `audioAttenuation` in `AgoraSpatialAudioZone`.
 *   - If the sound source and the listener are in the same sound insulation area or outside the
 * same sound insulation area, the sound attenuation effect is determined by `attenuation` in this
 * method.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setRemoteAudioAttenuation:(double)attenuation uid:(NSUInteger)uid forceSet:(BOOL)forceSet NS_SWIFT_NAME(setRemoteAudioAttenuation(_:uid:forceSet:));

- (int)updateSelfPositionEx:(simd_float3)position
                axisForward:(simd_float3)axisForward
                axisRight:(simd_float3)axisRight
                axisUp:(simd_float3)axisUp
                connection:(AgoraRtcConnection *)connection NS_SWIFT_NAME(updateSelfPositionEx(_:axisForward:axisRight:axisUp:connection:));

@end

NS_ASSUME_NONNULL_END
