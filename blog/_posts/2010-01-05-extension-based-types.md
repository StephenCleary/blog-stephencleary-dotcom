---
layout: post
title: "Extension-Based Types"
---
There's a new paradign rising in .NET: _extension-based types (EBTs)_.

## Introduction

It's an exciting time to be a .NET developer. I'm reminded of the time when Boost was young, and programmers were first starting to realize the capabilities of templates. Generic programming became a powerful mainstream paradign, producing techniques now described in [C++ Templates](http://www.amazon.com/gp/product/0201734842?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0201734842) and [C++ Template Metaprogramming](http://www.amazon.com/gp/product/0321227255?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0321227255).

Currently, we're seeing a similar transformation taking place with C# generics. The adoption of generics is just beginning to reach a critical mass, where new and inventive approches are discovered. The thrust towards generic programming is driven by a desire for higher levels of _abstraction_ and _extensibility_.

The EBT approach allows writing .NET libraries that permit an unprecedented level of end-user extensibility while discouraging leaky abstractions. This approach combines the clean abstraction of "programming to an interface" with the convenience of additional, more complex operations (some of which may be contributed by end-user code).

## Extension-Based Types

An EBT is a type that is primarily defined through extension methods on interfaces. Only a few core methods are defined on the interfaces themselves. Properly-designed EBTs have the minimal set of methods defined in their interface (often only one, and sometimes none).

EBTs are useful for end-user extensibility (and raising the level of abstraction), but they also have a few "gotchas" since they are dependent on the compile-time nature of method overload resolution.

Here's an example of an EBT that defines a single method, MethodA:

    public interface IMyInterface { }
    public sealed class MyImplementation : IMyInterface { }
    
    public static class MyMethods
    {
        public static void MethodA(this IMyInterface @this) { Console.WriteLine("IMyInterface.MethodA()"); }
    }
    
    class Program
    {
        static void Main()
        {
            var obj = new MyImplementation();
            obj.MethodA(); // Prints: "IMyInterface.MethodA()"
    
            Console.ReadKey();
        }
    }

## Simple Inheritance

EBT inheritance is performed using interface inheritance. Here's an example of a base interface that defines MethodA and a derived interface defining MethodB; the derived interface ends up supporting both methods:

    public interface IBase { }
    public interface IDerived : IBase { }
    public sealed class Derived : IDerived { }
    
    public static class MyMethods
    {
        public static void MethodA(this IBase @this) { Console.WriteLine("IBase.MethodA()"); }
        public static void MethodB(this IDerived @this) { Console.WriteLine("IDerived.MethodB()"); }
    }
    
    class Program
    {
        static void Main()
        {
            var obj = new Derived();
            obj.MethodA(); // Prints: "IBase.MethodA()"
            obj.MethodB(); // Prints: "IDerived.MethodB()"
    
            Console.ReadKey();
        }
    }

## Overriding Inherited Methods: Simple Overriding

A derived EBT may override a base EBT method by defining its own method with an identical signature. Here's a derived type that overrides the MethodA defined by its base type:

    public interface IBase { }
    public sealed class Base : IBase { }
    public interface IDerived : IBase { }
    public sealed class Derived : IDerived { }
    
    public static class MyMethods
    {
        public static void MethodA(this IBase @this) { Console.WriteLine("IBase.MethodA()"); }
        public static void MethodA(this IDerived @this) { Console.WriteLine("IDerived.MethodA()"); }
    }
    
    class Program
    {
        static void Main()
        {
            var obj1 = new Base();
            obj1.MethodA(); // Prints: "IBase.MethodA()"
    
            var obj2 = new Derived();
            obj2.MethodA(); // Prints: "IDerived.MethodA()"
    
            Console.ReadKey();
        }
    }

## Overriding Inherited Methods: Invoking the Base Method

In order to invoke the base method when the derived EBT overrides it, the compile-time type of the variable must explicitly be the base type. Here's an example that invokes the derived and base MethodA implementations on the same object:

    public interface IBase { }
    public interface IDerived : IBase { }
    public sealed class Derived : IDerived { }
    
    public static class MyMethods
    {
        public static void MethodA(this IBase @this) { Console.WriteLine("IBase.MethodA()"); }
        public static void MethodA(this IDerived @this) { Console.WriteLine("IDerived.MethodA()"); }
    }
    
    class Program
    {
        static void Main()
        {
            var d = new Derived();
            d.MethodA(); // Prints: "IDerived.MethodA()"
    
            IBase b = d;
            b.MethodA(); // Prints: "IBase.MethodA()"
    
            Console.ReadKey();
        }
    }

For convenience, an "identity transformation method" is usually provided that restricts the type of a subexpression; this way, a separate variable is not necessary. By convention, the identity transformation method is named "As{I}". The following example shows how an "AsBase" method removes the need for the IBase variable:

    public interface IBase { }
    public interface IDerived : IBase { }
    public sealed class Derived : IDerived { }
    
    public static class MyMethods
    {
        public static IBase AsBase(this IBase @this) { return @this; }
        public static void MethodA(this IBase @this) { Console.WriteLine("IBase.MethodA()"); }
        public static void MethodA(this IDerived @this) { Console.WriteLine("IDerived.MethodA()"); }
    }
    
    class Program
    {
        static void Main()
        {
            var obj = new Derived().AsBase();
            obj.MethodA(); // Prints: "IBase.MethodA()"
    
            Console.ReadKey();
        }
    }

## Overriding Inherited Methods: The Importance of Compile-Time Types

It's important to note that the compile-time type of the expression is what's used for method overloading, so the EBT style of overriding inherited methods is _not_ like object-oriented virtual function overriding:

    public interface IBase { }
    public interface IDerived : IBase { }
    public sealed class Derived : IDerived { }
    
    public static class MyMethods
    {
        public static IBase AsBase(this IBase @this) { return @this; }
        public static void MethodA(this IBase @this) { Console.WriteLine("IBase.MethodA()"); }
        public static void MethodA(this IDerived @this) { Console.WriteLine("IDerived.MethodA()"); }
    }
    
    class Program
    {
        static void Main()
        {
            IBase obj = new Derived();
            obj.MethodA(); // Prints: "IBase.MethodA()", NOT "IDervied.MethodA()"
    
            Console.ReadKey();
        }
    }

In fact, even something as minor as missing a "using" statement could cause the wrong method to be called. Consider the case where "MethodA(this IDerived)" is defined in a class in a different namespace. It must be brought into scope via a "using" statement before it could be considered by method resolution.

## Multiple Inheritance

Multiple inheritance is supported for EBTs; any ambiguity causes a compiler error:

    public interface IBaseA { }
    public interface IBaseB { }
    public interface IDerived : IBaseA, IBaseB { }
    public sealed class Derived : IDerived { }
    
    public static class MyMethods
    {
        public static void MethodA(this IBaseA @this) { Console.WriteLine("IBaseA.MethodA()"); }
        public static void MethodA(this IBaseB @this) { Console.WriteLine("IBaseB.MethodA()"); }
    }
    
    class Program
    {
        static void Main()
        {
            var obj = new Derived();
            obj.MethodA(); // Compiler error: ambiguous
    
            Console.ReadKey();
        }
    }

Ambiguity may be resolved by overriding the method in the derived EBT, or by constraining the compile-time type using the identity transformation method. The second approach is more flexible, since it allows any user-defined extensions. This example uses the second approach:

    public interface IBaseA { }
    public interface IBaseB { }
    public interface IDerived : IBaseA, IBaseB { }
    public sealed class Derived : IDerived { }
    
    public static class MyMethods
    {
        public static IBaseA AsBaseA(this IBaseA @this) { return @this; }
        public static void MethodA(this IBaseA @this) { Console.WriteLine("IBaseA.MethodA()"); }
        public static IBaseB AsBaseB(this IBaseB @this) { return @this; }
        public static void MethodA(this IBaseB @this) { Console.WriteLine("IBaseB.MethodA()"); }
    }
    
    class Program
    {
        static void Main()
        {
            var obj = new Derived();
            obj.AsBaseA().MethodA(); // Prints: "IBaseA.MethodA()"
            obj.AsBaseB().MethodA(); // Prints: "IBaseB.MethodA()"
    
            Console.ReadKey();
        }
    }

## Properties

Due to .NET limitations, properties may only be defined on interfaces (there's no such thing as an "extension property"). However, they may be simulated:

    public interface IBase { int Property { get; } }
    public interface IDerived : IBase { }
    public sealed class Derived : IDerived
    {
        int IBase.Property
        {
            get { return this.GetProperty(); }
        }
    }
    
    public static class MyMethods
    {
        public static IBase AsBase(this IBase @this) { return @this; }
        public static int GetProperty(this IDerived @this) { Console.WriteLine("IDerived.GetProperty()"); return 13; }
    }
    
    class Program
    {
        static void Main()
        {
            var obj = new Derived();
            obj.GetProperty(); // Prints: "IDerived.GetProperty()"
            int test = obj.AsBase().Property; // Prints: "IDerived.GetProperty()"
    
            Console.ReadKey();
        }
    }

One may think of this as the interface holding the property _declaration_ while the extension methods (derived EBT methods) hold the property _definition_.

Note that the property getter will always call the same derived method, regardless of whether it is accessed through an IBase interface. However, the extension method "GetProperty" (if it were defined on IBase) would call either the IBase or IDerived implementation, depending on the compile-time type.

## Limitations: Inability to Override Interface Methods

If a method is defined in the interface instead of as an extension method, then that method may never be overridden by a derived EBT type:

    public interface IBase { }
    public interface IDerived : IBase { void MethodA(); }
    public sealed class Derived : IDerived
    {
        public void MethodA() { Console.WriteLine("Derived.MethodA()"); }
    }
    
    public static class MyMethods
    {
        public static IBase AsBase(this IBase @this) { return @this; }
        public static void MethodA(this IBase @this) { Console.WriteLine("IBase.MethodA()"); }
        public static IDerived AsDerived(this IDerived @this) { return @this; }
        public static void MethodA(this IDerived @this) { Console.WriteLine("IDerived.MethodA()"); }
    }
    
    class Program
    {
        static void Main()
        {
            var obj = new Derived();
            obj.MethodA(); // Prints: "Derived.MethodA()"
            obj.AsBase().MethodA(); // Prints: "IBase.MethodA()"
            obj.AsDerived().MethodA(); // Prints: "Derived.MethodA()" (NOT "IDerived.MethodA")
    
            Console.ReadKey();
        }
    }

For this reason, it is important to distill as many methods as possible out of the interface that defines the EBT.

## Limitations: Dependency on Compile-Time Types

Code that uses EBTs must "see" all the associated methods in order for overridden derived methods to work properly. The parallel problem for C++ templates eventually resulted in "header file libraries", where libraries transitioned from dlls to source code that was included in the program using the library.

A similar transition will probably occur if EBTs are embraced. Currently, if EBTs are compiled into a library (e.g., System.Core.dll), then end-user code may supplement but not replace existing behavior.

## Real-World Examples from LINQ

As LINQ continues to evolve, it approaches EBTs. In particular, the [IEnumerable<T>](http://msdn.microsoft.com/en-us/library/9eekhta0.aspx) interface is extended by the [Enumerable](http://msdn.microsoft.com/en-us/library/system.linq.enumerable.aspx) class, including an [AsEnumerable](http://msdn.microsoft.com/en-us/library/bb335435.aspx) identity transformation method. [IOrderedEnumerable<T>](http://msdn.microsoft.com/en-us/library/bb534852.aspx) is one derived type; the interface adds no methods, but the Enumerable class does. The rarely-used [IQueryable<T>](http://msdn.microsoft.com/en-us/library/bb351562.aspx) derived type is extended by [Queryable](http://msdn.microsoft.com/en-us/library/system.linq.queryable.aspx). Grouping and ordering follow the same pattern.

The new PLINQ has a similar structure (though the derived [ParallelQuery<T>](http://msdn.microsoft.com/en-us/library/dd383736(VS.100).aspx) is a class instead of an interface).

[Rx](http://msdn.microsoft.com/en-us/devlabs/ee794896.aspx) (a.k.a. "LINQ to Events") continues the tradition. IObservable<T>, IObserver<T>, and IScheduler<T> provide basic definitions of types, with extension methods in the Observable, Observer, and Scheduler classes. Rx follows the EBT pattern in defining minimal interfaces; for example, IObservable<T> only defines one method (Subscribe), and extension methods are used to provide 5 overloads for Subscribe. The Rx team has a [video](http://channel9.msdn.com/posts/J.Van.Gogh/Controlling-concurrency-in-Rx/) in which they describe some of the design behind IScheduler/Scheduler.

## Future Blog Posts

In the (hopefully near) future, I'll be showing how to use EBTs to do compile-time generic specialization (including partial specialization, generic method specialization, and inheritance specialization). I also intend to cover "namespaces" and wrapper objects. Lots of fun! :)

