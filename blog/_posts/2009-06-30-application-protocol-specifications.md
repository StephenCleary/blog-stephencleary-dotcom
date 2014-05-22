---
layout: post
title: "Application Protocol Specifications"
series: "TCP/IP .NET Sockets FAQ"
seriesTitle: "Application Protocol Specifications"
---
When designing an application protocol, one should publish an application protocol specification document. Having a clearly-defined specification helps prevent errors on both sides.

## Versioning

The application protocol specification document should include the protocol version number to which it applies. Protocols change over time as additional requirements are added.

There should also be a way for the protocol to perform some form of version negotiation. Usually, it is enough to have one side send a list of supported versions, and have the other side respond with the chosen version.

This is a bit of up-front work, but allows partial upgrades in the future without breaking backwards compatibility. When two separate vendors or teams are producing applications on different sides of the protocol, or if the protocol is an open specification, then version negotiation becomes much more important.

## Terminology

The most important words in a specification are "must" and "may". When used consistently, these terms convey specific meanings. "Must" is used when an implementation _absolutely_ must obey the specification. "May" is used when an implementation _optionally_ may obey the specification.

When possible, use long-established terminology. The key reference for this is [RFC 2119](http://www.ietf.org/rfc/rfc2119.txt), which unambiguously defines MUST, MAY, SHOULD, etc. However, other standards often come into play; e.g., the Unicode standard has unambiguous definitions for "character", "code point", and "encoding", which are important to distinguish when writing an unambiguous protocol specification. Any special terms should be identified in the document, along with a reference to the defining standard.

## Server and Client: First Contact

The first question that is often answered when writing a TCP/IP protocol is: who contacts whom? More specifically, one side must be chosen as the _server_ and the other side as the _client_. In some cases, the choice of client and server sides is obvious. For other applications, it really doesn't matter which side is chosen for which role. Very loosely coupled applications (following more of a peer-to-peer model) may even act as a client, server, or both (for the same protocol).

Note that _client_ and _server_ only have meaning when the connection is being established. Once the TCP/IP connection is established, it will allow either side to send data to the other side at any time.

Usually, it is the responsibility of the server side to accept any incoming connections at any time; and it is the responsibility of the client side to retry dropped connections after a timeout. This timeout may be specified in the application protocol document, or it may be left as an implementation detail.

## Choosing the Port

The application protocol document should include the port number used for that protocol. Choosing a port number should be done with care; one must consider reserved port ranges as well as ephemeral port ranges. Ephemeral port ranges must be considered because any random client socket may be given a port in that range, and a server would be unable to bind on its port if that port was already being used by a client socket.

The [Internet Assigned Numbers Authority](http://www.iana.org/assignments/port-numbers) has reserved ports 0-1023 for specific, well-known protocols. A port in this range should never be used unless it is registered with IANA.

IANA has also reserved ports 1024-49151 in a similar manner (requiring registration). However, most people ignore this, and treat the 1024-49151 port range as available except for their ephemeral port ranges.

Ephemeral port ranges are trickier, since [different operating systems use different ranges](http://en.wikipedia.org/wiki/Ephemeral_port). Windows systems use 1025 to 5000 by default, but the upper value [may be changed](http://technet.microsoft.com/en-us/library/bb878133.aspx) via the registry.

In short, private Windows protocols (used only within a certain network) may pick a port from the range 5001-65535, with preference given to higher port numbers (so that individual machines may increase their MaxUserPort registry setting). If Linux compatibility is necessary, the range becomes 5001-32767 and 61001-65535, again prefering higher port numbers.

Public (published) protocols should be registered with IANA and use the assigned port in the 1024-49151 range. As of this writing, both Windows' and Linux's ephemeral port ranges overlap with this reserved range, so some extra action may need to be taken to prevent any possibility of conflicts (i.e., Windows' ReservedPorts registry key; see [KB812873](http://support.microsoft.com/default.aspx/kb/812873) or [The Cable Guy, Dec 2005](http://technet.microsoft.com/en-us/library/bb878133.aspx)).

Note: It is highly recommended that the port be configurable by the end user or administrator. Currently, there are not many "well-behaved" programs when it comes to choosing ports, so it is greatly beneficial to give the network admin the ability to change the port.
