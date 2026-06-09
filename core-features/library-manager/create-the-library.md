---
cover: >-
  https://images.unsplash.com/photo-1542208998-f6dbbb27a72f?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHwxfHxyZWNvcmQlMjBzdG9yZXxlbnwwfHx8fDE3MDk4Nzc0NzV8MA&ixlib=rb-4.0.3&q=85
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

# Create The Library

When you open the Library Manager,  you will see the two major sections of the library creation.

## Drag And Drop or Browse

This is pretty straightforward, you can bring any AudioClip into the library by drag and drop or browse. It will create an [Asset](./#asset) and one (or more) [Entity](./#entity) automatically.&#x20;

If multiple AudioClips are dropped. A confirmation window will pop up to ask you how you would like to structure them, these options are:

**1.  Multiple For Each:** Create multiple entities for each audio clip.

**2. One For All:** Create one entity and add all the audio clips to the entity's clip list.

{% hint style="info" %}
When an asset is created in this way, it is automatically named "Temp". You can start editing right away. However, if you want to use it in play mode, remember to give it an official name. You can change the name by clicking "Temp" at the top left.
{% endhint %}

## Asset List

If you have created any assets before, they will appear in the list. You can access the asset to modify its content by **double-clicking** it.&#x20;

The asset list works like an ordinary list shown in the inspector but with slightly different consequences from the action below.

**Add(+):**\
A naming window will pop up first. Once the naming is completed, an element and an Asset ([ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html)) will be created.

{% hint style="info" %}
The default file path for the asset is _Assets/BroAudio/AudioAssets_. If you want to change the path, you can do so in the last tab of [_<mark style="color:orange;">**Tools > BroAudio > Preferences**</mark>_](../customization.md#misc)_._
{% endhint %}

**Remove(-):**\
It will remove both the element in the list, and the asset file it's referring to.

