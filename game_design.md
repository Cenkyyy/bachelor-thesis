## 1. Introduction

// TODO: Add intro

### 1.1 Genre

This game merges multiple genres. Each subgenre will be explained in its separate section.

// TODO

#### 1.1.1 Sandbox

#### 1.1.2 Survival

#### 1.1.3 Exploration

#### 1.1.4 Action

### 1.2 Original Vision

Now that we have introduced the game's genre and genres themselves, we can use them to create an overview of the game we intend to make. It will be a single-player game. It will follow genres mentioned above. This means it will be a sandbox game, where the goal is to explore the world and its secrets, while trying to get better and stronger to survive against against enemies.

// TODO

#### Battles

The goal of each is battle is to survive. Player battles enemies using words, words are acquired by completing challenges in a dream world. Once a challenge is completed and player unlocks new word to use, he can bind it to a key on keyboard which allows him to crate phrases containing that word.

// TODO

#### Procedural generation

The world at the start will be procedurally generated, creating a new challange in each game you create.

#### Run progression



#### Platform

We will target the game for computers only. We decided on this based on the way the game will be played, player needs to have bindings on keys, for which a keyboard is necessary. Another point is that the game will contain many on click events, for which, the mouse is required (e.g. interacting with items in your inventory).

### 1.3 Current Scope and Goals

// TODO

## 2. Game Design

Before we start implementing the game, we should design its individual parts. In this chapter we will go into more detail and explain all core mechanics. We need to decide how will each game mechanic behave and how the user interacts with them. Firstly, we should define some design goals to specify what we are trying to achieve.

We would also like to mention that some features will not be implemented in the demo version of the game. These features will be marked by the following box: // TODO: Add box

### 2.1 Design Goals

The primary goal is to develop a game that is enjoyable, immersive and has engaging progression with clear game mechanics and intuitive controls. To achieve this, we analyzed several other popular games with similar genre to identify some of the most popular features that make them fun. Based on this, we selected a set of features that we believe, with modifications, would work for our game too.

Each of these selected features will be explained in detail in the following sections. We will also use other games as references to explain how their implementation benefits our game. These features were: 

#### 2.1.1. World Exploration

In sandbox and exploration-based games, curiosity is often the best way to keep the player engaged. We want to encourage the same motivation by providing a vast world with secrets to discover.

Let's take Minecraft or Core Keeper as an example, in these games exploration has multiple purposes - it serves as a method for gathering materials, finding new biomes or new types of enemies. Players are not set towards a specific path they need to follow, but are free to explore, which creates a nice and relaxing feeling for the game.

In both of the mentioned games, the world is procedurally generated, but each in slightly different way. In Minecraft, the world is generated using a seed value, a starting number, based on which the algorithm chooses biomes, places structures and resources. This allows for basically an infinite world from the origin point. 

In contrast, Core Keeper's world is finite. In figure 1.1, we can see a map of the world in Core Keeper. The grey thick line around the map represents the bound - a place where extremely difficult enemies are located to inform the player that they cannot go beyond. Inside the map there are 6 main biomes which have specified position and size, however the biomes themselves are then procedurally generated.

TODO: Add map
**Figure 1.1**  World map from Core Keeper

In our game, we would like to follow the Core Keeper's idea and have the world finite, with having each biome's position and size specified and biomes then being procedurally generated.  

#### 2.1.2 Memory Mining and Resource Gathering

One of the goals we identified to be important is the resource gathering, a mechanic that supports progression through crafting and upgrading.

In Minecraft or Core Keeper, resource gathering can be used to upgrade or craft new tools or better gear.

While we want to follow that, we would also like to make this mechanic more unique, that is why we have decided to introduce the concept of **Memory Ores**. Memory Ore is a rare material that rewards the player with memory experience points (XP) which are then essential for progression in the game.

