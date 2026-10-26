mergeInto(LibraryManager.library, {
  NL_PatternSpeak: function (textPtr, langPtr) {
    try {
      if (!window.speechSynthesis) return 0;
      var u = new SpeechSynthesisUtterance(UTF8ToString(textPtr));
      u.lang = UTF8ToString(langPtr) || "ja-JP";
      u.rate = 0.9;
      window.speechSynthesis.cancel();
      window.speechSynthesis.speak(u);
      return 1;
    } catch (e) { return 0; }
  }
});
