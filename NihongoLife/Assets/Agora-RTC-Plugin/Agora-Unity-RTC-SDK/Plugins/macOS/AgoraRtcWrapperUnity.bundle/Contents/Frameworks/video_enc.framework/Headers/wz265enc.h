///////////////////////////////////////////////////
//
//  Visionular H265 Codec Library
//
//  Copyright(c) visionular Inc.
//  https://www.visionular.com/
//
///////////////////////////////////////////////////
#ifndef _WZ265_ENCODER_INTERFACE_H_
#define _WZ265_ENCODER_INTERFACE_H_

#include "wz265def.h"
// ****************************************
// base configuration
// ****************************************
#define WZ265PRESET_ULTRAFAST 0
#define WZ265PRESET_SUPERFAST 1
#define WZ265PRESET_VERYFAST 2
#define WZ265PRESET_FASTER 3
#define WZ265PRESET_FAST 4
#define WZ265PRESET_MEDIUM 5
#define WZ265PRESET_SLOW 6
#define WZ265PRESET_SLOWER 7
#define WZ265PRESET_VERYSLOW 8
#define WZ265PRESET_SUPERSLOW 9
#define WZ265PRESET_PLACEBO 10

/*     WZ265 tuning types: Multiple tunings can be combined by "|"
       e.g., WZ265_TUNE_SSIM | WZ265_TUNE_SCREEN                */
#define WZ265_TUNE_DEFAULT 0x0000
#define WZ265_TUNE_SSIM 0x0001
#define WZ265_TUNE_VMAF 0x0002
#define WZ265_TUNE_SCREEN 0x0004
#define WZ265_TUNE_MOVIE 0x0008
#define WZ265_TUNE_GAME 0x0010
#define WZ265_TUNE_ZEROLATENCY 0x0020
#define WZ265_TUNE_UNIVERSE 0x0080

typedef enum WZ265_RC_TYPE {
  WZ265_RC_QUALITY = -1,
  WZ265_RC_CQP = 0,
  WZ265_RC_CRF = 1,
  WZ265_RC_ABR = 2,
  WZ265_RC_CBR = 3,
  WZ265_RC_MAX = WZ265_RC_CBR
} WZ265_RC_TYPE;

typedef enum WZ265_WAVEFRONT_TYPE {
  WZ265_WPP_DISABLE = 0,
  WZ265_WPP_ALL = 1,
  WZ265_WPP_RDO_ONLY = 2,
} WZ265_WAVEFRONT_TYPE;

typedef enum WZ265_INPUT_CSP {
  WZ265_CSP_DEFAULT,    /*read from input file , otherwise use WZ265_CSP_I420 */
  WZ265_CSP_I420,       /* yuv 4:2:0 planar */
  WZ265_CSP_YV12,       /* yvu 4:2:0 planar */
  WZ265_CSP_NV12,       /* yuv 4:2:0, with one y plane and one packed u+v per pixel */
  WZ265_CSP_NV21,       /* yuv 4:2:0, with one y plane and one packed v+u per pixel */
  WZ265_CSP_ANDROID420, /* yuv 4:2:0, with one y plane and packed u+v per line */
  WZ265_CSP_YUY2,       /* yuv 4:2:2, packed Y0 U0 Y1 V1*/
  WZ265_CSP_P010,       /* yuv 4:2:0, planar , 10bit */
  WZ265_CSP_P012,       /* yuv 4:2:0, planar , 12bit */
  WZ265_CSP_P016,       /* yuv 4:2:0, planar, 16bit */
  WZ265_CSP_P210,       /* yuv 4:2:2, planar, 10 bit */
  WZ265_CSP_P212,       /* yuv 4:2:2, planar, 12 bit */
  WZ265_CSP_Y210,       /* yuv 4:2:2, packed, 10 bit */
  WZ265_CSP_Y212,       /* yuv 4:2:2, packed, 12 bit */
  WZ265_CSP_P216,       /* yuv 4:2:2, planar, 16bit */
  WZ265_CSP_Y216,       /* yuv 4:2:2, packed, 16bit */
  WZ265_CSP_MAX = WZ265_CSP_Y216,
  WZ265_CSP_YUYV = WZ265_CSP_YUY2, /* yuv 4:2:2, packed Y0 U0 Y1 V1*/
} WZ265_INPUT_CSP;

