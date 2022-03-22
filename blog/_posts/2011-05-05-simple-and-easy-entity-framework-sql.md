---
layout: post
title: "Simple and Easy Entity Framework SQL Tracing"
---
There's an easy way to [add tracing to an application]({% post_url 2010-12-30-simple-and-easy-tracing-in-net %}), but Entity Framework brings some special challenges. [ObjectQuery.ToTraceString](http://msdn.microsoft.com/en-us/library/system.data.objects.objectquery.totracestring.aspx?WT.mc_id=DT-MVP-5000058) does allow tracing of SQL SELECT commands, but there's no built-in way to trace database updates.

However, there is an [Entity Framework Tracing Provider](http://efwrappers.codeplex.com/) that allows this. Follow the quick-start instructions on the home page, and you'll be off in no time!

Here's a few tests using SQL Server Compact Edition to access the Northwind sample database. This code:

{% highlight csharp %}

using (var db = new NorthwindContext())
{
    MessageBox.Show(db.Orders.Count(x => x.Order_Date < DateTime.Now).ToString());
}
{% endhighlight %}

will result in this trace:

    EntityFramework.NorthwindEntities Information: 0 : Executing 1: SELECT [GroupBy1].[A1] AS [C1] FROM ( SELECT COUNT(1) AS [A1] FROM [Orders] AS [Extent1] WHERE [Extent1].[Order Date] < ( CAST( GetDate() AS datetime)) ) AS [GroupBy1]
    EntityFramework.NorthwindEntities Information: 0 : Finished 1 in 00:00:00.0466592: [DbDataReader(C1:Int)]

Note that the total time taken by the query is included in the finishing trace. Another interesting tidbit is that _DateTime.Now_ is not evaluated on the client side; rather, the SQL statement includes a call to _GetDate_.

Here's some code that deletes an order:

{% highlight csharp %}

using (var db = new NorthwindContext())
{
    db.Orders.DeleteObject(db.Orders.OrderBy(x => x.Order_Date).First());
    db.SaveChanges();
}
{% endhighlight %}

resulting in this trace:

    EntityFramework.NorthwindEntities Information: 0 : Executing 2: SELECT TOP (1) [Extent1].[Order ID] AS [Order ID], [Extent1].[Customer ID] AS [Customer ID], [Extent1].[Employee ID] AS [Employee ID], [Extent1].[Ship Name] AS [Ship Name], [Extent1].[Ship Address] AS [Ship Address], [Extent1].[Ship City] AS [Ship City], [Extent1].[Ship Region] AS [Ship Region], [Extent1].[Ship Postal Code] AS [Ship Postal Code], [Extent1].[Ship Country] AS [Ship Country], [Extent1].[Ship Via] AS [Ship Via], [Extent1].[Order Date] AS [Order Date], [Extent1].[Required Date] AS [Required Date], [Extent1].[Shipped Date] AS [Shipped Date], [Extent1].[Freight] AS [Freight] FROM [Orders] AS [Extent1] ORDER BY [Extent1].[Order Date] ASC
    EntityFramework.NorthwindEntities Information: 0 : Finished 2 in 00:00:00.0027257: [DbDataReader(Order ID:Int, Customer ID:NVarChar, Employee ID:Int, Ship Name:NVarChar, Ship Address:NVarChar, Ship City:NVarChar, Ship Region:NVarChar, Ship Postal Code:NVarChar, Ship Country:NVarChar, Ship Via:Int, Order Date:DateTime, Required Date:DateTime, Shipped Date:DateTime, Freight:Money)]
    EntityFramework.NorthwindEntities Information: 0 : Executing 3: delete [Orders] where ([Order ID] = @0) { @0=[Int32,0,Input]10000 }
    EntityFramework.NorthwindEntities Information: 0 : Finished 3 in 00:00:00.0482807: 1 rows affected

As expected, the first command executes a single-row SELECT, followed by a DELETE that affects a single row. Note the use of the parameterized deletion query.

Unfortunately, the Entity Framework Tracing Provider does not support everything; in particular, direct database commands (e.g., [ExecuteStoreCommand](http://msdn.microsoft.com/en-us/library/system.data.objects.objectcontext.executestorecommand.aspx?WT.mc_id=DT-MVP-5000058)) are not supported.

