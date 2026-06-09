---
cover: >-
  https://images.unsplash.com/photo-1597243508456-d8cb4178de17?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHw1fHxleHBsb3Npb258ZW58MHx8fHwxNzA5ODc3NzM5fDA&ixlib=rb-4.0.3&q=85
coverY: -352
layout:
  width: default
  cover:
    visible: true
    size: full
  title:
    visible: true
  description:
    visible: false
  tableOfContents:
    visible: true
  outline:
    visible: true
  pagination:
    visible: true
  metadata:
    visible: true
  tags:
    visible: true
  actions:
    visible: true
---

# Dominator Player

## Introduction

Dominator allows you to 'dominate' other sounds while playing it, meaning Dominator will play in a normal way, while other sounds will be affected by the effects you specify until the Dominator is finished.

{% hint style="warning" %}
This feature is not supported in WebGL, [more details.](../../overview/compatibility.md#webgl)
{% endhint %}

## How To Use?

### .AsDominator()&#x20;

Common use cases and API examples:

#### When an explosion occurs nearby, other sounds become temporarily unclear, but the tinnitus, heartbeat, or breathing remain clear.

```csharp
BroAudio.Play(_explosion).AsDominator().LowPassOthers(_lowPassFrequency); 
BroAudio.Play(_tinnitus).AsDominator().LowPassOthers(_lowPassFrequency); 
```

#### When the theme song plays,  lower the volume of all other sounds.

```csharp
BroAudio.Play(_deathStrandingAwesomeMusic).AsDominator().QuietOthers(othersVol);
```

[**Go to view all API**](../../reference/api-documentation/interface/iplayereffect.md)

### Side Note:

Even though this feature is straightforward and effective, it might not cover all complex situations. For these, you can individually apply the SetEffect to specific sounds or sound categories. Essentially, this feature is built upon using SetEffect.



