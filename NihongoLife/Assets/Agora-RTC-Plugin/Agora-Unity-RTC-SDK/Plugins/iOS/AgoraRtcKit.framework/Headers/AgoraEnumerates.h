//
// AgoraEnumerates.h
// AgoraRtcEngineKit
//
// Copyright (c) 2018 Agora. All rights reserved.
//

#import <Foundation/Foundation.h>

/**
 * Warning code.
 *
 * Warning codes occur when the SDK encounters an error that may be recovered automatically.
 * These are only notifications, and can generally be ignored. For example, when the SDK loses connection to the server, the SDK reports the AgoraWarningCodeOpenChannelTimeout(106) warning and tries to reconnect automatically.
 */
typedef NS_ENUM(NSInteger, AgoraWarningCode) {
    /** 8: The specified view is invalid. Specify a view when using the video call function. */
    AgoraWarningCodeInvalidView = 8,
    /** 16: Failed to initialize the video function, possibly caused by a lack of resources. The users cannot see the video while the voice communication is not affected. */
    AgoraWarningCodeInitVideo = 16,
     /** 20: The request is pending, usually due to some module not being ready, and the SDK postpones processing the request. */
    AgoraWarningCodePending = 20,
    /** 103: No channel resources are available. Maybe because the server cannot allocate any channel resource. */
    AgoraWarningCodeNoAvailableChannel = 103,
    /** 104: A timeout occurs when looking up the channel. When joining a channel, the SDK looks up the specified channel. The warning usually occurs when the network condition is too poor for the SDK to connect to the server. */
    AgoraWarningCodeLookupChannelTimeout = 104,
    /** 105: The server rejects the request to look up the channel. The server cannot process this request or the request is illegal. */
    AgoraWarningCodeLookupChannelRejected = 105,
    /** 106: The server rejects the request to look up the channel. The server cannot process this request or the request is illegal. */
    AgoraWarningCodeOpenChannelTimeout = 106,
    /** 107: The server rejects the request to open the channel. The server cannot process this request or the request is illegal. */
    AgoraWarningCodeOpenChannelRejected = 107,
    /** 111: A timeout occurs when switching to the live video. */
    AgoraWarningCodeSwitchLiveVideoTimeout = 111,
    /** 118: A timeout occurs when setting the client role in the live broadcast profile. */
    AgoraWarningCodeSetClientRoleTimeout = 118,
    /** 119: The client role is unauthorized. */
    AgoraWarningCodeSetClientRoleNotAuthorized = 119,
    /** 121: The ticket to open the channel is invalid. */
    AgoraWarningCodeOpenChannelInvalidTicket = 121,
    /** 122: Try connecting to another server. */
    AgoraWarningCodeOpenChannelTryNextVos = 122,
    /** 701: An error occurs in opening the audio mixing file. */
    AgoraWarningCodeAudioMixingOpenError = 701,
    /** 1014: Audio Device Module: a warning occurs in the playback device. */
    AgoraWarningCodeAdmRuntimePlayoutWarning = 1014,
    /** 1016: Audio Device Module: a warning occurs in the recording device. */
    AgoraWarningCodeAdmRuntimeRecordingWarning = 1016,
    /** 1019: Audio Device Module: no valid audio data is collected. */
    AgoraWarningCodeAdmRecordAudioSilence = 1019,
    /** 1020: Audio Device Module: a playback device fails. */
    AgoraWarningCodeAdmPlaybackMalfunction = 1020,
    /** 1021: Audio Device Module: a recording device fails. */
    AgoraWarningCodeAdmRecordMalfunction = 1021,
    /** 1025: Call is interrupted by system events such as phone call or Siri. */
    AgoraWarningCodeAdmInterruption = 1025,
    /** 1031: Audio Device Module: the recorded audio is too low. */
    AgoraWarningCodeAdmRecordAudioLowlevel = 1031,
    /** 1032: Audio Device Module: the playback audio is too low. */
    AgoraWarningCodeAdmPlayoutAudioLowlevel = 1032,
    /** 1051: Audio Device Module: howling is detected. */
    AgoraWarningCodeApmHowling = 1051,
    /** 1052: Audio Device Module: the device is in the glitch state. */
    AgoraWarningCodeAdmGlitchState = 1052,
    /** 1053: Audio Device Module: the underlying audio settings have changed. */
    AgoraWarningCodeAdmImproperSettings = 1053,
};

/**
 * @brief Error codes.
 *
 * @details
 * An error code indicates that the SDK encountered an unrecoverable error that requires application
 * intervention. For example, an error is returned when the camera fails to open, and the app needs
 * to inform the user that the camera cannot be used.
 *
 */
typedef NS_ENUM(NSInteger, AgoraErrorCode) {
    /**
     * 0: No error.
     */
    AgoraErrorCodeNoError = 0,
    /**
     * 1: General error with no classified reason. Try calling the method again.
     */
    AgoraErrorCodeFailed = 1,
    /**
     * 2: An invalid parameter is used. For example, the specified channel name includes illegal
     * characters. Reset the parameter.
     */
    AgoraErrorCodeInvalidArgument = 2,
    /**
     * 3: The SDK is not ready. Possible reasons include the following:
     * - The initialization of `AgoraRtcEngineKit` fails. Reinitialize the `AgoraRtcEngineKit`.
     * - No user has joined the channel when the method is called. Check the code logic.
     * - The user has not left the channel when the `rate:rating:description:` or
     * `complain:description:` method is called. Check the code logic.
     * - The audio module is disabled.
     * - The program is not complete.
     */
    AgoraErrorCodeNotReady = 3,
    /**
     * 4: The `AgoraRtcEngineKit` does not support the request. Possible reasons include the following:
     * - The built-in encryption mode is incorrect, or the SDK fails to load the external encryption
     * library. Check the encryption mode setting, or reload the external encryption library.
     */
    AgoraErrorCodeNotSupported = 4,
    /**
     * 5: The request is rejected. Possible reasons include the following:
     * - The `AgoraRtcEngineKit` initialization fails. Reinitialize the `AgoraRtcEngineKit`.
     * - The channel name is set as the empty string `""` when joining the channel. Reset the channel
     * name.
     * - When the `joinChannelExByToken:connection:delegate:mediaOptions:joinSuccess:` method is called
     * to join multiple channels, the specified channel name is already in use. Reset the channel name.
     */
    AgoraErrorCodeRefused = 5,
    /**
     * 6: The buffer size is insufficient to store the returned data.
     */
    AgoraErrorCodeBufferTooSmall = 6,
    /**
     * 7: A method is called before the initialization of `AgoraRtcEngineKit`. Ensure that the
     * `AgoraRtcEngineKit` object is initialized before using this method.
     */
    AgoraErrorCodeNotInitialized = 7,
    /** 8: The state is invalid. */
    AgoraErrorCodeInvalidState = 8,
    /**
     * 9: Permission to access is not granted. Check whether your app has access to the audio and video
     * device.
     */
    AgoraErrorCodeNoPermission = 9,
    /**
     * 10: A timeout occurs. Some API calls require the SDK to return the execution result. This error
     * occurs if the SDK takes too long (more than 10 seconds) to return the result.
     */
    AgoraErrorCodeTimedOut = 10,
    /** 11: The request is canceled. This is for internal SDK use only, and is not returned to the app through any method or callback. */
    AgoraErrorCodeCanceled = 11,
    /** 12: The method is called too often. This is for internal SDK use only, and is not returned to the app through any method or callback. */
    AgoraErrorCodeTooOften = 12,
    /** 13: The SDK fails to bind to the network socket. This is for internal SDK use only, and is not returned to the app through any method or callback. */
    AgoraErrorCodeBindSocket = 13,
    /** 14: The network is unavailable. This is for internal SDK use only, and is not returned to the app through any method or callback. */
    AgoraErrorCodeNetDown = 14,
    /** 15: No network buffers are available. This is for internal SDK use only, and is not returned to the app through any method or callback. */
    AgoraErrorCodeNoBufs = 15,
    /**
     * 17: The request to join the channel is rejected. Possible reasons include the following:
     * - The user is already in the channel. Agora recommends that you use the
     * `rtcEngine:connectionChangedToState:reason:` callback to see whether the user is in the channel.
     * Do not call this method to join the channel unless you receive the
     * `AgoraConnectionStateDisconnected` (1) state.
     * - After calling `startEchoTestWithConfig:` for the call test, the user tries to join the channel
     * without calling `stopEchoTest` to end the current test. To join a channel, the call test must be
     * ended by calling `stopEchoTest`.
     */
    AgoraErrorCodeJoinChannelRejected = 17,
    /**
     * 18: Fails to leave the channel. Possible reasons include the following:
     * - The user has left the channel before calling the `leaveChannel:leaveChannelBlock:` method. Stop
     * calling this method to clear this error.
     * - The user calls the `leaveChannel:leaveChannelBlock:` method to leave the channel before joining
     * the channel. In this case, no extra operation is needed.
     */
    AgoraErrorCodeLeaveChannelRejected = 18,
    /**
     * 19: Resources are already in use.
     */
    AgoraErrorCodeAlreadyInUse = 19,
    /**
     * 20: The request is abandoned by the SDK, possibly because the request has been sent too
     * frequently.
     */
    AgoraErrorCodeAbort = 20,
    /**
     * 21: The `AgoraRtcEngineKit` fails to initialize and has crashed because of specific Windows
     * firewall settings.
     */
    AgoraErrorCodeInitNetEngine = 21,
    /**
     * 22: The SDK fails to allocate resources because your app uses too many system resources or system
     * resources are insufficient.
     */
    AgoraErrorCodeResourceLimited = 22,
    /**
     * 101: The specified App ID is invalid. Rejoin the channel with a valid App ID.
     */
    AgoraErrorCodeInvalidAppId = 101,
    /**
     * 102: The specified channel name is invalid. A possible reason is that the parameter's data type
     * is incorrect. Rejoin the channel with a valid channel name.
     */
    AgoraErrorCodeInvalidChannelId = 102,
    /** 103: Fails to get server resources in the specified region. Please try to specify another region when calling sharedEngine.*/
    AgoraErrorCodeNoServerResources = 103,
    /**
     * 109: The current token has expired. Apply for a new token on the server and call `renewToken:`.
     */
    AgoraErrorCodeTokenExpired = 109,
    /**
     * 110: Invalid token. Typical reasons include the following:
     * - App Certificate is enabled in Agora Console, but the code still uses App ID for authentication.
     * Once App Certificate is enabled for a project, you must use token-based authentication.
     * - The `uid` used to generate the token is not the same as the `uid` used to join the channel.
     */
    AgoraErrorCodeInvalidToken = 110,
    /**
     * 111: The network connection is interrupted. The SDK triggers this callback when it loses
     * connection with the server for more than four seconds after the connection is established.
     */
    AgoraErrorCodeConnectionInterrupted = 111,
    /**
     * 112: The network connection is lost. Occurs when the SDK cannot reconnect to Agora's edge server
     * 10 seconds after its connection to the server is interrupted.
     */
    AgoraErrorCodeConnectionLost = 112,
    /**
     * 113: The user is not in the channel when calling the `sendStreamMessage:data:` method.
     */
    AgoraErrorCodeNotInChannel = 113,
    /**
     * 114: The data size exceeds 1 KB when calling the `sendStreamMessage:data:` method.
     */
    AgoraErrorCodeSizeTooLarge = 114,
    /**
     * 115: The data bitrate exceeds 6 KB/s when calling the `sendStreamMessage:data:` method.
     */
    AgoraErrorCodeBitrateLimit = 115,
    /**
     * 116: More than five data streams are created when calling the `createDataStream:config:` method.
     */
    AgoraErrorCodeTooManyDataStreams = 116,
    /** 117: A timeout occurs for the data stream transmission.*/
    AgoraErrorCodeStreamMessageTimeout = 117,
    /** 119: Switching the user role fails. Please try to rejoin the channel.*/
    AgoraErrorCodeSetClientRoleNotAuthorzed = 119,
    /**
     * 120: Media streams decryption fails. The user might use an incorrect password to join the
     * channel. Check the entered password, or tell the user to try rejoining the channel.
     */
    AgoraErrorCodeDecryptionFailed = 120,
    /** 121: The user ID is invalid.*/
    AgoraErrorCodeInvalidUserId = 121,
    /**
     * 122: Data streams decryption fails. The user might use an incorrect password to join the channel.
     * Check the entered password, or tell the user to try rejoining the channel.
     */
    AgoraErrorCodeDatastreamDecryptionFailed = 122,
    /** 123: The app is banned by the server.*/
    AgoraErrorCodeClientIsBannedByServer = 123,
    /**
     * 130: The SDK does not support pushing encrypted streams to CDN.
     */
    AgoraErrorCodeEncryptedStreamNotAllowedPublish = 130,
    /**
     * 134: The user account is invalid, possibly because it contains invalid parameters.
     */
    AgoraErrorCodeInvalidUserAccount = 134,
    /** Stream publishing failed. */
    AgoraErrorCodePublishFailed = 150,
    /** 151: CDN related errors. Remove the original URL address and add a new one by calling the [removePublishStreamUrl]([AgoraRtcEngineKit removePublishStreamUrl:]) and [addPublishStreamUrl]([AgoraRtcEngineKit addPublishStreamUrl:transcodingEnabled:]) methods. */
    AgoraErrorCodePublishStreamCDNError = 151,
    /** 157: The extension library is not integrated, such as the library for enabling deep-learning noise reduction. */
    AgoraErrorCodeModuleNotFound = 157,

    /** 200: Unsupported pcm format. */
    AgoraErrorCodePcmSendFormat = 200,
    /** 201: Buffer overflow, the pcm send rate too quickly. */
    AgoraErrorCodePcmSendBufferOverflow = 201,

    /** 250~270 RDT error code */
    AgoraErrorCodeRdtUserNotExist = 250,
    AgoraErrorCodeRdtUserNotReady = 251,
    AgoraErrorCodeRdtDataBlocked = 252,
    AgoraErrorCodeRdtCmdExceedLimit = 253,
    AgoraErrorCodeRdtDataExceedLimit = 254,
    AgoraErrorCodeRdtEncryption = 255,

    /**
     * 1001: The SDK fails to load the media engine.
     */
    AgoraErrorCodeLoadMediaEngine = 1001,
    /** 1002: Fails to start the call after enabling the media engine. */
    AgoraErrorCodeStartCall = 1002,
    /** 1003: Fails to start the camera. */
    AgoraErrorCodeStartCamera = 1003,
    /** 1004: Fails to start the video rendering module. */
    AgoraErrorCodeStartVideoRender = 1004,
    /**
     * 1005: A general error occurs (no specified reason). Check whether the audio device is already in
     * use by another app, or try rejoining the channel.
     */
    AgoraErrorCodeAdmGeneralError = 1005,
    /** 1006: Audio Device Module: An error occurs in using the Java resources. */
    AgoraErrorCodeAdmJavaResource = 1006,
    /** 1007: Audio Device Module: An error occurs in setting the sampling frequency. */
    AgoraErrorCodeAdmSampleRate = 1007,
    /**
     * 1008: An error occurs when initializing the playback device. Check whether the playback device is
     * already in use by another app, or try rejoining the channel.
     */
    AgoraErrorCodeAdmInitPlayout = 1008,
    /**
     * 1009: An error occurs when starting the playback device. Check the playback device.
     */
    AgoraErrorCodeAdmStartPlayout = 1009,
    /**
     * 1010: An error occurs when stopping the playback device.
     */
    AgoraErrorCodeAdmStopPlayout = 1010,
    /**
     * 1011: An error occurs when initializing the recording device. Check the recording device, or try
     * rejoining the channel.
     */
    AgoraErrorCodeAdmInitRecording = 1011,
    /**
     * 1012: An error occurs when starting the recording device. Check the recording device.
     */
    AgoraErrorCodeAdmStartRecording = 1012,
    /**
     * 1013: An error occurs when stopping the recording device.
     */
    AgoraErrorCodeAdmStopRecording = 1013,
    /** 1015: Audio Device Module: A playback error occurs. Check your playback device, or try rejoining the channel. */
    AgoraErrorCodeAdmRuntimePlayoutError = 1015,
    /** 1017: Audio Device Module: A recording error occurs. */
    AgoraErrorCodeAdmRuntimeRecordingError = 1017,
    /** 1018: Audio Device Module: Fails to record. */
    AgoraErrorCodeAdmRecordAudioFailed = 1018,
    /** 1020: Audio Device Module: Abnormal audio playback frequency. */
    AgoraErrorCodeAdmPlayAbnormalFrequency = 1020,
    /** 1021: Audio Device Module: Abnormal audio recording frequency. */
    AgoraErrorCodeAdmRecordAbnormalFrequency = 1021,
    /** 1022: Audio Device Module: An error occurs in initializing the loopback device. */
    AgoraErrorCodeAdmInitLoopback  = 1022,
    /** 1023: Audio Device Module: An error occurs in starting the loopback device. */
    AgoraErrorCodeAdmStartLoopback = 1023,
    /** 1359: No recording device exists. */
    AgoraErrorCodeAdmNoRecordingDevice = 1359,
    /** 1360: No playback device exists. */
    AgoraErrorCodeAdmNoPlayoutDevice = 1360,
    /**
     * 1501: Permission to access the camera is not granted. Check whether permission to access the
     * camera permission is granted.
     */
    AgoraErrorCodeVdmCameraNotAuthorized = 1501,
    /** 1600: Video Device Module: An unknown error occurs. */
    AgoraErrorCodeVcmUnknownError = 1600,
    /** 1601: Video Device Module: An error occurs in initializing the video encoder. */
    AgoraErrorCodeVcmEncoderInitError = 1601,
    /** 1602: Video Device Module: An error occurs in video encoding. */
    AgoraErrorCodeVcmEncoderEncodeError = 1602,
    /** @deprecated
     * 1603: Video Device Module: An error occurs in setting the video encoder.
     */
    AgoraErrorCodeVcmEncoderSetError __deprecated = 1603,
};

/** @deprecated
 Video Profile */
typedef NS_ENUM(NSInteger, AgoraVideoProfile) {
    /** Invalid profile */
    AgoraVideoProfileInvalid = -1,
    /** 160 x 120  @ 15 fps */
    AgoraVideoProfileLandscape120P = 0,
    /** 120 x 120 @ 15 fps */
    AgoraVideoProfileLandscape120P_3 = 2, // iOS
    /** 320 x 180 @ 15 fps */
    AgoraVideoProfileLandscape180P = 10, // iOS
    /** 180 x 180  @ 15 fps */
    AgoraVideoProfileLandscape180P_3 = 12, // iOS
    /** 240 x 180 @ 15 fps */
    AgoraVideoProfileLandscape180P_4 = 13, // iOS
    /** 320 x 240 @ 15 fps */
    AgoraVideoProfileLandscape240P = 20,
    /** 240 x 240 @ 15 fps */
    AgoraVideoProfileLandscape240P_3 = 22, //iOS
    /** 424 x 240 @ 15 fps */
    AgoraVideoProfileLandscape240P_4 = 23, // iOS
    /** 640 x 360 @ 15 fps */
    AgoraVideoProfileLandscape360P = 30,
    /** 360 x 360 @ 15 fps */
    AgoraVideoProfileLandscape360P_3 = 32, // iOS
    /** 640 x 360 @ 30 fps */
    AgoraVideoProfileLandscape360P_4 = 33,
    /** 360 x 360 @ 30 fps */
    AgoraVideoProfileLandscape360P_6 = 35,
    /** 480 x 360 @ 15 fps */
    AgoraVideoProfileLandscape360P_7 = 36,
    /** 480 x 360 @ 30 fps */
    AgoraVideoProfileLandscape360P_8 = 37,
    /** 640 x 360 @ 15 fps */
    AgoraVideoProfileLandscape360P_9 = 38,
    /** 640 x 360 @ 24 fps */
    AgoraVideoProfileLandscape360P_10 = 39,
    /** 640 x 360 @ 24 fps */
    AgoraVideoProfileLandscape360P_11 = 100,
    /** 640 x 480 @ 15 fps */
    AgoraVideoProfileLandscape480P = 40,
    /** 480 x 480 @ 15 fps */
    AgoraVideoProfileLandscape480P_3 = 42, // iOS
    /** 640 x 480 @ 30 fps */
    AgoraVideoProfileLandscape480P_4 = 43,
     /** 480 x 480 @ 30 fps */
    AgoraVideoProfileLandscape480P_6 = 45,
    /** 848 x 480 @ 15 fps */
    AgoraVideoProfileLandscape480P_8 = 47,
    /** 848 x 480 @ 30 fps */
    AgoraVideoProfileLandscape480P_9 = 48,
    /** 640 x 480 @ 10 fps */
    AgoraVideoProfileLandscape480P_10 = 49,
    /** 1280 x 720 @ 15 fps */
    AgoraVideoProfileLandscape720P = 50,
    /** 1280 x 720 @ 30 fps */
    AgoraVideoProfileLandscape720P_3 = 52,
    /** 960 x 720 @ 15 fps */
    AgoraVideoProfileLandscape720P_5 = 54,
    /** 960 x 720 @ 30 fps */
    AgoraVideoProfileLandscape720P_6 = 55,
    /** 1920 x 1080 @ 15 fps */
    AgoraVideoProfileLandscape1080P = 60,
    /** 1920 x 1080 @ 30 fps */
    AgoraVideoProfileLandscape1080P_3 = 62,
    /** 1920 x 1080 @ 60 fps */
    AgoraVideoProfileLandscape1080P_5 = 64,
    /** 2560 x 1440 @ 30 fps */
    AgoraVideoProfileLandscape1440P = 66,
    /** 2560 x 1440 @ 60 fps */
    AgoraVideoProfileLandscape1440P_2 = 67,
    /** 3840 x 2160 @ 30 fps */
    AgoraVideoProfileLandscape4K = 70,
    /** 3840 x 2160 @ 60 fps */
    AgoraVideoProfileLandscape4K_3 = 72,
    /** 120 x 160 @ 15 fps */
    AgoraVideoProfilePortrait120P = 1000,
    /** 120 x 120 @ 15 fps */
    AgoraVideoProfilePortrait120P_3 = 1002, //iOS
    /** 180 x 320 @ 15 fps */
    AgoraVideoProfilePortrait180P = 1010, // iOS
    /** 180 x 180 @ 15 fps */
    AgoraVideoProfilePortrait180P_3 = 1012, // iOS
    /** 180 x 240 @ 15 fps */
    AgoraVideoProfilePortrait180P_4 = 1013, // iOS
    /** 240 x 320 @ 15 fps */
    AgoraVideoProfilePortrait240P = 1020,
    /** 240 x 240 @ 15 fps */
    AgoraVideoProfilePortrait240P_3 = 1022, // iOS
    /** 240 x 424 @ 15 fps */
    AgoraVideoProfilePortrait240P_4 = 1023,
    /** 360 x 640 @ 15 fps */
    AgoraVideoProfilePortrait360P = 1030, // iOS
    /** 360 x 360 @ 15 fps */
    AgoraVideoProfilePortrait360P_3 = 1032, // iOS
    /** 360 x 640 @ 30 fps */
    AgoraVideoProfilePortrait360P_4 = 1033,
    /** 360 x 360 @ 30 fps */
    AgoraVideoProfilePortrait360P_6 = 1035,
    /** 360 x 480 @ 15 fps */
    AgoraVideoProfilePortrait360P_7 = 1036,
    /** 360 x 480 @ 30 fps */
    AgoraVideoProfilePortrait360P_8 = 1037,
    /** 360 x 640 @ 15 fps */
    AgoraVideoProfilePortrait360P_9 = 1038,
    /** 360 x 640 @ 24 fps */
    AgoraVideoProfilePortrait360P_10 = 1039,
    /** 360 x 640 @ 24 fps */
    AgoraVideoProfilePortrait360P_11 = 1100,
    /** 480 x 640 @ 15 fps */
    AgoraVideoProfilePortrait480P = 1040,
    /** 480 x 480 @ 15 fps */
    AgoraVideoProfilePortrait480P_3 = 1042, // iOS
    /** 480 x 640 @ 30 fps */
    AgoraVideoProfilePortrait480P_4 = 1043,
    /** 480 x 480 @ 30 fps */
    AgoraVideoProfilePortrait480P_6 = 1045,
    /** 480 x 848 @ 15 fps */
    AgoraVideoProfilePortrait480P_8 = 1047,
    /** 480 x 848 @ 30 fps */
    AgoraVideoProfilePortrait480P_9 = 1048,
    /** 480 x 640 @ 10 fps */
    AgoraVideoProfilePortrait480P_10 = 1049,
    /** 720 x 1280 @ 15 fps */
    AgoraVideoProfilePortrait720P = 1050,
    /** 720 x 1280 @ 30 fps */
    AgoraVideoProfilePortrait720P_3 = 1052,
    /** 720 x 960 @ 15 fps */
    AgoraVideoProfilePortrait720P_5 = 1054,
     /** 720 x 960 @ 30 fps */
    AgoraVideoProfilePortrait720P_6 = 1055,
    /** 1080 x 1920 @ 15 fps */
    AgoraVideoProfilePortrait1080P = 1060, // macOS
    /** 1080 x 1920 @ 30 fps */
    AgoraVideoProfilePortrait1080P_3 = 1062, // macOS
    /** 1080 x 1920 @ 60 fps */
    AgoraVideoProfilePortrait1080P_5 = 1064, // macOS
    /** 1440 x 2560 @ 30 fps */
    AgoraVideoProfilePortrait1440P = 1066, // macOS
    /** 1440 x 2560 @ 60 fps */
    AgoraVideoProfilePortrait1440P_2 = 1067, // macOS
    /** 2160 x 3840 @ 30 fps */
    AgoraVideoProfilePortrait4K = 1070, // macOS
    /** 2160 x 3840 @ 60 fps */
    AgoraVideoProfilePortrait4K_3 = 1072, // macOS
    /** Default 640 x 360 @ 15 fps */
    AgoraVideoProfileDEFAULT = AgoraVideoProfileLandscape360P,
};

