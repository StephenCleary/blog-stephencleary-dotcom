---
layout: post
title: "XmlDocs: Linking to the Overloads Page"
---
XML documentation has a natural "link to code" element: the [<see> tag](http://msdn.microsoft.com/en-us/library/acd0tfbe.aspx). When a function is overloaded, the resulting help file contains an "overloads" page [like this](http://msdn.microsoft.com/en-us/library/system.text.encoding.getstring.aspx), but getting the **see** element to link to the overloads page is not exactly straightforward.

The <see> tag is one of the tags that is verified by the compiler, so it's not possible to just stick anything in there. The **see.cref** attribute must be a resolvable code element. The compiler doesn't allow you to resolve to a method group; it wants a single, unambiguous member reference.

> **Example warning/error message when attempting to link to an overload group:**  
> 
> Warning as Error: Ambiguous reference in cref attribute: 'FindFiles'. Assuming 'Nito.KitchenSink.WinInet.FtpHandle.FindFiles(string, Nito.KitchenSink.WinInet.FtpHandle.FindFilesFlags)', but could have also matched other overloads including 'Nito.KitchenSink.WinInet.FtpHandle.FindFiles()'.

Here's a little-known fact about the <see> tag: it will _not_ verify any **see.cref** values that start with a single character followed by a colon. This enables specifying full DocumentationId links such as "T:Nito.Async.ActionDispatcher".

There is a standard extension of the DocumentationId format for overloads that is understood by Sandcastle: it uses the "Overload:" prefix as such: "Overload:System.Windows.Threading.Dispatcher.Invoke". Unfortunately, Visual Studio (as of 2008) will attempt to resolve a link like this, and will fail.

The workaround is to use the "O:" prefix for such links (this prefix is unused by the DocumentationId format), and modify the XML documentation file before it is passed to Sandcastle. The "O:" prefix bypasses Visual Studio's verification, and the "Overload:" prefix is correctly understood by Sandcastle.

In my projects, I use the following XSLT transformation to automatically translate **see.cref** references starting with "O:" to have a prefix of "Overload:" instead:

<?xml version='1.0'?>
<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0'>
 <xsl:output method="xml" indent="yes"/>

   <!-- Copy all documentation as-is except for what matches other rules -->
   <xsl:template match="@* | node()">
    <xsl:copy>
     <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
   </xsl:template>

   <!-- Convert "cref" references that start with "O:" to starting with "Overload:". -->
   <xsl:template match="@cref[starts-with(., 'O:')]">
    <xsl:attribute name="cref">
     <xsl:value-of select="concat('Overload:', substring-after(., 'O:'))"/>
    </xsl:attribute>
   </xsl:template>
</xsl:stylesheet>

By the way, it's not difficult to include an XSLT transformation as part of an MSBuild project file (with the [MSBuild Extension Pack](http://msbuildextensionpack.codeplex.com/)). It's beyond the scope of this blog post, but you can check out the [Nito.Async main project build file](http://nitoasync.codeplex.com/SourceControl/changeset/view/40861#324550) for an example.

