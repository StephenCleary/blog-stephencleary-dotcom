---
layout: post
title: "Continuously Deploy Azure Functions from a GitHub Subfolder"
description: "Setting up Azure Functions to continuously deploy from a subfolder of a GitHub repository."
---

[You can set up continuous deployment for Azure Functions from any branch of a GitHub repository](https://docs.microsoft.com/en-us/azure/azure-functions/functions-continuous-deployment). Unfortunately, the deployment system will either just deploy your files (`Found solution 'D:\home\site\repository\src\Sample.sln' with no deployable projects. Deploying files instead.`) or may try to deploy the wrong project (e.g., if you have a console app in your solution).

You can use a different branch in GitHub for the output of your solution, but this is awkward. The solution I chose is to deploy the Azure Functions App from a _specific subfolder_ of a GitHub repository. You can do this by [adding a `.deployment` file in the root of your repository that tells the deployment system where your Azure Functions App is](https://github.com/projectkudu/kudu/wiki/Customizing-deployments). E.g.:

{% highlight ini %}
[config]
project = src/FunctionApp1
{% endhighlight %}

If you run into deployment errors `The "Move" task failed unexpectedly. (System.Runtime.InteropServices.COMException (0x800700A1): The specified path is invalid.)` when the `_GenerateFunctionsPostBuild` target tries to copy your project output, then [ensure you're using v1.0.1 or later of `Microsoft.NET.Sdk.Functions`](https://stackoverflow.com/questions/45743877/azure-function-ci-build-error-cannot-create-a-file-when-that-file-already-exis).
