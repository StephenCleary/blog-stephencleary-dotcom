---
layout: post
title: "Simple and Easy Code Contracts"
---
[Code Contracts](http://research.microsoft.com/en-us/projects/contracts/) are a wonderful thing. Every new library I write uses CC right from the beginning.

Here's Steve's "simple and easy" guide to getting started with Code Contracts (assuming you're writing a new library):

## Project Setup

After you've downloaded and installed Code Contracts and created a library project, you need to enable CC for that library. Under the Project Properties, there is a new tab called Code Contracts. Here's the way that I like to set it up:

- Set "Assembly Mode" to "Standard Contract Requires".
- **Debug** - Check "Perform Runtime Contract Checking" and set to "Full". Check "Call-site Requires Checking".
[![]({{ site_url }}/assets/CC-debug.PNG)  
]({{ site_url }}/assets/CC-debug.PNG)

- **Release** - Set "Contract Reference Assembly" to "Build" and check "Emit contracts into XML doc file".
[![]({{ site_url }}/assets/CC-release.PNG)  
]({{ site_url }}/assets/CC-release.PNG)

In addition, if you have the Academic or Commercial Premium edition of Code Contracts, add a configuration called **CodeAnalysis** with all the settings from **Debug** and also check "Perform Static Contract Checking" and all the other checkboxes in that section except "Baseline".

[![]({{ site_url }}/assets/CC-analysis.PNG)  
]({{ site_url }}/assets/CC-analysis.PNG)

This will give you three separate builds, with separate behavior:

 - **CodeAnalysis** - This evaluates all the code contracts at build time, searching for any errors. This "build" doesn't result in a usable dll, but should be run before checking in code, similar to a unit test.
 - **Debug** - This turns on all code contracts at runtime. This includes code contract checks for any libraries that your library uses (such as CLR libraries). It also turns on consistency checks such as **Contract.Assert** and **ContractInvariantMethod**.
 - **Release** - This turns off all code contracts in your dll at runtime. However, it includes a separate ".Contracts.dll" which contains the contracts for your library's public API.

Projects _consuming_ your library should reference your **Release** build. In their **Debug** configuration, they should check "Perform Runtime Contract Checking" and "Call-site Requires Checking"; this will ensure that the code contracts for your library's public API are enforced at runtime (the "Call-site" option uses the ".Contracts.dll" assembly that you built). In their **Release** configuration, they should leave code contracts disabled, which allows all assemblies to run at full speed.

[![]({{ site_url }}/assets/CC-consumer-debug.PNG)  
]({{ site_url }}/assets/CC-consumer-debug.PNG)
[![]({{ site_url }}/assets/CC-consumer-release.PNG)  
]({{ site_url }}/assets/CC-consumer-release.PNG)

If the consuming project suspects a bug in your library (i.e., their **Debug** build doesn't cause any Contracts violations but your library is still not behaving as expected), they can remove the reference to your **Release** build and add a reference to your **Debug** build. This is an easy way to enable all the code contract checks in your library, even the internal ones.

## Preconditions (Contract.Requires)

Preconditions require some condition at the beginning of a method. Common examples are requiring a parameter to be non-null, or to require the object to be in a particular state.

{% highlight csharp %}

public string GetObjectInfo(object obj)
{
  Contract.Requires(obj != null);
  return obj.ToString();
}
{% endhighlight %}

## Postconditions (Contract.Ensures)

Postconditions guarantee some condition at the end of a method. It is often used with Contract.Result to guarantee that a particular method won't return null.

{% highlight csharp %}

private string name; // never null
public string Name
{
  get
  {
    Contract.Ensures(Contract.Result<string>() != null);
    return this.name;
  }
}
{% endhighlight %}

## Preconditions and Postconditions on Interfaces

Both preconditions and postconditions are commonly placed on interface members. Code Contracts includes the ContractClassAttribute and ContractClassForAttribute to facilitate this:

{% highlight csharp %}

[ContractClass(typeof(MyInterfaceContracts))]
public interface IMyInterface
{
  string GetObjectInfo(object obj);
  string Name { get; }
}

[ContractClassFor(typeof(IMyInterface))]
internal abstract class MyInterfaceContracts : IMyInterface
{
  public string GetObjectInfo(object obj)
  {
    Contract.Requires(obj != null);
    return null; // fake implementation
  }

  public string Name
  {
    get
    {
      Contract.Ensures(Contract.Result<string>() != null);
      return null; // fake implementation does not need to satisfy postcondition
    }
  }
}
{% endhighlight %}

With generic interfaces, the same idea holds:

{% highlight csharp %}

[ContractClass(typeof(MyInterfaceContracts<,>))]
public interface IMyInterface<T, U>
{
  string GetObjectInfo(object obj);
  string Name { get; }
}

[ContractClassFor(typeof(IMyInterface<,>))]
internal abstract class MyInterfaceContracts<T, U> : IMyInterface<T, U>
{
  public string GetObjectInfo(object obj)
  {
    Contract.Requires(obj != null);
    return null; // fake implementation
  }

  public string Name
  {
    get
    {
      Contract.Ensures(Contract.Result<string>() != null);
      return null; // fake implementation does not need to satisfy postcondition
    }
  }
}
{% endhighlight %}

## Invariants (ContractInvariantMethod, Contract.Invariant)

Object invariants are expressed using the ContractInvariantMethod. If they are enabled by the build, then they are checked at the beginning of each method (except constructors) and at the end of each method (except Dispose and the finalizer).

{% highlight csharp %}

public class MyClass<T, U>: public IMyInterface<T, U>
{
  private string name;

  public MyClass(string name)
  {
    Contract.Requires(name != null);
    this.name = name;
  }

  [ContractInvariantMethod]
  private void ObjectInvariant()
  {
    Contract.Invariant(this.name != null);
  }

  public string Name
  {
    get
    {
      Contract.Ensures(Contract.Result<string>() != null);
      return this.name;
    }
  }

  public string GetObjectInfo(object obj)
  {
    Contract.Requires(obj != null);
    return obj.ToString();
  }
}
{% endhighlight %}

## Assertions and Assumptions (Contract.Assert, Contract.Assume)

There will always be some things that should be true but just have to be checked at runtime. For these, use Contract.Assert unless the static checker (i.e., the CodeAnalysis configuration) complains. You can then change them to be Contract.Assume so that the static checker can use them. There's no difference between Contract.Assert and Contract.Assume at runtime.

Reminder: if you're using the **Release** build setup recommended above, then all your **Contract.Assert** and **Contract.Assume** calls get removed from your release builds. So they can't be used to throw vexing exceptions, e.g., rejecting invalid input.

In the example below, the static checker would complain because **Type.MakeGenericType** has preconditions that are difficult to prove. So we give it a little help by inserting some **Contract.Assume** calls, and the static checker is then pacified.

{% highlight csharp %}

public static IMyInterface<T, U> CreateUsingReflection()
{
  var openGenericReturnType = typeof(MyClass<,>);
  Contract.Assume(openGenericReturnType.IsGenericTypeDefinition);
  Contract.Assume(openGenericReturnType.GetGenericArguments().Length == 2);
  var constructedGenericReturnType = openGenericReturnType.MakeGenericType(typeof(T), typeof(U));
  return (IMyInterface<T, U>)Activator.CreateInstance(constructedGenericReturnType);
}
{% endhighlight %}

## For More Information

The Code Contracts library has a thorough [user manual](http://research.microsoft.com/en-us/projects/contracts/userdoc.pdf) available. It's a bit of a hard read, but they include a lot of information that I've skipped for this "intro" post, such as:

  - Specifying postconditions that hold even if the method throws an exception.
  - Techniques for gradually migrating Code Contracts into an existing library.
  - Details on how Code Contracts are inherited.
  - Contract abbreviations.
  - Applying contracts to sequences (e.g., **ForAll** and **Exists** quantifiers).
  - Advanced contract checking with **Pure** methods.
  - Tips for working with the static checker.

Another great source of information is Jon Skeet's updated [C# in Depth](http://www.amazon.com/gp/product/1935182471?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=1935182471) - the second edition added a whole chapter just on Code Contracts.

