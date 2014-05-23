---
layout: post
title: "Exception Types"
---
A while ago, Eric Lippert wrote an excellent blog entry called [Vexing Exceptions](http://www.webcitation.org/5xQLUxwF3). He defines four categories of exceptions, along with recommendations of how to handle them. I summarized this information into a Word document which I've printed out and posted at my desk:

[Download](/assets/ExceptionTypes.docx)

I've also started to use Mr. Lippert's terminology in my regular work, and I see it becoming more common in the programming communities. A brief summary of the terminology is below:

- Fatal - exceptions that you can't prevent and cannot handle in a reasonable manner, e.g., out of memory, thread aborted.
- Boneheaded - exceptions that are bugs in the code, e.g., argument null, index out of range.
- Vexing - exceptions thrown in non-exceptional situations, e.g., parsing errors.
- Exogenous - exceptions due to external influences, e.g., file not found.

The question of what exactly constitutes a "non-exceptional situation" is not addressed; this is an age-old debate.

Eric Lippert's post is mainly concerned with client-side code; that is, how to handle the different exception categories. When I write code, I always strive to write it as if it were going to be a library (I find that a little thought about API design goes a long way towards code reusability, even if the code never becomes an actual _library_). Therefore, in my Word document, I added _design_ recommendations for each exception category as well.

