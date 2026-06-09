---
cover: >-
  https://images.unsplash.com/photo-1604079628040-94301bb21b91?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHw5fHxjb2xvcnxlbnwwfHx8fDE3MTM3NzA1NzF8MA&ixlib=rb-4.0.3&q=85
coverY: 0
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

# Customization

In this section, we will discuss the preference settings in BroAudio. Locate to _<mark style="color:orange;">**Tools > BroAudio > Preferences**</mark>_ and open the editor window. You will see a tab view just like the one below.

{% tabs %}
{% tab title="🎵Audio" %}
**Global Playback Group**\
For assigning a [playback group](playback-group.md) at the system level to act as the base fallback for all groups/rules.

**Audio Filter Slope**\
Determines the default value of the attenuation per octave that the HighPass/LowPass applies to. The higher the value it set, the more frequencies are attenuated by the filter.

more info will be explained in [Technical Details-Audio Filter Design](../reference/technical-details.md#the-audio-filter-design-in-broaudio)

**Update Mode**\
Determines whether audio processing, including volume changes, effects, and playback logic, is affected by [Time.timeScale](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Time-timeScale.html).

### BGM

**Always Play Music As BGM**\
If enabled, every entity with BroAudioType.Music will be played by the [MusicPlayer](audio-player/music-player.md).

**Default Transition**\
If "Always Play Music As BGM" is enabled, this option will appear, allowing you to set the transition type that the entity with BroAudioType.Music applies when it is played.

### **Default Easing**

The default easing function applies to the clip's Fade-In and Fade-Out.

### **Seamless Loop Easing**

The default easing function applies to the clip's Fade-In and Fade-Out while using a Seamless Loop.

### **Project Settings**

**Max Real Voices**\
The Max Real Voices count the current project set. It's the same as in [Edit/Project Settings/Audio](https://docs.unity3d.com/Manual/class-AudioManager.html). \[Read-Only]

**Bro Virtual Tracks**\
Indicates the number of tracks utilized as backup tracks for virtual voices within the BroAudioMixer. This is \[Read-Only] for now.

**Auto-adding tracks to match audio voices**\
This button becomes available when BroAudioMixer has fewer tracks than **Max Real Voices + Bro Virtual Tracks**. Clicking the button will prompt BroAudioMixer to duplicate the last track until the total track count matches the target count (Max Real Voices + Bro Virtual Tracks).
{% endtab %}

{% tab title="🎨GUI" %}
**Show VU Color On Volume Slider**\
Display a VU bar underneath the volume slider to indicate the sound level by a gradient color.

**Show Master Volume On Clip List Header**\
Display the master volume slider on the clip list's header in the LibraryManager.

**Show Audio Type On SoundID**\
Display a label to indicate the BroAudioType of an entity(soundID) on the inspector.

### **AudioType Color**

Customize the label color of each BroAudioType, the options become available when **Show Audio Type On SoundID** is enabled.

### **Displayed Properties**

<mark style="background-color:green;">Determines which entity's property will be displayed in the</mark> [<mark style="background-color:green;">LibraryManager</mark>](library-manager/) <mark style="background-color:green;">by BroAudioType.</mark> This allows you to hide some properties that may not need to be set, focusing on those essential for that AudioType

For example, [Music](../reference/api-documentation/enums/broaudiotype.md) is often played "globally", meaning it's not tied to a specific spatial location. Therefore, no Spatial Setting is necessary. Additionally, the Pitch is typically left at its default setting, so it's not needed either."
{% endtab %}

{% tab title="🔘Misc" %}
### Asset Output Path

When an audio asset is created, it is saved to this path. You can change it by clicking this option.

{% hint style="info" %}
This option is also displayed in the LibraryManager window if the path is unavailable.
{% endhint %}

### Initialize Bro Audio Manually

Enable this option if you want to initialize Bro Audio manually using the `BroAudio.Init()` method.

When enabled, this toggle automatically adds or removes the `BroAudio_InitManually` define from the [**Scripting Define Symbols**](https://docs.unity3d.com/2022.3/Documentation/Manual/CustomScriptingSymbols.html) in the Project Settings.

### Add Dominator Track

Determines how many DominatorPlayer can be played simultaneously. These tracks are directly connected to the "Master", without being affected by the "Main" track and any audio effects.

### Reset To Factory Settings

Click to reset all preference settings to their defaults.
{% endtab %}
{% endtabs %}
