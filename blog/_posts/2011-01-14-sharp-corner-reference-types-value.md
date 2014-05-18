---
layout: post
title: "Sharp Corner: Reference Types, Value Types, and Weirdos"
tags: [".NET", "Sharp Corners"]
---


This week I was writing some code that had to respond differently depending on whether a generic argument was a _reference type_ or a _value type_. This was bit complex, since "reference type" and "value type" do not have very specific meanings - which was actually a good thing because it forced me to consider _exactly_ what my code requirements were, and it turned out that I was using just slightly different meanings of "reference type" in different places.





During this exploration, I developed a few tests to evaluate the type system (code at the end of this post). The results are summarized below, along with some of my thoughts on the weirdos (types which are sort-of reference and sort-of value, depending on your definition of "reference" and "value").





One way of defining a "reference type" is whether [Type.IsClass](http://msdn.microsoft.com/en-us/library/system.type.isclass.aspx) is true; another way of defining a "reference type" is whether it satisfies a [generic class constraint](http://msdn.microsoft.com/en-us/library/d5x73970.aspx) (e.g., _void Test<T>() where T : class_). Likewise, value types have [Type.IsValueType](http://msdn.microsoft.com/en-us/library/system.type.isvaluetype.aspx) and generic struct constraints.





The table below includes tests on a variety of types, grouped into "mostly reference types" and "mostly value types". The types that are more clearly reference/value types are at the top of each group, with the weirdos at the bottom.



|Category|Example Type|IsClass|Satisfies class Constraint|IsValueType|Satisfies struct Constraint|Satisfies Without Constraints|
|-
|Classes|class Class {}|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|
|Arrays|int[]|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|
|Delegates|delegate void DelegateT();|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|
|Interfaces|interface Interface {}|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|
|Pointers|int*|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|
|
|Value types|int|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|
|Enumerations|enum EnumT {}|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|
|Nullable value types|int?|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|
|Void|void|&nbsp;|&nbsp;|&nbsp;|&nbsp;|&nbsp;|


## Interfaces are a Bit Weird



Interfaces return false for both **IsClass** and **IsValueType**. This makes sense, since either reference types or value types may inherit from an interface. However, interface variables may be declared and act like reference types (boxing value types as necessary), so interfaces do satisfy generic **class** constraints.



> Take-home point: If **IsClass** is false but **IsInterface** is true, the type will still satisfy a generic **class** constraint.


## Nullable Value Types are a Bit Weird



Nullable types return true for **IsValueType**, but do not satisfy generic **struct** constraints (nor **class** constraints). They can only be used as generic parameters without **class** or **struct** constraints.



> Take-home point: Nullable types **(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))** will not satisfy a generic **struct** constraint, even though **IsValueType** is true.


## Pointers are Definitely Weird



To be honest, I don't know [why IsClass is true for pointers](http://stackoverflow.com/questions/3317587/why-are-pointers-reference-types) (the spec says they are, but without any reason given). They act exactly like value types, and they can't satisfy a generic **class** constraint. In fact, they can't be used as any kind of generic argument. This makes pointer types a corner case: they only have to be dealt with if the user is passing a Type instance rather than a generic type argument.



> Take-home point: If **IsPointer** is true, then the type cannot be used as a generic type parameter at all (and therefore cannot satisfy a **class** constraint, even though **IsClass** is true).


## Void is Definitely Weird



Void claims to be a value type (**IsValueType** is true) - which sort of makes sense, if we think of it as a value type that cannot have a value - but it cannot satisfy a **struct** constraint. In fact, like pointers, **void** cannot be used as a generic type argument at all. This makes **void** another corner case: they only have to be dealt with if the user is passing a Type instance rather than a generic type argument.



> Take-home point: The **void** type **(type == typeof(void))** cannot be used as a generic type parameter at all (and therefore cannot satisfy a **struct** constraint, even though **IsValueType** is true).


## Test Code


