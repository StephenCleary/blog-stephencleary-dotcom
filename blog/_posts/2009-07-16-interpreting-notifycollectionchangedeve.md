---
layout: post
title: "Interpreting NotifyCollectionChangedEventArgs"
---
If you've ever consumed [INotifyCollectionChanged.CollectionChanged](http://msdn.microsoft.com/en-us/library/system.collections.specialized.inotifycollectionchanged.collectionchanged.aspx), then you've run into some inadequate documentation for [NotifyCollectionChangedEventArgs](http://msdn.microsoft.com/en-us/library/system.collections.specialized.notifycollectionchangedeventargs.aspx). I've added the following information to the MSDN "community extensions" (my first contribution), but others have had problems with the stability of community extensions, so the results of my research and Reflector spelunking are also in this blog entry.

In short, the value of the Action property determines the validity of other properties in this class. NewItems and OldItems are null when they are invalid; NewStartingIndex and OldStartingIndex are -1 when they are invalid.

If Action is NotifyCollectionChangedAction.Add, then NewItems contains the items that were added. In addition, if NewStartingIndex is not -1, then it contains the index where the new items were added.

If Action is NotifyCollectionChangedAction.Remove, then OldItems contains the items that were removed. In addition, if OldStartingIndex is not -1, then it contains the index where the old items were removed.

If Action is NotifyCollectionChangedAction.Replace, then OldItems contains the replaced items and NewItems contains the replacement items. In addition, NewStartingIndex and OldStartingIndex are equal, and if they are not -1, then they contain the index where the items were replaced.

If Action is NotifyCollectionChangedAction.Move, then NewItems and OldItems are logically equivalent (i.e., they are SequenceEqual, even if they are different instances), and they contain the items that moved. In addition, OldStartingIndex contains the index where the items were moved from, and NewStartingIndex contains the index where the items were moved to. Note that a Move operation is logically treated as a Remove followed by an Add, so NewStartingIndex is interpreted as though the items had already been removed.

If Action is NotifyCollectionChangedAction.Reset, then no other properties are valid.

## Sources

There are two blog entries that helped me get started: [Making Sense of NotifyCollectionChangedEventArgs](http://blogs.msdn.com/xtof/archive/2008/02/10/making-sense-of-notifycollectionchangedeventargs.aspx) and [An Alternative to ObservableCollection](http://baumbartsjourney.wordpress.com/2009/06/01/an-alternative-to-observablecollection/). However, more details were still lacking; fortunately, Reflector saved the day! The primary .NET sources were: [WindowsBase.dll, 3.0.0.0] System.Collections.ObjectModel.ObservableCollection<T> - SetItem, RemoveItem, MoveItem, etc. and [PresentationFramework.dll, 3.0.0.0] System.Windows.Data.CollectionView - ProcessCollectionChanged, AdjustCurrencyFor*.