typedef enum WZ265_REF_MODE {
  WZ265_REF_NORMOL = 0,
  WZ265_REF_DIST = 1,    // prefer short distance reference
  WZ265_REF_QUALITY = 2  // prefer high quality reference
} WZ265_REF_MODE;

static const char *const wz265_video_format_names[] = { "component", "pal",     "ntsc", "secam",
                                                        "mac",       "unknown", 0 };
static const char *const wz265_fullrange_names[] = { "limited", "full", 0 };
static const char *const wz265_colourprimaries_names[] = {
  "reserved",  "bt709", "unknown", "reserved", "bt470m",   "bt470bg",  "smpte170m",
  "smpte240m", "film",  "bt2020",  "smpte428", "smpte431", "smpte432", 0
};
static const char *const wz265_transfer_names[] = {
  "reserved",  "bt709",     "unknown",   "reserved", "bt470m",       "bt470bg", "smpte170m",
  "smpte240m", "linear",    "log100",    "log316",   "iec61966-2-4", "bt1361e", "iec61966-2-1",
  "bt2020-10", "bt2020-12", "smpte2084", "smpte428", "arib-std-b67", 0
};
static const char *const wz265_colourmatrix_names[] = {
  "gbr",       "bt709", "unknown",  "",        "fcc",       "bt470bg",           "smpte170m",
  "smpte240m", "ycgco", "bt2020nc", "bt2020c", "smpte2085", "chroma-derived-nc", "chroma-derived-c",
  "ictcp",     0
};

