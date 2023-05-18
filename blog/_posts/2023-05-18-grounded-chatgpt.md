---
layout: post
title: "Grounded ChatGPT"
description: "Grounding ChatGPT on your own data to prevent hallucination and enable source references."
---

So, there's this thing you may have heard of called ChatGPT. A lot of people (myself included) have thought "OK, nice toy. It's pretty good at producing human-sounding text. But I want to run it *on my own data* without becoming a data scientist and spending a few hundred thousand dollars in training costs."

Then someone pointed out to me there's already a technique for this called Retrieval Augmented Generation, and in fact there's some [sample code right there](https://github.com/Azure-Samples/azure-search-openai-demo) showing how to do it.

## Retrieval Augmented Generation

To save you a Google search (or ChatGPT query?), here's my super-simple description of this technique: when the user asks a question, instead of just giving it to ChatGPT directly, first do a *search* for that question over your own data, and combine the search results *along with* the user's question as the ChatGPT input.

This technique "grounds" ChatGPT, giving it your own data alongside the user's question. If you structure your input properly, you can influence ChatGPT to produce relevant results, even including source references. With this technique, ChatGPT is able to produce much better results, without the need for training or even fine-tuning the model itself.

## Sample Code

The official sample referenced above is in Python. And I love Python. As a language, I mean. But it's been... um... 25 years or so since I've used it. Definitely rusty. So I decided to write my own sample (heavily influenced by the official one) in C#. And using local Docker containers as much as possible instead of creating a bunch of Azure resources.

You can find my [C# Retrieval Augmented Generation code on GitHub](https://github.com/StephenCleary/grounded-chatgpt). It's not production-ready, but it gets the general point across. You can use it pretty easily to "teach" ChatGPT about modern events or your own custom data. It uses Elasticsearch and Seq (both in local Docker containers), preserving its data in local Docker volumes. And it has exhaustive logging out of the box, so you can always review what APIs were called and how exactly they work. My code does use the Azure OpenAI API to talk to ChatGPT, but everything else is in local Docker containers.

## More Implementation Details

When you use this sample code to do a retrieval-augmented generation, what actually happens is this:

The user's question is sent to ChatGPT to extract search keywords, using this template:

```
Below is a question asked by the user that needs to be answered by searching.
Generate a search query based on names and concepts extracted from the question.

### Question:
{question}

### Search query:
```

ChatGPT is pretty good at generating a search query from a user question; I set the `temperature` to zero to ensure there's no randomness in this call.

Next, this ChatGPT response is sent to Elasticsearch (just as a [simple query string](https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-simple-query-string-query.html)).

The results of the Elasticsearch search are then formatted and injected into a ChatGPT prompt that looks like this:

```
Answer the following question. You may include multiple answers, but each answer may only use the data provided in the References below.
Each Reference has a number followed by tab and then its data.
Use square brakets to indicate which Reference was used, e.g. [7]
Don't combine References; list each Reference separately, e.g. [1][2]
If you cannot answer using the References below, say you don't know. Only provide answers that include at least one Reference name.
If asking a clarifying question to the user would help, ask the question.
Do not comment on unused References.

### References:
{sources}
```

The result is then post-processed to extract the quoted references and change them to hyperlinks.

## Have Fun!

I've been pretty pleased with the results, even though I'm using very simplistic source processing, and a lexical search instead of a more proper semantic/vector search. Even with those limitations, the results are pretty impressive!

That's all I have to say for now. Have fun!
