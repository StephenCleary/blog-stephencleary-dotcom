---
layout: post
title: "Cross-browser Silverlight"
tags: [".NET", "Silverlight"]
---


I tried becoming a web developer back in the version 4 browser war days; my scars from that time made me swear off web page writing for years, until recently. In my mind, one of the greatest advantages of Silverlight is that it completely kills all that nasty browser compatibility stuff. However, when writing Silverlight controls that interact more heavily with HTML, cross-browser issues begin to rise up from the grave.



## Partial URLs



Partial URLs are transformed by Internet Exporer into absolute URLs as they enter the object model. This can cause exceptions from the Uri constructor if you're only passing a single string argument. The workaround that I've opted to use is to always use the Uri constructor that takes a Uri and a string as arguments, and I pass in the current page's Uri as the first argument.





I recommend never using the Uri constructor taking a single string parameter; in the following example, the <a> item has an href attribute similar to "/", which is quietly transformed by IE into a full "http://www.tempuri.org/":




// Fails on Google Chrome
new Uri(item.GetAttribute("href"))




Instead, pass the document Uri as the context of the relative Uri string. If the second parameter is actually an absolute Uri (e.g., when running in Internet Explorer), the context Uri is ignored:




// Works on both Chrome and IE
new Uri(HtmlPage.Document.DocumentUri, item.GetAttribute("href"))


## Text Content



Internet Explorer has an "innerText" attribute, while the DOM supports a "textContent" property. A simple extension method on HtmlElement suffices to get the text content of a node:




public static string GetTextContent(this HtmlElement htmlElement)
{
    string ret = htmlElement.GetProperty("textContent") as string;
    if (string.IsNullOrEmpty(ret))
    {
         ret = htmlElement.GetAttribute("innerText");
         if (ret == null)
         {
             return string.Empty;
         }
    }

    return ret;
}


## DocumentReady



A long-standing bug in Internet Explorer (at least since IE6; still present in IE8) causes document.onreadystatechange to be fired incorrectly, and the standard DOMContentLoaded isn't suppored. This means that HtmlPage.Document.DocumentReady _will fire_ when the document is not ready. This behavior has [been discussed](http://forums.silverlight.net/forums/p/82810/193149.aspx#193149) on a Silverlight forum.





I've also seen some situations where the jQuery ready handlers (and the window.onload handlers) will fire before the Silverlight control begins executing; this can happen if the page contains several images. To be safe, I recommend having both jQuery and Silverlight register "readyness", and the last one to register kicks off the initialization code.





This has a few steps; I'll be using as an example a Silverlight menu control I wrote that dynamically builds its menu from an unordered list in the HTML. First, the "initialization" code must be made a separate method in the Silverlight control (in this example, it's "BuildMenu"):




// In the Page constructor (there's only one page in this simple menu control):
HtmlPage.RegisterScriptableObject("SLMenu", this);

// Also defined as part of the Page class:
[ScriptableMember]
public void BuildMenu()




Define the "readyness" flags in plain, top-level JavaScript before the Silverlight control; this ensures that they're interpreted immediately:




// Top-level code; not in a function!
var silverlightLoaded = false;
var htmlLoaded = false;




Also define the JavaScript function that the Silverlight control should call in top-level JavaScript before the Silverlight control:




// Top-level code; not in a function!
MenuOnLoad = function() {
  if (!silverlightLoaded) {
    silverlightLoaded = true;
    if (htmlLoaded) {
      menuControl.Content.SLMenu.BuildMenu();
    }
  }
}




The code above is straightforward; it marks Silverlight as having loaded and then invokes the initialization code if the HTML has already loaded. The code run by the HTML when it loads is similar (shown here using jQuery, but window.onload could be used as well):




$(function() {
  if (!htmlLoaded) {
    htmlLoaded = true;
    if (silverlightLoaded) {
      menuControl.Content.SLMenu.BuildMenu();
    }
  }
});




Finally, the Silverlight control must invoke the JavaScript "MenuOnLoad" function when it is loaded. This must come after its registration with the browser:




// In the Page constructor (there's only one page in this simple menu control):
HtmlPage.RegisterScriptableObject("SLMenu", this);
HtmlPage.Window.Invoke("MenuOnLoad");


## Conclusion



Of course, these few examples are just minor inconsistencies. I'm sure that many more ugly browser incompatibilities will become troublesome over the next few months.





The Silverlight menu used as the example above is live at this site: [http://www.landmarkbaptist.ws/](http://www.landmarkbaptist.ws/). If it doesn't work properly for you, please let me know! (I'm particularly interested if there are any holes in my initialization serialization logic).





I'll keep posting as I find more problems. :)

