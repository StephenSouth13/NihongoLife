#import <AVFoundation/AVFoundation.h>
#import <AVKit/AVPictureInPictureController.h>
#include <Foundation/Foundation.h>
#import <UIKit/UIKit.h>

/**
 * AgoraPIPState
 * @note AgoraPIPStateStarted: pip is started
 * @note AgoraPIPStateStopped: pip is stopped
 * @note AgoraPIPStateFailed: pip is failed
 */
typedef NS_ENUM(NSInteger, AgoraPIPState) {
  AgoraPIPStateStarted = 0,
  AgoraPIPStateStopped = 1,
  AgoraPIPStateFailed = 2,
};

/**
 * @protocol AgoraPIPStateChangedDelegate
 * @abstract A protocol that defines the methods for pip state changed.
 */
@protocol AgoraPIPStateChangedDelegate

/**
 * @method pipStateChanged
 * @param state
 *        The state of pip.
 * @param error
 *        The error message.
 * @abstract Delegate can implement this method to handle the pip state changed.
 */
- (void)pipStateChanged:(AgoraPIPState)state error:(NSString *_Nullable)error;

@end

@interface AgoraPIPVideoStream : NSObject

@property(nonatomic, copy) NSString *_Nonnull channelId;

@property(nonatomic, assign) unsigned int localUid;

@property(nonatomic, assign) unsigned int uid;

@property(nonatomic, assign) unsigned int backgroundColor;

@property(nonatomic, assign) unsigned int renderMode;

@property(nonatomic, assign) unsigned int mirrorMode;

@property(nonatomic, assign) unsigned int setupMode;

@property(nonatomic, assign) unsigned int sourceType;

@property(nonatomic, assign) BOOL enableAlphaMask;

@property(nonatomic, assign) unsigned int position;

@end

@interface AgoraPipContentViewLayout : NSObject

@property(nonatomic, assign) NSInteger padding;

@property(nonatomic, assign) NSInteger spacing;

@property(nonatomic, assign) NSInteger row;

@property(nonatomic, assign) NSInteger column;

@end

/**
 * @class AgoraPIPOptions
 * @abstract A class that defines the options for pip.
 */
@interface AgoraPIPOptions : NSObject

/**
 * @property sourceContentView
 * @abstract The source content view for pip, set to nil will use the root
 * view of the application as the source content view.
 */
@property(nonatomic, assign) UIView *_Nullable sourceContentView;

/**
 * @property contentView
 * @abstract The content view for pip.
 * @discussion The content view is the view that will be displayed in the pip.
 * If this property is set, the `canvasArray` will be ignored.
 */
@property(nonatomic, assign) UIView *_Nullable contentView;

/**
 * @property videoStreamArray
 * @abstract The video stream array for pip.
 * @discussion The video stream array is the array of video stream that will be displayed in
 * the pip. If the `contentView` is set, this property will be ignored.
 */
@property(nonatomic, strong)
    NSArray<AgoraPIPVideoStream *> *_Nullable videoStreamArray;

/**
 * @property contentViewLayout
 * @abstract The content view layout for pip.
 * @discussion The content view layout is the layout of the content view that will be
 * displayed in the pip. If the `contentView` is set, this property will be
 * ignored.
 */
@property(nonatomic, strong)
    AgoraPipContentViewLayout *_Nullable contentViewLayout;

/**
 * @property apiEngine
 * @abstract The api engine of iris to create internal renderers for video streams.
 * @discussion The api engine is the api engine that will be used to create internal renderers for video streams.
 * This property must be set if you want to use the `videoStreamArray` property, and will be ignored if the `contentView` property is set.
 */
@property(nonatomic, assign) void *_Nullable apiEngine;

/**
 * @property autoEnterEnabled
 * @abstract Whether to enable auto enter pip.
 */
@property(nonatomic, assign) BOOL autoEnterEnabled;

/**
 * @property preferredContentSize
 * @abstract The preferred content size for pip.
 */
@property(nonatomic, assign) CGSize preferredContentSize;

/**
 * @property controlStyle
 * @abstract The style of pip control.
 */
@property(nonatomic, assign) int controlStyle;

@end

/**
 * @class AgoraPIPController
 * @abstract A class that controls the pip.
 */
@interface AgoraPIPController : NSObject<AVPictureInPictureControllerDelegate>

/**
 * @method initWith
 * @param delegate
 *        The delegate of pip state changed.
 * @abstract Initialize the pip controller.
 */
- (instancetype _Nonnull)initWith:
    (id<AgoraPIPStateChangedDelegate> _Nonnull)delegate;

/**
 * @method isSupported
 * @abstract Check if pip is supported.
 * @return Whether pip is supported.
 * @discussion This method is used to check if pip is supported, When No all
 * other methods will return NO or do nothing.
 */
- (BOOL)isSupported;

/**
 * @method isAutoEnterSupported
 * @abstract Check if pip is auto enter supported.
 * @return Whether pip is auto enter supported.
 */
- (BOOL)isAutoEnterSupported;

/**
 * @method isActivated
 * @abstract Check if pip is activated.
 * @return Whether pip is activated.
 */
- (BOOL)isActivated;

/**
 * @method setup
 * @param options
 *        The options for pip.
 * @abstract Setup pip or update pip options.
 * @return Whether pip is setup successfully.
 * @discussion This method is used to setup pip or update pip options, but only
 * the `videoCanvas` is allowed to update after the pip controller is
 * initialized, unless you call the `dispose` method and re-initialize the pip
 * controller.
 */
- (BOOL)setup:(AgoraPIPOptions *_Nonnull)options;

/**
 * @method getPIPHolderView
 * @abstract Get the pip holder view.
 * @return The pip holder view.
 */
- (UIView *_Nullable __weak)getPIPHolderView;

/**
 * @method start
 * @abstract Start pip.
 * @return Whether start pip is successful or not.
 * @discussion This method is used to start pip, however, it will only works
 * when application is in the foreground. If you want to start pip when
 * application is changing to the background, you should set the
 * `autoEnterEnabled` to YES when calling the `setup` method.
 */
- (BOOL)start;

/**
 * @method stop
 * @abstract Stop pip.
 * @discussion This method is used to stop pip, however, it will only works when
 * application is in the foreground. If you want to stop pip in the background,
 * you can use the `dispose` method, which will destroy the internal pip
 * controller and release the pip view.
 * If `isPictureInPictureActive` is NO, this method will do nothing.
 */
- (void)stop;

/**
 * @method dispose
 * @abstract Dispose all resources that pip controller holds.
 * @discussion This method is used to dispose all resources that pip controller
 * holds, which will destroy the internal pip controller and release the pip
 * view. Accroding to the Apple's documentation, you should call this method
 * when you want to stop pip in the background. see:
 * https://developer.apple.com/documentation/avkit/adopting-picture-in-picture-for-video-calls?language=objc
 */
- (void)dispose;

@end
