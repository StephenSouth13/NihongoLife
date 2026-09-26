//  AgoraVideoEffectObject.h
//  AgoraVideoEffect
//
//  Copyright (c) 2023 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraObjects.h"

NS_ASSUME_NONNULL_BEGIN

/**
 * @brief The protocol for video effect operations.
 *
 * @since v4.6.0
 */
@protocol AgoraVideoEffectObject <NSObject>

#pragma mark - Enumerations
/**
 * @brief Types of applicable video effect nodes.
 *
 * @since v4.6.0
 */
typedef NS_ENUM(NSUInteger, AgoraVideoEffectNodeId) {
    /**
     * (1): Beauty effect node.
     */
    AgoraVideoEffectNodeBeauty      = 1 << 0, 
    /**
     * (2): Style makeup effect node.
     */
    AgoraVideoEffectNodeStyleMakeup = 1 << 1, 
    /**
     * (4): Filter effect node.
     */
    AgoraVideoEffectNodeFilter      = 1 << 2  
};

/**
 * @brief Actions that can be performed on video effect nodes.
 *
 * @since v4.6.0
 */
typedef NS_ENUM(NSInteger, AgoraVideoEffectAction) {
    /**
     * (1): Save the current parameters of the video effect.
     */
    AgoraVideoEffectActionSave  = 1, 
    /**
     * (2): Reset the video effect to default parameters.
     */
    AgoraVideoEffectActionReset = 2  
};

#pragma mark - Core Operations
/**
 * @brief Adds or updates the video effect for the specified node ID and template.
 *
 * @since v4.6.0
 *
 * @note
 * Priority rules:
 * - The AgoraVideoEffectNodeStyleMakeup node takes precedence over the AgoraVideoEffectNodeFilter
 * parameter.
 * - To apply the AgoraVideoEffectNodeFilter parameter, you must first remove the
 * AgoraVideoEffectNodeStyleMakeup node:
 * ```
 * removeVideoEffect(VIDEO_EFFECT_NODE_ID::STYLE_MAKEUP);
 * addOrUpdateVideoEffect(VIDEO_EFFECT_NODE_ID::FILTER, "template name");
 * ```
 *
 * @param nodeId The unique identifier or combination of identifiers for the video effect node.
 * Examples:
 * - Single effect: `VIDEO_EFFECT_NODE_ID::BEAUTY`
 * - Combined effects: `VIDEO_EFFECT_NODE_ID::BEAUTY | VIDEO_EFFECT_NODE_ID::STYLE_MAKEUP`.
 * @param templateName The name of the effect template. If set to null or an empty string, the SDK
 * automatically loads the default parameter configuration from the resource package.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)addOrUpdateVideoEffectWithNodeId:(NSUInteger)nodeId
                          templateName:(NSString *)templateName
NS_SWIFT_NAME(addOrUpdateVideoEffect(nodeId:templateName:));

/**
 * @brief Removes the video effect with the specified node ID.
 *
 * @since v4.6.0
 *
 * @param nodeId The unique identifier of the video effect node to remove. See
 * `AgoraVideoEffectNodeId`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)removeVideoEffectWithNodeId:(NSUInteger)nodeId
NS_SWIFT_NAME(removeVideoEffect(nodeId:));

#pragma mark - Effect Operations


/**
 * @brief Performs an action on the specified video effect node.
 *
 * @since v4.6.0
 *
 * @param nodeId The unique identifier of the video effect node.
 * @param actionId The action to perform on the video effect. See `AgoraVideoEffectAction`.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)performVideoEffectActionWithNodeId:(NSUInteger)nodeId
                                actionId:(AgoraVideoEffectAction)actionId
NS_SWIFT_NAME(performVideoEffectAction(nodeId:actionId:));

#pragma mark - Parameter Configuration
/**
 * @brief Sets the float parameter for video effects.
 *
 * @since v4.6.0
 *
 * @param option The category of the parameter option.
 * @param key The key name of the parameter.
 * @param floatValue The float value to set.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVideoEffectFloatParamWithOption:(NSString *)option
                                    key:(NSString *)key
                              floatValue:(float)floatValue
NS_SWIFT_NAME(setVideoEffectFloatParam(option:key:floatValue:));

/**
 * @brief Sets an integer parameter for video effects.
 *
 * @since v4.6.0
 *
 * @param option The category of the parameter option.
 * @param key The key name of the parameter.
 * @param intValue The integer value to set.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVideoEffectIntParamWithOption:(NSString *)option
                                    key:(NSString *)key
                               intValue:(int)intValue
NS_SWIFT_NAME(setVideoEffectIntParam(option:key:intValue:));

/**
 * @brief Sets the boolean parameter for video effects.
 *
 * @since v4.6.0
 *
 * @param option The category of the parameter option.
 * @param key The key name of the parameter.
 * @param boolValue The boolean value to set.
 * - `YES`: Set to enabled.
 * - `NO`: Set to disabled.
 *
 * @return
 * - 0: Success.
 * - < 0: Failure.
 */
- (int)setVideoEffectBoolParamWithOption:(NSString *)option
                                    key:(NSString *)key
                              boolValue:(BOOL)boolValue
NS_SWIFT_NAME(setVideoEffectBoolParam(option:key:boolValue:));

#pragma mark - Parameter Retrieval
/**
 * @brief Gets `float` type parameters in video effects.
 *
 * @since v4.6.0
 *
 * @details
 * Used to retrieve the value of a `float` type parameter corresponding to the specified option and
 * key in video effects.
 *
 * @param option The category of the parameter option.
 * @param key The key name of the parameter.
 *
 * @return
 * - If the parameter exists, returns the corresponding float value.
 * - If the parameter does not exist or an error occurs, returns 0.0f.
 */
- (float)getVideoEffectFloatParamWithOption:(NSString *)option
                                    key:(NSString *)key
NS_SWIFT_NAME(getVideoEffectFloatParam(option:key:));

/**
 * @brief Gets integer parameters in video effects.
 *
 * @since v4.6.0
 *
 * @details
 * Used to retrieve integer-type parameters in video effects.
 *
 * @param option The identifier of the parameter category.
 * @param key The key value of the parameter.
 *
 * @return
 * - If the method call succeeds, returns the corresponding integer value of the parameter.
 * - If the method call fails or the parameter does not exist, returns 0.
 */
- (int)getVideoEffectIntParamWithOption:(NSString *)option
                                    key:(NSString *)key
NS_SWIFT_NAME(getVideoEffectIntParam(option:key:));

/**
 * @brief Gets the boolean parameter in video effects.
 *
 * @since v4.6.0
 *
 * @param option The identifier of the parameter category.
 * @param key The key value of the parameter.
 *
 * @return
 * - `YES`: The parameter is enabled.
 * - `NO`: The parameter is not enabled or does not exist.
 */
- (BOOL)getVideoEffectBoolParamWithOption:(NSString *)option
                                    key:(NSString *)key
NS_SWIFT_NAME(getVideoEffectBoolParam(option:key:));

@end

NS_ASSUME_NONNULL_END
