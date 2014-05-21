---
layout: post
title: "How to Implement IDisposable and Finalizers: Matrix and Flowchart"
---
I've decided to split off the matrix and flowchart from my original [How to Implement IDisposable and Finalizers: 3 Simple Rules]({% post_url 2009-08-27-how-to-implement-idisposable-and %}) post, and put them into their own post here.

## Summary: A Decision Matrix for IDisposable and Finalize

The flowchart and decision matrix below use these terms:

- Managed resource - an instance of any class implementing IDisposable.
- Unmanaged resource - a handle of some type representing a resource that requires a p/Invoke function to "free" the handle; OR an IntPtr allocated by one of the "allocation" functions in the [Marshal](http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.marshal.aspx) class; OR a [GCHandle](http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.gchandle.aspx) or its equivalent IntPtr.
- Own - a resource is "owned" by a class if the lifetime of the resource is scoped to an instance of that class. Some classes _share_ resources but do not own them.

### Decision Matrix as a Table

<div class="panel panel-default" markdown="1">
  <div class="panel-heading" markdown="1">Decision Matrix for IDisposable and Finalize</div>

{:.table .table-striped}
||Class does not own managed resources|Class owns at least one managed resource|
|-
|Class does not own unmanaged resources|Apply[Rule 1]({% post_url 2009-08-27-first-rule-of-implementing-idisposable %}): no IDisposable or Finalizer|Apply[Rule 2]({% post_url 2009-08-27-second-rule-of-implementing-idisposable %}): IDisposable but no Finalizer|
|Class owns one unmanaged resource|Apply[Rule 3]({% post_url 2009-08-27-third-rule-of-implementing-idisposable %}): both IDisposable and Finalizer|Refactor|
|Class owns more than one unmanaged resource|Refactor|Refactor|

</div>

### Decision Matrix as a Flowchart

Step 1 - Does the class own a managed resource? If not, go to Step 2.

Step 1.1 - Does the class own an unmanaged resource? If it does, refactor the class into two classes, so that any class owns either unmanaged or managed resources, but not both. Then apply this flowchart to each resulting class.

Step 1.2 - The class owns at least one managed resource and no unmanaged resources. Apply [Rule 2]({% post_url 2009-08-27-second-rule-of-implementing-idisposable %}) to the class, and you're done.

Step 2 - Does the class own an unmanaged resource? If not, then apply [Rule 1]({% post_url 2009-08-27-first-rule-of-implementing-idisposable %}) to the class, and you're done.

Step 2.1 - Does the class own more than one unmanaged resource? If it does, refactor the class so that any class only owns at most one unmanaged resource, and then apply this flowchart to each of the resulting classes.

Step 2.2 - The class owns exactly one unmanaged resource and no managed resources. Apply [Rule 3]({% post_url 2009-08-27-third-rule-of-implementing-idisposable %}) to the class.

