/**
 *
 * Agora Real Time Engagement
 * Copyright (c) 2024 Agora IO. All rights reserved.
 *
 */

#import <Foundation/Foundation.h>

@class AgoraRteError;
@class AgoraRteObserver;

/**
 * The InitialConfig class is used to initialize the AgoraRte object.
 * @since v4.4.0
 * @technical preview
 */
__attribute__((visibility("default"))) @interface AgoraRteInitialConfig : NSObject

- (instancetype _Nonnull)init;

@end

/**
 * @brief RTE configuration class, used to configure the `RTE` object.
 *
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRteConfig : NSObject

- (instancetype _Nonnull)init;

/**
 * @brief Sets the App ID.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method before `initMediaEngine:error:`.
 *
 * @param appId The App ID of your project, which you can obtain from the Console.
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 */
- (void)setAppId:(NSString * _Nullable)appId error:(AgoraRteError * _Nullable)error;

/**
 * Get the set App ID.
 * @since v4.4.0
 * @param error AgoraRteError object may return the following AgoraRteErrorCode
 *  - AgoraRteOk: Success
 * @return NSString Returns the set App ID value
 */
- (NSString* _Nullable)appId:(AgoraRteError * _Nullable)error;


- (void)setLogFolder:(NSString * _Nullable)logFolder error:(AgoraRteError * _Nullable)error;
- (NSString* _Nullable)logFolder:(AgoraRteError * _Nullable)error;

- (void)setLogFileSize:(size_t)logFileSize error:(AgoraRteError * _Nullable)error;
- (size_t)logFileSize:(AgoraRteError * _Nullable)error;

- (void)setAreaCode:(int32_t)areaCode error:(AgoraRteError * _Nullable)error;
- (int32_t)areaCode:(AgoraRteError * _Nullable)error;

- (void)setCloudProxy:(NSString * _Nullable)cloudProxy error:(AgoraRteError * _Nullable)error;
- (NSString * _Nullable)cloudProxy:(AgoraRteError * _Nullable)error;

/**
 * @brief Configures technical preview or specially customized features provided by the SDK via
 * JSON.
 *
 * @since v4.4.0
 *
 * @details
 * Applicable scenarios: You can call this method when you need to set private parameters or use
 * customized features.
 * Call timing: This method must be called before `setConfigs:error:`.
 *
 * @param jsonParameter Parameters in the form of a JSON string.
 * @param error Status or error information. See `AgoraRteError` for details.
 *
 */
- (void)setJsonParameter:(NSString * _Nullable)jsonParameter error:(AgoraRteError * _Nullable)error;

/**
 * Get the currently configured private parameters of the Rte.
 * @since v4.4.0
 * @param error 
 *   - AgoraRteOk: Success
 * @return NSString The set JSON formatted parameter set
 */
- (NSString * _Nullable)jsonParameter:(AgoraRteError * _Nullable)error;

@end

/**
 * The AgoraRte class, which is the basic interface of the Agora Real Time Engagement SDK.
 * @since v4.4.0
 */
__attribute__((visibility("default"))) @interface AgoraRte : NSObject

/**
 * @brief Bridges an RTE object from `AgoraRtcEngineKit`.
 *
 * @since v4.4.0
 *
 * @details
 * The RTE object created by calling this method does not require initialization through
 * `initMediaEngine:error:`. If you have not previously created and initialized the RTC engine, you
 * can first call `AgoraRte` to create an RTE object, and then initialize it using
 * `initMediaEngine:error:`.
 * Call timing: Before calling this method, make sure you have initialized the RTC engine by calling
 * `sharedEngineWithConfig:delegate:`.
 *
 * @param error A pointer to an `AgoraRteError` object used to receive the status and error code.
 *
 * @return
 * An RTE object.
 */
+ (instancetype _Nonnull)getFromBridge:(AgoraRteError * _Nullable)error;

/**
 * @brief Constructs an `RTE` object.
 *
 * @since v4.4.0
 *
 * @details
 * The `RTE` object is used to organize and manage internal resources of `Rte`.
 * Call timing: You must call this method before `initMediaEngine:error:`.
 *
 * @param config Initialization settings. Currently, you can pass `nil`.
 *
 * @return
 * - If the method call succeeds, returns an `RTE` object.
 * - If the method call fails, returns `nil`.
 */
- (instancetype _Nonnull)initWithInitialConfig:(AgoraRteInitialConfig * _Nullable)config;

