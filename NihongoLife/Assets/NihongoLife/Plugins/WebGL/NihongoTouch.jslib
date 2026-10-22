mergeInto(LibraryManager.library, {
  NihongoCoarsePointer: function () {
    try {
      if (window.matchMedia && window.matchMedia("(pointer: coarse)").matches) return 1;
    } catch (e) {}
    return 0;
  }
});