#define WZ265_MAX_PATH_LEN 1024
#define WZ265_CFG_AUTO_VALUE -1000
#define WZ265_RTC_MAX_TLAYER_ID 2
// Base configuration
typedef struct WZ265EncConfig {
  void *pAuth;  // WZAuth, invalid if don't need aksk auth
  int preset;   // [7:4] preset-strength, 0-9;  [3:0] preset, ultrafast ~ superslow
  int tune;     // tuning type, combined value
  WZ265_INPUT_CSP inputCsp;
  int inputBitDepth;  // input bit depth
  int profileId;      // support main profile(1) and main still profile(3)
  int levelIdc;       // according to resolution
  int picWidth;       // input frame width
  int picHeight;      // input frame height
  double frameRate;   // input frame rate
  int fpsNum;         // fpsNum/fpsDen has a higher priority than frameRate,when fpsNum>0&&fpsDen>0
  int fpsDen;
  int internalBitDepth;       // internal bit depth
  int bHeaderBeforeKeyframe;  // output vps,sps,pps before key frame, default 1
  //**************************************************************************
  // int type start from here
  // These configures = WZ265_CFG_AUTO_VALUE means set by encoder default, otherwise means set by
  // users for most configs, < 0 also means auto, but WZ265_CFG_AUTO_VALUE is more secure
  //**************************************************************************
  int rc;           // rc type 0 disable,1 crf,2 abr,3 cbr, default 1
  int b_vfr_input;  // VFR(Variable frame rate) input
  int timebaseNum;  // only used with b_vfr_input=1;
  int timebaseDen;  // only used with b_vfr_input=1;
  int bframes;      // num of bi-pred frames, -1: using default
  int bframesAdapt;
  int single_pps;       // limit pps to be only one
  int rps_prediction;   // rps prediction from sps
  int temporalLayer;    // works with zerolatency, separate P frames into temporal layers, 0 ~ 2
  int bitrateInkbps;    // target bit rate in kbps, valid when rctype is cbr abd vbr
  int vbv_buffer_size;  // buf size of vbv
  int vbv_max_rate;     // max rate of vbv
  int vbv_buffer_debt_frames;  // underflow vbvbuffer for rd gian
  int qp;                      // valid when rctype is disable, default 30
  int chroma_qp_offset;        // valid range -12..12, default 0, equals to
                               // pps_cb_qp_offset=pps_cr_qp_offset
  int QPDeltaMinAdapt;
  int rc_rf_by_fps;  //
  int rc_sync_dist;  // rc sync distance
  int keyint_max;    // Key-Frame period, 0 = only first, -1 = auto
  int keyint_min;    // Minimum Period of Key-Frame
  int scenecut;      // how aggressively to insert extra I frames, 0 - 100
  int qpmin;         // minimal qp, valid when rc != 0, 0~51
  int qpmax;         // maximal qp, valid when rc != 0, 1~51, qpmax = 0 means 51
  int qpmax_i;       // maximal qp for I frames
  int rcFrameSkip;   // frame skip for rate control. 0: not allowed;  > 0: continuious skipped frame
                     // number
  int enable_temporal_filter;
  int golden_frame_interval;
  int tf_frame_interval;

  //* Execute Properties
  int enWavefront;        // enable wave front parallel
  int enFrameParallel;    // enable frame parallel
  int ParallelledFrames;  // 0 means auto
  int enTileParallel;     // enable tile parallel, currently, tile parallel CAN'T work together with
                          // wpp or fpp
  int tile_rows;          // 0: auto
  int tile_columns;       // 0: auto
  int threads;  // number of threads used in encoding ( for wavefront, frame parallel, or both)
  int preanalyze_threads;  // pre-analyze threads
  int log2MaxCtuSize;      // log2_min_luma_coding_block_size: 4 ~ 6, default 6
  int lookahead;           // lookahead settings
  int calcPsnr;            // 0:not calc psnr; 1: print total psnr; 2: print each frame
  int calcSsim;            // calc ssim (0: don't 1: only summary 2: print each frame and summary
  int calcVmaf;            // 0: don't calc vmaf; 1: print summary; 2: print each frame
  int calcTime;            // 0: do not print; 1: print maximum ; 2: print each frame
  int rdoq;                // 1:enabling rdoq
  int scalingLists;        // 0: off; 1: default
  int me;                  // 0: DIA, 1: HEX, 2: UMH, 3:EPZS,
  int part;                // 0: 2NX2N 1: NX2N 2NXN NXN
  int partchecklevel;
  int do64;     // 1:enabling 64x64 cu
  int tuInter;  // inter RQT tu depth, 0~3, -1 means auto
  int tuIntra;  // intra RQT tu depth, 0~3, -1 means auto
  int checkZero;
  int smooth;                 // 1: enabling strong intra smoothing
  int transskip;              // 1: enabling transform skip
  int subme;                  // 0 : disable; 1 ~ 5: subme levels, default 3,4,5
  int satdInter;              // 1:enabling hardmad sad
  int satdIntra;              // 1:enabling hardmad sad
  int searchrange;            // search range
  int refmode;                // defined by WZ265_REF_MODE
  int refnum;                 // reference number
  int pRefNum;                // pref number
  int goldenFrameAsRefFirst;  // if 1 first nearest ref, second golden frame. if 0,do not handle
                              // golden frame special
  int sao;                    // sao enabling, 0: disable; 1:faster; 2: faster; 3: usual; 4:complex
  int longTermRef;            // 0:disabling longterm reference 1:enable;
  int intra32;                // 0:disable intra32
  int noiseReduction;         // inter noise reduction strength
  int weightp;                // Enable weighted prediction in P slices
  int weightb;                // Enable weighted prediction in B slices
  int iAqMode;  // adaptive quantization 0~6, 0: disable, 1: variance; 4: fine variance (default), 6
                // - activity equal to max var of subblocks
  int log2AqBlockSize;    // adaptive quantization block size, 4-6
  int log2QGroupSize;     // quantization group size (4-6, must less than maxcusize)
  int psyRdType;          // 0: default, 1: optional
  int crfAdaptByQuality;  // CRF adapt by quality, 0: auto; 1: adapt by PSNR; 2: adapt by Vmaf
                          // without motion
  int head_thr;           // the threshold of duration (s) to set crf of front frames
  int head_ratio;         // 1/head_ratio is the proportion of front frames
  int opengop;            // enable RASL for CRA, default 1; If opengop = 0, CRA acts like IDR
  int extra_opts;         // extra speedup optimization for huya original painting scenario
  int reduceCplxByTool;   // level of reduce complexity burst by tools
  int cplxFrameSkip;      // frame skip to reduce complexity
  //* vui_parameters
  // vui_parameters_present_flag set to 1 specifies that the vui_parameters() syntax in struct vui
  // must be set by usr
  int vui_parameters_present_flag;
  int accessUnitDelimitersPresent;
  int doviProfile;  // dolby vision profile 50, 81, 82, 84,
  int visual_opt;   // enable visual-opt, default 1, 0 ~ 2
  //=====================================
  //====== int type end           =======
  //=====================================

  //***************************************************************************
  // double type start from here
  // These configures = WZ265_CFG_AUTO_VALUE means set by encoder default, otherwise means set by
  // users
  //***************************************************************************
  double crf;           // valid when rctype is crf, default 30
  double quality;       // valid when rctype is quality, default -1
  double keyint_t;      // I-Frame period by time (s), -1 = not work, default -1
  double keyint_first;  // first GOP period by time (s), -1 = not work, default -1
  double ipratio;
  double iboost;
  double qcompCutree;
  double shortLoadingForPlayer;  // reduce b frames after I frame, for shorting the loading time of
                                 // VOD for some players
  double fRateTolerance;  // default 2.0f,0.5 is suitable to reduce the largest bitrate, and 0.1 is
                          // to make the bitrate stable
  double fAqStrength;     // strength of adaptive quantizaiton, 0~3.0, default 0.7
  double fAqSmooth;       // strength of adaptive quantizaiton for smooth area, 0~3.0, default 0.7
  double fAqWeight;       // weight of aq-mode 1 and 4;  or aq-mode 2 and 4
  double roiDeltaQp;      // roi region delta qp ratio, default 1.0
  double intra32pen;      // penalty on intra32x32 when intra32 is set
  double psyRd;           // psy-rd strength, 0 ~ 1.0
  double psyRdUV;
  double psyIframe;     // psy limit for I frame
  double qualityLevel;  // for PSNR(dB); for Vmaf(score)
  double qualityRange;  // adapt when quality > qualityLevel + qualityRange, for PSNR(dB);
  double head_crf;      // set crf of front frames when video is long
  double qualitySmooth;
  double reduceCplxByQP;  // reduce complexity burst by QP, default 0. It's for special case
  double duration;        // the duration (s) of the video
  //=====================================
  //====== double type end        =======
  //=====================================

  // VUI and SEI metadata (HDR releated)
  vui_parameters vui;
  /* SMPTE ST 2086 mastering display color volume SEI info,
   * A string with format "master-display=Y(%hu,%hu)U(%hu,%hu)V(%hu,%hu)WP(%hu,%hu)L(%u,%u)"
   * where %hu are unsigned 16bit integers and %u are unsigned 32bit integers,
   * can be parsed by WZ265ConfigParse */
  mastering_display_colour_volume_sei masteringDisplayColorVolume;
  int masteringDisplayPresent;  // 1: masteringDisplayColorVolume will be write to SEI nal
  /* CEA 861.3, Content light level information SEI info,
   * A string with format "max-cll=%hu,%hu" , can be parsed by WZ265ConfigParse*/
  content_light_level_info_sei contentLightLevel;
  int contentLightLevelPresent;  // 1: contentLightLevel will be write to SEI nal

  ambient_viewing_environment_sei ambientViewEnvironment;
  int ambientViewEnvironmentPresent;

  buffering_period_sei bufferingPeriod;

  // For Multi pass
  int iPass;  // Multi pass rate control, 0:disable(default); 1: first pass; 2: second pass
  char statFileInName[256];   // log file produced from first pass, set by user
  char statFileOutName[256];  // log file produced from first pass, set by user
  // For debug
  char dumpYuvFile[WZ265_MAX_PATH_LEN];
  char dumpSrcFile[WZ265_MAX_PATH_LEN];
  char dumpBsFile[WZ265_MAX_PATH_LEN];
  int logLevel;  // log level (-1: dbg; 0: info; 1:warn; 2:err; 3:fatal)
} WZ265EncConfig;

