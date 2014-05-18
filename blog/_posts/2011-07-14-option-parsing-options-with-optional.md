---
layout: post
title: "Option Parsing: Options with Optional Arguments"
tags: ["Option Parsing", ".NET", "Nito.KitchenSink"]
---


All of the examples so far have illustrated _options with required arguments_; that is, if the option is passed, it must be followed by an argument. It's also possible to define an option that takes an optional argument:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("with-extreme-prejudice", 'p', OptionArgument.Optional)]
    public int? PrejudiceLevel { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      if (options.PrejudiceLevel.HasValue)
        Console.WriteLine("Extreme Prejudice specified: " + options.PrejudiceLevel.Value);
      else
        Console.WriteLine("Regular prejudice will do.");

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
Regular prejudice will do.

> CommandLineParsingTest.exe -p 3
Extreme Prejudice specified: 3

> CommandLineParsingTest.exe -p
Regular prejudice will do.




The last example above illustrates the problem with options that take optional arguments: there isn't an easy way to determine whether the option _was passed without an argument_ or the option _was not passed at all_. In both of these cases, the property is left at the default value (**null** in this case).





The solution is to use the **OptionPresent** attribute, as such:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("with-extreme-prejudice", 'p', OptionArgument.Optional)]
    public int? PrejudiceLevel { get; set; }

    [OptionPresent('p')]
    public bool PrejudiceLevelWasSpecified { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      if (!options.PrejudiceLevelWasSpecified)
        Console.WriteLine("Regular prejudice will do.");
      else if (options.PrejudiceLevel.HasValue)
        Console.WriteLine("Extreme Prejudice specified: " + options.PrejudiceLevel.Value);
      else
        Console.WriteLine("Extreme Prejudice specified.");

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
Regular prejudice will do.

> CommandLineParsingTest.exe -p 3
Extreme Prejudice specified: 3

> CommandLineParsingTest.exe -p
Extreme Prejudice specified.




It is now possible to distinguish all possibilities. The **OptionPresent** example above uses the short option name, but this attribute also works with long names.

