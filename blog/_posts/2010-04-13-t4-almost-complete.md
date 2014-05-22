---
layout: post
title: "T4: Almost Complete"
---
Visual Studio 2010 was released today, and one of the (many) improvements is that T4 gets better support. T4 is a promising addition to the VS family of languages.

Currently, .NET languages do not have built-in metaprogramming support (which C++ has had for years). They do have generics and dynamic dispatch, which do handle some of the metaprogramming use cases, but they also have their own limitations. Both of these solutions generate code at runtime.

There are two other metaprogramming solutions currently available. T4 is a solution that generates code before compiling. The other solution exists at the other end of the spectrum: IL rewriters such as [CciSharp](http://ccisamples.codeplex.com/wikipage?title=CciSharp) can be used to modify (and generate) code after compiling.

T4 is a good step in the metaprogramming evolution. It allows generating one (or more) classes from a given template. T4 templates allow you to use an ASP-like syntax to create C# code using C# code. [Side note: T4 can generate a lot more than C# code; it can generate any kind of text files].

This is excellent, but there is one little part left out: the T4 template must be executed for it to generate its classes. It would be really great if there was a way for a T4 template to declare it can create class names matching a certain pattern (and receive a "requested class name" as a parameter). Then the compiler (or a compiler wrapper such as an MSBuild task) could execute the template "on demand," as those classes are used by other code.

Of course, this is a non-trivial change to make to the build system. But if we ever get there, then C# will become a language with complete metaprogramming support. As it currently stands, there's a small hole remaining.

<div class="alert alert-info" markdown="1">

Example: I recently wrote CRC16 and CRC32 classes, but they're based off a generic CRC algorithm that is valid for any bit length. They are currently independent classes, but they can be fairly easily changed to a single T4 template that can generate CRC classes for different bit lengths.  
 
The problem: any code that needed a different bit length would have to [modify the template parameters](http://www.olegsych.com/2008/04/t4-template-design/){:.alert-link} in order to generate another CRC class, instead of letting the class name itself act as an implicit template usage.
</div>
