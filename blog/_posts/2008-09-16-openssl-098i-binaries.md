---
layout: post
title: "OpenSSL 0.9.8i Binaries"
---
We built the OpenSSL binaries for Windows, and made them publicly available at [http://sourceforge.net/project/showfiles.php?group_id=26202&package_id=291670](http://sourceforge.net/project/showfiles.php?group_id=26202&package_id=291670)

Note that there are a few differences for our version, compared to other binary packages:

- All patent-encumbered algorithms have been removed (e.g., IDEA, RC5, etc.).
- No static libraries are built; these are all DLLs.
- Include directories and HTML documentation are packaged as well, but no import libraries.
- No executables are included (e.g., openssl.exe).
- The x86 DLL does not have any dependency on the Microsoft Visual C++ Runtime Redistributables.
- An x64 (AMD64) version of the DLLs are also included, though they do depend on the Microsoft Visual C++ Runtime Redistributables.