/**
 * @brief The level of the codec capability.
 */
typedef NS_ENUM(NSInteger, AgoraVideoCodecCapabilityLevel) {
  /**
   * -1: Unsupported video type. Currently, only H.264 and H.265 formats are supported. If the video
   * is in another format, this value will be returned.
   */
  AgoraVideoCodecCapabilityLevelUnspecified = -1,
  /**
   * 5: Supports encoding and decoding videos up to 1080p and 30 fps.
   */
  AgoraVideoCodecCapabilityLevelBasicSupport = 5,
  /**
   * 10: Supports encoding and decoding videos up to1080p and 30 fps.
   */
  AgoraVideoCodecCapabilityLevel1080p30fps = 10,
  /**
   * 20: Support encoding and decoding videos up to 1080p and 60 fps.
   */
  AgoraVideoCodecCapabilityLevel1080p60fps = 20,
  /**
   * 30: Support encoding and decoding videos up to 4K and 30 fps.
   */
  AgoraVideoCodecCapabilityLevel4k60fps = 30,
};

/**
 * @brief Video codec types.
 */
typedef NS_ENUM(NSInteger, AgoraVideoCodecType) {
  /**
   * 0: (Default) Unspecified codec format. The SDK automatically matches the appropriate codec format
   * based on the current video stream's resolution and device performance.
   */
  AgoraVideoCodecTypeNone = 0,
  /**
   * 1: Standard VP8.
   */
  AgoraVideoCodecTypeVP8 = 1,
  /**
   * 2: Standard H.264.
   */
  AgoraVideoCodecTypeH264 = 2,
  /**
   * 3: Standard H.265.
   */
  AgoraVideoCodecTypeH265 = 3,
  /**
   * 6: Generic. This type is used for transmitting raw video data, such as encrypted video frames.
   * The SDK returns this type of video frames in callbacks, and you need to decode and render the
   * frames yourself.
   */
  AgoraVideoCodecTypeGeneric = 6,
  /**
   * @technical preview
   */
  AgoraVideoCodecTypeAV1 = 12,
  AgoraVideoCodecTypeVP9 = 13,
  /**
   * 20: Generic JPEG. This type consumes minimum computing resources and applies to IoT devices.
   */
  AgoraVideoCodecTypeGenericJpeg = 20,
};

/**
 * @brief The local video encoding acceleration type.
 */
typedef NS_ENUM(NSInteger, AgoraVideoHwEncoderAccelerating) {
  /**
   * 0: Software encoding is applied without acceleration.
   */
  AgoraVideoHwEncoderSoftware = 0,
  /**
   * 1: Hardware encoding is applied for acceleration.
   */
  AgoraVideoHwEncoderHardware = 1,
};

typedef NS_ENUM(NSInteger, AgoraVideoFrameOrientation) {
  AgoraVideoFrameOrientation0 = 0,
  AgoraVideoFrameOrientation90 = 90,
  AgoraVideoFrameOrientation180 = 180,
  AgoraVideoFrameOrientation270 = 270,
};

typedef NS_ENUM(NSInteger, AgoraVideoEncodedFrameDepth) {
  AgoraVideoEncodedFrameOfSDR = 8,
  AgoraVideoEncodedFrameOfHDR = 10,
};

/**
 * @brief The codec type of the output video.
 *
 * @since v3.2.0
 */
typedef NS_ENUM(NSInteger, AgoraVideoCodecTypeForStream) {
  /**
   * 1: (Default) H.264.
   */
  AgoraVideoCodecTypeH264ForStream = 1,
  /**
   * 2: H.265.
   */
  AgoraVideoCodecTypeH265ForStream = 2,
};

/**
 * @brief The video frame rate.
 */
typedef NS_ENUM(NSInteger, AgoraVideoFrameRate) {
    /**
     * 1: 1 fps.
     */
    AgoraVideoFrameRateFps1 = 1,
    /**
     * 7: 7 fps.
     */
    AgoraVideoFrameRateFps7 = 7,
    /**
     * 10: 10 fps.
     */
    AgoraVideoFrameRateFps10 = 10,
    /**
     * 15: 15 fps.
     */
    AgoraVideoFrameRateFps15 = 15,
    /**
     * 24: 24 fps.
     */
    AgoraVideoFrameRateFps24 = 24,
    /**
     * 30: 30 fps.
     */
    AgoraVideoFrameRateFps30 = 30,
    /**
     * 60: (macOS only) 60 fps.
     */
    AgoraVideoFrameRateFps60 = 60,
};

/**
 * @brief Video output orientation mode.
 */
typedef NS_ENUM(NSInteger, AgoraVideoOutputOrientationMode) {
    /**
     * 0: (Default) The output video always follows the orientation of the captured video. The receiver
     * takes the rotational information passed on from the video encoder. This mode applies to scenarios
     * where video orientation can be adjusted on the receiver.
     * - If the captured video is in landscape mode, the output video is in landscape mode.
     * - If the captured video is in portrait mode, the output video is in portrait mode.
     */
    AgoraVideoOutputOrientationModeAdaptative = 0,
    /**
     * 1: In this mode, the SDK always outputs videos in landscape (horizontal) mode. If the captured
     * video is in portrait mode, the video encoder crops it to fit the output. Applies to situations
     * where the receiving end cannot process the rotational information. For example, CDN live
     * streaming.
     */
    AgoraVideoOutputOrientationModeFixedLandscape = 1,
    /**
     * 2: In this mode, the SDK always outputs video in portrait (portrait) mode. If the captured video
     * is in landscape mode, the video encoder crops it to fit the output. Applies to situations where
     * the receiving end cannot process the rotational information. For example, CDN live streaming.
     */
    AgoraVideoOutputOrientationModeFixedPortrait = 2,
};

/**
 * @brief The channel profile.
 */
typedef NS_ENUM(NSInteger, AgoraChannelProfile) {
    /**
     * 0: Communication. Use this profile when there are only two users in the channel.
     */
    AgoraChannelProfileCommunication = 0,
    /**
     * 1: Live streaming. Use this profile when there are more than two users in the channel.
     */
    AgoraChannelProfileLiveBroadcasting = 1,
    /**
     * 2: Gaming.
     * @deprecated
     */
    AgoraChannelProfileGame __deprecated = 2,
    /**
     * Cloud gaming. The scenario is optimized for latency. Use this profile if the use case requires
     * frequent interactions between users.
     * @deprecated
     */
    AgoraChannelProfileCloudGaming __deprecated = 3,

    /**
     * @deprecated
     * 4：1-to-1 communication.
     */
    AgoraChannelProfileCommunication_1v1 __deprecated = 4,
};

/**
 * @brief The user role in the interactive live streaming.
 */
typedef NS_ENUM(NSInteger, AgoraClientRole) {
    /**
     * 1: Host. A host can both send and receive streams.
     */
    AgoraClientRoleBroadcaster = 1,
    /**
     * 2: (Default) Audience. An audience member can only receive streams.
     */
    AgoraClientRoleAudience = 2,
};

/**
 * @brief The latency level of an audience member in interactive live streaming. This enum takes
 * effect only when the user role is set to AgoraClientRoleAudience .
 */
typedef NS_ENUM(NSInteger, AgoraAudienceLatencyLevelType) {
    /**
     * 1: Low latency.
     */
    AgoraAudienceLatencyLevelLowLatency = 1,
    /**
     * 2: (Default) Ultra low latency.
     */
    AgoraAudienceLatencyLevelUltraLowLatency = 2,
};

/**
 * @brief The brightness level of the video image captured by the local camera.
 */
typedef NS_ENUM(NSInteger, AgoraCaptureBrightnessLevelType) {
  /**
   * -1: The SDK does not detect the brightness level of the video image. Wait a few seconds to get
   * the brightness level from `captureBrightnessLevel` in the next callback.
   */
  AgoraCaptureBrightnessLevelInvalid = -1,
  /**
   * 0: The brightness level of the video image is normal.
   */
  AgoraCaptureBrightnessLevelNormal = 0,
  /**
   * 1: The brightness level of the video image is too bright.
   */
  AgoraCaptureBrightnessLevelBright = 1,
  /**
   * 2: The brightness level of the video image is too dark.
   */
  AgoraCaptureBrightnessLevelDark = 2,
} NS_SWIFT_NAME(AgoraCaptureBrightnessLevelType);

/** Media type */
typedef NS_ENUM(NSInteger, AgoraMediaType) {
    /** No audio and video */
    AgoraMediaTypeNone = 0,
    /** Audio only */
    AgoraMediaTypeAudioOnly = 1,
    /** Video only */
    AgoraMediaTypeVideoOnly = 2,
    /** Audio and video */
    AgoraMediaTypeAudioAndVideo = 3,
};


/**
 * @brief The built-in encryption mode.
 *
 * @details
 * Agora recommends using AgoraEncryptionModeAES128GCM2 or AgoraEncryptionModeAES256GCM2 encrypted
 * mode. These two modes support the use of salt for higher security.
 *
 */
typedef NS_ENUM(NSInteger, AgoraEncryptionMode) {
    /**
     * 1: 128-bit AES encryption, XTS mode.
     */
    AgoraEncryptionModeAES128XTS = 1,
    /**
     * 2: 128-bit AES encryption, ECB mode.
     */
    AgoraEncryptionModeAES128ECB = 2,
    /**
     * 3: 256-bit AES encryption, XTS mode.
     */
    AgoraEncryptionModeAES256XTS = 3,
    /**
     * 4: 128-bit SM4 encryption, ECB mode.
     */
    AgoraEncryptionModeSM4128ECB = 4,
    /**
     * 5: 128-bit AES encryption, GCM mode.
     */
    AgoraEncryptionModeAES128GCM = 5,
    /**
     * 6: 256-bit AES encryption, GCM mode.
     */
    AgoraEncryptionModeAES256GCM = 6,
    /**
     * 7: (Default) 128-bit AES encryption, GCM mode. This encryption mode requires the setting of salt
     * (`encryptionKdfSalt`).
     */
    AgoraEncryptionModeAES128GCM2 = 7,
    /**
     * 8: 256-bit AES encryption, GCM mode. This encryption mode requires the setting of salt
     * (`encryptionKdfSalt`).
     */
    AgoraEncryptionModeAES256GCM2 = 8,
    /**
     * Enumerator boundary.
     */
    AgoraEncryptionModeEnd,
};

/**
 * @brief The cloud proxy type.
 */
typedef NS_ENUM(NSUInteger, AgoraCloudProxyType) {
  /**
   * 0: The automatic mode. The SDK has this mode enabled by default. In this mode, the SDK attempts a
   * direct connection to SD-RTN™ and automatically switches to TCP/TLS 443 if the attempt fails.
   */
  AgoraNoneProxy = 0,
  /**
   * 1: The cloud proxy for the UDP protocol, that is, the Force UDP cloud proxy mode. In this mode,
   * the SDK always transmits data over UDP.
   */
  AgoraUdpProxy = 1,
  /**
   * 2: The cloud proxy for the TCP (encryption) protocol, that is, the Force TCP cloud proxy mode. In
   * this mode, the SDK always transmits data over TCP/TLS 443.
   */
  AgoraTcpProxy = 2,
} NS_SWIFT_NAME(AgoraCloudProxyType);

/**
 * @brief Reasons for a user being offline.
 */
typedef NS_ENUM(NSUInteger, AgoraUserOfflineReason) {
    /**
     * 0: The user quits the call.
     */
    AgoraUserOfflineReasonQuit = 0,
    /**
     * 1: The SDK times out and the user drops offline because no data packet is received within a
     * certain period of time.
     * @note If the user quits the call and the message is not passed to the SDK (due to an unreliable
     * channel), the SDK assumes the user dropped offline.
     */
    AgoraUserOfflineReasonDropped = 1,
    /**
     * 2: The user switches the client role from the host to the audience.
     */
    AgoraUserOfflineReasonBecomeAudience = 2,
};

/** Status of importing an external video stream in a live broadcast */
typedef NS_ENUM(NSUInteger, AgoraInjectStreamStatus) {
    /** The external video stream imported successfully. */
    AgoraInjectStreamStatusStartSuccess = 0,
    /** The external video stream already exists. */
    AgoraInjectStreamStatusStartAlreadyExists = 1,
    /** The external video stream import is unauthorized */
    AgoraInjectStreamStatusStartUnauthorized = 2,
    /** Import external video stream timeout. */
    AgoraInjectStreamStatusStartTimedout = 3,
    /** The external video stream failed to import. */
    AgoraInjectStreamStatusStartFailed = 4,
    /** The xternal video stream imports successfully. */
    AgoraInjectStreamStatusStopSuccess = 5,
    /** No external video stream is found. */
    AgoraInjectStreamStatusStopNotFound = 6,
    /** The external video stream is stopped from being unauthorized. */
    AgoraInjectStreamStatusStopUnauthorized = 7,
    /** Importing the external video stream timeout. */
    AgoraInjectStreamStatusStopTimedout = 8,
    /** Importing the external video stream failed. */
    AgoraInjectStreamStatusStopFailed = 9,
    /** The external video stream is broken. */
    AgoraInjectStreamStatusBroken = 10,
};

/**
 * @brief The output log level of the SDK.
 */
typedef NS_ENUM(NSUInteger, AgoraLogFilter) {
    /**
     * 0: Do not output any log information.
     */
    AgoraLogFilterOff = 0,
    /**
     * 0x080f: Output all log information. Set your log filter to this level if you want to get the most
     * complete log file.
     */
    AgoraLogFilterDebug = 0x080f,
    /**
     * 0x000f: Output `AgoraLogFilterCritical`, `AgoraLogFilterError`, `AgoraLogFilterWarning`, and
     * `AgoraLogFilterInfo` level log information. We recommend setting your log filter to this level.
     */
    AgoraLogFilterInfo = 0x000f,
    /**
     * 0x000e: Output `AgoraLogFilterCritical`, `AgoraLogFilterError`, and `AgoraLogFilterWarning` level
     * log information.
     */
    AgoraLogFilterWarning = 0x000e,
    /**
     * 0x000c: Output `AgoraLogFilterCritical` and `AgoraLogFilterError` level log information.
     */
    AgoraLogFilterError = 0x000c,
    /**
     * 0x0008: Output `AgoraLogFilterCritical` level log information.
     */
    AgoraLogFilterCritical = 0x0008,
};

/**
 * @brief Recording quality.
 */
typedef NS_ENUM(NSInteger, AgoraAudioRecordingQuality) {
    /**
     * 0: Low quality. The sample rate is 32 kHz, and the file size is around 1.2 MB after 10 minutes of
     * recording.
     */
    AgoraAudioRecordingQualityLow = 0,
    /**
     * 1: Medium quality. The sample rate is 32 kHz, and the file size is around 2 MB after 10 minutes
     * of recording.
     */
    AgoraAudioRecordingQualityMedium = 1,
    /**
     * 2: High quality. The sample rate is 32 kHz, and the file size is around 3.75 MB after 10 minutes
     * of recording.
     */
    AgoraAudioRecordingQualityHigh = 2,
    /**
     * 3: Ultra high quality. The sample rate is 32 kHz, and the file size is around 7.5 MB after 10
     * minutes of recording.
     */
    AgoraAudioRecordingQualityUltraHigh = 3,
};

/**
 * @brief Recording content. Set in `startAudioRecordingWithConfig:`.
 */
typedef NS_ENUM(NSInteger, AgoraAudioFileRecordingType) {
    /**
     * 1: Only records the audio of the local user.
     */
    AgoraAudioFileRecordingTypeMic = 1,
    /**
     * 2: Only records the audio of all remote users.
     */
    AgoraAudioFileRecordingTypePlayback = 2,
    /**
     * 3: Records the mixed audio of the local and all remote users.
     */
    AgoraAudioFileRecordingTypeMixed = 3,
};

/**
 * @brief Audio frame observer position.
 */
typedef NS_ENUM(NSInteger, AgoraAudioEncodedFrameDelegatePosition) {
    /**
     * 1: Only records the audio of the local user.
     */
    AgoraAudioEncodedFrameObserverPositionRecord = 1,
    /**
     * 2: Only records the audio of all remote users.
     */
    AgoraAudioEncodedFrameObserverPositionPlayback = 2,
    /**
     * 3: Records the mixed audio of the local and all remote users.
     */
    AgoraAudioEncodedFrameObserverPositionMixed = 3,
};

/**
 * @brief The codec type of audio.
 */
typedef NS_ENUM(NSInteger, AgoraAudioCodecType) {
  /**
   * 1: OPUS.
   */
  AgoraAudioCodecTypeOPUS = 1,
  /**
   * 8: LC-AAC.
   */
  AgoraAudioCodecTypeAACLC = 8,
  /**
   * 9: HE-AAC.
   */
  AgoraAudioCodecTypeHEAAC = 9,
  /**
   * 11: HE-AAC v2.
   */
  AgoraAudioCodecTypeHEAAC2 = 11,
};
/**
 * @brief Audio encoding type.
 */
typedef NS_ENUM(NSInteger, AgoraAudioEncodingType) {
    /**
     * 0x010101: AAC encoding format, 16000 Hz sampling rate, bass quality. A file with an audio
     * duration of 10 minutes is approximately 1.2 MB after encoding.
     */
    AgoraAudioEncodingTypeAAC16000Low = 0x010101,
    /**
     * 0x010102: AAC encoding format, 16000 Hz sampling rate, medium sound quality. A file with an audio
     * duration of 10 minutes is approximately 2 MB after encoding.
     */
    AgoraAudioEncodingTypeAAC16000Medium = 0x010102,
    /**
     * 0x010201: AAC encoding format, 32000 Hz sampling rate, bass quality. A file with an audio
     * duration of 10 minutes is approximately 1.2 MB after encoding.
     */
    AgoraAudioEncodingTypeAAC32000Low = 0x010201,
    /**
     * 0x010202: AAC encoding format, 32000 Hz sampling rate, medium sound quality. A file with an audio
     * duration of 10 minutes is approximately 2 MB after encoding.
     */
    AgoraAudioEncodingTypeAAC32000Medium = 0x010202,
    /**
     * 0x010203: AAC encoding format, 32000 Hz sampling rate, high sound quality. A file with an audio
     * duration of 10 minutes is approximately 3.5 MB after encoding.
     */
    AgoraAudioEncodingTypeAAC32000High = 0x010203,
    /**
     * 0x010302: AAC encoding format, 48000 Hz sampling rate, medium sound quality. A file with an audio
     * duration of 10 minutes is approximately 2 MB after encoding.
     */
    AgoraAudioEncodingTypeAAC48000Medium = 0x010302,
    /**
     * 0x010303: AAC encoding format, 48000 Hz sampling rate, high sound quality. A file with an audio
     * duration of 10 minutes is approximately 3.5 MB after encoding.
     */
    AgoraAudioEncodingTypeAAC48000High = 0x010303,
    /**
     * 0x020101: OPUS encoding format, 16000 Hz sampling rate, bass quality. A file with an audio
     * duration of 10 minutes is approximately 2 MB after encoding.
     */
    AgoraAudioEncodingTypeOPUS16000Low = 0x020101,
    /**
     * 0x020102: OPUS encoding format, 16000 Hz sampling rate, medium sound quality. A file with an
     * audio duration of 10 minutes is approximately 2 MB after encoding.
     */
    AgoraAudioEncodingTypeOPUS6000Medium = 0x020102,
    /**
     * 0x020302: OPUS encoding format, 48000 Hz sampling rate, medium sound quality. A file with an
     * audio duration of 10 minutes is approximately 2 MB after encoding.
     */
    AgoraAudioEncodingTypeOPUS48000Medium = 0x020302,
    /**
     * 0x020303: OPUS encoding format, 48000 Hz sampling rate, high sound quality. A file with an audio
     * duration of 10 minutes is approximately 3.5 MB after encoding.
     */
    AgoraAudioEncodingTypeOPUS48000High = 0x020303,
};

/**
 * @brief Lifecycle of the CDN live video stream.
 */
typedef NS_ENUM(NSInteger, AgoraRtmpStreamLifeCycle) {
    /**
     * Bind to the channel lifecycle. If all hosts leave the channel, the CDN live streaming stops after
     * 30 seconds.
     */
    AgoraRtmpStreamLifeCycleBindToChannel = 1,
    /**
     * Bind to the owner of the RTMP stream. If the owner leaves the channel, the CDN live streaming
     * stops immediately.
     */
    AgoraRtmpStreamLifeCycleBindToOwner = 2,
};

/**
 * @brief Network quality types.
 */
typedef NS_ENUM(NSUInteger, AgoraNetworkQuality) {
    /**
     * 0: The network quality is unknown.
     */
    AgoraNetworkQualityUnknown = 0,
    /**
     * 1: The network quality is excellent.
     */
    AgoraNetworkQualityExcellent = 1,
    /**
     * 2: The network quality is quite good, but the bitrate may be slightly lower than excellent.
     */
    AgoraNetworkQualityGood = 2,
    /**
     * 3: Users can feel the communication is slightly impaired.
     */
    AgoraNetworkQualityPoor = 3,
    /**
     * 4: Users cannot communicate smoothly.
     */
    AgoraNetworkQualityBad = 4,
    /**
     * 5: The quality is so bad that users can barely communicate.
     */
    AgoraNetworkQualityVBad = 5,
    /**
     * 6: The network is down and users cannot communicate at all.
     */
    AgoraNetworkQualityDown = 6,
    /** Users cannot detect the network quality. (Not in use.) */
    AgoraNetworkQualityUnsupported = 7,
    /**
     * 8: The last-mile network probe test is in progress.
     */
    AgoraNetworkQualityDetecting = 8,
};

/**
 * @brief The Quality of Experience (QoE) of the local user when receiving a remote audio stream.
 */
typedef NS_ENUM(NSUInteger, AgoraExperienceQuality) {
  /**
   * 0: The QoE of the local user is good.
   */
  AgoraExperienceQualityGood = 0,
  /**
   * 1: The QoE of the local user is poor.
   */
  AgoraExperienceQualityBad = 1,
};

/**
 * @brief Reasons why the QoE of the local user when receiving a remote audio stream is poor.
 */
typedef NS_ENUM(NSUInteger, AgoraExperiencePoorReason) {
  /**
   * 0: No reason, indicating a good QoE of the local user.
   */
  AgoraExperiencePoorReasonNone = 0,
  /**
   * 1: The remote user's network quality is poor.
   */
  AgoraExperiencePoorReasonRemoteNetworkQualityPoor = 1,
  /**
   * 2: The local user's network quality is poor.
   */
  AgoraExperiencePoorReasonLocalNetworkQualityPoor = 2,
  /**
   * 4: The local user's Wi-Fi or mobile network signal is weak.
   */
  AgoraExperiencePoorReasonWirelessSignalPoor = 4,
  /**
   * 8: The local user enables both Wi-Fi and bluetooth, and their signals interfere with each other.
   * As a result, audio transmission quality is undermined.
   */
  AgoraExperiencePoorReasonWifiBluetoothCoexist = 8,
};

/**
 * @brief The status of the last-mile probe test.
 */
typedef NS_ENUM(NSUInteger, AgoraLastmileProbeResultState) {
  /**
   * 1: The last-mile network probe test is complete.
   */
  AgoraLastmileProbeResultComplete = 1,
  /**
   * 2: The last-mile network probe test is incomplete because the bandwidth estimation is not
   * available due to limited test resources. One possible reason is that testing resources are
   * temporarily limited.
   */
  AgoraLastmileProbeResultIncompleteNoBwe = 2,
  /**
   * 3: The last-mile network probe test is not carried out. Probably due to poor network conditions.
   */
  AgoraLastmileProbeResultUnavailable = 3,
};

typedef NS_ENUM(NSInteger, AgoraUploadErrorReason) {
    AgoraUploadErrorReasonSuccess = 0,
    AgoraUploadErrorReasonNetError = 1,
    AgoraUploadErrorReasonServerError = 2,
};

/**
 * @brief The type of video streams.
 */
