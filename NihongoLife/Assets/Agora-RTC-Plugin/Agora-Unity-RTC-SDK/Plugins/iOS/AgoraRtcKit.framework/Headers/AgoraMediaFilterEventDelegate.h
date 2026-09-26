//
//  AgoraMediaFilterEventDelegate.h
//  Agora SDK
//
//  Created by LLF on 2020-9-21.
//  Copyright (c) 2020 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>

/**
 * @brief The context information of the extension.
 */
@interface AgoraExtensionContext : NSObject
/**
 * Whether the `uid` in `AgoraExtensionContext` is valid:
 * - `YES`: The `uid` is valid.
 * - `NO`: The `uid` is invalid.
 */
@property (assign, nonatomic) BOOL isValid;
/**
 * The user ID. 0 represents a local user, while greater than 0 represents a remote user.
 */
@property (assign, nonatomic) NSUInteger uid;
/**
 * The name of the extension provider.
 */
@property (copy, nonatomic) NSString * _Nullable providerName;
/**
 * The name of the extension.
 */
@property (copy, nonatomic) NSString * _Nullable extensionName;
@end

@protocol AgoraMediaFilterEventDelegate <NSObject>
@optional
/**
 * @brief The event callback of the extension.
 *
 * @details
 * To listen for events while the extension is running, you need to register this callback.
 *
 * @param context The context information of the extension, see `AgoraExtensionContext`.
 * @param key The key of the extension.
 * @param value The value of the extension key.
 *
 */
- (void)onEventWithContext:(AgoraExtensionContext * _Nonnull)context
            key:(NSString * _Nullable)key
          value:(NSString * _Nullable)value NS_SWIFT_NAME(onEventWithContext(_:key:value:));

/**
 * @brief Occurs when the extension is enabled.
 *
 * @details
 * The callback is triggered after the extension is successfully enabled.
 *
 * @param context The context information of the extension, see `AgoraExtensionContext`.
 *
 */
- (void)onExtensionStartedWithContext:(AgoraExtensionContext * _Nonnull)context NS_SWIFT_NAME(onExtensionStartedWithContext(_:));

/**
 * @brief Occurs when the extension is disabled.
 *
 * @details
 * The callback is triggered after the extension is successfully disabled.
 *
 * @param context The context information of the extension, see `AgoraExtensionContext`.
 *
 */
- (void)onExtensionStoppedWithContext:(AgoraExtensionContext * _Nonnull)context NS_SWIFT_NAME(onExtensionStoppedWithContext(_:));
 
/**
 * @brief Occurs when the extension runs incorrectly.
 *
 * @details
 * In case of extension enabling failure or runtime errors, the extension triggers this callback and
 * reports the error code along with the reasons.
 *
 * @param context The context information of the extension, see `AgoraExtensionContext`.
 * @param error Error code. For details, see the extension documentation provided by the extension
 * provider.
 * @param message Reason. For details, see the extension documentation provided by the extension
 * provider.
 *
 */
- (void)onExtensionErrorWithContext:(AgoraExtensionContext * _Nonnull)context
                   error:(int)error
                 message:(NSString * _Nullable)message NS_SWIFT_NAME(onExtensionErrorWithContext(_:error:message:));
 
@end
