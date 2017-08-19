---
layout: post
title: "Azure Functions SPA"
description: "Using Azure Functions to host a single-page application."
---

It seems to me that Azure Functions is a perfect match for single-page applications. I believe SPAs are going to be a natural and common use case for Azure Functions in the near future.

I had to set up a SPA with an API running on Azure Functions recently, and it took me a bit to figure out all the pieces. Without further ado...

## Host the API

The first step is to define your API and host it in Azure Functions. [The Visual Studio tooling is out of preview now](https://blogs.msdn.microsoft.com/appserviceteam/2017/08/14/azure-functions-tools-released-for-visual-studio-2017-update-3/), and setting up a C# API is straightforward. Personally, I set up automated deployment from GitHub to my Azure Functions app.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Tip: If you want to deploy from a _subdirectory_ of a GitHub repository, [see this post]({% post_url 2017-08-24-azure-functions-github-subfolder %}).
</div>

## Host the SPA

The second step is to host the SPA. I'm hosting mine on a static file server, which is easily done for free. With static file hosting, I can keep assets in external files without having to handle all of that in my Azure Functions. Or you can use Azure Blob Storage / Amazon S3 for a really cheap hosting solution, too.

## Serve through Azure Functions Proxies

The final step is to set up proxies that will forward API calls to the actual Azure Functions API or to the SPA. In your Azure Functions App, turn on Proxies (currently in preview). Then, in your Functions App project in Visual Studio, make a copy of the `host.json` file, rename it to `proxies.json`, and replace its contents with:

{% highlight json %}
{
    "$schema": "http://json.schemastore.org/proxies",
    "proxies": {
        "api": {
            "matchCondition": {
                "route": "/api/{*rest}"
            },
            "backendUri": "https://%WEBSITE_HOSTNAME%/api/{rest}"
        },
        "app": {
            "matchCondition": {
                "route": "{*rest}"
            },
            "backendUri": "https://%SPA_HOST%"
        }
    }
}
{% endhighlight %}

The only part you need to change is `%SPA_HOST%` - you can change this to where your SPA is hosted, or just keep the file as-is and add an application setting `SPA_HOST` that points to your SPA.

This will set up two proxies. The `api` proxy will proxy all requests starting with `/api` to the actual Azure Function implementations (using [the predefined `WEBSITE_HOSTNAME` setting](https://github.com/projectkudu/kudu/wiki/Azure-runtime-environment)). The `app` proxy will proxy all remaining requests to the SPA. Azure Functions Proxies use the same [route ordering rules as WebAPI 2 attribute routing](https://docs.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/attribute-routing-in-web-api-2#route-order); the `api` proxy is always evaluated before the `app` proxy because the `api` proxy starts with an `/api/` path segment.

## Conclusion

Now you have a single Azure Functions App instance that serves both a SPA and its API!
