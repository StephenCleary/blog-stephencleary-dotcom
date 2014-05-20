---
layout: post
title: "An Async Horror Story"
---
Congratulations to the Async/Await team on their RTW release!



This is a **huge** milestone for .NET. Personally, I think Async will have a monumental impact on how we write code. It makes _maintainable asynchronous code_ a reality.



Today, I will **not** write a long blog post about how to use Async or some obscure synchronization pitfall. Instead, I will tell a horror story from long ago... a story that has never before been told, but I assure you it is true. For I was there.



Sit back, young programmers, and listen to a tale of Async... from the Dark Ages!!! [lightning, thunder]



## The Setting

The year was 2000. I was a young programmer at that time: only three years of work experience, all at a small engineering company. But I had several successful projects under my belt, I had contributed to [Boost](http://www.boost.org), and I felt confident that I could tackle anything. In this pre-September 11th world, optimism reigned.



## The Client

<div style="float:right;">
<img border="0" height="126" width="190" src="http://3.bp.blogspot.com/-RxuOHzFP7tA/T_oqOGyG45I/AAAAAAAAHPw/tiL2Kvvnc_c/s320/Building.jpg" />
</div>

A certain large newspaper was expanding. They built a new press building and had plans to outfit it to the tune of $40 million. Their new Swiss printing press was 750 tons; and was capable of printing 70,000 pages per hour, moving the paper through at 30 mph. The paper was delivered at the bottom of the presses and traveled up through 27 feet of machinery. It was quite a sight!



But the press was only $25 million of the story. Besides the building and all of the planning software, they needed a way to get the paper to the press on time, and that's where we came in.



My company had recently hired a "rock star" developer, who had already been assigned another newsprint project. He assured the software department heads that this new newsprint project would only require minor tweaks and configuration changes to the software he was developing for the other client (which was his first project for our company). Unlike our traditional procedural design, the Rock Star's code reveled in "abstraction" and "OOP." As the only other programmer in the company who had "done OOP," I was slated to work as the Rock Star's assistant programmer.



> In one conversation around this time, I was opposing what I saw as unnecessary complexity in the system (I had been asked to abstract the IoC component by using DI). The Rock Star responded by giving me an additional assignment: a module which would continuously check memory for errors while the system was running. At that point, the conversation had just gotten too ridiculous; I walked away without saying a word.
> 
> 
> 
> The software was sufficiently complex to require its own program for modifying the configuration (called, unsurprisingly, "The Configurator").
> 
> 
> 
> Long-time readers of [The Daily WTF](http://thedailywtf.com) will immediately realize what will happen to these projects.


I was nominally assigned to the new newspaper project (in addition to my other tasks). The Rock Star assured me that the project would be easy, once he actually gave me the holy grail code base.



## Clouds on the Horizon

Deadlines slipped. Weeks became months. It was now 2001. More deadlines slipped. I was routinely assured that the code would be there, and that it would be better than my wildest imagination. The software department heads hung on the Rock Star's every word, and I had no option but to go along. I had no code to work with, so after finishing my other projects, I played around with alternative programming styles. I was experimenting with something I called "single-threaded concurrency," and began exploring how the newsprint project would look if it was coded in that fashion.



Finally, the time came for the installation. Ordinarily, the projects at my company would run for 6-9 months followed by a 3-week on-site installation. Inevitably, many minor issues were brought up during the installation, and having a programmer on-site encouraged quick resolutions.



I was very nervous on my first flight out to the job site for the install, since I was flying without any code. But I was super-seriously assured that the code would be in my inbox when I got there.



Of course, it wasn't. I was given instructions to go to the customer site and "look busy" every day while the Rock Star put the finishing touches on his masterpiece.



Another week went by. I continued my "single-threaded concurrency" experiment. Another week or two went by. I came home for the weekend and then returned to the job site - very unusual for my company's projects. More weeks went by.



Excuse after excuse was given for the lack of code. The most memorable one was that the Rock Star's hard drive had crashed, and he was working day and night (at home, naturally) to recover it.



Fortunately, the other vendors for the newsprint system were even later than we were with their deliverables, so the client didn't blame us for the delays.



## The Storm

<div style="float:right;">
<img border="0" height="214" width="87" src="http://1.bp.blogspot.com/-0yRR_vK72q4/T_oq1CUcTFI/AAAAAAAAHP8/B1zTaPjBzDw/s320/Press.jpg" />
</div>

Finally, some six weeks after the official contract deadline, the Rock Star delivered the code base. It was a steaming pile of useless nonsense. It did not even compile. I took a few hours to go through it, and it contained a ton of hard-coded logic for the Rock Star's newsprint project that would have to be changed to work for any other newsprint system. In short, it was a disaster, and almost completely useless for our new client.



Terrorists attacked the World Trade Center.



And the Rock Star quit, taking the glowing recommendations of our software department heads with him. He had not completed a single project, but - amazingly - they still believed him when he said we just had to "snap together a few components" to complete both of his projects.



The new newsprint project was officially turned over to me. The Rock Star's former project was passed off to an Unsuspecting Teammate, who truly had no idea of the mess he was getting into - but he was (and still is) the best programmer I've ever worked with, so I didn't worry too much about him. I had big enough problems of my own.



The Rock Star's code base was downright unusable. My only options were to restart the project from scratch, or to expand my single-threaded concurrency experiment into an actual solution. I knew the rewrite would take many months, and the ire of the client would fall on us in full force. I was also tempted by my curiosity: _could_ I, in fact, create a full solution using these techniques?



I chose to continue my experiment. It was my choice to bring it to life.



## The Monster

I will now attempt to explain my "single-threaded concurrency" experiment. I leave it to the reader to judge whether it was inherently evil, or whether it was merely born at the wrong time and in the wrong place.



Please do keep in mind that this was written in unmanaged C++ with the NT4 API: there were no spin locks, no thread pool, no garbage collection, or many other things that programmers can take for granted these days.



In the unmanaged world at that time, the most common type of asynchronous operation was overlapped I/O. My early experiment used overlapped I/O exclusively, with some APCs thrown in for good measure, but I quickly discovered how difficult it was to _extend_. I shifted to using HANDLEs.



> For my .NET-only readers, you can think of a HANDLE as like a Task.


The entire system was conceptually built around a single thread. The main loop of this thread essentially just called WaitForMultipleObjects on a dynamic array of HANDLEs and executed completion callbacks. Completion callbacks would add and remove HANDLEs from the main array as necessary. This approach was fully extensible using the monkey wrench of synchronization primitives: the Manual Reset Event.



> For my .NET-only readers: A Manual Reset Event is analogous to a TaskCompletionSource. There was no thread pool, so I used WaitForMultipleObjects to make a "thread pool" that would only ever have one thread.


There is one major limitation with this approach: everything **must** be asynchronous. I was fairly sure, however, that I could _make_ everything asynchronous, one way or another.



As the system grew, I encountered the 64-HANDLE limit: WaitForMultipleObjects could only wait for 64 HANDLEs at a time. This was insufficient, so I wrote the _most complex_ software component I have ever written: The Event Demultiplexer!



The Event Demultiplexer, conceptually, was quite simple. It was a WaitForMultipleObjects loop that was unlimited in its number of HANDLEs. That's it. When it came to implementation, however, it was considerably difficult. It had to manage its own threads (no thread pool, remember?), and doing all of that in a thread-safe way was not trivial.



> Curiously, the 64-handle limitation still exists in .NET (in WaitHandle.WaitAll/WaitAny). I had thought they would abstract it away! It's not so important anymore, though: Task's WaitAll/WhenAll/WaitAny/WhenAny do not use the same mechanism and are not subject to a 64-task limit.


Of course, there were many external problems as well. The remote database driver had a nasty habit of crashing its host process every time the server was unreachable. The local database did not support asynchronous operations (and required regular offline compaction). The robot controlled via RS232 did not like commands sent too quickly (apparently, they only tested with slow-typing humans). The press communications used an underdocumented protocol over an unusual bus. Pretty much the only thing that worked perfectly was the PLC talking over OPC.



<div style="text-align:center;">
<img border="0" height="240" width="320" src="http://4.bp.blogspot.com/-QTztUJPGNoo/T_orKl0VLqI/AAAAAAAAHQI/9SYTNcKJzMU/s320/Operator.jpg" />
</div>

Every problem was eventually solved. The remote database driver was encapsulated into its own child process and restarted whenever it crashed. The local database got its own dedicated thread which exposed an asynchronous API to the core system. I added delays to the RS232 interface, using the wonderful Waitable Timer (interestingly, not available in .NET). The press communications were developed "by observation."



At the end, I had a working system. It was the first 100% asynchronous system I had ever written; possibly the first (non-trivial) 100% asynchronous system ever.



I was proud of my creation.



## Darkness Falls

For many years, I was proud of my accomplishment. The client _did_ discover that I essentially wrote the entire system on-site, but we were not responsible for any major delays, so we didn't get nailed. Also, I learned a _lot_ about asynchronous programming: you have to turn your code "inside out" to do it right. This valuable experience enabled me to create a fully-asynchronous component a couple of years later when our communications system changed from serial to TCP/IP.



> Note: Only old-school async requires you to write your code "inside out." The new async/await support in .NET does the rewriting for you, so you can write asynchronous code _so much easier!_


However, it gradually became apparent that I was too proud of my creation. Being a fully asynchronous system (and therefore completely different than any of our other systems), it was difficult for me to maintain. And, as difficult as it was for me to maintain it, it was near-impossible for anyone else to maintain it!



True, I had pulled off a working system in a bad situation. But that system wouldn't last - _couldn't_ last. It was years later when I admitted to myself that it was, in fact, [a](http://thedailywtf.com/Articles/Avoiding-Development-Disasters.aspx) [failure](http://thedailywtf.com/Articles/What_Could_Possibly_Be_Worse_Than_Failure_0x3f_.aspx).



I had created a monster.



Time passed. My company moved to Detroit, and I stayed here. The software department got absorbed by another company, and then the entire company got bought by yet another company.



Now, many years later, I still occasionally wonder about my creation - my first fully-asynchronous system. Is it still living in that server room, sending out commands to move rolls of paper hither and yon? Does it horrify innocent maintenance programmers? Or has it been replaced by another system that is easier to understand and maintain?



Sometimes, late at night, I feel a profound sense of guilt. My professional pride wants me to go back... to replace it with a system that would not shame me any more... to kill the monster.



Perhaps someday I will be brave enough to face my creation again.



Perhaps it is too late. Too late for me.



Yet, I have a ray of hope.



My hope is that all programmers will learn the new way of async. With the new async support in .NET, no one should ever have to create another monster.



There is always hope.

