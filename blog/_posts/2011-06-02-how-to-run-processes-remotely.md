---
layout: post
title: "How to Run Processes Remotely"
---
Today I'm going to delve deeply into something I discovered many years ago (c. 2003). It's an interesting little trick that hopefully no one will ever have to use.

When a process running on one computer needs to perform some operation on _another_ computer, the common solution is to actually have two processes that use interprocess communication. The one process sends its commands to the other process, which executes them on behalf of the first process. Normally, one must install a server on one computer and a client on the other. So, if someone needs to perform an operation on another computer, then that computer must _already have_ the software installed.

However, there _is_ a way to send a program to a remote computer and run it, without having any special existing software on the target machine. This approach doesn't work in every situation, but it's useful to know. The command line programs in the famous [PSTools suite](http://technet.microsoft.com/en-us/sysinternals/bb896649) use the approach documented here to "inject" copies of themselves onto remote computers; this allows a simple form of remote administration. The white paper [PsExec Internals](http://www.ntkernel.com/?White_papers:PsExec_Internals) ([webcite](http://www.webcitation.org/5yUALT8gw)) includes the specific details for PsExec.

## Step 1: Establish an Authenticated Connection

### About Connections

A user session on one computer may have network connections to other computers. One common example is network drives; each network drive is a connection to another computer. Network connections may also exist without mapping a drive letter.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Network connections may be examined and modified using the [Windows Networking (WNet) API](http://msdn.microsoft.com/en-us/library/aa385406.aspx){:.alert-link} or the **net** command. Unfortunately, there are no .NET wrappers for this API in the BCL.
</div>

### About Authentication

Each network connection has to be authenticated, but there are situations where this happens automatically. When you map a network drive using Explorer, by default Windows will use your local logon to attempt to log onto the remote machine, and if it's accepted, you won't actually get prompted for credentials. This is particularly common in Domain environments.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

The **net use** command allows you to display current connections to other computers, and add or remove those connections.
</div>

### Authentication Quirks

Microsoft made the design decision that any number of network connections may exist between two different computers, but that the same credentials must be used for all those connections. You may use different credentials for connections to two different servers, but all connections to the same server must use the same credentials. According to a rather dated [KB106211](http://support.microsoft.com/kb/106211) ([webcite](http://www.webcitation.org/5yelY3I5Z)), this is done "for security purposes." The newer [KB183366](http://support.microsoft.com/kb/183366) ([webcite](http://www.webcitation.org/5yemC7rC8)) documents the limitation in more detail, but does not give a reason.

If you do attempt to use different credentials for different connections to the same server, you'll get a 1219 error: "Multiple connections to a server or shared resource by the same user, using more than one user name, are not allowed. Disconnect all previous connections to the server or shared resource and try again." I've also seen this error when Explorer tries to auto-reconnect its mapped drives and it gets confused; it appears to happen more commonly on wireless networks when resuming from a low-power state.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

There's a "greybeard" trick used to get around this limitation: connect to the IP address instead of the hostname (or, if you want more work, set up multiple hostnames for that server). The logic behind "the same server" appears to be just a string comparison. This workaround has been documented in [KB938120](http://support.microsoft.com/kb/938120){:.alert-link} ([webcite](http://www.webcitation.org/5yemWypLb){:.alert-link}).
</div>

There are some notable situations where it's not possible to establish an authenticated connection:

- If the target (server) machine is running a client OS "Home" edition (e.g., XP Home, Vista Home Basic, Vista Home Premium, Windows 7 Home Basic, Windows 7 Home Premium), then no authenticated connections are possible.
- If the target (server) machine is running a client OS "Professional" edition (e.g., XP Professional, Vista Business/Enterprise/Ultimate, Windows 7 Professional/Enterprise/Ultimate), then that machine must _either_ be a member of a domain _or_ turn off "simple file sharing" to support authenticated connections.

Note that if you're working in a domain enviroment, Everything Just Works. For the rest of us, we have to turn off "simple file sharing."

If the server is running a Home edition, or if it is not connected to a domain and is using simple file sharing, then it does not support authenticated connections. Instead, every incoming network connection is authenticated with the Guest account; see [KB300489](http://support.microsoft.com/kb/300489) ([webcite](http://www.webcitation.org/5yenW0M9U)).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Another non-authenticated approach is to use _null sessions_, which are truly anonymous. This means they work even if the Guest account is disabled. Null sessions are disabled by default and considered a security risk.
</div>

To send a program to a remote computer, you'll need an authenticated connection. A Guest authentication (or null session) is insufficient.

### Common Shares

There are some hidden network shares for Windows systems. They are recreated automatically on reboot if they've been deleted.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Hidden shares are not shown in the normal GUI, but they can be displayed by the command **net share**.
</div>

The standard hidden share names that are important to us are:

 - **IPC$** - An share that is used only for authentication.
 - **ADMIN$** - The equivalent of **%SYSTEMROOT%** (usually "C:\Windows").

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

You can create your own hidden shares: [KB314984](http://support.microsoft.com/kb/314984){:.alert-link} ([webcite](http://www.webcitation.org/5yep2mpjH){:.alert-link}). You can also prevent the automatic creation of the standard hidden shares: [KB954422](http://support.microsoft.com/kb/954422){:.alert-link} ([webcite](http://www.webcitation.org/5yep4SjDH){:.alert-link}), but this may cause lots of problems: [KB842715](http://support.microsoft.com/kb/842715){:.alert-link} ([webcite](http://www.webcitation.org/5yepDm7Rl){:.alert-link}).
</div>

With all of that background information, our first step is to actually establish the authenticated connection to **\\computer\IPC$**. The other steps are quite simple in comparison!

## Step 2: Copy the Program to the Target

Just copy the program to **\\computer\ADMIN$**, right into the Windows directory. I recommend renaming the file during the copy to a unique name, to avoid conflicts. You don't need to explicitly establish a network connection to **\\computer\ADMIN$**; the existing connection to **\\computer\IPC$** will be your authentication.

## Step 3: Register and Execute the Program

This step makes use of the little-known fact that Win32 services may be _installed_ remotely. The [service configuration API](http://msdn.microsoft.com/en-us/library/ms685148(v=VS.85).aspx) can be used to install the service on the remote computer and then start it.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

The .NET [ServiceController class](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicecontroller.aspx){:.alert-link} does expose remote _control_ of services (starting, stopping, etc), but it does not expose remote _installation_ of services.
</div>

## Step 4: Securely Communicate

Once the service is running on the remote computer, it is simple matter to communicate with the original process and carry out its instructions. It's not quite as simple to do so in a secure manner, though; strongly consider _encrypting_ all network communication and using _impersonation_ in the service.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Also remember that - as a service - you [are limited]({ % post_url TODO % }){:.alert-link} in what you can do.
</div>

## Enjoy!

There aren't too many good use cases for this technique. Remote administration is one, as demonstrated by the PsTools suite from Microsoft TechNet Systems Internals.

Another possible application is to inject an installer for remote control software, such as VNC or pcAnywhere. This could be useful in the rare case where a computer is physically inaccessible.