using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TypeSystemTests
{
    internal static class TypeEx
    {
        public static void NoConstraint<T>() { }
        public static void StructConstraint<T>() where T : struct { }
        public static void ClassConstraint<T>() where T : class { }

        public static bool CanBeUsedAsGenericParameter(this Type type)
        {
            try
            {
                typeof(TypeEx).GetMethod("NoConstraint").MakeGenericMethod(type).Invoke(null, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SatisfiesGenericStructConstraint(this Type type)
        {
            try
            {
                typeof(TypeEx).GetMethod("StructConstraint").MakeGenericMethod(type).Invoke(null, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool SatisfiesGenericClassConstraint(this Type type)
        {
            try
            {
                typeof(TypeEx).GetMethod("ClassConstraint").MakeGenericMethod(type).Invoke(null, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsVoid(this Type type)
        {
            return (type == typeof(void));
        }

        public static bool IsNullable(this Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }
    }

    [TestClass]
    public class UnitTests
    {
        public class Class { }
        [TestMethod]
        public void Classes()
        {
            var type = typeof(Class);
            Assert.IsTrue(type.IsClass);
            Assert.IsTrue(type.SatisfiesGenericClassConstraint());
            Assert.IsFalse(type.IsValueType);
            Assert.IsFalse(type.SatisfiesGenericStructConstraint());
            Assert.IsTrue(type.CanBeUsedAsGenericParameter());

            Assert.IsFalse(type.IsArray);
            Assert.IsFalse(type.IsEnum);
            Assert.IsFalse(type.IsInterface);
            Assert.IsFalse(type.IsPointer);
            Assert.IsFalse(type.IsVoid());
            Assert.IsFalse(type.IsNullable());
        }

        [TestMethod]
        public void Arrays()
        {
            var type = typeof(int[]);
            Assert.IsTrue(type.IsClass);
            Assert.IsTrue(type.SatisfiesGenericClassConstraint());
            Assert.IsFalse(type.IsValueType);
            Assert.IsFalse(type.SatisfiesGenericStructConstraint());
            Assert.IsTrue(type.CanBeUsedAsGenericParameter());

            Assert.IsTrue(type.IsArray);
            Assert.IsFalse(type.IsEnum);
            Assert.IsFalse(type.IsInterface);
            Assert.IsFalse(type.IsPointer);
            Assert.IsFalse(type.IsVoid());
            Assert.IsFalse(type.IsNullable());
        }

        public delegate void DelegateT();
        [TestMethod]
        public void Delegates()
        {
            var type = typeof(DelegateT);
            Assert.IsTrue(type.IsClass);
            Assert.IsTrue(type.SatisfiesGenericClassConstraint());
            Assert.IsFalse(type.IsValueType);
            Assert.IsFalse(type.SatisfiesGenericStructConstraint());
            Assert.IsTrue(type.CanBeUsedAsGenericParameter());

            Assert.IsFalse(type.IsArray);
            Assert.IsFalse(type.IsEnum);
            Assert.IsFalse(type.IsInterface);
            Assert.IsFalse(type.IsPointer);
            Assert.IsFalse(type.IsVoid());
            Assert.IsFalse(type.IsNullable());
        }

        public interface Interface { }
        [TestMethod]
        public void Interfaces()
        {
            var type = typeof(Interface);
            Assert.IsFalse(type.IsClass);
            Assert.IsTrue(type.SatisfiesGenericClassConstraint());
            Assert.IsFalse(type.IsValueType);
            Assert.IsFalse(type.SatisfiesGenericStructConstraint());
            Assert.IsTrue(type.CanBeUsedAsGenericParameter());

            Assert.IsFalse(type.IsArray);
            Assert.IsFalse(type.IsEnum);
            Assert.IsTrue(type.IsInterface);
            Assert.IsFalse(type.IsPointer);
            Assert.IsFalse(type.IsVoid());
            Assert.IsFalse(type.IsNullable());
        }

        [TestMethod]
        public void Pointers()
        {
            unsafe
            {
                var type = typeof(int*);
                Assert.IsTrue(type.IsClass);
                Assert.IsFalse(type.SatisfiesGenericClassConstraint());
                Assert.IsFalse(type.IsValueType);
                Assert.IsFalse(type.SatisfiesGenericStructConstraint());
                Assert.IsFalse(type.CanBeUsedAsGenericParameter());

                Assert.IsFalse(type.IsArray);
                Assert.IsFalse(type.IsEnum);
                Assert.IsFalse(type.IsInterface);
                Assert.IsTrue(type.IsPointer);
                Assert.IsFalse(type.IsVoid());
                Assert.IsFalse(type.IsNullable());
            }
        }


        [TestMethod]
        public void ValueTypes()
        {
            var type = typeof(int);
            Assert.IsFalse(type.IsClass);
            Assert.IsFalse(type.SatisfiesGenericClassConstraint());
            Assert.IsTrue(type.IsValueType);
            Assert.IsTrue(type.SatisfiesGenericStructConstraint());
            Assert.IsTrue(type.CanBeUsedAsGenericParameter());

            Assert.IsFalse(type.IsArray);
            Assert.IsFalse(type.IsEnum);
            Assert.IsFalse(type.IsInterface);
            Assert.IsFalse(type.IsPointer);
            Assert.IsFalse(type.IsVoid());
            Assert.IsFalse(type.IsNullable());
        }

        public enum EnumT { }
        [TestMethod]
        public void Enums()
        {
            var type = typeof(EnumT);
            Assert.IsFalse(type.IsClass);
            Assert.IsFalse(type.SatisfiesGenericClassConstraint());
            Assert.IsTrue(type.IsValueType);
            Assert.IsTrue(type.SatisfiesGenericStructConstraint());
            Assert.IsTrue(type.CanBeUsedAsGenericParameter());

            Assert.IsFalse(type.IsArray);
            Assert.IsTrue(type.IsEnum);
            Assert.IsFalse(type.IsInterface);
            Assert.IsFalse(type.IsPointer);
            Assert.IsFalse(type.IsVoid());
            Assert.IsFalse(type.IsNullable());
        }

        [TestMethod]
        public void NullableValueTypes()
        {
            var type = typeof(int?);
            Assert.IsFalse(type.IsClass);
            Assert.IsFalse(type.SatisfiesGenericClassConstraint());
            Assert.IsTrue(type.IsValueType);
            Assert.IsFalse(type.SatisfiesGenericStructConstraint());
            Assert.IsTrue(type.CanBeUsedAsGenericParameter());

            Assert.IsFalse(type.IsArray);
            Assert.IsFalse(type.IsEnum);
            Assert.IsFalse(type.IsInterface);
            Assert.IsFalse(type.IsPointer);
            Assert.IsFalse(type.IsVoid());
            Assert.IsTrue(type.IsNullable());
        }

        [TestMethod]
        public void Void()
        {
            var type = typeof(void);
            Assert.IsFalse(type.IsClass);
            Assert.IsFalse(type.SatisfiesGenericClassConstraint());
            Assert.IsTrue(type.IsValueType);
            Assert.IsFalse(type.SatisfiesGenericStructConstraint());
            Assert.IsFalse(type.CanBeUsedAsGenericParameter());

            Assert.IsFalse(type.IsArray);
            Assert.IsFalse(type.IsEnum);
            Assert.IsFalse(type.IsInterface);
            Assert.IsFalse(type.IsPointer);
            Assert.IsTrue(type.IsVoid());
            Assert.IsFalse(type.IsNullable());
        }
    }
}
