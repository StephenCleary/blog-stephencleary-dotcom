---
layout: post
title: "Sharp Corners: IList<T>.IsReadOnly != IList.IsReadOnly"
---
Here's a "sharp corner" of sorts, though it's with the BCL rather than the C# language. It turns out that the property **IsReadOnly** changed meanings from **IList** to **IList<T>**. As of this writing, it's unclear whether this change in meaning was intentional; the MSDN documentation for both properties is identical (and ambiguous).

[TestMethod]
public void IListOfT_IsReadOnly_IsDifferentThan_IList_IsReadOnly()
{
    int[] array = new[] { 13 };
    IList<int> generic = array;
    System.Collections.IList nongeneric = array;

    Assert.AreNotEqual(generic.IsReadOnly, nongeneric.IsReadOnly);
}
