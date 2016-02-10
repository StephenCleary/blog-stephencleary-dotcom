---
layout: post
title: "A Few Words on GUIDs"
---
I keep seeing a lot of confusion in programmers as to how exactly GUIDs work, likeliness of collision, etc. I did some work [a while ago]({% post_url 2009-08-17-alternative-guids-for-mobile-devices %}) developing my own GUID, so I thought I'd post some clarifications about GUIDs.

## The Standard

First off, GUIDs do have a standard: [RFC 4122](http://www.apps.ietf.org/rfc/rfc4122.html). However, there are other types of GUIDs; these will be called "non-conforming GUIDs" in this blog post.

## High-Level Structure

A GUID is a 128-bit (16-byte) value that is normally divided into five groups of varying lengths: 4 bytes, 2 bytes, 2 bytes, 2 bytes, and 6 bytes. Certain bits have certain meanings.

RFC 4122 defines several different types of GUIDs, each of which may be composed of "fields." The first field of note is the Variant field, which determines the "type" of the GUID. The two most significant bits in the 8th octet may be used to get the variant: 0 and 1 are reserved for NCS backwards compatibility; 2 is an RFC 4122 GUID; 3 is reserved for Microsoft backwards compatibility and future expandibility. Almost all discussion of GUIDs on the Internet are dealing with Variant 2 (RFC 4122) GUIDs. [Note: The actual Variant field interpretation is more complex, but this description is close enough].

The second field of note is the Version field. This acts as a sort of "sub-type" for Variant 2 RFC 4122 GUIDs. The four most significant bits in the 7th octet determine the version: 1 is a time-based GUID; 2 is a DCE GUID (not described in the RFC); 3 is an MD5-hashed name-based GUID; 4 is a random GUID; 5 is a SHA1-hashed name-based GUID; and [0, 6-15] are undefined. Again, almost all discussion of GUIDs on the Internet are dealing with Version 1 or 4 GUIDs.

Name-based GUIDs (versions 3 and 5) are hardly ever used; they provide a means to hash a name in a given "namespace" to a GUID value consistently. This is a nice idea, but most programs just use the hash directly instead of truncating it into a GUID structure.

## Random GUIDs (Version 4)

Today, the most common type of GUIDs are Variant 2, Version 4 RFC 4122 GUIDs, also known as "random GUIDs". Aside from the Variant and Version fields, all other bits in the GUID are random. In particular, random GUIDs do _not_ expose a MAC address.

The .NET Framework [Guid.NewGuid](http://msdn.microsoft.com/en-us/library/system.guid.newguid.aspx) static method generates a random GUID. The "Create GUID" tool (guidgen.exe) included in Visual Studio and the Windows SDK also generates random GUIDs, as does the uuidgen.exe tool in the SDK.

## Likelihood of Collision

The Variant and Version fields are 6 bits together, which leaves 122 bits of randomness in a random GUID. There are 5.3e36 total unique random GUIDs, which is a lot. What is more important, however, is the _likelihood of collision_.

Assuming a perfect source of entropy on each device generating random GUIDs, there is a 50% chance of collision after 2.7e18 random GUIDs have been generated. That's more than 2.7 million million million. That's a lot.

Even if you reduce the chance of collision to 1%, it would still take about 3.27e17 random GUIDs for just a 1% chance of collision. That's more than 326 million billion. That's a lot.

Random GUIDs cannot ever collide with other types of RFC 4122 GUIDs (e.g., time-based GUIDs). This is because the Variant or Version fields would be set to different values. However, non-conforming GUIDs may collide with random GUIDs.

## Time-Based GUIDs (Version 1)

Time-based GUIDs are Variant 2, Version 1 RFC 4122 GUIDs, also known as "sequential GUIDs" because they can be generated with values very close to each other. They consist of three fields in addition to Variant and Version: a 60-bit UTC Timestamp, a 14-bit Clock Sequence, and a 48-bit Node Identifier.

The Node Identifier is normally the MAC address of the computer generating the time-based GUID (which is guaranteed to be unique, since MAC addresses use a registration system). However, it may also be a 47-bit random value (with the broadcast bit set). In this case, there is no danger of collision with real MAC addresses because the broadcast bit of a physical MAC address is always 0. There is a danger of collision with other random node identifiers, though; specifically, there is a 50% chance of collision once 13.97 million random nodes enter the network.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Note: using a random value instead of the MAC address is not currently supported by Microsoft's Win32 API. This means that any GUID generation done using [UuidCreateSequential](http://msdn.microsoft.com/en-us/library/aa379322(VS.85).aspx){:.alert-link} _will_ expose the MAC address.
</div>

The Clock Sequence field is initialized to a random value and incremented whenever the system clock has moved backward since the last generated GUID (e.g., if the computer corrects its time with a time server, or if it lost its date and thinks it's 1980). This allows 16,384 clock resets without any danger of a collision. If the GUIDs are being generated so quickly that the system clock has not moved _forward_ since the last GUID's timestamp, then the GUID generation algorithm will generally stall until the system clock increments the timestamp.

Sequential GUIDs are not actually _sequential_. In normal circumstances, GUIDs being generated by the same computer will have gradually increasing Timestamp fields (with the other fields remaining constant). However, the Timestamp field is not in the least-significant bit positions of the GUID, so if the GUID is treated as a 128-bit number, it does not actually _increment_.

It's important to note that the likelihood of collisions of sequential GUIDs is extremely small. The Clock Sequence and Timestamp almost certainly uniquely identify a point in time, and the Node Identifier almost certainly identifies a unique source.

Sequential GUIDs can be created by the Win32 function [UuidCreateSequential](http://msdn.microsoft.com/en-us/library/aa379322(VS.85).aspx) or by using uuidgen.exe from the Windows SDK passing the -x parameter.

## Microsoft's Change

The primary method for creating GUIDs on Windows is the [UuidCreate](http://msdn.microsoft.com/en-us/library/aa379205(VS.85).aspx) function. Before Windows 2000 (e.g., Windows NT and the 9x line), GUIDs created by this function were time-based (version 1) GUIDs. This was changed in Windows 2000 to return random (version 4) GUIDs due to privacy concerns regarding the exposure of the MAC address in Version 1 GUIDs.

Note that "the" GUID algorithm did not change. Microsoft simply changed which GUID algorithm they were using to implement that function. Both the old and new implementations are RFC 4122 compliant, and "old" GUIDs will not conflict with "new" GUIDs. "Old" (Version 1) GUIDs can still be created by calling [UuidCreateSequential](http://msdn.microsoft.com/en-us/library/aa379322(VS.85).aspx).

## The Database Problem(s)

Database indexes do not work well with random values; the on-disk search trees end up very wide because the indexes do not cluster well. So, when using GUIDs for keys, it helps to use a more... _sequential..._ solution.

However, there's another problem with GUIDs as database keys: the order in which the database compares GUIDs. Remember that sequential GUIDs aren't really _sequential_ because the Timestamp field is not at the end of the GUID structure. Furthermore, some databases compare GUID values in strange ways (I'm looking at you, [SQL Server](http://blogs.msdn.com/b/sqlprogrammability/archive/2006/11/06/how-are-guids-compared-in-sql-server-2005.aspx) ([webcite](http://www.webcitation.org/5ylIiAwyb))).

So, when Microsoft added [newsequentialid()](http://msdn.microsoft.com/en-us/library/ms189786.aspx) to SQL Server, they did not just return a regular sequential GUID. They [shuffled some of the bytes](http://www.jorriss.net/blog/jorriss/archive/2008/04/24/unraveling-the-mysteries-of-newsequentialid.aspx) ([webcite](http://www.webcitation.org/5ylItnhAb)) to make index clustering more efficient.

 

Unfortunately, the shuffled bytes include the Version field. This means that **newsequentialid() GUIDs are not RFC 4122 compliant!** As a result, GUIDs from newsequentialid() have a higher likelihood of colliding with RFC 4122 compliant GUIDs such as sequential or random GUIDs.

## Conclusion

When using GUIDs as keys in a database, you must ensure that the GUIDs are all compatible with each other. In particular, newsequentialid() is not compatible with Guid.NewGuid or UuidCreateSequential (unless you're doing byte swapping manually). Guid.NewGuid and UuidCreateSequential are compatible with each other (since they are both RFC 4122 compliant). Other made-up GUIDs - including ["comb" GUIDs](http://www.informit.com/articles/article.aspx?p=25862) ([webcite](http://www.webcitation.org/5ylJ1c1VK)) - are not compatible with any other type of GUID.

Mixing incompatible GUIDs may work for a while, but you do greatly increase your chances of collisions. If GUIDs are used as keys in a database, then you should choose one particular type of combed GUID (such as newsequentialid()) and use it exclusively. If the GUIDs are not used as keys in a database, just use random RFC 4122 GUIDs. Published GUIDs (e.g., COM identifiers) are usually assumed to be RFC 4122 compliant.

There are many statements on the Internet about observing GUID collisions in production. These statements almost always conclude that "GUIDs can collide", which should be taken with a healthy dose of skepticism. Collisions are most likely a result of using two incompatible GUID formats (e.g., an RFC 4122 GUID and a non-conforming GUID); however, they may also be caused by one of the devices using a poor source of entropy (for random GUIDs), or a device repeatedly having its clock reset (for sequential GUIDs).

Another possible problem is when well-meaning coders actually _increment_ an existing GUID instead of generating a new one. It is **wrong** to take any GUID and increment it. Period. Always has been and always will be.

## Code!

When doing the research for [my own combed GUID]({% post_url 2009-08-17-alternative-guids-for-mobile-devices %}), I wrote [a few extension methods](http://nitokitchensink.codeplex.com/SourceControl/changeset/view/57812#1006424) for the Guid structure that allow examining the RFC 4122 fields. For example, you can extract the MAC address and creation time for a sequential GUID. Naturally, these extension methods will only work for RFC 4122 GUIDs.