typedef NS_ENUM(NSInteger, AgoraVideoStreamType) {
    /**
     * 0: High-quality video stream, that is, a video stream with the highest resolution and bitrate.
     */
    AgoraVideoStreamTypeHigh = 0,
    /**
     * 1: Low-quality video stream, that is, a video stream with the lowest resolution and bitrate.
     */
    AgoraVideoStreamTypeLow = 1,
    /**
     * 4. Video stream layer 1. The resolution of this quality level is only lower than that of
     * AgoraVideoStreamTypeHigh.
     */
    AgoraVideoStreamTypeLayer1 = 4,
    /**
     * 5: Video stream layer 2. The resolution of this quality level is only lower than that of
     * AgoraVideoStreamTypeLayer1.
     */
    AgoraVideoStreamTypeLayer2 = 5,
    /**
     * 6: Video stream layer 3. The resolution of this quality level is only lower than that of
     * AgoraVideoStreamTypeLayer2.
     */
    AgoraVideoStreamTypeLayer3 = 6,
    /**
     * 7: Video stream layer 4. The resolution of this quality level is only lower than that of
     * AgoraVideoStreamTypeLayer3.
     */
    AgoraVideoStreamTypeLayer4 = 7,
    /**
     * 8: Video stream layer 5. The resolution of this quality level is only lower than that of
     * AgoraVideoStreamTypeLayer4.
     */
    AgoraVideoStreamTypeLayer5 = 8,
    /**
     * 9: Video stream layer 6. The resolution of this quality level is only lower than that of
     * AgoraVideoStreamTypeLayer5.
     */
    AgoraVideoStreamTypeLayer6 = 9,
};

/**
 * @brief Quality change of the local video in terms of target frame rate and target bit rate since
 * last count.
 */
typedef NS_ENUM(NSInteger, AgoraVideoQualityAdaptIndication) {
  /**
   * 0: The local video quality stays the same.
   */
  AgoraVideoQualityAdaptNone = 0,
  /**
   * 1: The local video quality improves because the network bandwidth increases.
   */
  AgoraVideoQualityAdaptUpBandwidth = 1,
  /**
   * 2: The local video quality deteriorates because the network bandwidth decreases.
   */
  AgoraVideoQualityAdaptDownBandwidth = 2,
} NS_SWIFT_NAME(AgoraVideoQualityAdaptIndication);

/**
 * @brief Video display modes.
 */
typedef NS_ENUM(NSUInteger, AgoraVideoRenderMode) {
    /**
     * 1: Hidden mode. The priority is to fill the window. Any excess video that does not match the
     * window size will be cropped.
     */
    AgoraVideoRenderModeHidden = 1,

    /**
     * 2: Fit mode. The priority is to ensure that all video content is displayed. Any areas of the
     * window that are not filled due to the mismatch between video size and window size will be filled
     * with black.
     */
    AgoraVideoRenderModeFit = 2,

    /** @deprecated
     Adaptive(3)：This mode is obsolete.
     */
    AgoraVideoRenderModeAdaptive __deprecated_enum_msg("AgoraVideoRenderModeAdaptive is deprecated.") = 3,
};

/**
 * @brief Video codec profile types.
 */
typedef NS_ENUM(NSInteger, AgoraVideoCodecProfileType) {
    /**
     * 66: Baseline video codec profile; generally used for video calls on mobile phones.
     */
    AgoraVideoCodecProfileTypeBaseLine = 66,
    /**
     * 77: Main video codec profile; generally used in mainstream electronics such as MP4 players,
     * portable video players, PSP, and iPads.
     */
    AgoraVideoCodecProfileTypeMain = 77,
    /**
     * 100: (Default) High video codec profile; generally used in high-resolution live streaming or
     * television.
     */
    AgoraVideoCodecProfileTypeHigh = 100
};

/**
 * @brief Video mirror mode.
 */
typedef NS_ENUM(NSUInteger, AgoraVideoMirrorMode) {
    /**
     * 0: The SDK determines the mirror mode.
     * - For the mirror mode of the local video view: If you use a front camera, the SDK enables the
     * mirror mode by default; if you use a rear camera, the SDK disables the mirror mode by default.
     * - For the remote user: The mirror mode is disabled by default.
     */
    AgoraVideoMirrorModeAuto = 0,
    /**
     * 1: Enable mirror mode.
     */
    AgoraVideoMirrorModeEnabled = 1,
    /**
     * 2: Disable mirror mode.
     */
    AgoraVideoMirrorModeDisabled = 2,
};

/**
 * @brief Local video state types.
 */
typedef NS_ENUM(NSUInteger, AgoraVideoLocalState) {
  /**
   * 0: The local video is in the initial state.
   */
  AgoraVideoLocalStateStopped = 0,
  /**
   * 1: The local video capturing device starts successfully.
   */
  AgoraVideoLocalStateCapturing = 1,
  /**
   * 2: The first video frame is successfully encoded.
   */
  AgoraVideoLocalStateEncoding = 2,
  /**
   * 3: Fails to start the local video.
   */
  AgoraVideoLocalStateFailed = 3
};

/**
 * @brief Reasons for local video state changes.
 */
typedef NS_ENUM(NSUInteger, AgoraLocalVideoStreamReason) {
  /**
   * 0: The local video is normal.
   */
  AgoraLocalVideoStreamReasonOK = 0,
  /**
   * 1: No specified reason for the local video failure.
   */
  AgoraLocalVideoStreamReasonFailure = 1,
  /**
   * 2: No permission to use the local video capturing device. Prompt the user to grant permissions
   * and rejoin the channel.
   */
  AgoraLocalVideoStreamReasonDeviceNoPermission = 2,
  /**
   * 3: The local video capturing device is in use. Prompt the user to check if the camera is being
   * used by another app, or try to rejoin the channel.
   */
  AgoraLocalVideoStreamReasonDeviceBusy = 3,
  /**
   * 4: The local video capture fails. Prompt the user to check whether the video capture device is
   * working properly, whether the camera is used by another app, or try to rejoin the channel.
   */
  AgoraLocalVideoStreamReasonCaptureFailure = 4,
  /**
   * 5: The local video encoding fails.
   */
  AgoraLocalVideoStreamReasonCodecNotSupport = 5,
  /**
   * 6: (iOS only) The app is in the background. Prompt the user that video capture cannot be
   * performed normally
   * when the app is in the background.
   */
  AgoraLocalVideoStreamReasonCaptureInBackGround = 6,
  /**
   * 7: (iOS only) The current app window is running in Slide Over, Split View, or Picture in Picture
   * mode, and
   * another app is occupying the camera. Prompt the user that the app cannot capture video properly
   * when it is running in Slide Over, Split View, or Picture in Picture mode and another app is
   * occupying the camera.
   */
  AgoraLocalVideoStreamReasonCaptureMultipleForegroundApps = 7,
  /** 8: The SDK cannot find the local video capture device. Check whether the camera is connected to the device properly, check whether the camera is working properly, or try to rejoin the channel.
   */
  AgoraLocalVideoStreamReasonCaptureNoDeviceFound = 8,
  /**
   * 9: (macOS only) The video capture device currently in use is disconnected (such as being
   * unplugged).
   */
  AgoraLocalVideoStreamReasonCaptureDeviceDisconnected = 9,
  /**
   * 10: (macOS only) The SDK cannot find the video device in the video device list. Check whether the
   * ID of the
   * video device is valid.
   */
  AgoraLocalVideoStreamReasonCaptureDeviceInvalidId = 10,
  /**
   * 11: (macOS only) The shared window is minimized when you call the
   * `startScreenCaptureByWindowId:regionRect:captureParams:` method to share a window. The SDK cannot
   * share a minimized window. Please prompt the user to unminimize the shared window.
   */
  AgoraLocalVideoStreamReasonScreenCaptureWindowMinimized = 11,
  /**
   * 12:  (macOS only) The error code indicates that a window shared by the window ID has been closed
   * or a
   * full-screen window shared by the window ID has exited full-screen mode. After exiting full-screen
   * mode, remote users cannot see the shared window. To prevent remote users from seeing a black
   * screen, Agora recommends that you immediately stop screen sharing.
   * Common scenarios reporting this error code:
   * - The local user closes the shared window.
   * - The local user shows some slides in full-screen mode first, and then shares the windows of the
   * slides. After the user exits full-screen mode, the SDK reports this error code.
   * - The local user watches a web video or reads a web document in full-screen mode first, and then
   * shares the window of the web video or document. After the user exits full-screen mode, the SDK
   * reports this error code.
   */
  AgoraLocalVideoStreamReasonScreenCaptureWindowClosed = 12,

  /** 22: No permision to capture screen. */
  AgoraLocalVideoStreamReasonScreenCaptureNoPermission = 22,

  /**
   * 27: (macOS only) The window for screen capture has been restored from the minimized state.
   */
  AgoraLocalVideoStreamReasonScreenCaptureRecoverFromMinimized = 27,

  /**
   * 30: (macOS only) The displayer used for screen capture is disconnected. The current screen
   * sharing has been
   * paused. Prompt the user to restart the screen sharing.
   */
  AgoraLocalVideoStreamReasonScreenCaptureDisplayDisconnected = 30,
};

/**
 * @brief The state of the remote video stream.
 */
typedef NS_ENUM(NSUInteger, AgoraVideoRemoteState) {
    /**
     * 0: The remote video is in the initial state. The SDK reports this state in the case of
     * `AgoraVideoRemoteReasonLocalMuted`, `AgoraVideoRemoteReasonRemoteMuted`, or
     * `AgoraVideoRemoteReasonRemoteOffline`.
     */
    AgoraVideoRemoteStateStopped = 0,
    /**
     * 1: The first remote video packet is received.
     */
    AgoraVideoRemoteStateStarting = 1,
    /**
     * 2: The remote video stream is decoded and plays normally. The SDK reports this state in the case
     * of `AgoraVideoRemoteReasonRecovery`, `AgoraVideoRemoteReasonLocalUnmuted`,
     * `AgoraVideoRemoteReasonRemoteUnmuted`, or `AgoraVideoRemoteReasonAudioFallbackRecovery`.
     */
    AgoraVideoRemoteStateDecoding = 2,
    /**
     * 3: The remote video is frozen. The SDK reports this state in the case of
     * `AgoraVideoRemoteReasonCongestion` or `AgoraVideoRemoteReasonAudioFallback`.
     */
    AgoraVideoRemoteStateFrozen = 3,
    /**
     * 4: The remote video fails to start. The SDK reports this state in the case of
     * `AgoraVideoRemoteReasonInternal`.
     */
    AgoraVideoRemoteStateFailed = 4,
};

/**
 * @brief The reason for the remote video state change.
 */
typedef NS_ENUM(NSUInteger, AgoraVideoRemoteReason) {
      /**
       * 0: The SDK reports this reason when the video state changes.
       */
      AgoraVideoRemoteReasonInternal = 0,

      /**
       * 1: Network congestion.
       */
      AgoraVideoRemoteReasonCongestion = 1,

      /**
       * 2: Network is recovered.
       */
      AgoraVideoRemoteReasonRecovery = 2,

      /**
       * 3: The local user stops receiving the remote video stream or disables the video module.
       */
      AgoraVideoRemoteReasonLocalMuted = 3,

      /**
       * 4: The local user resumes receiving the remote video stream or enables the video module.
       */
      AgoraVideoRemoteReasonLocalUnmuted = 4,

      /**
       * 5: The remote user stops sending the video stream or disables the video module.
       */
      AgoraVideoRemoteReasonRemoteMuted = 5,

      /**
       * 6: The remote user resumes sending the video stream or enables the video module.
       */
      AgoraVideoRemoteReasonRemoteUnmuted = 6,

      /**
       * 7: The remote user leaves the channel.
       */
      AgoraVideoRemoteReasonRemoteOffline = 7,

      /**
       * 8: The remote audio-and-video stream falls back to the audio-only stream due to poor network
       * conditions.
       */
      AgoraVideoRemoteReasonAudioFallback = 8,

      /**
       * 9: The remote audio-only stream switches back to the audio-and-video stream after the network
       * conditions improve.
       */
      AgoraVideoRemoteReasonAudioFallbackRecovery = 9,

      /**
       * 12: (iOS only) The remote user's app has switched to the background.
       */
      AgoraVideoRemoteReasonSDKInBackground = 12,

      /**
       * 13: The local video decoder does not support decoding the remote video stream.
       */
      AgoraVideoRemoteReasonCodecNotSupport = 13
};

/**
 * @brief The state of the local audio.
 */
typedef NS_ENUM(NSUInteger, AgoraAudioLocalState) {
    /**
     * 0: The local audio is in the initial state.
     */
    AgoraAudioLocalStateStopped = 0,
    /**
     * 1: The local audio capturing device starts successfully.
     */
    AgoraAudioLocalStateRecording = 1,
    /**
     * 2: The first audio frame encodes successfully.
     */
    AgoraAudioLocalStateEncoding = 2,
    /**
     * 3: The local audio fails to start.
     */
    AgoraAudioLocalStateFailed = 3,
};

/**
 * @brief Reasons for local audio state changes.
 */
typedef NS_ENUM(NSUInteger, AgoraAudioLocalReason) {
    /**
     * 0: The local audio is normal.
     */
    AgoraAudioLocalReasonOK = 0,
    /**
     * 1: No specified reason for the local audio failure. Remind your users to try to rejoin the
     * channel.
     */
    AgoraAudioLocalReasonFailure = 1,
    /**
     * 2: No permission to use the local audio capturing device. Remind your users to grant permission.
     */
    AgoraAudioLocalReasonDeviceNoPermission = 2,
    /**
     * 3: (iOS only) The local audio capture device is already in use. Remind your users to check
     * whether another
     * application occupies the microphone. Local audio capture automatically resumes after the
     * microphone is idle for about five seconds. You can also try to rejoin the channel after the
     * microphone is idle.
     */
    AgoraAudioLocalReasonDeviceBusy = 3,
    /**
     * 4: The local audio capture fails.
     */
    AgoraAudioLocalReasonRecordFailure = 4,
    /**
     * 5: The local audio encoding fails.
     */
    AgoraAudioLocalReasonEncodeFailure = 5,
    /**
     * 6: (macOS only) No local audio capture device. Remind your users to check whether the microphone
     * is connected
     * to the device properly in the control panel of the device or if the microphone is working
     * properly.
     */
    AgoraAudioLocalReasonNoRecordingDevice = 6,
    /**
     * 7: (macOS only) No local audio capture device. Remind your users to check whether the speaker is
     * connected to
     * the device properly in the control panel of the device or if the speaker is working properly.
     */
    AgoraAudioLocalReasonNoPlayoutDevice = 7,
    /**
     * 8: (iOS only) The local audio capture is interrupted by a system call, smart assistants, or alarm
     * clock.
     * Prompt your users to end the phone call, smart assistants, or alarm clock if the local audio
     * capture is required.
     */
    AgoraAudioLocalReasonInterrupted = 8,
};

/**
 * @brief Self-defined audio codec profile.
 */
typedef NS_ENUM(NSInteger, AgoraAudioCodecProfileType) {
  /**
   * 0: (Default) LC-AAC.
   */
  AgoraAudioCodecProfileLCAAC = 0,
  /**
   * 1: HE-AAC.
   */
  AgoraAudioCodecProfileHEAAC = 1,
  /**
   * 2: HE-AAC v2.
   */
  AgoraAudioCodecProfileHEAACv2 = 2
};

typedef NS_ENUM(NSInteger, AgoraLicenseVerifyCode) {
  /**
   * 1: Invalid license
  */
  AgoraLicenseVerifyInvalid = 1,
  /**
   * 2: License expired
  */
  AgoraLicenseVerifyExpire = 2,
  /**
   * 3: Exceed license minutes limit
  */
  AgoraLicenseVerifyMinutesExceed = 3,
  /**
   * 4: License use in limited period
  */
  AgoraLicenseVerifyLimitedPeriod = 4,
  /**
   * 5: Same license used in different devices at the same time
  */
  AgoraLicenseVerifyDiffDevices = 5,
  /**
   * 99: SDK internal error
  */
  AgoraLicenseVerifyInternal = 99,
};

/**
 * @brief Remote audio states.
 */
typedef NS_ENUM(NSUInteger, AgoraAudioRemoteState) {
    /**
     * 0: The local audio is in the initial state. The SDK reports this state in the case of
     * `AgoraAudioRemoteReasonLocalMuted`, `AgoraAudioRemoteReasonRemoteMuted` or
     * `AgoraAudioRemoteReasonRemoteOffline`.
     */
    AgoraAudioRemoteStateStopped = 0,
    /**
     * 1: The first remote audio packet is received.
     */
    AgoraAudioRemoteStateStarting = 1,
    /**
     * 2: The remote audio stream is decoded and plays normally. The SDK reports this state in the case
     * of `AgoraAudioRemoteReasonNetworkRecovery`, `AgoraAudioRemoteReasonLocalUnmuted` or
     * `AgoraAudioRemoteReasonRemoteUnmuted`.
     */
    AgoraAudioRemoteStateDecoding = 2,
    /**
     * 3: The remote audio is frozen. The SDK reports this state in the case of
     * `AgoraAudioRemoteReasonNetworkCongestion`.
     */
    AgoraAudioRemoteStateFrozen = 3,
    /**
     * 4: The remote audio fails to start. The SDK reports this state in the case of
     * `AgoraAudioRemoteReasonInternal`.
     */
    AgoraAudioRemoteStateFailed = 4,
};

/**
 * @brief The reason for the remote audio state change.
 */
typedef NS_ENUM(NSUInteger, AgoraAudioRemoteReason) {
      /**
       * 0: The SDK reports this reason when the audio state changes.
       */
      AgoraAudioRemoteReasonInternal = 0,
      /**
       * 1: Network congestion.
       */
      AgoraAudioRemoteReasonNetworkCongestion = 1,
      /**
       * 2: Network recovery.
       */
      AgoraAudioRemoteReasonNetworkRecovery = 2,
      /**
       * 3: The local user stops receiving the remote audio stream or disables the audio module.
       */
      AgoraAudioRemoteReasonLocalMuted = 3,
      /**
       * 4: The local user resumes receiving the remote audio stream or enables the audio module.
       */
      AgoraAudioRemoteReasonLocalUnmuted = 4,
      /**
       * 5: The remote user stops sending the audio stream or disables the audio module.
       */
      AgoraAudioRemoteReasonRemoteMuted = 5,
      /**
       * 6: The remote user resumes sending the audio stream or enables the audio module.
       */
      AgoraAudioRemoteReasonRemoteUnmuted = 6,
      /**
       * 7: The remote user leaves the channel.
       */
      AgoraAudioRemoteReasonRemoteOffline = 7,
};

/**
 * @brief Options for handling audio and video stream fallback when network conditions are weak.
 */
typedef NS_ENUM(NSInteger, AgoraStreamFallbackOptions) {
    /**
     * 0: No fallback processing is performed on audio and video streams, the quality of the audio and
     * video streams cannot be guaranteed.
     */
    AgoraStreamFallbackOptionDisabled = 0,
    /**
     * 1: Only receive low-quality (low resolution, low bitrate) video stream.
     */
    AgoraStreamFallbackOptionVideoStreamLow = 1,
    /**
     * 2: When the network conditions are weak, try to receive the low-quality video stream first. If
     * the video cannot be displayed due to extremely weak network environment, then fall back to
     * receiving audio-only stream.
     */
    AgoraStreamFallbackOptionAudioOnly = 2,
    /** 3~8: If the receiver SDK uses [setRemoteSubscribeFallbackOption]([AgoraRtcEngineKit setRemoteSubscribeFallbackOption:])，
     * it will receive one of the streams from agora::rtc::VIDEO_STREAM_LAYER_1 to agora::rtc::VIDEO_STREAM_LAYER_6 orderly
     * if the related layer exists when the network condition is poor. The lower bound of fallback depends on
     * the AgoraStreamFallbackOptionVideoStreamLayerX.
     */
    AgoraStreamFallbackOptionVideoStreamLayer1 = 3,
    AgoraStreamFallbackOptionVideoStreamLayer2 = 4,
    AgoraStreamFallbackOptionVideoStreamLayer3 = 5,
    AgoraStreamFallbackOptionVideoStreamLayer4 = 6,
    AgoraStreamFallbackOptionVideoStreamLayer5 = 7,
    AgoraStreamFallbackOptionVideoStreamLayer6 = 8,
};

/**
 * @brief The audio sampling rate of the stream to be pushed to the CDN.
 */
typedef NS_ENUM(NSInteger, AgoraAudioSampleRateType) {
    /**
     * 32000: 32 kHz
     */
    AgoraAudioSampleRateType32000 = 32000,
    /**
     * 44100: 44.1 kHz
     */
    AgoraAudioSampleRateType44100 = 44100,
    /**
     * 48000: (Default) 48 kHz
     */
    AgoraAudioSampleRateType48000 = 48000,
};

/**
 * @brief The audio profile.
 */
typedef NS_ENUM(NSInteger, AgoraAudioProfile) {
    /**
     * 0: The default audio profile.
     * - For the interactive streaming profile: A sample rate of 48 kHz, music encoding, mono, and a
     * bitrate of up to 64 Kbps.
     * - For the communication profile: A sample rate of 32 kHz, audio encoding, mono, and a bitrate of
     * up to 18 Kbps.
     */
    AgoraAudioProfileDefault = 0,
    /**
     * 1: A sample rate of 32 kHz, audio encoding, mono, and a bitrate of up to 18 Kbps.
     */
    AgoraAudioProfileSpeechStandard = 1,
    /**
     * 2: A sample rate of 48 kHz, music encoding, mono, and a bitrate of up to 64 Kbps.
     */
    AgoraAudioProfileMusicStandard = 2,
    /**
     * 3: A sample rate of 48 kHz, music encoding, stereo, and a bitrate of up to 80 Kbps.
     * On iOS, to implement stereo audio, you also need to call `setAdvancedAudioOptions:` and set
     * `audioProcessingChannels`
     * to `AgoraAudioProcessChannelstereo` in `AgoraAdvancedAudioOptions`.
     */
    AgoraAudioProfileMusicStandardStereo = 3,
    /**
     * 4: A sample rate of 48 kHz, music encoding, mono, and a bitrate of up to 96 Kbps.
     */
    AgoraAudioProfileMusicHighQuality = 4,
    /**
     * 5: A sample rate of 48 kHz, music encoding, stereo, and a bitrate of up to 128 Kbps.
     * On iOS, to implement stereo audio, you also need to call `setAdvancedAudioOptions:` and set
     * `audioProcessingChannels`
     * to `AgoraAudioProcessChannelstereo` in `AgoraAdvancedAudioOptions`.
     */
    AgoraAudioProfileMusicHighQualityStereo = 5,
    /**
     * 6: A sample rate of 16 kHz, audio encoding, mono, and Acoustic Echo Cancellation (AEC) enabled.
     */
    AgoraAudioProfileIot = 6,
};

/**
 * @brief The audio scenarios.
 */
typedef NS_ENUM(NSInteger, AgoraAudioScenario) {
    /**
     * 0: (Default) Automatic scenario match, where the SDK chooses the appropriate audio quality
     * according to the user role and audio route.
     */
    AgoraAudioScenarioDefault = 0,
    /**
     * 3: High-quality audio scenario, where users mainly play music. For example, instrument tutoring.
     */
    AgoraAudioScenarioGameStreaming = 3,
    /**
     * 5: Chatroom scenario, where users need to frequently switch the user role or mute and unmute the
     * microphone. For example, education scenarios.
     */
    AgoraAudioScenarioChatRoom = 5,
    /**
     * 7: Real-time chorus scenario, where users have good network conditions and require ultra-low
     * latency.
     */
    AgoraAudioScenarioChorus = 7,
    /**
     * 8: Meeting scenario that mainly contains the human voice.
     */
    AgoraAudioScenarioMeeting = 8,
    /** AI Server 
     * @technical preview*/
    AgoraAudioScenarioAIServer = 9,
    /**
     * 10: AI conversation scenario, which is only applicable to scenarios where the user interacts with
     * the conversational AI agent created by `Conversational AI Engine`.
     */
    AgoraAudioScenarioAiClient = 10
};

/**
 * @brief The type of the audio route.
 */
typedef NS_ENUM(NSInteger, AgoraAudioOutputRouting) {
    /**
     * -1: The default audio route.
     */
    AgoraAudioOutputRoutingDefault = -1,
    /**
     * 0: Audio output routing is a headset with microphone.
     */
    AgoraAudioOutputRoutingHeadset = 0,
    /**
     * 1: The audio route is an earpiece.
     */
    AgoraAudioOutputRoutingEarpiece = 1,
    /**
     * 2: The audio route is a headset without a microphone.
     */
    AgoraAudioOutputRoutingHeadsetNoMic = 2,
    /**
     * 3: The audio route is the speaker that comes with the device.
     */
    AgoraAudioOutputRoutingSpeakerphone = 3,
    /**
     * 4: The audio route is an external speaker.
     */
    AgoraAudioOutputRoutingLoudspeaker = 4,
    /**
     * 5: The audio route is a Bluetooth device using the HFP protocol.
     */
    AgoraAudioOutputRoutingBluetoothDeviceHfp = 5,
    /**
     * 6: (macOS only) The audio route is a USB peripheral device.
     */
    AgoraAudioOutputRoutingUsb = 6,
    /**
     * 7: (macOS only) The audio route is an HDMI peripheral device.
     */
    AgoraAudioOutputRoutingHdmi = 7,
    /**
     * 8: (macOS only) The audio route is a DisplayPort peripheral device.
     */
    AgoraAudioOutputRoutingDisplayPort = 8,
    /**
     * 9: (macOS only) The audio route is Apple AirPlay.
     */
    AgoraAudioOutputRoutingAirPlay = 9,
    /**
     * 10: (macOS only) The audio route is a Bluetooth device using the A2DP protocol.
     */
    AgoraAudioOutputRoutingBluetoothDeviceA2dp = 10
};

/**
 * @brief The use mode of the audio data.
 */
