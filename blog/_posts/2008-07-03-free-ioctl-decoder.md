---
layout: post
title: "Free IOCTL Decoder"
---
Unexpected IOCTL codes are something that every first-time driver writer must become accustomed to. Sometimes it can be difficult to find out the meaning of a particular IOCTL, since the driver sees it as just a number but the documentation requires a symbolic name and the header files use macros to define it. Often searching for just the number turns up no results, even on the WWW.

Because of this common problem, we at Nito Programs have developed a small command-line utility to help decode IOCTLs. Unlike other IOCTL decoders, this utility does not just break down the number into fields; it actually contains a database of known IOCTLs defined by Microsoft. Since this utility has this information available, it supports searching by symbolic name as well as numerical value. Of course, if the IOCTL is not in the database, then the utility will break it down into fields just like other IOCTL decoders.

Our IOCTL decoder also supports regular expression string matching, symbolic device name matching (useful for listing all IOCTLs for a particular device), and fuzzy matching (for those few IOCTLs that are defined with the wrong method/access type).

We have decided to release this useful utility for free to the driver development community. It can be downloaded from 
[SourceForge](http://sourceforge.net/project/showfiles.php?group_id=213700&package_id=279739)

Hope you find it useful!

