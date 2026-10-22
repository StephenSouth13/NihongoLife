// Agora Web SDK bridge for the WebGL player. The native Agora Unity plugin does not run in browsers.
// The script tag is injected on the first voice or classroom call, not at page load.
// Desktop and Android keep using Agora-RTC-Plugin; this file is only compiled into WebGL.
mergeInto(LibraryManager.library, {
  $nlAgoraGet: function () {
    if (window.__nlAgora) return window.__nlAgora;
    var SDK = "https://download.agora.io/sdk/release/AgoraRTC_N-4.23.4.js";
    var s = {
      target: "",
      appId: "",
      client: null,
      mode: "",
      joined: false,
      leaving: false,
      channel: "",
      token: "",
      uid: 0,
      role: "audience",
      pubMic: false,
      pubCam: false,
      subAudio: true,
      subVideo: false,
      mic: null,
      cam: null,
      ans: true,
      spatial: false,
      range: 18,
      maxRecv: 6,
      selfPos: null,
      remotePos: {},
      cameras: [],
      camIndex: 0,
      gen: 0,
      op: Promise.resolve(),
      sdkPromise: null
    };

    function dist(a, b) {
      var dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
      return Math.sqrt(dx * dx + dy * dy + dz * dz);
    }

    s.queue = function (fn) {
      s.op = s.op.then(fn).catch(function (err) { s.fail(err); });
      return s.op;
    };
    s.send = function (method, msg) {
      if (!s.target || typeof SendMessage === "undefined") return;
      SendMessage(s.target, method, msg == null ? "" : String(msg));
    };
    s.fail = function (err) {
      var msg = (err && (err.message || err.name)) || String(err || "unknown");
      var name = err && err.name ? String(err.name) : "";
      var code = 3;
      if (name === "NotAllowedError" || name === "PermissionDeniedError" || /permission|notallowed/i.test(msg)) code = 2;
      else if (/camera|NotFoundError|device not found/i.test(name + " " + msg)) code = 4;
      else if (/token|invalid.*app|CAN_NOT_GET_GATEWAY/i.test(msg)) code = 5;
      else if (/sdk|network|failed to fetch|load/i.test(msg)) code = 1;
      console.warn("[NihongoAgora]", code, msg);
      s.send("OnAgoraError", code + "|" + msg);
    };
    s.loadSdk = function () {
      if (window.AgoraRTC) return Promise.resolve();
      if (s.sdkPromise) return s.sdkPromise;
      s.sdkPromise = new Promise(function (resolve, reject) {
        var el = document.createElement("script");
        el.src = SDK;
        el.async = true;
        el.onload = function () {
          if (window.AgoraRTC) resolve();
          else reject(new Error("Agora Web SDK loaded but AgoraRTC is missing."));
        };
        el.onerror = function () {
          s.sdkPromise = null;
          reject(new Error("Could not load the Agora Web SDK (download.agora.io). Voice needs that host allowed."));
        };
        document.head.appendChild(el);
      });
      return s.sdkPromise.then(function () {
        try { AgoraRTC.setLogLevel(2); } catch (e) {}
        AgoraRTC.onAutoplayFailed = function () { s.showUnlock(); };
      });
    };
    s.showUnlock = function () {
      if (document.getElementById("nl-agora-unlock")) return;
      var b = document.createElement("button");
      b.id = "nl-agora-unlock";
      b.type = "button";
      b.textContent = "Tap to enable voice";
      b.style.cssText = "position:fixed;z-index:40;left:50%;top:16px;transform:translateX(-50%);padding:10px 18px;border:0;border-radius:8px;background:#f2c14e;color:#1a1408;font:600 16px/1.2 sans-serif;cursor:pointer;";
      b.onclick = function () {
        try { if (window.AgoraRTC && AgoraRTC.resumeAudioContext) AgoraRTC.resumeAudioContext(); } catch (e) {}
        b.remove();
      };
      document.body.appendChild(b);
    };
    s.ensureDiv = function (id) {
      var el = document.getElementById(id);
      if (el) return el;
      el = document.createElement("div");
      el.id = id;
      el.style.position = "absolute";
      el.style.zIndex = "30";
      el.style.overflow = "hidden";
      el.style.pointerEvents = "none";
      el.style.display = "none";
      el.style.background = "#111";
      document.body.appendChild(el);
      return el;
    };
    s.placeVideo = function (id, x, y, w, h, screenW, screenH) {
      var el = s.ensureDiv(id);
      var canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
      if (!canvas || !screenW || !screenH) return;
      var b = canvas.getBoundingClientRect();
      var sx = b.width / screenW;
      var sy = b.height / screenH;
      el.style.left = (b.left + x * sx) + "px";
      el.style.top = (b.top + (screenH - (y + h)) * sy) + "px";
      el.style.width = Math.max(0, w * sx) + "px";
      el.style.height = Math.max(0, h * sy) + "px";
    };
    s.playLocal = function () {
      if (!s.cam) return;
      s.cam.play(s.ensureDiv("nl-agora-local"));
    };
    s.bind = function (client) {
      client.on("user-joined", function (user) { s.send("OnAgoraUserJoined", String(user.uid)); });
      client.on("user-left", function (user) {
        delete s.remotePos[String(user.uid)];
        s.send("OnAgoraUserOffline", String(user.uid));
      });
      client.on("user-published", function (user, mediaType) {
        s.queue(function () { return s.subscribe(user, mediaType); });
      });
      client.on("user-unpublished", function (user, mediaType) {
        if (mediaType === "video") s.send("OnAgoraRemoteVideo", user.uid + "|0");
      });
      client.on("token-privilege-will-expire", function () { s.send("OnAgoraToken", ""); });
      client.on("token-privilege-did-expire", function () { s.send("OnAgoraToken", ""); });
      client.on("connection-state-change", function (cur, prev, reason) {
        if (cur === "DISCONNECTED" && !s.leaving && (reason === "UID_BANNED" || reason === "CHANNEL_BANNED" || reason === "IP_BANNED"))
          s.fail(new Error("Disconnected (" + reason + ")."));
      });
      client.enableAudioVolumeIndicator();
      client.on("volume-indicator", function (vols) {
        if (!vols || !vols.length) { s.send("OnAgoraVolumes", ""); return; }
        var parts = [];
        for (var i = 0; i < vols.length; i++) parts.push(vols[i].uid + ":" + Math.round((vols[i].level || 0) * 2.55));
        s.send("OnAgoraVolumes", parts.join(","));
      });
    };
    s.ensureClient = function (profile) {
      var mode = profile === 0 ? "rtc" : "live";
      if (s.client && s.mode === mode) return Promise.resolve();
      return s.leave(true).then(function () {
        s.client = AgoraRTC.createClient({ mode: mode, codec: "vp8" });
        s.mode = mode;
        s.bind(s.client);
      });
    };
    s.subscribe = function (user, mediaType) {
      if (!s.client) return Promise.resolve();
      if (mediaType === "audio" && !s.subAudio) return Promise.resolve();
      if (mediaType === "video" && !s.subVideo) return Promise.resolve();
      return s.client.subscribe(user, mediaType).then(function () {
        if (mediaType === "audio" && user.audioTrack) {
          user.audioTrack.play();
          s.applySpatial();
        }
        if (mediaType === "video" && user.videoTrack) {
          user.videoTrack.play(s.ensureDiv("nl-agora-remote"));
          s.send("OnAgoraRemoteVideo", user.uid + "|2");
        }
      });
    };
    s.ensureMic = function () {
      if (!s.pubMic) return Promise.resolve();
      if (s.mic) { s.mic.setEnabled(true); return Promise.resolve(); }
      return AgoraRTC.createMicrophoneAudioTrack({
        encoderConfig: "speech_standard", AEC: true, ANS: s.ans, AGC: true
      }).then(function (track) { s.mic = track; }).catch(function (err) {
        s.pubMic = false;
        throw err;
      });
    };
    s.ensureCam = function () {
      if (!s.pubCam) return Promise.resolve();
      if (s.cam) { s.cam.setEnabled(true); s.playLocal(); return Promise.resolve(); }
      return AgoraRTC.createCameraVideoTrack({ encoderConfig: "480p_1" }).then(function (track) {
        s.cam = track;
        s.playLocal();
      }).catch(function (err) {
        s.pubCam = false;
        s.send("OnAgoraError", "4|" + ((err && err.message) || "No camera."));
      });
    };
    s.applyPublish = function (role, pubMic, pubCam) {
      s.role = role === 1 ? "host" : "audience";
      s.pubMic = !!pubMic;
      s.pubCam = !!pubCam;
      if (!s.client || !s.joined) return Promise.resolve();
      var wantHost = s.mode === "rtc" || s.pubMic || s.pubCam;
      var roleStep = (s.mode === "live") ? s.client.setClientRole(wantHost ? "host" : "audience") : Promise.resolve();
      return roleStep.then(function () { return s.ensureMic(); }).then(function () { return s.ensureCam(); }).then(function () {
        var pending = [];
        function publish(track) {
          if (!track) return;
          pending.push(s.client.publish(track).catch(function (e) {
            if (!/already published/i.test(String(e && e.message))) throw e;
          }));
        }
        if (s.pubMic) publish(s.mic);
        if (s.pubCam) publish(s.cam);
        return Promise.all(pending);
      }).then(function () {
        var drop = [];
        if (!s.pubMic && s.mic) drop.push(s.client.unpublish(s.mic).catch(function () {}));
        if (!s.pubCam && s.cam) drop.push(s.client.unpublish(s.cam).catch(function () {}));
        return Promise.all(drop);
      });
    };
    s.join = function (req) {
      var gen = ++s.gen;
      s.leaving = false;
      return s.loadSdk().then(function () { return s.ensureClient(req.profile); }).then(function () {
        if (gen !== s.gen) return;
        s.channel = req.channel;
        s.token = req.token || "";
        s.role = req.role === 1 ? "host" : "audience";
        s.pubMic = !!req.pubMic;
        s.pubCam = !!req.pubCam;
        s.subAudio = !!req.subAudio;
        s.subVideo = !!req.subVideo;
        var uid = parseInt(req.uid, 10);
        if (!uid) uid = null;
        var token = s.token ? s.token : null;
        var ready = (s.mode === "live")
          ? s.client.setClientRole(s.pubMic || s.pubCam ? "host" : s.role)
          : Promise.resolve();
        return ready.then(function () { return s.client.join(s.appId, s.channel, token, uid); }).then(function (assigned) {
          if (gen !== s.gen) return s.client.leave();
          s.joined = true;
          s.uid = assigned;
          s.send("OnAgoraJoinOk", s.channel + "|" + assigned);
          return s.applyPublish(s.role === "host" ? 1 : 2, s.pubMic ? 1 : 0, s.pubCam ? 1 : 0);
        });
      });
    };
    s.startPreview = function () {
      return s.loadSdk().then(function () { return AgoraRTC.getCameras(); }).then(function (cams) {
        s.cameras = cams || [];
        s.send("OnAgoraDevices", String(s.cameras.length));
        if (!s.cameras.length) throw new Error("No camera found on this browser.");
        if (s.cam) { s.playLocal(); return; }
        return AgoraRTC.createCameraVideoTrack({
          encoderConfig: "480p_1",
          cameraId: s.cameras[0].deviceId
        }).then(function (track) { s.cam = track; s.playLocal(); });
      });
    };
    s.stopPreview = function () {
      if (s.cam && !s.pubCam) {
        try { s.cam.stop(); s.cam.close(); } catch (e) {}
        s.cam = null;
      } else if (s.cam) {
        try { s.cam.stop(); } catch (e) {}
      }
      var el = document.getElementById("nl-agora-local");
      if (el && !s.pubCam) el.style.display = "none";
      return Promise.resolve();
    };
    s.switchCamera = function () {
      return AgoraRTC.getCameras().then(function (cams) {
        s.cameras = cams || [];
        if (s.cameras.length < 2 || !s.cam) return;
        s.camIndex = (s.camIndex + 1) % s.cameras.length;
        return s.cam.setDevice(s.cameras[s.camIndex].deviceId);
      });
    };
    s.applySpatial = function () {
      if (!s.client || !s.client.remoteUsers) return;
      var users = s.client.remoteUsers;
      if (!s.spatial || !s.selfPos) {
        for (var i = 0; i < users.length; i++) if (users[i].audioTrack) users[i].audioTrack.setVolume(100);
        return;
      }
      var ranked = [];
      for (var j = 0; j < users.length; j++) {
        var uid = String(users[j].uid);
        var pos = s.remotePos[uid];
        ranked.push({ user: users[j], d: pos ? dist(s.selfPos, pos) : 0, known: !!pos });
      }
      ranked.sort(function (a, b) { return a.d - b.d; });
      var kept = 0;
      for (var k = 0; k < ranked.length; k++) {
        var track = ranked[k].user.audioTrack;
        if (!track) continue;
        var audible = !ranked[k].known || (ranked[k].d <= s.range && kept < s.maxRecv);
        if (audible && ranked[k].known) kept++;
        var vol = 100;
        if (ranked[k].known) vol = audible ? Math.max(0, Math.round(100 * (1 - ranked[k].d / Math.max(0.01, s.range)))) : 0;
        track.setVolume(vol);
      }
    };
    s.leave = function () {
      s.leaving = true;
      var client = s.client;
      s.joined = false;
      s.client = null;
      s.mode = "";
      if (s.mic) { try { s.mic.close(); } catch (e) {} s.mic = null; }
      if (s.cam) { try { s.cam.close(); } catch (e) {} s.cam = null; }
      var done = client ? client.leave().catch(function () {}) : Promise.resolve();
      return done.then(function () { s.leaving = false; });
    };
    s.dispose = function () {
      return s.leave().then(function () {
        var unlock = document.getElementById("nl-agora-unlock");
        if (unlock) unlock.remove();
        var local = document.getElementById("nl-agora-local");
        var remote = document.getElementById("nl-agora-remote");
        if (local) local.remove();
        if (remote) remote.remove();
      });
    };

    window.__nlAgora = s;
    return s;
  },

  NL_AgoraSetTarget__deps: ["$nlAgoraGet"],
  NL_AgoraSetTarget: function (namePtr) { nlAgoraGet().target = UTF8ToString(namePtr); },

  NL_AgoraInitialize__deps: ["$nlAgoraGet"],
  NL_AgoraInitialize: function (appIdPtr) {
    var s = nlAgoraGet();
    s.appId = UTF8ToString(appIdPtr);
    s.queue(function () { return s.loadSdk(); });
    return 0;
  },

  NL_AgoraJoin__deps: ["$nlAgoraGet"],
  NL_AgoraJoin: function (tokenPtr, channelPtr, uidPtr, profile, role, pubMic, pubCam, subAudio, subVideo) {
    var s = nlAgoraGet();
    if (!s.appId) return -1;
    s.queue(function () {
      return s.join({
        token: UTF8ToString(tokenPtr),
        channel: UTF8ToString(channelPtr),
        uid: UTF8ToString(uidPtr),
        profile: profile,
        role: role,
        pubMic: pubMic,
        pubCam: pubCam,
        subAudio: subAudio,
        subVideo: subVideo
      });
    });
    return 0;
  },

  NL_AgoraUpdateOptions__deps: ["$nlAgoraGet"],
  NL_AgoraUpdateOptions: function (role, pubMic, pubCam) {
    var s = nlAgoraGet();
    s.queue(function () { return s.applyPublish(role, pubMic, pubCam); });
    return 0;
  },

  NL_AgoraLeave__deps: ["$nlAgoraGet"],
  NL_AgoraLeave: function () {
    var s = nlAgoraGet();
    s.gen++;
    s.queue(function () { return s.leave(); });
  },

  NL_AgoraRenewToken__deps: ["$nlAgoraGet"],
  NL_AgoraRenewToken: function (tokenPtr) {
    var s = nlAgoraGet();
    var token = UTF8ToString(tokenPtr);
    s.queue(function () { if (s.client && s.joined) return s.client.renewToken(token); });
    return 0;
  },

  NL_AgoraStartPreview__deps: ["$nlAgoraGet"],
  NL_AgoraStartPreview: function () {
    var s = nlAgoraGet();
    s.queue(function () { return s.startPreview(); });
    return 0;
  },

  NL_AgoraStopPreview__deps: ["$nlAgoraGet"],
  NL_AgoraStopPreview: function () { nlAgoraGet().queue(function () { return nlAgoraGet().stopPreview(); }); },

  NL_AgoraMuteLocalAudio__deps: ["$nlAgoraGet"],
  NL_AgoraMuteLocalAudio: function (mute) { var s = nlAgoraGet(); if (s.mic) s.mic.setEnabled(!mute); },

  NL_AgoraMuteLocalVideo__deps: ["$nlAgoraGet"],
  NL_AgoraMuteLocalVideo: function (mute) { var s = nlAgoraGet(); if (s.cam) s.cam.setEnabled(!mute); },

  NL_AgoraSwitchCamera__deps: ["$nlAgoraGet"],
  NL_AgoraSwitchCamera: function () { nlAgoraGet().queue(function () { return nlAgoraGet().switchCamera(); }); },

  NL_AgoraEnableSpatial__deps: ["$nlAgoraGet"],
  NL_AgoraEnableSpatial: function (enabled) { var s = nlAgoraGet(); s.spatial = !!enabled; s.applySpatial(); },

  NL_AgoraSetSpatialLimits__deps: ["$nlAgoraGet"],
  NL_AgoraSetSpatialLimits: function (range, maxCount) {
    var s = nlAgoraGet();
    s.range = range;
    s.maxRecv = maxCount | 0;
    s.applySpatial();
  },

  NL_AgoraClearRemotes__deps: ["$nlAgoraGet"],
  NL_AgoraClearRemotes: function () { var s = nlAgoraGet(); s.remotePos = {}; s.applySpatial(); },

  NL_AgoraRemoveRemote__deps: ["$nlAgoraGet"],
  NL_AgoraRemoveRemote: function (uidPtr) {
    var s = nlAgoraGet();
    delete s.remotePos[UTF8ToString(uidPtr)];
    s.applySpatial();
  },

  NL_AgoraUpdateSelf__deps: ["$nlAgoraGet"],
  NL_AgoraUpdateSelf: function (x, y, z) { var s = nlAgoraGet(); s.selfPos = [x, y, z]; s.applySpatial(); },

  NL_AgoraUpdateRemote__deps: ["$nlAgoraGet"],
  NL_AgoraUpdateRemote: function (uidPtr, x, y, z) {
    var s = nlAgoraGet();
    s.remotePos[UTF8ToString(uidPtr)] = [x, y, z];
    s.applySpatial();
  },

  NL_AgoraSetVideoRect__deps: ["$nlAgoraGet"],
  NL_AgoraSetVideoRect: function (idPtr, x, y, w, h, screenW, screenH) {
    nlAgoraGet().placeVideo(UTF8ToString(idPtr), x, y, w, h, screenW, screenH);
  },

  NL_AgoraSetVideoVisible__deps: ["$nlAgoraGet"],
  NL_AgoraSetVideoVisible: function (idPtr, visible) {
    var el = document.getElementById(UTF8ToString(idPtr));
    if (el) el.style.display = visible ? "block" : "none";
  },

  NL_AgoraDispose__deps: ["$nlAgoraGet"],
  NL_AgoraDispose: function () {
    var s = nlAgoraGet();
    s.gen++;
    s.queue(function () { return s.dispose(); });
  }
});
