---
layout: post
title: "How to Implement IDisposable and Finalizers: 3 Easy Rules"
tags: [".NET", "IDisposable/Finalizers"]
---


Microsoft's documentation on IDisposable is needlessly confusing. It really boils down to three simple rules.



## Rule 1: Don't do it (unless you need to).



There are only two situations when IDisposable does need to be implemented:



- The class owns unmanaged resources.
- The class owns managed (IDisposable) resources.




See [The First Rule of Implementing IDisposable and Finalizers](http://blog.stephencleary.com/2009/08/first-rule-of-implementing-idisposable.html) for more details.



## Rule 2: For a class owning managed resources, implement IDisposable (but not a finalizer)



This implementation of IDisposable should only call Dispose for each owned resource. It should not have any other code: no "if" statements, no setting anything to null; just calls to Dispose or Close.





The class should not have a finalizer.





See [The Second Rule of Implementing IDisposable and Finalizers](http://blog.stephencleary.com/2009/08/second-rule-of-implementing-idisposable.html) for more details.



## Rule 3: For a class owning a single unmanaged resource, implement both IDisposable and a finalizer



A class that owns a single unmanaged resource should not be responsible for anything else. It should _only_ be responsible for _closing_ that resource.





No class should be responsible for multiple unmanaged resources.





No class should be responsible for both managed and unmanaged resources.





This implementation of IDisposable should call an internal "CloseHandle" method and then end with a call to [GC.SuppressFinalize(this)](http://msdn.microsoft.com/en-us/library/system.gc.suppressfinalize.aspx).





The internal "CloseHandle" method should close the handle if it is a valid value, and then set the handle to an invalid value. This makes "CloseHandle" (and therefore Dispose) safe to call multiple times.





The finalizer for the class should just call "CloseHandle".





See [The Third Rule of Implementing IDisposable and Finalizers](http://blog.stephencleary.com/2009/08/third-rule-of-implementing-idisposable.html) for more details.

