//
//  Copyright (c) 2020 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "AgoraObjects.h"

@protocol AgoraAudioSpectrumDelegate <NSObject>
@optional
/**
 * @brief Gets the statistics of a local audio spectrum.
 *
 * @details
 * After successfully calling `registerAudioSpectrumDelegate:` to implement the
 * `onLocalAudioSpectrum:` callback in `AgoraAudioSpectrumDelegate` and calling
 * `enableAudioSpectrumMonitor:` to enable audio spectrum monitoring, the SDK triggers this callback
 * as the time interval you set to report the received remote audio data spectrum before encoding.
 *
 * @param audioSpectrumData The audio spectrum data of the local user. Agora divides the audio
 * frequency into 256 frequency domains, and reports the energy value of each frequency domain
 * through this parameter. The value range of each energy type is [-300, 1] and the unit is dBFS.
 *
 * @return
 * Whether the spectrum data is received:
 * - `YES`: Spectrum data is received.
 * - `NO`: No spectrum data is received.
 */
- (BOOL)onLocalAudioSpectrum:(NSArray<NSNumber *> * _Nullable)audioSpectrumData NS_SWIFT_NAME(onLocalAudioSpectrum(_:));

/**
 * @brief Gets the remote audio spectrum.
 *
 * @details
 * After successfully calling `registerAudioSpectrumDelegate:` to implement the
 * `onRemoteAudioSpectrum:` callback in the `AgoraAudioSpectrumDelegate` and calling
 * `enableAudioSpectrumMonitor:` to enable audio spectrum monitoring, the SDK will trigger the
 * callback as the time interval you set to report the received remote audio data spectrum.
 *
 * @param AudioSpectrumInfo The audio spectrum information of the remote user. See
 * `AgoraAudioSpectrumInfo`. The number of arrays is the number of remote users monitored by the
 * SDK. If the array is null, it means that no audio spectrum of remote users is detected.
 *
 * @return
 * Whether the spectrum data is received:
 * - `YES`: Spectrum data is received.
 * - `NO`: No spectrum data is received.
 */
- (BOOL)onRemoteAudioSpectrum:(NSArray<AgoraAudioSpectrumInfo *> * _Nullable)AudioSpectrumInfo NS_SWIFT_NAME(onRemoteAudioSpectrum(_:));

@end
