<a name="readme-top"></a>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="Bro Audio Logo">
    <img src="https://raw.githubusercontent.com/man572142/Bro_Audio/main/Assets/BroAudio/Logo.png" alt="Logo" width="128" height="128">
  </a>

  <!-- PROJECT Title -->
<h3 align="center">BroAudio</h3>

  <p align="center">A simple and intutive Audio Middleware for Unity
    <br />
    <a href="https://man572142s-organization.gitbook.io/broaudio/"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://discord.com/invite/CNWRcc6Zfp">Discord</a>
    ·
    <a href="https://github.com/man572142/Bro_Audio/issues">Report Bug</a>
    ·
    <a href="https://github.com/man572142/Bro_Audio/issues">Request Feature</a>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#about-the-project">About The Project</a></li>
    <li><a href="#quick-start">Quick Start</a></li>
    <li><a href="#explanation">Explanation</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
# About The Project

<a href="https://youtu.be/HtjgdIc9ons?si=OwSPlDb3wNZUFVNP">
  <img src="https://markdown-videos-api.jorgenkh.no/url?url=https%3A%2F%2Fyoutu.be%2FHtjgdIc9ons%3Fsi%3DOwSPlDb3wNZUFVNP" alt="Official Tralier" width="700" title="Official Tralier"/>
</a>

![Product Name Screen Shot][product-screenshot]

BroAudio is a sound management and playback system designed for Unity. It's unique in its focus on <a href="#sound-quality">sound quality</a> and a <a href="#developer-friendly">developer-friendly</a> experience. You can create extensive and captivating audio systems without the need for mastering complex middleware like FMOD or Wwise. Whether you're part of a large team or a solo developer, achieving the goals becomes effortless and efficient with BroAudio.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Features

