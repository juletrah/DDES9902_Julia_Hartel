// A2Submit.jslib
// Schickt die Messwerte eines Durchlaufs an ein Google-Formular.
//
// Warum nicht UnityWebRequest: das will die Antwort lesen, und Google
// schickt keine CORS-Freigabe mit. Der Aufruf scheitert dann, obwohl der
// Eintrag angekommen waere. Mit fetch und mode:'no-cors' geht die Anfrage
// raus, die Antwort bleibt fuer uns undurchsichtig - und genau das reicht,
// weil wir nichts zurueckbrauchen.

mergeInto(LibraryManager.library, {

  A2PostForm: function (urlPtr, bodyPtr) {
    var url  = UTF8ToString(urlPtr);
    var body = UTF8ToString(bodyPtr);

    try {
      fetch(url, {
        method: 'POST',
        mode: 'no-cors',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: body
      }).then(function () {
        console.log('[A2] metrics sent');
      }).catch(function (e) {
        console.warn('[A2] metrics not sent', e);
      });
    } catch (e) {
      console.warn('[A2] metrics not sent', e);
    }
  }

});