typedef NS_ENUM(NSInteger, AgoraAudioRawFrameOperationMode) {
    /**
     * 0: Read-only mode, For example, when users acquire the data with the Agora SDK, then start the
     * media push.
     */
    AgoraAudioRawFrameOperationModeReadOnly = 0,
    /**
     * 2: Read and write mode, For example, when users have their own audio-effect processing module and
     * perform some voice preprocessing, such as a voice change.
     */
    AgoraAudioRawFrameOperationModeReadWrite = 2,
};

/**
 * @brief The midrange frequency for audio equalization.
 */
typedef NS_ENUM(NSInteger, AgoraAudioEqualizationBandFrequency) {
    /**
     * 0: 31 Hz.
     */
    AgoraAudioEqualizationBand31 = 0,
    /**
     * 1: 62 Hz.
     */
    AgoraAudioEqualizationBand62 = 1,
    /**
     * 2: 125 Hz.
     */
    AgoraAudioEqualizationBand125 = 2,
    /**
     * 3: 250 Hz.
     */
    AgoraAudioEqualizationBand250 = 3,
    /**
     * 4: 500 Hz.
     */
    AgoraAudioEqualizationBand500 = 4,
    /**
     * 5: 1 kHz.
     */
    AgoraAudioEqualizationBand1K = 5,
    /**
     * 6: 2 kHz.
     */
    AgoraAudioEqualizationBand2K = 6,
    /**
     * 7: 4 kHz.
     */
    AgoraAudioEqualizationBand4K = 7,
    /**
     * 8: 8 kHz.
     */
    AgoraAudioEqualizationBand8K = 8,
    /**
     * 9: 16 kHz.
     */
    AgoraAudioEqualizationBand16K = 9,
};

/**
 * @brief Audio reverberation types.
 */
typedef NS_ENUM(NSInteger, AgoraAudioReverbType) {
    /**
     * 0: The level of the dry signal (dB). The value is between -20 and 10.
     */
    AgoraAudioReverbDryLevel = 0,
    /**
     * 1: The level of the early reflection signal (wet signal) (dB). The value is between -20 and 10.
     */
    AgoraAudioReverbWetLevel = 1,
    /**
     * 2: The room size of the reflection. The value is between 0 and 100.
     */
    AgoraAudioReverbRoomSize = 2,
    /**
     * 3: The length of the initial delay of the wet signal (ms). The value is between 0 and 200.
     */
    AgoraAudioReverbWetDelay = 3,
    /**
     * 4: The reverberation strength. The value is between 0 and 100.
     */
    AgoraAudioReverbStrength = 4,
};

/**
 * @brief The options for SDK preset voice beautifier effects.
 */
typedef NS_ENUM(NSInteger, AgoraVoiceBeautifierPreset) {
  /**
   * Turn off voice beautifier effects and use the original voice.
   */
  AgoraVoiceBeautifierPresetOff = 0x00000000,
  /**
   * A more magnetic voice.
   * @note Agora recommends using this enumerator to process a male-sounding voice; otherwise, you may
   * experience vocal distortion.
   */
  AgoraVoiceBeautifierPresetChatBeautifierMagnetic = 0x01010100,
  /**
   * A fresher voice.
   * @note Agora recommends using this enumerator to process a female-sounding voice; otherwise, you
   * may experience vocal distortion.
   */
  AgoraVoiceBeautifierPresetChatBeautifierFresh = 0x01010200,
  /**
   * A more vital voice.
   * @note Agora recommends using this enumerator to process a female-sounding voice; otherwise, you
   * may experience vocal distortion.
   */
  AgoraVoiceBeautifierPresetChatBeautifierVitality = 0x01010300,
  /**
   * Singing beautifier effect.
   * - If you call `setVoiceBeautifierPreset:` ( AgoraVoiceBeautifierPresetSingingBeautifier ), you
   * can beautify a male-sounding voice and add a reverberation effect that sounds like singing in a
   * small room. Agora recommends using this enumerator to process a male-sounding voice; otherwise,
   * you might experience vocal distortion.
   * - If you call `setVoiceBeautifierParameters:param1:param2:` (
   * AgoraVoiceBeautifierPresetSingingBeautifier, param1, param2), you can beautify a male or
   * female-sounding voice and add a reverberation effect.
   * @since v3.3.0
   */
  AgoraVoiceBeautifierPresetSingingBeautifier= 0x01020100,
  /**
   * A more vigorous voice.
   */
  AgoraVoiceBeautifierTimbreTransformationVigorous = 0x01030100,
  /**
   * A deep voice.
   */
  AgoraVoiceBeautifierTimbreTransformationDeep = 0x01030200,
  /**
   * A mellower voice.
   */
  AgoraVoiceBeautifierTimbreTransformationMellow= 0x01030300,
  /**
   * Falsetto.
   */
  AgoraVoiceBeautifierTimbreTransformationFalsetto= 0x01030400,
  /**
   * A fuller voice.
   */
  AgoraVoiceBeautifierTimbreTransformationFull = 0x01030500,
  /**
   * A clearer voice.
   */
  AgoraVoiceBeautifierTimbreTransformationClear = 0x01030600,
  /**
   * A more resounding voice.
   */
  AgoraVoiceBeautifierTimbreTransformationResounding = 0x01030700,
  /**
   * A more ringing voice.
   */
  AgoraVoiceBeautifierTimbreTransformatRinging= 0x01030800,

  /**
   * A ultra-high quality voice, which makes the audio clearer and restores more details.
   * - To achieve better audio effect quality, Agora recommends that you set the `profile` of
   * `setAudioProfile:` to `AgoraAudioProfileMusicHighQuality` (4) or
   * `AgoraAudioProfileMusicHighQualityStereo` (5) and `scenario` to `AgoraAudioScenarioGameStreaming`
   * (3) before calling `setVoiceBeautifierPreset:`.
   * - If you have an audio capturing device that can already restore audio details to a high degree,
   * Agora recommends that you do not enable ultra-high quality; otherwise, the SDK may over-restore
   * audio details, and you may not hear the anticipated voice effect.
   */
  AgoraVoiceBeautifierUltraHighQuality = 0x01040100
};

/**
 * @brief Preset audio effects.
 *
 * @details
 * To get better audio effects, Agora recommends calling `setAudioProfile:scenario:` and setting the
 * `profile` parameter as recommended below before using the preset audio effects.
 * | Preset audio effects                                                                                                                          | `profile`                                                                           |
 * | --------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
 * | - AgoraAudioEffectPresetRoomAcousVirtualStereo - AgoraAudioEffectPresetRoomAcous3DVoice - AgoraAudioEffectPresetRoomAcousVirtualSurroundSound | `AgoraAudioProfileMusicHighQualityStereo` or `AgoraAudioProfileMusicStandardStereo` |
 * | Other preset audio effects (except for `AgoraAudioEffectPresetOff` )                                                                          | `AgoraAudioProfileMusicHighQuality` or `AgoraAudioProfileMusicHighQualityStereo`    |
 *
 */
typedef NS_ENUM(NSInteger, AgoraAudioEffectPreset) {
  /**
   * Turn off voice effects, that is, use the original voice.
   */
  AgoraAudioEffectPresetOff = 0x00000000,
  /**
   * The voice effect typical of a KTV venue.
   */
  AgoraAudioEffectPresetRoomAcousticsKTV = 0x02010100,
  /**
   * The voice effect typical of a concert hall.
   */
  AgoraAudioEffectPresetRoomAcousVocalConcer = 0x02010200,
  /**
   * The voice effect typical of a recording studio.
   */
  AgoraAudioEffectPresetRoomAcousStudio = 0x02010300,
  /**
   * The voice effect typical of a vintage phonograph.
   */
  AgoraAudioEffectPresetRoomAcousPhonograph = 0x02010400,
  /**
   * The virtual stereo effect, which renders monophonic audio as stereo audio.
   */
  AgoraAudioEffectPresetRoomAcousVirtualStereo = 0x02010500,
  /**
   * A more spatial voice effect.
   */
  AgoraAudioEffectPresetRoomAcousSpatial = 0x02010600,
  /**
   * A more ethereal voice effect.
   */
  AgoraAudioEffectPresetRoomAcousEthereal = 0x02010700,
  /**
   * A 3D voice effect that makes the voice appear to be moving around the user. The default cycle
   * period is 10 seconds. After setting this effect, you can call
   * `setAudioEffectParameters:param1:param2:` to modify the movement period.
   * @note If the 3D voice effect is enabled, users need to use stereo audio playback devices to hear
   * the anticipated voice effect.
   */
  AgoraAudioEffectPresetRoomAcous3DVoice = 0x02010800,
  /**
   * Virtual surround sound, that is, the SDK generates a simulated surround sound field on the basis
   * of stereo channels, thereby creating a surround sound effect.
   * @note If the virtual surround sound is enabled, users need to use stereo audio playback devices
   * to hear the anticipated audio effect.
   */
  AgoraAudioEffectPresetRoomAcousVirtualSurroundSound = 0x02010900,
  /**
   * The audio effect of chorus. Agora recommends using this effect in chorus scenarios to enhance the
   * sense of depth and dimension in the vocals.
   */
  AgoraAudioEffectPresetRoomAcousticsChorus = 0x02010D00,
  /**
   * A middle-aged man's voice.
   * @note Agora recommends using this preset to process a male-sounding voice; otherwise, you may not
   * hear the anticipated voice effect.
   */
  AgoraAudioEffectPresetVoiceChangerEffectUncle = 0x02020100,
  /**
   * An older man's voice.
   * @note Agora recommends using this preset to process a male-sounding voice; otherwise, you may not
   * hear the anticipated voice effect.
   */
  AgoraAudioEffectPresetVoiceChangerEffectOldMan = 0x02020200,
  /**
   * A boy's voice.
   * @note Agora recommends using this preset to process a male-sounding voice; otherwise, you may not
   * hear the anticipated voice effect.
   */
  AgoraAudioEffectPresetVoiceChangerEffectBoy= 0x02020300,
  /**
   * A young woman's voice.
   * @note Agora recommends using this preset to process a female-sounding voice; otherwise, you may
   * not hear the anticipated voice effect.
   */
  AgoraAudioEffectPresetVoiceChangerEffectSister= 0x02020400,
  /**
   * A girl's voice.
   * @note Agora recommends using this preset to process a female-sounding voice; otherwise, you may
   * not hear the anticipated voice effect.
   */
  AgoraAudioEffectPresetVoiceChangerEffectGirl = 0x02020500,
  /**
   * The voice of Pig King, a character in Journey to the West who has a voice like a growling bear.
   */
  AgoraAudioEffectPresetVoiceChangerEffectPigKin = 0x02020600,
  /**
   * The Hulk's voice.
   */
  AgoraAudioEffectPresetVoiceChangerEffectHulk = 0x02020700,
  /**
   * The voice effect typical of R&B music.
   */
  AgoraAudioEffectPresetStyleTransformationRnb = 0x02030100,
  /**
   * The voice effect typical of popular music.
   */
  AgoraAudioEffectPresetStyleTransformationPopular= 0x02030200,
  /**
   * A pitch correction effect that corrects the user's pitch based on the pitch of the natural C
   * major scale. After setting this voice effect, you can call
   * `setAudioEffectParameters:param1:param2:` to adjust the basic mode of tuning and the pitch of the
   * main tone.
   */
  AgoraAudioEffectPresetPitchCorrection = 0x02040100
};

/**
 * @brief The options for SDK preset voice conversion effects.
 */
typedef NS_ENUM(NSInteger, AgoraVoiceConversionPreset) {
  /**
   * Turn off voice conversion effects and use the original voice.
   */
  AgoraVoiceConversionPresetOff = 0x00000000,
  /**
   * A gender-neutral voice. To avoid audio distortion, ensure that you use this enumerator to process
   * a female-sounding voice.
   */
  AgoraVoiceConversionPresetNeutral = 0x03010100,
  /**
   * A sweet voice. To avoid audio distortion, ensure that you use this enumerator to process a
   * female-sounding voice.
   */
  AgoraVoiceConversionPresetSweet = 0x03010200,
  /**
   * A steady voice. To avoid audio distortion, ensure that you use this enumerator to process a
   * male-sounding voice.
   */
  AgoraVoiceConversionPresetChangerSolid = 0x03010300,
  /**
   * A deep voice. To avoid audio distortion, ensure that you use this enumerator to process a
   * male-sounding voice.
   */
  AgoraVoiceConversionPresetChangerBass = 0x03010400,
  /** A voice like a cartoon character. */
  AgoraVoiceConversionPresetChangerCartoon = 0x03010500,
  /** A voice like a child. */
  AgoraVoiceConversionPresetChangerChildlike = 0x03010600,
  /** A voice like a phone operator. */
  AgoraVoiceConversionPresetChangerPhoneOperator = 0x03010700,
  /** A monster voice. */
  AgoraVoiceConversionPresetChangerMonster = 0x03010800,
  /** A voice like Transformers. */
  AgoraVoiceConversionPresetChangerTransformers = 0x03010900,
  /** A voice like Groot. */
  AgoraVoiceConversionPresetChangerGroot = 0x03010A00,
  /** A voice like Darth Vader. */
  AgoraVoiceConversionPresetChangerDarthVader = 0x03010B00,
  /** A rough female voice. */
  AgoraVoiceConversionPresetChangerIronLady = 0x03010C00,
  /** A voice like Crayon Shin-chan. */
  AgoraVoiceConversionPresetChangerShinchan = 0x03010D00,
  /** A voice like a castrato. */
  AgoraVoiceConversionPresetChangerGirlishMan = 0x03010E00,
  /** A voice like chipmunk. */
  AgoraVoiceConversionPresetChangerChipmunk = 0x03010F00,

};

/** The preset local voice reverberation option. */
typedef NS_ENUM(NSInteger, AgoraAudioReverbPreset) {
    /** The original voice (no local voice reverberation). */
    AgoraAudioReverbPresetOff = 0x00000000,
    /** The reverberation style typical of a KTV venue (enhanced). */
    AgoraAudioReverbPresetFxKTV = 0x02010100,
    /** The reverberation style typical of a concert hall (enhanced). */
    AgoraAudioReverbPresetFxVocalConcert = 0x02010200,
    /** The reverberation style typical of an uncle's voice. */
    AgoraAudioReverbPresetFxUncle = 0x02020100,
    /** The reverberation style typical of a little sister's voice. */
    AgoraAudioReverbPresetFxSister = 0x02020400,
    /** The reverberation style typical of a recording studio (enhanced). */
    AgoraAudioReverbPresetFxStudio = 0x02010300,
    /** The reverberation style typical of popular music (enhanced). */
    AgoraAudioReverbPresetFxPopular = 0x02030200,
    /** The reverberation style typical of R&B music (enhanced). */
    AgoraAudioReverbPresetFxRNB = 0x02030100,
    /** The reverberation style typical of the vintage phonograph. */
    AgoraAudioReverbPresetFxPhonograph = 0x02010400,
    /** The reverberation style typical of chorus. */
    AgoraAudioReverbPresetFxChorus   =  0x02010D00
};

/**
 * @brief Preset headphone equalizer types.
 */
typedef NS_ENUM(NSInteger, AgoraHeadphoneEQPreset){
  /**
   * The headphone equalizer is disabled, and the original audio is heard.
   */
  AgoraAudioHeadphoneEQPresetOff = 0x00000000,
  /**
   * An equalizer is used for headphones.
   */
  AgoraAudioHeadphoneEQPresetOverear = 0x04000001,
  /**
   * An equalizer is used for in-ear headphones.
   */
  AgoraAudioHeadphoneEQPresetInear = 0x04000002
};

/**
 * @brief Voice AI tuner sound types.
 */
typedef NS_ENUM(NSInteger, AgoraVoiceAITunerType){
  /**
   * 0: Mature male voice. A deep and magnetic male voice.
   */
  AgoraVoiceAITunerMatureMale,
  /**
   * 1: Fresh male voice. A fresh and slightly sweet male voice.
   */
  AgoraVoiceAITunerFreshMale,
  /**
   * 2: Elegant female voice. A deep and charming female voice.
   */
  AgoraVoiceAITunerElegantFemale,
  /**
   * 3: Sweet female voice. A high-pitched and cute female voice.
   */
  AgoraVoiceAITunerSweetFemale,
  /**
   * 4: Warm male singing. A warm and melodious male voice.
   */
  AgoraVoiceAITunerWarmMaleSinging,
  /**
   * 5: Gentle female singing. A soft and delicate female voice.
   */
  AgoraVoiceAITunerGentleFemaleSinging,
  /**
   * 6: Husky male singing. A unique husky male voice.
   */
  AgoraVoiceAITunerHuskyMaleSinging,
  /**
   * 7: Warm elegant female singing. A warm and mature female voice.
   */
  AgoraVoiceAITunerWarmElegantFemaleSinging,
  /**
   * 8: Powerful male singing. A strong and powerful male voice.
   */
  AgoraVoiceAITunerPowerfulMaleSinging,
  /**
   * 9: Dreamy female singing. A dreamy and soft female voice.
   */
  AgoraVoiceAITunerDreamyFemaleSinging,
};

/**
 * @brief The operation permissions of the SDK on the audio session.
 */
typedef NS_OPTIONS(NSUInteger, AgoraAudioSessionOperationRestriction) {
    /**
     * 0: No restriction, the SDK can change the audio session.
     */
    AgoraAudioSessionOperationRestrictionNone              = 0,
    /**
     * 1: The SDK cannot change the audio session category.
     */
    AgoraAudioSessionOperationRestrictionSetCategory       = 1,
    /**
     * 2: The SDK cannot change the audio session category, mode, or categoryOptions.
     */
    AgoraAudioSessionOperationRestrictionConfigureSession  = 1 << 1,
    /**
     * 4: The SDK keeps the audio session active when the user leaves the channel, for example, to play
     * an audio file in the background.
     */
    AgoraAudioSessionOperationRestrictionDeactivateSession = 1 << 2,
    /**
     * 128: Completely restricts the operation permissions of the SDK on the audio session; the SDK
     * cannot change the audio session.
     */
    AgoraAudioSessionOperationRestrictionAll               = 1 << 7
};

/**
 * @brief Media device types.
 */
typedef NS_ENUM(NSInteger, AgoraMediaDeviceType) {
    /**
     * -1: Unknown device type.
     */
    AgoraMediaDeviceTypeAudioUnknown = -1,
    /**
     * 0: Audio playback device.
     */
    AgoraMediaDeviceTypeAudioPlayout = 0,
    /**
     * 1: Audio capturing device.
     */
    AgoraMediaDeviceTypeAudioRecording = 1,
    /**
     * 2: Video rendering device (graphics card).
     */
    AgoraMediaDeviceTypeVideoRender = 2,
    /**
     * 3: Video capturing device.
     */
    AgoraMediaDeviceTypeVideoCapture = 3,
    /**
     * 4: Audio playback device for an app.
     */
    AgoraMediaDeviceTypeAudioApplicationPlayout = 4,
    /**
     * 5: Virtual audio playback device (virtual sound card).
     */
    AgoraMediaDeviceTypeAudioVirtualPlayout = 5,
    /**
     * 6: Virtual audio capturing device (virtual sound card).
     */
    AgoraMediaDeviceTypeAudioVirtualRecording = 6,

};

/**
 * @brief The video pixel format.
 */
typedef NS_ENUM(NSInteger, AgoraVideoFormat) {
    /**
     * 1: The format is I420.
     */
    AgoraVideoFormatI420 = 1,
    /** BGRA video frame fromat*/
    AgoraVideoFormatBGRA = 2,
    /** NV21 video frame fromat*/
    AgoraVideoFormatNV21 = 3,
    /**
     * 4: The format is RGBA.
     */
    AgoraVideoFormatRGBA = 4,
    /** NV12 video frame fromat*/
    AgoraVideoFormatNV12 = 8,
    /**
     * 12: The format is CVPixelBufferRef NV12.
     */
    AgoraVideoFormatCVPixelNV12 = 12,
    /**
     * 13: The format is CVPixelBufferRef I420.
     */
    AgoraVideoFormatCVPixelI420 = 13,
    /**
     * 14: The format is CVPixelBufferRef BGRA.
     */
    AgoraVideoFormatCVPixelBGRA = 14,
    /** iOS texture P010 (CVPixelBufferRef)*/
    AgoraVideoFormatCVPixelP010 = 15,
    /**
     * 16: The format is I422.
     */
    AgoraVideoFormatI422 = 16,
    /** I010 video frame fromat*/
    AgoraVideoFormatI010 = 18,
    /**
     * 0: Raw video pixel format.
     */
    AgoraVideoFormatDefault = 0,
};

/**
 * @brief The video frame type.
 */
typedef NS_ENUM(NSInteger, AgoraVideoFrameType) {
  /**
   * 0: A black frame.
   */
  AgoraVideoFrameTypeBlankFrame = 0,
  /**
   * 3: Key frame.
   */
  AgoraVideoFrameTypeKeyFrame = 3,
  /**
   * 4: Delta frame.
   */
  AgoraVideoFrameTypeDeltaFrame = 4,
  /**
   * 5: The B frame.
   */
  AgoraVideoFrameTypeBFrame = 5,
  /**
   * 6: A discarded frame.
   */
  AgoraVideoFrameTypeDroppableFrame = 6,
  /**
   * Unknown frame.
   */
  AgoraVideoFrameTypeUnknow
};

typedef NS_ENUM(NSInteger, AgoraColorPrimaryID) {
  AgoraColorPrimaryIDBT709 = 1,

  AgoraColorPrimaryIDUnspecified = 2,

  AgoraColorPrimaryIDSMPTE170M = 6,

  AgoraColorPrimaryIDBT2020 = 9,

  AgoraColorPrimaryIDInvalid = 100,
};

typedef NS_ENUM(NSInteger, AgoraColorTransferID) {
  AgoraColorTransferIDBT709 = 1,

  AgoraColorTransferIDUnspecified = 2,

  AgoraColorTransferIDSMPTE170M = 6,

  AgoraColorTransferIDSMPTEST2084 = 16,

  AgoraColorTransferIDARIB_STD_B67 = 18,

  AgoraColorTransferIDInvalid = 100,
};

typedef NS_ENUM(NSInteger, AgoraColorMatrixID) {
  AgoraColorMatrixIDRGB = 0,

  AgoraColorMatrixIDBT709 = 1,

  AgoraColorMatrixIDUnspecified = 2,

  AgoraColorMatrixIDSMPTE170M = 6,

  AgoraColorMatrixIDBT2020_NCL = 9,

  AgoraColorMatrixIDInvalid = 100,
};

typedef NS_ENUM(NSInteger, AgoraColorRangeID) {
  AgoraColorRangeIDInvalid = 0,
  // Limited Rec. 709 color range with RGB values ranging from 16 to 235.
  AgoraColorRangeIDLimited = 1,
  // Full RGB color range with RGB valees from 0 to 255.
  AgoraColorRangeIDFull = 2,
  // Range is defined by MatrixCoefficients/TransferCharacteristics.
  AgoraColorRangeIDDerived = 3,
};

/**
 * @brief The process mode of the video frame:
 */
typedef NS_ENUM(NSInteger, AgoraVideoFrameProcessMode) {
  /**
   * Read-only mode.
   * In this mode, you do not modify the video frame. The video frame observer is a renderer.
   */
  AgoraVideoFrameProcessModeReadOnly, // Observer works as a pure renderer and will not modify the original frame.
  /**
   * Read and write mode.
   * In this mode, you modify the video frame. The video frame observer is a video filter.
   */
  AgoraVideoFrameProcessModeReadWrite, // Observer works as a filter that will process the video frame and affect the following frame processing in SDK.
};

/**
 * @brief Connection states.
 */
typedef NS_ENUM(NSInteger, AgoraConnectionState) {
  /**
   * 1: The SDK is disconnected from the Agora edge server. The state indicates the SDK is in one of
   * the following phases:
   * - Theinitial state before calling the
   * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` method.
   * - The app calls the `leaveChannel:` method.
   */
  AgoraConnectionStateDisconnected = 1,
  /**
   * 2: The SDK is connecting to the Agora edge server. This state indicates that the SDK is
   * establishing a connection with the specified channel after the app calls
   * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`.
   * - If the SDK successfully joins the channel, it triggers the
   * `rtcEngine:connectionChangedToState:reason:` callback and the connection state switches to
   * AgoraConnectionStateConnected.
   * - After the connection is established, the SDK also initializes the media and triggers
   * `rtcEngine:didJoinChannel:withUid:elapsed:` when everything is ready.
   */
  AgoraConnectionStateConnecting = 2,
  /**
   * 3: The SDK is connected to the Agora edge server. This state also indicates that the user has
   * joined a channel and can now publish or subscribe to a media stream in the channel. If the
   * connection to the channel is lost because, for example, if the network is down or switched, the
   * SDK automatically tries to reconnect and triggers `rtcEngine:connectionChangedToState:reason:`
   * callback, notifying that the current network state becomes AgoraConnectionStateReconnecting.
   */
  AgoraConnectionStateConnected = 3,
  /**
   * 4: The SDK keeps reconnecting to the Agora edge server. The SDK keeps rejoining the channel after
   * being disconnected from a joined channel because of network issues.
   * - If the SDK cannot rejoin the channel within 10 seconds, it triggers
   * `rtcEngineConnectionDidLost:`, stays in the AgoraConnectionStateReconnecting state, and keeps
   * rejoining the channel.
   * - If the SDK fails to rejoin the channel 20 minutes after being disconnected from the Agora edge
   * server, the SDK triggers the `rtcEngine:connectionChangedToState:reason:` callback, switches to
   * the AgoraConnectionStateFailed state, and stops rejoining the channel.
   */
  AgoraConnectionStateReconnecting = 4,
  /**
   * 5: The SDK fails to connect to the Agora edge server or join the channel. This state indicates
   * that the SDK stops trying to rejoin the channel. You must call `leaveChannel:` to leave the
   * channel.
   * - You can call `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to rejoin the
   * channel.
   * - If the SDK is banned from joining the channel by the Agora edge server through the RESTful API,
   * the SDK triggers the `rtcEngine:connectionChangedToState:reason:` callback.
   */
  AgoraConnectionStateFailed = 5,
};

