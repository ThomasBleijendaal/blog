---
theme: Azure Container Apps
title: Introduction
visible: True
---

This blog is going to be used as a reason to create a small application using Container Apps in Azure. I want to create a functioning blog system with several moving parts, all hosted in Azure Container Apps. These parts will probably be:

- A public internet facing container that serves all content.
- An ingest container that pre-renders all pages (written in markdown) to html, and probably some compression variants too, and rss for good measure.
- A GitHub repo hosting all markdown, and is used by the ingest container to import all stuff. I'll probably also use that repo to deploy everything.
- Some storage container to host all pre-rendered pages.

:::note
_Over-engineering_ will be the name of the game, so most things will be way too complex or even convoluted for what they really need to be.
:::

I was inspired by a post on danluu.com ([How web bloat impacts users with slow devices](https://danluu.com/slow-device/)), so I want to make this blog pretty bare bones and light, and keep the server side also very fast. The danluu website is a bit too bare bones for me, so I've added a little CSS, just to play around with grids.

[The source code for this blog is available at GitHub](https://github.com/ThomasBleijendaal/blog).
