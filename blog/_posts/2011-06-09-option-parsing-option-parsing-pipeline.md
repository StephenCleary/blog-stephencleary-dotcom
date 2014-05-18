---
layout: post
title: "Option Parsing: The Option Parsing Pipeline"
tags: ["Option Parsing", ".NET", "Nito.KitchenSink"]
---


There are three main phases during option parsing:



1. Lexing
1. Parsing
1. Validation




The **Lexing** phase deals with the escaping and quoting of special characters and splitting the command line string into a sequence of strings. The **Parsing** phase evaluates the sequence of strings from the lexing phase, and interprets them as options and arguments; this includes parsing arguments as necessary, e.g., converting a string argument _"3"_ into the numeric argument value _3_. The **Validation** phase determines if the options and arguments represent a valid command for the program to perform.





The [Nito.KitchenSink.OptionParser](http://nuget.org/List/Packages/Nito.KitchenSink.OptionParsing) library does not have a _lexer_, but does have a _parser_ and hooks for _validation_. The easiest way to use the library is by calling a single method:




var options = OptionParser.Parse<MyOptionArguments>();




This single method wraps all the phases of the option parsing pipeline:




 1. The command line for the process is lexed using the default .NET lexing.
 1. The option and argument definitions are inferred from properties and attributes on the **MyOptionArguments** type.
 1. These definitions are used to parse the lexed command line, saving the results into properties on a default-constructed **MyOptionsArguments** object.
 1. Validation is performed on the **MyOptionsArguments** object, which is then returned.




Future posts will show how each of these steps may be configured (or replaced).

