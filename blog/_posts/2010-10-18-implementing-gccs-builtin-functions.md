---
layout: post
title: "Implementing GCC's Builtin Functions"
---
GCC has a number of [useful builtin functions](http://gcc.gnu.org/onlinedocs/gcc-4.3.2/gcc/Other-Builtins.html), which translate directly to the appropriate assembly instruction if the processor supports it. A certain algorithm I was coding made use of a few of these: __builtin_ffs (find first set bit), __builtin_clz (count leading zero bits), and __builtin_ctz (count trailing zero bits).



In theory, if the target processor does not support these instructions (like mine), then the gcc library for that target should implement them in software. Unfortunately, mine did not.



The solution was surprisingly simple: I just had to implement the [expected functions](http://gcc.gnu.org/onlinedocs/gccint/Integer-library-routines.html) myself. The mapping is fairly obvious (e.g., __builtin_clz is implemented by __clzsi2).



By adding the following code to the project, I was able to build the algorithm using __builtin_clz, __builtin_ctz, and __builtin_ffs:




// Returns the number of leading 0-bits in x, starting at the most significant bit position.
// If x is zero, the result is undefined.
int __clzsi2(unsigned x);
int __clzsi2(unsigned x)
{
  // This uses a binary search (counting down) algorithm from Hacker's Delight.
   unsigned y;
   int n = 32;
   y = x >>16;  if (y != 0) {n = n -16;  x = y;}
   y = x >> 8;  if (y != 0) {n = n - 8;  x = y;}
   y = x >> 4;  if (y != 0) {n = n - 4;  x = y;}
   y = x >> 2;  if (y != 0) {n = n - 2;  x = y;}
   y = x >> 1;  if (y != 0) return n - 2;
   return n - x;
}

// Returns the number of trailing 0-bits in x, starting at the least significant bit position.
// If x is zero, the result is undefined.
int __ctzsi2(unsigned x);
int __ctzsi2(unsigned x)
{
  // This uses a binary search algorithm from Hacker's Delight.
  int n = 1;
  if ((x & 0x0000FFFF) == 0) {n = n +16; x = x >>16;}
  if ((x & 0x000000FF) == 0) {n = n + 8; x = x >> 8;}
  if ((x & 0x0000000F) == 0) {n = n + 4; x = x >> 4;}
  if ((x & 0x00000003) == 0) {n = n + 2; x = x >> 2;}
  return n - (x & 1);
}

// Returns the index of the least significant 1-bit in x, or the value zero if x is zero.
// The least significant bit is index one.
int __ffsdi2 (unsigned x);
int __ffsdi2 (unsigned x)
{
  return (x == 0) ? 0 : __builtin_ctz(x) + 1;
}


Presumably, this same approach would work for the many other GCC builtins.

