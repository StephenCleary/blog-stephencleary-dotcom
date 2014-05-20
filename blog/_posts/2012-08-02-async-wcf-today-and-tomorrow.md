---
layout: post
title: "Async WCF Today and Tomorrow"
---
In the near future, all of my web services will be changed to REST APIs served via the Web API library. However, for now I do have some WCF services that are implemented using the Async CTP, and I thought it would be helpful to describe how it was done.



My current production services use the Task-based Asynchronous Pattern (TAP) on VS2010 with the Async CTP. You can do it the same way using VS2012 with the Async Targeting Pack for .NET 4.0. I'll also describe how WCF is becoming async-friendly in the near future with .NET 4.5.



To keep things simple, I'm just going to expose a "Calculator" service that has a single "Divide" method. If there is a DivideByZeroException, the Calculator service will raise a CalculatorFault.



## Today

Let's tackle the server first. If we want to create an asynchronous WCF service method, we have to set OperationContract.AsyncPattern to true and follow the Asynchronous Programming Model (APM) pattern:




[DataContract]
public class CalculatorFault
{
  [DataMember]
  public string Message { get; set; }
}

[ServiceContract]
public interface ICalculator
{
  // Synchronous equivalent:
  //  [OperationContract]
  //  [FaultContract(typeof(CalculatorFault))]
  //  uint Divide(uint numerator, uint denominator);

  [OperationContract(AsyncPattern = true)]
  [FaultContract(typeof(CalculatorFault))]
  IAsyncResult BeginDivide(uint numerator, uint denominator, AsyncCallback callback, object state);
  uint EndDivide(IAsyncResult asyncResult);
}


In WCF, the "asynchronicity" of the server is an implementation detail. If you look at the metadata that is published for ICalculator, it looks exactly like the synchronous equivalent; the ICalculator metadata just describes a single operation named "Divide".



If we're going to have an asynchronous server, we're going to want to use the Task-based Asynchronous Pattern (TAP) to write it. So here's our implementation, error handling and all:




public class Calculator : ICalculator
{
  public async Task<uint> DivideAsync(uint numerator, uint denominator)
  {
    try
    {
      var myTask = Task.Factory.StartNew(() => numerator / denominator);
      var result = await myTask;
      return result;
    }
    catch (DivideByZeroException)
    {
      throw new FaultException<CalculatorFault>(new CalculatorFault { Message = "Undefined result" });
    }
  }
}


> I'm using StartNew for example code; real code can use Task.Run (TaskEx.Run for Async CTP) if you want to run code on a background thread.


OK, so we've got our implementation (using TAP), and our interface (using APM). Now we have to wire them together by writing Begin/End wrapper methods around our TAP method:




public class Calculator : ICalculator
{
  public IAsyncResult BeginDivide(uint numerator, uint denominator, AsyncCallback callback, object state)
  {
    // See the Task-Based Asynchronous Pattern document for an explanation of the Begin/End implementations.
    var tcs = new TaskCompletionSource<uint>(state);
    var task = DivideAsync(numerator, denominator);
    task.ContinueWith(t =>
    {
      if (t.IsFaulted)
        tcs.TrySetException(t.Exception.InnerExceptions);
      else if (t.IsCanceled)
        tcs.TrySetCanceled();
      else
        tcs.TrySetResult(t.Result);

      if (callback != null)
        callback(tcs.Task);
    });

    return tcs.Task;
  }

  public uint EndDivide(IAsyncResult asyncResult)
  {
    try
    {
      return ((Task<uint>)asyncResult).Result;
    }
    catch (AggregateException ex)
    {
      // Note: the original stack trace is lost by this re-throw, but it doesn't really matter.
      throw ex.InnerException;
    }
  }
}


The wrappers are straightforward. The only tricky part is in the End wrapper where we re-throw a FaultException.



