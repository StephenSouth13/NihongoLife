//
//  AgoraRtcEngineKit.h
//  AgoraRtcEngineKit
//
//  Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>

@protocol AgoraMediaRecorderDelegate;
@class AgoraMediaRecorderConfiguration;

__attribute__((visibility("default"))) @interface AgoraMediaRecorder : NSObject

/**
 * @brief Registers the `AgoraMediaRecorderDelegate` observer.
 *
 * @since v4.0.0
 *
 * @details
 * This method sets the callback for audio and video recording, so that the app can be notified of
 * the recording status and information of the audio and video stream during the recording process.
 * Before calling this method, make sure that:
 * - You have created and initialized the `AgoraRtcEngineKit` object.
 * - You have created the recorder using `createMediaRecorder:`.
 *
 * @param delegate The callback for audio and video stream recording. See
 * `AgoraMediaRecorderDelegate` for details.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting suggestions.
 */
- (int)setMediaRecorderDelegate:(id<AgoraMediaRecorderDelegate> _Nullable)delegate;

/**
 * @brief Sets whether to dispatch callbacks to the main queue.
 *
 * @details
 * If you do not dispatch callback methods to the main queue, your app should dispatch UI operations
 * to the main queue.
 *
 * @param enabled - `YES`: Dispatches callback methods to the main queue.
 * - `NO`: Does not dispatch callback methods to the main queue.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error codes` for details and troubleshooting suggestions.
 */
- (int)enableMainQueueDispatch:(BOOL)enabled NS_SWIFT_NAME(enableMainQueueDispatch(_:));

/**
 * @brief Starts audio and video stream recording.
 *
 * @since v4.0.0
 *
 * @details
 * This method starts recording audio and video streams. The Agora SDK supports recording both local
 * and remote users' audio and video streams simultaneously.
 * 
 * Before starting the recording, make sure that:
 * - You have created a media recorder object using `createMediaRecorder:`.
 * - You have registered a media recorder observer using `setMediaRecorderDelegate:` to listen for
 * recording-related callbacks.
 * - You have joined a channel.
 * 
 * This method supports recording the following data:
 * - Audio captured by the microphone in AAC encoding format.
 * - Video captured by the camera in H.264 or H.265 encoding format.
 * 
 * After recording starts, if the video resolution changes during the recording process, the SDK
 * stops recording. If the audio sample rate or channel count changes, the SDK continues recording
 * and generates a single MP4 recording file.
 * 
 * A recording file is generated only when recordable audio or video streams are detected. If there
 * are no recordable audio or video streams, or if the streams are interrupted for more than 5
 * seconds during recording, the SDK stops recording and triggers the
 * `mediaRecorder:stateDidChanged:uid:state:reason:` (`AgoraMediaRecorderStateError`,
 * `AgoraMediaRecorderReasonCodeNoStream`) callback.
 *
 * @note
 * - If you want to record the local audio and video streams, make sure the local user's role is set
 * to broadcaster before starting the recording.
 * - If you want to record remote users' audio and video streams, make sure you have subscribed to
 * the target user's audio and video streams before starting the recording.
 *
 * @param config The configuration for audio and video stream recording. See
 * `AgoraMediaRecorderConfiguration` for details.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails. See `Error Codes` for details and troubleshooting.
 *   - -2: Invalid parameter. Make sure that:
 *     - The specified file path for saving the recording is correct and writable.
 *     - The specified recording file format is correct.
 *     - The maximum recording duration is set correctly.
 *   - -4: The current state of `AgoraRtcEngineKit` does not support this operation. This may be due
 * to an ongoing recording or a recording error that caused it to stop.
 *   - -7: `AgoraRtcEngineKit` is not initialized when the method is called. Make sure that the
 * `AgoraMediaRecorder` object is created before calling this method.
 */
- (int)startRecording:(AgoraMediaRecorderConfiguration* _Nonnull)config NS_SWIFT_NAME(startRecording(_:));
/**
 * @brief Stops recording the audio and video stream.
 *
 * @since v4.0.0
 *
 * @note After calling `startRecording:`, if you want to stop recording, you must call this method;
 * otherwise, the generated recording file may not play properly.
 *
 * @return
 * - 0: The method call succeeds.
 * - < 0: The method call fails:
 *   - -7: The method is called before `AgoraRtcEngineKit` is initialized. Make sure you have
 * created the `Recorder` object before calling this method.
 */
- (int)stopRecording NS_SWIFT_NAME(stopRecording());

@end