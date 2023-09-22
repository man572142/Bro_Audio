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
  <a href="https://thenounproject.com/icon/fist-177973/">
    <img src="https://raw.githubusercontent.com/man572142/Bro_Audio/main/README/Logo.png" alt="Logo" width="128" height="128">
  </a>

  <!-- PROJECT Title -->
<h3 align="center">BroAudio</h3>

  <p align="center">A simple and intutive Audio Middleware for Unity
    <br />
    <a href="https://github.com/man572142/Bro_Audio"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/man572142/Bro_Audio">View Demo</a>
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
## About The Project
![Product Name Screen Shot][product-screenshot]


BroAudio is a sound management and playback tool designed for Unity. It's unique in its focus on <a href="#sound-quality">sound quality</a> and a <a href="#developer-friendly">developer-friendly</a> experience. You can create extensive and captivating audio systems without the need for mastering complex middleware like FMOD or Wwise. Whether you're part of a large team or a solo developer, achieving the goals becomes effortless and efficient with BroAudio.


<p align="right">(<a href="#readme-top">back to top</a>)</p>

## Features

- Fade In/Out with multiple ease function
- Cross Fade
- Seamless Loop
- Random Playback (with weight)
- Sequential Playback
- Clip Editor for permanent clip editing
- Dynamic audio effect
- Enhanced Volume Control Range (up to +20dB)
- Customizable GUI settings
- Visualized waveform and playback lines
- Low Code Design
- and more…

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- Quick Start -->
## Quick Start

### Creating sound libraries
Locate <b><i>BroAudio/LibraryManager</i></b> in the Unity menu bar. Open it and try to create an Asset and a Library. This process doesn't involve any coding and the GUI works like a regular list you're familiar with.

### Declare an AudioID and use BroAudio.Play() to play it 
![BasicAPI][basic-API-screenshot]

### Set the AudioID in the Inspector
The AudioID is what you've created in <b>LibraryManager</b>

&emsp;![SetAudioID][set-audioid]

### Hit the Unity play button and enjoy it !
That's all you need to start using BroAudio. Of course, there are more than just this. Check out the documentation to fully unlock all the features of BroAudio.

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

<a name="developer-friendly"></a>

## What does Developer-Friendly mean in this tool?

BroAudio aims to minimize the amount of information presented to developers. This means that by default, you will only see a few commonly used features, while other functions dynamically appear in the UI interface as you interact. You won't be overwhelmed by a large number of rarely used, or even completely unfamiliar settings and parameters. 

What's even better is that BroAudio allows you to customize the GUI layout. You can set and select the parameters you want to display in <b><i>BroAudio/Settings</i></b>.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<a name="sound-quality"></a>

## Why is this tool related to sound quality?

Indeed, apart from issues like noise and distortion that need to be repaired, there is no objective method to enhance sound quality. However, a playback system still holds significant impact over the quality of sound. This is because the loss of sound quality is relatively easy to occur and hard to avoid. That's why BroAudio consistently prioritizes preserving the original sound quality as its primary design consideration.


Here are some common issues listed along with how BroAudio addresses them:

| Issues | Bro's Solutions|
| -- | -- | 
| Distortion caused by playing multiple sounds simultaneously. | Well-designed mixer and auto-ducking on the master track |
|Comb Filtering / Haas Effect|Preventing rapid repetition of the same sound|
|Unbalanced volume levels|Highly adaptable real-time volume control system|
|Unnatural volume changes in Fade In, Fade Out and CrossFade|Utilizing AudioMixer for volume control|

 
<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- ROADMAP -->
## Roadmap

- [ ] Add AnimationCurve settings for fade In/Out
- [ ] More effect tracks
- [ ] Performance Improvement
    - [ ]  Add AudioClipLoadType settings
    - [ ]  Improve asset loading mechanic

See the [open issues](https://github.com/man572142/Bro_Audio/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>


<!-- LICENSE -->
## License

Distributed under the MIT License. See `LICENSE` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

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

Logo Artist : []()[Creative Stall](https://thenounproject.com/icon/fist-177973/)

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
[product-screenshot]: https://raw.githubusercontent.com/man572142/Bro_Audio/main/README/LibraryManager_Shadow.png
[basic-API-screenshot]: https://raw.githubusercontent.com/man572142/Bro_Audio/fb5cc09792fda6d9fe5dac65fc4178d6ae3cf77a/README/BasicAPI.svg
[set-audioid]: https://raw.githubusercontent.com/man572142/Bro_Audio/main/README/AudioID.gif