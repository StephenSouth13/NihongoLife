//
//  AgoraMusicContentCenter.h
//  AgoraMusicContentCenter
//
//  Created by dingyusong on 2022/6/1.
//  Copyright © 2022 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>

@protocol AgoraRtcMediaPlayerProtocol;
@protocol AgoraRtcMediaPlayerDelegate;

/**
 * @brief Playback modes for music resources.
 */
typedef NS_ENUM(NSUInteger, AgoraMusicPlayMode) {
    /**
     * 0: Original singer.
     */
    AgoraMusicPlayModeOriginal = 0,

    /**
     * 1: Accompaniment.
     */
    AgoraMusicPlayModeAccompany = 1,
    /**
     * 2: Vocal guide.
     */
    AgoraMusicPlayModeLeadsing = 2,
};

/**
 * @brief The loading state of music resources.
 */
typedef NS_ENUM(NSUInteger, AgoraMusicContentCenterPreloadState) {
    /**
     * 0: Music resource loading is complete.
     */
    AgoraMusicContentCenterPreloadStateOK = 0,

    /**
     * 1: Failed to load music resource.
     */
    AgoraMusicContentCenterPreloadStateError = 1,

    /**
     * 2: Music resources are loading.
     */
    AgoraMusicContentCenterPreloadStatePreloading = 2,
    
    /**
     * 3: The cached music resource has been removed.
     */
    AgoraMusicContentCenterPreloadStateRemoveCache = 3,
};

/**
 * @brief Request status codes for the music content center.
 */
typedef NS_ENUM(NSUInteger, AgoraMusicContentCenterStateReason) {
    /**
     * 0: Request succeeds.
     */
    AgoraMusicContentCenterStateReasonOK = 0,
    /**
     * 1: General error with no specific attribution.
     */
    AgoraMusicContentCenterStateReasonError = 1,
    /**
     * 2: Gateway error. Possible causes include:
     * - The current Token has expired. Please regenerate the Token.
     * - The provided Token is invalid. Make sure you are using a Temporary Token.
     * - Network error. Please check your network connection.
     */
    AgoraMusicContentCenterStateReasonErrorGateway = 2,
    /**
     * 3: Permission error or the music resource does not exist. Make sure your project has enabled the
     * Agora Copyright music Content Center permission. Please `contact technical support`.
     */
    AgoraMusicContentCenterStateReasonErrorPermissionAndResource = 3,
    /**
     * 4: Internal data parsing error. Please `contact technical support`.
     */
    AgoraMusicContentCenterStateReasonErrorInternalDataParse = 4,
    /**
     * 5: Error occurs while loading music resources. Please `contact technical support`.
     */
    AgoraMusicContentCenterStateReasonErrorMusicLoading = 5,
    /**
     * 6: Error occurs while decrypting music resources. Please `contact technical support`.
     */
    AgoraMusicContentCenterStateReasonErrorMusicDecryption = 6,
    /**
     * 7: An internal HTTP error occurs. Please try again later.
     */
    AgoraMusicContentCenterStateReasonErrorHttpInternalError = 7,
};

/**
 * @brief The cache state of music resources.
 */
typedef NS_ENUM(NSUInteger, AgoraMusicCacheStatusType) {
    /**
     * 0: Music resource is cached.
     */
    AgoraMusicCacheStatusTypeCached = 0,

    /**
     * 1: Music resources are being cached.
     */
    AgoraMusicCacheStatusTypeCaching = 1,
};

NS_ASSUME_NONNULL_BEGIN

/**
 * @brief Information about cached Copyright music resources.
 */
__attribute__((visibility("default"))) @interface AgoraMusicCacheInfo : NSObject
/**
 * The ID of the music resource, used to identify the music resource.
 */
@property(nonatomic, assign) NSInteger songCode;

/**
 * The cache status of music resources. See `AgoraMusicCacheStatusType` for details.
 */
@property(nonatomic, assign) AgoraMusicCacheStatusType statusType;
@end

/**
 * @brief Detailed information of the music chart.
 */
__attribute__((visibility("default"))) @interface AgoraMusicChartInfo : NSObject
/**
 * Name of the chart.
 */
@property (nonatomic, copy) NSString *chartName;
/**
 * The ID of the music chart.
 */
@property (nonatomic, assign) NSInteger identify;
@end

