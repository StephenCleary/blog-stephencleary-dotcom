---
layout: post
title: "Getting the Local IP Address"
---
(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

One common FAQ is how to get the local IP address of the computer. In fact, the very question is wrong: a computer may easily have multiple IP addresses. In fact, a computer may have multiple network adapters, each of which has multiple addresses. A single network card may have multiple IP addresses as long as they are on separate logical networks; this is called "multihoning" and is sometimes done for security reasons. Of course, a computer may have multiple network cards as well, especially when one considers virtual networks.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

True (but boring) story: my laptop (on which I am writing this) currently has seven network "adapters": one physical, one wireless, one dialup, two VPN, and two for virtual machine networks. This is not including the Teredo virtual adapter, and others may also install the loopback adapter, which is commonly seen on testing machines.
</div>

The moral of this (short) FAQ entry? Never, ever assume that a computer only has one IP address. A lot of sample code for retrieving the local IP address does make this faulty assumption. However, the code [here]({% post_url 2009-05-18-getting-local-ip-addresses %}) displays a _list_ of IP addresses.

(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

