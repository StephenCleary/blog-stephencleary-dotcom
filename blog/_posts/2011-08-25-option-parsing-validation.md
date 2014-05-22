---
layout: post
title: "Option Parsing: Validation"
series: "Option Parsing"
seriesTitle: "Validation"
---
The [option parsing pipeline]({% post_url 2011-06-09-option-parsing-option-parsing-pipeline %}) consists of three steps: lexing, parsing, and validation. So far, we've only talked about the first two steps; today we'll look at validation.

Option argument classes must derive from **IOptionArguments**, which only has one method:

{% highlight csharp %}

/// <summary>
/// An arguments class, which uses option attributes on its properties.
/// </summary>
public interface IOptionArguments
{
  /// <summary>
  /// Validates the arguments by throwing <see cref="OptionParsingException"/> errors as necessary.
  /// </summary>
  void Validate();
}
{% endhighlight %}

The **Validate** method should do any validation, and throw an exception if the option argument class properties are not acceptable. The **OptionArgumentsBase** type includes an implementation of **Validate** that just does some basic validation (we'll cover it in detail next week). This method may be overridden in derived classes.

## Validating Option Values

It's possible to include any logic you need in the **Validate** method. This example forces an option value to be in the range [0, 3]:

{% highlight csharp %}

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("level", 'l')]
    public int Level { get; set; }

    public override void Validate()
    {
      base.Validate();
      if (this.Level < 0 || this.Level > 3)
        throw new OptionParsingException.OptionArgumentException("Level must be in the range [0, 3].");
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
    Level: 0
    
    > CommandLineParsingTest.exe -l 3
    Level: 3
    
    > CommandLineParsingTest.exe -l 4
    Level must be in the range [0, 3].

Other option parsing libraries do validation using various attributes (e.g., the example above would use a [RangeAttribute]). However, using a **Validate** method is both simpler and more powerful.

## Required Options

It's possible to use validation to _require_ an option.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-3x pull-left"></i>

**Please note:** The technique described here is controversial! In general, people who have designed many command-line interfaces do not recommend _required options_ (at the very least, the terminology is confusing: it's a required optional parameter). Usually, a required option is better represented as a positional argument or a subcommand (both of which will be covered in later blog posts). Consider carefully before using required options.
</div>

The example below requires a level to be specified:

{% highlight csharp %}

class Program
{
  private sealed class Options : OptionArgumentsBase
  {
    [Option("level", 'l')]
    public int? Level { get; set; }

    public override void Validate()
    {
      base.Validate();
      if (this.Level == null)
        throw new OptionParsingException.OptionArgumentException("Level must be specified.");
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
    Level must be specified.
    
    > CommandLineParsingTest.exe -l 4
    Level: 4
    
    > CommandLineParsingTest.exe -l 0
    Level: 0

To reiterate, people with much more experience than I recommend against using "required options". They recommend positional arguments or subcommands instead.