__attribute__((visibility("default"))) @interface AgoraMvProperty : NSObject
/**
 * The resolution of the mv
 */
@property (nonatomic, copy) NSString *resolution;
/**
 * The bandwidth of the mv
 */
@property (nonatomic, copy) NSString *bandwidth;
@end

/**
 * @brief The climax parts of the music.
 */
__attribute__((visibility("default"))) @interface AgoraClimaxSegment : NSObject
/**
 * The time (ms) when the climax part begins.
 */
@property(nonatomic, assign) NSInteger startTimeMs;
/**
 * The time (ms) when the climax part ends.
 */
@property(nonatomic, assign) NSInteger endTimeMs;
@end

/**
 * @brief Detailed information about the music resource.
 */
__attribute__((visibility("default"))) @interface AgoraMusic : NSObject
/**
 * The ID of the music resource, used to identify a music resource.
 */
@property(nonatomic, assign) NSInteger songCode;
/**
 * Music resource types:
 * - 1: An individual mode audio track with accompaniment in the left channel and original vocals in
 * the right channel.
 * - 2: An individual mode audio track with accompaniment only.
 * - 3: An individual mode audio track with original vocals only.
 * - 4: A multi-track audio source.
 */
@property(nonatomic, assign) NSInteger type;
/**
 * Whether the song supports the singing scoring feature:
 * - 1: The song supports the singing scoring feature.
 * - 2: The song does not support the singing scoring feature.
 */
@property(nonatomic, assign) NSInteger pitchType;
/**
 * Name of the music resource.
 */
@property(nonatomic, copy) NSString* name;
/**
 * Singer name.
 */
@property(nonatomic, copy) NSString* singer;
/**
 * The download URL of the music resource poster.
 */
@property(nonatomic, copy) NSString* poster;

/**
 * The release time of the music resource.
 */
@property(nonatomic, copy) NSString* releaseTime;
/**
 * Total duration of the music resource (in seconds).
 */
@property(nonatomic, assign) NSInteger durationS;
/**
 * Supported lyric types:
 * - 0: xml format.
 * - 1: lrc format.
 */
@property(nonatomic, strong) NSArray<NSNumber *>* lyricList;
/**
 * The mv property list of music
 */
@property(nonatomic, strong) NSArray<AgoraMvProperty *>* mvPropertyList;
/**
 * List of music climax segments. See `AgoraClimaxSegment` for details.
 */
@property(nonatomic, strong) NSArray<AgoraClimaxSegment *>* climaxSegmentList;
@end

/**
 * @brief Detailed information of the music resource list.
 */
__attribute__((visibility("default"))) @interface AgoraMusicCollection : NSObject
/**
 * The number of music items in the music resource list of this request.
 */
@property(nonatomic, assign) NSInteger count;
/**
 * The total number of music resources in the list.
 */
@property(nonatomic, assign) NSInteger total;
/**
 * Current page number, starts from 1 by default.
 */
@property(nonatomic, assign) NSInteger page;
/**
 * The total number of pages in the current music resource list. The maximum value is 50.
 */
@property(nonatomic, assign) NSInteger pageSize;
/**
 * Detailed information about the music resources in the current page list. See `AgoraMusic` for
 * more details.
 */
@property(nonatomic, strong) NSArray<AgoraMusic *>* musicList;
@end

/**
 * The request event delegate callback
 */
@protocol AgoraMusicContentCenterEventDelegate <NSObject>
/**
 * @brief Callback for retrieving music charts.
 *
 * @details
 * After you call the `getMusicCharts` method to retrieve all available music charts, the SDK
 * triggers this callback.
 *
 * @param requestId The request ID. A unique identifier for this request.
 * @param reason The request status code from the music content center. See
 * `AgoraMusicContentCenterStateReason`.
 * @param result The list of currently playable music charts. See `AgoraMusicChartInfo`.
 *
 */
- (void)onMusicChartsResult:(NSString *)requestId result:(NSArray<AgoraMusicChartInfo*> *)result reason:(AgoraMusicContentCenterStateReason)reason;

