## 1. Introduction

// TODO: Add intro

### 1.1 Genre

#### 1.1.1 Sandbox

#### 1.1.2 Survival

#### 1.1.3 Exploration

#### 1.1.4 Action

### 1.2 Original Vision

### 1.3 Current Scope and Goals

## 2. Game Design

Before we start implementing the game, we should design its individual parts. In this chapter we will go into more detail and explain all core mechanics. We need to decide how will each game mechanic behave and how the user interacts with them. Firstly, we should define some design goals to specify what we are trying to achieve.

We would also like to mention that some features will not be implemented in the demo version of the game. These features will be marked by the following box: // TODO: Add box

### 2.1 Design Goals

The primary goal is to develop a game that is enoyable, engaging, and easy to play, with clear game mechanics and intuitive controls. To achieve this, we analyzed several other popular games with similar genre to identify some of the most popular features that make them fun. Based on this, we selected a set of features that we believe would work for our game too. 

Each of these selected features will be explained in detail in the following sections. We will also use other games as references to explain how their implementation benefits our game. These features were: 

#### 2.1.1. World Exploration

In many exploration-based games, it is common for player to have the desire to explore the unknown. We want to encourage the same motivation by providing a vast world with secrets to discover.

Let's take Minecraft or Core Keeper as an example, in these games exploration has multiple purposes - it serves as a method of gathering materials, finding new biomes or new types of enemies. Players are not set towards a specific path they need to follow, they are free to explore, which creates a nice and relaxing feeling for the game.

In both of the mentioned games, the world is procedurally generated, but each in slightly different way. In Minecraft, the world is generated using a seed value, a starting number, based on which the algorithm chooses biomes, places structures and resources. This allows for basically an infinite world from the origin point. 

In contrast, Core Keeper's world is finite. In figure 1.1, we can see a map of the world in Core Keeper. The grey thick line around the map represents the bound - a place where extremely difficult enemies are located to inform the player that they cannot go beyond. Inside the map there are 6 main biomes which have specified position and size, however the biomes themselves are then procedurally generated.

![core_keeper_map](https://github.com/user-attachments/assets/5c535b3a-36a0-44d2-9b09-0f7aff76528d)
**Figure 1.1**  World map from Core Keeper

In our game, we would like to follow the Core Keeper's idea and have the world finite, with having each biome's position and size specified and biomes then being procedurally generated.  

#### 2.1.2 Memory Mining and Resource Gathering

TODO: Look at these games: Figment, Gris, Spiritfarer, The Longing, NieR: Automata

#### 2.1.3 Dream-Challange Realms

Celeste (B-Sites), Hollow Knight

#### 2.1.4. Word-Spell Forging

TODO: Look at these games: Noita, Mages of Mystralia, Magicka

#### 2.1.5. Customizable Spell Language

TODO: Look at these games: Magicka, Baba Is You

#### 2.1.6. Base Building and Customization 

Minecraft, Core Keeper, Stardew Valley

### 2.2 Procedural generation

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

### 2.9 GUI

### 2.10 Saving the Game

### 2.11 Future Features

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
