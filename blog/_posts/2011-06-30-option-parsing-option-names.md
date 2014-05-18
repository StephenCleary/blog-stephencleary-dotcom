---
layout: post
title: "Option Parsing: Option Names"
tags: ["Option Parsing", ".NET", "Nito.KitchenSink"]
---


An option may have a long name, a short name, or both. "Short names" are just single characters, while "long names" are strings. Option names may not contain the special characters **:** or **=**.





Commonly-used options should have both a long name and a short name. The short name enables faster typing on the command line, while the long name enables self-documenting command lines (for use in script and batch files). Normally, the short name is the first character of the long name, but this is not required.





Less-common options should have just a long name; this avoids polluting the short name namespace.




using System;
using Nito.KitchenSink.OptionParsing;

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("level", 'l')]
    public int Level { get; set; }

    [Option("priority")]
    public int Priority { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();
      
      Console.WriteLine("Level: " + options.Level);
      Console.WriteLine("Priority: " + options.Priority);

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
Priority: 0

> CommandLineParsingTest.exe /l 3
Level: 3
Priority: 0

> CommandLineParsingTest.exe /level 3
Level: 3
Priority: 0

> CommandLineParsingTest.exe /priority 1
Level: 0
Priority: 1

> CommandLineParsingTest.exe /p 1
Unknown option  p  in parameter  /p




Normally, options do not have just a short name without a long name, but you _can_ do it if you want do.



## Multiple Long and Short Names



Options may have "aliases" (multiple long and/or short names). The easiest way to add aliases is to have separate properties on your Option Arguments class that refer to the same underlying field.





The following example shows one alias that is used to change an old option "level" into a more descriptive option "frob-level", marking the old option as obsolete. Another alias "frobbing-level" is also added, which is just a regular alias (without any options marked obsolete).




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    // The old "level" option, now obsolete and made into an alias for "frob-level".
    [Option("level", 'l')]
    [Obsolete]
    public int Level
    {
      get { return FrobLevel; }
      set
      {
        Console.Error.WriteLine("Warning: The --level option is obsolete; use --frob-level instead.");
        FrobLevel = value;
      }
    }

    [Option("frob-level")]
    public int FrobLevel { get; set; }

    // Another alias for "frob-level"; this one is not obsolete.
    [Option("frobbing-level")]
    public int FrobbingLevel
    {
      get { return FrobLevel; }
      set { FrobLevel = value; }
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine(options.FrobLevel);

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



> CommandLineParsingTest.exe /level 4
Warning: The --level option is obsolete; use --frob-level instead.
4

> CommandLineParsingTest.exe /frob-level 4
4

> CommandLineParsingTest.exe /frobbing-level 6
6


## Abbreviated Option Names



Some programs support abbreviated option names; for example, the option "pack" may be abbreviated as "pa" or "p" (assuming there is no other option that starts with "pa" or "p", respectively). However, this causes backwards compatibility issues; for example, an updated version of the program may introduce an option named "push", and any scripts that used the abbreviated option "p" then become ambiguous. For this reason, [Nito.KitchenSink.OptionParsing](http://www.nuget.org/List/Packages/Nito.KitchenSink.OptionParsing) does not include automatic support for abbreviated option names. If you need abbreviated option names, you may use explicit aliases to achieve the same effect.




> CommandLineParsingTest.exe /frob 6
Unknown option  frob  in parameter  /frob
