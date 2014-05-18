---
layout: post
title: "Option Parsing: Case Sensitivity"
tags: ["Option Parsing", ".NET", "Nito.KitchenSink"]
---


By default, all option parsing is case-sensitive:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("name", 'n')]
    public string Name { get; private set; }
  }

  static int Main(string[] args)
  {
    try
    {
      var options = OptionParser.Parse<Options>();
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



> CommandLineParsingTest.exe /name Bob
Name: Bob

> CommandLineParsingTest.exe /Name Bob
Unknown option  Name  in parameter  /Name




This is normal for Unix users, but Windows users expect case-insensitivity. You can pass your own **StringComparer** to the **Parse** method to support case-insensitivity:




class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("name", 'n')]
    public string Name { get; private set; }
  }

  static int Main(string[] args)
  {
    try
    {
      var options = OptionParser.Parse<Options>(stringComparer:StringComparer.CurrentCultureIgnoreCase);
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



> CommandLineParsingTest.exe /name Bob
Name: Bob

> CommandLineParsingTest.exe /Name Bob
Name: Bob
