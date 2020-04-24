---
layout: post
title: "Using GitHub Actions for Pull Request Staging Environments, Part 3: Implementing Deploy and Teardown"
series: "Using GitHub Actions for Pull Request Staging Environments"
seriesTitle: "Implementing Deploy and Teardown"
description: "Using Surge.sh from a GitHub Action Workflow to create and tear down staging environments for pull requests."
---

## Implementing Deploy

So far, we have a `slash-commands` GitHub Action that translates `/deploy` and `/teardown` ChatOps commands into `repository_dispatch` events.

To handle the `/deploy` slash command, we'll need to handle a `repository_dispatch` event of type `deploy-command`. In your GitHub repository, open up the Actions tab and choose "New workflow" and then "Set up a workflow yourself". Name the file `deploy-command.yml` and paste this in:

{% raw %}
```yaml
# Inputs:
#  client_payload.pull_request.number - PR number
#  client_payload.pull_request.head.sha - PR SHA

name: Create PR Staging Environment

on:
  repository_dispatch:
    types: [deploy-command]

# Set environment variables available to all action steps.
env:
  DOMAIN: ${{ format('{0}-{1}-pr{2}.surge.sh', github.event.repository.owner.login, github.event.repository.name, github.event.client_payload.pull_request.number) }}

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v2
        with:
          ref: ${{ github.event.client_payload.pull_request.head.sha }}

      - name: Install dependencies
        run: npm ci

      - name: Build static site
        run: npx gatsby build

      - name: Publish to surge.sh
        run: npx surge ./public ${{ env.DOMAIN }}
        env:
          SURGE_LOGIN: ${{ secrets.SURGE_LOGIN }}
          SURGE_TOKEN: ${{ secrets.SURGE_TOKEN }}

      - name: Add comment to PR
        uses: peter-evans/create-or-update-comment@v1
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          issue-number: ${{ github.event.client_payload.pull_request.number }}
          body: |
            ${{ format('Published to [staging environment](https://{0})', env.DOMAIN) }}

            To teardown, comment with the `/teardown` command.
```
{% endraw %}

This one's a bit longer, so let's walk through the steps.

The first step checks out the repository. Note that it specifically checks out the SHA of the pull request (`client_payload.pull_request` is provided by `slash-command-dispatch`). So we're checking out the code *for that PR*.

{% raw %}
```yaml
      - name: Checkout
        uses: actions/checkout@v2
        with:
          ref: ${{ github.event.client_payload.pull_request.head.sha }}
```
{% endraw %}

The next couple steps build the site by running `npm ci` and `npx gatsby build`. Just like building locally, the output is placed in the `public` folder.

The publish step runs {% raw %}`npx surge ./public ${{ env.DOMAIN }}`{% endraw %}. This time we're running `surge` and giving it the name of the domain we want to publish to. The `DOMAIN` environment variable was defined earlier in the file:

{% raw %}
```yaml
env:
  DOMAIN: ${{ format('{0}-{1}-pr{2}.surge.sh', github.event.repository.owner.login, github.event.repository.name, github.event.client_payload.pull_request.number) }}
```
{% endraw %}

What's really nice about this setup is that every pull request gets a different domain - and thus a different staging environment.

`SURGE_LOGIN` and `SURGE_TOKEN` are additional environment variables used by the `surge` command line so it authenticates under your account while deploying.

The last step adds a comment to the pull request with a clickable link for the deployed staging environment:

{% raw %}
```yaml
      - name: Add comment to PR
        uses: peter-evans/create-or-update-comment@v1
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          issue-number: ${{ github.event.client_payload.pull_request.number }}
          body: |
            ${{ format('Published to [staging environment](https://{0})', env.DOMAIN) }}

            To teardown, comment with the `/teardown` command.
```
{% endraw %}

This ends up looking like this:

{:.center}
[![]({{ site_url }}/assets/github-actions-deploy.png)]({{ site_url }}/assets/github-actions-deploy.png)

### Set Up Surge Secrets

There are a couple of new secrets used by the deploy action: `SURGE_LOGIN` and `SURGE_TOKEN`. These can be added as [repository secrets](https://help.github.com/en/actions/configuring-and-managing-workflows/creating-and-storing-encrypted-secrets) just like last time.

Set `SURGE_LOGIN` to the email address [you used to sign up with Surge](2020-04-02-github-actions-pull-request-staging-environments-part-1).

To get `SURGE_TOKEN`, run `surge token` from your own computer. This will give you a token that you can save in the `SURGE_TOKEN` secret, so your deployments are associated with your Surge account.

## Try It Out!

At this point, you should be able to create a pull request and then add a `/deploy` comment on it. Check out the Actions tab of the repository to watch your actions run or see the logs for old action runs.

## Implementing Teardown

The next step is to implement teardown. In your GitHub repository, open up the Actions tab and choose "New workflow" and then "Set up a workflow yourself". Name the file `teardown-command.yml` and paste this in:

{% raw %}
```yaml
# Inputs:
#  client_payload.pull_request.number - PR number

name: Delete PR Staging Environment

on:
  repository_dispatch:
    types: [teardown-command]

env:
  DOMAIN: ${{ format('{0}-{1}-pr{2}.surge.sh', github.event.repository.owner.login, github.event.repository.name, github.event.client_payload.pull_request.number) }}

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - name: Teardown surge.sh
        run: npx surge teardown ${{ env.DOMAIN }}
        env:
          SURGE_LOGIN: ${{ secrets.SURGE_LOGIN }}
          SURGE_TOKEN: ${{ secrets.SURGE_TOKEN }}

      - name: Add comment to PR
        uses: peter-evans/create-or-update-comment@v1
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          issue-number: ${{ github.event.client_payload.pull_request.number }}
          body: ${{ format('Tore down {0}', env.DOMAIN) }}
```
{% endraw %}

This one is pretty simple; we use a similar pattern to `/deploy` but there are fewer steps since there's no build (or even a checkout). We use the same pattern for defining `DOMAIN` and the Surge secrets, and then we run {% raw %}`npx surge teardown ${{ env.DOMAIN }}`{% endraw %} to tear down the environment for this pull request. The last step adds a comment to the PR indicating that its staging environment has been torn down.

## Who Can Issue Commands?

By default, only developers with write access to your repository can issue slash commands. This is the default behavior of `slash-command-dispatch`. So if this is just your project, then only you can create or tear down staging environments. If you have an open-source project - and if creating staging environments is cheap for you - you can edit the `slash-commands.yml` file and add a `permission` argument to `slash-command-dispatch` with the value `none`. That way, *anyone* would be able to create and tear down staging environments.

## Next Time

ChatOps are cool. But can we automate this further?