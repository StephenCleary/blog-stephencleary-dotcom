---
layout: post
title: "I/O Limitation in Windows"
---
Earlier today I was stress-testing a SerialPort component for [Nito.Async](http://nitoasync.codeplex.com/) when I ran into an unusual error: ERROR_NO_SYSTEM_RESOURCES (1450).

This error can be caused by exhausting any of several OS resources, though all the examples I've found deal with exhausing memory-related resources. In my particular example, I was trying to shove a 600 MB file across a serial port all at once.

There's a limit to how big of a user-mode buffer one can send to a device driver (so this comes into play if you're talking to a _device_, such as a serial port or named pipe; it also affects I/O to regular files if FILE_FLAG_NO_BUFFERING was used). According to Dan Moseley of Microsoft, the basis of this limitation is in how the I/O Manager creates its memory descriptor list (MDL).

I'm in a position where I will need to transfer large amounts of data over serial ports, so I wanted to know how much data can be transferred in a single call. [Dan Moseley's original description](http://msdn.microsoft.com/en-us/library/aa365747(VS.85).aspx) updated with the [IoAllocateMdl MSDN docs](http://msdn.microsoft.com/en-us/library/aa490866.aspx), along with the page size information from the [latest revision of Windows Internals](http://www.amazon.com/gp/product/0735625301?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735625301) was enough information to calculate the answer, which I've summarized below.

<div class="panel panel-default" markdown="1">
  <div class="panel-heading">Maximum I/O Buffer Size for Individual Unbuffered Read/Write Operations</div>

{:.table .table-striped}
|Operating System|Architecture|Page Size|Calculation|Maximum I/O Buffer Size|
|-
|2K/XP/2K3|x86|4096|PAGE_SIZE * (65535 - sizeof(MDL)) / sizeof(ULONG_PTR)|67076096 bytes (63.97 MB)|
|XP/2K3|x64|4096|PAGE_SIZE * (65535 - sizeof(MDL)) / sizeof(ULONG_PTR)|33525760 bytes (31.97 MB)|
|2K/XP/2K3|IA-64|8192|PAGE_SIZE * (65535 - sizeof(MDL)) / sizeof(ULONG_PTR)|67051520 bytes (63.95 MB)|
|Vista/2K8|x86 & x64|4096|(2 GB - PAGE_SIZE)|2147479552 bytes (1.999996 GB)|
|Vista/2K8|IA-64|8192|(2 GB - PAGE_SIZE)|2147479552 bytes (1.999992 GB)|
|Win7/2K8R2|x86 & x64|4096|(4 GB - PAGE_SIZE)|4294963200 bytes (3.999996 GB)|
|Win7/2K8R2|IA-64|8192|(4 GB - PAGE_SIZE)|4294959104 bytes (3.999992 GB)|

</div>

The lowest entry here is for XP/2K3 running on x64. So, if 64-bit XP is important, then you should not use I/O buffers over ~31 MB. If you ignore 64-bit XP, then you can use I/O buffers up to ~63 MB. Newer operating systems take great strides towards removing this limitation completely.

Note that this table only applies to the buffer passed to a _single_ API call. There are other I/O-related restrictions; in particular, I cannot simply split up my 600 MB file into 16 MB chunks and still send them all at once; the serial port will not be able to keep up with the requests and will eventually run into another limitation (with the same error code, ERROR_NO_SYSTEM_RESOURCES (1450)). The solution is to implement buffering in the application.

