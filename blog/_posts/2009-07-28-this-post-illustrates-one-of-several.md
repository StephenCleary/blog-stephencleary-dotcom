---
layout: post
title: "SimplePropertyPath: A Poor Man's Binding"
---
This post illustrates one of several utility classes that are in the [Nito.MVVM](http://nitomvvm.codeplex.com/) library: SimplePropertyPath.

SimplePropertyPath is used to create a very simple binding using only [INotifyPropertyChanged](http://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged.aspx) and not using [DependencyProperty](http://msdn.microsoft.com/en-us/library/system.windows.dependencyproperty.aspx) or [DependencyObject](http://msdn.microsoft.com/en-us/library/system.windows.dependencyobject.aspx). The [System.Windows.Data.Binding](http://msdn.microsoft.com/en-us/library/system.windows.data.binding.aspx) class is much more powerful, but is dependent on the WPF-specific dependency property/object system. SimplePropertyPath only uses INotifyPropertyChanged, which gives it two advantages:

- It can be used in non-WPF environments with only minor changes, e.g., using the MVVM pattern on a compact device.
- It allows all ViewModel classes to be POCO (utilizing INotifyPropertyChanged) instead of forcing them to be derived from DependencyObject. (In my mind, at least, DependencyObject or FrameworkElement-derived classes are more of a View class).

SimplePropertyPath merely propagates an existing INotifyPropertyChanged implementation "forward" to other listeners, and propogates writes "back" to the original property. It is only capable of understanding a "simple" property path: one comprised entirely of member accessors.

Some examples will help clarify how this class can be used; the following "fake ViewModel" class is used by these examples. It just has two properties: "int Value" and "FakeVM Child", and implements INotifyPropertyChanged:

{% highlight csharp %}
private sealed class FakeVM : INotifyPropertyChanged
{
    private int value;
    private FakeVM child;

    public int Value
    {
        get { return this.value; }
        set
        {
            this.value = value;
            this.OnPropertyChanged("Value");
        }
    }

    public FakeVM Child
    {
        get { return this.child; }
        set
        {
            this.child = value;
            this.OnPropertyChanged("Child");
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        if (this.PropertyChanged != null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
{% endhighlight %}

The API of SimplePropertyPath is rather simple:

![SimplePropertyPath Class Diagram]({{ site_url }}/assets/SimplePropertyPath.png)  

The two properties "Root" and "Path" are used to define the SimplePropertyPath. The "Value" property is used to read or write the nested property.

A simple example to start off with; to read or write the "Value" property on a FakeVM object:

{% highlight csharp %}
FakeVM obj = new FakeVM { Value = 13 };
SimplePropertyPath path = new SimplePropertyPath { Root = obj, Path = "Value" };

Assert.AreEqual(13, path.Value);

path.Value = 17;
Assert.AreEqual(17, obj.Value);
{% endhighlight %}

Nothing too difficult there. The next example exercises reading and writing to a longer path:

{% highlight csharp %}
FakeVM obj = new FakeVM { Child = new FakeVM { Value = 10 } };
SimplePropertyPath path = new SimplePropertyPath { Root = obj, Path = "Child.Value" };

Assert.AreEqual(10, path.Value);

path.Value = 17;
Assert.AreEqual(17, obj.Child.Value);
{% endhighlight %}

Now it's starting to act more like a real binding. Invalid property paths will result in a Value of null (writing errors to PresentationTraceSources.DataBindingSource just like WPF data binding does):

{% highlight csharp %}
FakeVM obj = new FakeVM { Value = 13 };
SimplePropertyPath path = new SimplePropertyPath { Root = obj, Path = "value" };

Assert.IsNull(path.Value);

path.Value = 17;
Assert.AreEqual(13, obj.Value);
Assert.IsNull(path.Value);
{% endhighlight %}

The "childmost" property is not the only one that is being monitored; SimplePropertyPath will monitor INotifyPropertyChanged for each object along the path:

{% highlight csharp %}
FakeVM obj = new FakeVM { Child = new FakeVM { Value = 100 } };
SimplePropertyPath path = new SimplePropertyPath { Root = obj, Path = "Child.Value" };

Assert.AreEqual(100, path.Value);

obj.Child = new FakeVM { Value = 113 };
Assert.AreEqual(113, path.Value);
{% endhighlight %}

Finally, SimplePropertyPath will raise its own INotifyPropertyChanged for its Value property every time it changes, whether it was caused by a change in the "childmost" property or any of the objects along the path.

SimplePropertyPath is used by the [Nito.MVVM](http://nitomvvm.codeplex.com/) library as a building block to construct some of the more advanced classes, such as MultiProperty and MultiCommand. However, it can be useful in its own right.

[Note: all of the examples above were copied almost verbatim from the unit tests in the Nito.MVVM library].

