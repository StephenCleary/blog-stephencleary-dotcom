---
layout: post
title: "The Border Case of Abs"
---
Things have been busy here, adjusting to two children in the house and trying to hold down two jobs! One problem I ran into on my day job was a boundary condition that I'd never seen before...

Long story short: it turns out that the result of **abs(0x80000000)** is undefined in C (in C#, **Math.Abs((int)0x80000000)** throws an **OverflowException**). In the C library used by my firmware, **abs(0x80000000)** is actually a negative number (!).

In my case, this caused the wrong logic path to be taken; the call to **abs** was in an expression like **if (abs(large_unsigned - smaller_unsigned) < (signed_value))**. Another programmer had added the call to **abs** because the compiler was complaining about comparing a signed integer to an unsigned integer. As it turns out, the compiler would do an unsigned comparison when it gave that warning (which was correct). By adding the **abs**, the programmer had introduced a very subtle bug: whenever the difference between **large_unsigned** and **smaller_unsigned** was _exactly_ 0x8000000, the result of **abs** would be negative, causing the branch to be taken when it shouldn't be.

I removed the **abs** (and cast **signed_value** to **unsigned** to - correctly - get rid of the compiler warning). All told, it was a rather expensive mistake: two failures were seen at a customer site, and we had to set up a test bench here with several people working for several days just to find the problem.

Lessons learned:

- Don't "just make the compiler shut up". I'm a big proponent of warning-free code, but the correct way to get there is to first _understand_ the warning, and _then_ correct the code.
- There is an interesting boundary condition around **abs**. I've already searched through the rest of the source for similar occurrences. :)
