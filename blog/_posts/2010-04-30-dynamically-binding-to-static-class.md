---
layout: post
title: "Dynamically Binding to Static (Class-Scoped) Members"
---
.NET 4.0 was a huge release, containing a wide variety of much-anticipated features. One of these features is the C# support for dynamic languages via the new keyword _dynamic_. Dynamic brings some very powerful semantics into the language, and naturally also comes with a few limitations.

One limitation is dynamically accessing static (class-scoped) members. The _dynamic_ type is intended to represent a dynamic instance, not a dynamic class. For example, if two different classes have the same static method defined, there is no way to use _dynamic_ to invoke those static methods.

One can use the [DynamicObject](http://msdn.microsoft.com/en-us/library/system.dynamic.dynamicobject.aspx) class to redirect instance member access to static member access. This approach was first explored in David Ebbo's blog post ["Using C# dynamic to call static members"](http://blogs.msdn.com/davidebb/archive/2009/10/23/using-c-dynamic-to-call-static-members.aspx). However, this approach brings with it its own limitation.

The general concept is to implement a DynamicObject type that uses reflection to access static members. This makes sense since _dynamic_ may be seen as a more user-friendly type of reflection (of course, this simple interpretation ignores a lot of other DLR benefits). Unfortunately, DynamicObject does not support the concept of ref/out parameters, even though they are fully supported by _dynamic_. There is a work-around for this: wrapping ref or out parameters, adding a layer of indirection. The RefOutArg class was invented for this purpose ([official source](http://nitokitchensink.codeplex.com/SourceControl/changeset/view/51391#1073961)):

{% highlight csharp %}

/// <summary>
/// A wrapper around a "ref" or "out" argument invoked dynamically.
/// </summary>
public sealed class RefOutArg
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefOutArg"/> class.
    /// </summary>
    private RefOutArg()
    {
    }

    /// <summary>
    /// Gets or sets the wrapped value as an object.
    /// </summary>
    public object ValueAsObject { get; set; }

    /// <summary>
    /// Gets or sets the wrapped value.
    /// </summary>
    public dynamic Value
    {
        get
        {
            return this.ValueAsObject;
        }

        set
        {
            this.ValueAsObject = value;
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RefOutArg"/> class wrapping the default value of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of value to wrap.</typeparam>
    /// <returns>A new instance of the <see cref="RefOutArg"/> class wrapping the default value of <typeparamref name="T"/>.</returns>
    public static RefOutArg Create<T>()
    {
        return new RefOutArg { ValueAsObject = default(T) };
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RefOutArg"/> class wrapping the specified value.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>A new instance of the <see cref="RefOutArg"/> class wrapping the specified value.</returns>
    public static RefOutArg Create(object value)
    {
        return new RefOutArg { ValueAsObject = value };
    }
}
{% endhighlight %}

RefOutArg is a very simple class that contains a single value (which can be accessed either as _object_ or _dynamic_).

The DynamicStaticTypeMembers class enables dynamic access to static members. It is similar to David's StaticMembersDynamicWrapper, only this class allows setting static properties, invoking overloaded static methods, and ref/out parameters using RefOutArg ([official source](http://nitokitchensink.codeplex.com/SourceControl/changeset/view/51391#1073960)):

{% highlight csharp %}

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Reflection;

/// <summary>
/// A dynamic object that allows access to a type's static members, resolved dynamically at runtime.
/// </summary>
public sealed class DynamicStaticTypeMembers : DynamicObject
{
    /// <summary>
    /// The underlying type.
    /// </summary>
    private readonly Type type;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicStaticTypeMembers"/> class wrapping the specified type.
    /// </summary>
    /// <param name="type">The underlying type to wrap.</param>
    private DynamicStaticTypeMembers(Type type)
    {
        this.type = type;
    }

    /// <summary>
    /// Gets a value for a static property defined by the wrapped type.
    /// </summary>
    /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
    /// <param name="result">The result of the get operation. For example, if the method is called for a property, you can assign the property value to <paramref name="result"/>.</param>
    /// <returns>
    /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a run-time exception is thrown.)
    /// </returns>
    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        var prop = this.type.GetProperty(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public);
        if (prop == null)
        {
            result = null;
            return false;
        }

        result = prop.GetValue(null, null);
        return true;
    }

    /// <summary>
    /// Sets a value for a static property defined by the wrapped type.
    /// </summary>
    /// <param name="binder">Provides information about the object that called the dynamic operation. The binder.Name property provides the name of the member to which the value is being assigned. For example, for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
    /// <param name="value">The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, the <paramref name="value"/> is "Test".</param>
    /// <returns>
    /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        var prop = this.type.GetProperty(binder.Name, BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public);
        if (prop == null)
        {
            return false;
        }

        prop.SetValue(null, value, null);
        return true;
    }

    /// <summary>
    /// Calls a static method defined by the wrapped type.
    /// </summary>
    /// <param name="binder">Provides information about the dynamic operation. The binder.Name property provides the name of the member on which the dynamic operation is performed. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, binder.Name returns "SampleMethod". The binder.IgnoreCase property specifies whether the member name is case-sensitive.</param>
    /// <param name="args">The arguments that are passed to the object member during the invoke operation. For example, for the statement sampleObject.SampleMethod(100), where sampleObject is derived from the <see cref="T:System.Dynamic.DynamicObject"/> class, <c>args[0]</c> is equal to 100.</param>
    /// <param name="result">The result of the member invocation.</param>
    /// <returns>
    /// true if the operation is successful; otherwise, false. If this method returns false, the run-time binder of the language determines the behavior. (In most cases, a language-specific run-time exception is thrown.)
    /// </returns>
    public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
    {
        // Convert any RefOutArg arguments into ref/out arguments
        var refArguments = new RefOutArg[args.Length];
        for (int i = 0; i != args.Length; ++i)
        {
            refArguments[i] = args[i] as RefOutArg;
            if (refArguments[i] != null)
            {
                args[i] = refArguments[i].ValueAsObject;
            }
        }

        // Resolve the method
        const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public;
        object state;
        MethodBase method;

        var methods = this.type.GetMethods(flags).Where(x => x.Name == binder.Name);
        method = Type.DefaultBinder.BindToMethod(flags, methods.ToArray(), ref args, null, null, null, out state);

        // Ensure that all ref/out arguments were properly wrapped
        if (method.GetParameters().Count(x => x.ParameterType.IsByRef) != refArguments.Count(x => x != null))
        {
            throw new ArgumentException("ref/out parameters need a RefOutArg wrapper when invoking " + this.type.Name + "." + binder.Name + ".");
        }

        // Invoke the method, allowing exceptions to propogate
        try
        {
            result = method.Invoke(null, args);
        }
        finally
        {
            if (state != null)
            {
                Type.DefaultBinder.ReorderArgumentArray(ref args, state);
            }

            // Convert any ref/out arguments into RefOutArg results
            for (int i = 0; i != args.Length; ++i)
            {
                if (refArguments[i] != null)
                {
                    refArguments[i].ValueAsObject = args[i];
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DynamicStaticTypeMembers"/> class wrapping the specified type.
    /// </summary>
    /// <param name="type">The underlying type to wrap. May not be <c>null</c>.</param>
    /// <returns>An instance of <see cref="DynamicStaticTypeMembers"/>, as a dynamic type.</returns>
    public static dynamic Create(Type type)
    {
        Contract.Requires<ArgumentNullException>(type != null);
        return new DynamicStaticTypeMembers(type);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DynamicStaticTypeMembers"/> class wrapping the specified type.
    /// </summary>
    /// <typeparam name="T">The underlying type to wrap.</typeparam>
    /// <returns>An instance of <see cref="DynamicStaticTypeMembers"/>, as a dynamic type.</returns>
    public static dynamic Create<T>()
    {
        return new DynamicStaticTypeMembers(typeof(T));
    }
}
{% endhighlight %}

An instance of DynamicStaticTypeMembers may be constructed by passing either a generic type or Type instance into the Create method:

{% highlight csharp %}

var mathClass = DynamicStaticTypeMembers.Create(typeof(Math));
var intEqualityComparerClass = DynamicStaticTypeMembers.Create<EqualityComparer<int>>();
var threadClass = DynamicStaticTypeMembers.Create<Thread>();
var intClass = DynamicStaticTypeMembers.Create<int>();
{% endhighlight %}

Once created, any static property or method of that class may be invoked using instance syntax:

{% highlight csharp %}

int result0 = mathClass.Min(13, 15); // invokes Math.Min(int, int)
var comparer = intEqualityComparerClass.Default; // gets EqualityComparer<int>.Default
threadClass.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("Bob"), new string[] { }); // sets Thread.CurrentPrincipal
{% endhighlight %}

Invoking methods with ref or out parameters is more awkward, but possible:

{% highlight csharp %}

int result1;
var result1arg = RefOutArg.Create<int>(); // or: RefOutArg.Create(0);
intClass.TryParse("13", result1arg); // invokes int.TryParse(string, out int)
result1 = result1arg.Value;
{% endhighlight %}

This can be a powerful tool in some cases, allowing a higher form of "duck typing." For instance, the new [BigInteger](http://msdn.microsoft.com/en-us/library/system.numerics.biginteger.aspx) numeric type defines its own _DivRem_ method similar to the existing _DivRem_ methods defined on the [Math](http://msdn.microsoft.com/en-us/library/system.math.aspx) class for _int_ and _long_. Using DynamicStaticTypeMembers, it is possible to define a generic _DivRem_ that attempts to invoke _Math.DivRem_ but falls back on a _DivRem_ defined by the numeric type:

{% highlight csharp %}

public static T DivRem<T>(T dividend, T divisor, out T remainder)
{
    var remainderArg = RefOutArg.Create<T>();
    dynamic ret;
    try
    {
        var dT = DynamicStaticTypeMembers.Create(typeof(Math));
        ret = dT.DivRem(dividend, divisor, remainderArg);
    }
    catch
    {
        var dT = DynamicStaticTypeMembers.Create<T>();
        ret = dT.DivRem(dividend, divisor, remainderArg);
    }

    remainder = remainderArg.Value;
    return ret;
}
{% endhighlight %}

Our generic _DivRem_ can be invoked with T being _int_, _long_, _BigInteger_, or any other type as long as that type defines its own _DivRem_ with a compatible signature.

Most programs will not require this level of type flexibility, but it's nice to know it's there for those few cases that do need it.