/**
 * @brief Callback for retrieving the music content list.
 *
 * @details
 * When you call the `getMusicCollectionWithMusicChartId:page:pageSize:jsonOption:` method to get
 * the music content list of a specified chart, or call
 * `searchMusicWithKeyWord:page:pageSize:jsonOption:` to search for music content, the SDK triggers
 * this callback to report the detailed information of the music content list in the chart.
 *
 * @param requestId The request ID. A unique identifier for this request.
 * @param reason The request status code from the music content center. See
 * `AgoraMusicContentCenterStateReason`.
 * @param result The detailed information of the music content list. See `AgoraMusicCollection`.
 *
 */
- (void)onMusicCollectionResult:(NSString *)requestId result:(AgoraMusicCollection *)result reason:(AgoraMusicContentCenterStateReason)reason;

/**
 * @brief Callback for the lyric download URL.
 *
 * @details
 * After you call `getLyricWithSongCode:lyricType:` to get the lyric download URL of a specified
 * song, the SDK triggers this callback.
 *
 * @param requestId The request ID. A unique identifier for this request.
 * @param songCode The identifier of the music resource, used to specify the music resource.
 * @param lyricUrl The download URL of the lyrics.
 * @param reason The request status code from the music content center. See
 * `AgoraMusicContentCenterStateReason` for details.
 *
 */
- (void)onLyricResult:(NSString*)requestId songCode:(NSInteger)songCode lyricUrl:(NSString* _Nullable)lyricUrl reason:(AgoraMusicContentCenterStateReason)reason;

/**
 * @brief Callback for detailed information of a music resource.
 *
 * @details
 * This callback is triggered by the SDK after you call `getSongSimpleInfoWithSongCode:` to retrieve
 * the detailed information of a specific music resource.
 *
 * @param requestId The request ID. A unique identifier for this request.
 * @param songCode The identifier of the music resource.
 * @param simpleInfo The related information of the music resource, including:
 * - Start and end time (ms) of the chorus segment
 * - Lyric download URL of the chorus segment
 * - Duration (ms) of the chorus segment
 * - Song name
 * - Singer name
 * @param reason The request status code from the music content center. See
 * `AgoraMusicContentCenterStateReason` for details.
 *
 */
- (void)onSongSimpleInfoResult:(NSString*)requestId songCode:(NSInteger)songCode simpleInfo:(NSString* _Nullable)simpleInfo reason:(AgoraMusicContentCenterStateReason)reason;

/**
 * @brief Reports the event of preloading a music resource.
 *
 * @details
 * After you call the `preloadWithSongCode:jsonOption:` or `preloadWithSongCode:` method to preload
 * a music resource, the SDK triggers this callback.
 *
 * @param requestId The request ID. A unique identifier for this request.
 * @param songCode The code of the music resource, used to identify a specific music resource.
 * @param percent The current loading progress of the music resource, ranging from [0,100].
 * @param lyricUrl The URL to download the lyrics.
 * @param state The current loading status of the music resource. See
 * `AgoraMusicContentCenterPreloadState`.
 * @param reason The request status code from the music content center. See
 * `AgoraMusicContentCenterStateReason`.
 *
 */
- (void)onPreLoadEvent:(NSString*)requestId songCode:(NSInteger)songCode percent:(NSInteger)percent lyricUrl:(NSString * _Nullable)lyricUrl state:(AgoraMusicContentCenterPreloadState)state reason:(AgoraMusicContentCenterStateReason)reason;
@end


@class AgoraRtcEngineKit;
/**
 * @brief Settings for the music content center.
 */
__attribute__((visibility("default"))) @interface AgoraMusicContentCenterConfig : NSObject
/**
 * `AgoraRtcEngineKit` instance.
 */
@property(assign, nonatomic) AgoraRtcEngineKit* _Nullable rtcEngine;
/**
 * The App ID of the project with content center enabled.
 */
@property (nonatomic, copy) NSString *appId;
/**
 * The RTM Token used for authentication when using the music content center.
 * @note
 * - Agora recommends that you use AccessToken2 for authentication. See `Deploy Token Server` for
 * details. When generating the Token, pass the `String` type `mccUid` to the `uid` parameter.
 * - When your Token is about to expire, you can call `renewToken:` to pass in a new Token.
 */
@property (nonatomic, copy) NSString *token;
/**
 * Uses the user ID for the music content center. This ID can be the same as the `uid` you use to
 * join an RTC channel, but it cannot be 0.
 */
@property (nonatomic, assign) NSInteger mccUid;
/**
 * The number of cacheable music resources must not exceed 50.
 */
