---
layout: post
title: "Naming Unit Tests"
tags: [".NET"]
---


I spent some of this last weekend writing my first real unit tests, and I noticed by the end of the weekend that they were not exactly maintainable (a common problem for first-time unit testers). I understood the theory of unit tests, so I didn't make the mistakes of having dependent tests or testing multiple failures in a single unit test. However, my unit tests are on the long side (up to ~20 lines), mainly because I'm testing a complex threading and synchronization library ([Nito.Async](http://www.codeplex.com/NitoAsync))





My biggest problem was naming. There are many different situations that need to be tested when working with these low-level synchronization objects (indeed, I've been enlightened that code coverage is a nearly useless metric for the Nito.Async library). As a result, there are more than 100 unit tests already, and I still have one of the more complex components to test. Even though the unit tests are grouped by component, the individual test names are still inadequate.





Take one example unit test method for the ActionDispatcher, named "TestCurrentPropertyInsideAction". Not a very good name; it does describe the _situation_ (the Current property is accessed from inside an action), but it doesn't describe the _expected behavior_ (that the Current property should be that action's ActionDispatcher). For that matter, the situation could read better, too. I wanted to fix this test name, but also develop a consistent naming scheme.





So, I turned to my trusty book (which I have not finished reading), [The Art of Unit Testing](http://www.amazon.com/gp/product/1933988274?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=1933988274) and skipped to the tips on maintainable unit tests. Long story short: he had some recommendations I didn't particularly care for regarding naming unit tests. Unit testing is still relatively new, and "best practices" are still in development, so I turned to Google, and there are a couple of independently discovered alternatives.





Here's my opinion on three different approaches I've found. Each one is tested with my problem case "TestCurrentPropertyInsideAction" as an example.



## Common Elements



Each of the naming conventions have a "method", "stimulus", or "subject" that they are testing. They also have a "state", "context" or "scenario" defining the situation in which the test takes place. Finally, each convention has a "behavior" or "result" that is expected. These naming conventions were developed independently, from different perspectives, and they still show a remarkable commonality. The obvious conclusion is that these three elements are criticial components in any unit test name.



## Option 1 - Method/State/Behavior



This is the method recommended by [The Art of Unit Testing](http://www.amazon.com/gp/product/1933988274?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=1933988274); if you're following along at home, this pattern is also described on [the author's blog](http://weblogs.asp.net/rosherove/archive/2005/04/03/TestNamingStandards.aspx).





The "Method" is the name of the method/property that is being tested. The "State" is the state of the object and parameters passed to that method. The "Behavior" is the expected behavior of that method or expected value of that property.





Original example: "Sum_NegativeNumberAs1stParam_ExceptionThrown()"





Applied example: "Current_FromInsideAction_ActionDispatcherForThatAction()"





The main reason I dislike this approach is because it has an emphasis on method and property testing. In general, I think this would lead to more "procedural tests" rather than "object-oriented tests"; I'm not talking about the unit test methods themselves (which are of course procedural), but rather about _how one conceives of the component under test_. This would have the side effect of writing unit tests to achieve code coverage, rather than testing state as well as behaviour.





Another reason I'm not too fond is that it does suffer from some readability problems. More readable examples would be "Sum_WhenFirstParamIsNegative_ThrowsException()" or "Current_WhenReadFromInsideAction_IsTheActionDispatcherForThatAction()".



## Option 2 - Stimulus/Result/Context



The Stimulus/Result/Context approach is described [on this blog post](http://weblogs.asp.net/pgielens/archive/2006/04/30/444517.aspx).  The "Stimulus" is what the object is requested to do. The "Result" is the expected behavior. The "Context" is the relevant state of the object.





The original example was a bit unreadable for my taste: "CalculatePayIncludingSalesBonusAfterSalesBonusGrantedToEmployee()".





Splitting the components with underscores makes it better: "CalculatePay_IncludingSalesBonus_AfterSalesBonusGrantedToEmployee()".





Applied example: "CurrentProperty_IsSameActionDispatcherAsScopingAction_WithinActionScope()".





Still not too readable. Better examples would be "CalculatedPay_IncludesSalesBonus_AfterSalesBonusGrantedToEmployee()" and "CurrentProperty_IsSameActionDispatcherAsScopingAction_WhenWithinActionScope()".





The main reason I don't like this approach is that it places the result in the middle and the context last. I think it's more natural to think of context in the middle and the result last. For example, I think it's more natural to state "_if_ the car is exceeding the speed limit, _then_ it becomes eligible to receive a ticket" rather than "a car becomes eligible to receive a ticket _if_ it exceeds the speed limit". This is personal opinion (like the rest of this post!); I just prefer "if/then" rather than "then/if".





Another reason is that it does seem to emphasize testing behavior rather than state. It is, however, more object-oriented than option 1.



## Option 3 - Subject/Scenario/Result



Described [here](http://blog.codeville.net/2009/08/24/writing-great-unit-tests-best-and-worst-practises/), this approach seems to combine a data-oriented mindset with placing the result last.





The "Subject" is the item under test. The "Scenario" is the context of the test. The "Result" is the expected result of the test.





Original example: "ProductPurchaseAction_IfStockIsZero_RendersOutOfStockView()".





Applied example: "Current_FromInsideAction_IsActionDispatcherForThatAction()".





This naming convention is more data-centered (note the "ProductPurchase_Action_" subject), so it should work well with MVVM-based designs. If "subject" could be interpreted as "object method" as well, then this approach could be called more object-oriented.





In short, this option is a more readable and more object-oriented version of the first option. I've decided to go forward with this one for my project; your mileage may vary.

