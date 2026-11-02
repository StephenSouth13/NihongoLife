// Browser helpers for the WebGL build (only compiled into WebGL players).
mergeInto(LibraryManager.library, {
  // Files under Application.persistentDataPath live in IndexedDB (IDBFS): flush them so a save survives a reload.
  NL_SyncPersistentData: function () {
    if (typeof FS !== 'undefined' && FS.syncfs) FS.syncfs(false, function (err) { if (err) console.warn('[NihongoLife] syncfs: ' + err); });
  },

  // Saves when the tab is hidden or closing (visibilitychange / pagehide / beforeunload): Unity writes the save into the
  // virtual file system, then it is synced to IndexedDB so a reload or closed tab never loses progress.
  NL_RegisterPersistHooks: function (goPtr) {
    var target = UTF8ToString(goPtr);
    if (window.__nlPersistHooked) return;
    window.__nlPersistHooked = true;
    function sync() { try { if (typeof FS !== 'undefined' && FS.syncfs) FS.syncfs(false, function () {}); } catch (e) {} }
    function hide(reason) {
      try { if (typeof SendMessage !== 'undefined') SendMessage(target, 'OnBrowserHide', reason); } catch (e) {}
      sync();
    }
    document.addEventListener('visibilitychange', function () { if (document.visibilityState === 'hidden') hide('visibilitychange'); });
    window.addEventListener('pagehide', function () { hide('pagehide'); });
    window.addEventListener('beforeunload', function () { hide('beforeunload'); });
  },

  // Read by the WebGL template before Unity starts. 1.5 is the default cap.
  NL_SetDprCap: function (cap) {
    try { localStorage.setItem('nihongo.webgl.dprCap', String(cap)); } catch (e) {}
  },

  // Public client settings (Supabase URL + anon key, Agora App ID) written next to index.html at build time as
  // nl-config.js (window.NIHONGOLIFE_CONFIG). A browser cannot read LocalSecrets/secrets.json. Empty when absent.
  NL_GetWebConfig: function () {
    var text = '';
    try { if (window.NIHONGOLIFE_CONFIG) text = JSON.stringify(window.NIHONGOLIFE_CONFIG); } catch (e) {}
    var size = lengthBytesUTF8(text) + 1;
    var buffer = _malloc(size);
    stringToUTF8(text, buffer, size);
    return buffer;
  },

  // Web release check (Core/WebQaBridge, page URL with qa=1 only).
  // A lesson of the course library: YouTube's own player, placed over the game's player area (fractions of the canvas).
    NL_VideoShow: function (idPtr, x, y, width, height) {
        var id = UTF8ToString(idPtr);
        var canvas = document.querySelector("#unity-canvas") || document.querySelector("canvas");
        if (!canvas || !/^[\w-]{11}$/.test(id)) return;
        var frame = document.getElementById("nl-video");
        if (!frame) {
            frame = document.createElement("iframe");
            frame.id = "nl-video";
            frame.allow = "autoplay; encrypted-media; picture-in-picture; fullscreen";
            frame.setAttribute("allowfullscreen", "");
            frame.style.position = "fixed";
            frame.style.border = "0";
            frame.style.borderRadius = "12px";
            frame.style.zIndex = "5";
            canvas.parentNode.appendChild(frame);
        }
        var box = canvas.getBoundingClientRect();
        frame.style.left = (box.left + box.width * x) + "px";
        frame.style.top = (box.top + box.height * y) + "px";
        frame.style.width = (box.width * width) + "px";
        frame.style.height = (box.height * height) + "px";
        frame.src = "https://www.youtube-nocookie.com/embed/" + id + "?autoplay=1&rel=0";
    },

    NL_VideoHide: function () {
        var frame = document.getElementById("nl-video");
        if (frame) frame.parentNode.removeChild(frame);
    },

    NL_QaLog: function (type, messagePtr) {
    var q = window.__nlQa = window.__nlQa || { logs: [] };
    q.logs.push([type, UTF8ToString(messagePtr)]);
  },

  NL_QaReport: function (jsonPtr) {
    var q = window.__nlQa = window.__nlQa || { logs: [] };
    q.report = UTF8ToString(jsonPtr);
  },

  NL_GetDprCap: function () {
    try {
      var parsed = parseFloat(localStorage.getItem('nihongo.webgl.dprCap'));
      if (!isNaN(parsed) && parsed >= 0.5 && parsed <= 3) return parsed;
    } catch (e) {}
    return 1.5;
  }
});
