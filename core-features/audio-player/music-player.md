---
cover: >-
  https://images.unsplash.com/photo-1526394931762-90052e97b376?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHwyfHxtdXNpYyUyMHBsYXllcnxlbnwwfHx8fDE3MTI4ODk0NDF8MA&ixlib=rb-4.0.3&q=85
coverY: 529
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

# Music Player

## Introduction

MusicPlayer allows you to seamlessly switch between different BGMs effortlessly. Whenever players enter a new scene or trigger a specific plot point, you can instantly play new music using a new [SoundID](../library-manager/#entity) without needing to know which one was playing before.&#x20;

The key differences between MusicPlayer and standard players are:

1. Only one Entity(SoundID) is played at a time (except during transitions).
2. It automatically transitions from the previous one to the new one, and the previous one will stop after the transition is complete.

## How To Use?

For the no-code approach, there is an option in _<mark style="color:orange;">**Tools > BroAudio > Preferences**</mark>_ named "Always Play Music As BGM". if it's enabled, all entities with audio type: [Music](../../reference/api-documentation/enums/broaudiotype.md) will apply this feature with the transition setting specified under this option.&#x20;

However, the recommended way is to implement this feature via code for better flexibility and maintainability.

### .AsBGM()

Play the music using the basic API, and add .AsBGM() after it, the sound will be played as MusicPlayer.

```csharp
BroAudio.Play(_id).AsBGM();
```

🔔The sound doesn't have to be a [BroAudioType.Music](../../reference/api-documentation/enums/broaudiotype.md), there is no restriction on this API.

### .SetTransition([Transition ](../../reference/api-documentation/enums/transition.md)transition, float fadeTime)

You can specify the transition type and the duration of the transition. If you don't set the fadeTime, the system will use the FadeIn and FadeOut settings of the entity that is defined in [LibraryManager](../library-manager/); otherwise, these settings will be ignored (or overridden). The types of transitions include:

* <mark style="color:green;">Default:</mark> FadeOut the current one, and then FadeIn the new one.
* <mark style="color:green;">Immediate:</mark> Immediately stop the current one and play the new one without any transition.
* <mark style="color:green;">OnlyFadeIn:</mark> Immediately stop the current one, then FadeIn the new one.
* <mark style="color:green;">OnlyFadeOut:</mark> FadeOut the current one, then immediately play the new one.
* <mark style="color:green;">CrossFade:</mark> Start playing the next one with FadeIn while the previous one begins to FadeOut.

The settings for fadeTime, Transition, and settings of the Entity interact with each other. The rule is:&#x20;

Unless using [Transition](../../reference/api-documentation/enums/transition.md)[.Immediat](../../reference/api-documentation/enums/transition.md)e (ignoring all fadeTime), the argument: **fadeTime** has the highest priority. If it's not set, the system defaults to the entity's FadeIn/Out settings, followed by the transition mode.

_The configurations in the following table all result in the equivalent of_ [_Transition.Immediate_](../../reference/api-documentation/enums/transition.md)_, meaning there is no transition involved. Hope this can help you understand the relationships._

| Entity FadeIn | Entity FadeOut | Transition  | FadeTime |
| ------------- | -------------- | ----------- | -------- |
| 0             | 0              | Default     | Not set  |
| 2             | 2              | CrossFade   | 0        |
| 2             | 2              | Immediate   | 2        |
| 0             | 2              | OnlyFadeIn  | Not Set  |
| 2             | 0              | OnlyFadeOut | NotSet   |

