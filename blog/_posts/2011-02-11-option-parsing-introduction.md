---
layout: post
title: "Option Parsing: Introduction"
series: "Option Parsing"
seriesTitle: "Introduction"
---
Last week, [Nito.KitchenSink.OptionParsing](http://nuget.org/List/Packages/Nito.KitchenSink.OptionParsing) was released on NuGet. This is a command-line option parsing library that I've used for years. Since my day job currently consists of re-architecting firmware, I figured I'd write a few posts on the Nito.KitchenSink NuGet (mini-)libraries.

First, here's a little sample program to show how the option parsing library is used:

{% highlight csharp %}

using System;
using System.IO;
using System.Reflection;

using Nito.KitchenSink.OptionParsing;

class Program
{
    // Define the options
    private sealed class MyOptions : OptionArgumentsBase
    {
        [Option("level", 'l')]
        public int? Level { get; set; }

        public static int Usage()
        {
            Console.Error.WriteLine("Usage: myprog [OPTIONS]...");
            Console.Error.WriteLine("  -l, --level=LEVEL   Sets the level at which to operate.");
            return -1;
        }
    }

    static int Main()
    {
        try
        {
            // Parse the options.
            var options = OptionParser.Parse<MyOptions>();

            // Do the requested operation.
            if (options.Level != null)
                Console.WriteLine("Level: " + options.Level);
            return 0;
        }
        catch (OptionParsingException ex)
        {
            // Handle usage errors.
            Console.Error.WriteLine(ex.Message);
            return MyOptions.Usage();
        }
        catch (Exception ex)
        {
            // Handle operation errors.
            Console.Error.WriteLine(ex);
            return -1;
        }
    }
}
{% endhighlight %}

The sample program above only takes a single option: a "level". First, I define the option in the **MyOptions** class, along with a static **Usage** to display command-line usage.

The actual program just parses its command-line options and then displays the level if it was specified. The error handling code distinguishes between usage errors and operating errors (all option parsing errors derive from **OptionParsingException**).

Even though the sample program only includes a single option, a lot of variety is allowed by the option parsing library:

    > myprog
    
    > myprog -l 3
    Level: 3
    
    > myprog --level 3
    Level: 3
    
    > myprog /level 3
    Level: 3
    
    > myprog /l 3
    Level: 3

By default, the Nito.KitchenSink.OptionParsing library allows short options (with a single dash), long options (with a double dash), and short _or_ long options (with a forward slash).

In addition, the option argument can be separated by whitespace (as in the examples above), a full colon, or an equals sign:

    > myprog /l:3
    Level: 3
    
    > myprog /level=3
    Level: 3
    
    > myprog -l=3
    Level: 3

The Nito.KitchenSink.OptionParsing library also handles common errors, and tries to give meaningful error messages:

    > myprog wha?
    Unknown parameter  wha?
    Usage: myprog [OPTIONS]...
      -l, --level=LEVEL   Sets the level at which to operate.
    
    > myprog -bad
    Unknown option  b  in parameter  -bad
    Usage: myprog [OPTIONS]...
      -l, --level=LEVEL   Sets the level at which to operate.
    
    > myprog /bad
    Unknown option  bad  in parameter  /bad
    Usage: myprog [OPTIONS]...
      -l, --level=LEVEL   Sets the level at which to operate.
    
    > myprog -l
    Missing argument for option  level
    Usage: myprog [OPTIONS]...
      -l, --level=LEVEL   Sets the level at which to operate.
    
    > myprog -l null
    Could not parse  null  as Int32
    Usage: myprog [OPTIONS]...
      -l, --level=LEVEL   Sets the level at which to operate.

Option parsing is case sensitive by default:

    > myprog /Level:3
    Unknown option  Level  in parameter  /Level:3
    Usage: myprog [OPTIONS]...
      -l, --level=LEVEL   Sets the level at which to operate.

There's actually a lot of work being done in the single-line **OptionParser.Parse<MyOptions>()**! And this post is just scratching the surface; the Nito.KitchenSink.OptionParsing library is all about flexibility and extensibility.

