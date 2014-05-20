---
layout: post
title: "Minor Improvements for Language Design"
---
From time to time, I find myself having to design a new computer language. The smallest of these languages only contain expressions, but some have branched out into statements. The design of languages is a fascinating topic, and one which I believe is very benificial to any programmer.

Of course, the best way to go about designing a language is to find out what worked and didn't work for other languages. For example, the [official Gawk manual](http://www.gnu.org/software/gawk/manual/gawk.html) starts its section on string concatenation with the quote "It seemed like a good idea at the time," so it's probably best not to use a space character as a string concatenation operator... Likewise, Bjarne Stroustrup started out in favor of resumption-based exception handling, but "over the next four years, I learned otherwise..." (he tells the fascinating story about Cedar/Mesa in section 16.6 of [The Design and Evolution of C++](http://www.amazon.com/gp/product/0201543303?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0201543303)).

One of the best sources for language design is the Python mailing list archives; not a single change is made to the language without being carefully scrutinized by some of the brightest minds in language design. Python, as a result, is one of the finest languages. I personally recall being positively electrified watching generator syntax evolve; and this same idea several years later was brought into C# (where they're called "iterator blocks").

However, many new languages do continue to make the mistakes of the old languages, simply because "it's always been done that way." In particular, there are three syntactical elements that I always put in my languages now, and I think they're underused in modern language design. Two of them are stolen straight from Python; one is my own invention. The next time you need to design a language, please consider these.

## Math Conditionals

It seems like everyone has a different way of writing "if (x > 3 && x < 7)". If you're a strict follower of [Code Complete](http://www.amazon.com/gp/product/0735619670/ref=as_li_tl?ie=UTF8&camp=1789&creative=390957&creativeASIN=0735619670&linkCode=as2&tag=stepheclearys-20&linkId=JWZPA42GZ3AY3CE3), that would be "if (3 < x && x < 7)". I like to allow in my languages the "math conditionals", which look like "if (3 < x < 7)". It doesn't have a big impact on your EBNF to support this. More complex math conditionals are also not difficult: "if (3 < x < y <= z < 7)".

## Indentation Defines Scope

One of Python's trademark features is the use of indentation to define scope. It seems like people either love it or hate it, and I'm in the former camp. Furthermore, it's not that hard to get working once you discover the trick of using indentation as a lexical symbol instead of whitespace.

## Forcing Parenthesis in Logical and Bitwise Expressions

OK, _anyone_ who's been programming any reasonable amount of time has been bitten by this one at least once. The logical operators (&&, ||) and bitwise operators (&, |) have had strictly defined precedence since the beginning days of C. But _why?_ Whoever writes "(x | y == 0 && !z && y & x == 1)" should be dragged out into the street and shot.

That's why my languages define all the logical operators on the same precedence level, and all the bitwise operators on the same precedence level. It does allow chaining, but only for the same operator. E.g., "(x && y && z)" is legal, but "(x && y || z)" is not; likewise, "(x | y | z)" is accepted, but "(x | y & z)" is rejected. This restriction enforces the use of parenthesis whenever the interpretation is not immediately obvious. I usually also combine the precedence levels of bitwise and logical operators, so that expressions like "(x | y && y & z)" require additional parenthesis.

Unlike the other two recommendations above, this one does have a bigger impact on the EBNF grammar, "partitioning" the set of allowed expressions. I do believe the increased complexity is totally worth it, though.

