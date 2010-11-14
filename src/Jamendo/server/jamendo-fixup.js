// Copyright 2010 Bertrand Lorentz
// Written by Bertrand Lorentz
// Made available under the MIT license:
// http://www.opensource.org/licenses/mit-license.php

Jamendo_ENV["PREFS"] = {"download":"", "stream":"xspf_ogg2"}

for (var l = document.getElementsByClassName ('jambutton'), i = 0;
    l && i < l.length; i++) {

    for (var e = l[i].getElementsByTagName('a'), j = 0;
        e && j < e.length; j++) {
        var click_text = e[j].getAttribute('onclick');
        if (click_text && click_text.indexOf('Jamendo.page.download') >= 0) {
            e[j].onclick = '';
        }
    }
}
