## 1. Introduction

// TODO: Add intro

### 1.1 Genre

This game merges multiple genres. Each subgenre will be explained in its separate section

#### 1.1.1 Sandbox

#### 1.1.2 Survival

#### 1.1.3 Exploration

#### 1.1.4 Action

### 1.2 Original Vision

Now that we have introduced the game's genre and genres themselves, we can use them to create an overview of the game we intend to make. It will be a single-player game. It will follow genres mentioned above. This means it will be a sandbox game, where the goal is to explore the world and its secrets, while trying to get better and stronger to survive against against enemies.

#### Battles

The goal of each is battle is to survive. Player battles enemies using words, words are acquired by completing challanges in a dream world. Once a challange is completed and player unlocks new word to use, he can bind it to a key on keyboard.

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

The primary goal is to develop a game that is enjoyable, engaging, and easy to play, with clear game mechanics and intuitive controls. To achieve this, we analyzed several other popular games with similar genre to identify some of the most popular features that make them fun. Based on this, we selected a set of features that we believe would work for our game too. 

Each of these selected features will be explained in detail in the following sections. We will also use other games as references to explain how their implementation benefits our game. These features were: 

#### 2.1.1. World Exploration

In many exploration-based games, it is common for player to have the desire to explore the unknown. We want to encourage the same motivation by providing a vast world with secrets to discover.

Let's take Minecraft or Core Keeper as an example, in these games exploration has multiple purposes - it serves as a method of gathering materials, finding new biomes or new types of enemies. Players are not set towards a specific path they need to follow, they are free to explore, which creates a nice and relaxing feeling for the game.

In both of the mentioned games, the world is procedurally generated, but each in slightly different way. In Minecraft, the world is generated using a seed value, a starting number, based on which the algorithm chooses biomes, places structures and resources. This allows for basically an infinite world from the origin point. 

In contrast, Core Keeper's world is finite. In figure 1.1, we can see a map of the world in Core Keeper. The grey thick line around the map represents the bound - a place where extremely difficult enemies are located to inform the player that they cannot go beyond. Inside the map there are 6 main biomes which have specified position and size, however the biomes themselves are then procedurally generated.

TODO: Add map
**Figure 1.1**  World map from Core Keeper

In our game, we would like to follow the Core Keeper's idea and have the world finite, with having each biome's position and size specified and biomes then being procedurally generated.  

#### 2.1.2 Memory Mining and Resource Gathering

One of the goals we identified to be important is the resource gathering, which tends to be a straightforward process that can massively help the player progress in the game.

In typical games that also follow this game mechanic, like Minecraft or Core Keeper, resouce gathering can be used to upgrade your tools, gear, gain materials used for crafting.

While we want to follow that, we would also like to make this mechanic more unique, that is why we have decided to add a special type of ore, a memory ore. Memory ores will be used to gather memory XP (experience points) which are then essential for progression in the game.

#### 2.1.3 Dream-Challange Realms

Now that we have explained how memory XP are gathered, we should explain their purpose. We decided to take an inspiration from games, where alternate realms exist.

One of the games is for example a game called Celeste. It is a 2D platformer, where when you find and acquire a cassette tape in a certain level, you will unlock the so-called B-site of the level, which is a much harder variation of the level.

Our game will combine the memory XP and alternate realms to create a dream-challange realm, where player can spend their XP to try and complete the challange and unlock a **word** (will be explained later in separate section). The difficulty of the challanges inside the realm will be based on how many XP the player decides to spend. Regarding the types of challanges, they will vary, but it will be a type of a mini-game. Few examples we have in mind are solving a riddle, extracting a word from dialogue or some small battle inside an arena. This design then lets the player play multiple types of mini-games and also reinforces the importance of memories.

#### 2.1.4. Word-Spell Forging and Customizable Spell Language

As we mentioned above, words are obtained by succeeding in dream-challanges. We will use words to create spells by combining words into phrases.

In the game called Noita, players experiment with wands and preset spell slots, where player adds different items to the wand, which lets them have freedom when creating wands, but ultimately, during the battle they are fixed, pre-defined combinations.

We would like to follow the freedom of the creating spells, but we would also like to allow it during the battle, that is why we have decided to introduce a customizable spell language concept. This concept lets player bind the acquired words to specific keys, creating a vocabulary that they can rearrange at will. This means that a sequence of keys like 1-2-3 might cast a complemetely different spell than 3-2-1, allowing the player to adapt their combat strategy in real time based on the situation.

#### 2.1.5. Base Building and Customization 

Last of the key design goals we have identified is base building and its customization. This is a typical feature for sandbox, survival type games, where player can have its freedom and create its own base where he will feel safe.

Let's take Core Keeper as an example...TODO

### 2.2 Procedural Generation

### 2.3 Memory Mining 

### 2.4 Resource Gathering

### 2.5 Crafting

### 2.6 Base-Building

### 2.7 Dream world

### 2.8 Battle

#### 2.8.1 Words

#### 2.8.2 Mana Points

#### 2.8.3 Health Points

#### 2.8.4 Spell customization

### 2.9 Movement and Controls

### 2.10 GUI

### 2.11 Saving the Game

### 2.12 Future Features

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
