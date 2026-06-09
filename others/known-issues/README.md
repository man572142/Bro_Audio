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

# Known Issues

## Error: <mark style="color:red;">/.../BroAudio/Demo/Scripts/PreferenceOverrider.cs(25,43):error CS1061: 'RuntimeSetting' does not contain a definition for 'LogCombFilteringWarning'...</mark>

**Problem:** Both `PreferenceOverrider.cs` and the `LogCombFilteringWarning` field were removed in version 2.0.0. If you're upgrading from an older version, the unitypackage won’t automatically delete the `PreferenceOverrider` script after import, which leads to issues.

**✅**<mark style="background-color:green;">**Solution:**</mark> <mark style="background-color:green;"></mark><mark style="background-color:green;">Remove the</mark> <mark style="background-color:green;">`PreferenceOverrider.cs`</mark> <mark style="background-color:green;"></mark><mark style="background-color:green;">script, as it's no longer needed.</mark>

## :hushed:Warning: <mark style="color:orange;">The character with Unicode value \u2728 was not found in the \[Inter-Regular SDF] font asset or any potential fallbacks. It was replaced by Unicode character \u25A1</mark>

**Problem:** When opening BroAudio's description in the Package Manager, a warning is logged to the console indicating that some emojis are not supported by Unity's default font. This is a confirmed [Unity bug](https://issuetracker.unity3d.com/issues/unicode-character-error-in-console-when-browsing-assets-in-package-manager) and mostly occurs in Unity 2022.\
\
&#xNAN;**✅**<mark style="background-color:green;">**Solution:**</mark> <mark style="background-color:green;">**This issue will not cause any problems**</mark><mark style="background-color:green;">, so you can continue using BroAudio with confidence.</mark>





*   ### ~~Error: "<mark style="color:red;">InvalidOperationException: Collection was modified; enumeration operation may not execute.</mark>" is thrown when calling BroAudio.Pause()~~&#x20;

    ### [**Fixed in 2.1.0**](../release-notes.md#ver-2.1.0-github-unity-asset-store)
* ### ~~**Error:**~~ ~~<mark style="color:red;">"An asset is marked with HideFlags.DontSave but is included in the build: BroAudio/Core/Resources/Editor/Logo\_Main.png"</mark>~~[ Fixed in 2.0.0](../release-notes.md#ver-2.0.0-unity-asset-store-github)
* [x] ~~Changing the master volume with a fadeTime of zero in WebGL doesn't work as expected~~[ Fixed in 2.0.0](../release-notes.md#ver-2.0.0-unity-asset-store-github)

