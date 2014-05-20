---
layout: post
title: "Declarative Programming Languages"
---
There's a lot of excitement about declarative languages, as opposed to imperative languages.

Any programmer who has been around for a while knows that there's _always been_ a lot of excitement about declarative languages. There's a tendency to overinflate their importance. Why, by using declarative constructs, we can program more efficiently (developer time gains)! Why, by using declarative constructs, we can have more intelligent interpreters (run-time gains)! Why, by using declarative constructs, we can have threadsafe programs (safety gains)!

And all of this is true, **but only to a point.** That point is at a very specific location: where the designers of the declarative language _stopped adding features._ So, declarative languages work great when the programmers stay within the box. However, no declarative language can do everything, and one of two things happens:

1. The declarative language eventually ceases to evolve; e.g., a standards body decides that it is complete.
1. The declarative language includes extension points (which are not written in the declarative language itself), so that others may add to the language; this results in a handful of experts feeding libraries to the masses.

Neither solution is maintainable in the long term.

> Note that I'm only addressing declarative _programming_ here; declarative languages are perfectly well-suited for _declaring_ things, such as file structure or GUI layouts. But why do we take a perfectly good declarative language and try to shove programs into it?

## Blast from the Past

I'm a relatively new programmer, entering the workforce in 1995. There was a big, new thing that came out around that time. It was called XML. XML was a declarative language, and if you believed all the hype about it, it could cure cancer.

XML was perfectly fine for what it was used for: structuring text data. Binary data was a bit more complex, but there were ways to make it work. Relational databases didn't fit perfectly into the XML world, but there were mappings that worked sufficiently. XML was even used to represent function calls (as data).

So, XML worked for data, and worked well. But then some genius decided to write an XML _programming language._ There was lot of talk about how XML declarative languages would be the future of all programming - seriously!

A lot of work went into designing various XML languages, only one of which has survived. It is called XSLT, and pretty much everyone hates it. Is it possible to program in XML? Yes. It is fun? No.

## A Word about Functional Languages

Functional languages are sometimes called declarative languages, but I disagree with this classification. Imperative languages and functional languages are both concerned with _how_ a program is supposed to run. Declarative languages attempt to make the semantic leap to only being concerned with _what_ a program is supposed to do.

When looked at from this perspective, functional languages are really the same as imperative languages; they are just inside-out from each other. Declarative languages are completely different.

## Partially-Declarative Languages

LINQ is an example of a declarative sub-language within an imperative language (C# or VB). LINQ, when used with the Queryable system, will actually build a complete expression tree. The LINQ provider can then use that higher-level view of the code to generate the most efficient implementation.

Since anyone is free to implement a LINQ provider, LINQ is an example of a declarative language with an extension point (the Queryable system). People have written providers for an amazing array of data sources.

The problem: implementing a LINQ provider is [hard!](http://blogs.msdn.com/mattwar/pages/linq-links.aspx) Getting one working is hard enough; making it general-purpose (i.e., intelligently handling all LINQ operations) is a nightmare; and creating one that is efficient is next to impossible. So, this leads us to the predictable conclusion: a handful of provider authors attempting to satisfy the demands of the masses. Furthermore, the vast majority of LINQ providers only do the minimum necessary to get it working; they are neither general-purpose nor efficient.

## Declarative Languages Aren't All Bad

If a programmer can stay within the existing boundaries of a declarative language, then they are very useful! I love LINQ and I love XAML data binding, both of which are declarative. I'm just trying to point out that "declarative languages" are not general-purpose solutions for all problems.

Microsoft made a genius decision with regards to LINQ in particular: they allow a programmer to "step out" of the declarative language when the language falls short. They did this by including LINQ to Objects, which is _functional_ and not _declarative,_ technically speaking. Every LINQ provider has its own limitations (that "point" where the implementors stopped adding features), and at that point one can use "AsEnumerable" to transfer from the declarative system to a functional one.

> Example: LINQ to Entities cannot select new object instances like this: "db.ServiceSet.Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString(), Selected = x.Id == serviceId });"  
> However, one can use LINQ to Entities to retrieve the entity set and then switch to LINQ to Objects to complete the transformation: "db.ServiceSet.AsEnumerable().Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString(), Selected = x.Id == serviceId });"

## Final Rant: Declarative Code Is Still Code

Remember a few years ago when XAML came out? I had a hard time keeping from laughing out loud at some of those demos.

First off, the central breakthrough is that we're now using _declarations_ to _declare_ the UI (instead of doing it with code, a la WinForms)... But it's kind of funny that before WinForms, the way to declare the UI was using an old thing called a dialog resource... and this dialog resource was _declared_ within an RC file, which was written in a _declarative language_. Going in circles is always amusing.

The _really_ funny part of a lot of these demos, though, is when they would try to code in XAML. After showing how _amazing_ it was to declare a UI in XML, they showed us how it could even support (limited) programming! Without fail, after cutting and pasting tons of XAML, they would show some fancy UI animation and proudly proclaim: _"with zero lines of code!"_

So... um... you just took a dozen lines of C# and replaced it with a couple hundred lines of XAML? And that's an improvement? I'm sorry to break it to you, fella, but **XAML is still code!**

Of course, XAML is good for declaring things like UIs or even animation sequences. But programmers trying to do real _programming_ in XAML quickly run into its limitations. Ever try to chain a converter? Or apply a filter on a collection? Just like LINQ, XAML can be extended, but it is surprisingly difficult.

MVVM advocates originally attempted to achieve the "no code-behind nirvana," but quickly ran into the limitations of the declarative language (XAML). A handful of brave souls attempted to fill the gaps - at least for their specific needs - but the general advice from the MVVM community has shifted to the more practical "minimal code-behind."

## Conclusion

The fundamental problem with every declarative language is that the programmer has to place themselves at the complete mercy of the language designer(s). It's simply unmaintainable as a permanent solution. I believe that every sane programmer will continue _programming_ in imperative languages and continue _declaring_ in declarative languages.

And there's nothing wrong with that, in spite of the declarative programming hype.