@property (nonatomic, assign) NSUInteger maxCacheSize;
/**
 * @technical preview
 */
@property(nonatomic, copy) NSString* mccDomain;
/**
 * Event handler to get callback result.
 */
@property(nonatomic, weak) id<AgoraMusicContentCenterEventDelegate> eventDelegate;
@end

@protocol AgoraMusicPlayerProtocol <AgoraRtcMediaPlayerProtocol>
/**
 * @brief Opens a music resource by its resource ID.
 *
 * @details
 * Before calling this method, make sure the music resource you want to play has been fully loaded.
 * You can check whether the music resource has been preloaded by calling the
 * `isPreloadedWithSongCode:` method, or by listening to the
 * `onPreLoadEvent:songCode:percent:lyricUrl:state:reason:` callback.
 * 
 * After calling this method, the `AgoraRtcMediaPlayer:didChangedToState:reason:` callback is
 * triggered. Once you receive the playback state `AgoraMediaPlayerStateOpenCompleted`, you can call
 * the `play` method to start playback.
 *
 * @note If the music resource you want to open is protected by digital rights management, you
 * must use this method to open it. For music resources that are not protected by digital rights,
 * you can choose to open them using either this method or the `open:startPos:` method under the
 * `AgoraRtcMediaPlayerProtocol` class.
 *
 * @param songCode The ID of the music resource, used to identify the music content.
 * @param startPos The starting playback position in milliseconds. The default value is 0.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and recommended solutions.
 */
- (NSInteger)openMediaWithSongCode:(NSInteger)songCode startPos:(NSInteger)startPos NS_SWIFT_NAME(openMedia(songCode:startPos:));

/**
 * @brief Sets the playback mode for a music resource.
 *
 * @details
 * You can call this method to enable the original vocals, accompaniment, or vocal guide. If you do
 * not call this method, accompaniment is played by default. If the music resource does not contain
 * accompaniment, the original vocals are played.
 * Applicable scenarios: In entertainment scenarios such as online karaoke or live shows, if you
 * need to play copyrighted music provided by the Agora Content Center, you can call this method to
 * set the playback mode.
 * Call timing: You must call this method after `createMusicPlayerWithDelegate:`.
 *
 * @note You can obtain detailed information about the music resource through the
 * `onMusicCollectionResult:result:reason:` callback. From the `result` parameter, you can learn
 * which playback modes are supported for the copyrighted music.
 *
 * @param mode The playback mode. See `AgoraMusicPlayMode` for details.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting.
 *   - -2: Invalid parameter. Please reset the parameter.
 */
- (NSInteger)setPlayMode:(AgoraMusicPlayMode)mode NS_SWIFT_NAME(setPlayMode(mode:));

@end


__attribute__((visibility("default"))) @interface AgoraMusicContentCenter : NSObject

/**
 * @brief Initializes `AgoraMusicContentCenter`.
 *
 * @details
 * You must call this method to initialize `AgoraMusicContentCenter` before calling any other
 * methods under the `AgoraMusicContentCenter` class.
 *
 * @param config The configuration for `AgoraMusicContentCenter`. See
 * `AgoraMusicContentCenterConfig` for details.
 *
 * @return
 * - If the method call succeeds, returns an `AgoraMusicContentCenter` instance.
 * - If the method call fails, returns nil.
 */
+ (instancetype _Nullable)sharedContentCenterWithConfig:(AgoraMusicContentCenterConfig *)config NS_SWIFT_NAME(sharedContentCenter(config:));

/**
 * @brief Renews the Token.
 *
 * @details
 * When the Token used for authentication is about to expire or has already expired, you can call
 * this method to pass in a newly generated Token.
 *
 * @param token The new Token.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting suggestions.
 */
- (NSInteger)renewToken:(NSString * _Nonnull)token;

/**
 * @brief Registers the callback events for the music content center.
 *
 * @param eventDelegate The callback event to be registered. See
 * `AgoraMusicContentCenterEventDelegate` for details. Pass in nil if you want to remove a
 * previously registered callback event.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting advice.
 */
- (NSInteger)registerEventDelegate:(id<AgoraMusicContentCenterEventDelegate> _Nullable)eventDelegate;