Memory XP can be also acquired upon discovering certain ruins or landmarks that the player used to remember before he lost his memories. These events can be triggered by stepping into a certain part of the map, where the player could for example say "I feel like I have been here before...but why?". This creates a great sense of discovery, where each landmark can have the potential to be more than just a part of the map.

#### 2.1.3 Dream-Challange Realms

Now that we have explained how memory XP are gathered, we should explain their purpose. We decided to take an inspiration from games, where alternate realms exist, because they are a great way to provide challenges outside the main gameplay loop for the player.  

One of the games is for example a game called Celeste. It is a 2D platformer, where player can find and acquire a cassette tape in a certain level. Cassette tapes then unlock the so-called "B-site", which is a much harder version of the original one.

Our game will combine the memory XP and alternate realms to create a dream-challenge realm, where player can spend their XP to enter a **Dream Realm**, where they face a randomly selected challenge in the form of mini-games. These challenges vary, they can be from solving riddles and extracting the correct word from the dialogue to surviving combat scenarios or navigating mazes. The difficulty of the challenge is determined by the amount of memory XP spent on the challenge. Current idea is to have three difficulties, easy, medium, hard, where each one costs more and more memory XP. When player completes a challenge, they will receive a **Word**, with rarity given based on difficulty of the challenge. Words are then used for combat, it will be explained in greater detail in the next section.

This mechanic brings a drastic shift in gameplay style, introducing puzzle-solving mini-games inside an survival action game.

#### 2.1.4. Word-Spell Forging and Customizable Spell Language

As we mentioned above, words are obtained by succeeding in dream-challenges. We will use words to create spells by combining words into phrases. We want the player to have freedom and creativity when creating spells, that is why we decided on a customizable spell language, which is quite rare, but highly engaging when implemented well.

Let's take Noita as an example, it offers players to experiment with wands and preset spell slots by a wand-building mechanic, where spell can be combined in sequence, encouraging experimentation. However, the wand-building can be only done outside of battles, otherwise player could be killed while changing wand's preset during the battle.

Our game will follow similar approach of spell creation, but it will remove the restriction on mid-combat wand modifications. Words, earned through dream challenges, can be bound to specific keyboard keys, allowing player to form spells dynamically by pressing sequences of keys. For example a sequence of keys like 1->2->3 might cast a spell "Shot of Fire".

This mechanic challenges the player to think about which words to equip before the battle, but also flexible creation of phrases mid-combat. How this mechanic will be implemented will be explained in the later section.

#### 2.1.5. Base Building and Customization 

Last of the key design goals we have identified is base building and its customization. This is a typical feature for sandbox, survival type games, where player can have its freedom and create its own base where he will feel safe.

Let's take Core Keeper as an example, it provides the option to build shelters to protect against monsters and organize player's resources. Except that, it also provides craftable furniture, workbenches and farmable crops that reinforce the gameplay loop with the exploring, gathering and upgrading.

// TODO Decide whether I want a fixed starting Base/Transportable Base or Hybrid
We aim to follow this mechanic by letting the player build its own base, with 

### 2.2 Procedural Generation

### 2.3 Memory Mining 

#### 2.3.1 Memory Points

### 2.4 Resource Gathering

### 2.5 Crafting

### 2.6 Base-Building

### 2.7 Dream world

### 2.8 Battle

#### 2.8.1 Words

#### 2.8.2 Mana Points

#### 2.8.3 Health Points

#### 2.8.4 Spell customization

### 2.9. Movement and Controls

### 2.10. Inventory

### 2.11 Day and Night Cycle

### 2.12 Graphical User Interface

### 2.13 Saving the Game

### 2.14 Future Features

## 3. Analysis

// TODO

## 4. Developer Documentation

// TODO

## 5. Designer Documentation

// TODO

## 6. User Documentation

// TODO

## 7. Playtesting

// TODO

## 8. Conclusion

// TODO

## Bibliography

// TODO

## Attachments

// TODO
