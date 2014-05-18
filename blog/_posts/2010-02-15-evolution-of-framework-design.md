---
layout: post
title: "Evolution of Framework Design Guidelines?"
tags: [".NET", "Books"]
---


Joe Duffy recently [blogged about one of his comments](http://www.bluebytesoftware.com/blog/PermaLink,guid,b9072376-beaf-4d17-a6c4-4bfccfbd34b0.aspx) in the book [Framework Design Guidelines](http://www.amazon.com/gp/product/0321545613?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0321545613). His comment is rather brief in the book, but he expounds on it nicely in his blog: essentially, he suggests defining only the minimum set of operations in an interface, and using extension methods to provide "default" implementations of other "interface methods". This is the approach that I've been calling [extension-based types](http://blog.stephencleary.com/2010/01/extension-based-types.html). He also calls out the problems with extension properties and the non-virtual nature of overriding extension functions (these problems are explored on my earlier blog post as well).





I've hesitated mentioning the book in the past, because any review coming from me would be a bit harsh... To be blunt, I think a good amount of the book is obviously correct (in some cases, painfully obvious), and a good amount is flat-out bad advice. If you want an explaination of why the first version of the BCL was designed the way it was, then this is a decent reference. However, the book suggests that its guidelines should be accepted for other general .NET libraries, and there I must disagree. Microsoft themselves have evolved far from many of the guidelines in this book.





That said, there are a few gems. I would say that it's worth owning; just keep in mind while reading that it is not the gospel. I'd like to see a third edition, updated with the current Microsoft practices and spending more than a couple sentences on newer concepts such as extension-based types.

