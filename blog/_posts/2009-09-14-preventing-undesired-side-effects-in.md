---
layout: post
title: "Preventing Undesired Side-Effects in Enumerators"
---
I normally don't write a post just to link to a post on another blog, because I want this blog to be mostly original content. However, today I read an excellent article/blog post on the B# blog: [Taming Your Sequence's Side-Effects Through IEnumerable.Let](http://bartdesmet.net/blogs/bart/archive/2009/09/12/taming-your-sequence-s-side-effects-through-ienumerable-let.aspx).



The author does a great job describing how side effects in sequences can be problematic, and how to create a "MemoizeEnumerable" to prevent multiple evaluations of a source sequence. It's a great application of dynamic programming (see [The Algorithm Design Manual](http://www.amazon.com/gp/product/1848000693?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=1848000693)).



Side effects when dealing with enumerables are rare; few people have needed an "IEnumerable.Let" operator. Side effects are much more common when dealing with events, e.g., the up-and-coming Rx framework. I highly recommend reading the B# blog entry to understand the rationale behind "Let" for enumerables; it will prove good background information to understand "Let" for observables.

