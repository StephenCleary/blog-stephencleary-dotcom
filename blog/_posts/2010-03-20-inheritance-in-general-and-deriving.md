---
layout: post
title: "Inheritance in General, and Deriving from HashAlgorithm in Particular"
---
In the early days of OOP, the general consensus was that inheritance would become the key to software reusability. After experience showed that multiple implementation inheritance was too easily misused (e.g., C++), modern OOP languages adjusted to only allow multiple interface inheritance (e.g., C#).

Furthermore, in recent years the true difficulty of designing for inheritance has become known. The main problem is that when designing a base class, instead of one API there are two: the public API and the protected API. If an "API" was just a set of methods, then this would not be too much; however, one must consider invariants (for each API) as well as how the invariants behave when both APIs are used simultaneously.

This quickly becomes complex for all but the simplest classes, which has led to the modern design guideline "prefer composition over inheritance." This guideline seeks to simplify the inheritance situation by only using one API. Such renowned C# gurus as Jon Skeet [have stated](http://stackoverflow.com/questions/252257/why-arent-classes-sealed-by-default) that classes should be sealed (non-inheritable) by default, and we may yet see the next OOP language following that advice.

Most classes are not designed for inheritance. Even for classes that _are_ designed for inheritance, a common problem surfaces: only the public API is sufficiently documented. Since this is the API used by the vast majority of developers, the protected API is too often neglected.

I ran into an example of this today, when writing up a [general CRC-32 implementation](http://nitokitchensink.codeplex.com/SourceControl/changeset/view/48149#1012328). Naturally, I wanted to derive from [HashAlgorithm](http://msdn.microsoft.com/en-us/library/system.security.cryptography.hashalgorithm.aspx?WT.mc_id=DT-MVP-5000058), but the MSDN documentation is completely lacking. After surfing around a few other implementations, I kept seeing a lot of the same mistakes.

Plunging into Reflector, I dissected HashAlgorithm once and for all, and here's what _should_ be on MSDN under "Notes to Inheritors":

- You should invoke Initialize in your constructor. The "all-in-one" methods like ComputeHash do not start by calling Initialize (but do call it at the end).
- You should set HashSizeValue in your constructor to set the return value for HashSize. Overriding HashSize is unnecessary.
- The State value is set to nonzero after TransformBlock and reset to 0 after TransformFinalBlock. This enables derived classes to restrict their set of legal operations when in the middle of calculating a hash.

After performing this exercise, I pondered previous similar encounters. To my surprise, I cannot recall one time when I needed to derive from a class and was actually able to do it supported only by the documentation. Every time I've had to implement a derived type, I've _always_ had to peek into the implementation of the base because the protected API was insufficiently documented.

Hence the guideline, "prefer composition over inheritance." I go one step futher: every class I write is sealed unless it truly needs to be a base class (and even then, usually every base class is abstract and the non-abstract classes are sealed). This is a design guideline that I've followed since my C++ days, and it serves me equally well in C#.

