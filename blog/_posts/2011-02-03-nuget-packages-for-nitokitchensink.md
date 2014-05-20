---
layout: post
title: "NuGet Packages for Nito.KitchenSink"
---
Ever since NuGet was released, I've been working on changing the design of the [Nito.KitchenSink](http://nitokitchensink.codeplex.com/) library. As a CodePlex project, it's acted as a "catch-all" for reusable code that wasn't large enough for its own project. NuGet provides a simple way to handle many small packages.



I've been taking the more stable parts of Nito.KitchenSink, reviewing the design of each type, completing the XML documentation, and instrumenting them with Code Contracts. Tonight, the first twelve packages were published to the official NuGet feed. They all start with "Nito.KitchenSink".



The KitchenSink project will continue, but this is the first step of making it into a "library collection" rather than a single library. Eventually, the huge ILMerged binary will be replaced by many independent NuGet packages and a single, smaller binary.



<!--

<p>The Nito.KitchenSink packages published tonight are:</p>
<ul>
<li><b>BinaryData</b> - A <b>BinaryConverter</b> class which is easier to use than <a href="http://msdn.microsoft.com/en-us/library/system.bitconverter.aspx">BitConverter</a> for packed byte arrays, and extension methods for displaying binary byte arrays as a string.</li>
<li><b>CRC</b> - <b>CRC16</b> and <b>CRC32</b> classes (deriving from <a href="http://msdn.microsoft.com/en-us/library/system.security.cryptography.hashalgorithm.aspx">HashAlgorithm</a>) which can implement <i>any</i> CRC-16 or CRC-32 algorithm, and definitions for the common implementations.</li>
<li><b>Dynamic</b> - Classes for dynamically accessing static type members, using the approach <a href="http://blog.stephencleary.com/2010/04/dynamically-binding-to-static-class.html">described on this blog last year.</a></li>
<li><b>Exceptions</b> - Extension methods for exceptions: preserving stack traces when re-throwing, dumping to xml, and unwrapping <a href="http://msdn.microsoft.com/en-us/library/system.aggregateexception.aspx">AggregateExceptions</a>.</li>
<li><b>FileSystemPaths</b> - A specialized string wrapper that provides a more OO/fluent API for <a href="http://msdn.microsoft.com/en-us/library/system.io.path.aspx">System.IO.Path</a>.</li>
<li></li>
</ul>

-->