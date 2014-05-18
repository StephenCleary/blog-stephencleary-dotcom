---
layout: post
title: "ReSharper and the Obscure CTS Corner Case"
tags: []
---


Like several other people, I collect tricky code snippets for fun. Today's image is courtesy of the Common Type System (part of the CLR). As such, it's not so much an artifact of the C# language as it is an artifact of the floating point standard.





Interestingly, the current version of ReSharper recommended a code transformation that is wrong.



{:.center}

[![](http://3.bp.blogspot.com/_lkN-6AUYgOI/TECl7qdiWRI/AAAAAAAADZI/jXf3PO-O9dI/s400/ReSharper_doubleNaN.PNG)](http://3.bp.blogspot.com/_lkN-6AUYgOI/TECl7qdiWRI/AAAAAAAADZI/jXf3PO-O9dI/s1600/ReSharper_doubleNaN.PNG)



Don't get me wrong; ReSharper is a great tool. This is the first time I've seen it make a mistake, and it's an obscure corner case. ReSharper did make another questionable recommendation a few weeks ago, and I felt the C# standard wasn't clear on the subject. However, Eric Lippert did confirm that ReSharper's refactoring was correct that time.



## Update, 2013-04-11:



Jon Skeet explores ReSharper's mistake around double NaN near the end of [his recent presentation](http://tv.jetbrains.net/videocontent/jon-skeet-inspects-resharper). He also throws a lot of other crazy situations at Re# trying to break it. Fun stuff!

