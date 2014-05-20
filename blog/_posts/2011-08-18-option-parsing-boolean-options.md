---
layout: post
title: "Option Parsing: Boolean Options"
series: "Option parsing"
seriesTitle: "Option Parsing: Boolean Options"
---
## Options as Flags

Most options require an option argument. Some options take [an optional argument]({% post_url 2011-07-14-option-parsing-options-with-optional %}). Then there are the options that take no argument at all. These are the "flag" options - the option value is either set or unset.



Options with no arguments may only be defined on boolean properties. Consider this program, which defines two options (**a** and **b**) that do not take arguments, and a third option (**c**) which takes a required argument:




class ProgramtO
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option('a', Argument = OptionArgument.None)]
    public bool A { get; set; }

    [Option('b', Argument = OptionArgument.None)]
    public bool B { get; set; }

    [Option('c')]
    public bool C { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("A: " + options.A);
      Console.WriteLine("B: " + options.B);
      Console.WriteLine("C: " + options.C);

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
A: False
B: False
C: False

> CommandLineParsingTest.exe -a -b
A: True
B: True
C: False

> CommandLineParsingTest.exe -c
Missing argument for option  c

> CommandLineParsingTest.exe -c true
A: False
B: False
C: True


## Short Option Runs

Arguments that do not take arguments may be combined on the command line into a "short option run." A short option run must use the short names of the options; it cannot use the long names.




> CommandLineParsingTest.exe -ab
A: True
B: True
C: False


There is no way to pass an argument to an option in a short option run.




> CommandLineParsingTest.exe -ac true
Option  c  cannot be in a short option run (because it takes an argument) in parameter  -ac

> CommandLineParsingTest.exe -ac=true
Invalid parameter  -ac=true


> This is a deliberate departure from the behavior of GNU's getopt. Short option runs with arguments are not readable and may cause compatibility problems when the options change.


## Inverse Aliases

Some programs prefer the ability to specify an "on" and an "off" version for the same option. This can be easily done by having the boolean properties share a single backing value, with the "off" version inverting its value. These are very similar to aliases, except that they mean the _opposite_ instead of the _same_.




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    public Options()
    {
      this.B = true;
    }

    [Option("a", Argument = OptionArgument.None)]
    public bool A { get; set; }

    [Option("no-a", Argument = OptionArgument.None)]
    public bool NoA
    {
      get { return !this.A; }
      set { this.A = !value; }
    }

    [Option("b", Argument = OptionArgument.None)]
    public bool B { get; set; }

    [Option("no-b", Argument = OptionArgument.None)]
    public bool NoB
    {
      get { return !this.B; }
      set { this.B = !value; }
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("A: " + options.A);
      Console.WriteLine("B: " + options.B);

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
A: False
B: True

> CommandLineParsingTest.exe /a
A: True
B: True

> CommandLineParsingTest.exe /b
A: False
B: True

> CommandLineParsingTest.exe /no-a
A: False
B: True

> CommandLineParsingTest.exe /no-b
A: False
B: False

> CommandLineParsingTest.exe /a /no-a
A: False
B: True


The last example shows that the default [overwrite behavior]({% post_url 2011-08-04-option-parsing-preventing-multiple %}) of options produces the expected result: when there are multiple conflicting options on a command line, the last one wins.



Note that the options in this sample do not have short names. They _are_ allowed to have short names, but options with inverse aliases do not usually have short names.