/**
 * @brief Network type.
 */
typedef NS_ENUM(NSInteger, AgoraNetworkType) {
  /**
   * -1: The network type is unknown.
   */
  AgoraNetworkTypeUnknown = -1,
  /**
   * 0: The SDK disconnects from the network.
   */
  AgoraNetworkTypeDisconnected = 0,
  /**
   * 1: The network type is LAN.
   */
  AgoraNetworkTypeLAN = 1,
  /**
   * 2: The network type is Wi-Fi (including hotspots).
   */
  AgoraNetworkTypeWIFI = 2,
  /**
   * 3: The network type is mobile 2G.
   */
  AgoraNetworkType2G = 3,
  /**
   * 4: The network type is mobile 3G.
   */
  AgoraNetworkType3G = 4,
  /**
   * 5: The network type is mobile 4G.
   */
  AgoraNetworkType4G = 5,
  /**
   * 6: The network type is mobile 5G.
   */
  AgoraNetworkType5G = 6,
};

/**
 * @brief Video degradation preferences when the bandwidth is a constraint.
 */
typedef NS_ENUM(NSInteger, AgoraDegradationPreference) {
    /**
     * -1: (Default) Automatic mode. The SDK will automatically select
     * AgoraDegradationMaintainFramerate, AgoraDegradationBalanced or AgoraDegradationMaintainResolution
     * based on the video scenario you set, in order to achieve the best overall quality of experience
     * (QoE).
     */
    AgoraDegradationMaintainAuto = -1,
    /**
     * 0: Prefers to reduce the video frame rate while maintaining video resolution during video
     * encoding under limited bandwidth. This degradation preference is suitable for scenarios where
     * video quality is prioritized.
     * @deprecated
     */
    AgoraDegradationMaintainQuality = 0,
    /** 1: Reduces the video resolution while maintaining the video frame rate during video encoding
     * under limited bandwidth. This degradation preference is suitable for scenarios where smoothness
     * is prioritized and video quality is allowed to be reduced. 
     */
    AgoraDegradationMaintainFramerate = 1,
    /**
     * 2: Reduces the video frame rate and video resolution simultaneously during video encoding under
     * limited bandwidth. The AgoraDegradationBalanced has a lower reduction than
     * AgoraDegradationMaintainQuality and AgoraDegradationMaintainFramerate, and this preference is
     * suitable for scenarios where both smoothness and video quality are a priority.
     * @note The resolution of the video sent may change, so remote users need to handle this issue. See
     * `rtcEngine:videoSizeChangedOfSourceType:uid:size:rotation:`.
     */
    AgoraDegradationBalanced = 2,
     /**
      * 3: Reduces the video frame rate while maintaining the video resolution during video encoding
      * under limited bandwidth. This degradation preference is suitable for scenarios where video
      * quality is prioritized.
      */
    AgoraDegradationMaintainResolution = 3,
    /** Disable VQC adjustion. */
    AgoraDegradationDisable = 100,
};

/**
 * @brief Video encoder preference.
 */
typedef NS_ENUM(NSInteger, AgoraEncodingPreference) {
   /**
    * -1: Adaptive preference. The SDK automatically selects the optimal encoding type for encoding
    * based on factors such as platform and device type.
    */
   AgoraEncodingPreferAuto = -1,
  /**
   * 0: Software coding preference. The SDK prefers software encoders for video encoding.
   */
  AgoraEncodingPrefersoftware = 0,
  /**
   * 1: Hardware encoding preference. The SDK prefers a hardware encoder for video encoding. When the
   * device does not support hardware encoding, the SDK automatically uses software encoding and
   * reports the currently used video encoder type through `hwEncoderAccelerating` in the
   * `rtcEngine:localVideoStats:sourceType:` callback.
   */
  AgoraEncodingPreferhardware = 1,
};

/**
 * @brief Compression preference for video encoding.
 */
typedef NS_ENUM(NSInteger, AgoraCompressionPreference) {
    /**
     * -1: (Default) Automatic mode. The SDK will automatically select AgoraCompressionLowLatency or
     * AgoraCompressionQuality based on the video scenario you set to achieve the best user experience.
     */
    AgoraCompressionAuto = -1,
    /**
     * 0: Low latency preference. The SDK compresses video frames to reduce latency. This preference is
     * suitable for scenarios where smoothness is prioritized and reduced video quality is acceptable.
     */
    AgoraCompressionLowLatency = 0,
    /**
     * 1: High quality preference. The SDK compresses video frames while maintaining video quality. This
     * preference is suitable for scenarios where video quality is prioritized.
     */
    AgoraCompressionQuality = 1,
};

/**
 * @brief The bit mask of the codec type.
 */
typedef NS_ENUM(NSInteger, AgoraCodecCapMask) {
    /**
     * (0): The device does not support encoding or decoding.
     */
    AgoraCodecMaskNone = 0,
    /**
     * (1 << 0): The device supports hardware decoding.
     */
    AgoraCodecMaskHwDec = 1,
    /**
     * (1 << 1): The device supports hardware encoding.
     */
    AgoraCodecMaskHwEnc = 2,
    /**
     * (1 << 2): The device supports software decoding.
     */
    AgoraCodecMaskSwDec = 4,
    /**
     * (1 << 3): The device supports software ecoding.
     */
    AgoraCodecMaskSwEnc = 8,
};

/**
 * @brief The contrast level.
 */
typedef NS_ENUM(NSUInteger, AgoraLighteningContrastLevel) {
  /**
   * 0: Low contrast level.
   */
  AgoraLighteningContrastLow = 0,
  /**
   * 1: (Default) Normal contrast level.
   */
  AgoraLighteningContrastNormal = 1,
  /**
   * 2: High contrast level.
   */
  AgoraLighteningContrastHigh = 2,
};

/**
 * @brief Chooses the specific facial areas that need to be adjusted.
 *
 * @since v4.4.0
 */
typedef NS_ENUM(NSInteger, AgoraFaceShapeArea) {
  /**
   * -1: (Default) Invalid area; facial enhancement effects do not take effect.
   */
  AgoraFaceShapeAreaNone = -1,
  /**
   * (100): Head, used to achieve a smaller head effect. The value range is 0 to 100, and the default
   * value is 50. The larger the value, the more noticeable the adjustment.
   */
  AgoraFaceShapeAreaHeadScale = 100,
  /**
   * (101): Forehead, used to adjust the hairline height. The range is [0, 100], with a default value
   * of 0. The larger the value, the more noticeable the adjustment.
   */
  AgoraFaceShapeAreaForehead = 101,
  /**
   * (102): Face contour, used to achieve a slimmer face effect. The range is [0, 100], with a default
   * value of 0. The larger the value, the more noticeable the adjustment.
   */
  AgoraFaceShapeAreaFaceContour = 102,
  /**
   * (103): Face length, used to achieve a longer face effect. The range is [-100, 100], with a
   * default value of 0. The greater the absolute value, the more noticeable the adjustment. Negative
   * values indicate the opposite direction.
   */
  AgoraFaceShapeAreaFaceLength = 103,
  /**
   * (104): Face width, used to achieve a narrower face effect. The range is [0, 100], with a default
   * value of 0. The larger the value, the more noticeable the adjustment.
   */
  AgoraFaceShapeAreaFaceWidth = 104,
  /**
   * (105): Cheekbone, used to adjust cheekbone width. The range is [0, 100], with a default value of
   * 0. The larger the value, the more noticeable the adjustment.
   */
  AgoraFaceShapeAreaCheekbone = 105,
  /**
   * (106): Cheek, used to adjust cheek width. The range is [0, 100], with a default value of 0. The
   * larger the value, the more noticeable the adjustment.
   */
  AgoraFaceShapeAreaCheek = 106,
  /**
   * (107): Adjustment of the mandible. The range is [0, 100], with a default value of 0. The larger
   * the value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaMandible = 107,
  /**
   * (108): Chin, used to adjust chin length. The range is [-100, 100], with a default value of 0. The
   * greater the absolute value, the more noticeable the adjustment. Negative values indicate the
   * opposite direction.
   */
  AgoraFaceShapeAreaChin = 108,
  /**
   * (200): Eyes, used to achieve a larger eye effect. The value range is 0 to 100, and the default
   * value is 50. The larger the value, the more noticeable the adjustment.
   */
  AgoraFaceShapeAreaEyeScale = 200,
  /**
   * (201): Eye distance adjustment. The range is [-100, 100], with a default value of 0. The greater
   * the absolute value, the more noticeable the adjustment. Negative values indicate the opposite
   * direction.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaEyeDistance = 201,
  /**
   * (202): Eye position adjustment. The range is [-100, 100], with a default value of 0. The greater
   * the absolute value, the more noticeable the adjustment. Negative values indicate the opposite
   * direction.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaEyePosition = 202,
  /**
   * (203): Lower eyelid adjustment. The range is [0, 100], with a default value of 0. The larger the
   * value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaLowerEyelid = 203,
  /**
   * (204): Pupil size adjustment. The range is [0, 100], with a default value of 0. The larger the
   * value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaEyePupils = 204,
  /**
   * (205): Inner eye corner adjustment. The range is [-100, 100], with a default value of 0. The
   * greater the absolute value, the more noticeable the adjustment. Negative values indicate the
   * opposite direction.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaEyeInnerCorner = 205,
  /**
   * (206): Outer eye corner adjustment. The range is [-100, 100], with a default value of 0. The
   * greater the absolute value, the more noticeable the adjustment. Negative values indicate the
   * opposite direction.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaEyeOuterCorner = 206,
  /**
   * (300): Nose length, used to achieve a longer nose effect. The range is [-100, 100], with a
   * default value of 0.
   */
  AgoraFaceShapeAreaNoseLength = 300,
  /**
   * (301): Nose width, used to achieve a slimmer nose effect. The range is [0, 100], with a default
   * value of 0. The larger the value, the more noticiable the effect of narrowing the nose.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaNoseWidth = 301,
  /**
   * (302): Nose wing adjustment. The value range is 0 to 100, and the default value is 10. The larger
   * the value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaNoseWing = 302,
  /**
   * (303): Nose root adjustment. The range is [0, 100], with a default value of 0. The larger the
   * value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaNoseRoot = 303,
  /**
   * (304): Nose bridge adjustment. The value range is 0 to 100, and the default value is 50. The
   * larger the value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaNoseBridge = 304,
  /**
   * (305): Nose tip adjustment. The value range is 0 to 100, and the default value is 50. The larger
   * the value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaNoseTip = 305,
  /**
   * (306): Overall nose adjustment. The range is [-100, 100], with a default value of 50. The greater
   * the absolute value, the more noticeable the adjustment. Negative values indicate the opposite
   * direction.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaNoseGeneral = 306,
  /**
   * (400): Mouth, used to achieve a larger mouth effect. The range is [-100, 100], with a default
   * value of 20. The greater the absolute value, the more noticeable the adjustment. Negative values
   * indicate the opposite direction.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaMouthScale = 400,
  /**
   * (401): Mouth position adjustment. The range is [0, 100], with a default value of 0. The larger
   * the value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaMouthPosition = 401,
  /**
   * (402): Mouth smile adjustment. The value range is [0,1], and the default value is 0. The larger
   * the value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaMouthSmile = 402,
  /**
   * (403): Lip shape adjustment. The range is [0, 100], with a default value of 0. The larger the
   * value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaMouthLip = 403,
  /**
   * (500): Eyebrow position adjustment. The range is [-100, 100], with a default value of 0. The
   * greater the absolute value, the more noticeable the adjustment. Negative values indicate the
   * opposite direction.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaEyebrowPosition = 500,
  /**
   * (501): Eyebrow thickness adjustment. The range is [-100, 100], with a default value of 0. The
   * larger the value, the more noticeable the adjustment.
   * @since v4.6.0
   */
  AgoraFaceShapeAreaEyebrowThickness = 501,
};

/**
 * @brief The facial enhancement style options.
 *
 * @since v4.4.0
 */
typedef NS_ENUM(NSUInteger, AgoraFaceShapeStyle) {
  /**
   * 0: (Default) Feminine style.
   */
  AgoraFaceShapeStyleFemale = 0,
  /**
   * 1: Masculine style.
   */
  AgoraFaceShapeStyleMale = 1,
  /**
   * 2: The natural style beauty effect.
   * @since v4.6.0
   */
  AgoraFaceShapeStyleNatural = 2,
};

/**
 * @brief Video noise reduction mode.
 *
 * @since v4.0.0
 */
typedef NS_ENUM(NSUInteger, AgoraVideoDenoiserMode) {
  /**
   * 0: (Default) Automatic mode. The SDK automatically enables or disables the video noise reduction
   * feature according to the ambient light.
   */
  AgoraVideoDenoiserModeAuto = 0,
  /**
   * 1: Manual mode. Users need to enable or disable the video noise reduction feature manually.
   */
  AgoraVideoDenoiserModeManual = 1,
};

/**
 * @brief Video noise reduction level.
 *
 * @since v4.0.0
 */
typedef NS_ENUM(NSUInteger, AgoraVideoDenoiserLevel) {
  /**
   * 0: (Default) Promotes video quality during video noise reduction. balances performance
   * consumption and video noise reduction quality. The performance consumption is moderate, the video
   * noise reduction speed is moderate, and the overall video quality is optimal.
   */
  AgoraVideoDenoiserLevelHighQuality = 0,
  /**
   * 1: Promotes reducing performance consumption during video noise reduction. It prioritizes
   * reducing performance consumption over video noise reduction quality. The performance consumption
   * is lower, and the video noise reduction speed is faster. To avoid a noticeable shadowing effect
   * (shadows trailing behind moving objects) in the processed video, Agora recommends that you use
   * this setting when the camera is fixed.
   */
  AgoraVideoDenoiserLevelFast = 1,
};

/**
 * @brief The low-light enhancement mode.
 *
 * @since v4.0.0
 */
typedef NS_ENUM(NSUInteger, AgoraLowlightEnhanceMode) {
  /**
   * 0: (Default) Automatic mode. The SDK automatically enables or disables the low-light enhancement
   * feature according to the ambient light to compensate for the lighting level or prevent
   * overexposure, as necessary.
   */
  AgoraLowlightEnhanceModeAuto = 0,
  /**
   * 1: Manual mode. Users need to enable or disable the low-light enhancement feature manually.
   */
  AgoraLowlightEnhanceModeManual = 1,
};

/**
 * @brief The low-light enhancement level.
 *
 * @since v4.0.0
 */
typedef NS_ENUM(NSUInteger, AgoraLowlightEnhanceLevel) {
  /**
   * 0: (Default) Promotes video quality during low-light enhancement. It processes the brightness,
   * details, and noise of the video image. The performance consumption is moderate, the processing
   * speed is moderate, and the overall video quality is optimal.
   */
  AgoraLowlightEnhanceLevelQuality = 0,
  /**
   * 1: Promotes performance during low-light enhancement. It processes the brightness and details of
   * the video image. The processing speed is faster.
   */
  AgoraLowlightEnhanceLevelFast = 1,
};

/**
 * @brief The custom background.
 *
 * @since v3.7.200
 */
typedef NS_ENUM(NSUInteger, AgoraVirtualBackgroundSourceType) {
  /**
   * 0: Process the background as alpha data without replacement, only separating the portrait and the
   * background. After setting this value, you can call `startLocalVideoTranscoder:` to implement the
   * picture-in-picture effect.
   */
  AgoraVirtualBackgroundNone = 0,
  /**
   * 1: (Default) The background image is a solid color.
   */
  AgoraVirtualBackgroundColor = 1,
  /**
   * 2: The background is an image in PNG or JPG format.
   */
  AgoraVirtualBackgroundImg = 2,
  /**
   * 3: The background is a blurred version of the original background.
   */
  AgoraVirtualBackgroundBlur = 3,
  /**
   * 4: The background is a local video in MP4, AVI, MKV, FLV, or other supported formats.
   */
  AgoraVirtualBackgroundVideo = 4,
} NS_SWIFT_NAME(AgoraVirtualBackgroundSourceType);

/**
 * @brief The degree of blurring applied to the custom background image.
 */
typedef NS_ENUM(NSUInteger, AgoraBlurDegree) {
  /**
   * 1: The degree of blurring applied to the custom background image is low. The user can almost see
   * the background clearly.
   */
  AgoraBlurLow = 1,
  /**
   * 2: The degree of blurring applied to the custom background image is medium. It is difficult for
   * the user to recognize details in the background.
   */
  AgoraBlurMedium = 2,
  /**
   * 3: (Default) The degree of blurring applied to the custom background image is high. The user can
   * barely see any distinguishing features in the background.
   */
  AgoraBlurHigh = 3,
} NS_SWIFT_NAME(AgoraBlurDegree);

/**
 * @brief The type of algorithms to user for background processing.
 */
typedef NS_ENUM(NSUInteger, SegModelType) {
  /**
   * 1: (Default) Use the algorithm suitable for all scenarios.
   */
  SegModelAgoraAi = 1,
  /**
   * 2: Use the algorithm designed specifically for scenarios with a green screen background.
   */
  SegModelAgoraGreen = 2,
} NS_SWIFT_NAME(SegModelType);

/**
 * @brief Screen color type.
 */
typedef NS_ENUM(NSUInteger, ScreenColorType) {
  /**
   * (0): Automatically selects the screen color type.
   */
  ScreenColorAuto = 0,
  /**
   * (1): Uses green as the screen color type.
   */
  ScreenColorGreen = 1,
  /**
   * (2): Uses blue as the screen color type.
   */
  ScreenColorBlue = 2,
} NS_SWIFT_NAME(ScreenColorType);

/**
 * @brief Reasons causing the change of the connection state.
 */
typedef NS_ENUM(NSInteger, AgoraConnectionChangedReason) {
  /**
   * 0: The SDK is connecting to the Agora edge server.
   */
  AgoraConnectionChangedReasonConnecting = 0,
  /**
   * 1: The SDK has joined the channel successfully.
   */
  AgoraConnectionChangedReasonJoinSuccess = 1,
  /**
   * 2: The connection between the SDK and the Agora edge server is interrupted.
   */
  AgoraConnectionChangedReasonInterrupted = 2,
  /**
   * 3: The connection between the SDK and the Agora edge server is banned by the Agora edge server.
   * For example, when a user is kicked out of the channel, this status will be returned.
   */
  AgoraConnectionChangedReasonBannedByServer = 3,
  /**
   * 4: The SDK fails to join the channel. When the SDK fails to join the channel for more than 20
   * minutes, this code will be returned and the SDK stops reconnecting to the channel. You need to
   * prompt the user to try to switch to another network and rejoin the channel.
   */
  AgoraConnectionChangedReasonJoinFailed = 4,
  /**
   * 5: The SDK has left the channel.
   */
  AgoraConnectionChangedReasonLeaveChannel = 5,
  /**
   * 6: The App ID is invalid. You need to rejoin the channel with a valid APP ID and make sure the
   * App ID you are using is consistent with the one generated in the Agora Console.
   */
  AgoraConnectionChangedReasonInvalidAppId = 6,
  /**
   * 7: Invalid channel name. Rejoin the channel with a valid channel name. A valid channel name is a
   * string of up to 64 bytes in length. Supported characters (89 characters in total):
   * - All lowercase English letters: a to z.
   * - All uppercase English letters: A to Z.
   * - All numeric characters: 0 to 9.
   * - "!", "#", "$", "%", "&", "(", ")", "+", "-", ":", ";", "<", "=", ".", ">", "?", "@", "[", "]",
   * "^", "_", "{", "}", "|", "~", ","
   */
  AgoraConnectionChangedReasonInvalidChannelName = 7,
  /**
   * 8: Invalid token. Possible reasons are as follows:
   * - The App Certificate for the project is enabled in Agora Console, but you do not pass in a token
   * when joining a channel.
   * - The uid specified when calling `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to
   * join the channel is inconsistent with the uid passed in when generating the token.
   * - The generated token and the token used to join the channel are not consistent.
   * Ensure the following:
   * - When your project enables App Certificate, you need to pass in a token to join a channel.
   * - The user ID specified when generating the token is consistent with the user ID used when
   * joining the channel.
   * - The generated token is the same as the token passed in to join the channel.
   */
  AgoraConnectionChangedReasonInvalidToken = 8,
  /**
   * 9: The token currently being used has expired. You need to generate a new token on your server
   * and rejoin the channel with the new token.
   */
  AgoraConnectionChangedReasonTokenExpired = 9,
  /**
   * 10: The connection is rejected by server. Possible reasons are as follows:
   * - The user is already in the channel and still calls a method, for example,
   * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:`, to join the channel. Stop calling
   * this method to clear this error.
   * - The user tries to join a channel while a test call is in progress. The user needs to join the
   * channel after the call test ends.
   */
  AgoraConnectionChangedReasonRejectedByServer = 10,
  /**
   * 11: The connection state changed to reconnecting because the SDK has set a proxy server.
   */
  AgoraConnectionChangedReasonSettingProxyServer = 11,
  /**
   * 12: The connection state changed because the token is renewed.
   */
  AgoraConnectionChangedReasonRenewToken = 12,
  /**
   * 13: Client IP address changed. If you receive this code multiple times, You need to prompt the
   * user to switch networks and try joining the channel again.
   */
  AgoraConnectionChangedReasonClientIpAddressChanged = 13,
  /**
   * 14: Timeout for the keep-alive of the connection between the SDK and the Agora edge server. The
   * SDK tries to reconnect to the server automatically.
   */
  AgoraConnectionChangedReasonKeepAliveTimeout = 14,
  /**
   * 15: The user has rejoined the channel successfully.
   */
  AgoraConnectionChangedReasonRejoinSuccess = 15,
  /**
   * 16: The connection between the SDK and the server is lost.
   */
  AgoraConnectionChangedReasonLost = 16,
  /**
   * 17: The connection state changes due to the echo test.
   */
  AgoraConnectionChangedReasonEchoTest = 17,
  /**
   * 18: The local IP address was changed by the user.
   */
  AgoraConnectionChangedClientIpAddressChangedByUser = 18,
  /**
   * 19: The user joined the same channel from different devices with the same UID.
   */
  AgoraConnectionChangedSameUidLogin = 19,
  /**
   * 20: The number of hosts in the channel has reached the upper limit.
   */
  AgoraConnectionChangedTooManyBroadcasters = 20,
  /**
   * 21: The connection is failed due to license validation failure.
   */
  AgoraConnectionChangedLicenseValidationFailure = 21,
  /**
   * 22: The connection is failed due to certification verify failure.
   */
  AgoraConnectionChangedCertificationVerifyFailure = 22,
};

/**
 * @brief The reason for a user role switch failure.
 */
typedef NS_ENUM(NSInteger, AgoraClientRoleChangeFailedReason) {
  /**
   * 1: The number of hosts in the channel exceeds the limit.
   * @note This enumerator is reported only when the support for 128 users is enabled. The maximum
   * number of hosts is based on the actual number of hosts configured when you enable the 128-user
   * feature.
   */
  AgoraClientRoleChangeFailedTooManyBroadcasters = 1,
  /**
   * 2: The request is rejected by the Agora server. Agora recommends you prompt the user to try to
   * switch their user role again.
   */
  AgoraClientRoleChangeFailedNotAuthorized = 2,
  /**
   * 3: The request is timed out. Agora recommends you prompt the user to check the network connection
   * and try to switch their user role again.
   */
  AgoraClientRoleChangeFailedRequestTimeout = 3,
  /**
   * 4: The SDK is disconnected from the Agora edge server. You can troubleshoot the failure through
   * the `reason` reported by `rtcEngine:connectionChangedToState:reason:`.
   */
  AgoraClientRoleChangeFailedConnectionFailed = 4,
};

/**
 * @brief The playback state of the music file.
 */
typedef NS_ENUM(NSInteger, AgoraAudioMixingStateType) {
  /**
   * 710: The music file is playing.
   */
  AgoraAudioMixingStateTypePlaying = 710,
  /**
   * 711: The music file pauses playing.
   */
  AgoraAudioMixingStateTypePaused = 711,
  /**
   * 713: The music file stops playing.
   * The possible reasons include:
   * - AgoraAudioMixingReasonAllLoopsCompleted (723)
   * - AgoraAudioMixingReasonStoppedByUser (724)
   */
  AgoraAudioMixingStateTypeStopped = 713,
  /**
   * 714: An error occurs during the playback of the audio mixing file.
   * The possible reasons include:
   * - AgoraAudioMixingReasonCanNotOpen (701)
   * - AgoraAudioMixingReasonTooFrequentlyCall (702)
   * - AgoraAudioMixingReasonInterruptedEOF (703)
   */
  AgoraAudioMixingStateTypeFailed = 714,
};

