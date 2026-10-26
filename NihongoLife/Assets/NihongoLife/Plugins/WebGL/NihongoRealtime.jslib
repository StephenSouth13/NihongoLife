mergeInto(LibraryManager.library, {
  NihongoRealtimeConnect: function (urlPtr, gameObjectPtr) {
    var url = UTF8ToString(urlPtr);
    var target = UTF8ToString(gameObjectPtr);
    var old = window.__nihongoRealtimeSocket;
    if (old) {
      old.__intentionalClose = true;
      try { old.close(1000, "reconnect"); } catch (_) {}
    }

    var socket;
    try {
      socket = new WebSocket(url);
    } catch (error) {
      SendMessage(target, "OnWebGLRealtimeError", String(error));
      return;
    }
    window.__nihongoRealtimeSocket = socket;
    socket.onopen = function () {
      if (window.__nihongoRealtimeSocket === socket)
        SendMessage(target, "OnWebGLRealtimeOpen", "");
    };
    socket.onmessage = function (event) {
      if (window.__nihongoRealtimeSocket === socket && typeof event.data === "string")
        SendMessage(target, "OnWebGLRealtimeMessage", event.data);
    };
    socket.onerror = function () {
      if (window.__nihongoRealtimeSocket === socket)
        SendMessage(target, "OnWebGLRealtimeError", "WebSocket network error");
    };
    socket.onclose = function (event) {
      if (window.__nihongoRealtimeSocket === socket) {
        window.__nihongoRealtimeSocket = null;
        if (!socket.__intentionalClose)
          SendMessage(target, "OnWebGLRealtimeClose", "WebSocket closed (" + event.code + ")");
      }
    };
  },

  NihongoRealtimeSend: function (messagePtr) {
    var socket = window.__nihongoRealtimeSocket;
    if (socket && socket.readyState === WebSocket.OPEN)
      socket.send(UTF8ToString(messagePtr));
  },

  NihongoRealtimeClose: function () {
    var socket = window.__nihongoRealtimeSocket;
    window.__nihongoRealtimeSocket = null;
    if (socket) {
      socket.__intentionalClose = true;
      try { socket.close(1000, "client disconnect"); } catch (_) {}
    }
  }
});
