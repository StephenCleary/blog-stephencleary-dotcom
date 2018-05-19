---
layout: post
title: "Docker as a Tool Provider"
description: "Docker is more than a scalable backend technology; it can also be used to encapsulate and execute tools."
---

Docker is everywhere these days! Even with serverless technology growing more mature, Docker is still a giant in the cloud world. Everyone by now should be familiar with Docker as a way of scaling out your servers using containers.

However, there's another perfectly legitimate use case for Docker: building containers for tools.

This is particularly useful in a Windows environment. I'm an old curmudgeon who knows how to use Windows and hasn't taken the time to learn Macs. Every time I get a new dev machine, I consider switching, but I just haven't done it. So my dev machines - even in this modern day - are still all Windows machines.

This is all good, until you want to make use of some nifty Linux tools. The thing is, Windows just doesn't work so great with Ruby. Or Python. Or Perl. Some tools have Windows builds that bundle their own dependencies, and others require certain environment variables to find just the right version of whatever runtime they need. It works *ok*, but what usually ends up happening is that you have to tweak your dev machine until it is *just right* for all the dependencies of all the different tools you want to use... and then you can never get another machine into that exact same state ever again.

I *could* use Ubuntu on Windows, which is awesome. But I'd still have to install the tools, and manage dependency conflicts and updates and all that. And I don't want to shell out to a separate subsystem just to be able to build; I want my overall development environment to be Windows.

