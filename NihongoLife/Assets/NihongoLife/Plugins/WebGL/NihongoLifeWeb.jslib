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

  NL_GetDprCap: function () {
    try {
      var parsed = parseFloat(localStorage.getItem('nihongo.webgl.dprCap'));
      if (!isNaN(parsed) && parsed >= 0.5 && parsed <= 3) return parsed;
    } catch (e) {}
    return 1.5;
  }
});