/**
 * @brief Creates a music player.
 *
 * @details
 * If you need to play music resources from the Copyright music center, you must call this method
 * first to create a music player.
 *
 * @param delegate See `AgoraRtcMediaPlayerDelegate`.
 *
 * @return
 * - If the method call succeeds: returns an `AgoraMusicPlayerProtocol` instance.
 * - If the method call fails: returns a null pointer.
 */
- (id<AgoraMusicPlayerProtocol> _Nullable)createMusicPlayerWithDelegate:(id<AgoraRtcMediaPlayerDelegate> _Nullable)delegate NS_SWIFT_NAME(createMusicPlayer(delegate:));

/**
 * @brief Destroys the music player object.
 *
 * @details
 * When you no longer need to use the music player, you can call this method to destroy the music
 * player object. 
 * If you need to use the music player again after destruction, you must call
 * `createMusicPlayerWithDelegate:` to create a new music player object.
 * Call timing: You can call this method either before or after joining a channel, but make sure to
 * call it before the `destroy` method of `AgoraRtcEngineKit`.
 *
 * @param musicPlayer The `AgoraMusicPlayerProtocol` object.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting suggestions.
 */
- (NSInteger)destroyMusicPlayer:(id<AgoraMusicPlayerProtocol>)musicPlayer;

/**
 * @brief Retrieves all music charts.
 *
 * @details
 * When you call this method, the SDK triggers the `onMusicChartsResult:result:reason:` callback to
 * report detailed information about the music charts.
 *
 * @return
 * A `requestId` that uniquely identifies this request.
 */
- (NSString *)getMusicCharts;

/**
 * @brief Retrieves the list of music resources from a specified music chart by its ID.
 *
 * @details
 * After this method is successfully called, the SDK triggers the
 * `onMusicCollectionResult:result:reason:` callback to report detailed information about the music
 * resources in the chart.
 *
 * @param musicChartId The ID of the music chart, which can be obtained through the
 * `onMusicChartsResult:result:reason:` callback. You can also use the RESTful API to
 * retrieve the complete music library or retrieve the incremental music list.
 * @param page The current page number, starting from 1 by default.
 * @param pageSize The total number of pages of the current music resource list. The maximum value
 * is 50.
 * @param jsonOption An optional JSON extension field, default is nil. You can use this field to
 * filter the music resources you need. Currently supports filtering music resources that support
 * scoring and music resources with chorus segments:
 * | Key            | Value                                                                 | Example                 |
 * | ------------- | --------------------------------------------------------------------- | ---------------------- |
 * | pitchType     | Whether scoring is supported: - 1: music resources that support scoring. - 2: music resources that do not support scoring. | {"pitchType":1}        |
 * | needHighPart  | Whether chorus segment resources are needed: - `YES`: chorus segment resources are needed. - `NO`: chorus segment resources are not needed. | {"needHighPart":true} |
 *
 * @return
 * A `requestId` that uniquely identifies this request.
 */
- (NSString *)getMusicCollectionWithMusicChartId:(NSInteger)musicChartId page:(NSInteger)page pageSize:(NSInteger)pageSize jsonOption:(NSString * _Nullable)jsonOption NS_SWIFT_NAME(getMusicCollection(musicChartId:page:pageSize:jsonOption:));

/**
 * @brief Search for music resources.
 *
 * @details
 * After this method is successfully called, the SDK triggers the
 * `onMusicCollectionResult:result:reason:` callback to report the retrieved list of music
 * resources.
 *
 * @param keyword The search keyword. Supports searching by song title or artist name.
 * @param page The target page number of the music resource list you want to retrieve.
 * @param pageSize The maximum number of music resources displayed per page. The maximum value is
 * 50.
 * @param jsonOption An optional extended JSON field, default is `nil`. You can use this field to
 * filter the music resources you need. Currently, it supports filtering music resources that are
 * scorable and those with chorus segments:
 * | Key         | Value                                                                 | Example                  |
 * | ----------- | --------------------------------------------------------------------- | ------------------------ |
 * | pitchType   | Whether the music resource supports scoring:                         - 1: scorable music resources. - 2: non-scorable music resources.        | {"pitchType":1}         |
 * | needHighPart| Whether chorus segment resources are needed:                         - `YES`: chorus segment resources needed. - `NO`: chorus segment resources not needed. | {"needHighPart":true}   |
 *
 * @return
 * A `requestId`, which is the unique identifier for this request.
 */
