---
layout: post
title: "Option Parsing: Preventing Multiple Argument Values"
series: "Option Parsing"
seriesTitle: "Preventing Multiple Argument Values"
---
When dealing with multiple argument values, there are four basic behaviors: _overwrite_, _append_, _prevent_, and _ignore_.

[Last week's post]({% post_url 2011-07-28-option-parsing-allowing-multiple %}) contained a few examples of the _append_ behavior, which is supported by having the property setter place the values into a backing list.

The default behavior in the [Nito.KitchenSink option parsing library](http://nuget.org/List/Packages/Nito.KitchenSink.OptionParsing) is to _overwrite_ previous values. In other words, options coming later on the command line may "override" options earlier on the command line. Consider this example:

{% highlight csharp %}

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("level", 'l')]
    public int? Level { get; set; }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("Level: " + options.Level);

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
{% endhighlight %}

    > CommandLineParsingTest.exe
    Level:
    
    > CommandLineParsingTest.exe -l 3
    Level: 3
    
    > CommandLineParsingTest.exe -l 3 -l 9
    Level: 9

This is the default behavior, and is probably what users expect. However, for some options, the _prevent_ or _ignore_ behaviors may make sense.

The _prevent_ and _ignore_ behaviors are closely related. Like last week's post, these behaviors are implemented by placing special code in the property setter.

The _prevent_ behavior can be implemented by having a nullable backing value, and throwing from the setter if it is already set. The only tricky part is choosing the exception to throw from the setter; I recommend throwing an exception derived from **OptionParsingException**, since that indicates a usage error. Any exception thrown from a property setter will be wrapped in an **OptionParsingException.OptionArgumentException** (in versions 1.1.2 and newer).

{% highlight csharp %}

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    private int? level;

    [Option("level", 'l')]
    public int? Level
    {
      get
      {
        return this.level;
      }

      set
      {
        if (this.level.HasValue)
          throw new OptionParsingException.OptionArgumentException("The value may only be specified once.");
        this.level = value;
      }
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("Level: " + options.Level);

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
{% endhighlight %}

    > CommandLineParsingTest.exe
    Level:
    
    > CommandLineParsingTest.exe -l 3
    Level: 3
    
    > CommandLineParsingTest.exe -l 3 -l 9
    The value may only be specified once.

Likewise, the _ignore_ behavior can be implemented by having a nullable backing value, and ignoring the setter if it is already set:

{% highlight csharp %}

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    private int? level;

    [Option("level", 'l')]
    public int? Level
    {
      get
      {
        return this.level;
      }

      set
      {
        if (!this.level.HasValue)
          this.level = value;
      }
    }
  }

  static int Main()
  {
    try
    {
      var options = OptionParser.Parse<Options>();

      Console.WriteLine("Level: " + options.Level);

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
{% endhighlight %}

    > CommandLineParsingTest.exe
    Level:
    
    > CommandLineParsingTest.exe -l 3
    Level: 3
    
    > CommandLineParsingTest.exe -l 3 -l 9
    Level: 3

Note that the _ignore_ behavior may confuse users; most command-line programs use _overwrite_ behavior, which is the default.

