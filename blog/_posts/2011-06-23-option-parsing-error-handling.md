---
layout: post
title: "Option Parsing: Error Handling"
series: "Option Parsing"
seriesTitle: "Error Handling"
---
The [Nito.KitchenSink Option Parsing Library](http://www.nuget.org/List/Packages/Nito.KitchenSink.OptionParsing) wraps all option parsing errors into an exception derived from **Nito.KitchenSink.OptionParsing.OptionParsingException**. There are three more specific exception types (**InvalidParameterException**, **OptionArgumentException**, and **UnknownOptionException**), but they are seldomly needed.

All steps of the option parsing pipeline should only throw exceptions derived from **OptionParsingException**. In particular, this is true for custom validation (which will be described in detail in a future post).

The following example program shows how option parsing errors should be handled in a console application:

{% highlight csharp %}

using System;
using Nito.KitchenSink.OptionParsing;

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("level", 'l')]
    public int Level { get; set; }

    public static int Usage()
    {
      // Standard console size:
      //                      [                                                                                ]
      Console.Error.WriteLine("Usage: CommandLineOptionTest <arguments>");
      Console.Error.WriteLine("  -l, --level=LEVEL        level at which to operate");
      return 2;
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();
      
      // Program logic
      Console.WriteLine(options.Level);

      return 0;
    }
    catch (OptionParsingException ex)
    {
      Console.Error.WriteLine(ex.Message);
      return Options.Usage();
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine(ex);
      return 1;
    }
  }
}
{% endhighlight %}

First, the **Options** class is declared, which defines the options our program takes. It also exposes a **Usage** method, which displays command-line usage information. **Usage** writes its information to **Console.Error** and returns an error code.

> The Nito.KitchenSink.OptionParsing library does not attempt to write the **Usage** method for you automatically. Other option parsing libraries have attempted this, but the results are (IMHO) less than ideal.

The program's **Main** method returns an **int**, and contains a top-level try/catch. The try block parses the options, performs its requested task (in this case, the program just writes the Level option to the console), and then returns 0 (meaning "success").

If there is an option parsing exception, then the exception message is written to **Console.Error**, usage information is displayed, and an error code is returned.

If there is some other (unexpected) exception (during option parsing or program logic), then the entire exception (including the call stack) is written to **Console.Error** and an error code is returned.

## Notes

For console programs, a return value of 0 indicates success and any other return value usually indicates an error. I used two different error codes in the example above, but they could just as easily be a single error code because distinguishing usage errors is not normally useful.

An options class does not have to include a **Usage** method; I just usually put it there so it's along with the class that defines the options. In future blog posts, I'll post example code that skips the **Usage** method to avoid distractions, but it should be included in real-world code.