/**
 * @brief The reason why the playback state of the music file changes. Reported in the
 * `rtcEngine:audioMixingStateChanged:reasonCode:` callback.
 */
typedef NS_ENUM(NSInteger, AgoraAudioMixingReasonCode) {
  /**
   * 701: The SDK cannot open the music file. For example, the local music file does not exist, the
   * SDK does not support the file format, or the the SDK cannot access the music file URL.
   */
  AgoraAudioMixingReasonCanNotOpen = 701,
  /**
   * 702: The SDK opens the music file too frequently. If you need to call `startAudioMixing` multiple
   * times, ensure that the call interval is more than 500 ms.
   */
  AgoraAudioMixingReasonTooFrequentlyCall = 702,
  /**
   * 703: The music file playback is interrupted.
   */
  AgoraAudioMixingReasonInterruptedEOF = 703,
  /**
   * 721: The music file completes a loop playback.
   */
  AgoraAudioMixingReasonOneLoopCompleted = 721,
  /**
   * 723: The music file completes all loop playback.
   */
  AgoraAudioMixingReasonAllLoopsCompleted = 723,
  /**
   * 724: Successfully call `stopAudioMixing` to stop playing the music file.
   */
  AgoraAudioMixingReasonStoppedByUser = 724,
  /**
   * 726: The audio mixing playback has resumed by user
   */
  AgoraAudioMixingReasonResumedByUser = 726,
  /**
   * 0: The SDK opens music file successfully.
   */
  AgoraAudioMixingReasonTypeOk = 0,
};

/**
 * @brief Metadata type of the observer. We only support video metadata for now.
 */
typedef NS_ENUM (NSInteger, AgoraMetadataType) {
  /**
   * -1: The type of metadata is unknown.
   */
  AgoraMetadataTypeUnknown = -1,
  /**
   * 0: The type of metadata is video.
   */
  AgoraMetadataTypeVideo = 0
};

/**
 * The maximum metadata size.
 */
typedef NS_ENUM (NSInteger, AgoraMaxMetadataSizeType) {
  AgoraMaxMetadataSizeTypeInvalid = -1,
  AgoraMaxMetadataSizeTypeDefault = 512,
  AgoraMaxMetadataSizeTypeMax = 1024
};

/**
 * @brief The output log level of the SDK.
 */
typedef NS_ENUM (NSInteger, AgoraLogLevel) {
  /**
   * 0: Do not output any log information.
   */
  AgoraLogLevelNone = 0x0000,
  /**
   * 0x0001: (Default) Output `FATAL`, `ERROR`, `WARN`, and `INFO` level log information. We recommend
   * setting your log filter to this level.
   */
  AgoraLogLevelInfo = 0x0001,
  /**
   * 0x0002: Output `FATAL`, `ERROR`, and `WARN` level log information.
   */
  AgoraLogLevelWarn = 0x0002,
  /**
   * 0x0004: Output `FATAL` and `ERROR` level log information.
   */
  AgoraLogLevelError = 0x0004,
  /**
   * 0x0008: Output `FATAL` level log information.
   */
  AgoraLogLevelFatal = 0x0008
};

/**
 * @brief The region for connection, which is the region where the server the SDK connects to is
 * located.
 */
typedef NS_ENUM(NSUInteger, AgoraAreaCodeType) {
  /**
   * Mainland China.
   */
  AgoraAreaCodeTypeCN = 0x1,
  /**
   * North America.
   */
  AgoraAreaCodeTypeNA = 0x2,
  /**
   * Europe.
   */
  AgoraAreaCodeTypeEUR = 0x4,
  /**
   * Asia, excluding Mainland China.
   */
  AgoraAreaCodeTypeAS = 0x8,
  /**
   * Japan.
   */
  AgoraAreaCodeTypeJP = 0x10,
  /**
   * India.
   */
  AgoraAreaCodeTypeIN = 0x20,
  /**
   * Russia
   */
  AgoraAreaCodeTypeRU = 0x1000,
  /**
   * Global.
   */
  AgoraAreaCodeTypeGlobal = 0xFFFFFFFF
};

/**
 * @brief States of the Media Push.
 */
typedef NS_ENUM(NSUInteger, AgoraRtmpStreamingState) {
  /**
   * 0: The Media Push has not started or has ended.
   */
  AgoraRtmpStreamingStateIdle = 0,
  /**
   * 1: The streaming server and CDN server are being connected.
   */
  AgoraRtmpStreamingStateConnecting = 1,
  /**
   * 2: The RTMP or RTMPS streaming publishes. The SDK successfully publishes the RTMP or RTMPS
   * streaming and returns this state.
   */
  AgoraRtmpStreamingStateRunning = 2,
  /**
   * 3: The RTMP or RTMPS streaming is recovering. When exceptions occur to the CDN, or the streaming
   * is interrupted, the SDK tries to resume RTMP or RTMPS streaming and returns this state.
   * - If the SDK successfully resumes the streaming, AgoraRtmpStreamingStateRunning (2) returns.
   * - If the streaming does not resume within 60 seconds or server errors occur,
   * AgoraRtmpStreamingStateFailure (4) returns. If you feel that 60 seconds is too long, you can also
   * actively try to reconnect.
   */
  AgoraRtmpStreamingStateRecovering = 3,
  /**
   * 4: The RTMP or RTMPS streaming fails. After a failure, you can troubleshoot the cause of the
   * error through the returned error code.
   */
  AgoraRtmpStreamingStateFailure = 4,
  /**
   * 5: The SDK is disconnecting from the Agora streaming server and CDN. When you call
   * `stopRtmpStream:` to stop the Media Push normally, the SDK reports the Media Push state as
   * `AgoraRtmpStreamingStateDisconnecting` and `AgoraRtmpStreamingStateIdle` in sequence.
   */
  AgoraRtmpStreamingStateDisconnecting = 5,
};

/**
 * @brief Reasons for changes in the status of RTMP or RTMPS streaming.
 */
typedef NS_ENUM(NSUInteger, AgoraRtmpStreamingReason) {
  /**
   * 0: The RTMP or RTMPS streaming has not started or has ended.
   */
  AgoraRtmpStreamingReasonOK = 0,
  /**
   * 1: Invalid argument used. Check the parameter setting.
   */
  AgoraRtmpStreamingReasonInvalidParameters = 1,
  /**
   * 2: The RTMP or RTMPS streaming is encrypted and cannot be published.
   */
  AgoraRtmpStreamingReasonEncryptedStreamNotAllowed = 2,
  /**
   * 3: Timeout for the RTMP or RTMPS streaming.
   */
  AgoraRtmpStreamingReasonConnectionTimeout = 3,
  /**
   * 4: An error occurs in Agora's streaming server.
   */
  AgoraRtmpStreamingReasonInternalServerError = 4,
  /**
   * 5: An error occurs in the CDN server.
   */
  AgoraRtmpStreamingReasonRtmpServerError = 5,
  /**
   * 6: The RTMP or RTMPS streaming publishes too frequently.
   */
  AgoraRtmpStreamingReasonTooOften = 6,
  /**
   * 7: The host publishes more than 10 URLs. Delete the unnecessary URLs before adding new ones.
   */
  AgoraRtmpStreamingReasonReachLimit = 7,
  /**
   * 8: The host manipulates other hosts' URLs. For example, the host updates or stops other hosts'
   * streams. Check your app logic.
   */
  AgoraRtmpStreamingReasonNotAuthorized = 8,
  /**
   * 9: Agora's server fails to find the RTMP or RTMPS streaming.
   */
  AgoraRtmpStreamingReasonStreamNotFound = 9,
  /**
   * 10: The format of the RTMP or RTMPS streaming URL is not supported. Check whether the URL format
   * is correct.
   */
  AgoraRtmpStreamingReasonFormatNotSupported = 10,
  /**
   * 11: The user role is not host, so the user cannot use the CDN live streaming function. Check your
   * application code logic.
   */
  AgoraRtmpStreamingReasonNotBroadcaster = 11,
  /**
   * 13: The `updateRtmpTranscoding:` method is called to update the transcoding configuration in a
   * scenario where there is streaming without transcoding. Check your application code logic.
   */
  AgoraRtmpStreamingReasonTranscodingNoMixStream = 13,
  /**
   * 14: Errors occurred in the host's network.
   */
  AgoraRtmpStreamingReasonNetDown = 14,
  /** User AppId have not authorized to push stream. */
  AgoraRtmpStreamingReasonInvalidAppId = 15,
  /**
   * 16: Your project does not have permission to use streaming services.
   */
  AgoraRtmpStreamingReasonInvalidPrivilege = 16,
  /**
   * 100: The streaming has been stopped normally. After you stop the Media Push, the SDK returns this
   * value.
   * @since v3.4.5
   */
  AgoraRtmpStreamingReasonUnpublishOK = 100,
};

/**
 * @brief Events during the Media Push.
 */
typedef NS_ENUM(NSUInteger, AgoraRtmpStreamingEvent) {
  /**
   * 1: An error occurs when you add a background image or a watermark image in the Media Push.
   */
  AgoraRtmpStreamingEventFailedLoadImage = 1,
  /**
   * 2: The streaming URL is already being used for Media Push. If you want to start new streaming,
   * use a new streaming URL.
   * @since v3.4.5
   */
  AgoraRtmpStreamingEventUrlAlreadyInUse = 2,
  /**
   * 3: The feature is not supported.
   */
  AgoraRtmpStreamingEventAdvancedFeatureNotSupport = 3,
  /**
   * 4: Reserved.
   */
  AgoraRtmpStreamingEventRequestTooOften = 4,
};

/**
 * @brief Encryption error type.
 */
typedef NS_ENUM(NSInteger, AgoraEncryptionErrorType) {
  /**
   * 0: Internal reason.
   */
  ENCRYPTION_ERROR_INTERNAL_FAILURE = 0,
  /**
   * 1: Media stream decryption error. Ensure that the receiver and the sender use the same encryption
   * mode and key.
   */
  ENCRYPTION_ERROR_DECRYPTION_FAILURE = 1,
  /**
   * 2: Media stream encryption error.
   */
  ENCRYPTION_ERROR_ENCRYPTION_FAILURE = 2,
  /**
   * 3: Data stream decryption error. Ensure that the receiver and the sender use the same encryption
   * mode and key.
   */
  ENCRYPTION_ERROR_DATASTREAM_DECRYPTION_FAILURE = 3,
  /**
   * 4: Data stream encryption error.
   */
  ENCRYPTION_ERROR_DATASTREAM_ENCRYPTION_FAILURE = 4,

};

/**
 * @brief The camera direction.
 */
typedef NS_ENUM(NSUInteger, AgoraCameraDirection) {
  /**
   * 0: The rear camera.
   */
  AgoraCameraDirectionRear = 0,
  /**
   * 1: (Default) The front camera.
   */
  AgoraCameraDirectionFront = 1,
};

/**
 * @brief The camera focal length types.
 */
typedef NS_ENUM(NSInteger, AgoraFocalLength) {
  /**
   * 0: (Default) Standard lens.
   */
  AgoraFocalLengthDefault = 0,
  /**
   * 1: Wide-angle lens.
   */
  AgoraFocalLengthWide = 1,
  /**
   * 2: Ultra-wide-angle lens.
   */
  AgoraFocalLengthUltraWide = 2,
  /**
   * 3: Telephoto lens.
   */
  AgoraFocalLengthTelephoto = 3,
};

/**
 * @brief Camera stabilization modes.
 *
 * @details
 * The camera stabilization effect increases in the order of 1 < 2 < 3, and the latency will also
 * increase accordingly.
 *
 */
typedef NS_ENUM(NSInteger, AgoraCameraStabilizationMode) {
  /**
   * -1: (Default) Camera stabilization mode off.
   */
  AgoraCameraStabilizationModeOff = -1,
  /**
   * 0: Automatic camera stabilization. The system automatically selects a stabilization mode based on
   * the status of the camera. However, the latency is relatively high in this mode, so it is
   * recommended not to use this enumeration.
   */
  AgoraCameraStabilizationModeAuto = 0,
  /**
   * 1: (Recommended) Level 1 camera stabilization.
   */
  AgoraCameraStabilizationModeLevel1 = 1,
  /**
   * 2: Level 2 camera stabilization.
   */
  AgoraCameraStabilizationModeLevel2 = 2,
  /**
   * 3: Level 3 camera stabilization.
   */
  AgoraCameraStabilizationModeLevel3 = 3,
};

/**
 * @brief The state code of the channel media relay.
 */
typedef NS_ENUM(NSInteger, AgoraChannelMediaRelayState) {
  /**
   * 0: The initial state. After you successfully stop the channel media relay by calling
   * `stopChannelMediaRelay`, the `rtcEngine:channelMediaRelayStateDidChange:error:` callback returns
   * this state.
   */
  AgoraChannelMediaRelayStateIdle = 0,
  /**
   * 1: The SDK tries to relay the media stream to the destination channel.
   */
  AgoraChannelMediaRelayStateConnecting = 1,
  /**
   * 2: The SDK successfully relays the media stream to the destination channel.
   */
  AgoraChannelMediaRelayStateRunning = 2,
  /**
   * 3: An error occurs. See `code` in `rtcEngine:channelMediaRelayStateDidChange:error:` for the
   * error code.
   */
  AgoraChannelMediaRelayStateFailure = 3,
};

/** The event code in AgoraChannelMediaRelayEvent.
 */
typedef NS_ENUM(NSInteger, AgoraChannelMediaRelayEvent) {
  /** 0: The user disconnects from the server due to poor network connections.
   */
  AgoraChannelMediaRelayEventDisconnect = 0,
  /** 1: The network reconnects.
   */
  AgoraChannelMediaRelayEventConnected = 1,
  /** 2: The user joins the source channel.
   */
  AgoraChannelMediaRelayEventJoinedSourceChannel = 2,
  /** 3: The user joins the destination channel.
   */
  AgoraChannelMediaRelayEventJoinedDestinationChannel = 3,
  /** 4: The SDK starts relaying the media stream to the destination channel.
   */
  AgoraChannelMediaRelayEventSentToDestinationChannel = 4,
  /** 5: The server receives the video stream from the source channel.
   */
  AgoraChannelMediaRelayEventReceivedVideoPacketFromSource = 5,
  /** 6: The server receives the audio stream from the source channel.
   */
  AgoraChannelMediaRelayEventReceivedAudioPacketFromSource = 6,
  /** 7: The destination channel is updated.
   */
  AgoraChannelMediaRelayEventUpdateDestinationChannel = 7,
  /** 8: The destination channel update fails due to internal reasons.
   */
  AgoraChannelMediaRelayEventUpdateDestinationChannelRefused = 8,
  /** 9: The destination channel does not change, which means that the destination channel fails to be updated.
   */
  AgoraChannelMediaRelayEventUpdateDestinationChannelNotChange = 9,
  /** 10: The destination channel name is NULL.
   */
  AgoraChannelMediaRelayEventUpdateDestinationChannelIsNil = 10,
  /** 11: The video profile is sent to the server.
   */
  AgoraChannelMediaRelayEventVideoProfileUpdate = 11,
};

/**
 * @brief The error code of the channel media relay.
 */
typedef NS_ENUM(NSInteger, AgoraChannelMediaRelayError) {
  /**
   * 0: No error.
   */
  AgoraChannelMediaRelayErrorNone = 0,
  /**
   * 1: An error occurs in the server response.
   */
  AgoraChannelMediaRelayErrorServerErrorResponse = 1,
  /**
   * 2: No server response.
   * This error may be caused by poor network connections. If this error occurs when initiating a
   * channel media relay, you can try again later; if this error occurs during channel media relay,
   * you can call `leaveChannel:leaveChannelBlock:` to leave the channel.
   * This error can also occur if the channel media relay service is not enabled in the project. You
   * can contact `technical support` to enable the service.
   */
  AgoraChannelMediaRelayErrorServerNoResponse = 2,
  /**
   * 3: The SDK fails to access the service, probably due to limited resources of the server.
   */
  AgoraChannelMediaRelayErrorNoResourceAvailable = 3,
  /**
   * 4: Fails to send the relay request.
   */
  AgoraChannelMediaRelayErrorFailedJoinSourceChannel = 4,
  /**
   * 5: Fails to accept the relay request.
   */
  AgoraChannelMediaRelayErrorFailedJoinDestinationChannel = 5,
  /**
   * 6: The server fails to receive the media stream.
   */
  AgoraChannelMediaRelayErrorFailedPacketReceivedFromSource = 6,
  /**
   * 7: The server fails to send the media stream.
   */
  AgoraChannelMediaRelayErrorFailedPacketSentToDestination = 7,
  /**
   * 8: The SDK disconnects from the server due to poor network connections. You can call
   * `leaveChannel:leaveChannelBlock:` to leave the channel.
   */
  AgoraChannelMediaRelayErrorServerConnectionLost = 8,
  /**
   * 9: An internal error occurs in the server.
   */
  AgoraChannelMediaRelayErrorInternalError = 9,
  /**
   * 10: The token of the source channel has expired.
   */
  AgoraChannelMediaRelayErrorSourceTokenExpired = 10,
  /**
   * 11: The token of the destination channel has expired.
   */
  AgoraChannelMediaRelayErrorDestinationTokenExpired = 11,
};

/**
 * @brief The subscribing state.
 */
typedef NS_ENUM (NSInteger, AgoraStreamSubscribeState) {
  /**
   * 0: The initial publishing state after joining the channel.
   */
  AgoraStreamSubscribeStateIdle = 0,
  /**
   * 1: Fails to subscribe to the remote stream. Possible reasons:
   * - The remote user:
   *   - Calls `muteLocalAudioStream:` (`YES`) or `muteLocalVideoStream:` (`YES`) to stop sending
   * local media stream.
   *   - Calls `disableAudio` or `disableVideo` to disable the local audio or video module.
   *   - Calls `enableLocalAudio:` ( NO ) or `enableLocalVideo:` ( NO ) to disable local audio or
   * video capture.
   *   - The role of the remote user is audience.
   * - The local user calls the following methods to stop receiving remote streams:
   *   - Call `muteRemoteAudioStream:mute:` ( YES ) or `muteAllRemoteAudioStreams:` ( YES ) to stop
   * receiving the remote audio stream.
   *   - Call `muteRemoteVideoStream:mute:` ( YES ) or `muteAllRemoteVideoStreams:` ( YES ) to stop
   * receiving the remote video stream.
   */
  AgoraStreamSubscribeStateNoSubscribed = 1,
  /**
   * 2: Subscribing.
   */
  AgoraStreamSubscribeStateSubscribing = 2,
  /**
   * 3: The remote stream is received, and the subscription is successful.
   */
  AgoraStreamSubscribeStateSubscribed = 3
};

/**
 * @brief The publishing state.
 */
typedef NS_ENUM (NSInteger, AgoraStreamPublishState) {
  /**
   * 0: The initial publishing state after joining the channel.
   */
  AgoraStreamPublishStateIdle = 0,
  /**
   * 1: Fails to publish the local stream. Possible reasons:
   * - The local user calls `muteLocalAudioStream:` (`YES`) or `muteLocalVideoStream:` (`YES`) to stop
   * sending local media streams.
   * - The local user calls `disableAudio` or `disableVideo` to disable the local audio or video
   * module.
   * - The local user calls `enableLocalAudio:` (`NO`) or `enableLocalVideo:` (`NO`) to disable the
   * local audio or video capture.
   * - The role of the local user is audience.
   */
  AgoraStreamPublishStateNoPublished = 1,
  /**
   * 2: Publishing.
   */
  AgoraStreamPublishStatePublishing = 2,
  /**
   * 3: Publishes successfully.
   */
  AgoraStreamPublishStatePublished = 3
};
/**
 * @brief The video application scenarios.
 *
 * @since v4.2.0
 */
typedef NS_ENUM(NSInteger, AgoraApplicationScenarioType) {
  /**
   * 0: (Default) The general scenario.
   */
  AgoraApplicationGeneralScenario = 0,
  /**
   * 1: The meeting scenario.
   * `AgoraApplicationMeetingScenario` (1) is suitable for meeting scenarios. The SDK automatically
   * enables the following strategies:
   * - In meeting scenarios where low-quality video streams are required to have a high bitrate, the
   * SDK automatically enables multiple technologies used to deal with network congestions, to enhance
   * the performance of the low-quality streams and to ensure the smooth reception by subscribers.
   * - The SDK monitors the number of subscribers to the high-quality video stream in real time and
   * dynamically adjusts its configuration based on the number of subscribers.
   *   - If nobody subscribers to the high-quality stream, the SDK automatically reduces its bitrate
   * and frame rate to save upstream bandwidth.
   *   - If someone subscribes to the high-quality stream, the SDK resets the high-quality stream to
   * the `AgoraVideoEncoderConfiguration` configuration used in the most recent calling of
   * `setVideoEncoderConfiguration:`. If no configuration has been set by the user previously, the
   * following values are used:
   *      - On iOS:
   *        - Resolution: 960 × 540
   *        - Frame rate: 15 fps
   *        - Bitrate: 1000 Kbps
   *      - On macOS:
   *        - Resolution: 1280 × 720
   *        - Frame rate: 15 fps
   *        - Bitrate: 1600 Kbps
   * - The SDK monitors the number of subscribers to the low-quality video stream in real time and
   * dynamically enables or disables it based on the number of subscribers.
   *   - If nobody subscribes to the low-quality stream, the SDK automatically disables it to save
   * upstream bandwidth.
   *   - If someone subscribes to the low-quality stream, the SDK enables the low-quality stream and
   * resets it to the `AgoraSimulcastStreamConfig` configuration used in the most recent calling of
   * `setDualStreamMode:streamConfig:`. If no configuration has been set by the user previously, the
   * following values are used:
   *     - Resolution: 480 × 272
   *     - Frame rate: 15 fps
   *     - Bitrate: 500 Kbps
   * @note If the user has called `setDualStreamMode:streamConfig:` to set that never send low-quality
   * video stream ( `AgoraDisableSimulcastStream` ), the dynamic adjustment of the low-quality stream
   * in meeting scenarios will not take effect.
   */
  AgoraApplicationMeetingScenario = 1,
  /**
   * 2: 1v1 video call scenario.
   * `AgoraApplication1V1Scenario` (2) This is applicable to the `one to one live` scenario. To meet
   * the requirements for low latency and high-quality video in this scenario, the SDK optimizes its
   * strategies, improving performance in terms of video quality, first frame rendering, latency on
   * mid-to-low-end devices, and smoothness under weak network conditions.
   * @note This enumeration value is only applicable to the broadcaster vs. broadcaster scenario.
   */
  AgoraApplication1V1Scenario = 2,
  /**
   * 3. Live show scenario.
   * `AgoraApplicationLiveShowScenario` (3) This is applicable to the `show room` scenario. In this
   * scenario, fast video rendering and high image quality are crucial. The SDK implements several
   * performance optimizations, including automatically enabling accelerated audio and video frame
   * rendering to minimize first-frame latency (no need to call `enableInstantMediaRendering` ), and
   * B-frame encoding to achieve better image quality and bandwidth efficiency. The SDK also provides
   * enhanced video quality and smooth playback, even in poor network conditions or on lower-end
   * devices.
   */
  AgoraApplicationLiveShowScenario = 3,
};

/**
 * The video qoe preference enum type.
 *
 * @since v4.2.0
 */
typedef NS_ENUM(NSInteger, AgoraQoEPreferenceType) {
  /**
   * 0: Default QoE type, balance the delay, picture quality and fluency.
   */
  AgoraVideoQOEPreferenceBalance = 1,
  /**
   * 1: lower the e2e delay.
   */
  AgoraVideoQOEPreferenceDelayFirst = 2,
  /**
   * 2: picture quality.
   */
  AgoraVideoQOEPreferencePictureQualityFirst = 3,
  /**
   * 3: more fluency.
   */
  AgoraVideoQOEPreferenceFluencyFirst = 4,
};

/**
 * @brief The screen sharing scenario.
 *
 * @since v4.0.0
 */
typedef NS_ENUM(NSInteger, AgoraScreenScenarioType) {
  /**
   * 1: (Default) Document. This scenario prioritizes the video quality of screen sharing and reduces
   * the latency of the shared video for the receiver. If you share documents, slides, and tables, you
   * can set this scenario.
   */
  AgoraScreenScenarioDocument = 1,
  /**
   * 2: Game. This scenario prioritizes the smoothness of screen sharing. If you share games, you can
   * set this scenario.
   */
  AgoraScreenScenarioGaming = 2,
  /**
   * 3: Video. This scenario prioritizes the smoothness of screen sharing. If you share movies or live
   * videos, you can set this scenario.
   */
  AgoraScreenScenarioVideo = 3,
  /**
   * 4: Remote control. This scenario prioritizes the video quality of screen sharing and reduces the
   * latency of the shared video for the receiver. If you share the device desktop being remotely
   * controlled, you can set this scenario.
   */
  AgoraScreenScenarioRDC = 4,
};
/**
 * @brief Media source type.
 */