- (NSString *)searchMusicWithKeyWord:(NSString *)keyWord page:(NSInteger)page pageSize:(NSInteger)pageSize jsonOption:(NSString * _Nullable)jsonOption NS_SWIFT_NAME(searchMusic(keyWord:page:pageSize:jsonOption:));

/**
 * @brief Preloads music resources.
 *
 * @deprecated This method is deprecated. Use preload(songCode:) instead.
 *
 * @details
 * You can call this method to preload the music resources you want to play. After this method is
 * called successfully, the SDK triggers the
 * `onPreLoadEvent:songCode:percent:lyricUrl:state:reason:` callback to report the event of
 * preloading music resources.
 * Before calling this method to preload music resources, you need to call the
 * `getMusicCollectionWithMusicChartId:page:pageSize:jsonOption:` or
 * `searchMusicWithKeyWord:page:pageSize:jsonOption:` method to obtain the music resources you want
 * to play. Then, retrieve the music resource ID (`songCode`) from the
 * `onMusicCollectionResult:result:reason:` callback triggered by those methods.
 *
 * @note To destroy the `AgoraRtcEngineKit` object, make sure to wait for the
 * `onPreLoadEvent:songCode:percent:lyricUrl:state:reason:` callback before calling the `destroy`
 * method.
 *
 * @param songCode The ID of the music resource, used to identify the music.
 * @param jsonOption Extended JSON field.
 * Agora charges based on the application scenario you pass in the `sceneType` field. Different
 * application scenarios correspond to different rates. Refer to the Billing Instructions for
 * details.
 * - 1: Channel profile: Karaoke and background music playback in live streaming.
 * - 2: Channel profile: Background music playback in live streaming.
 * - 3: (Default) Channel profile: Karaoke in voice chat.
 * - 4: Channel profile: Background music playback in voice chat.
 * - 5: Channel profile: Karaoke and background music playback in VR.
 *                 If you need to switch to a different scenario, you must call this method again
 * and pass the new `sceneType` value in this field.
 * Example: `{"sceneType":1}`
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. Refer to Error Codes for details and troubleshooting suggestions.
 */
- (NSInteger)preloadWithSongCode:(NSInteger)songCode jsonOption:(NSString* _Nullable)jsonOption NS_SWIFT_NAME(preload(songCode:jsonOption:))  __attribute__((deprecated("Use preload(songCode:) instead.")));

/**
 * @brief Preloads a music resource.
 *
 * @details
 * You can call this method to preload a music resource that you want to play. After the method is
 * called successfully, the SDK triggers the
 * `onPreLoadEvent:songCode:percent:lyricUrl:state:reason:` callback to report the preload event of
 * the music resource.
 * Before calling this method to preload a music resource, you need to call either
 * `getMusicCollectionWithMusicChartId:page:pageSize:jsonOption:` or
 * `searchMusicWithKeyWord:page:pageSize:jsonOption:` to retrieve the music resource you want to
 * play. Then, obtain the song code (`songCode`) from the `onMusicCollectionResult:result:reason:`
 * callback triggered by these methods.
 *
 * @note To destroy the `AgoraRtcEngineKit` object, you must wait until you receive the
 * `onPreLoadEvent:songCode:percent:lyricUrl:state:reason:` callback before calling the `destroy`
 * method.
 *
 * @param songCode The identifier of the music resource.
 *
 * @return
 * A `requestId`, which is the unique identifier of this request.
 */
- (NSString *)preloadWithSongCode:(NSInteger)songCode NS_SWIFT_NAME(preload(songCode:));

/**
 * @brief Checks whether a music resource has been preloaded.
 *
 * @details
 * This method is a synchronous call. To preload a new music resource, you can call
 * `preloadWithSongCode:`.
 *
 * @param songCode The identifier of the music resource used to specify a particular music resource.
 *
 * @return
 * - 0: The method call succeeds and the music resource has been preloaded.
 * - < 0: The method call fails. See `Error Codes` for more details and troubleshooting suggestions.
 */
- (NSInteger)isPreloadedWithSongCode:(NSInteger)songCode NS_SWIFT_NAME(isPreloaded(songCode:));

