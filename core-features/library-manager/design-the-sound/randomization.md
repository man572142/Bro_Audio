---
cover: >-
  https://images.unsplash.com/photo-1596270893948-f493df7740f1?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHw1fHxkaWNlfGVufDB8fHx8MTcxMTAwNDIxMnww&ixlib=rb-4.0.3&q=85
coverY: -665
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

# 🎲 Randomization

## Introduction

To create a game with exceptional immersion and rich, vivid sound effects, using a vast sound library is one option. However, finding suitable and sufficiently varied sounds can be challenging, and employing numerous audio files can significantly increase the game's size. Therefore, the ideal approach is simply to use randomization.

## How To Use?

BroAudio provides numerous randomization methods, all of which can be configured in the [LibraryManager](../) without writing any code.

### Audio Clips

If an AudioEntity has more than one clip. You can set its [PlayMode](./#playmode) to 'Random'. It will randomly select one AudioClip based on its weight when it's played. Probability is calculated as _<mark style="background-color:green;">Weight/Total sum of all weights.</mark>_

### Volume

Toggle the \[RND] button next to the '[Master Volume](./#master-volume)' setting in AudioEntity, and set the random range.

### Pitch

Toggle the \[RND] button next to the '[Pitch](./#pitch)' setting in AudioEntity, and set the random range.