typedef NS_ENUM(NSInteger, AgoraMediaSourceType) {
  /**
   * 0: Audio playback device.
   */
  AgoraMediaSourceTypeAudioPlayout = 0,
  /**
   * 1: Audio capturing device.
   */
  AgoraMediaSourceTypeRecording = 1,
  /**
   * 2: The primary camera.
   */
  AgoraMediaSourceTypePrimaryCamera = 2,
  /**
   * 3: A secondary camera.
   */
  AgoraMediaSourceTypeSecondaryCamera = 3,
  /**
   * 4: Video captured by primary screen capturer.
   */
  AgoraMediaSourceTypePrimaryScreen = 4,
  /**
   * 5: Video captured by secondary screen capturer.
   */
  AgoraMediaSourceTypeSecondaryScreen = 5,
  /**
   * 6: Custom video source.
   */
  AgoraMediaSourceTypeCustomVideo = 6,
  /**
   * 7: Video for media player sharing.
   */
  AgoraMediaSourceTypeMediaPlayer = 7,
  /**
   * 8: Video for png image.
   */
  AgoraMediaSourceTypeRtcImagePng = 8,
  /**
   * 9: Video for jpeg image.
   */
  AgoraMediaSourceTypeRtcImageJpeg = 9,
  /**
   * 10: Video for gif image.
   */
  AgoraMediaSourceTypeRtcImageGif = 10,
  /**
   * 11: Remote video received from network.
   */
  AgoraMediaSourceTypeRemoteVideo = 11,
  /**
   * 12: Video for transcoded.
   */
  AgoraMediaSourceTypeTranscodedVideo = 12,
  /**
   * 13: Video processed by the speech driven extension.
   */
  AgoraMediaSourceTypeSpeechDriven = 13,
  /**
   * 100: Unknown media source.
   */
  AgoraMediaSourceTypeUnknown = 100
};

/**
 * @brief The audio source type.
 */
typedef NS_ENUM(NSInteger, AgoraAudioSourceType) {
  /**
   * 0: (Default) Microphone.
   */
  AgoraAudioSourceMicrophone = 0,
  /**
   * 1: Custom audio stream.
   */
  AgoraAudioSourceCustom = 1,
  /**
   * 2: Media player.
   */
  AgoraAudioSourceMediaPlayer = 2,
  /**
   * 3: System audio stream captured during screen sharing.
   */
  AgoraAudioSourceLoopbackRecording = 3,
  /** Audio generated by local mixed track.
   */
  AgoraAudioSourceMixedStream = 4,
  /**
   * 5: Audio stream from a specified remote user.
   */
  AgoraAudioSourceRemoteUser = 5,
  /**
   * 6: Mixed audio streams from all users in the current channel.
   */
  AgoraAudioSourceRemoteChannel = 6,
  /**
   * 100: An unknown audio source.
   */
  AgoraAudioSourceTypeUnknown = 100
};


/**
 * @brief The type of the video source.
 */
typedef NS_ENUM(NSInteger, AgoraVideoSourceType) {
  /**
   * 0: (Default) The primary camera.
   */
  AgoraVideoSourceTypeCamera = 0,
  /**
   * 1: The secondary camera.
   */
  AgoraVideoSourceTypeCameraSecondary = 1,
  /**
   * 2: The primary screen.
   */
  AgoraVideoSourceTypeScreen = 2,
  /**
   * 3: The secondary screen.
   */
  AgoraVideoSourceTypeScreenSecondary = 3,
  /**
   * 4: A custom video source.
   */
  AgoraVideoSourceTypeCustom = 4,
  /**
   * 5: The media player.
   */
  AgoraVideoSourceTypeMediaPlayer = 5,
  /**
   * 6: One PNG image.
   */
  AgoraVideoSourceTypeImagePNG = 6,
  /**
   * 7: One JPEG image.
   */
  AgoraVideoSourceTypeImageJPEG = 7,
  /**
   * 8: One GIF image.
   */
  AgoraVideoSourceTypeImageGIF = 8,
  /**
   * 9: One remote video acquired by the network.
   */
  AgoraVideoSourceTypeRemote = 9,
  /**
   * 10: One transcoded video source.
   */
  AgoraVideoSourceTypeTransCoded = 10,
  /**
   * 11: (macOS only) The third camera.
   */
  AgoraVideoSourceTypeCameraThird = 11,
  /**
   * 12: (macOS only) The fourth camera.
   */
  AgoraVideoSourceTypeCameraFourth = 12,
  /**
   * 13: (macOS only) The third screen.
   */
  AgoraVideoSourceTypeScreenThird = 13,
  /**
   * 14: (macOS only) The fourth screen.
   */
  AgoraVideoSourceTypeScreenFourth = 14,
  /**
   * 15: Video processed by the speech driven extension.
   */
  AgoraVideoSourceTypeSpeechDriven = 15,
  /**
   * 100: An unknown video source.
   */
  AgoraVideoSourceTypeUnknown = 100
};

/**
 * @brief The external video frame encoding type.
 */
typedef NS_ENUM(NSInteger, AgoraExternalVideoSourceType) {
  /**
   * 0: The video frame is not encoded.
   */
  AgoraExternalVideoSourceTypeVideoFrame = 0,
  /**
   * 1: The video frame is encoded.
   */
  AgoraExternalVideoSourceTypeEncodedVideoFrame,
};

/**
 * @brief The mode in which the video stream is sent.
 */
typedef NS_ENUM(NSInteger, AgoraSimulcastStreamMode) {
  /**
   * -1: By default, do not send the low-quality video stream until a subscription request for the
   * low-quality video stream is received from the receiving end, then automatically start sending
   * low-quality video stream.
   */
  AgoraAutoSimulcastStream = -1,
  /**
   * 0: Never send low-quality video stream.
   */
  AgoraDisableSimulcastStream = 0,
  /**
   * 1: Always send low-quality video stream.
   */
  AgoraEnableSimulcastStream = 1,
};

/**
 * @brief Index of video streams of different quality levels.
 *
 * @since v4.6.0
 */
typedef NS_ENUM(NSInteger, AgoraStreamLayerIndex) {
  /**
   * 0: Video stream layer_1, with lower resolution and bitrate than AgoraVideoStreamTypeHigh.
   */
  AgoraStreamLayer1 = 0,
  /**
   * 1: Video stream layer_2, with lower resolution and bitrate than AgoraVideoStreamTypeLayer1.
   */
  AgoraStreamLayer2 = 1,
  /**
   * 2: Video stream layer_3, with lower resolution and bitrate than AgoraVideoStreamTypeLayer2.
   */
  AgoraStreamLayer3 = 2,
  /**
   * 3: Video stream layer_4, with lower resolution and bitrate than AgoraVideoStreamTypeLayer3.
   */
  AgoraStreamLayer4 = 3,
  /**
   * 4: Video stream layer_5, with lower resolution and bitrate than AgoraVideoStreamTypeLayer4.
   */
  AgoraStreamLayer5 = 4,
  /**
   * 5: Video stream layer_6, with lower resolution and bitrate than AgoraVideoStreamTypeLayer5.
   */
  AgoraStreamLayer6 = 5,
  /**
   * 6: Low-quality video stream, with the lowest resolution and bitrate.
   */
  AgoraStreamLow = 6,
  /**
   * 7: Maximum number of video stream layers.
   */
  AgoraStreamLayerCountMax = 7,
};

/**
 * @brief The type of the media stream.
 */
typedef NS_ENUM(NSInteger, AgoraMediaStreamType) {
  /**
   * 0: The type is unknown.
   */
  AgoraMediaStreamTypeUnknown = 0,
  /**
   * 1: The video stream.
   */
  AgoraMediaStreamTypeVideo = 1,
  /**
   * 2: The audio stream.
   */
  AgoraMediaStreamTypeAudio = 2,
  /**
   * 3: The subtitle stream.
   */
  AgoraMediaStreamTypeSubtitle = 3,
};

/**
 * @brief The playback state.
 */
typedef NS_ENUM(NSInteger, AgoraMediaPlayerState) {
  /**
   * 0: The default state. The media player returns this state code before you open the media resource
   * or after you stop the playback.
   */
  AgoraMediaPlayerStateIdle = 0,
  /**
   * 1: Opening the media resource.
   */
  AgoraMediaPlayerStateOpening,
  /**
   * 2: Opens the media resource successfully.
   */
  AgoraMediaPlayerStateOpenCompleted,
  /**
   * 3: The media resource is playing.
   */
  AgoraMediaPlayerStatePlaying,
  /**
   * 4: Pauses the playback.
   */
  AgoraMediaPlayerStatePaused,
  /**
   * 5: The playback is complete.
   */
  AgoraMediaPlayerStatePlayBackCompleted,
  /**
   * 6: The loop is complete.
   */
  AgoraMediaPlayerStatePlayBackAllLoopsCompleted,
  /**
   * 7: The playback stops.
   */
  AgoraMediaPlayerStateStopped,
  /** Player pausing (internal)
   */
  AgoraMediaPlayerStatePausingInternal = 50,
  /** Player stopping (internal)
   */
  AgoraMediaPlayerStateStoppingInternal,
  /** Player seeking state (internal)
   */
  AgoraMediaPlayerStateSeekingInternal,
  /** Player getting state (internal)
   */
  AgoraMediaPlayerStateGettingInternal,
  /** None state for state machine (internal)
   */
  AgoraMediaPlayerStateNoneInternal,
  /** Do nothing state for state machine (internal)
   */
  AgoraMediaPlayerStateDoNothingInternal,
  /** Player set track state (internal)
   */
  AgoraMediaPlayerStateSetTrackInternal,
  /**
   * 100: The media player fails to play the media resource.
   */
  AgoraMediaPlayerStateFailed = 100,
};

/**
 * @brief Video display modes.
 */
typedef NS_ENUM(NSUInteger, AgoraMediaPlayerRenderMode) {
  /**
   * 1: Uniformly scale the video until one of its dimension fits the boundary (zoomed to fit). Hidden
   * mode. One dimension of the video may have clipped contents.
   */
  AgoraMediaPlayerRenderModeHidden = 1,
  /**
   * 2: Uniformly scale the video until one of its dimension fits the boundary (zoomed to fit). Fit
   * mode. Areas that are not filled due to disparity in the aspect ratio are filled with black.
   */
  AgoraMediaPlayerRenderModeFit = 2,
};

/**
 * @brief Reasons for the changes in the media player status.
 */
typedef NS_ENUM(NSInteger, AgoraMediaPlayerReason) {
  /**
   * 0: No error.
   */
  AgoraMediaPlayerReasonNone = 0,
  /**
   * -1: Invalid arguments.
   */
  AgoraMediaPlayerReasonInvalidArguments = -1,
  /**
   * -2: Internal error.
   */
  AgoraMediaPlayerReasonInternal = -2,
  /**
   * -3: No resource.
   */
  AgoraMediaPlayerReasonNoSource = -3,
  /**
   * -4: Invalid media resource.
   */
  AgoraMediaPlayerReasonInvalidMediaSource = -4,
  /**
   * -5: The media stream type is unknown.
   */
  AgoraMediaPlayerReasonUnknowStreamType = -5,
  /**
   * -6: The object is not initialized.
   */
  AgoraMediaPlayerReasonObjNotInitialized = -6,
  /**
   * -7: The codec is not supported.
   */
  AgoraMediaPlayerReasonCodecNotSupported = -7,
  /**
   * -8: Invalid renderer.
   */
  AgoraMediaPlayerReasonVideoRenderFailed = -8,
  /**
   * -9: An error with the internal state of the player occurs.
   */
  AgoraMediaPlayerReasonInvalidState = -9,
  /**
   * -10: The URL of the media resource cannot be found.
   */
  AgoraMediaPlayerReasonUrlNotFound = -10,
  /**
   * -11: Invalid connection between the player and the Agora Server.
   */
  AgoraMediaPlayerReasonInvalidConnectState = -11,
  /**
   * -12: The playback buffer is insufficient.
   */
  AgoraMediaPlayerReasonSrcBufferUnderflow = -12,
  /**
   * -13: The playback is interrupted.
   */
  AgoraMediaPlayerReasonInterrupted = -13,
  /**
   * -14: The SDK does not support the method being called.
   */
  AgoraMediaPlayerReasonNotSupported = -14,
  /**
   * -15: The authentication information of the media resource is expired.
   */
  AgoraMediaPlayerReasonTokenExpired = -15,
  /** `-16`: The ip has expired.*/
  AgoraMediaPlayerReasonIpExpired = -16,
  /**
   * -17: An unknown error.
   */
  AgoraMediaPlayerReasonUnknown = -17,
};

/**
 * @brief Media player events.
 */
typedef NS_ENUM(NSInteger, AgoraMediaPlayerEvent) {
  /**
   * 0: The player begins to seek to a new playback position.
   */
  AgoraMediaPlayerEventSeekBegin = 0,
  /**
   * 1: The player finishes seeking to a new playback position.
   */
  AgoraMediaPlayerEventSeekComplete = 1,
  /**
   * 2: An error occurs when seeking to a new playback position.
   */
  AgoraMediaPlayerEventSeekError = 2,
  /**
   * 5: The audio track used by the player has been changed.
   */
  AgoraMediaPlayerEventAudioTrackChanged= 5,
  /**
   * 6: The currently buffered data is not enough to support playback.
   */
  AgoraMediaPlayerEventBufferLow = 6,
  /**
   * 7: The currently buffered data is just enough to support playback.
   */
  AgoraMediaPlayerEventBufferRecover = 7,
  /**
   * 8: The audio or video playback freezes.
   */
  AgoraMediaPlayerEventFreezeStart = 8,
  /**
   * 9: The audio or video playback resumes without freezing.
   */
  AgoraMediaPlayerEventFreezeStop = 9,
  /**
   * 10: The player starts switching the media resource.
   */
  AgoraMediaPlayerEventSwitchBegin = 10,
  /**
   * 11: Media resource switching is complete.
   */
  AgoraMediaPlayerEventSwitchComplete = 11,
  /**
   * 12: Media resource switching error.
   */
  AgoraMediaPlayerEventSwitchError = 12,
  /**
   * 13: The first video frame is rendered.
   */
  AgoraMediaPlayerEventFirstDisplayed = 13,
  /**
   * 14: The cached media files reach the limit in number.
   */
  AgoraMediaPlayerEventReachCacheFileMaxCount = 14,
  /**
   * 15: The cached media files reach the limit in aggregate storage space.
   */
  AgoraMediaPlayerEventReachCacheFileMaxSize = 15,
  /** Triggered when a retry is required to open the media*/
  AgoraMediaPlayerEventTryOpenStart = 16,
  /** Triggered when the retry to open the media is successful*/
  AgoraMediaPlayerEventTryOpenSucceed = 17,
  /** Triggered when retrying to open media fails*/
  AgoraMediaPlayerEventTryOpenFailed = 18,
  /** Triggered when an http redirect occurs
   * @technical preview
  */
  AgoraMediaPlayerEventHttpRedirect = 19,
};

/**
 * @brief Events that occur when media resources are preloaded.
 */
typedef NS_ENUM(NSUInteger, AgoraMediaPlayerPreloadEvent) {
  /**
   * 0: Starts preloading media resources.
   */
  AgoraMediaPlayerPreloadEventBegin = 0,
  /**
   * 1: Preloading media resources is complete.
   */
  AgoraMediaPlayerPreloadEventComplete = 1,
  /**
   * 2: An error occurs when preloading media resources.
   */
  AgoraMediaPlayerPreloadEventError = 2,
};

/**
 * @brief The type of media metadata.
 */
typedef NS_ENUM(NSUInteger, AgoraMediaPlayerMetaDataType) {
  /**
   * 0: The type is unknown.
   */
  AgoraMediaPlayerMetaDataTypeUnknown = 0,
  /**
   * 1: The type is SEI.
   */
  AgoraMediaPlayerMetaDataTypeSEI = 1,
};

/**
 * @brief The type of the device permission.
 */
typedef NS_ENUM(NSInteger, AgoraPermissionType) {
  /**
   * 0: Permission for the audio capture device.
   */
  AgoraPermissionTypeRecordAudio = 0,
  /**
   * 1: Permission for the camera.
   */
  AgoraPermissionTypeCamera = 1,
};

/**
 * @brief The current CDN streaming state.
 *
 * @deprecated v4.6.0.
 */
typedef NS_ENUM(NSInteger, AgoraDirectCdnStreamingState) {
  /**
   * 0: The initial state before the CDN streaming starts.
   */
  AgoraDirectCdnStreamingStateIdle = 0,
  /**
   * 1: Streams are being pushed to the CDN. The SDK returns this value when you call the
   * `startDirectCdnStreaming:publishUrl:mediaOptions:` method to push streams to the CDN.
   */
  AgoraDirectCdnStreamingStateRunning = 1,
  /**
   * 2: Stops pushing streams to the CDN. The SDK returns this value when you call the
   * `stopDirectCdnStreaming` method to stop pushing streams to the CDN.
   */
  AgoraDirectCdnStreamingStateStopped = 2,
  /**
   * 3: Fails to push streams to the CDN. You can troubleshoot the issue with the information reported
   * by the `onDirectCdnStreamingStateChanged:reason:message:` callback, and then push streams to the
   * CDN again.
   */
  AgoraDirectCdnStreamingStateFailed = 3,
  /**
   * 4: Tries to reconnect the Agora server to the CDN. The SDK attempts to reconnect a maximum of 10
   * times; if the connection is not restored, the streaming state becomes
   * AgoraDirectCdnStreamingStateFailed.
   */
  AgoraDirectCdnStreamingStateRecovering = 4,
};

/**
 * @brief Reasons for the changes in CDN streaming status.
 *
 * @deprecated v4.6.0.
 */
typedef NS_ENUM(NSInteger, AgoraDirectCdnStreamingReason) {
// No error occurs.
  /**
   * 0: No error.
   */
  AgoraDirectCdnStreamingReasonOK = 0,
// A general error occurs (no specified reason).
  /**
   * 1: A general error; no specific reason. You can try to push the media stream again.
   */
  AgoraDirectCdnStreamingReasonFailed = 1,
// Audio publication error.
  /**
   * 2: An error occurs when pushing audio streams. For example, the local audio capture device is not
   * working properly, is occupied by another process, or does not get the permission required.
   */
  AgoraDirectCdnStreamingReasonAudioPublication = 2,
// Video publication error.
  /**
   * 3: An error occurs when pushing video streams. For example, the local video capture device is not
   * working properly, is occupied by another process, or does not get the permission required.
   */
  AgoraDirectCdnStreamingReasonVideoPublication = 3,
// Network connect error.
  /**
   * 4: Fails to connect to the CDN.
   */
  AgoraDirectCdnStreamingReasonNetConnect = 4,
// Already exist stream name.
  /**
   * 5: The URL is already being used. Use a new URL for streaming.
   */
  AgoraDirectCdnStreamingReasonBadName = 5,
};

/**
 * @brief The audio filter types of in-ear monitoring.
 */
typedef NS_OPTIONS(NSInteger, AgoraEarMonitoringFilterType) {
  /**
   * 1<<0: No audio filter added to in-ear monitoring.
   */
  AgoraEarMonitoringFilterNone = (1<<0),
  /**
   * 1<<1: Add vocal effects audio filter to in-ear monitoring. If you implement functions such as
   * voice beautifier and audio effect, users can hear the voice after adding these effects.
   */
  AgoraEarMonitoringFilterBuiltInAudioFilters = (1<<1),
  /**
   * 1<<2: Add noise suppression audio filter to in-ear monitoring.
   */
  AgoraEarMonitoringFilterNoiseSuppression = (1<<2),
  /**
   * 6: Enable both audio fileter and noise suppression to the in-ear monitor.
   */
  AgoraEarMonitoringFilterBuiltInAudioFiltersWithNoiseSuppression = (1<<1) | (1<<2),

  /**
   * 1<<15: Reuse the audio filter that has been processed on the sending end for in-ear monitoring.
   * This enumerator reduces CPU usage while increasing in-ear monitoring latency, which is suitable
   * for latency-tolerant scenarios requiring low CPU consumption.
   */
  AgoraEarMonitoringFilterReusePostProcessingFilter = (1<<15),
};

/** The bit mask that controls the audio observation positions.
 */
typedef NS_OPTIONS(NSUInteger, AgoraAudioFramePosition) {
  /** The position for observing the playback audio of all remote users after
   mixing, which enables the SDK to trigger the
   [onPlaybackAudioFrame]([AgoraAudioFrameDelegate onPlaybackAudioFrame:])
   callback.
   */
  AgoraAudioFramePositionPlayback = 1 << 0,
  /** The position for observing the recorded audio of the local user, which
   enables the SDK to trigger the
   [onRecordAudioFrame]([AgoraAudioFrameDelegate onRecordAudioFrame:])
   callback.
   */
  AgoraAudioFramePositionRecord = 1 << 1,
  /** The position for observing the mixed audio of the local user and all
   remote users, which enables the SDK to trigger the
   [onMixedAudioFrame]([AgoraAudioFrameDelegate onMixedAudioFrame:])
   callback.
   */
  AgoraAudioFramePositionMixed = 1 << 2,
  /** The position for observing the audio of a single remote user before
   mixing, which enables the SDK to trigger the
   [onPlaybackAudioFrameBeforeMixing]([AgoraAudioFrameDelegate onPlaybackAudioFrameBeforeMixing:uid:])
   callback.
   */
  AgoraAudioFramePositionBeforeMixing = 1 << 3,
  /** The position for observing the ear monitoring audio of the local user
   [onEarMonitoringAudioFrame]([AgoraAudioFrameDelegate onEarMonitoringAudioFrame:]) callback.
   */
  AgoraAudioFramePositionEarMonitoring = 1 << 4,
};

/**
 * @brief The content hint for screen sharing.
 */
typedef NS_ENUM(NSUInteger, AgoraVideoContentHint) {
  /**
   * (Default) No content hint.
   */
  AgoraVideoContentHintNone = 0,
  /**
   * Motion-intensive content. Choose this option if you prefer smoothness or when you are sharing a
   * video clip, movie, or video game.
   */
  AgoraVideoContentHintMotion = 1,
  /**
   * Motionless content. Choose this option if you prefer sharpness or when you are sharing a picture,
   * PowerPoint slides, or texts.
   */
  AgoraVideoContentHintDetails = 2,
};

/** Thread priority type.
 */
typedef NS_ENUM(NSInteger, AgoraThreadPriorityType) {
  /**
   * 0: Lowest priority.
   */
  AgoraThreadPriorityTypeLowest = 0,
  /**
   * 1: Low priority.
   */
  AgoraThreadPriorityTypeLow = 1,
  /**
   * 2: Normal priority.
   */
  AgoraThreadPriorityTypeNormal = 2,
  /**
   * 3: High priority.
   */
  AgoraThreadPriorityTypeHigh = 3,
  /**
   * 4. Highest priority.
   */
  AgoraThreadPriorityTypeHighest = 4,
  /**
   * 5. Critical priority.
   */
  AgoraThreadPriorityTypeCritical = 5,
};

/**
 * @brief The type of video content moderation module.
 */
typedef NS_ENUM(NSUInteger, AgoraContentInspectType) {
    /**
     * 0: (Default) This module has no actual function. Do not set `type` to this value.
     */
    AgoraContentInspectTypeInvalid = 0,
    /**
     * @deprecated
     *  content inspect type moderation
     */
    AgoraContentInspectTypeModeration __deprecated = 1,
    /**
     * 2: Video screenshot and upload via Agora self-developed extension. SDK takes screenshots of the
     * video stream in the channel and uploads them.
     */
    AgoraContentInspectTypeSupervise = 2,
    /**
     * 3: Video screenshot and upload via extensions from Agora Extensions Marketplace. SDK uses video
     * moderation extensions from Agora Extensions Marketplace to take screenshots of the video stream
     * in the channel and uploads them.
     */
    AgoraContentInspectTypeImageModeration = 3,
};

typedef NS_ENUM(NSUInteger, AgoraContentInspectResult) {
  AgoraContentInspectNeutral = 1,
  // background image does not exist
  AgoraContentInspectSexy = 2,
  // color format is not supported
  AgoraContentInspectPorn = 3,
};

/**
 * @brief The channel mode.
 */
typedef NS_ENUM(NSUInteger, AgoraAudioDualMonoMode) {
    /**
     * 0: Original mode.
     */
    AgoraAudioDualMonoStereo,
    /**
     * 1: Left channel mode. This mode replaces the audio of the right channel with the audio of the
     * left channel, which means the user can only hear the audio of the left channel.
     */
    AgoraAudioDualMonoL,
    /**
     * 2: Right channel mode. This mode replaces the audio of the left channel with the audio of the
     * right channel, which means the user can only hear the audio of the right channel.
     */
    AgoraAudioDualMonoR,
    /**
     * 3: Mixed channel mode. This mode mixes the audio of the left channel and the right channel, which
     * means the user can hear the audio of the left channel and the right channel at the same time.
     */
    AgoraAudioDualMonoMix
};

/**
 * @brief The channel mode.
 */
