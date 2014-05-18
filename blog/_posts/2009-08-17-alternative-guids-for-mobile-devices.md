---
layout: post
title: "Alternative GUIDs for mobile devices using SQL Server Compact"
tags: [".NET"]
---


This is a bit of an obscure topic, but something I had to work on recently. A company I work for does a lot of mobile (compact framework) projects, and all of them use SQL Server CE to store collected data in a database. This data is later synchronized up to a central machine. The synchronization (and other data access) right now is very slow on the devices, since they are using the same code for data access as the desktop applications. As part of a re-thinking of the data layer, I came up with a new type of GUID.



## Anatomy of a Normal GUID and SqlGuid


- GUIDs are 128 bits long (16 bytes), normally grouped into 4 bytes + 2 bytes + 2 bytes + 2 bytes + 6 bytes because of its [RFC4122](http://www.faqs.org/rfcs/rfc4122.html) definition (time-low group, time-mid group, time-high-and-version group, clock-seq-high-and-reserved + clock-seq-low group, and node group).
- GUIDs are compared by SQL server as byte groups right-to-left, then each byte left-to-right within the group. (See [http://blogs.msdn.com/sqlprogrammability/archive/2006/11/06/how-are-guids-compared-in-sql-server-2005.aspx](http://blogs.msdn.com/sqlprogrammability/archive/2006/11/06/how-are-guids-compared-in-sql-server-2005.aspx)).
- The performance benefits of a custom GUID generation based on time (even with most bits left random) are well known. (See [http://www.informit.com/articles/article.aspx?p=25862&seqNum=7](http://www.informit.com/articles/article.aspx?p=25862&seqNum=7)).
- SQL server's newsequentialid() function actually does not return a standards-conforming (RFC 4122) GUID because it reverses the bytes in the first group. (See [http://www.jorriss.net/blog/jorriss/archive/2008/04/24/unraveling-the-mysteries-of-newsequentialid.aspx](http://www.jorriss.net/blog/jorriss/archive/2008/04/24/unraveling-the-mysteries-of-newsequentialid.aspx)).




The problem is that SQL Server Compact Edition does not support newsequentialid(), so I experimented with finding a replacement. UuidCreateSequential is not supported on CE/WM (though there have been some reports of people getting it working), so I decided to write my own GUID generation algorithm.



## Design Constraints


 - The system clock should be considered particularly unreliable; handheld devices often reset their time to a "zero point" after a hard reset.
 - The presence of a MAC address can be assumed. All devices have the ability of network connectivity, though not always connected.
 - However, the MAC address should not directly be placed in the result, due to security concerns.
 - Efficiency of GUID generation is not a high concern; the goal is to produce sequential GUIDs that work well with SQL server indexing.
 - Be RFC 4122 conforming if possible.


## Considering RFC 4122 Conformity



The version 1 and 4 GUIDs are quite well known. Version 1 is unacceptable because of the exposure of the MAC address, although a 47-bit random number can be substituted. Version 4 does not have a sequential variant because it is fully random. Versions 3 and 5 are name-based GUIDs, but they use one-way hashing, so they are also not sequential.





It is possible to use a RFC 4122 Version 1 GUID if the MAC address is replaced with a 47-bit random "node" number. However, the SQL Server ordering does not work nicely with the byte ordering of the timestamp in a Version 1 GUID; this is why the newsequentialid() function reverses the bytes in the first group. Technically, this makes it non-RFC 4122-conforming, but if we're going to set RFC 4122 aside, could we make a better GUID? All bits in an RFC 4122 GUID are either used or reserved, so we're going to start from scratch with a completely incompatible GUID structure.



## An Alternative GUID


  - The two most-significant bits are used for the version:
   - Version 0 is a MAC-based sequential GUID.
   - Version 1 is a random-based sequential GUID.
   - Versions 2-3 are reserved.
   - We can generate a (system-wide) node value by hashing the lowest MAC address, or a 46-bit random value if the MAC address is unavailable. This should be changed whenever the MAC address changes.
    - This creates a natural grouping "by source node" in SQL server.
    - The clock sequence is similar to RFC 4122, only it is 20 bits instead of 14 bits, due to the higher possibility of clock resets in handheld devices.
     - The clock sequence is incremented when the current timestamp is less than the last timestamp.
     - The clock sequence is initialized to a random value when the node value changes (or is first calculated).
     - This creates a subgrouping "by run" in SQL server.
     - Timestamp is a 60-bit value identical to RFC 4122. The timestamp must be stored in a system-wide location.
      - This creates a subgrouping "by time" in SQL server.
      - All fields are stored in little-endian instead of big-endian, to maximize the efficiency of SQL server's comparision algorithm.




Non-volatile storage needs:
       - Lowest MAC address on the device, and its hash; or, the random value used in place of the hash.
       - Last clock sequence value.
       - Last timestamp generated.






Implementation notes:
        - RNGCryptoServiceProvider may be used for random number generation.
        - Guid or SqlGuid may be used to hold the result.
        - TimeSpan.Ticks may be used for timestamp calculations.
        - There is no support for reading the MAC address; we'd have to p/Invoke iphlpapi.dll|GetAdaptersInfo.
        - We would need a platform-specific method for non-volatile storage. 
        - There is no support for named mutexes. We can p/Invoke CreateMutex from coredll.dll (using them as a WaitHandle instead of an unmanaged wait).




## Final Words



This is just a rough draft of a replacement GUID design. These GUIDs are designed specifically for use as primary keys in a SQL Server database; they are not guaranteed to be unique when compared with normal GUIDs.





Another application I'm working on is using the excellent ESENT embedded database built into Windows 2000 and higher (it's a portable application, so SQL Server CE isn't usable). ESENT has its own GUID ordering which is different than SQL Server, so I'll probably be using this GUID design with a different byte ordering on that platform.