* Fade In/Out and Crossfade with multiple ease function
* Seamless Looping
* Random Playback (with weight), Sequential Playback
* [SpectrumAnalyzer](https://man572142s-organization.gitbook.io/broaudio/core-features/no-code-components/spectrum-analyzer) for audio visualization
* [SoundVolume](https://man572142s-organization.gitbook.io/broaudio/core-features/no-code-components/sound-volume) for instant game configuration
* [Audio Clip Editor](https://man572142s-organization.gitbook.io/broaudio/tools/clip-editor) for permanent clip editing
* Supports <b>Unity Addressables</b>
* Dynamic audio effects
* Enhanced Volume Control Range (up to +20dB)
* Visualized waveform and visualized playback flow view
* Customizable GUI settings
* Simple one-line code integration
* and more…

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- Quick Start -->
# Quick Start

## Install

Get it from [Unity Asset Store](https://assetstore.unity.com/packages/tools/audio/bro-audio-257362), install it from Package Manager

## Creating sound libraries
* Locate <span style="color: #FF9333;"><i>Tools/BroAudio/Library Manager</i></span> in the Unity menu bar.
* Drag and drop the required AudioClip.
* Edit the parameters<br/>
  edit the clip's volume, playback position... etc.
* Name the asset and entities and choose an AudioType<br/>
You could also skip this step. Just remember to set it before you need them in your scene.

## Implementation 
You can implement the sound you've created in Library Manager with or without coding.

### Without Code 
* <b>Add <span style="color: #FF9333;">SoundSource</span> component to a game object</b>
* <b>Select the required sound via the dropdown menu</b>

&emsp;![SetAudioID][set-audioid]

* <b>Choose the triggering strategy</b><br/>
Enables "Play On Start" if you want to play it when the game object is activated at runtime. Or using <a href="https://docs.unity3d.com/2022.3/Documentation/Manual/UnityEvents.html">Unity Event</a> to trigger the Play() function like the Button's OnClick as below. 

### With Code

* <b>Declare a SoundID and implement `BroAudio.Play()` to play it</b>

<img src="https://man572142s-organization.gitbook.io/~gitbook/image?url=https:%2F%2F886210201-files.gitbook.io%2F%7E%2Ffiles%2Fv0%2Fb%2Fgitbook-x-prod.appspot.com%2Fo%2Fspaces%252FfI79StJ3o7OKZxf9vHhb%252Fuploads%252Fq9iuTTM70cD2urGbBuDW%252FBasicAPI2.png%3Falt=media%26token=30d39bd0-4390-4ca8-ae13-d1ae64ea4a38&width=768&dpr=4&quality=100&sign=7c816015883b672221427c948189177a41fb917148c2ccf8bc12d9c2922661d3" alt="Logo" width=475 height=285>

* <b>Select the required sound via the dropdown menu in the Inspector</b><br/>
The same way as using SoundSource.


### Hit the Unity play button and enjoy it !
That's all you need to start using BroAudio. Of course, there is more than just this. Check out the rest of the documentation to fully unlock all the features of BroAudio.

<p align="right">(<a href="#readme-top">back to top</a>)</p>


# Explanation
<!--
## Library Manager

### Asset
A ScriptableObject that contains a group of libraries and their informations.
### Library
Represent a sound associated with an AudioID that can be played. It can store many clips and their settings about how they would behave.

## Clip Editor
Let you edit an audio clip and save it permanently in Unity.
-->

<a name="sound-quality"></a>

## Why is this tool related to sound quality?

Indeed, apart from issues like noise and distortion that need to be repaired, there is no objective method to enhance sound quality. However, a playback system still holds significant impact over the quality of sound. This is because the loss of sound quality is relatively easy to occur and hard to avoid.

Here are some common issues listed along with how BroAudio addresses them:

| Issues | Bro's Solutions|
| -- | -- | 
| Distortion caused by playing multiple sounds simultaneously. | Well-designed mixer and auto-ducking on the master track |
|Comb Filtering|Preventing rapid repetition of the same sound|
|Unbalanced volume levels|Full range (-80dB to +20dB) and highly adaptable real-time volume control system|
|Unnatural volume changes in Fade In, Fade Out and CrossFade|Utilizing AudioMixer for volume control|

 
<p align="right">(<a href="#readme-top">back to top</a>)</p>

<a name="developer-friendly"></a>

## What does Developer-Friendly mean in this tool?

BroAudio aims to minimize the amount of information presented to developers. This means that by default, you will only see a few commonly used features, while other functions dynamically appear in the UI interface as you interact. You won't be overwhelmed by a large number of rarely used, or even completely unfamiliar settings and parameters.

What's even better is that BroAudio allows you to customize the GUI layout. You can set and select the parameters you want to display in <span style="color: #FF9333;"><i>Tools/BroAudio/Preferences</i></span>.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Can I still use Unity's built-in audio features?
Yes, BroAudio is an add-on plugin, all of Unity's built-in audio features are still available. 


<!-- ROADMAP -->
## Roadmap

See the [Roadmap](https://man572142s-organization.gitbook.io/broaudio/others/roadmap) page of the documentation.

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- CONTACT -->
## Support & Contact

Join us on <a href="https://discord.com/invite/CNWRcc6Zfp">Discord</a>

Che Hsiang Weng - man572142@gmail.com

Project Link: [https://github.com/man572142/Bro_Audio](https://github.com/man572142/Bro_Audio)

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments
These blogs and articles have been a great source of help and inspiration for BroAudio.

* []()[Game Dev Beginner](https://gamedevbeginner.com/)
* []()[What is Adaptive Audio?](https://youtu.be/p-FLWabby4Y?si=v1oABcEIx_o7xfUe)
* []()[THE BASICS ABOUT COMB FILTERING (AND HOW TO AVOID IT)](https://www.dpamicrophones.com/mic-university/the-basics-about-comb-filtering-and-how-to-avoid-it)
* []()[Blog | Audiokinetic](https://blog.audiokinetic.com/)
* []()[FMOD Blog](https://www.fmod.com/blog)

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/man572142/Bro_Audio.svg?style=for-the-badge
[contributors-url]: https://github.com/man572142/Bro_Audio/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/man572142/Bro_Audio.svg?style=for-the-badge
[forks-url]: https://github.com/man572142/Bro_Audio/network/members
[stars-shield]: https://img.shields.io/github/stars/man572142/Bro_Audio.svg?style=for-the-badge
[stars-url]: https://github.com/man572142/Bro_Audio/stargazers
[issues-shield]: https://img.shields.io/github/issues/man572142/Bro_Audio.svg?style=for-the-badge
[issues-url]: https://github.com/man572142/Bro_Audio/issues
[license-shield]: https://img.shields.io/github/license/man572142/Bro_Audio.svg?style=for-the-badge
[license-url]: https://github.com/man572142/Bro_Audio/blob/main/LICENSE
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/哲祥-翁-577109225

[product-screenshot]: https://man572142s-organization.gitbook.io/~gitbook/image?url=https:%2F%2F886210201-files.gitbook.io%2F%7E%2Ffiles%2Fv0%2Fb%2Fgitbook-x-prod.appspot.com%2Fo%2Fspaces%252FfI79StJ3o7OKZxf9vHhb%252Fuploads%252FUhzDSjEL4KixTmABleCK%252FUnity_j0cQdDpL3G.png%3Falt=media%26token=b16a7854-259a-492a-83e8-104a0f78ba46&width=1248&dpr=1&quality=100&sign=b1ab21666607303b6d4612999dc58c39888f9262550f796cb6e0d068c0a5f804

[set-audioid]: https://man572142s-organization.gitbook.io/~gitbook/image?url=https:%2F%2F886210201-files.gitbook.io%2F%7E%2Ffiles%2Fv0%2Fb%2Fgitbook-x-prod.appspot.com%2Fo%2Fspaces%252FfI79StJ3o7OKZxf9vHhb%252Fuploads%252FL6ysT8NV3kDCNG8BoB9A%252FUnity_VVf8IWAV8o.gif%3Falt=media%26token=94af1bae-086b-453c-8732-d2255deb6577&width=768&dpr=1&quality=100&sign=8e7a2bdb695270ce0bc523ef03fb270eb1ea8777b1db39d894e4bc49e6ecb410