// input frame data and info
typedef struct WZ265YUV {
  int iWidth;      // input frame width
  int iHeight;     // input frame height
  int iBitDepth;   // input YUV bit depth (8, 10, 12)
  void *pData[3];  // input frame Y U V
  int iStride[3];  // stride for Y U V
} WZ265YUV;

// input frame data and info
typedef struct WZ265Picture {
  int iSliceType;  // specified by output pictures
  int poc;         // ignored on input
  long long pts;
  long long dts;
  WZ265YUV *yuv;
  // for input, custom decide which ref can be used as reference,if not used, make sure it is
  // zero!!!
  // if there is no matched poc in DPB ,encoder will ignore this!!
  // if this  setting is valid, other reference in DPB will be cleaned
  int forceRefNum;
  int forceRefPoc[8];
  /* User defined SEI */
  WZ265SEI userSEI;
  /* Dobly Vision Rpu metadata */
  WZ265DoviRpu doviRpu;

  // for output,
  double psnr[3];  // Out: PSNR of Y, U, and V (if calcPsnr is set)
  double frameAvgQp;
  // preset current DPB picture poc
  int curDPBRefNum;
  int dpbRefPoc[8];
  void *roi;
} WZ265Picture;

typedef struct WZ265Nal {
  int naltype;
  int tid;
  int iSize;
  long long pts;
  unsigned char *pPayload;
} WZ265Nal;

