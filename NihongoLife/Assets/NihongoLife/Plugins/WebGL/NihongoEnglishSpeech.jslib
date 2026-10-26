mergeInto(LibraryManager.library, {
  NihongoLifeSpeakEnglish: function (textPtr) {
    if (!window.speechSynthesis) return;
    window.speechSynthesis.cancel();
    var utterance = new SpeechSynthesisUtterance(UTF8ToString(textPtr));
    utterance.lang = 'en-US';
    utterance.rate = 0.88;
    window.speechSynthesis.speak(utterance);
  }
});
