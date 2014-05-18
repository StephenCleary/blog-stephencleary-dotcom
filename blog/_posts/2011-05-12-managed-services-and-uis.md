---
layout: post
title: "Managed Services and UIs"
tags: [".NET", "Windows Services"]
---


One common question that I've seen is how to display a UI from a service.





The answer is: "don't".





Usually, when someone asks this question, the correct solution is to change the application from a _service_ to a background application run whenever a user logs in (e.g., from the Startup folder), possibly with a tray icon. Occasionally, this isn't possible, and the correct solution in that case is to split the application into two applications: a service without a UI, and a UI front-end (which may be a backround application run automatically).





Unfortunately, some people try to push forward with the "service with a UI" approach. This is doomed to fail.



### Inevitable Failure



There are two hurdles to displaying a UI from a service; the first is architectural, and the second is technical.





The architectural hurdle is simply that displaying a UI from a service just doesn't make sense. A Win32 service is a program that runs (or can be run) any time the computer is running, regardless of whether or not there is a user logged in. It doesn't make much sense to talk about "displaying a UI" if there isn't a user to show it to. Also consider multi-user (terminal server or fast-user-switching) computers: _which_ user would see the UI?





The technical hurdle is a bit more complex. To summarize: services which display UIs are a security risk.





The Win32 windows messaging system was designed without security in mind. Before you get too mad at Microsoft, remember that the Internet (including email, TCP/IP, and HTTP) were designed without security, too. Back then, it was hard enough just to get it working, without worrying about someone deliberately trying to destroy it. Most security on the Internet today is due to wrapping the original insecure protocols in an encrypted, authenticated stream (SSL/TLS).



> In fact, when I first got on the Internet, the common instructions for setting up an email server explicitly stated that it should be set up as an open relay so that anyone could send email through it. Then someone invented spam. The instructions have since been revised.




Similarly, in the early days, Windows had no need for security. In early versions of Windows, multitasking was non-preemptive, so any program could effortlessly cause a denial-of-service attack. Furthermore, each program had direct access to hardware, and causing a complete system crash was trivial.





These days, the situation is much improved. On modern OSes (not including the 9x line), a user-mode program simply cannot crash the system; it can only crash itself. With almost every new OS, Microsoft has enhanced security by trusting programs less (e.g., User Account Control).





One attack vector for malicious programs is a _privilege escalation_. This is a way for an untrusted program to trick the OS into trusting it more. One privilege escalation attack that has been discovered is called a [shatter attack](http://en.wikipedia.org/wiki/Shatter_attack). This "shatter attack" is based on Win32 message passing.





In response, Microsoft made two changes starting in Vista: User Interface Privilege Isolation (see [New UAC Technologies for Windows Vista](http://msdn.microsoft.com/en-us/library/bb756960.aspx) ([webcite](http://www.webcitation.org/5yJMQ8H2i))); and Session 0 Isolation (see [this Application Compatibility blog post](http://blogs.technet.com/b/askperf/archive/2007/04/27/application-compatibility-session-0-isolation.aspx) ([webcite](http://www.webcitation.org/5yJcr5ySR)) or [this Word document](http://msdn.microsoft.com/en-us/windows/hardware/gg463353)).





User Interface Privilege Isolation is a simple system where less-trusted programs (such as Internet Explorer) are limited in which Win32 messages they may send to more-trusted programs (such as services). This doesn't prevent services from having UIs, but may trip up programmers if they try to communicate with their service via message passing.





Session 0 Isolation is more surprising to most programmers, simply because most programmers are not aware of desktops or window stations. The following MSDN resources provide a good intro to the concept:




- [Services, Desktops, and Window Stations (KB171890)](http://support.microsoft.com/kb/171890) ([webcite](http://www.webcitation.org/5yJMygiUo))
- [Process Connection to a Window Station](http://msdn.microsoft.com/en-us/library/ms684859.aspx) ([webcite](http://www.webcitation.org/5yJN86Gvo))
- [Thread Connection to a Desktop](http://msdn.microsoft.com/en-us/library/ms686744.aspx) ([webcite](http://www.webcitation.org/5yJNBEJc4))




In essence, the older versions of Windows (XP and earlier) would run services in the same session as the first user that logged on to the physical computer, and services displaying UIs would be seen by that user. Newer versions of Windows (Vista and later) run services in their own special session (Session 0), which has its own window station and desktop completely independent from anything the user sees.





Naturally, this broke a lot of existing services, so Microsoft implemented a couple of workarounds. One is the "Interactive Service" flag, which would allow a service to display a UI. Another is the Interactive Service Detection Service, which is a special service that detects dialogs on Session 0 and notifies the user (if any) of them.





It is possible to set the Interactive Service flag when installing a service (not through the regular .NET Framework APIs; you have to p/Invoke for it). That is a horrible hack and should never, ever be applied to a new application. Even with that flag, some notification systems may not work as expected (see the end of [this blog post from the security team](http://blogs.technet.com/b/voy/archive/2007/02/23/services-isolation-in-session-0-of-windows-vista-and-longhorn-server.aspx) ([webcite](http://www.webcitation.org/5yJd1Jb7p))).





Remember that the Interactive Service flag is a _backwards_ compatibility hack that weakens overall system security and may be removed from Windows vNext. Similarly, the Interactive Service Detection Service comes with this disclaimer: "This support might be removed from a future Windows release, at which time all applications and drivers must handle Session 0 isolation properly."





So - while it is _possible_ to hack together a service with a UI today - you'd only be setting yourself up for failure in the future.



## Update (2013-09-19):



Windows 8 (and Server 2012) [no longer allow interactive services by default](http://blogs.technet.com/b/home_is_where_i_lay_my_head/archive/2012/10/09/windows-8-interactive-services-detection-error-1-incorrect-function.aspx). So any service with a UI will fail.





Currently, you are allowed to hack the OS to re-enable interactive services by setting `HKLM\SYSTEM\CurrentControlSet\Control\Windows\NoInteractiveServices` to `0`. This is an OS-level hack that you must apply _in addition to_ the service-level Interactive Service flag hack.





As predicted, Microsoft is moving further and further away from interactive services. I _strongly_ recommend not using these hacks in production.

