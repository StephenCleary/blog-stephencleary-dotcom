---
layout: post
title: "Using GitHub Actions for Pull Request Staging Environments, Part 1: Introduction"
series: "Using GitHub Actions for Pull Request Staging Environments"
seriesTitle: "Introduction"
description: "Using GitHub Actions to automatically deploy and teardown staging environments for pull requests."
---

## The Goal

This is a short series of posts on how to use GitHub Actions to deploy and teardown staging environments for your pull requests.

The idea is that each pull request on GitHub represents some change (feature, bug fix, etc) that you want to test out before merging it into master. This series looks at using GitHub Actions to deploy the PR code into an isolated staging environment, and then tearing down that environment when you're done testing.

## Example Services

Since this series is focusing on using GitHub Actions, our environments will be deliberately simple.

I'll be using [Gatsby](https://www.gatsbyjs.org/) as the framework for the example project. I have not used Gatsby yet, but I'm considering moving this blog to it. Gatsby is a React-based front-end development system that produces static files as its output.

I'll be using [Surge](https://surge.sh/) as a deployment engine. I never heard of Surge until I was reading the docs for Gatsby, but I must say I'm impressed with it! Surge allows you to quickly publish any folder to a domain name, and just as quickly tear it down again.

## Getting Started

It's best if you follow along right on GitHub.

First, create a test repository (mine is [here](https://github.com/StephenClearyExamples/PullRequestStaging)); the following steps give you a repository with a tiny Gatsby site on it:
- Create a new repository on GitHub and clone it to your local machine.
- Create a new "hello, world" Gatsby project in the `my-hello-world` folder by running `npx gatsby new my-hello-world https://github.com/gatsbyjs/gatsby-starter-hello-world`
- Copy the files in the `my-hello-world` folder to the root of your repository.
- Commit and push.

Next, sign up for a Surge account:
- Install Surge by running `npm install --global surge`
- Sign up for a Surge account by running `surge login`

Optionally, you can deploy what's currently on your master branch:
- Build the Gatsby site by running `npx gatsby build`
  - Gatsby output is placed in the `public` folder.
- Choose a domain name that should be unique.
- Deploy the Gatsby output to your domain name by running `surge ./public MY-UNIQUE-DOMAIN.surge.sh`

## Quick Surge Primer

Be aware that running Surge without any arguments will attempt to deploy the current folder. If you need help, you have to pass a `--help` argument.

At any time, you can check the status of your Surge deployments by running `surge list`, and you can tear down any Surge deployment by running `surge teardown`.

## Next Steps

That's it for the command line; the rest of the repository updates can be done entirely on GitHub. 
