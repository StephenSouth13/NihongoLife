/**
 *
 * Agora Real Time Engagement
 * Copyright (c) 2024 Agora IO. All rights reserved.
 *
 */

#import <Foundation/Foundation.h>

#import "AgoraRteEnumerates.h"

#if TARGET_OS_IPHONE
#import <UIKit/UIKit.h>
typedef UIView AgoraRteView;
#elif TARGET_OS_MAC
#import <AppKit/AppKit.h>
typedef NSView AgoraRteView;
#endif

@class AgoraRteError;
@class AgoraRte;

/**
 * The Rect class is used to manage the rect.
 * @since v4.4.0
 * @technical preview
 */
__attribute__((visibility("default"))) @interface AgoraRteRect : NSObject
- (instancetype _Nonnull)init;

- (void)setX:(int32_t)x;
- (int32_t)x;

- (void)setY:(int32_t)y;
- (int32_t)y;

- (void)setWidth:(int32_t)width;
- (int32_t)width;

- (void)setHeight:(int32_t)height;
- (int32_t)height;
@end

/**
 * The ViewConfig class is used to manage the view config.
 * @since v4.4.0
 * @technical preview
 */
__attribute__((visibility("default"))) @interface AgoraRteViewConfig : NSObject
- (instancetype _Nonnull)init;

- (void)setCropArea:(AgoraRteRect* _Nullable)cropArea error:(AgoraRteError* _Nullable)error;

- (AgoraRteRect* _Nullable)cropArea:(AgoraRteError* _Nullable)error;
@end

/**
 * The CanvasInitialConfig class is used to initialize the Canvas object.
 * @since v4.4.0
 * @technical preview
 */
__attribute__((visibility("default"))) @interface AgoraRteCanvasInitialConfig : NSObject
- (instancetype _Nonnull)init;
@end


/**
 * @brief Provides methods for configuring player video rendering.
 *
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRteCanvasConfig : NSObject
- (instancetype _Nonnull)init;

/**
 * Set the video render mode.
 * @since v4.4.0
 * @param mode The render mode to set. Refer to AgoraRteVideoRenderMode type, default is AgoraRteVideoRenderModeHidden.
 * @param error AgoraRteError object may return the following AgoraRteErrorCode
 * - AgoraRteOk: Success
 * - AgoraRteErrorInvalidArgument: The mode parameter is set to an illegal value.
 * @return void
 */
- (void)setVideoRenderMode:(AgoraRteVideoRenderMode)mode error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the currently set video rendering mode.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You need to call this method after `getConfigs:error:`.
 *
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * The currently set video rendering mode. See `AgoraRteVideoRenderMode` for details.
 */
- (AgoraRteVideoRenderMode)videoRenderMode:(AgoraRteError * _Nullable)error;

/**
 * @brief Sets the video mirror mode.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method before `setConfigs:error:`.
 *
 * @param mode The mirror mode. See `AgoraRteVideoMirrorMode` for details. The default value is
 * `AgoraRteVideoMirrorModeAuto`, which means the SDK determines the mirror mode. By default, the
 * mirror mode for remote users is disabled.
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 */
- (void)setVideoMirrorMode:(AgoraRteVideoMirrorMode)mode error:(AgoraRteError* _Nullable)error;

/**
 * @brief Gets the currently set mirror mode.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method after `getConfigs:error:`.
 *
 * @param error The status or error information. See `AgoraRteError`.
 *
 * @return
 * The currently set mirror mode. See `AgoraRteVideoMirrorMode`.
 */
- (AgoraRteVideoMirrorMode)videoMirrorMode:(AgoraRteError * _Nullable)error;

- (void)setCropArea:(AgoraRteRect* _Nonnull)cropArea error:(AgoraRteError * _Nullable)error;

- (AgoraRteRect* _Nonnull)cropArea:(AgoraRteError* _Nullable)error;

@end


/**
 * AgoraRteCanvas interface, used to set the video rendering view.
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRteCanvas : NSObject

/**
 * Construct an AgoraRteCanvas object.
 * @since v4.4.0
 * @param rte AgoraRte object.
 * @param config CanvasInitialConfig initialization configuration object. Currently, a null pointer can be passed.
 */
- (instancetype _Nonnull)initWithRte:(AgoraRte* _Nonnull)rte initialConfig:(AgoraRteCanvasInitialConfig * _Nullable)config;

/**
 * @brief Gets the current video rendering configuration of the player.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You need to call this method after `initWithRte:initialConfig:`.
 *
 * @param config Settings of the `Canvas` object. See `AgoraRteCanvasConfig` for details.
 * @param error Status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the configuration is retrieved successfully:
 * - `YES`: Retrieval succeeds.
 * - `NO`: Retrieval fails.
 */
- (BOOL)getConfigs:(AgoraRteCanvasConfig* _Nonnull)config error:(AgoraRteError* _Nullable)error;

/**
 * @brief Sets the video rendering configuration for the player.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method before `openWithUrl:startTime:cb:`.
 *
 * @param config The settings of the `Canvas` object. See `AgoraRteCanvasConfig` for details.
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the configuration is successful:
 * - `YES`: The configuration is successful.
 * - `NO`: The configuration fails.
 */
- (BOOL)setConfigs:(AgoraRteCanvasConfig* _Nonnull)config error:(AgoraRteError* _Nullable)error;

/**
 * @brief Adds a rendering view.
 *
 * @since v4.4.0
 *
 * @details
 * Applicable scenarios: When you need to use the player to play a video, you can call this method
 * to add a view to the video renderer in order to display the video content.
 * Call timing: This method must be called before `setCanvas:error:`.
 *
 * @note Currently, only one view is supported.
 *
 * @param view Pointer to the View object.
 * - iOS: A `UIView` object.
 * - macOS: An `NSView` object.
 * @param config Configuration for the view object. Pass in `nil` for now.
 * @param error Status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the view is added successfully:
 * - `YES`: The view is added successfully.
 * - `NO`: Failed to add the view.
 */
- (BOOL)addView:(AgoraRteView * _Nonnull)view config:(AgoraRteViewConfig* _Nullable)config error:(AgoraRteError* _Nullable)error;

/**
 * @brief Removes a rendering view.
 *
 * @since v4.4.0
 *
 * @details
 * After you call `addView:config:error:` to add a view, you can call this method to remove the
 * rendering view.
 * Call timing: This method must be called after `addView:config:error:`.
 *
 * @note Only one view can be removed at a time.
 *
 * @param view The view object to be removed.
 * @param config The configuration of the view object. Pass in nil for now.
 * @param error Status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the view is removed successfully:
 * - `YES`: The view is removed successfully.
 * - `NO`: Failed to remove the view.
 */
- (BOOL)removeView:(AgoraRteView * _Nonnull)view config:(AgoraRteViewConfig* _Nullable)config error:(AgoraRteError* _Nullable)error;

@end
