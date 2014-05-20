---
layout: post
title: "On A Lighter Note: SocketFlags.MaxIOVectorLength"
---
Today I was just working along, minding my own business, when out of the blue my mind jumped back to something strange I had seen over a year ago. (Is anyone else insane like that, or is it just me?)

The seldom-used [SocketFlags](http://msdn.microsoft.com/en-us/library/system.net.sockets.socketflags.aspx) enumeration serves a dual purpose: it can represent flags passed to the Send or Receive operation, and it also represents flags passed back from the Send or Receive operation.

Reading through the enumeration values is pretty much straightforward: it's fairly obvious which ones are meant as "input" or "output" parameters, and what their meanings are. One value, however, is rather strange: MaxIOVectorLength, which (according to the MSDN documentation) "Provides a standard value for the number of WSABUF structures that are used to send and receive data."

That should give anyone pause. That value is clearly not a _flag_. It would make (a twisted sort of) sense if, by passing that flag, you could specify the maximum I/O vector length. But a quick look at the Send and Receive methods make it clear that this flag is not "enabling" some other parameter.

The fact is: this flag value should simply not exist. The value is real enough; it's defined in WinSock2.h as "MSG_MAXIOVLEN". However, it defines a limitation in the WinSock implementation, _not_ a flag for Send or Recv.

Why do I find this amusing? Because someone, during the devlopment of the .NET framework, had to track down all the meanings of these flags. This person undoubtedly discovered that MSG_MAXIOVLEN was undocumented in its header file, and learned its meaning from someone else (likely someone responsible for the WinSock code). And in all of that research, that person never once noticed that this value was _obviously_ not a flag? Not only that, but all of the reviewers reading this documentation never once realized how its description was completely different than all of the other descriptions!

This is a case of someone working too fast, and no one catching their fundamental mistake. The other flag values (which existed in WinSock.h with names like "MSG_OOB", "MSG_PEEK", and "MSG_DONTROUTE") had straightforward translations to SocketFlags, and MSG_MAXIOVLEN somehow got lumped in with them.

P.S. An interesting futher note: the person who put MaxIOVectorLength into SocketFlags _correctly_ did not include a translation of MSG_INTERRUPT. The MSG_INTERRUPT flag was used to signal WinSock that the Send/Recv is being called in a hardware interrupt context (and therefore WinSock could not call other Windows methods). That was back in the 16-bit Windows days, and that flag is no longer used.

P.P.S. Bonus amusing fact: SocketFlags.MaxIOVectorLength has the same value as MSG_INTERRUPT. He, he, he... I just _wonder_ what would happen if someone ever used it... :)

