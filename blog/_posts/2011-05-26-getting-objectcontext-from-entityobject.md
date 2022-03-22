---
layout: post
title: "Getting the ObjectContext from an EntityObject"
---
There are a few situations where it's useful to get an **ObjectContext** from an **EntityObject**. Note that in general I do not recommend a design that depends on this; there doesn't appear to be an easy way to do this using code first in EF 4.1 (using the **DbContext** API). That said, either of the solutions in this blog post will work when using the **ObjectContext** API.

The most common solution for this problem is from [a 2009 Microsoft blog post by Alex James](https://docs.microsoft.com/en-us/archive/blogs/alexj/tip-24-how-to-get-the-objectcontext-from-an-entity?WT.mc_id=DT-MVP-5000058) ([webcite](http://www.webcitation.org/5yYYB64NN)). Unfortunately, that solution has several limitations (including the requirement that the entities must have relations to other entities). Both of the solutions below do not have these limitations.

We use an example entity container named **NorthwindEntites**, derived from **ObjectContext**. To this we will add a factory method **FromEntity(EntityObject entity)**, which retrieves the **NorthwindEntities** instance to which that entity is attached, or **null** if the entity is detached.

## Solution 1: Dynamic

The idea behind this solution is to add a property to the entity type that points to its own **ObjectContext**. It's possible to do this by [modifying the code-generating](http://msdn.microsoft.com/en-us/library/dd456821.aspx?WT.mc_id=DT-MVP-5000058) [template](http://msdn.microsoft.com/en-us/library/ff477605.aspx?WT.mc_id=DT-MVP-5000058) file, but it's also possible to just add the property to each entity type manually and use dynamic duck typing to access it.

The modified **NorthwindEntities** uses **OnContextCreated** to hook into its constructor and set up event handlers to respond whenever an entity is added to or removed from this context. Each event handler uses dynamic duck typing to access an "ObjectContext" property on the entity; if no such property exists, the entity is ignored.

{% highlight csharp %}

using System.ComponentModel;
using System.Data.Objects.DataClasses;
using Microsoft.CSharp.RuntimeBinder;

namespace WindowsFormsApplication1
{
  public partial class NorthwindEntities
  {
    partial void OnContextCreated()
    {
      this.ObjectMaterialized += (_, e) =>
      {
        try
        {
          dynamic entity = e.Entity;
          entity.ObjectContext = this;
        }
        catch (RuntimeBinderException)
        {
        }
      };
      this.ObjectStateManager.ObjectStateManagerChanged += (_, e) =>
      {
        if (e.Action == CollectionChangeAction.Add)
        {
          try
          {
            dynamic entity = e.Element;
            entity.ObjectContext = this;
          }
          catch (RuntimeBinderException)
          {
          }
        }
        else if (e.Action == CollectionChangeAction.Remove)
        {
          try
          {
            dynamic entity = e.Element;
            entity.ObjectContext = null;
          }
          catch (RuntimeBinderException)
          {
          }
        }
      };
    }

    /// <summary>
    /// Gets the object context for the entity. Returns <c>null</c> if the entity is detached or does not define an <c>ObjectContext</c> property.
    /// </summary>
    /// <param name="entity">The entity for which to return the object context.</param>
    public static NorthwindEntities FromEntity(EntityObject entity)
    {
      try
      {
        dynamic dynamicEntity = entity;
        return dynamicEntity.ObjectContext;
      }
      catch (RuntimeBinderException)
      {
        return null;
      }
    }
  }
}
{% endhighlight %}

The disadvantage to this approach is that you have to add an **ObjectContext** property to each entity type, like this:

{% highlight csharp %}

namespace WindowsFormsApplication1
{
  public partial class Order
  {
    /// <summary> 
    /// Gets or sets the context for this entity.
    ///  This should not be set by end-user code; this property will be set
    ///  automatically as entities are created or added,
    ///  and will be set to <c>null</c> as entities are detached.
    /// </summary> 
    internal NorthwindEntities ObjectContext { get; set; }
  }
}
{% endhighlight %}

Alternatively, you could modify the creation template. Either way, it's a fair amount of work.

## Solution 2: Connected Properties

The [Connected Properties](http://www.nuget.org/List/Packages/ConnectedProperties) library may be used to "attach" properties to entity objects at run-time. This means it's no longer necessary to add the **ObjectContext** property on each entity type.

This modified **NorthwindEntities** uses the same hooks as the one above, but it uses connected properties instead of dynamic duck typing:

{% highlight csharp %}

using System.ComponentModel;
using System.Data.Objects.DataClasses;
using Nito.ConnectedProperties;
using Nito.ConnectedProperties.Implicit;

namespace WindowsFormsApplication1
{
  public partial class NorthwindEntities
  {
    /// <summary>
    /// The object context connected property type.
    /// </summary>
    private struct ObjectContextProperty { }

    /// <summary>
    /// Gets the object context connected property for a specified carrier object.
    /// </summary>
    /// <param name="entity">The carrier object.</param>
    /// <returns>The connected property.</returns>
    private static IConnectibleProperty<NorthwindEntities> ObjectContext(object entity)
    {
      return entity.GetConnectedProperty<NorthwindEntities, ObjectContextProperty>();
    }

    /// <summary>
    /// Handles post-construction event.
    /// </summary>
    partial void OnContextCreated()
    {
      this.ObjectMaterialized += (_, e) =>
      {
        ObjectContext(e.Entity).Set(this);
      };

      this.ObjectStateManager.ObjectStateManagerChanged += (_, e) =>
      {
        if (e.Action == CollectionChangeAction.Add)
        {
          ObjectContext(e.Element).Set(this);
        }
        else if (e.Action == CollectionChangeAction.Remove)
        {
          ObjectContext(e.Element).Set(null);
        }
      };
    }

    /// <summary>
    /// Gets the object context for the entity. Returns <c>null</c> if the entity is detached.
    /// </summary>
    /// <param name="entity">The entity for which to return the object context.</param>
    public static NorthwindEntities FromEntity(EntityObject entity)
    {
      return ObjectContext(entity).GetOrConnect(null);
    }
  }
}
{% endhighlight %}

The disadvantage of this approach is that you do need to take a dependency on the [Connected Properties](http://www.nuget.org/List/Packages/ConnectedProperties) library, but I think that's a reasonable tradeoff.

This post was inspired by [a recent SO question](http://stackoverflow.com/questions/5707312/whats-the-fastest-way-to-get-an-objectcontext-reference-from-an-entity-object).