The wrappers are also tedious, especially if you have a lot of methods to wrap. My [AsyncEx library](http://nitoasyncex.codeplex.com/) includes AsyncFactory.ToBegin and AsyncFactory.ToEnd methods that handle the TAP-to-APM conversion cleanly. That's what I use:




public class Calculator : ICalculator
{
  public IAsyncResult BeginDivide(uint numerator, uint denominator, AsyncCallback callback, object state)
  {
    var task = DivideAsync(numerator, denominator);
    return AsyncFactory<uint>.ToBegin(task, callback, state);
  }

  public uint EndDivide(IAsyncResult asyncResult)
  {
    return AsyncFactory<uint>.ToEnd(asyncResult);
  }
}


At this point, we have a working server that is implemented asynchronously.



Now, let's turn our attention to the client. In WCF, either the service or the client can be either synchronous or asynchronous; they don't have to match. I usually want asynchronous clients, though - and if they're asynchronous, I want them to be TAP!



Fortunately, this is pretty easy. Create a client proxy _enabling asynchronous methods_ (under the "advanced" options). By default, the generated proxy supports APM and EAP (Event-based Asynchronous Programming), but not TAP.



There are two ways to add TAP support. You can add it manually by implementing a TAP wrapper method around the APM methods:




static class Program
{
  // Wrap those Begin/End methods back into a Task-based API.
  public static Task<uint> DivideAsyncTask(this CalculatorClient client, uint numerator, uint denominator)
  {
    return Task<uint>.Factory.FromAsync(client.BeginDivide, client.EndDivide, numerator, denominator, null);
  }
}


**Or,** you can build the sample project at "My Documents\Microsoft Visual Studio Async CTP\Samples\(C# WCF) Stock Quotes", copy the TaskWsdlImportExtension.dll into your solution, and modify your app.config to use it for building WCF proxies (as described in [this blog post](http://www.larswilhelmsen.com/2010/11/05/taskwsdlimportextensiona-hidden-gem-in-the-c-vnext-async-ctp-samples/)):




<configuration>
 <system.serviceModel>
  <client>
   <metadata>
    <wsdlImporters>
     <extension type="TaskWsdlImportExtension.TaskAsyncWsdlImportExtension, TaskWsdlImportExtension" />
    </wsdlImporters>
   </metadata>
  </client>
 </system.serviceModel>
</configuration>


This is more work to set up, but once it's done you don't have to write any TAP wrappers at all. TaskAsyncWsdlImportExtension does them for you. Unfortunately, this doesn't seem to be an option on VS2012 with the Async Targeting Pack.



> Side note: TaskWsdlImportExtension will generate a method called "DivideAsync", while our manual wrapper uses "DivideAsyncTask" - why the difference? Well, we _would_ have used "DivideAsync", but the name was already taken by the EAP method. TaskWsdlImportExtension does not generate the EAP methods, so it can use the "DivideAsync" name.


Now, we're ready to actually call the client. I have some TAP code that uses the WCF client proxy ("CallCalculator") as well as a simple Main:




static class Program
{
  static async Task CallCalculator()
  {
    try
    {
      var proxy = new CalculatorClient();
      // The following call should be "DivideAsyncTask" if we wrote our own wrappers.
      var task = proxy.DivideAsync(10, 0);
      var result = await task;
      Console.WriteLine("Result: " + result);
    }
    catch (FaultException<CalculatorFault> ex)
    {
      Console.Error.WriteLine("Error: " + ex.Detail.Message);
    }
  }

  static void Main(string[] args)
  {
    try
    {
      CallCalculator().Wait();
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine(ex);
    }

    Console.ReadKey();
  }
}


> In this sample code, Main is blocking on the Task returned from CallCalculator. This is not recommended for real-world code.


## Tomorrow

Well, that's quite a bit of work to enable async!



The good news is: in .NET 4.5, [this all gets easier](http://blogs.msdn.com/b/endpoint/archive/2010/11/13/simplified-asynchronous-programming-model-in-wcf-with-async-await.aspx) ([webcite](http://www.webcitation.org/69BfBD7pO)). How much easier, you ask?



A **lot** easier.



Let's start over, this time targeting .NET 4.5 for both server and client. First, the service interface:



[DataContract]
public class CalculatorFault
{
  [DataMember]
  public string Message { get; set; }
}

[ServiceContract]
public interface ICalculator
{
  // Synchronous equivalent:
  //  [OperationContract]
  //  [FaultContract(typeof(CalculatorFault))]
  //  uint Divide(uint numerator, uint denominator);

  [OperationContract]
  [FaultContract(typeof(CalculatorFault))]
  Task<uint> DivideAsync(uint numerator, uint denominator);
}


OK, it's a little simpler so far, because we can declare service methods returning Task instead of a Begin/End pair.



The core implementation is _exactly the same:_




public class Calculator : ICalculator
{
  public async Task<uint> DivideAsync(uint numerator, uint denominator)
  {
    try
    {
      var myTask = Task.Factory.StartNew(() => numerator / denominator);
      var result = await myTask;
      return result;
    }
    catch (DivideByZeroException)
    {
      throw new FaultException<CalculatorFault>(new CalculatorFault { Message = "Undefined result" });
    }
  }
}


And... wait for it... that's it! No need for any APM wrapper methods! The WCF runtime is intelligent enough to understand that this is an asynchronous implementation of a service method.



Now let's turn our attention to the client. There's another nice surprise awaiting us there.



> I reiterate: the "asynchronicity" of a WCF service is independent from the "asynchronicity" of a WCF client. So if you only control one half of the connection, you can still make your half asynchronous.


Create a WCF client proxy. Heh, that's it. :)



Not only are TAP methods created, they are created _by default!_ Totally awesome!



The client code is _exactly the same_ as if the TaskWsdlImportExtension was used:




static class Program
{
  static async Task CallCalculator()
  {
    try
    {
      var proxy = new CalculatorClient();
      var task = proxy.DivideAsync(10, 0);
      var result = await task;
      Console.WriteLine("Result: " + result);
    }
    catch (FaultException<CalculatorFault> ex)
    {
      Console.Error.WriteLine("Error: " + ex.Detail.Message);
    }
  }

  static void Main(string[] args)
  {
    try
    {
      CallCalculator().Wait();
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine(ex);
    }

    Console.ReadKey();
  }
}


Unfortunately for me, all this wonderful WCF async goodness is coming out along with the ASP.NET Web API, and I'll be migrating away from WCF. Oh, well.

