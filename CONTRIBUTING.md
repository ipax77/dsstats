# Welcome to dsstats contributing guide

Thank you for investing your time in contributing to our project!

Read our [Code of Conduct](./CODE_OF_CONDUCT.md) to keep our community approachable and respectable.

In this guide you will get an overview of the contribution workflow from opening an issue, creating a PR, reviewing, and merging the PR.

## New contributor guide

To get an overview of the project, read the [README](README.md). Here are some resources to help you get started with open source contributions:

- [Set up Git](https://docs.github.com/en/get-started/quickstart/set-up-git)
- [GitHub flow](https://docs.github.com/en/get-started/quickstart/github-flow)
- [Collaborating with pull requests](https://docs.github.com/en/github/collaborating-with-pull-requests)
- [Contributing](https://gist.github.com/MarcDiethelm/7303312)


## Getting started

There are two main projects in this repository:
- .NET MAUI Blazor app (Windows only) [Set up guid](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/tutorials/maui?view=aspnetcore-7.0&pivots=windows)
    * [sc2dsstats.maui](./src/sc2dsstats.maui)
- Hosted Blazor WebAssembly
    * [pax.dsstats.web](./src/pax.dsstats.web)

These projects share most of the other libraries. While the MAUI app should run out of the box you need a local mysql db set up for the Hosted Blazor WebAssembly project.

## Pull requests (PR)

- Please file an issue before you start.
- Target the 'dev' branch for new pull requests.