#if defined(__cplusplus)
extern "C" {
#endif  //__cplusplus
/**
 * Create encoder
 *
 * @param pCfg : base config of encoder
 * @param errorCode : error code
 * @return encoder handle
 */
_h_dll_export void *wz265_encoder_open(WZ265EncConfig *pCfg, int *errorCode);

/**
 * Close and destroy encoder
 *
 * @param pEncoder :  encoder handle
 */
_h_dll_export void wz265_encoder_close(void *pEncoder);

/**
 * Reconfig encoder
 *
 * @param pEncoder encoder handle
 * @param pCfg     new base config
 */
_h_dll_export void wz265_encoder_reconfig(void *pEncoder, WZ265EncConfig *pCfg);

/**
 * Produce video header nals(VPS,SPS,PPS)
 *
 * @param pEncoder   encoder handle
 * @param pNals      header nals that will be used for the following stream
 * @param iNalCount  number of nals
 * @return if succeed, return 0; if failed, return the error code
 */
_h_dll_export int wz265_encode_headers(void *pEncoder, WZ265Nal **pNals, int *iNalCount);

/**
 * Encode one frame
 *
 * @param pEncoder   handle of encoder
 * @param pNals      pointer array of output wz265NAL units
 * @param iNalCount  output wz265NAL unit count
 * @param pInpic     input frame
 * @param pOutpic    output frame
 * @return if succeed, return 0; if failed, return the error code
 */
_h_dll_export int wz265_encoder_frame(void *pEncoder, WZ265Nal **pNals, int *iNalCount,
                                      WZ265Picture *pInpic, WZ265Picture *pOutpic);

/**
 * Request encoder to encode next frame as Key Frame
 *
 * @param pEncoder   encoder handle
 */
_h_dll_export void wz265_keyframe_request(void *pEncoder);

/**
 * Query buffered frames in encoder
 *
 * @param pEncoder   encoder handle
  @return Number of buffered frames in encoder
 */
_h_dll_export int wz265_encoder_delayed_frames(void *pEncoder);
/* clang-format off */

/* preset can be further set by preset-strength by a delimiter in ",./-+",
   e.g, "veryfast-5" or "medium-0",
   valid preset-strength ranged in 0-9, "medium-0" is same to "medium",
   larger preset-strength means speed down for better RD quality.
*/
static const char *const wz265_preset_names[] = { "ultrafast", "superfast", 0 };
/*      Multiple tunings can be used if separated by a delimiter in ",./-+",
        e.g., "ssim,screen" or "ssim/screen"                                */
static const char *const wz265_tunes_names[] = { "default",     "ssim",     "vmaf",
                                                 "screen",      "movie",    "game",
                                                 "zerolatency", "universe", 0 };
/* clang-format on */
// Get default config values by preset, tune and latency. enum format
_h_dll_export int wz265_param_default(WZ265EncConfig *pConfig, int preset, int tune);

// Get default config values by preset, tune and latency. string format
_h_dll_export int wz265_param_default_preset(WZ265EncConfig *pConfig, const char *preset,
                                             const char *tune);
// Parse name,value paire and set into WZ265EncConfig
_h_dll_export int WZ265ConfigParse(WZ265EncConfig *p, const char *name, const char *value);
#if defined(__cplusplus)
}
#endif  //__cplusplus

#endif
