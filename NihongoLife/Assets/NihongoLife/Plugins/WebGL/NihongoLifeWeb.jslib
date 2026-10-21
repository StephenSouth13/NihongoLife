// Browser helpers for the WebGL build (only compiled into WebGL players).
mergeInto(LibraryManager.library, {
  // Files under Application.persistentDataPath live in IndexedDB (IDBFS): flush them so a save survives a reload.
  NL_SyncPersistentData: function () {
    if (typeof FS !== 'undefined' && FS.syncfs) FS.syncfs(false, function (err) { if (err) console.warn('[NihongoLife] syncfs: ' + err); });
  }
});