I *could* use a solution like [Boxstarter](https://boxstarter.org/), but some of this setup is so specific I'd have to write my own Chocolatey packages for some of them and tie it all together with some very custom scripts. I don't want another project to maintain (the "Steve Cleary dev box setup script and Chocolately package collection"), especially because newer versions of the tools would require changes to those scripts, so I'd end up changing them every time I need to run them anyway! At the end of the day, it would just be way too complex and brittle.

What I want is to use Boxstarter for my common tools that I use *regularly* (Visual Studio, VSCode, Docker, Node, Git, etc); but for my more esoteric tools, I want to be able to encapsulate them and pull them in as a complete unit when I want to use them. And I don't want to deal with conflicting dependencies for my different tools; I want them more... *contained*. (Heh, see what I did there?)

The answer? Docker, of course.

## Docker for Tools

For this example, I'm going to run [Lilypond](http://lilypond.org/) as a Dockerized tool. If you're not familiar with it, Lilypond is pretty much *the* standard for free musical notation software.

I have [an old project](https://github.com/StephenCleary/Hymnals) of Lilypond files that I wanted to hack on recently, but my current dev laptop doesn't have Lilypond installed. At this point, it has been literally *years* since I've installed Lilypond, and I don't remember the details. I do remember that I was using some kind of GUI frontend Lilypond runner (not part of the actual Lilypond project), and I *think* the frontend bundled Lilypond along with its dependencies. Maybe. And that GUI frontend project may not be maintained anymore, assuming I can even remember its name (GUI frontends are abandoned much more often than the "real" console applications they build on). I think there was also some weird stuff with PostScript printer drivers or something that may or may not have required tweaking since PostScript isn't standard on Windows. And of course I didn't write any of this down.

Am I going to install Lilypond on my modern dev laptop? Um, no.

I'm going to Dockerize this tool. I'm going to run Lilypond on the platform it was designed for (Linux), and I'm going to run it on my Windows machine inside a Docker container. I'm going to get this set up *once* and then never, ever have to install this on any machine for the rest of my life.

## Lilypond on Docker

I started out thinking that I'd have to write a `Dockerfile` and install Lilypond on it and everything. I went down that path a little ways before I remembered that **duh**, Docker has [a public repository of images](https://hub.docker.com/)! And it turns out that I [wasn't the first one](https://hub.docker.com/search/?isAutomated=0&isOfficial=0&page=1&pullCount=0&q=lilypond&starCount=0) to want Lilypad Dockerized.

Well, [look at that](https://hub.docker.com/r/iskaron/lilypond/) - here's a nice little Docker image that has Lilypond installed. It's kept up-to-date (automated build), and seems to have everything I want! Docker Hub FTW!

With a little tinkering, I found that I could run this straight from the command line:

{% highlight text %}
docker run --rm --volume=C:\Work\Hymnals:/app -w /app iskaron/lilypond lilypond SeniorHymnal/Hymnal.ly
{% endhighlight %}

This command will download the Lilypond Docker image automatically (if it's not already downloaded on the local machine), create a new container, run Lilypond within that container on my local files, and clean up the container when Lilypond exits. Let's tear apart this command, piece by piece.

`docker run` - This command is used to create a new Docker container from a template image, and run it. It also implicitly downloads the Docker image from Docker Hub if necessary.

`iskaron/lilypond` - The name of the template image that Docker uses to create the container.

`--volume=C:\Work\Hymnals:/app` - Create a volume that links to `C:\Work\Hymnals` and mount it as `/app` within the container.

`-w /app` - Set the working directory within the container to `/app`.

`lilypond SeniorHymnal/Hymnal.ly` - The actual command to run inside the container. On my local disk I have `C:\Work\Hymnals\SeniorHymnal\Hymnal.ly`, which is accessible inside the container as `/app/SeniorHymnal/Hymnal.ly`.

`--rm` - When our tool is done executing, clean up the Docker image and its resources.

So now I have a single command that I can run, and it will automatically pull down a Dockerized tool and run it in a clean environment! There's no interference with my local dev box at all; the Dockerized tool is *completely* independent. What's more, it runs in a new "clean room" environment every time it's executed; even if the tool messes up the container, the next time it's run, it'll have a brand new, clean container to run in.

## NPM Scripts

Since I apparently only hack on my Lilypond files every few years, I'm not expecting myself to remember this command. Also, there's this annoying hardcoded `C:\Work\Hymnals` path that I want to get rid of. Time for `package.json`!

{% highlight json %}
"scripts": {
  "build": "docker run --rm --volume=%INIT_CWD%:/app -w /app/SeniorHymnal iskaron/lilypond lilypond Hymnal.ly"
},
{% endhighlight %}

Inside the npm script, I have access to `%INIT_CWD%`, which is a Windows-specific way of getting the current working directory. To be honest, getting the current working directory was the hardest part of this whole setup!

Now I can just do an `npm run build` to process my Lilypond files. What's more, I can [edit them in VSCode](https://marketplace.visualstudio.com/items?itemName=truefire.lilypond) and bind `npm run build` as the default build command. Now I have an actual development environment for Lilypond - no separate GUI frontend necessary!

## Updating Images

There's something else I'd like to do: currently, `docker run` will pull down the latest `iskaron/lilypond` image *the first time it is run*. After that, [it never checks for updates](https://github.com/moby/moby/issues/34394). So I'd like to easily do a `docker pull` as well.

Here's a setup that checks for a new version each time it's run:

{% highlight json %}
"scripts": {
  "prebuild": "docker pull iskaron/lilypond",
  "build": "docker run --rm --volume=%INIT_CWD%:/app -w /app/SeniorHymnal iskaron/lilypond lilypond Hymnal.ly"
},
{% endhighlight %}

Perhaps this is a little too much, though. I rarely run Lilypond, but when I do, I'll run it a lot within a few days. So I think it makes more sense to have an explicit `npm run pull` command:

{% highlight json %}
"scripts": {
  "pull": "docker pull iskaron/lilypond",
  "build": "docker run --rm --volume=%INIT_CWD%:/app -w /app/SeniorHymnal iskaron/lilypond lilypond Hymnal.ly"
},
{% endhighlight %}

The disadvantage to this approach is that I can easily forget to run `npm run pull` when I've been away from the project a long time.

## NPM Scripts for Dockerized Tools

What I have so far is good, but it's pretty tied to Lilypond specifically. I want to make my NPM scripts a bit more copy-pastable by making the Docker commands more generic. After hacking around a bit, I ended up with this:

{% highlight json %}
"config" : { "image" : "iskaron/lilypond" },
"scripts": {
  "pull": "docker pull %npm_package_config_image%",
  "docker-run": "docker run --rm -v %INIT_CWD%:/app -w /app %npm_package_config_image%",
  "lilypond": "npm run docker-run lilypond",
  "build": "npm run lilypond -- SeniorHymnal/Hymnal.ly"
},
{% endhighlight %}

Now I have a single `config` value that will be different for different `project.json` files. I can run `npm run pull` to update the latest Dockerized tool for this project, and I can run `npm run build` to execute that Dockerized tool. The `pull` and `docker-run` scripts can work with any Dockerized tool and can be copy-pased long with `config` from one project to the next. `docker-run` in particular has all the "magic" that Docker needs to run a tool in a throwaway container.

If anyone has any recommendations to make this even better, I'm all ears!

<!--However, this approach does assume only one Dockerized tool per `project.json`. It's possible to override the `config` when calling one script from another, but that gets verbose pretty quickly. Well, none of my current projects need more than a single Dockerized tool, so I'll cross that bridge when I come to it.-->

The nice thing about using Docker from `project.json` is that my dev machine now only has a couple of common requirements (Docker and Node), which it should get from Boxstarter. That's all it needs to be capable of building any Lilypond script project. No more tool installs!

## Next Target

So, I never actually hacked on any of my Lilypond files; I just spent a bunch of time doing this instead. But now it's done and I'll never have to set up another machine with Lilypond (or its GUI wrapper) ever again.

The next logical target for Dockerized tooling is this blog. It currently uses Jekyll with Pygments. That means Ruby *and* Python. Currently, to build this blog you have to [first build a Rube Goldberg machine](https://github.com/StephenCleary/blog-stephencleary-dotcom#to-build) with a portable (self-contained) Ruby + Jekyll and a local (impacting your dev environment) Python + setuptools + pygments. It's a mess of tooling, and it's the next thing on my list to Dockerize!
