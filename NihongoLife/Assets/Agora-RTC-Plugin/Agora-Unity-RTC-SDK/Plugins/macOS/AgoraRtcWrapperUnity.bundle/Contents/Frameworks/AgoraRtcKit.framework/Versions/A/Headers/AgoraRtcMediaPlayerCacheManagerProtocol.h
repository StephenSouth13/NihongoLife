//
//  Copyright (c) 2020 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@protocol AgoraRtcMediaPlayerCacheManagerProtocol <NSObject>

/**
 * @brief Gets a `AgoraRtcMediaPlayerCacheManagerProtocol` instance.
 *
 * @return
 * The `AgoraRtcMediaPlayerCacheManagerProtocol` instance.
 */
+ (instancetype)sharedInstance NS_SWIFT_NAME(sharedInstance());
/**
 * @brief Deletes all cached media files in the media player.
 *
 * @note The cached media file currently being played will not be deleted.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (int)removeAllCaches NS_SWIFT_NAME(removeAllCaches());
/**
 * @brief Deletes a cached media file that is the least recently used.
 *
 * @details
 * You can call this method to delete a cached media file when the storage space for the cached
 * files is about to reach its limit. After you call this method, the SDK deletes the cached media
 * file that is least used.
 *
 * @note The cached media file currently being played will not be deleted.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (int)removeOldCache NS_SWIFT_NAME(removeOldCache());
/**
 * @brief Deletes a cached media file.
 *
 * @note The cached media file currently being played will not be deleted.
 *
 * @param uri The URI (Uniform Resource Identifier) of the media file to be deleted.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (int)removeCacheByUri:(NSString *)uri NS_SWIFT_NAME(removeCache(byUri:));
/**
 * @brief Sets the storage path for the media files that you want to cache.
 *
 * @note Make sure `AgoraRtcEngineKit` is initialized before you call this method.
 *
 * @param cacheDir The absolute path of the media files to be cached. Ensure that the directory for
 * the media files exists and is writable.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (int)setCacheDir:(NSString *)cacheDir NS_SWIFT_NAME(setCacheDir(_:));
/**
 * @brief Sets the maximum number of media files that can be cached.
 *
 * @param count The maximum number of media files that can be cached. The default value is 1,000.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (int)setMaxCacheFileCount:(NSInteger)count NS_SWIFT_NAME(setMaxCacheFileCount(_:));
/**
 * @brief Sets the maximum size of the aggregate storage space for cached media files.
 *
 * @param cacheSize The maximum size (bytes) of the aggregate storage space for cached media files.
 * The default value is 1 GB.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (int)setMaxCacheFileSize:(NSInteger)cacheSize NS_SWIFT_NAME(setMaxCacheFileSize(_:));
/**
 * @brief Sets whether to delete cached media files automatically.
 *
 * @details
 * If you enable this function to remove cached media files automatically, when the cached media
 * files exceed either the number or size limit you set, the SDK automatically deletes the least
 * recently used cache file.
 *
 * @param enable Whether to enable the SDK to delete cached media files automatically:
 * - `YES`: Delete cached media files automatically.
 * - `NO`: (Default) Do not delete cached media files automatically.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (int)enableAutoRemoveCache:(BOOL)enable NS_SWIFT_NAME(enableAutoRemoveCache(_:));
/**
 * @brief Gets the storage path of the cached media files.
 *
 * @details
 * If you have not called the `setCacheDir:` method to set the storage path for the media files to
 * be cached before calling this method, you get the default storage path used by the SDK.
 *
 * @return
 * - The call succeeds, and the SDK returns the storage path of the cached media files.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (NSString *)cacheDir NS_SWIFT_NAME(cacheDir());
/**
 * @brief Gets the maximum number of media files that can be cached.
 *
 * @details
 * By default, the maximum number of media files that can be cached is 1,000.
 *
 * @return
 * - > 0: The call succeeds and returns the maximum number of media files that can be cached.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (NSInteger)maxCacheFileCount NS_SWIFT_NAME(maxCacheFileCount());
/**
 * @brief Gets the maximum size of the aggregate storage space for cached media files.
 *
 * @details
 * By default, the maximum size of the aggregate storage space for cached media files is 1 GB. You
 * can call the `setMaxCacheFileSize:` method to set the limit according to your scenarios.
 *
 * @return
 * - > 0: The call succeeds and returns the maximum size (in bytes) of the aggregate storage space
 * for cached media files.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (NSInteger)maxCacheFileSize NS_SWIFT_NAME(maxCacheFileSize());
/**
 * @brief Gets the number of media files that are cached.
 *
 * @return
 * - ≥ 0: The call succeeds and returns the number of media files that are cached.
 * - < 0: Failure. See `AgoraMediaPlayerReason`.
 */
- (NSInteger)cacheFileCount NS_SWIFT_NAME(cacheFileCount());

@end

NS_ASSUME_NONNULL_END
