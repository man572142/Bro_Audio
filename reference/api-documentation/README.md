---
layout:
  width: default
  title:
    visible: true
  description:
    visible: false
  tableOfContents:
    visible: true
  outline:
    visible: false
  pagination:
    visible: false
  metadata:
    visible: true
  tags:
    visible: true
  actions:
    visible: true
---

# Scripting API

### The Method Chaining design

BroAudio utilizes the Method Chaining design, just like the [System.Linq in C#](https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/write-linq-queries), you can chain one method with another method, adding behaviors and settings in just one line of code. For example:

```csharp
BroAudio.Play(_sound).AsBGM().SetTransition(Transition.CrossFade).SetVolume(0.8f);
```

More Information in the Class/[BroAudio](class/broaudio.md), and Interface/[IAudioPlayer](interface/iaudioplayer.md) section.

### Page Links

{% content-ref url="class/" %}
[class](class/)
{% endcontent-ref %}

{% content-ref url="interface/" %}
[interface](interface/)
{% endcontent-ref %}

{% content-ref url="struct/" %}
[struct](struct/)
{% endcontent-ref %}

{% content-ref url="enums/" %}
[enums](enums/)
{% endcontent-ref %}
