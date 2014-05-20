---
layout: post
title: "Interop documentation pointers"
---
To become really proficient at good interop code, one must master a range of skills. The MSDN documentation is rather spread out regarding this, so here's an attempt to bring it together, as a "C++ .NET interop quick reference":

- [Development Tools and Languages :: Visual Studio :: .NET Framework Programming in Visual Studio :: .NET Framework Advanced Development :: Interoperability :: Interoperating with Unmanaged Code :: Interop Marshaling :: Marshaling Data with Platform Invoke](http://msdn.microsoft.com/en-us/library/fzhhdwae.aspx) - Gives a good overview of how to marshal the actual data to and from unmanaged code. Particularly useful when dealing with arrays and strings.
- [Development Tools and Languages :: Visual Studio :: Visual C++ :: .NET Programming Guide :: Interoperability with Other .NET Languages](http://msdn.microsoft.com/en-us/library/s1kw2y09.aspx) - Details how to get C#-like behavior in C++.
- [Development Tools and Languages :: Visual Studio :: Visual C++ :: Reference :: C/C++ Languages :: C++ Language Reference :: Language Features for Targeting the CLR](http://msdn.microsoft.com/en-us/library/xey702bw.aspx) - Reference information for cli::pin_ptr, cli::array, managed enums, etc.
- [.NET Development :: .NET Framework SDK :: .NET Framework :: .NET Framework Class Library :: System.Runtime.InteropServices Namespace :: Marshal Class](http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.marshal.aspx) - A BCL type that defines some very useful functions such as PtrToString* and StringTo*.