/**
 * @brief Initializes the RTE engine.
 *
 * @since v4.4.0
 *
 * @details
 * This method is asynchronous and returns the initialization result through a callback function.  
 * Call timing: You need to call this method after creating the `RTE` object and setting the App ID.
 *
 * @note This return value only indicates whether the initialization operation has been successfully
 * scheduled in the asynchronous task queue. It does not indicate whether the initialization has
 * succeeded.
 *
 * @param cb An asynchronous callback function used to return the engine initialization result. You
 * can determine the result or error code of the engine initialization through the `err` parameter
 * of this callback. See `AgoraRteError` for details.
 * @param error Error information during the initialization process. See `AgoraRteError` for
 * details. When calling the `initMediaEngine:error:` method to initialize, if an immediately
 * detectable error occurs (such as invalid parameters or insufficient resources), the SDK
 * synchronously returns the error information through this parameter.
 *
 * @return
 * Whether the asynchronous operation is successfully added to the queue:
 * - `YES`: Successfully added to the queue.
 * - `NO`: Failed to be added to the queue.
 * Note: This return value only indicates whether the initialization operation has been successfully
 * scheduled in the asynchronous task queue. It does not indicate whether the initialization has
 * succeeded.
 */
- (BOOL)initMediaEngine:(void (^ _Nullable)(AgoraRteError* _Nullable err) )cb error:(AgoraRteError * _Nullable)error;

/**
 * @brief Gets the configuration information of the RTE object.
 *
 * @since v4.4.0
 *
 * @details
 * Call timing: You must call this method after `initWithInitialConfig:`.
 *
 * @param config The settings of the RTE object. See `AgoraRteConfig`.
 * @param error The status or error information. See `AgoraRteError`.
 *
 * @return
 * Whether the configuration is retrieved successfully:
 * - `YES`: Retrieval succeeded.
 * - `NO`: Retrieval failed.
 */
- (BOOL)getConfigs:(AgoraRteConfig *_Nonnull)config error:(AgoraRteError * _Nullable)error;

/**
 * @brief Configures the information of the RTE object.
 *
 * @since v4.4.0
 *
 * @details
 * You can call this method to set information such as the App ID.
 * Call timing: This method must be called after `initWithInitialConfig:`.
 *
 * @param config Settings for the RTE object. See `AgoraRteConfig`.
 * @param error Status or error information. See `AgoraRteError`.
 *
 * @return
 * Whether the configuration is successful:
 * - `YES`: Configuration succeeded.
 * - `NO`: Configuration failed.
 */
- (BOOL)setConfigs:(AgoraRteConfig * _Nonnull)config error:(AgoraRteError * _Nullable)error;

/**
 * Register AgoraRte observer.
 * @since v4.4.0
 * @param observer The object that observes AgoraRte callback events.
 * @param error AgoraRteError object may return the following AgoraRteErrorCode
 *   - AgoraRteOk: Success
 *   - AgoraRteErrorInvalidOperation: The corresponding internal AgoraRte object has been destroyed or is invalid.
 *   - AgoraRteErrorInvalidArgument: The registered observer object is null.
 * @return BOOL Returns the result of registering the observer.
 *   - YES: Registration is successful
 *   - NO: Registration failed
 * @technical preview
 */
- (BOOL)registerObserver:(AgoraRteObserver * _Nonnull)observer error:(AgoraRteError * _Nullable)error;

/**
 * Unregister AgoraRte observer.
 * @since v4.4.0
 * @param error AgoraRteError object may return the following AgoraRteErrorCode
 *   - AgoraRteOk: Success
 *   - AgoraRteErrorInvalidOperation: The corresponding internal AgoraRte object has been destroyed or is invalid.
 *   - AgoraRteErrorInvalidArgument: The observer object to be unregistered is null.
 * @return BOOL Returns the result of unregistering the observer.
 *   - YES: Unregistration is successful
 *   - NO: Unregistration failed
 * @technical preview
 */
- (BOOL)unregisterObserver:(AgoraRteObserver * _Nullable)observer error:(AgoraRteError * _Nullable)error;


/**
 * @brief Destroys the RTE object.
 *
 * @since v4.4.0
 *
 * @details
 * This method releases all resources used by the RTE object.
 *
 * @param error The status or error information. See `AgoraRteError` for details.
 *
 * @return
 * Whether the RTE object is destroyed successfully:
 * - `YES`: Destruction succeeded.
 * - `NO`: Destruction failed.
 */
- (BOOL)destroy:(AgoraRteError * _Nullable)error;


@end
