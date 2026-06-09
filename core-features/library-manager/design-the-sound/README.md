---
description: How a sound would be played and how it will behave is all set in Entity.
cover: >-
  https://images.unsplash.com/photo-1680026548022-e76f693d0a62?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHw0fHx3YXZlZm9ybXxlbnwwfHx8fDE3MDk4ODYxMzJ8MA&ixlib=rb-4.0.3&q=85
coverY: 171
---

# Design The Sound

## Name

Click the text field to name the entity when it's unfolded.&#x20;

🔔The name will be used in the SoundID dropdown menu, so a meaningful name is recommended.

## Bro Audio Type

The AudioType of the entity, it can be utilized by the API such as SetVolume(AudioType) or SetEffect(AudioType) ...etc. Choose the type that suits your needs.

🔔Changing the AudioType will re-generate a new SoundID, which means the reference to this entity that you've saved in your scene or scriptable object will be gone.

{% hint style="success" %}
All settings and features in LibraryManager can be utilized without the need for any coding or API usage. Such as [randomization](randomization.md), [fading](fade-in-out-and-cross-fade.md), [seamless loop](./#looping)...etc.
{% endhint %}

{% tabs %}
{% tab title="🎵Clips" %}
### Clip List

The list that stores all the Audio Clips. Each clip can have different parameter settings, and they will be picked by the PlaybackMode setting when it's played at runtime. The parameters will be shown when one of the clips is selected.

### PlayMode

When an entity is played, it will follow the rule of the following options to pick an Audio Clip.

1. **Single:** always play the first AudioClip.
2. **Sequence:** The first playback will be at index 0, the second will be at index 1, and so on. After playing the last AudioClip, the next playback will return to index 0.
3. [**Random**](randomization.md#audio-clips)**:** It will randomly select one AudioClip based on its weight. Probability is calculated as _<mark style="background-color:green;">Weight/Total sum of all weights.</mark>_
4. [**Shuffle**](randomization.md#audio-clips)**:** Plays audio clips randomly, ensuring that no clip is repeated until all clips in the list have been played.
5. [**Velocity**](velocity.md)**:** Play clip by the specified number.
6. [**Chained**](chained-playback.md): Plays a start sound when triggered, then loops a base sound. An end sound is played when stopped.

🔔 if there is only one AudioClip, the options won't show, and it will use **Single** mode

### Clip Parameters

**Volume**\
The audio volume that the clip will be played. More details on the [Volume ](volume-control.md)page.

#### Playback Position (in seconds)

**Start:** The starting point of the clip.\
**End:** The ending point of the clip.\
**In:** The duration time of the fade-in process.\
**Out:** The duration time of the fade-out process.\
**Delay:** The duration time before the entity plays.

{% hint style="success" %}
The playback position can be controlled by dragging the icon on the waveform GUI, and the green guiding line could help you see the playback flow more clearly.
{% endhint %}
{% endtab %}

{% tab title="✨Overall" %}
### Master Volume

The audio volume that the entity will be played. It is useful when using multiple clips in an entity. More details on the [Volume](volume-control.md) page.

✅This setting supports 🎲 [Randomization](randomization.md)! Click the \[RND] button to set its random range.

### Pitch

The pitch of the entity. Currently, it is the same as AudioSource's pitch, which is achieved through time stretching. The higher the pitch is set, the more it is shortened.

✅This setting supports 🎲 [Randomization](randomization.md)! Click the \[RND] button to set its random range.

### Priority

The same as AudioSource's priority. [see Unity's Doc](https://docs.unity3d.com/Manual/class-AudioSource.html)

### Looping

Let the entity automatically replay when it ends. More details in [Seamless Loop](seamless-loop.md).

### Spatial

The same as AudioSource's 3D Sound Settings ([see Unity's Doc](https://docs.unity3d.com/Manual/class-AudioSource.html)), but it's optional instead of built-in in all entities. If the sound needs positioning, then click the \[Create And Open] button to create a spatial setting asset and open its editor window.

_Tips:_ \
_Sounds that might not needed: BGM, Ambience, UI, VoiceOver._ \
_Sounds that might needed: SFX_
{% endtab %}
{% endtabs %}

{% hint style="info" %}
**Alt** + **Left-Click** on the foldout icon of any entity can expand/collapse all the entities quickly.
{% endhint %}