typedef NS_ENUM(NSUInteger, AgoraAudioMixingDualMonoMode) {
    /**
     * 0: Original mode.
     */
    AgoraAudioMixingDualMonoAuto = 0,
    /**
     * 1: Left channel mode. This mode replaces the audio of the right channel with the audio of the
     * left channel, which means the user can only hear the audio of the left channel.
     */
    AgoraAudioMixingDualMonoL = 1,
    /**
     * 2: Right channel mode. This mode replaces the audio of the left channel with the audio of the
     * right channel, which means the user can only hear the audio of the right channel.
     */
    AgoraAudioMixingDualMonoR = 2,
    /**
     * 3: Mixed channel mode. This mode mixes the audio of the left channel and the right channel, which
     * means the user can hear the audio of the left channel and the right channel at the same time.
     */
    AgoraAudioMixingDualMonoMix = 3,
};

/**
 * @brief Virtual metronome state.
 */
typedef NS_ENUM(NSInteger, AgoraRhythmPlayerState) {
  /**
   * 810: The virtual metronome is not enabled or disabled already.
   */
  AgoraRhythmPlayerStateIdle = 810,
  /**
   * 811: Opening the beat files.
   */
  AgoraRhythmPlayerStateOpening,
  /**
   * 812: Decoding the beat files.
   */
  AgoraRhythmPlayerStateDecoding,
  /**
   * 813: The beat files are playing.
   */
  AgoraRhythmPlayerStatePlaying,
  /**
   * 814: Failed to start virtual metronome. You can use the reported `errorCode` to troubleshoot the
   * cause of the error, or you can try to start the virtual metronome again.
   */
  AgoraRhythmPlayerStateFailed,
};

/**
 * @brief Virtual Metronome error message.
 */
typedef NS_ENUM(NSInteger, AgoraRhythmPlayerReason) {
  /**
   * 0: The beat files are played normally without errors.
   */
  AgoraRhythmPlayerReasonOK = 0,
  /**
   * 1: A general error; no specific reason.
   */
  AgoraRhythmPlayerReasonFailed = 1,
  /**
   * 801: There is an error when opening the beat files.
   */
  AgoraRhythmPlayerReasonCanNotOpen = 801,
  /**
   * 802: There is an error when playing the beat files.
   */
  AgoraRhythmPlayerReasonCanNotPlay,
  /**
   * 803: The duration of the beat file exceeds the limit. The maximum duration is 1.2 seconds.
   */
  AgoraRhythmPlayerReasonFileOverDurationLimit,
};

/**
 * @brief The error code of the local video mixing failure.
 */
typedef NS_ENUM(NSInteger, AgoraVideoTranscoderError) {
  /**
   * No error
   */
  AgoraVideoTranscoderErrorOK = 0,
  /**
   * 1: The selected video source has not started video capture. You need to create a video track for
   * it and start video capture.
   */
  AgoraVideoTranscoderErrorVideoSourceNotReady = 1,
  /**
   * 2: The video source type is invalid. You need to re-specify the supported video source type.
   */
  AgoraVideoTranscoderErrorInvalidVideoSourceType = 2,
  /**
   * 3: The image path is invalid. You need to re-specify the correct image path.
   */
  AgoraVideoTranscoderErrorInvalidImagePath = 3,
  /**
   * 4: The image format is invalid. Make sure the image format is one of PNG, JPEG, or GIF.
   */
  AgoraVideoTranscoderErrorUnsupportImageFormat = 4,
  /**
   * 5: The video encoding resolution after video mixing is invalid.
   */
  AgoraVideoTranscoderErrorInvalidLayout = 5,
  /**
   * 20: Unknown internal error.
   */
  AgoraVideoTranscoderErrorInternal = 20
};

/**
 * @brief Connection mode with Agora private media server.
 */
typedef NS_ENUM(NSUInteger, AgoraLocalProxyMode) {
  /**
   * 0: The SDK first attempts to connect to the specified Agora private media server. If it fails to
   * connect to the specified Agora private media server, it connects to the Agora SD-RTN™.
   */
  AgoraConnectivityFirst = 0,
  /**
   * 1: The SDK only attempts to connect to the specified Agora private media server.
   */
  AgoraLocalOnly = 1,
} NS_SWIFT_NAME(AgoraLocalProxyMode);

/**
 * @brief The cloud proxy type.
 */
typedef NS_ENUM(NSUInteger, AgoraProxyType) {
  /**
   * 0: Reserved for future use.
   */
  AgoraNoneProxyType = 0,
  /**
   * 1: The cloud proxy for the UDP protocol, that is, the Force UDP cloud proxy mode. In this mode,
   * the SDK always transmits data over UDP.
   */
  AgoraUdpProxyType = 1,
  /**
   * 2: The cloud proxy for the TCP (encryption) protocol, that is, the Force TCP cloud proxy mode. In
   * this mode, the SDK always transmits data over TCP/TLS 443.
   */
  AgoraTcpProxyType = 2,
  /**
   * 3: Reserved for future use.
   */
  AgoraLocalProxyType = 3,
  /**
   * 4: Automatic mode. In this mode, the SDK attempts a direct connection to SD-RTN™ and
   * automatically switches to TCP/TLS 443 if the attempt fails.
   */
  AgoraTcpProxyAutoFallbackType = 4,
  /** The http proxy.
   */
  AgoraHttpProxyType = 5,
  /** The https proxy.
   */
  AgoraHttpsProxyType = 6,
} NS_SWIFT_NAME(AgoraProxyType);


/**
 * @brief Setting mode of the view.
 */
typedef NS_ENUM(NSInteger, AgoraVideoViewSetupMode) {
  /**
   * 0: (Default) Clear all added views and replace with a new view.
   */
  AgoraVideoViewSetupReplace = 0,
  /**
   * 1: Adds a view.
   */
  AgoraVideoViewSetupAdd = 1,
  /**
   * 2: Deletes a view.
   * @note When you no longer need to use a certain view, it is recommended to delete the view by
   * setting `setupMode` to AgoraVideoViewSetupRemove, otherwise it may lead to leak of rendering
   * resources.
   */
  AgoraVideoViewSetupRemove = 2,
};

/**
 * @brief The number of channels for audio preprocessing.
 *
 * @details
 * In scenarios that require enhanced realism, such as concerts, local users might need to capture
 * stereo audio and send stereo signals to remote users. For example, the singer, guitarist, and
 * drummer are standing in different positions on the stage. The audio capture device captures their
 * stereo audio and sends stereo signals to remote users. Remote users can hear the song, guitar,
 * and drum from different directions as if they were at the auditorium.
 * You can set the dual-channel processing to implement stereo audio in this class. Agora recommends
 * the following settings:1. Preprocessing: call `setAdvancedAudioOptions:` and set
 * `audioProcessingChannels` to `AgoraAudioProcessChannelstereo` (2) in `AgoraAdvancedAudioOptions`.
 * 2. Post-processing: call `setAudioProfile:` and set `profile` to
 * `AgoraAudioProfileMusicStandardStereo` (3) or `AgoraAudioProfileMusicHighQualityStereo` (5).
 *
 * @note
 * - The stereo setting only takes effect when the SDK uses the media volume.
 * - On iOS, the minimum version of system for stereo sound is 14.0. The minimum device requirements
 * are as follows:
 *   - iPhone XS
 *   - iPhone XS Max
 *   - iPhone XR
 *   - iPhone 11
 *   - iPhone 11 Pro
 *   - iPhone 11 Pro Max
 *   - iPhone SE (2020)
 *   - iPad Pro 11" and 12.9" (3rd generation)
 *   - iPad Pro 11" and 12.9" (4th generation)
 *
 */
typedef NS_ENUM(NSInteger, AgoraAudioProcessChannels) {
  /**
   * 1: (Default) Mono.
   */
  AgoraAudioProcessChannelsMono = 1,
  /**
   * 2: Stereo.
   */
  AgoraAudioProcessChannelstereo = 2,
};

/**
 * @brief The frame position of the video observer.
 */
typedef NS_OPTIONS(NSUInteger, AgoraVideoFramePosition) {
   /**
    * 1: The location of the locally collected video data after preprocessing corresponds to the
    * `onCaptureVideoFrame:sourceType:` callback. The observed video here has the effect of video
    * pre-processing, which can be verified by enabling image enhancement, virtual background, or
    * watermark.
    */
   AgoraVideoFramePositionPostCapture = 1 << 0,
   /**
    * 2: The pre-renderer position , which corresponds to the video data in the
    * `onRenderVideoFrame:uid:channelId:` callback.
    */
   AgoraVideoFramePositionPreRenderer = 1 << 1,
   /**
    * 4: The pre-encoder position , which corresponds to the video data in the
    * `onPreEncodeVideoFrame:sourceType:` callback. The observed video here has the effects of video
    * pre-processing and encoding pre-processing.
    * - To verify the pre-processing effects of the video, you can enable image enhancement, virtual
    * background, or watermark.
    * - To verify the pre-encoding processing effect, you can set a lower frame rate (for example, 5
    * fps).
    */
   AgoraVideoFramePositionPreEncoder = 1 << 2,
};

/**
 * @brief The current recording state.
 */
typedef NS_ENUM(NSInteger, AgoraMediaRecorderState) {
  /**
   * -1: An error occurs during the recording. See `AgoraMediaRecorderReasonCode` for the reason.
   */
  AgoraMediaRecorderStateError = -1,
  /**
   * 2: The audio and video recording starts.
   */
  AgoraMediaRecorderStateStarted = 2,
  /**
   * 3: The audio and video recording stops.
   */
  AgoraMediaRecorderStateStopped = 3,
};
/**
 * @brief The reason for the state change.
 */
typedef NS_ENUM(NSInteger, AgoraMediaRecorderReasonCode) {
  /**
   * 0: No error.
   */
  AgoraMediaRecorderReasonCodeNoError = 0,
  /**
   * 1: The SDK fails to write the recorded data to a file.
   */
  AgoraMediaRecorderReasonCodeWriteFailed = 1,
  /**
   * 2: The SDK does not detect any audio and video streams, or audio and video streams are
   * interrupted for more than five seconds during recording.
   */
  AgoraMediaRecorderReasonCodeNoStream = 2,
  /**
   * 3: The recording duration exceeds the upper limit.
   */
  AgoraMediaRecorderReasonCodeOverMaxDuration = 3,
  /**
   * 4: The recording configuration changes.
   */
  AgoraMediaRecorderReasonCodeConfigChange = 4,
};
/**
 * @brief The recording content.
 */
typedef NS_ENUM(NSInteger, AgoraMediaRecorderStreamType) {
  /**
   * 1: Only audio.
   */
  AgoraMediaRecorderStreamTypeAudio = 1,
  /**
   * 2: Only video.
   */
  AgoraMediaRecorderStreamTypeVideo = 2,
  /**
   * 3: (Default) Audio and video.
   */
  AgoraMediaRecorderStreamTypeBoth = 3,
};
/**
 * @brief Format of the recording file.
 */
typedef NS_ENUM(NSInteger, AgoraMediaRecorderContainerFormat) {
  /**
   * 1: (Default) MP4.
   */
  AgoraMediaRecorderContainerFormatMP4 = 1,
};
/**
 * @brief Type of video streams to be recorded.
 */
typedef NS_ENUM(NSInteger, AgoraRecorderStreamType) {
  /**
   * 0: (Default) The video stream within the source channel.
   */
  AgoraRecorderStreamTypeRtc = 0,
  /**
   * 1: The video stream from local video capturing that you preview before joining a channel.
   */
  AgoraRecorderStreamTypePreview = 1,
};
/**
 * @brief The type of the shared target. Set in `AgoraScreenCaptureSourceInfo`.
 */
typedef NS_ENUM(NSInteger, AgoraScreenCaptureSourceType) {
  /**
   * -1: Unknown type.
   */
  AgoraScreenCaptureSourceTypeUnknown = -1,
  /**
   * 0: The shared target is a window.
   */
  AgoraScreenCaptureSourceTypeWindow = 0,
  /**
   * 1: The shared target is a screen of a particular monitor.
   */
  AgoraScreenCaptureSourceTypeScreen = 1,
  /**
   * 2: Reserved parameter.
   */
  AgoraScreenCaptureSourceTypeCustom = 2,
};

/**
 * @brief The highest frame rate supported by the screen sharing device.
 */
typedef NS_ENUM(NSInteger, AgoraScreenCaptureFrameRateCapability) {
  AgoraScreenCaptureFrameRateUnKonw = -1,
  /**
   * 0: The device supports the frame rate of up to 15 fps.
   */
  AgoraScreenCaptureFrameRate15FPS = 0,
  /**
   * 1: The device supports the frame rate of up to 30 fps.
   */
  AgoraScreenCaptureFrameRate30FPS = 1,
  /**
   * 2: The device supports the frame rate of up to 60 fps.
   */
  AgoraScreenCaptureFrameRate60FPS = 2,
};

/**
 * @brief The type of the audio track.
 */
typedef NS_ENUM(NSInteger, AgoraAudioTrackType) {
  /**
   * 0: Mixable audio tracks. This type of audio track supports mixing with other audio streams (such
   * as audio streams captured by microphone) and playing locally or publishing to channels after
   * mixing. The latency of mixable audio tracks is higher than that of direct audio tracks.
   */
  AgoraAudioTrackTypeMixable = 0,
  /**
   * 1: Direct audio tracks. This type of audio track will replace the audio streams captured by the
   * microphone and does not support mixing with other audio streams. The latency of direct audio
   * tracks is lower than that of mixable audio tracks.
   * @note If `AgoraAudioTrackTypeDirect` is specified for this parameter, you must set
   * `publishMicrophoneTrack` to `NO` in `AgoraRtcChannelMediaOptions` when calling
   * `joinChannelByToken:channelId:uid:mediaOptions:joinSuccess:` to join the channel; otherwise,
   * joining the channel fails and returns the error code -2.
   */
  AgoraAudioTrackTypeDirect = 1,
};

/**
 * @brief AI noise suppression modes.
 */
typedef NS_ENUM(NSInteger, AUDIO_AINS_MODE) {
    /**
     * 0: (Default) Balance mode. This mode allows for a balanced performance on noice suppression and
     * time delay.
     */
    AINS_MODE_BALANCED = 0,
    /**
     * 1: Aggressive mode. In scenarios where high performance on noise suppression is required, such as
     * live streaming outdoor events, this mode reduces nosie more dramatically, but may sometimes
     * affect the original character of the audio.
     */
    AINS_MODE_AGGRESSIVE = 1,
    /**
     * 2: Aggressive mode with low latency. The noise suppression delay of this mode is about only half
     * of that of the balance and aggressive modes. It is suitable for scenarios that have high
     * requirements on noise suppression with low latency, such as sing together online in real time.
     */
    AINS_MODE_ULTRALOWLATENCY = 2
};

/**
 * @brief The frame position of the video observer.
 */
typedef NS_ENUM(NSInteger, AgoraVideoModulePosition) {
   /**
    * 1: The location of the locally collected video data after preprocessing corresponds to the
    * `onCaptureVideoFrame:sourceType:` callback. The observed video here has the effect of video
    * pre-processing, which can be verified by enabling image enhancement, virtual background, or
    * watermark.
    */
   AgoraVideoModulePositionPostCapture = 1 << 0,
   /**
    * 2: The pre-renderer position , which corresponds to the video data in the
    * `onRenderVideoFrame:uid:channelId:` callback.
    */
   AgoraVideoModulePositionPreRenderer = 1 << 1,
   /**
    * 4: The pre-encoder position , which corresponds to the video data in the
    * `onPreEncodeVideoFrame:sourceType:` callback. The observed video here has the effects of video
    * pre-processing and encoding pre-processing.
    * - To verify the pre-processing effects of the video, you can enable image enhancement, virtual
    * background, or watermark.
    * - To verify the pre-encoding processing effect, you can set a lower frame rate (for example, 5
    * fps).
    */
   AgoraVideoModulePositionPreEncoder = 1 << 2,
   /**
    * 8: The position after local video capture and before pre-processing. The observed video here does
    * not have pre-processing effects, which can be verified by enabling image enhancement, virtual
    * background, or watermarks.
    */
   AgoraVideoModulePositionPostCaptureOrigin = 1 << 3,
};

/**
 * @brief The rendering state of the media frame.
 */
typedef NS_ENUM(NSInteger, AgoraMediaTraceEvent) {
  /**
   * 0: The video frame has been rendered.
   */
  AgoraMediaTraceEventVideoRendered = 0,
  /**
   * 1: The video frame has been decoded.
   */
  AgoraMediaTraceEventVideoDecoded = 1,
};

typedef NS_ENUM(NSInteger, AgoraH265TranscodeResult) {
  /**
   * -1: Unknown error.
  */
  AgoraH265TranscodeResultUnknown = -1,
  /**
   * 0: The request of operation is successfully.
  */
  AgoraH265TranscodeResultSuccess = 0,
  /**
   * This request is invalid. Possible reasons include incorrect parameters.
  */
  AgoraH265TranscodeResultRequestInvalid = 1,
  /**
   * 2: Authentication failed, please check for correctnes of token.
  */
  AgoraH265TranscodeResultUnauthorized = 2,
  /**
   * 3: The token is expired, please update token.
  */
  AgoraH265TranscodeResultTokenExpired = 3,
  /**
   * 4: No permission to access the interface.
  */
  AgoraH265TranscodeResultForbidden = 4,
  /**
   * 5: The url of request is not found.
  */
  AgoraH265TranscodeResultNotFound = 5,
  /**
   * 6: The request encountered a conflict, please try again.
  */
  AgoraH265TranscodeResultConflict = 6,
  /**
   * 7: Content type not supported.
  */
  AgoraH265TranscodeResultNotSupported = 7,
  /**
   * 8: The requests are too frequent.
  */
  AgoraH265TranscodeResultTooOften = 8,
  /**
   * 9: Internal Server Error, you can try sending the request again.
  */
  AgoraH265TranscodeResultServerInternalError = 9,
  /**
   * 10: Service is unavailable.
  */
  AgoraH265TranscodeResultServiceUnavailable = 10
};

/**
 * @brief The type of the advanced feature.
 */
typedef NS_ENUM(NSUInteger, AgoraFeatureType) {
  /**
   * 1: Virtual background.
   */
  AgoraVideoPreprocessVirtualBackground = 1,
  /**
   * 2: Image enhancement.
   */
  AgoraVideoPreprocessBeauty = 2,
};

typedef NS_ENUM(NSInteger, AgoraVideoModule) {
  AgoraVideoModuleCapturer = 0,
  AgoraVideoModuleSoftwareEncoder = 1,
  AgoraVideoModuleHardwareEncoder = 2,
  AgoraVideoModuleSoftwareDecoder = 3,
  AgoraVideoModuleHardwareDecoder = 4,
  AgoraVideoModuleRenderer = 5,
};

typedef NS_ENUM(NSInteger, AgoraHDRCapability) {
  AgoraHDRCapabilityUnknown = -1,
  AgoraHDRCapabilityUnsupported = 0,
  AgoraHDRCapabilitySupported = 1,
};

/**
 * @brief The relative position of `alphaBuffer` and video frames.
 */
typedef NS_ENUM(NSUInteger, AgoraAlphaStitchMode) {
    /**
     * 0: (Default) Only video frame, that is, `alphaBuffer` is not stitched with the video frame.
     */
    AgoraNoAlphaStitch = 0,
    /**
     * 1: `alphaBuffer` is above the video frame.
     */
    AgoraAlphaStitchUp = 1,
    /**
     * 2: `alphaBuffer` is below the video frame.
     */
    AgoraAlphaStitchBelow = 2,
    /**
     * 3: `alphaBuffer` is to the left of the video frame.
     */
    AgoraAlphaStitchLeft = 3,
    /**
     * 4: `alphaBuffer` is to the right of the video frame.
     */
    AgoraAlphaStitchRight = 4,
};

/**
 * @brief Type of watermark source.
 *
 * @since 4.6.0
 */
typedef NS_ENUM(NSUInteger, WatermarkType) {
    /**
     * (0): The watermark source is an image.
     */
    WatermarkTypeImage = 0,
    /**
     * (1): The watermark source is a buffer.
     */
    WatermarkTypeBuffer = 1,
};

/**
 * @brief The data format of the watermark buffer.
 *
 * @since 4.6.0
 */
typedef NS_ENUM(NSUInteger, WatermarkBufferFormat){
    /**
     * (1): The watermark buffer uses the I420 format.
     */
    WatermarkBufferFormatI420 = 1,
    /**
     * (2): The watermark buffer uses the BGRA format.
     */
    WatermarkBufferFormatBGRA = 2,
    /**
     * (3): The watermark buffer uses the NV21 format.
     */
    WatermarkBufferFormatNV21 = 3,
    /**
     * (4): The watermark buffer uses the RGBA format.
     */
    WatermarkBufferFormatRGBA = 4,
};

/**
 * @brief Reliable Data Transmission Tunnel message stream type
 *
 * @technical preview
 */
typedef NS_ENUM(NSInteger, AgoraRdtStreamType) {
  /**
   * Command stream type.
   * Characterized by: reliability, high priority, and not affected by congestion control.
   * Transmission limits: a maximum of 256 bytes per packet, and 100 packets per second.
   */
  AgoraRdtStreamCmd = 0,
  /**
   * Data stream type.
   * Characterized by: reliability, low priority, and affected by congestion control.
   * Transmission limits: a maximum of 128 KBytes per packet, with a rate of 4 Mbps.
   */
  AgoraRdtStreamData = 1,
  /**
   * Reliable Data Transmission stream type count
   */
  AgoraRdtStreamCount = 2,
};

/**
 * @brief Reliable Data Transmission tunnel state
 *
 * @technical preview
 */
typedef NS_ENUM(NSInteger, AgoraRdtState) {
  /**
   * The RDT tunnel is in the initial or is closed.
   */
  AgoraRdtStateClose = 0,
  /**
   * The RDT tunnel is open, and data can only be sent in this state.
   */
  AgoraRdtStateOpened = 1,
  /**
   * The send buffer of the RDT tunnel is full. RDT_STREAM_DATA cannot be sent,
   * but RDT_STREAM_CMD can be sent, as the latter is not affected by congestion control.
   */
  AgoraRdtStateBlocked = 2,
  /**
   * The RDT tunnel is in a suspended state because SDK has disconnected.
   * It will automatically resume to the RDT_STATE_OPENED state after rejoining the channel.
   */
  AgoraRdtStatePending = 3,
  /**
   * The RDT channel is broken, and the data being sent and received will be cleared.
   * It will automatically resume to the RDT_STATE_OPENED state later.
   * Reason for occurrence: The remote user actively called the API to leave the
   * channel and then rejoined the channel, without being detected by this end.
   */
  AgoraRdtStateBroken = 4,
};

/**
 * @brief The transmission mode of data over multiple network paths.
 *
 * @since 4.6.0
 */
typedef NS_ENUM(NSInteger, AgoraMultipathMode) {
  /**
   * 0: Duplicate mode, where data is sent over multiple paths simultaneously.
   * @technical preview
   */
  AgoraMultipathModeDuplicate = 0,
  /**
   * (1): Dynamic transmission mode. The SDK dynamically selects the optimal path for data
   * transmission to improve transmission performance.
   */
  AgoraMultipathModeDynamic = 1,
};

/**
 * @brief Network path types used in multipath transmission.
 *
 * @since 4.6.0
 */
typedef NS_ENUM(NSInteger, AgoraMultipathType) {
  /**
   * (0): LAN type, indicates data is transmitted over a local area network.
   */
  AgoraMultipathTypeLAN = 0,
  /**
   * (1): WIFI type, indicates data is transmitted over a Wi-Fi network.
   */
  AgoraMultipathTypeWIFI = 1,
  /**
   * (2): Mobile type, indicates data is transmitted over a mobile network.
   */
  AgoraMultipathTypeMobile = 2,
  /**
   * (99): Unknown type, indicates an unrecognized network path type.
   */
  AgoraMultipathTypeUnknown = 99,
};

/**
 * @brief Represents the error codes after calling `renewToken:`.
 *
 * @since 4.6.0
 */
typedef NS_ENUM(NSInteger, AgoraRenewTokenErrorCode) {
  /**
   * (0): Token updated successfully.
   */
  AgoraRenewTokenSuccess = 0,
  /**
   * (1): Token update failed. It is recommended to generate a new Token and retry `renewToken:`.
   */
  AgoraRenewTokenFailure = 1,
  /**
   * 2: The token renewal failed because the provided token has expired. 
   * It is recommended that the user generate a new token with a longer expiration time and retry renewToken.
   */
  AgoraRenewTokenExpiredToken = 2,
  /**
   * (3): Token update failed because the provided Token is invalid. Common reasons include: the
   * project has enabled App Certificate in the Agora Console but did not use a Token when joining the
   * channel; the uid specified in `joinChannel` is inconsistent with the uid used when generating the
   * Token; the channel name specified in `joinChannel` is inconsistent with the one used when
   * generating the Token. It is recommended to check the Token generation process, generate a new
   * Token, and retry `renewToken:`.
   */
  AgoraRenewTokenInvalidToken = 3,
  /**
   * (4): Token update failed because the channel name in the Token does not match the current
   * channel. It is recommended to check the channel name, generate a new Token, and retry
   * `renewToken:`.
   */
  AgoraRenewTokenInvalidChannelName = 4,
  /**
   * (5): Token update failed because the App ID in the Token does not match the current App ID. It is
   * recommended to check the App ID, generate a new Token, and retry `renewToken:`.
   */
  AgoraRenewTokenInconsistentAppId = 5,
  /**
   * (6): The Token update request was canceled because a new request was initiated.
   */
  AgoraRenewTokenCanceledByNewRequest = 6,
};
