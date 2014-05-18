---
layout: post
title: "Windows Services and the Network"
tags: [".NET", "Windows Services"]
---


Let's make this very clear: **a service should not use or change drive mappings _at all_**. See [KB180362 (INFO: Services and Redirected Drives)](http://support.microsoft.com/kb/180362) [(webcite)](http://www.webcitation.org/5wvCVoiMW) and [Services and Redirected Drives (MSDN)](http://msdn.microsoft.com/en-us/library/ms685143.aspx) [(webcite)](http://www.webcitation.org/5wvCgBLpy) for more information. If a service needs to use network resources, it should use UNC paths.





Network drive mappings are handled differently on different Windows versions. In addition, network drive mappings are one type of an "MS-DOS Device Name", so they fall under the additional complications described in [Local and Global MS-DOS Device Names](http://msdn.microsoft.com/en-us/library/ff554302.aspx) [(webcite)](http://www.webcitation.org/5wvCJn9TO).





Note that a service running as [LocalService](http://msdn.microsoft.com/en-us/library/ms684188.aspx) [(webcite)](http://www.webcitation.org/5wvCjwaeD) uses anonymous credentials to access network resources, and services running as [NetworkService](http://msdn.microsoft.com/en-us/library/ms684272.aspx) [(webcite)](http://www.webcitation.org/5wvCoNkZK) or [LocalSystem](http://msdn.microsoft.com/en-us/library/ms684190(VS.85).aspx) [(webcite)](http://www.webcitation.org/5wvCtIBSx) use machine account credentials.