/**
 * @brief Deletes a cached music resource.
 *
 * @details
 * You can call this method to delete a cached music resource. To delete multiple music resources,
 * you can call this method multiple times.
 *
 * @note This method does not delete cached music resources that are currently playing.
 *
 * @param songCode The identifier of the music resource to be deleted.
 *
 * @return
 * - 0: The method call succeeds and the music resource is deleted.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting suggestions.
 */
- (NSInteger)removeCacheWithSongCode:(NSInteger)songCode NS_SWIFT_NAME(removeCache(songCode:));

/**
 * @brief Gets information about the cached music resources.
 *
 * @details
 * Before calling this method, you need to allocate sufficient memory to store information about the
 * cached music resources.  
 * If you want to set the number of music resources that can be cached, you can configure it through
 * the `config` parameter in `sharedContentCenterWithConfig:`.
 * When you no longer need the cached music resources, you should release the memory in time to
 * prevent memory leaks.
 *
 * @return
 * - If the method call succeeds, returns an array containing `AgoraMusicCacheInfo` objects.
 * - If the method call fails, returns nil.
 */
- (NSArray *)getCaches NS_SWIFT_NAME(getCaches());

/**
 * @brief Creates an ID for the chorus segment of a music resource.
 *
 * @details
 * Applicable scenarios: When you need to play the chorus segment of a music resource, you must call
 * this method to create an ID for the chorus segment of the resource using the `jsonOption`
 * parameter and the `songCode` of the music resource. This ID serves as the unique identifier of
 * the resource. After obtaining this ID, you need to pass it as the `songCode` parameter when
 * calling the corresponding methods to open, preload, or remove the resource.
 *
 * @param songCode The unique identifier of the music resource. You can obtain it by calling the
 * `getMusicCollectionWithMusicChartId:page:pageSize:jsonOption:` or
 * `searchMusicWithKeyWord:page:pageSize:jsonOption:` method, and retrieve the `songCode` from the
 * `onMusicCollectionResult:result:reason:` callback.
 * @param jsonOption Extended JSON field, defaults to nil. The following values are currently
 * supported:
 * | Key        | Value                                                                                                                                                                                                                                                                                                      | Example                          |
 * | ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------- |
 * | sceneType  | Channel profile: <br> - 1: Live streaming scenario: Karaoke and background music playback. <br> - 2: Live streaming scenario: Background music playback. <br> - 3: (Default) Voice chat scenario: Karaoke. <br> - 4: Voice chat scenario: Background music playback. <br> - 5: VR scenario: Karaoke and background music playback. <br> Note: Agora charges you based on the channel profile you specify in `sceneType`. Different channel profiles correspond to different rates. Refer to the billing documentation for details. To switch channel profiles, you must call this method again and pass in a new `sceneType` value. | {"sceneType":1}                  |
 * | highPart   | The index of the chorus segment. You can obtain the index from the `onMusicCollectionResult:result:reason:` callback and pass it in. The index starts from 0.                                                                                                                                              | {"format": {"highpart": 0}}      |
 *
 * @return
 * - If the method call succeeds, returns the generated internal ID of the music resource.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting suggestions.
 */
- (NSInteger)getInternalSongCode:(NSInteger)songCode jsonOption:(NSString * _Nullable)jsonOption  NS_SWIFT_NAME(getInternalSongCode(songCode:jsonOption:));

/**
 * @brief Gets the download URL for the lyrics of a music asset.
 *
 * @details
 * After this method is successfully called, the SDK triggers the
 * `onLyricResult:songCode:lyricUrl:reason:` callback to report the download URL of the lyrics.
 *
 * @param songCode The identifier of the music asset, used to specify the music resource.
 * @param lyricType The type of lyrics:
 * - 0: xml format.
 * - 1: lrc format.
 *
 * @return
 * A `requestId`, which is the unique identifier for this request.
 */
- (NSString *)getLyricWithSongCode:(NSInteger)songCode lyricType:(NSInteger)lyricType NS_SWIFT_NAME(getLyric(songCode:lyricType:));

/**
 * @brief Retrieves detailed information of a specific music resource.
 *
 * @details
 * Before calling this method, you need to obtain the identifier of the corresponding music
 * resource. You can retrieve music resources by calling the
 * `getMusicCollectionWithMusicChartId:page:pageSize:jsonOption:` or
 * `searchMusicWithKeyWord:page:pageSize:jsonOption:` methods, and obtain the music resource
 * identifier (`songCode`) through the `onMusicCollectionResult:result:reason:` callback triggered
 * by those methods.
 * After you call this method, the SDK triggers the
 * `onSongSimpleInfoResult:songCode:simpleInfo:reason:` callback to report the detailed information
 * of the music resource.
 *
 * @param songCode The identifier of the music resource, used to specify the music resource.
 *
 * @return
 * A `requestId`, which uniquely identifies this request.
 */
