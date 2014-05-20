---
layout: post
title: "Option Parsing: Default and Nullable Argument Values"
series: "Option parsing"
seriesTitle: "Option Parsing: Default and Nullable Argument Values"
---
Most of our examples so far have already dealt with options taking arguments, because most options in the real world _do_ take arguments. Today we'll start looking at option arguments in depth.



The [option pipeline]({% post_url 2011-06-09-option-parsing-option-parsing-pipeline %}) post laid out the steps taken when using an Option Arguments class:



1. The Option Arguments class is default-constructed.
1. Attributes of properties on the class are used to produce a collection of option definitions.
1. The command line is parsed, setting properties on the Option Arguments instance.


We'll take advantage of these steps to handle several common scenarios.



## Default Values

Default argument values are set in the default constructor:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    public Options()
    {
      // Set default values.
      Quality = 3;
    }

    [Option("level", 'l')]
    public int Level { get; set; }

    [Option("quality")]
    public int Quality { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("Level: " + options.Level);
      Console.WriteLine("Quality: " + options.Quality);

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
Level: 0
Quality: 3

> CommandLineParsingTest.exe /level 7
Level: 7
Quality: 3

> CommandLineParsingTest.exe /quality 4
Level: 0
Quality: 4


## Nullable Values

There are some situations where a "default value" doesn't make sense for an option; you need to know whether there was a value passed, and what the value is (if it was passed). In this situation, you can use a nullable value type for your property:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("level", 'l')]
    public int? Level { get; set; }

    [Option("name", 'n')]
    public string Name { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      if (options.Level.HasValue)
        Console.WriteLine("Level: " + options.Level.Value);
      else
        Console.WriteLine("Level not specified.");
      if (options.Name != null)
        Console.WriteLine("Name: " + options.Name);
      else
        Console.WriteLine("Name not specified.");

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
Level not specified.
Name not specified.

> CommandLineParsingTest.exe /level 3
Level: 3
Name not specified.

> CommandLineParsingTest.exe /level 0
Level: 0
Name not specified.

> CommandLineParsingTest.exe /name Bob
Level not specified.
Name: Bob

> CommandLineParsingTest.exe /name ""
Level not specified.
Name: 
