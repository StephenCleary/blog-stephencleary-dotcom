---
layout: post
title: "Using GitHub Actions for Pull Request Staging Environments, Part 2: Slash Commands"
series: "Using GitHub Actions for Pull Request Staging Environments"
seriesTitle: "Slash Commands"
description: "Using GitHub Actions to dispatch ChatOps slash commands on pull requests."
---

## Slash Commands

I'm not very familiar with the *term* "ChatOps", but I've seen ChatOps actually *used* a lot. The idea is that you can set up chat bots to listen to your team's chat and take actions based on commands you can type in the chat.

What we'll be setting up here is pretty close to that; we want to be able to add a comment to a PR with a "slash command" that will do our deploy (or teardown) for us. Specifically, we'll be using:

- `/deploy` to deploy a PR to its staging environment.
- `/teardown` to tear down a PR staging environment when we're done with the PR.

## Dispatching Slash Commands

The way we'll be setting this up is to have one GitHub Action that listens for PR comments and decides if they have any slash commands. For any slash commands, we want to dispatch an event to our repository.

In your GitHub repository, open up the Actions tab and choose "Set up a workflow yourself". Name the file `slash-commands.yml` and paste this in:

{% raw %}
```yaml
# Translates slash-commands in issue comments to repository-dispatch events.

# Name of the action (displayed in the Actions tab)
name: Slash command dispatch

# Triggers for this action.
#  This one only runs when a comment is added to an issue.
#  (on GitHub, pull requests are one kind of "issue")
on:
  issue_comment:
    types: [ created ]

# When the trigger fires, we run these jobs.
jobs:
  dispatch: #  We just have one job, called "dispatch"
    runs-on: ubuntu-latest # The OS we run on. Doesn't really matter for this simple action.
    steps: # This job only has one step, called "Dispatch slash command"
      - name: Dispatch slash command
        uses: peter-evans/slash-command-dispatch@v1 # Uses a pre-built action from the Marketplace
        with: # These are the parameters passed to the action
          token: ${{ secrets.DISPATCH_TOKEN }} # This action needs a personal access token in order to dispatch
          reactions: false # By default, this action will add reactions to the slash command comment; this turns those off
          issue-type: pull-request # We only want to look for slash commands in pull requests, not other issues
          commands: deploy, teardown # The slash commands we look for: /deploy and /teardown
```
{% endraw %}

In this case, our GitHub Action is simple; [`slash-command-dispatch`](https://github.com/marketplace/actions/slash-command-dispatch) is specifically designed for matching slash commands in issue and/or PR comments, and dispatching a command to the repository.

Note that we're passing a `token` to this GitHub Action, and we're taking the value from `secrets.DISPATCH_TOKEN`. We don't have that secret yet, so let's set that up now. In order to dispatch, `slash-command-dispatch` needs a token with write access to the repository. You can get one by [following the GitHub directions](https://help.github.com/en/github/authenticating-to-github/creating-a-personal-access-token-for-the-command-line); when creating your token, you'll want `public_repo` scope if your repository is public - otherwise, you'll want `repo` scope. Copy that access token value once it's created.

Next, create a [repository secret](https://help.github.com/en/actions/configuring-and-managing-workflows/creating-and-storing-encrypted-secrets) named `DISPATCH_TOKEN` and paste that value in... and if you did that without doing any verification, then you just failed Security 101.

On a more serious note, right now the world of GitHub Actions (and its Marketplace) are in the "just trying to get it to work" stage. And in that stage of technology adoption, security is often overlooked. So when you're reading some blog on the Internet and it tells you to make a personal access token and paste it somewhere, you should take a step back and really think about what's going on.

## Security Concerns

At the very least, take a look at the code that's receiving the token. I'm using [`peter-evans/slash-command-dispatch`](https://github.com/marketplace/actions/slash-command-dispatch) in the example above. Does it look like an upstanding project? Good documentation? High(ish) number of stars? Not forked from a different project? Who is this "Peter Evans" and does he seem like a trustworthy person? Go ahead and open the action's repository; does the code look OK?

Any time you're passing a token to an action, you should do this kind of research, if not more. If you're not comfortable with pasting a personal access token, there are a few alternative approaches.

### Alternative Approaches

The security concerns above are due to the choice to *dispatch* the repository events rather than handling them directly, so we end up handing a token to a third-party GitHub Action.

Alternative approaches include:
- Performing a security audit of the GitHub Action and then SHA-locking to that specific version. I.e., instead of `peter-evans/slash-command-dispatch@v1`, use `peter-evans/slash-command-dispatch@8a61cc727ff2d87afea4c46b11145543bef0c02f`.
- Performing a security audit of the GitHub Action, cloning it to your own personal GitHub Action, and using that one instead.
- Writing your own GitHub Action that does essentially the same thing.
- Creating a separate GitHub account, inviting that account to your repository (in a `write` role), accepting that invitation, and using a `public_repo`/`repo` token from *that* account instead of your personal account.
  - This ensures that the token can only be used to disrupt this *one* repository, instead of *all* your repositories.
  - It does still allow write access to this repository, though.
- Handling all slash commands directly instead of dispatching.
  - You have to either combine all slash command handling into a single file (which makes your workflow file messy), or have multiple slash command handler actions (which makes your PR "checks" section messy).
  - At the time of this writing, there isn't a great GitHub Action for parsing multiple slash commands and setting [step outputs](https://help.github.com/en/actions/reference/context-and-expression-syntax-for-github-actions#steps-context) that can be used by future steps.
  - Even if such an action did exist, the resulting `slash-command.yml` file would get rather long and ugly with `if:` conditionals throughout.
  - However, this is the only alternative that is fully safe, since you would no longer require a personal access token *at all*. Because it doesn't do dispatching.

I've tried out a few alternatives, and I tend to prefer either just doing it the easy way (as done in this post), or creating a separate GitHub account (to limit the scope of a breached token to this single repository). I don't like handling all slash commands directly instead of dispatching, for reasons that will become more clear when we extend this solution to automate deploy and teardown commands (in a future post).

## Back to The Goal

The rest of this blog series assumes that you have done a sufficient security check and have stored a token in the repository secrets, named `DISPATCH_TOKEN`.

## Dispatch

The `slash-command-dispatch` action recognizes slash commands and then sends a [repository dispatch event](https://help.github.com/en/actions/reference/events-that-trigger-workflows#external-events-repository_dispatch) to the repository. `repository_dispatch` is a special event that you can listen for (with another GitHub Action) and respond to.

`slash-command-dispatch` follows a convention where the commands it listens to (`deploy` and `teardown` in our case) are sent with the `repository_dispatch` event, with a `-command` suffix. So, just like today's GitHub Action listened for an `issue_comment` event of type `created`, next time we'll write GitHub Actions that listen for a `repository_dispatch` event of type `deploy-command` or `teardown-command`.

## Next Steps

At this point, you should have a "ChatOps bot" of sorts that listens for `/deploy` and `/teardown` comments on your pull requests, and then translates those into `repository_dispatch` events. Next time we'll add handlers for those events.
