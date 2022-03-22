---
layout: post
title: "Async Console Apps on .NET CoreCLR"
description: "Asynchronous console application entry points are supported on CoreCLR."
---

I finally had a chance this week to play around a bit with [Visual Studio 2015 CTP 6](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-ctp-vs). I hardly ever have the time to use CTP bits, but with [all the talk about CoreCLR](https://devblogs.microsoft.com/dotnet/introducing-net-core/?WT.mc_id=DT-MVP-5000058) I just *had* to try it out. :)

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

This entire post is about prerelease technologies. Everything is subject to change.
</div>

In [one of the recent articles about CoreCLR](https://msdn.microsoft.com/en-us/magazine/dn913182.aspx?WT.mc_id=DT-MVP-5000058), Daniel Roth mentions in passing "You can even make the main entry point asynchronous and return a Task."

Wait, what?

Yup, that's right! Asynchronous `Main` methods for Console applications! Let's see how this works...

## Getting Started with the CoreCLR CTP

If you want to follow along at home, first [install the VS2015 CTP](https://www.visualstudio.com/en-us/downloads/visual-studio-2015-ctp-vs).

Next, you'll need to install some K runtime stuff. The best instructions I've found for this are on the [home page of the ASP.NET 5 GitHub repo](https://github.com/aspnet/home). If you just want to get started quickly, open up PowerShell and execute the following commands:

{% highlight PowerShell %}
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.ps1'))"
kvm upgrade
{% endhighlight %}

## Aside: How Many Runtimes?

It's important to note that there are *two* K runtimes installed (well, four, if you count 32/64-bit variants). There's the "ordinary" desktop CLR and the new CoreCLR. You can type `kvm list` to show the installed runtimes and switch between them with `kvm upgrade -r coreclr` and `kvm upgrade -r clr`:

    PS> kvm list

    Active Version     Runtime Architecture Location                     Alias
    ------ -------     ------- ------------ --------                     -----
           1.0.0-beta3 clr     x64          C:\Users\stephen\.k\runtimes
      *    1.0.0-beta3 clr     x86          C:\Users\stephen\.k\runtimes default
           1.0.0-beta3 coreclr x64          C:\Users\stephen\.k\runtimes
           1.0.0-beta3 coreclr x86          C:\Users\stephen\.k\runtimes


    PS> kvm upgrade -r coreclr

    Determining latest version
    kre-coreclr-win-x86.1.0.0-beta3 already installed.
    Adding C:\Users\stephen\.k\runtimes\kre-coreclr-win-x86.1.0.0-beta3\bin to process PATH
    Adding C:\Users\stephen\.k\runtimes\kre-coreclr-win-x86.1.0.0-beta3\bin to user PATH
    Updating alias 'default' to 'kre-coreclr-win-x86.1.0.0-beta3'


    PS> kvm list

    Active Version     Runtime Architecture Location                     Alias
    ------ -------     ------- ------------ --------                     -----
           1.0.0-beta3 clr     x64          C:\Users\stephen\.k\runtimes
           1.0.0-beta3 clr     x86          C:\Users\stephen\.k\runtimes
           1.0.0-beta3 coreclr x64          C:\Users\stephen\.k\runtimes
      *    1.0.0-beta3 coreclr x86          C:\Users\stephen\.k\runtimes default


    PS> kvm upgrade -r clr

    Determining latest version
    kre-clr-win-x86.1.0.0-beta3 already installed.
    Adding C:\Users\stephen\.k\runtimes\kre-clr-win-x86.1.0.0-beta3\bin to process PATH
    Adding C:\Users\stephen\.k\runtimes\kre-clr-win-x86.1.0.0-beta3\bin to user PATH
    Updating alias 'default' to 'kre-clr-win-x86.1.0.0-beta3'


    PS> kvm list

    Active Version     Runtime Architecture Location                     Alias
    ------ -------     ------- ------------ --------                     -----
           1.0.0-beta3 clr     x64          C:\Users\stephen\.k\runtimes
      *    1.0.0-beta3 clr     x86          C:\Users\stephen\.k\runtimes default
           1.0.0-beta3 coreclr x64          C:\Users\stephen\.k\runtimes
           1.0.0-beta3 coreclr x86          C:\Users\stephen\.k\runtimes

The regular desktop CLR is sitting on top of the full .NET framework. The CoreCLR is a new runtime. This is the one that a lot of people refer to as "cloud optimized" - though in reality, it's just more of a microkernel-based framework as opposed to the traditional monolithic one. I expect we'll see some great things from CoreCLR other than just running ASP.NET apps in the cloud (though it will of course be good for that too!) - the microkernel "only pay for what you use" architecture is perfectly suited for mobile platforms - and even smaller-than-mobile platforms!

## Creating a CoreCLR Console Application

Let's get started! In the CTP6, to create a CoreCLR console app, you choose New Project, and then...

Select "Web" on the left-hand side, and then "ASP.NET 5 Console Application".

{:.center}
[![]({{ site_url }}/assets/AspNetConsoleApplication.png)]({{ site_url }}/assets/AspNetConsoleApplication.png)

Wait, what?

Yeah, "ASP.NET Console Application" is a ridiculous juxtaposition... This will probably be renamed completely as the tooling continues to develop. For now, though, the CoreCLR has strong roots in the ASP.NET world, so we create a CoreCLR Console application as an "ASP.NET Console" application.

## Making It Asynchronous!

The default code the template gives you is pretty straightforward:

{% highlight csharp %}
public class Program
{
    public void Main(string[] args)
    {
        Console.WriteLine("Hello World");
        Console.ReadLine();
    }
}
{% endhighlight %}

So let's just rewrite it to be asynchronous:

{% highlight csharp %}
public class Program
{
    public async Task Main(string[] args)
    {
        Console.WriteLine("Hello World");
        await Task.Delay(TimeSpan.FromSeconds(1));
        Console.WriteLine("Still here!");
        Console.ReadLine();
    }
}
{% endhighlight %}

Compile and run it, and you'll see the Console application sticking around after the `await`!

{:.center}
[![]({{ site_url }}/assets/AsyncConsoleFullClr.png)]({{ site_url }}/assets/AsyncConsoleFullClr.png)

Note the title bar. This console application is actually just a dll, which is hosted in the K runtime for the full desktop CLR. So this is actually running on whatever .NET desktop framework you have installed.

## Running on the CoreCLR

To run on the actual CoreCLR, select the dropdown arrow of the Start button and choose the .NET Core as your CLR:

{:.center}
[![]({{ site_url }}/assets/SwitchToNetCore.png)]({{ site_url }}/assets/SwitchToNetCore.png)

Then when you run it, you'll be running on the CoreCLR (again, note the title bar):

{:.center}
[![]({{ site_url }}/assets/AsyncConsoleCoreClr.png)]({{ site_url }}/assets/AsyncConsoleCoreClr.png)

## Compiling for Multiple Frameworks

It's important to peek under the covers a bit. Pop open your `project.json`. See the `frameworks` section?

{% highlight json %}
"frameworks" : {
    "aspnet50" : { },
    "aspnetcore50" : { 
        "dependencies": {
            "System.Console": "4.0.0-beta-22523"
        }
    }
}
{% endhighlight %}

There are two frameworks for this project: `aspnet50` is the regular desktop CLR-based framework - it's the one that runs on the full .NET desktop framework. The `aspnetcore50` is the CoreCLR framework - it's the new microkernel one. Since CoreCLR is a microkernel architecture, you need to explicitly bring in nearly everything you intend to use - in this case, the VS template automatically added a dependency on `System.Console` for you. (You can add more dependencies by installing NuGet packages; `System.Console` is in fact just a NuGet package that is already installed into this project).

The important takeaway here is that this project is compiled *twice*, once for each framework. And there are APIs in each framework that the other doesn't have. For example, I have a weird quirk where I strongly prefer using `ReadKey` instead of `ReadLine` in my throwaway Console applications. But CoreCLR's `System.Console` package doesn't support `ReadKey`!

So, what happens if I change `ReadLine` to `ReadKey`? At first, it looks OK, but it will no longer build.

{:.center}
[![]({{ site_url }}/assets/ConsoleReadKeyError.png)]({{ site_url }}/assets/ConsoleReadKeyError.png)

The confusing part here is that there's no "red squigglies" or anything indicating that `Console` doesn't have a `ReadKey` method. Just an error that seems to come out of nowhere.

The key is to *use the navigation bar* - VS is aware that this file is compiled two different ways because our project is supporting two frameworks. By switching the *view* of the file to the CoreCLR, I can now see the error:

{:.center}
[![]({{ site_url }}/assets/ConsoleReadKeyErrorVisible.png)]({{ site_url }}/assets/ConsoleReadKeyErrorVisible.png)

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Note that the "Project" column of the Error List window specifies the *framework* as well as the project. This should be the first thing to check if you're seeing "impossible" or mysterious errors like this.
</div>

## Conditional Compilation

Well, let's say I really *want* to use `ReadKey`?

One solution is to make use of conditional compilation. Remember, this project is being compiled separately for each platform. As [Rick Strahl pointed out](http://weblog.west-wind.com/posts/2014/Dec/02/Creating-multitarget-NuGet-Packages-with-vNext), each platform defines a compiler directive that is just the platform name in all uppercase:

{% highlight csharp %}
public class Program
{
    public async Task Main(string[] args)
    {
        Console.WriteLine("Hello World");
        await Task.Delay(TimeSpan.FromSeconds(1));
        Console.WriteLine("Still here!");
#if ASPNET50
        Console.ReadKey();
#else
        Console.ReadLine();
#endif
    }
}
{% endhighlight %}

That works! So, you can use conditional compilation to take advantage of different APIs for different frameworks.

## Limiting Frameworks

The conditional compilation does work, but it also causes a change in behavior between the frameworks. This is not ideal.

Instead, I'm going to decide that `ReadKey` is just plain critical for my demos, and so CoreCLR is out. Thanks for playing.

To limit the frameworks for my project, I just have to edit the `project.json` file and remove the CoreCLR:

{% highlight json %}
"frameworks" : {
    "aspnet50" : { }
}
{% endhighlight %}

Save the file, and now my project only targets the full desktop CLR.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Removing the `aspnetcore50` framework will probably have to be done a *lot* over the next few months. There are a lot of third-party libraries that do not support the CoreCLR (yet).
</div>

Incidentally, I provided this technique as a [Stack Overflow answer](http://stackoverflow.com/a/28887513/263693) earlier today.

## Future Speculations

Currently, the "console application" we're building is actually just a dll that's hosted by a K runtime. I *expect* that when VS2015 gets to RTM, they'll have a real executable popping out of the build. They have [already started on something like that](https://devblogs.microsoft.com/dotnet/coreclr-is-now-open-source/?WT.mc_id=DT-MVP-5000058).

The old Console application type will undoubtedly stick around, for backwards compatibility at least. So, I'd expect VS2015 will have two Console application types: a legacy one that runs directly on the .NET Desktop Framework, and a new one that run on the K runtime (which in turn can run on either .NET Core CLR or .NET Desktop CLR). It's the K runtime that gives us the power of an asynchronous entry point, so I'd expect only the new console application type would be able to have an `async Main`.

I just have to say that it's **so cool** to get `async Main` support! :)