- (NSString *)getSongSimpleInfoWithSongCode:(NSInteger)songCode NS_SWIFT_NAME(getSongSimpleInfo(songCode:));

/**
 * @brief Whether to dispatch callbacks to the main queue.
 *
 * @details
 * If you do not dispatch callback methods to the main queue, your app should manually dispatch UI
 * operations to the main queue.
 *
 * @param enabled - `YES`: Dispatch callback methods to the main queue.
 * - `NO`: (Default) Do not dispatch callback methods to the main queue.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and recommended solutions.
 */
- (void)enableMainQueueDispatch:(BOOL)enabled;
 
/**
 * @brief Destroys the `AgoraMusicContentCenter` instance.
 *
 * @details
 * You must call this method before calling the `destroy` method of `AgoraRtcEngineKit`.
 *
 */
+ (void)destroy;


#pragma mark - Unavailable Delegate Methods
#if TARGET_OS_IPHONE
- (void)onMusicChartsResult:(NSString *)requestId result:(NSArray<AgoraMusicChartInfo*> *)result errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(ios,deprecated=7_0,message="Use onMusicChartsResult:result:statusCode: instead.")));
- (void)onMusicCollectionResult:(NSString *)requestId result:(AgoraMusicCollection *)result errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(ios,deprecated=7_0,message="Use onMusicCollectionResult:result:statusCode: instead.")));
- (void)onLyricResult:(NSString*)requestId songCode:(NSInteger)songCode lyricUrl:(NSString* _Nullable)lyricUrl errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(ios,deprecated=7_0,message="Use onLyricResult:songCode:lyricUrl:statusCode: instead.")));
- (void)onSongSimpleInfoResult:(NSString*)requestId songCode:(NSInteger)songCode simpleInfo:(NSString* _Nullable)simpleInfo errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(ios,deprecated=7_0,message="Use onSongSimpleInfoResult:songCode:simpleInfo:statusCode: instead.")));
- (void)onPreLoadEvent:(NSString*)requestId songCode:(NSInteger)songCode percent:(NSInteger)percent lyricUrl:(NSString * _Nullable)lyricUrl state:(AgoraMusicContentCenterPreloadState)state errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(ios,deprecated=7_0,message="Use onPreLoadEvent:songCode:percent:lyricUrl:status:statusCode: instead.")));
#endif

#if (!(TARGET_OS_IPHONE) && (TARGET_OS_MAC))
- (void)onMusicChartsResult:(NSString *)requestId result:(NSArray<AgoraMusicChartInfo*> *)result errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(macos,deprecated=10_9,message="Use onMusicChartsResult:result:statusCode: instead.")));
- (void)onMusicCollectionResult:(NSString *)requestId result:(AgoraMusicCollection *)result errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(macos,deprecated=10_9,message="Use onMusicCollectionResult:result:statusCode: instead.")));
- (void)onLyricResult:(NSString*)requestId songCode:(NSInteger)songCode lyricUrl:(NSString* _Nullable)lyricUrl errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(macos,deprecated=10_9,message="Use onLyricResult:songCode:lyricUrl:statusCode: instead.")));
- (void)onSongSimpleInfoResult:(NSString*)requestId songCode:(NSInteger)songCode simpleInfo:(NSString* _Nullable)simpleInfo errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(macos,deprecated=10_9,message="Use onSongSimpleInfoResult:songCode:simpleInfo:statusCode: instead.")));
- (void)onPreLoadEvent:(NSString*)requestId songCode:(NSInteger)songCode percent:(NSInteger)percent lyricUrl:(NSString * _Nullable)lyricUrl state:(AgoraMusicContentCenterPreloadState)state errorCode:(AgoraMusicContentCenterStateReason)errorCode __attribute__((availability(macos,deprecated=10_9,message="Use onPreLoadEvent:songCode:percent:lyricUrl:status:statusCode: instead.")));
#endif

@end

NS_ASSUME_NONNULL_END
