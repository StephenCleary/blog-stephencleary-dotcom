---
layout: post
title: "Review of Bill Wagner's Effective C# (2nd ed), Part 1"
---
I had always flat-out disregarded Effective C#. I was heavily into C++ (i.e., contributing to Boost) when Effective C++ came out, and it jumped onto the "must-read"  C++ shortlist. The only reason it worked, though, was because most of the smartest programmers had decades of experience with C++, so the "dark corners" of the language eventually became known. C#, in constrast, simply hasn't had that kind of adoption for so long. It is also still experiencing drastic changes (e.g., generic variance and dynamic typing in 4.0). My conclusion was that Effective C# cannot possibly be the same as "Effective C++ for C#".

Indeed, it is not. Martin Shoemaker sums it up well in his [Amazon review](http://www.amazon.com/review/R1Y1O9FX8NU3OS/ref=cm_cr_rdp_perm): "Bill Wagner failed to deliver [the same] enlightenment [as Scott Meyers]. But that's a good thing..." After reading his review, I decided to read the book on its own (not expecting it to be the same as Effective C++). Besides, Bill Wagner gave me a copy, which was really nice of him.

> Disclaimer: I've met Bill Wagner and heard him speak. He gave me a copy of the book for free (not even to review; just as a gift). He's also the Microsoft regional director for my region. So hopefully he won't get too mad when I point out the trouble spots along the way. :)

The book is split into 50 sections, each one addressing in detail a particular coding recommendation. In my review, I treat each section separately, with "plus" and "minus" signs for each of my responses. Since I consider myself an "intermediate" C# programmer (with Jon Skeet and Jeffrey Richter being the only known "expert" C# programmers), anything new I learned is listed as a plus.

## Item 1: Use Properties Instead of Accessible Data Members
+ This is excellent advice, and Bill goes into good detail explaining why, including examples of what happens if this is not followed.

- This advice is pretty old, though. Who doesn't already know this?

- Structures used for p/Invoke interop should be mentioned as an exception to this rule.

## Item 2: Prefer readonly to const
+ I had never really thought of this before, but he's right.

+ Includes a few examples where const is OK, and even required.

+ Also points out that default values for optional parameters may be implicitly const values.

## Item 3: Prefer the is or as Operators to Casts
+ Clearly explains the semantic difference between is/as and casting, particularly regarding user-defined conversions (which I didn't know previously).

- Correctly points out that as-casting does not work for value types but fails to point out the easy solution of as-casting to the corresponding nullable value type.

+ Delves a bit into how foreach uses casing (again, something new I learned).

## Item 4: Use Conditional Attributes Instead of #if
- The only examples given for #if usage are contract checking and tracing. The usage of #if for cross-platform compatibility (e.g., Silverlight / WPF) is ignored, though this is a perfectly valid use of #if where conditional attributes are not even a possible alternative.

- Conditional attributes are suggested instead of #if statements for contract checking, when the users should have been directed to the new Contracts support in .NET 4.

- Conditional attributes are suggested instead of #if statements for tracing, but the majority of applications would benefit from leaving tracing on in the field (controlled via app.config).

## Item 5: Always Provide ToString()
+ Excellent advice that is often overlooked.

+ Also includes helpful advice regarding IFormattable.

- Minor technical error: the book states that IFormattable should accept an empty string "format" parameter, but [MSDN disagrees](http://msdn.microsoft.com/en-us/library/system.iformattable.tostring.aspx).

