---
description: enum
---

# Transition

### Values

* **Default**\
  FadeOut the current sound first, then FadeIn the new one.
* **Immediate**\
  Immediately stop the current sound and play the new one without any transition.
* **OnlyFadeIn**\
  Immediately stop the current one, then FadeIn the new one.
* **OnlyFadeOut**\
  FadeOut the current one, then immediately play the new one.
* **CrossFade**\
  Start playing the next one with FadeIn while the previous one begins to FadeOut.

{% hint style="info" %}
More information is in the [Music Player](../../../core-features/audio-player/music-player.md#settransition-transition-transition-float-fadetime) section.
{% endhint %}
