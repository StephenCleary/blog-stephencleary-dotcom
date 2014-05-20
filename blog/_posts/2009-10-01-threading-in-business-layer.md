---
layout: post
title: "Threading in the Business Layer"
---
I don't have a great deal to say on the matter. This is just something that I've observed when working on a few different projects.

When writing end-user applications, I find my business layer tends to be stateful and affinitized to the UI thread. This just makes it easier, particularly when doing MVVM.

However, when writing web services, I find my business layer tends to be stateless and free-threaded. Well, sometimes they're not stateless, but they're nearly always free-threaded. This makes it easier (and more scalable) when dealing with web requests.

I don't know if this is good or bad. [Lhotka](http://www.amazon.com/gp/product/1430210192?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=1430210192), for example, has a very different approach to business objects. I really enjoyed reading his book (especially the first few chapters which deal with enterprise-level design in general), but I'm still not sold on his idea of literally moving business objects from one process to another. Maybe I'm just stuck in the 1980's...

Then again, I've never had to deal with a truly distributed application. Lhotka's examples are really based on a _single_ application that needs to be distributed for performance or reliability reasons. When I work with GUI apps and web services, I always end up treating them as _different_ applications. I think that's where the differences in our BO models come from.

Like I said, I don't know if my approach is best or not. This was just an observation. If you do have an opinion, feel free to let me know!

