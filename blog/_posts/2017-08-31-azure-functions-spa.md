---
layout: post
title: "Azure Functions SPA"
description: "Using Azure Functions to host a single-page application."
---

It seems to me that Azure Functions are a perfect match for single-page applications. I believe SPAs are going to be a natural and common use case for Azure Functions in the near future.

I had to set up a SPA with an API running on Azure Functions recently, and it took me a bit to figure out all the pieces. Without further ado...

## Host the API

The first step is to define your API and host it in Azure Functions. [The Visual Studio tooling is out of preview now](https://azure.github.io/AppService/2017/08/14/Azure-Functions-Tools-released-for-Visual-Studio-2017-Update-3.html), and setting up a C# API is straightforward. Personally, I set up automated deployment from GitHub to my Azure Functions app.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Tip: If you want to deploy from a _subdirectory_ of a GitHub repository, [see this post]({% post_url 2017-08-25-azure-functions-github-subfolder %}).
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
                "route": "/api/{*url}"
            },
            "backendUri": "https://%WEBSITE_HOSTNAME%/api/{url}"
        },
        "app": {
            "matchCondition": {
                "route": "{*url}",
                "methods": [ "GET", "HEAD", "OPTIONS" ]
            },
            "backendUri": "https://%SPA_HOST%/"
        },
        "appResources": {
            "matchCondition": {
                "route": "/static/{*url}",
                "methods": [ "GET", "HEAD", "OPTIONS" ]
            },
            "backendUri": "https://%SPA_HOST%/static/{url}"
        }
    }
}
{% endhighlight %}

The only part you need to change is `%SPA_HOST%` - you can change this to where your SPA is hosted, or just keep the file as-is and add an application setting `SPA_HOST` that points to your SPA.

This will set up three proxies: one for your API calls, one to serve up the SPA HTML, and one to serve up SPA static resources (JavaScript bundle, images, etc).

The `api` proxy will forward all requests starting with `/api` to the actual Azure Function implementations (using [the predefined `WEBSITE_HOSTNAME` setting](https://github.com/projectkudu/kudu/wiki/Azure-runtime-environment)).

The `appResources` proxy will forward all requests starting with `/static` to your SPA host, preserving the remainder of your url.

The `app` proxy will forward all remaining requests to the SPA.

A brief note on priorities: Azure Functions Proxies use the same [route ordering rules as WebAPI 2 attribute routing](https://docs.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/attribute-routing-in-web-api-2#route-order). So the `api` and `appResources` proxies are always evaluated before the `app` proxy because they start with constant path segment prefixes (the `api` proxy starts with an `/api/` path segment, and the `appResources` proxy starts with a `/static/` path segment). This way, the `app` proxy doesn't intercept `/api` and `/static` requests.

## Conclusion

Now you have a single Azure Functions App instance that serves both a SPA and its API! Since both your SPA app and your Azure Functions API exist on the same domain, you don't need to open up CORS for your API.

Requests such as `/api/bob` will be forwarded to your Azure Function `bob`, and requests such as `/` will be forwarded to your main SPA HTML page. Requests such as `/some/long/path` will *also* be forwarded to your SPA HTML page, so you can easily use HTML5 history routing instead of hash routing in your SPA, and your app will be served properly when users refresh the page.