// Browser helpers for the WebGL build (only compiled into WebGL players).
mergeInto(LibraryManager.library, {
  // Files under Application.persistentDataPath live in IndexedDB (IDBFS): flush them so a save survives a reload.
  NL_SyncPersistentData: function () {
    if (typeof FS !== 'undefined' && FS.syncfs) FS.syncfs(false, function (err) { if (err) console.warn('[NihongoLife] syncfs: ' + err); });
  },

  // Read by the WebGL template before Unity starts. 1.5 is the default cap.
  NL_SetDprCap: function (cap) {
    try { localStorage.setItem('nihongo.webgl.dprCap', String(cap)); } catch (e) {}
  },

  NL_GetDprCap: function () {
    try {
      var parsed = parseFloat(localStorage.getItem('nihongo.webgl.dprCap'));
      if (!isNaN(parsed) && parsed >= 0.5 && parsed <= 3) return parsed;
    } catch (e) {}
    return 1.5;
  }
});
