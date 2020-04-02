---
layout: post
title: "Using GitHub Actions for Pull Request Staging Environments, Part 4: Automated Deploy and Teardown"
series: "Using GitHub Actions for Pull Request Staging Environments"
seriesTitle: "Automated Deploy and Teardown"
description: "Automatically create and tear down staging environments for pull requests."
---

## Automating Deploy

We currently have slash commands `/deploy` and `/teardown` that are converted into `repository_dispatch` (`deploy-command` / `teardown-command`) events, and those `repository_dispatch` events deploy or tear down a staging environment specific to a pull request. That's pretty cool.

But what I'd *really* like is to deploy a PR's staging environment as soon as the PR is created, and automatically tear it down when the PR is closed. That would be *really* cool.

## Security (Again)

First, though, I do have to talk about security. Yes, again.

For security reasons, *all secrets are unavailable* when automating a pull request from a *fork* of a repository. This makes sense; you don't want someone to create a fork, change the actions, and be able to retrieve your secrets by opening a PR against your repository.

Currently, this is a hard and fast rule. But there are discussions about loosening up these rules a bit:
- [Opt-in to secret sharing to forks](https://github.community/t5/GitHub-Actions/Make-secrets-available-to-builds-of-forks/td-p/30678)
- [Mark certain actions as "trusted"](https://github.community/t5/GitHub-Actions/Allow-secrets-to-be-shared-with-trusted-Actions/td-p/34278)

Personally, I think it makes sense to always have pull request events run using the actions of the *base* branch, not the *head* branch. So cross-repository pull requests will always run the actions in the origin repository, not the fork repository.

However, for now, the result of this security restriction is that we can only automate *local* pull requests (from one branch to another in the same repository). The solution we're using here will not work for cross-repository PRs (i.e., a PR from a forked repository).

## Dispatch when a Pull Request is Opened

We already have an action that handles `repository_dispatch` events of type `deploy-command`, so we can just use a pull request event as a "trigger", dispatching the same kind of command:

{% raw %}
```yaml
# Queues a deploy command for every local PR.

name: Local PR Opened/Updated

# By default, this is run when a PR is opened or synchronized.
on:
  pull_request:

jobs:
  dispatch-deploy-command:
    if: github.repository == github.event.pull_request.head.repo.full_name # Only try to deploy local PRs
    runs-on: ubuntu-latest
    steps:
    - name: Dispatch /deploy Command
      uses: peter-evans/repository-dispatch@v1
      with:
        token: ${{ secrets.DISPATCH_TOKEN }} # Same security issues as before, unfortunately
        event-type: deploy-command # Send the deploy-command type for the repository_dispatch event
        client-payload: '{"pull_request": ${{ toJson(github.event.pull_request) }}}' # Pass along the pull request details
```
{% endraw %}

The only tricky part here is the last line: our `deploy-command.yml` handler expects to be able to use `client_payload.pull_request.number` and `client_payload.pull_request.head.sha`. `client_payload.pull_request` is populated automatically by `slash-command-dispatch`, but here we need to fill it out ourselves. Fortunately, the `pull_request` data is already provided to us as part of the `pull_request` event, so we just need to copy it over.

## Dispatch when a Pull Request is Closed

Similarly, we have an action that handles `repository_dispatch` events of type `teardown-command`, so we can use a pull request close event to dispatch the same command:

{% raw %}
```yaml
# Queues a teardown command for every local PR closed.

name: Local PR Closed

# Only listen for close events.
on:
  pull_request:
    types: [ closed ]

jobs:
  dispatch-teardown-command:
    if: github.repository == github.event.pull_request.head.repo.full_name # Only try to tear down local PRs
    runs-on: ubuntu-latest
    steps:
    - name: Dispatch /teardown Command
      uses: peter-evans/repository-dispatch@v1
      with:
        token: ${{ secrets.DISPATCH_TOKEN }} # Same security issues as before, unfortunately
        event-type: teardown-command # Send the deploy-command type for the repository_dispatch event
        client-payload: '{"pull_request": ${{ toJson(github.event.pull_request) }}}' # Pass along the pull request details
```
{% endraw %}

## Done

At this point, we're as automated as we can be (safely). All PRs support `/deploy` and `/teardown` commands, managing a per-PR staging environment. In addition, local PRs get their environments deployed and torn down automatically.

Hopefully in the future, we can fully automate PRs from forks. That would be especially helpful for open-source projects. In the meantime, `/deploy` and `/teardown` are still pretty cool.

As a final reminder, the staging environments in this example were deliberately simple: building a static site and deploying that static site. This simple example is fine for a lot of front-end projects. But staging environments can also include back-end code. There's no reason you can't define "deploy" to mean "deploy the front end to Surge *and* deploy the backend to a new Azure resource group" or something like that. So dream big!

Enjoy GitHub Actions!