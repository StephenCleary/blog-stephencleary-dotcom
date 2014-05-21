---
layout: post
title: "Option Parsing: Lexing"
series: "Option Parsing"
seriesTitle: "Lexing"
---
The first step in parsing a command line is _lexing_, which converts a single string (the command line) into a sequence of strings (individual options and/or arguments). Actually, the _very_ first step takes place before the program even runs: the command shell has its own simple lexer.

## Command Shell Escaping and Quoting

> The information in this section is derived from the TechNet articles [Command shell overview](http://technet.microsoft.com/en-us/library/bb490954.aspx) ([webcite](http://www.webcitation.org/5ytzcAcrB)) and [The Windows NT Command Shell](http://technet.microsoft.com/en-us/library/cc723564.aspx) ([webcite](http://www.webcitation.org/5ytzuqd4h)).

The command shell has these special characters: **&**, **|**, **(**, **)**, **<**, **>**, and **^**. There are two ways to pass these special characters on the command line: _escaping_ and _quoting_.

The **^** character is the shell escape character. You may prefix any of the special shell characters with that escape character, and the special shell character will be passed to the program (without the escape character).

The command shell also supports quoting; special characters may be passed within a pair of double-quotes. In this case, the special characters are passed to the program along with the surrounding quotes.

The shell escaping and quoting appears to be a simple algorithm: escaped characters (including normal characters) are passed through directly, and each (non-escaped) double-quote either starts or ends a quoted string. Consider the outputs from this example program:

static void Main(string[] args)
{
  Console.WriteLine(Environment.CommandLine);
}

> CommandLineParsingTest.exe ^^ "^"
CommandLineParsingTest.exe ^ "^"

> CommandLineParsingTest.exe ^"^"
CommandLineParsingTest.exe ""

> CommandLineParsingTest.exe ^""
CommandLineParsingTest.exe ""

> CommandLineParsingTest.exe "^"^"
CommandLineParsingTest.exe "^""

> CommandLineParsingTest.exe "^"^^"
CommandLineParsingTest.exe "^"^"

> CommandLineParsingTest.exe "^^
CommandLineParsingTest.exe "^^

Shell escaping and quoting are applied to every process by the Command Shell; there is no way to opt out of this behavior. After the command shell does its own escaping and quoting, the command line is passed to the program.

## Default .NET Lexing

The command line is split up into a list of process arguments by the .NET runtime. The algorithm is described in the documentation for [Environment.GetCommandLineArgs](http://msdn.microsoft.com/en-us/library/system.environment.getcommandlineargs.aspx). The same results (except for the process name) are also passed as the single argument to the **Main** method, if present.

The .NET lexing also uses a combination of escaping and quoting, but it has some surprising results because escaping is allowed inside quoting. The escape character is **\**, and the quote character is the double-quote.

Each non-escaped double-quote starts or ends a quoted string, just like command shell quoting. However, unlike command shell quoting, escaping is allowed within quoted strings. The .NET lexing also allows two consecutive double-quotes inside a quoted string to represent a single double-quote. Consider the outputs from this example program:

static void Main(string[] args)
{
  foreach (var arg in args)
    Console.WriteLine(arg);
}

> CommandLineParsingTest.exe "a"
a

> CommandLineParsingTest.exe \"a"
"a

> CommandLineParsingTest.exe \"a
"a

> CommandLineParsingTest.exe "a\"
a"

> CommandLineParsingTest.exe "a\\"
a\

> CommandLineParsingTest.exe a \"
a
"

> CommandLineParsingTest.exe "a \\"
a \

> CommandLineParsingTest.exe "a\"b"
a"b

> CommandLineParsingTest.exe "a""b"
a"b

> CommandLineParsingTest.exe a "" """"
a

"

This lexing behavior is particularly problematic when passing directories. Since directories may contain spaces, they should be wrapped with quotes. However, if the directory ends with a backslash, the closing quote will be escaped:

> CommandLineParsingTest.exe "c:\my path"
c:\my path

> CommandLineParsingTest.exe "c:\my path\"
c:\my path"

This is a rather serious limitation of the default .NET lexer. It is possible to write your own replacement lexer using a different algorithm. This lexer would take the process command line as input and produce a sequence of strings.

The [Nito.KitchenSink.OptionParser](http://nuget.org/List/Packages/Nito.KitchenSink.OptionParsing) library does not have a lexer of its own, but it will accept a sequence of strings as input into its parsing methods. If no sequence of strings is passed to a parsing method, then the method will use the process' command line lexed with the default .NET lexer.

