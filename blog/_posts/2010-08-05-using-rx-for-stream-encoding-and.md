---
layout: post
title: "Using Rx for Stream Encoding and Decoding"
tags: ["Rx", ".NET"]
---


Still on my Rx kick...





The Rx team had a two-part series last month demonstrating one way to use Rx on the server: [asynchronous Stream](http://blogs.msdn.com/b/jeffva/archive/2010/07/23/rx-on-the-server-part-1-of-n-asynchronous-system-io-stream-reading.aspx) and [asynchronous StreamReader](http://blogs.msdn.com/b/jeffva/archive/2010/07/26/rx-on-the-server-part-2-of-n-asynchronous-streamreader.aspx). In the asynchronous StreamReader example, they punt on the decoding issue, instead demonstrating how to split the text into lines.





As it turns out, decoding (and encoding) are quite simple in Rx. The code below should be clear to anyone who's been following the Rx team blog posts:




using System;
using System.Linq;
using System.Text;

/// <summary>
/// Observable extension methods that encode and decode streams.
/// </summary>
public static class EncodingObservables
{
  /// <summary>
  /// Takes a "chunked" sequence of characters and converts it to a "chunked" sequence of bytes using the specified encoding.
  /// </summary>
  /// <param name="source">The "chunked" sequence of characters.</param>
  /// <param name="encoding">The encoding used to translate the sequence of characters to a sequence of bytes.</param>
  /// <returns>The "chunked" sequence of bytes.</returns>
  public static IObservable<byte[]> Encode(this IObservable<char[]> source, Encoding encoding)
  {
    return Observable.CreateWithDisposable<byte[]>(observer =>
    {
      var encoder = encoding.GetEncoder();

      return source.Subscribe(
        data =>
        {
          try
          {
            var ret = new byte[encoder.GetByteCount(data, 0, data.Length, false)];
            encoder.GetBytes(data, 0, data.Length, ret, 0, false);
            if (ret.Length != 0)
            {
              observer.OnNext(ret);
            }
          }
          catch (EncoderFallbackException ex)
          {
            observer.OnError(ex);
          }
        },
        observer.OnError,
        () =>
        {
          try
          {
            var ret = new byte[encoder.GetByteCount(new char[0], 0, 0, true)];
            encoder.GetBytes(new char[0], 0, 0, ret, 0, true);
            if (ret.Length != 0)
            {
              observer.OnNext(ret);
            }

            observer.OnCompleted();
          }
          catch (EncoderFallbackException ex)
          {
            observer.OnError(ex);
          }
        });
    });
  }

  /// <summary>
  /// Takes a "chunked" sequence of bytes and converts it to a "chunked" sequence of characters using the specified encoding.
  /// </summary>
  /// <param name="source">The "chunked" sequence of bytes.</param>
  /// <param name="encoding">The encoding used to translate the sequence of bytes to a sequence of characters.</param>
  /// <returns>The "chunked" sequence of characters.</returns>
  public static IObservable<char[]> Decode(this IObservable<byte[]> source, Encoding encoding)
  {
    return Observable.CreateWithDisposable<char[]>(observer =>
    {
      var decoder = encoding.GetDecoder();

      return source.Subscribe(
        data =>
        {
          try
          {
            var ret = new char[decoder.GetCharCount(data, 0, data.Length, false)];
            decoder.GetChars(data, 0, data.Length, ret, 0, false);
            if (ret.Length != 0)
            {
              observer.OnNext(ret);
            }
          }
          catch (EncoderFallbackException ex)
          {
            observer.OnError(ex);
          }
        },
        observer.OnError,
        () =>
        {
          try
          {
            var ret = new char[decoder.GetCharCount(new byte[0], 0, 0, true)];
            decoder.GetChars(new byte[0], 0, 0, ret, 0, true);
            if (ret.Length != 0)
            {
              observer.OnNext(ret);
            }

            observer.OnCompleted();
          }
          catch (EncoderFallbackException ex)
          {
            observer.OnError(ex);
          }
        });
    });
  }
}




This class defines two operators (Encode and Decode) which can be used like this:




[TestClass]
public class EncodingObservablesUnitTests
{
  [TestMethod]
  public void MSDNEncoderSample()
  {
    var chars = new[]
    {
      new[] { '\u0023' }, // #
      new[] { '\u0025' }, // %
      new[] { '\u03a0' }, // Pi
      new[] { '\u03a3' } // Sigma
    };

    var result = chars.ToObservable(Scheduler.ThreadPool)
                      .Encode(Encoding.UTF7)
                      .ToEnumerable()
                      .SelectMany(x => x)
                      .ToArray();
    Assert.IsTrue(result.SequenceEqual(new byte[] { 43, 65, 67, 77, 65, 74, 81, 79, 103, 65, 54, 77, 45 }));
  }

  [TestMethod]
  public void MSDNEncoderGetBytesSample()
  {
    var chars = new[]
    {
      new[] { '\u0023' }, // #
      new[] { '\u0025' }, // %
      new[] { '\u03a0' }, // Pi
      new[] { '\u03a3' } // Sigma
    };

    var result = chars.ToObservable(Scheduler.ThreadPool)
                      .Encode(Encoding.Unicode)
                      .ToEnumerable()
                      .SelectMany(x => x)
                      .ToArray();

    Assert.IsTrue(result.SequenceEqual(new byte[] { 35, 0, 37, 0, 160, 3, 163, 3 }));
  }

  [TestMethod]
  public void MSDNDecoderSample()
  {
    var bytes = new[]
    {
      new byte[] { 0x20, 0x23, 0xe2 },
      new byte[] { 0x98, 0xa3 },
    };

    var result = bytes.ToObservable(Scheduler.ThreadPool)
                      .Decode(Encoding.UTF8)
                      .ToEnumerable()
                      .SelectMany(x => x)
                      .ToArray();

    Assert.IsTrue(result.SequenceEqual(new[] { '\u0020', '\u0023', '\u2623' }));
  }
}




Note that I've defined the Encode and Decode operators as working on "chunks" of data. As such, they don't really "fit in" with most LINQ and Rx operators, which work on individual data elements. However, this approach makes sense any time there's buffered reading going on. The Encode and Decode operators here will work fine with the Rx team's example AsyncRead operator.





Also note that these simple Encode and Decode operators will _not_ treat encoding preambles in any special way (including Unicode byte order marks). They won't prefix the encoded output with a preamble, nor will they detect any preambles when decoding.

