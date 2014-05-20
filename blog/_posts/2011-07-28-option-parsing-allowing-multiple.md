---
layout: post
title: "Option Parsing: Allowing Multiple Argument Values"
series: "Option parsing"
seriesTitle: "Allowing Multiple Argument Values"
---
Some options need to take a _sequence_ of argument values. There are several ways to accomplish this using the [Nito.KitchenSink Option Parsing library](http://nuget.org/List/Packages/Nito.KitchenSink.OptionParsing).

## Enumeration Flags

If the option values are a series of enumerated flags, then the built-in enumeration parser will handle multiple values automatically:

class Program
{
  [Flags]
  private enum FavoriteThings
  {
    None = 0x0,
    Mittens = 0x1,
    Kittens = 0x2,
    Snowflakes = 0x4,
  }

  private sealed class Options : OptionArgumentsBase
  {
    [Option("favorite-things", 'f')]
    public FavoriteThings FavoriteThings { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine(options.FavoriteThings);

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
None

> CommandLineParsingTest.exe /favorite-things Mittens
Mittens

> CommandLineParsingTest.exe /favorite-things Mittens,Kittens
Mittens, Kittens

> CommandLineParsingTest.exe /favorite-things "Mittens, Snowflakes"
Mittens, Snowflakes

> CommandLineParsingTest.exe /favorite-things DogBites
Could not parse  DogBites  as FavoriteThings

## Using a Property Setter for Individual Values

The example above works well enough for enumerations, but not all arguments are that simple. In these situations, we can take advantage of the fact that arguments are applied to the options class by property setters.

The following example allows multiple individual values for an argument. As each argument value is set, it is saved into a collection of values.

Note that using a property setter in this fashion is not a good OOP practice; however, the adverse design affects are contained within the options class.

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    public Options()
    {
      Numbers = new List<int>();
    }

    public List<int> Numbers { get; private set; }

    [Option("number", 'n')]
    public int NumberOption
    {
      set
      {
        Numbers.Add(value);
      }
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine(string.Join(", ", options.Numbers));

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

> CommandLineParsingTest.exe -n 3
3

> CommandLineParsingTest.exe -n 3 -n 6
3, 6

> CommandLineParsingTest.exe -n 3,6
Could not parse  3,6  as Int32

Note that the last test failed; the options class above only allows multiple individual arguments, not a group of values.

## Using a Property Setter for Grouped Values

In this case, we want to be able to pass a sequence of values (delimited somehow) as a single argument, and have them interpreted as multiple individual values.

We can again take advantage of the property setter hack, but we have to do our own parsing of the delimited value. We will use a property type of **string** to prevent automatic parsing.

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    public Options()
    {
      Numbers = new List<int>();
    }

    public List<int> Numbers { get; private set; }

    [Option("number", 'n')]
    public string NumberOption
    {
      set
      {
        // Note: this example uses poor error handling!
        //  We *should* use TryParse and throw OptionParsingException.
        Numbers.AddRange(value.Split(';').Select(x => int.Parse(x)));
      }
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine(string.Join(", ", options.Numbers));

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

> CommandLineParsingTest.exe -n 3 -n 6
3, 6

> CommandLineParsingTest.exe -n 3;6
3, 6

This works, but still feels a bit "hackish". We're out of time for today, but in [a few weeks]({% post_url 2011-08-11-option-parsing-argument-parsing %}) we'll revisit this problem when we talk about _custom argument parsers_.

