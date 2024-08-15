---
layout: post
title: "Nito.Async 1.4 Released"
---
Today marks a good day: at long last, [Nito.Async](https://github.com/StephenClearyArchive/Nito.Asynchronous) Version 1.4 has been released!

One thing I'm proud of in this release is my unit test coverage. Those were some very adventerous unit tests to write, too!

The major additions this time around are the ActionThread (an ActionDispatcher with a dedicated Thread) and the SynchronizationContextRegister (which the Nito.Async classes now use to check if their SynchronizationContexts satisfy their requirements).

There were a slew of other "cleanup" kinds of changes as well: publisher policy files were added, the help system updated, source-indexed pdbs were included, samples were added to documentation, etc.

