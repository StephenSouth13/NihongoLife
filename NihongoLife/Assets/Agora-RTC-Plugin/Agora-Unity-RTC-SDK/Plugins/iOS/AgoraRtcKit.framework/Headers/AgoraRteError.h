/**
 *
 * Agora Real Time Engagement
 * Copyright (c) 2024 Agora IO. All rights reserved.
 *
 */

#import <Foundation/Foundation.h>

#import "AgoraRteEnumerates.h"

/**
 * @brief This class provides methods to retrieve error codes and error messages.
 *
 * @technical preview
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRteError : NSObject

- (instancetype _Nonnull)init;

- (void)setErrorWithCode:(AgoraRteErrorCode)code message:(NSString * _Nullable)message;

/**
 * @brief Gets the error code returned from an API call.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You can call this method to retrieve the error code when an API call fails.
 *
 * @return
 * The error code. See `AgoraRteErrorCode` for details.
 */
- (AgoraRteErrorCode)code;

/**
 * @brief Retrieves detailed error information.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: When the API call fails, you can call this method to retrieve the error information
 * to help troubleshoot the issue.
 *
 * @return
 * Error information.
 */
- (NSString * _Nullable)message;

@end
