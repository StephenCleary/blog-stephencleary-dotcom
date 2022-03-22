---
layout: post
title: "Unit Testing with Roslyn"
description: "An unusual use case for Roslyn: unit testing DotNetApis."
---

## Roslyn

Roslyn was a huge project that spanned many years. It was a difficult rewrite of the C# and VB compilers into managed code, with the intent of also enabling fast analysis of partial code segments. These days, Roslyn powers many different systems: the compiler itself, the little hints that pop up as you type, and even static analysis systems that you can bundle with your NuGet package to encourage proper usage.

I've thought many times about using Roslyn to create some analyzers for common mistakes with `Task` and `async`, but I've just never taken the time to do it. However, just recently, I had the opportunity to use Roslyn for something completely different.

## DotNetApis

I run [a service called DotNetApis](http://dotnetapis.com/) that autogenerates reference documentation for NuGet packages. It does this by walking the CLI metadata (using the awesome [Mono.Cecil](https://github.com/jbevain/cecil)) and matching accessible elements with their XML documentation.

The code that currently runs that site is a bit of a mess. I've been cleaning up the code into a v2 [that is open-source through and through](https://github.com/StephenClearyApps/DotNetApis). As a part of this rewrite, I needed a way to unit test some odd code elements. In the v1 code, I had a single "test"/"sample" dll that had a bunch of weird members, and I ran my unit tests against that. This worked, but I wanted my unit tests to be more self-contained.

Enter Roslyn.

I have unit tests in DotNetApis v2 that need to compile some code (as a `string`) and then parse the resulting dll and xml. Getting this working was surprisingly easy!

{% highlight csharp %}
public static (AssemblyDefinition Dll, XDocument Xml) Compile(string code)
{
  // Parse the C# code...
  CSharpParseOptions parseOptions = new CSharpParseOptions()
    .WithKind(SourceCodeKind.Regular) // ...as representing a complete .cs file
    .WithLanguageVersion(LanguageVersion.Latest); // ...enabling the latest language features

  // Compile the C# code...
  CSharpCompilationOptions compileOptions =
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary) // ...to a dll
    .WithOptimizationLevel(OptimizationLevel.Release) // ...in Release configuration
    .WithAllowUnsafe(enabled: true); // ...enabling unsafe code

  // Invoke the compiler...
  CSharpCompilation compilation =
    CSharpCompilation.Create("TestInMemoryAssembly") // ..with some fake dll name
    .WithOptions(compileOptions)
    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)); // ...referencing the same mscorlib we're running on

  // Parse and compile the C# code into a *.dll and *.xml file in-memory
  var tree = CSharpSyntaxTree.ParseText(code, parseOptions);
  var compilation = compilation.AddSyntaxTrees(tree);
  var peStream = new MemoryStream();
  var xmlStream = new MemoryStream();
  var emitResult = compilation.Emit(peStream, xmlDocumentationStream: xmlStream);
  if (!emitResult.Success)
    throw new InvalidOperationException("Compilation failed: " + string.Join("\n", emitResult.Diagnostics));

  // Parse the *.dll (with Cecil) and the *.xml (with XDocument)
  peStream.Seek(0, SeekOrigin.Begin);
  xmlStream.Seek(0, SeekOrigin.Begin);
  return (AssemblyDefinition.ReadAssembly(peStream), XDocument.Load(xmlStream));
}
{% endhighlight %}

This beautiful little utilty method takes C# code and spits out a parsed dll and xml file, all in-memory! The [actual code](https://github.com/StephenClearyApps/DotNetApis/blob/0b119d8698a3439b2170ae12c3a438fc2f6e9a0b/service/UnitTestUtility/Utility.cs) is a bit more complex for efficiency reasons. I currently have 86 unit tests using this method, with lots more on the way!

Here's what one of the unit tests looks like:

{% highlight csharp %}
[Fact]
public void Basic_InTopLevelType()
{
  var code =
    @"public class SampleClass {
      /// <summary>Text to find.</summary>
      public void SampleMethod() { } }";
  var (assembly, xmldoc) = Compile(code);
  var type = assembly.Modules.SelectMany(x => x.Types).Single(x => x.Name == "SampleClass");
  var method = type.Methods.Single(x => x.Name == "SampleMethod");

  Assert.Equal("M:SampleClass.SampleMethod", method.XmldocIdentifier());
  AssertXmldoc("Text to find.", xmldoc, method);
}

public static void AssertXmldoc(XDocument xmldoc, string expectedValue, IMemberDefinition member, string elementName = "summary")
{
    var doc = xmldoc.Descendants("member").FirstOrDefault(x => x.Attribute("name")?.Value == member.MemberXmldocIdentifier()).Element(elementName);
    Assert.Equal(expectedValue, string.Join("", doc.Nodes().Select(x => x.ToString(SaveOptions.DisableFormatting))));
}
{% endhighlight %}

This unit test is checking that the [Xmldoc Identifier](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/processing-the-xml-file?WT.mc_id=DT-MVP-5000058) calculated by DotNetApis is in fact what we expect it to be (`M:SampleClass.SampleMethod`) *and* that it matches what the C# compiler generated in the xml file. The `AssertXmldoc` helper is taking the Xmldoc Id from DotNetApis, looking it up in the *.xml file from the compiler, and asserting that the text we extract is what is expected.

Sure, this example is pretty easy, but I've also started adding the more rare cases like methods that take an array of pointers by reference. There's a *lot* of more complex cases that are undocumented, and we have to rely on observed compiler behavior.

## Roslyn is Cool

That's all I have to say. I just thought it's *so cool* how easy Roslyn made this. And it's *fast*, too! I can keep my Live Unit Testing running while hacking around, even with most of my tests running Roslyn, and it's all pretty slick!
