---
layout: post
title: "Option Parsing: Argument Parsing"
tags: ["Option Parsing", ".NET", "Nito.KitchenSink"]
---


This is going to be an in-depth post on how argument parsing works in the [Nito.KitchenSink.OptionParsing library](http://nuget.org/List/Packages/Nito.KitchenSink.OptionParsing), and a couple of ways the parsing can be modified.



## General Option Argument Parsing Rules



First, a reminder about terminology; in this example, the "v" is the short option name, and the "3" is the option argument:



> CommandLineTest.exe -v 3



Also remember that an option argument may be _required_ for an option, or it may be _optional_. If you need a refresher, read the earlier post [options with optional arguments](http://blog.stephencleary.com/2011/07/option-parsing-options-with-optional.html).





Required option arguments are allowed to begin with a dash (**-**) or forward-slash (**/**), but optional option arguments are not. To start an optional option argument with these characters, specify the argument using a full-colon (**:**) or equals sign (**=**).





Consider this example program, which just takes two string arguments, one required and one optional:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("required", 'r')]
    public string RequiredValue { get; set; }

    [Option("optional", 'o', Argument = OptionArgument.Optional)]
    public string OptionalValue { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      if (options.RequiredValue != null)
        Console.WriteLine("Required Value: " + options.RequiredValue);
      if (options.OptionalValue != null)
        Console.WriteLine("Optional Value: " + options.OptionalValue);

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



> CommandLineParsingTest.exe -r a -o b
Required Value: a
Optional Value: b

> CommandLineParsingTest.exe -r /a -o b
Required Value: /a
Optional Value: b

> CommandLineParsingTest.exe -r a -o /b
Unknown option  b  in parameter  /b

> CommandLineParsingTest.exe -o "/b"
Unknown option  b  in parameter  /b

> CommandLineParsingTest.exe -o:/b
Optional Value: /b

> CommandLineParsingTest.exe -o=/b
Optional Value: /b




Note that placing the argument in double-quotes does _not_ allow the argument to start with a dash or forward-slash.





Reminder: the command shell has its own set of reserved characters (**&**, **|**, **(**, **)**, **<**, **>**, and **^**). These can be escaped using **^**, or they can be wrapped in double-quotes. Command shell escapes are described in more detail in the [post on command-line lexing](http://blog.stephencleary.com/2011/06/option-parsing-lexing.html).



## Implementing a Simple Argument Parser



Parsing an argument option is done in two steps. The first step is to parse that portion of the command line as a string, using the rules above. The second step is to parse the string into an instance of the corresponding property type on the option arguments class. Since the examples above used a property type of string, there was no processing during the second step.



> It is possible to use only a part of the [option parsing pipeline](http://blog.stephencleary.com/2011/06/option-parsing-option-parsing-pipeline.html) to get options and their arguments as strings. Pass a sequence of **OptionDefinition** instancess and a command line into the parser; the result is a sequence of **Option** instances (where each argument is typed as **string**). Details of these types will be covered in a future blog post.




The option parsing library uses a collection of "simple parsers" to convert from a string to a known type. By default, the simple parser collection understands how to parse **bool**; signed and unsigned 8-bit, 16-bit, 32-bit, and 64-bit integers; **BigInteger**; single and double-precision floating point; **decimal**; **Guid**; **TimeSpan**; **DateTime**; and **DateTimeOffset**. Strings, enumerations and nullable types are treated specially: strings are never parsed, enumerations use **Enum.Parse**, and nullable types are supported if their corresponding non-nullable types are supported. The built-in parsers all use the standard **TryParse** methods.





Say, for example, we wanted to accept an argument of type [Complex](http://msdn.microsoft.com/en-us/library/system.numerics.complex.aspx). The Complex type is not included in the default simple parser collection (in fact, it does not even have a Parse or TryParse method!).





If we try to add it to our program, then whatever we pass as the argument value will just fail to parse:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("value", 'v')]
    public Complex? Value { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      if (options.Value != null)
        Console.WriteLine("Value: " + options.Value);

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



> CommandLineParsingTest.exe -v (3,5)
Could not parse  (3,5)  as Complex




We can create a parser for the **Complex** type by implementing **ISimpleParser**. This interface only has two members: the type of the result and a **TryParse** method.





Once we've implemented our special parser, we need to pass it to the Parse method. To do this, we create a **SimpleParserCollection**, add our special parser, and pass the collection to the Parse method.





Our solution now looks like this:




class Program
{
  private sealed class ComplexParser : ISimpleParser
  {
    public Type ResultType
    {
      get { return typeof(Complex); }
    }

    public object TryParse(string value)
    {
      // Match the following pattern: '(' double ',' double ')'
      if (value.Length < 5 || value[0] != '(' || value[value.Length - 1] != ')')
        return null;
      var components = value.Substring(1, value.Length - 2).Split(',');
      if (components.Length != 2)
        return null;
      double real, imaginary;
      if (!double.TryParse(components[0], out real))
        return null;
      if (!double.TryParse(components[1], out imaginary))
        return null;
      return new Complex(real, imaginary);
    }
  }


  private sealed class Options : OptionArgumentsBase
  {
    [Option("value", 'v')]
    public Complex? Value { get; set; }
  }

  static int Main()
  {
    try
    {
      var parsers = new SimpleParserCollection();
      parsers.Add(new ComplexParser());
      var options = OptionParser.Parse<Options>(parserCollection: parsers);

      if (options.Value != null)
        Console.WriteLine("Value: " + options.Value);

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



> CommandLineParsingTest.exe -v (3,5)
Value: (3, 5)




We added a custom parser to the collection, and the option parsing library now understands how to parse a new type. We could add any number of **Complex** properties, and they would all use the new parser.





This is a powerful extension point, but what if we want to modify the way an extisting type is parsed?



## Replacing a Simple Argument Parser



The default parsers in a simple parser collection only use the basic **TryParse** methods, which may not be exactly what is needed. **SimpleParserCollection.Add** will actually _replace_ the parser for a given type if there is already a parser for that type.





We'll use **uint** for our example. We want to allow either decimal numbers or hexadecimal numbers prefixed by "0x". **System.UInt32.TryParse(string)** does not accept hexadecimal numbers:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("value", 'v')]
    public uint? Value { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      if (options.Value != null)
        Console.WriteLine("Value: " + options.Value);

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



> CommandLineParsingTest.exe -v 11
Value: 11

> CommandLineParsingTest.exe -v 0x11
Could not parse  0x11  as UInt32




Just like the last example, we'll implement our own parser, and we'll add it to the parser collection (replacing the default parser).




class Program
{
  private sealed class UInt32HexParser : ISimpleParser
  {
    public Type ResultType
    {
      get { return typeof(uint); }
    }

    public object TryParse(string value)
    {
      uint ret;
      if (value.StartsWith("0x"))
      {
        if (!uint.TryParse(value.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ret))
          return null;
        return ret;
      }

      if (!uint.TryParse(value, out ret))
        return null;
      return ret;
    }
  }

  private sealed class Options : OptionArgumentsBase
  {
    [Option("value", 'v')]
    public uint? Value { get; set; }
  }

  static int Main()
  {
    try
    {
      var parsers = new SimpleParserCollection();
      parsers.Add(new UInt32HexParser());
      var options = OptionParser.Parse<Options>(parserCollection: parsers);

      if (options.Value != null)
        Console.WriteLine("Value: " + options.Value);

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



> CommandLineParsingTest.exe -v 11
Value: 11

> CommandLineParsingTest.exe -v 0x11
Value: 17




Our program now allows decimal or hexadecimal values for all **uint** argument values.





These custom parsers can be written for any type, including types specific for your program. The only type they won't work on is string, since the simple parser collection just passes string values straight through.



## Overriding the Simple Argument Parser



The examples so far have implemented a custom parser and added it to the parser collection. This changes the parsing behavior for _every_ property of that type. Sometimes we just want to apply a parser to a single property; this can be done by using the **SimpleParserAttribute**.





This example defines a hex parser (without the "0x" prefix) and then uses it for only one of its properties:




class Program
{
  private sealed class UInt32HexParser : ISimpleParser
  {
    public Type ResultType
    {
      get { return typeof(uint); }
    }

    public object TryParse(string value)
    {
      uint ret;
      if (!uint.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ret))
        return null;
      return ret;
    }
  }

  private sealed class Options : OptionArgumentsBase
  {
    [Option("hex-value", 'h')]
    [SimpleParser(typeof(UInt32HexParser))]
    public uint? HexValue { get; set; }

    [Option("dec-value", 'd')]
    public uint? DecValue { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      if (options.HexValue != null)
        Console.WriteLine("HexValue: " + options.HexValue);
      if (options.DecValue != null)
        Console.WriteLine("DecValue: " + options.DecValue);

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



> CommandLineParsingTest.exe -h 11 -d 11
HexValue: 17
DecValue: 11


## Custom Parsers for Multiple Argument Values



Revisiting the problem of [multiple argument values](http://blog.stephencleary.com/2011/07/option-parsing-allowing-multiple.html), we can use a custom parser for a cleaner solution. This example "sequence parser" uses the default simple parser for **int** types, which is easier to deal with than **int.TryParse**:




class Program
{
  private sealed class Int32SequenceParser : ISimpleParser
  {
    public Type ResultType
    {
      get { return typeof(IEnumerable<int>); }
    }

    public object TryParse(string value)
    {
      var values = value.Split(',');
      ISimpleParser defaultParser = new DefaultSimpleParser<int>();
      var result = values.Select(x => defaultParser.TryParse(x));
      if (result.Any(x => x == null))
        return null;
      return result.Cast<int>();
    }
  }

  private sealed class Options : OptionArgumentsBase
  {
    [Option("values", 'v')]
    [SimpleParser(typeof(Int32SequenceParser))]
    public IEnumerable<int> Values { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      if (options.Values != null)
        Console.WriteLine("Values: " + string.Join(" ", options.Values));

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

> CommandLineParsingTest.exe -v 2,3,5,7
Values: 2 3 5 7

> CommandLineParsingTest.exe -v 2,3a,5
Could not parse  2,3a,5  as IEnumerable<Int32>

> CommandLineParsingTest.exe -v 2,3 -v 5,7
Values: 5 7




The last example above shows that the default behavior of the actual property setter is still _overwrite_, not _append_. If you want to allow appending sequences, you'll need to change the setter to append each sequence to an internal collection.

