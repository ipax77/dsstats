# Direct Strike Fan Project

Fan Project for analyzing [Starcarft II](https://starcraft2.com) [Direct Strike](https://www.patreon.com/Tya) Replays

## Install instructions

[<img src="images/store.png" alt="store" width="100"/>](https://apps.microsoft.com/detail/9nnnmb503hn5)

Go to the Microsoft Store and install the [sc2dsstats app](https://apps.microsoft.com/detail/9nnnmb503hn5) for free.

The app can be used offline to analyze your stats locally. If you want to upload your replays and contribute to this site, you must give your explicit consent.

The app is distributed via the Microsoft Store to make installation as easy as possible. It is and always will be freely available.

## Website
[dsstats.pax77.org](https://dsstats.pax77.org)

![stats](/images/cmdrstats.png)
![details](/images/playerstats.png)
![replay](/images/replay.png)

## Projects

- **[Server](/src/server)** — Backend database and API (https://dsstats.pax77.org).
- **[Maui](/src/maui)** — .NET MAUI Blazor Hybrid desktop client (Microsoft Store).
- **[mydsstats](/src/mydsstats)** — Blazor WASM PWA for browser-based replay uploads (https://mydsstats.pax77.org).
- **[Service](/src/service)** — Windows Service using the WiX Toolset (deployed to https://github.com/ipax77/dsstats.service)


# Contributing

We really like people helping us with the project. Nevertheless, take your time to read our contributing guidelines [here](./CONTRIBUTING.md).

## ChangeLog

<details open="open"><summary>v3.0.1</summary>

>- Fix maui save config error

</details>

<details open="open"><summary>v3.0.0</summary>

>- Complete rewrite/upgrade to .NET 10
>- RatingType 'All' includes all Direct Strike games (1v1, brawl, ...)

</details>