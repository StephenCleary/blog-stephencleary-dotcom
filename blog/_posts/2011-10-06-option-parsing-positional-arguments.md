---
layout: post
title: "Option Parsing; Positional Arguments"
series: "Option Parsing"
seriesTitle: "Option Parsing; Positional Arguments"
---
"Positional arguments" are any arguments not associated with an option. When using the [Nito.KitchenSink option parsing library](http://nuget.org/List/Packages/Nito.KitchenSink.OptionParsing), positional arguments must come after any options and their arguments.

## Individual Positional Arguments

You can use the **PositionalArgumentAttribute** to specify positional arguments in your options class. This attribute takes a single integral parameter, the 0-based index of the positional argument.

Positional arguments support the entire range of [parsing possibilities]({% post_url 2011-08-11-option-parsing-argument-parsing %}), including **SimpleParserAttribute**.

This example uses a regular **Level** option along with a **Name** positional parameter.

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option('l')]
    public int? Level { get; set; }

    [PositionalArgument(0)]
    public string Name { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("Level: " + options.Level);
      Console.WriteLine("Name: " + options.Name);

      return 0;
    }
    catch (OptionParsingException ex)
    {
      Console.Error.WriteLine(ex.Message);
      return 2;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine(ex);
      return 1;
    }
  }
}

> CommandLineParsingTest.exe
Level:
Name:

> CommandLineParsingTest.exe Bob
Level:
Name: Bob

> CommandLineParsingTest.exe -l 13
Level: 13
Name:

> CommandLineParsingTest.exe -l 13 Bob
Level: 13
Name: Bob

> CommandLineParsingTest.exe Bob -l 13
Unknown parameter  -l

The last test above shows that positional arguments must come after all regular options.

If you need to pass a positional argument that starts with a dash (-) or forward slash (/), you can pass the special option "--", which forces all remaining command-line arguments to be interpreted as positional arguments:

> CommandLineParsingTest.exe -Negative
Unknown option  N  in parameter  -Negative

> CommandLineParsingTest.exe -- -Negative
Level:
Name: -Negative

## The Positional Argument Collection

Every options class must have one property that can receive "extra" positional arguments. Extra positional arguments are any positional arguments after those defined by **PositionalArgumentAttribute**.

Most programs do not need this functionality, so the **OptionArgumentsBase** class provides a simple collection called **AdditionalArguments**. By default, **OptionArgumentsBase.Validate** will throw an **UnknownOptionException** if any positional arguments end up in that collection.

A program may make use of the **AdditionalArguments** collection by overriding **Validate**:

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [PositionalArgument(0)]
    public string Name { get; set; }

    public override void Validate()
    {
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("Name: " + options.Name);
      Console.WriteLine("ArgList: " + string.Join(", ", options.AdditionalArguments));

      return 0;
    }
    catch (OptionParsingException ex)
    {
      Console.Error.WriteLine(ex.Message);
      return 2;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine(ex);
      return 1;
    }
  }
}

> CommandLineParsingTest.exe
Name:
ArgList:

> CommandLineParsingTest.exe Bob
Name: Bob
ArgList:

> CommandLineParsingTest.exe Bob 17
Name: Bob
ArgList: 17

> CommandLineParsingTest.exe Bob -l 13
Name: Bob
ArgList: -l, 13

> CommandLineParsingTest.exe -- Bob
Name: Bob
ArgList:

Alternatively, an options class may provide its own collection, marked with the **PositionalArgumentsAttribute** (note the plural "Argument**s**"). When it does this, the options class may _not_ derive from **OptionArgumentsBase**; rather, it should implement the **IOptionArguments** interface.

The property does not have to be **List<string>** (which is used by **OptionArgumentsBase**). The only requirements on the collection is that it only have one method named **Add** which takes a single parameter. The parameter does not have to be **string**; it can be any type, and the [standard parsing rules]({% post_url 2011-08-11-option-parsing-argument-parsing %}) apply.

> This means that **PositionalArguments** can be placed on a property of dictionary type, as long as a matching parser is provided.

Here's an example of a program taking any number of integer parameters:

class Program
{
  private sealed class Options : IOptionArguments
  {
    public Options()
    {
      this.Integers = new List<int>();
    }

    [PositionalArguments]
    public List<int> Integers { get; private set; }

    public void Validate()
    {
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("Integers: " + string.Join(", ", options.Integers));

      return 0;
    }
    catch (OptionParsingException ex)
    {
      Console.Error.WriteLine(ex.Message);
      return 2;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine(ex);
      return 1;
    }
  }
}

> CommandLineParsingTest.exe
Integers:

> CommandLineParsingTest.exe 13
Integers: 13

> CommandLineParsingTest.exe 13 7
Integers: 13, 7

> CommandLineParsingTest.exe 13 7 Bob
Could not parse  Bob  as Int32
