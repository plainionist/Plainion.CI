---
title: HowTo configure IronDoc?
navigation_weight: 3
---

# HowTo configure IronDoc?

In order to let Plainion.CI generate API doc with [IronDoc](https://github.com/plainionist/Plainion.IronDoc) apply the following settings
to your build definition:

- *ApiDoc*
  - *Generator*: "\bin\Plainion.IronDoc\Plainion.IronDoc.exe"
  - *Arguments*: "-output doc\Api -assembly %1 -sources %2"



