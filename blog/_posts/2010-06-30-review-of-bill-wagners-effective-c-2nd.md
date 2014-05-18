---
layout: post
title: "Review of Bill Wagner's Effective C# (2nd ed), Part 2"
tags: [".NET", "Books"]
---


Continuing my long and drawn-out [review of Effective C#](http://blog.stephencleary.com/2010/05/review-of-bill-wagners-effective-c-2nd.html), this post takes a look at items 6-10.



## Item 6: Understand the Relationships Among the Many Different Concepts of Equality


+ This is often a confusing topic for newcomers, and Bill explains it pretty well. He clearly distinguishes reference and value equality.




+ Correct recommendations on when and how to define equality for user-defined types.




+ Correctly discusses handling equality in the context of a type hierarchy. [Note: the class hierarchy example is only the simple case where objects of different types are always different. This does not handle the (uncommon) case where there is a sub-hierarchy where objects of different types can be equal.]




- Minor technical error: this section references the "IStructuralEquality" interface which had its name changed prior to the 4.0 release and is now called [IStructuralEquatable](http://msdn.microsoft.com/en-us/library/system.collections.istructuralequatable.aspx).




- The only mention of overriding GetHashCode is buried in the text and not even a comment is included in the examples for overriding Equals.



## Item 7: Understand the Pitfalls of GetHashCode()


- Repeatedly states that the result of GetHashCode must be equal if the two objects are equivalent as defined by operator==. This is incorrect; GetHashCode must be kept in sync with Object.Equals, not operator==.




+ Correctly explains efficiency problems with default GetHashCode implementations.




- Attempts to enforce more strict requirements on GetHashCode - specifically, that it can only be based on immutable fields. The actual requirements are only that the "key" field values do not change _while the object's hash is being used_.




- Incorrectly states that only immutable types can have a correct and efficient implementation of GetHashCode.




+ Pushes readers towards immutable value types. Even though GetHashCode _doesn't_ require them, they are easier to work with.



## Item 8: Prefer Query Syntax to Loops


- Assumes that query syntax is always cleaner than loops.




+ Points out the "composable API" benefit of query syntax.



## Item 9: Avoid Conversion Operators in Your APIs


+ I agree completely, and would include operator overloading in the same cautionary advice.



## Item 10: Use Optional Parameters to Minimize Method Overloads


+ Clearly explains all of the binary compatibility issues with optional parameters and default values.

