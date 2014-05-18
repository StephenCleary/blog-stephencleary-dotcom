---
layout: post
title: "Unit Testing Without Design Suicide"
tags: [".NET"]
---


One of the big problems when doing unit testing is that it's easy enough to test simple classes (without many dependencies), but testing more complex classes requires changes to the actual _design_ of the code.





Mocks and stubs are common approaches to substitute other types on which the class under test depends. A number of frameworks have sprung up to make mocking and stubbing easier (I like [Moq](http://code.google.com/p/moq/)). However, every mock or stub has another problem: how does one force the class under test to use the mock/stub instead of the real implementation?





There are a few common solutions:



1. Define an interface for each dependency, and pass references to the interfaces into the constructor for the class.
1. Define an interface for each dependency, and add a property to the class for each interface with a public setter.
1. Make every class unsealed and virtual, moving the dependency code to one of many protected virtual methods, and then create a new derived type that is used for testing, overriding the virtual methods representing dependent code.




None of these approaches are suitable for all situations. They become particularly problematic when the type under test depends on _static_ properties or methods.





I had a choice two weeks ago when writing unit tests for a rollover logger. It depended on DateTime.Now as well as a few static methods from the File and Directory classes. Should I create an interface for getting the current date and time (which is unlikely to change)? An interface for the file system (also unlikely to change)? Should I make the class unsealed and all methods virtual (opening up a second API - the protected API - that would have required _much_ more work in terms of API definition and documentation)?





Some unit testing advocates say those are good ideas. I say it's design suicide.





I ended up just writing integration tests; I didn't want to overcomplicate my design _just_ for the sake of some unit tests.



## A Better Solution



Just this morning I was reading a PDC-related [blog post](http://blogs.microsoft.co.il/blogs/sasha/archive/2009/11/18/pdc-2009-day-1-code-contracts-and-pex-power-charge-your-assertions-and-unit-tests.aspx) (man, I wish I could go some year...), and Sasha mentioned the existence of [Moles/Stubs](http://research.microsoft.com/en-us/projects/stubs/).





The whole idea behind the Moles/Stubs framework is to inject replacement implementation code for _any_ public property or method of _any_ type. This includes _static_ properties and methods. This also includes methods and properties of _sealed_ types.





Now that's sweet.





I haven't had a chance to play with it much, but it apparently uses profiling hooks to forward any types defined in an XML file. So, you could stub out mscorlib.dll by adding mscorlib.stubx. The Moles framework then creates a substitute types for mscorlib.dll, which have _delegate properties_ that you can set to override the properties/methods of the original class.





If we wanted to override the getter for System.DateTime.Now, then we would set a property on System.Stubs.MDateTime. Here's the DateTime.Now example code from the Moles/Stubs site:




// let's detour DateTime.Now
MDateTime.NowGet = () => new DateTime(2000,1,1);

if (DateTime.Now == new DateTime(2000, 1, 1))
    throw new Y2KBugException(); // take cover!




By setting the MDateTime.NowGet property, you're able to specify the behavior of DateTime.Now.





I don't often get excited, but this is one of the exceptions. There are some limitations to the Mole framework: it's not an official/production level release, and the replaced properties/methods "must match one of the predefined set of code signatures" that they support. However, even with these limitations, I think it's something I'll be using quite a lot of!





Because it allows me to do unit testing without design suicide.

