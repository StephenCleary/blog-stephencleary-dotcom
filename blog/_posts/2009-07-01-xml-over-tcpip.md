---
layout: post
title: "XML over TCP/IP"
---
(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

XML is a popular choice when designing communications protocols, since XML parsers are ubiquitous. The phrase "XML over TCP" makes a good executive summary, but this FAQ entry is concerned with how to actually make it work. One should always write an application protocol specification document to clearly define the actual communication.

Anyone designing an XML protocol should have a good understanding of the terms. Familiarization with the [XML standard](http://www.w3.org/TR/2008/REC-xml-20081126/) is a bonus; this FAQ entry will define most terms along the way, but the official definitions are in the relevant standards.

When used as a communications protocol, XML does not usually use processing instructions or entities (except for the normal "escaping entities" such as &lt;, etc.). It also does not normally use XSDs or DTDs; XML used for communication is well-formed but not valid (see the XML standard for the definitions of these terms).

This FAQ entry is laid out in a sequence of steps, but they do not necessarily have to be done in this sequence; in fact, the last step is often done first. However, they each _must_ be addressed in an application protocol specification, so they should be viewed more as a "checklist" than a "timeline". This advice should be considered additional to the general advice given in [Application Protocol Specifications]({% post_url 2009-06-30-application-protocol-specifications %}).

## Step 1: Encoding

XML documents are just a sequence of Unicode characters. However, TCP/IP works with streams of bytes. A translation must be made between Unicode characters and byte sequences, and this translation is called the _encoding_.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**Encoding:** The translation used when converting a Unicode string to or from a byte sequence. The most common encodings are UTF-16 (which uses 2 bytes for most characters; it is recognizable because every other byte is "00" for normal English text) and UTF-8 (which uses 1 byte for normal English characters, but most characters take 2 or more bytes).
</div>

The encoding may be detected one of several ways (if it is not specified in the application protocol document). A byte order mark may be used to detect the encoding in some situations.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**Byte Order Mark (BOM):** A special Unicode character that is sometimes inserted at the beginning of a document as it is encoded. The UTF-16 encoding has little-endian and big-endian versions, so it requires a BOM. UTF-8, however, does not require a BOM.
</div>

The XML file itself may include a prolog, which may specify the encoding being used. This is more difficult to work with, since the prolog itself must be interpreted heuristically by guessing at the encoding. Note that if an XML prolog exists that specifies an incorrect encoding, Microsoft's XML parsers may get confused (this often happens when reading XML from a string or writing XML to a string).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**XML Prolog:** The "<?xml ... ?>" element at the beginning of some XML files, specifying the XML version and (optionally) the encoding used.
</div>

Normally, the application protocol document specifies the encoding; this is simpler than dealing with automatically detecting the encoding. If it does specify the encoding, it should also specify whether a BOM should be present, or if a prolog is allowed to specify the encoding. A common choice is "UTF-8 without BOM or prolog", which makes the encoding always UTF-8 without a byte order mark or XML prolog.

## Working with the System.Text.Encoding class

The encoding for an output may be specified by using the XmlWriterSettings class. This is the preferred way to create XmlWriters. Encodings for inputs may also be forced; this is done if the remote side sends incorrect XML prologs, causing problems with XmlReaders.

Always translate XML to and from byte arrays, not strings. The System.String class actually uses an encoding itself (UTF-16). Using strings as an intermediary between byte arrays and XML classes may cause problems because of this additional encoding; encoding the string to and from a byte array will not update the XML prolog. An encoding may be given to an XmlWriterSettings class, but as the [documentation for XmlWriterSettings.Encoding](http://msdn.microsoft.com/en-us/library/system.xml.xmlwritersettings.encoding.aspx) states, any encoding on a stream will override that setting. For this reason, it is better to use XmlWriters with a MemoryStream, which does not have an encoding.

Each instance of an Encoding object must make another choice: whether to throw an exception on invalid input bytes or silently replace them (with the Unicode replacement character U+FFFD, which looks like '?' in a diamond). The default is to silently replace, but this is not generally recommended when implementing application protocols. The example code in this FAQ entry will always use encodings that throw exceptions.

The following sample code takes an XElement and an Encoding, and translates the XML to a byte array (not including an XML prolog):

{% highlight csharp %}
byte[] ConvertXmlToByteArray(XElement xml, Encoding encoding)
{
    using (MemoryStream stream = new MemoryStream())
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        // Add formatting and other writer options here if desired
        settings.Encoding = encoding;
        settings.OmitXmlDeclaration = true; // No prolog
        using (XmlWriter writer = XmlWriter.Create(stream, settings))
        {
            xml.Save(writer);
        }
        return stream.ToArray();
    }
}
{% endhighlight %}

When reading XML from a byte array, one may either use an XmlReader or a StreamReader. Using an XmlReader allows incoming XML to use any encoding; however, it will get confused if the XML has an incorrect encoding specified in its prolog. Using a StreamReader will force the byte array to be interpreted according to a particular encoding, but does not allow any other encodings.

If the application protocol specification does not specify an encoding, then the XmlReader must be used. Incorrect XML prologs cannot be allowed. The following sample code allows any encoding:

{% highlight csharp %}
XElement ConvertByteArrayToXml(byte[] data)
{
    // Interpret the byte array allowing any encoding
    XmlReaderSettings settings = new XmlReaderSettings();
    // Add validation and other reader options here if desired
    using (MemoryStream stream = new MemoryStream(data))
    using (XmlReader reader = XmlReader.Create(stream, settings))
    {
        return XElement.Load(reader);
    }
}
{% endhighlight %}

If the application protocol specification does specify a particular encoding, then using a StreamReader will allow incorrect XML prologs. The following sample code forces a specific encoding:

{% highlight csharp %}
XElement ConvertByteArrayToXml(byte[] data, Encoding encoding)
{
    // Interpret the byte array according to a specific encoding
    using (MemoryStream stream = new MemoryStream(data))
    using (StreamReader reader = new StreamReader(stream, encoding, false))
    {
        return XElement.Load(reader);
    }
}
{% endhighlight %}

The default [Encoding.Unicode](http://msdn.microsoft.com/en-us/library/system.text.encoding.unicode.aspx) (UTF-16 little endian), [Encoding.BigEndianUnicode](http://msdn.microsoft.com/en-us/library/system.text.encoding.bigendianunicode.aspx) (UTF-16 big endian), and [Encoding.UTF8](http://msdn.microsoft.com/en-us/library/system.text.encoding.utf8.aspx) instances always write out BOMs and never throw on errors. A better choice is to create instances of [UnicodeEncoding](http://msdn.microsoft.com/en-us/library/system.text.unicodeencoding.aspx) (UTF-16) or [UTF8Encoding](http://msdn.microsoft.com/en-us/library/system.text.utf8encoding.aspx) that are more suitable for application protocols. For example, "new UTF8Encoding(false, true)" will create a UTF-8 encoding without BOM that throws on invalid characters.

## Step 2: Message Framing

[Message framing]({% post_url 2009-04-30-message-framing %}) is highly recommended for XML messages. Technically, message framing is not strictly necessary, but reading XML from a socket without message framing is extremely difficult (the message must be considered complete when the root node is closed).

When sending, message framing is actually applied after the encoding, so the message framing wraps a byte array. Similarly, when receiving, message framing is applied directly to the received data, and each message (consisting of a byte array) is then passed through the encoding to produce XML.

## Step 3: Keepalives

[Keepalive messages]({% post_url 2009-05-16-detection-of-half-open-dropped %}) are usually necessary. Having a keepalive message defined in the application protocol specification often removes the need for separate timers when implementing the protocol.

XML keepalive messages (e.g., "<keepalive/>") are not normally used. Usually, a keepalive message may be sent by using the message framing to send an empty (zero-length) message.

## Step 4: Messages

Once the encoding, message framing, and keepalive options have all been chosen, then there is a framework over which XML messages may be exchanged. For each XML message, the following information should be included:

- When the message is meaningful (e.g., a "Response" should only be sent in response to a "Request").
- Which attributes and elements are required and which are optional. This includes complex relations (e.g., a "Log" element must contain at least one "Message" element and exactly one "Source" element). Be sure to use terms with specific definitions ("at least one", "exactly one", etc).
- The format of non-string data such as dates, booleans, and integers. Often, this type of "formatting data" is defined globally near the top of the application protocol specification, and applies to each possible message type.

Many protocols are based on a request/response or subscription/event model. One thing to watch out for is if the protocol elements begin looking like generic or object-oriented function calls, as though one side is accessing a remote object (e.g., "<CallMethod ObjectID='37' MethodName='GetData'/>"). At this point, the protocol will devolve into something eventually looking like SOAP. There's nothing wrong with SOAP, but there's no need to re-invent the wheel. If that level of abstraction is truly necessary, then just use SOAP instead of creating a separate protocol.

## Recommendations

A choice must be made for encoding, message framing, and keepalives.

 - The encoding is usually "any" or "UTF-8 without BOM or prolog", but could be anything.
 - A common choice for message framing is "4-byte (un)signed little-endian integer length prefix". Delimeter-based message framing may also be used for XML by appointing illegal XML characters to be the delimiters, and scanning for them during decoding (however, this is very difficult to do if the encoding is "any").
 - Since most XML protocols use length prefixing, most of them also choose "length prefix of 0 with no message" for keepalives.

One final note: nothing helps track down interfacing errors like verbose logging. It's recommended to log the byte arrays sent and received as well as the actual XML messages. Logging is your friend. :)

(This post is part of the [TCP/IP .NET Sockets FAQ]({% post_url 2009-04-30-tcpip-net-sockets-faq %}))

