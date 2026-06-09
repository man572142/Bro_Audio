---
description: enum
---

# BroAudioType

_**This enums use \[System.Flags] attribute, so it could be used as a combination.**_

For example, BroAudio.SetVolume(BroAudioType.Music | BroAudioType.UI, 0.5f)&#x20;



### Values

* None = 0
* Music = 1
* UI = 2
* Ambience = 4
* SFX = 8
* VoiceOver = 16
* All = Music | UI | Ambience | SFX | VoiceOver
