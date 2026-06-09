---
description: Know some of the terminologies of Audio
layout:
  width: default
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

# Audio Terminology

## Comb Filtering

If the same sound is played repeatedly in a very short period. It may cause a quality loss or unexpected behavior due to the nature of Comb Filtering.

Listen to the sound between 1ms to 20ms

{% embed url="https://w.soundcloud.com/player/?auto_play=false&color=%23ff5500&hide_related=false&show_comments=true&show_reposts=false&show_teaser=true&show_user=true&url=https://api.soundcloud.com/tracks/678416055?secret_token=s-xZ9CH" fullWidth="false" %}

This article: [THE BASICS ABOUT COMB FILTERING (AND HOW TO AVOID IT)](https://www.dpamicrophones.com/mic-university/the-basics-about-comb-filtering-and-how-to-avoid-it) provides further details.

[Click here to see how BroAudio addresses this issue](technical-details.md#preventing-comb-filtering)

## Clipping

Clipping is a distortion that occurs when a signal exceeds the maximum capacity of the system handling it. The exceeded signal will be cut off (data would be disregarded in a digital system), leading to harsh distortion. Generally, the maximum capacity is 0 dBFS in a digital audio system. [Clipping(audio) Wikipedia](https://en.wikipedia.org/wiki/Clipping_\(audio\))

<figure><img src="https://upload.wikimedia.org/wikipedia/commons/thumb/4/4e/Clipping.svg/512px-Clipping.svg.png" alt=""><figcaption><p>Clipping of a digital waveform. by <a href="https://commons.wikimedia.org/wiki/User:Gutten_p%C3%A5_Hemsen">Gutten på Hemsen</a></p></figcaption></figure>

## Low Pass Filter

Allows signals below a specified frequency to pass through while attenuating those above the specified frequency. **It can cause the sound to become relatively Muffled or Muddy.**\
**🔔** In Unity, If the specified frequency is 22000Hz, it is the same as bypassing the filter.

## High Pass Filter

The opposite of a Low Pass Filter.  **It can cause the sound to become relatively Thin and Crispy.**\
**🔔** In Unity, If the specified frequency is 20Hz, it is the same as bypassing the filter.

## **Real Voices and Virtual Voices**

In every software and hardware system, the number of sounds (or voices) that can be played and be audible simultaneously is limited by the **"Real Voices"** coun&#x74;**.**  When the number of sounds being played exceeds this limit, the excess sounds are converted into '**Virtual Voices**'. These virtual voices are inaudible during playback, but their playback process continues in the background. As soon as any of the real voices currently playing finishes, the corresponding virtual voice immediately becomes a real voice, thus becoming audible and continues to play until its end.

For a more comprehensive understanding, I highly recommend the following article.

{% embed url="https://gamedevbeginner.com/unity-audio-optimisation-tips/#voice_limits" %}
