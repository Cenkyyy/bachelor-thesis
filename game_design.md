# Game Design Document  

---

## 1. Introduction

(Title) is a 2D topdown pixel art survival sandbox set in a fantasy world. The player controls a wizard exploring a procedurally generated world, gathering resources, crafting items, building a small base, fighting enemies and trying to survive. On top of this survival layer, the game introduces a parallel world tied to the wizards lost memories and a magic system based on combining Words into spells.

### 1.1 Key aspects

The game is built around the following pillars:

- **Exploration and survival in a procedurally generated world**  
  Moving around the world, interacting with the environment, gathering resources, mining and managing hunger should feel smooth, readable and rewarding. The player should quickly understand what is dangerous, what is useful and where they can go. The procedurally generated biomes and decorations should support this by making each world interesting to explore without confusing the player.

- **Crafting book and progression through items and structures**  
  The crafting book is the main interface for creating new items, equipment, potions, food and building pieces. It should be fast and intuitive to use, clearly show which recipes are available and why, and allow the player to improve their situation without spending unnecessary time in menus. The feeling should be that once the player has the resources and a safe moment, they can reliably turn those resources into meaningful upgrades.

- **Parallel world and Word based magic**  
  Entering the parallel world and unlocking new Words is the main way the wizard regains their lost power. This system needs to feel special and clearly separated from normal survival. The player should understand that exploring the parallel world and collecting Memory lets them learn new magical Words, combine them into Phrases and unlock spells that change how combat and exploration play out in the long term.

### 1.2 Gameplay

#### Story

The player was once the strongest wizard alive, but an ultimate boss used a forbidden spell that erased the wizards memory of all spells. The only clear memory is of an ultimate boss who caused that loss. The wizard now finds themselves in a strange, partially ruined world with only basic survival skills and a vague awareness of a parallel realm connected to their memories. Their long term goal is to regain their power, relearn spellcasting, and eventually defeat the boss.

#### Game loop

After launching the game and selecting a world, the player enters a loop that repeats over many in game days and nights. In broad strokes, one cycle looks like this:

1. **Start of the world and spawn**  
   When a new world is created, the game generates the entire world in the background based on a seed. After generation the player spawns in a safe starting area, typically with a minimal starter kit and access to a first bed or camp. The details of world generation are described in section 4.

2. **Planning the day**  
   Each day the player chooses how to use the limited time before night. They can decide to explore outward into new biomes, gather basic resources like wood and stone, mine ores, look for food sources, or stay near their base to craft and build.

3. **Exploration, gathering and combat**  
   While moving through the world, the player encounters enemies, mineable tiles. They can:
   - Fight enemies using basic attacks and later spells.
   - Mine rocks and other tiles to get resources for crafting.
   - Collect loot from dungeon chests and other structures.
   - Manage hunger.

4. **Returning to base and crafting**  
   Eventually the player returns to a base or temporary camp. Here they:
   - Store items in chests.
   - Open the crafting book to craft tools, equipment, potions, food and building pieces, if they have enough materials and no enemies are nearby.
   - Place new structures from the Building category, such as beds, chests, simple walls and floors, to slowly improve their shelter.
   Crafting takes a short time and the player is vulnerable during it, so choosing a safe moment and location is important.

5. **Managing hunger and night**  
   As time passes, the hunger bar slowly decreases. The player needs to obtain food, for example from NPCs or creatures that drop food items, and consume it to avoid penalties. At the same time the world transitions from day to night. Nights are more dangerous, with stronger or more frequent enemies. The player can decide to stay awake and try to use the night for risky activities, or head back to their bed.

6. **Sleep and the parallel world**  
   When the player lies down in a bed, instead of simply skipping time they can enter the parallel world. There, different rules apply: the focus shifts from basic survival to Memory and magic. The player can explore special areas, participate in minigames and interact with Memory related resources to unlock new Words. The exact structure of the parallel world and its minigames is described in a later section, but the important point here is that sleep ties the survival loop to the magical progression loop.

7. **Return and progression**  
   After leaving the parallel world, the player wakes up back in the overworld. Any resources, Words or experiences gained now influence the next cycle: with better tools, more food, new building options and stronger spells, the player can explore further, survive tougher encounters and approach their long term goal of defeating the boss.

### 1.3 Target group

TBA

### 1.4 Similar games

#### Core Keeper

#### Minecraft

TBA

---

## 2. Menus and scene structure

This section describes how the player moves through the games menus and scenes, from starting the application to entering a world and pausing the game. The goal is to have a simple and predictable flow that still supports multiple worlds and a short intro/tutorial for each new world.

### 2.1 Main menu

When the game starts, the player is shown the main menu (picture 1.1.). The main menu is a separate scene with a simple background and three primary options:

- **Play**  
  Button that opens the world selection interface, where the player can create, open or delete worlds. The world selection is described in section 2.2.

- **Settings**  
  Button that opens the settings panel on top of the main menu. The settings panel lets the player configure audio and basic controls. The general behavior of settings panels is described in section 2.5.

- **Exit**  
  Button that closes the application.

<img width="638" height="360" alt="image" src="https://github.com/user-attachments/assets/5742896f-2a7c-4e30-8566-c361c23a9f8f" />

Picture 1.1.

### 2.2 World selection and world management

After pressing Play in the main menu, the game transitions to the world selection interface (picture 1.2.). This can be a separate scene or a panel over the main menu, but conceptually it is a list of existing worlds plus controls for managing them. Each world will have an edit button, where the player will be able to rename the world.

The available actions are:

- **Create new world**  
  Opens a small panel where the player can enter a world name and optionally a custom seed. If no seed is entered, the game generates one. After confirming, the game proceeds to world creation: it loads the loading screen, generates the world based on the seed, and then starts the intro for this new world. World generation is described in section 3, and the intro in section 2.4.

- **Open world**
  Loads an existing world from disk and sends the player directly into that worlds in game scene, restoring the saved player state, world state and time of day. Before the world becomes visible, a short loading screen is shown while saved data is applied. By default the button cannot be interacted with, but after selecting a world, the button becomes interactable and the world can be loaded (alternative, create a small button next to the edit button).

- **Delete world**  
  Removes the selected world and all related save data. This action must show a confirmation dialog (for example "Are you sure you want to delete this world? This cannot be undone.") to avoid accidental deletion. By default the button cannot be interacted with, but after selecting a world, the button becomes interactable and the world can be deleted (alternative, create a small button next to the delete button).

<img width="635" height="357" alt="image" src="https://github.com/user-attachments/assets/f6d74636-7100-4a48-9493-c59802e1fb1a" />

Picture 1.2.

### 2.3 Loading screen

Whenever the game needs to perform a longer operation, such as generating a new world or loading an existing one from disk, it uses a dedicated loading screen (picture 1.3.).

The loading screen is a simple black or dark background with a short text such as "Loading..." with the animation of changing the amount of dots from 1 to 3.

The loading screen is used mainly in two cases:

1. **New world creation**  
   After the player confirms creating a new world from the world selection interface, the game switches to the loading screen and runs the world generation pipeline. When this work is done, the game transitions from the loading screen to the intro story and tutorial for that world (section 2.4).

2. **Loading an existing world**
   When the player selects an existing world and presses "Open", the loading screen appears while the game:
   - Reads saved data for that world.
   - Reconstructs the world state (terrain, buildings, chests, enemies if needed).
   - Restores the player position, inventory and stats.
   Once the state is ready, the loading screen disappears and the camera fades into the in game scene.

<img width="640" height="361" alt="image" src="https://github.com/user-attachments/assets/f514f318-7186-4e47-b937-375910fe9092" />

Picture 1.3.

### 2.4 Intro story and tutorial

For every newly created world, the first time the player enters it there will be an intro. The intro is only shown once per world; after it finishes and the player saves and quits, opening the same world later goes directly to gameplay.

The intro has two main parts:

- **Story introduction (OPTIONAL)**
  - A short sequence that explains the story of the game:
  - This can be presented as a series of static images with text or a simple text overlay. The focus is on clarity rather than complex cinematics. The story should connect directly to the visual of the player waking up in the starting area.

- **Tutorial section**  
  - After the story text, an NPC will approach character and explains the mechanics of the game (movement, spell system and casting of the first spell, opening/closing of the inventory, chest, settings, map, crafting...).

### 2.5 In game settings and pause

While playing inside a world, the player can open settings or pause the game by pressing Esc (picture 1.5.). The in game settings behave as follows:

- Pressing **Esc** when no other panel is open:
  - Opens the pause/settings panel.
  - Pauses game time and enemy behavior while the panel is visible.
- The pause/settings panel contains:
  - "Settings" button:
    - Audio options (for example master volume, music volume, effects volume).
    - A simple control overview and possibly keybinding changes (OPTIONAL).
  - "Resume" button:
    - closes the panel and resumes the game
  - "Return to main menu" button:
    - Saves the curernt world state and returns to the main menu scene.

<img width="169" height="227" alt="image" src="https://github.com/user-attachments/assets/a22b11fb-dc06-450c-a597-ba0a7c802dbc" />

Picture 1.5.

---

## 3. World and map generation

This section describes how a world is created when the player makes a new world. The goal is to have a deterministic map that is fully defined by a seed, but still feels different and interesting every time.

### 3.1 Overview and goals

When a new world is created from the world selection screen, the game generates the entire map in the background during the loading screen (see section 2.3). The generation process:

- Uses a world seed so the same seed always produces the same world.
- Lays out a small number of distinct biomes (for example grassland, forest, snow) using a Voronoi based system.
- Fills each biome with appropriate ground tiles and natural looking variation.
- Places decorations such as trees, rocks and plants in patterns that match the biome.
- Places a few special structures and scenes (shrines, dungeons, boss arenas, NPC camps) at valid positions.

The design aims for the following properties:

- Worlds are **deterministic**: the same seed always gives the same layout.
- Worlds are **different enough**: changing the seed changes the arrangement of biomes, decorations and scenes, so players cannot memorize fixed paths and skip progression.
- Biomes are **limited but distinct**: there are only a few biomes in the current version, but each has its own ground tiles, decorations and possible structures.
- Certain scenes and bosses are **tied to biomes**, but their positions move between worlds: an ice shrine always appears in a snow biome, but not always in the same place.

### 3.3 Biome layout with Voronoi regions

Biomes are assigned using a Voronoi style layout:

- A set of biome centers is generated over the area of the world using the seed.
- Each center is assigned a biome type, for example:
  - Grassland
  - Forest
  - Snow
  - Any additional biome defined later
- For every tile in the world, the closest biome center determines the biome type of that tile.

This creates large, irregular regions around each center instead of repeating patterns or simple stripes. The result is a map where each biome forms organic shapes.

On top of this basic rule, additional constraints can be applied:

- Some biomes can be made more common or larger by adjusting how many centers they receive.
- Certain biome pairs are restricted from touching directly to avoid jarring transitions (for example, a winter biome should not directly border a warm beach like area). This can be achieved by:
  - Placing cold and warm biome centers in different coarse “zones”, or
  - Adjusting the resulting biome map after generation to soften problematic borders.

The important point is that the **set** of biomes is fixed for a given version of the game, but the **position and shape** of each biome region depends on the seed. In one world, the snow biome might be north of spawn, in another world it might be to the east, etc.

### 3.4 Terrain inside biomes

Once every tile knows which biome it belongs to, the next step is to decide which specific ground tile appears there. The initial pass simply assigns a default base tile per biome (for example “grass” for grassland, “snow” for snow biome), but this would look flat and repetitive if left as is.

To make the terrain look more natural, each biome defines:

- A small **palette of ground variants**, for example:
  - Plain grass.
  - Grass with small plants.
  - Grass with stones.
  - Slightly darker or lighter versions of the base tile.
- A set of **rules or noise parameters** that describe where each variant appears:
  - Some variants are more common overall.
  - Some variants appear in patches or clusters.
  - Some variants might prefer certain conditions (for example near biome edges or near water, if water exists).

For each tile in the biome, the game evaluates these rules and picks one of the variants. The result should be:

- Large areas of visually consistent ground, but with gentle variation.
- Patches of special tiles, such as fields of small flowers or rougher ground, created by thresholding noise fields.
- Avoiding pure random “TV static” where every tile is different.

The logic for this refinement is different per biome. A forest biome can use more dark, leafy ground variants and patches of fallen leaves, while a snow biome might vary between fresh snow, trampled snow and icy patches.

### 3.5 Decorations and structures

After ground tiles are decided, the generator adds further detail by placing decorations and larger structures.

#### Decorations

Decorations are small objects and props placed on top of the ground that do not fundamentally change the terrain but give the world character and provide resources. Examples include:

- Trees and bushes.
- Rocks and boulders.
- Small plants, flowers and mushrooms.
- Other biome specific objects (for example dead trees in a haunted area).

Each biome defines:

- Which decoration types can appear there.
- Approximate density for each type (how common they should be).
- Any special rules about where they like or dislike appearing.

Some typical patterns:

- In a grassland biome:
  - Trees might be relatively sparse and mostly appear as single trees with a minimum distance between them, so the area feels open.
  - Flowers might appear in clusters forming flower fields, using a density map so that patches of many flower tiles appear together.
  - Small rocks might appear near transitions between different ground variants.

- In a snow biome:
  - Trees might be even rarer, often without leaves.
  - Rocks and icy formations can be more common.
  - Decoration density might be lower overall to convey harsh emptiness.

The decoration system should produce:

- Clusters where clustering makes sense (flowers, small rocks).
- More evenly spaced elements where that fits (trees, big boulders).
- Clear biome identity, so the player can often guess the biome from decorations even if ground tiles are similar.

#### Structures and special scenes

Structures are larger handmade or semi handmade arrangements of tiles and objects that represent interesting locations in the world. Examples include:

- Shrines or altars (for example an ice shrine in a snow biome).
- NPC camps or small settlements.
- Dungeon entrances or small underground areas.
- Boss arenas or special combat spaces.

These structures are not generated tile by tile using the same simple rules as ground and decorations. Instead, each structure exists as a reusable template that can be placed into the world:

- The template defines the layout of ground tiles, walls, props and any scripts needed.
- For each biome, the generator decides how many such structures should appear. In the current design this is intentionally small, such as one or two shrines or boss areas per biome.

During world generation, for each structure type:

- The game looks for candidate positions that satisfy simple conditions:
  - The tile belongs to the correct biome.
  - The area is not underwater or otherwise invalid.
  - The position is not too close to the starting area or to other major structures.
- From these candidates, one or several positions are selected and the structure template is stamped into the world there.

By combining deterministic seeds, Voronoi based biome layout, per biome terrain variation, decorations and a small number of handcrafted structures per biome, the generator can produce worlds that share the same rules but still feel fresh and exploratory for each new run.

---

## 4. Player interface, HUD and controls

This section describes what the player sees on screen during gameplay and how they interact with the game using the keyboard and mouse. The goal is to keep the HUD readable and focused on the most important information, while making core controls predictable and consistent from the tutorial onward.

### 4.1 HUD overview

The HUD is always visible during gameplay and shows the basic state of the player and the world (Picture 1.6.). At minimum it contains:

- **Health bar**  
  Indicates how much damage the player can still take before dying. The bar should be placed in a clearly visible location and change color or animate when health is critically low.

- **Mana bar**  
  Shows the amount of mana available for casting spells. Mana is primarily consumed by the Word based magic system and refills over time or via potions.

- **Hunger bar**  
  Represents how hungry the player is. Hunger slowly decreases over time and is restored by eating food items. Low hunger should be clearly indicated, as it can lead to penalties.

- **Experience bar and level**  
  Tracks regular experience gained from defeating enemies or completing actions. The level number can be displayed next to or inside the bar.

- **Memory experience bar and Memory level**  
  Used for progression tied to the parallel world. Memory experience and levels are gained from activities in that world and later used to unlock Words or minigames. Even if the full system is defined later, the bar should already exist visually so the player knows there is a second progression layer.

- **Hotbar**  
  A row of slots showing currently quick usable items such as tools, weapons, food or building pieces. One slot is always selected and corresponds to what the player uses when they mine, attack or place buildings.

- **Minimap**  
  A small minimap can be shown once implemented. It provides a local overview of the area around the player, helping with navigation without opening a full screen map.

- **Current in game time**  
  A simple clock or time indicator shows whether it is day or night and how far along the current cycle is. This helps the player decide when to head back to base or prepare for sleep.

<img width="1280" height="718" alt="image" src="https://github.com/user-attachments/assets/157ea185-cdea-477c-b4bd-c7d23210a7bb" />

Picture 1.6.


### 4.2 Core controls

The game uses keyboard and mouse controls in a way that is typical for 2D topdown survival games. These controls are introduced in the tutorial and remain consistent throughout the game.

The main controls are:

- **Movement**  
  Movement is handled by the keyboard by WASD. The player moves in four directions on the 2D plane. Exact key bindings can be shown in the settings and in a simple control overview.

- **Inventory**  
  The inventory shows the players backpack grid, hotbar connections and equipment panel.
  - Open inventory: `E`
  - Close inventory: `E` or `Esc`  
  Opening the inventory does not pause the game. World continues to update.

- **Chests**  
  Chests open a separate panel that shows chest storage on one side and the player inventory on the other.
  - Open chest: right click on a chest in the world.
  - Close chest: `E` or `Esc`.
  - If the player walks away from an open chest beyond a certain radius, the chest panel closes automatically.  
  This prevents the player from accidentally leaving chest windows open when they move away and reinforces the idea that chest interaction requires being physically near the chest.

- **Map**  
  When a map system is implemented, the player can open a larger view of the world or a local area.
  - Open map: `M`
  - Close map: `M` or `Esc`  
  Like the inventory, opening the map does not pause the game. The map will be by default unexplored, the exploration will be based on the minimap. The scaling will work like this: The in game vision is smaller than the minimap, upon opening the whole map, everything explored (everything that was on minimap) will be explored in the map (See 1.7.).

<img width="943" height="666" alt="image" src="https://github.com/user-attachments/assets/468135c3-ef58-4110-9c27-f4164e94ce9b" />

Picture 1.7.


- **Settings and pause**  
  The settings and pause menu is the only interface that actually pauses the game.
  - Open pause/settings: `Esc` when no other panel is open.
  - Close pause/settings: `Esc` or by clicking a "Resume" button.  
  While the pause menu is open, game time stops, enemies do not move and the player cannot interact with the world. From this menu the player can adjust audio, view controls and return to the main menu.

The tutorial explicitly teaches these controls and should explain the difference between panels that pause the game (pause/settings) and panels that do not (inventory, chests, map, crafting book). This helps players form correct expectations about when it is safe to manage items or read the map and when they need to be ready to react to enemies.

---
## 5. Items, inventory, equipment and chests

Items are at the center of progression in the game. The player gathers resources, turns them into tools, equipment, food and building pieces, and stores them in inventories and chests. This section describes how items are represented, how the inventory and hotbar behave, how equipment slots work and how chests are used both as storage and as loot containers.

### 5.1 Item definition and categories

Each item in the game is defined by a small set of properties that describe how it should look and behave. At a minimum, an item has:

- A **name**  
  Shown in tooltips, crafting recipes and inventory slots. The name should clearly indicate what the item is and, if possible, hint at its function (for example "Simple Wand", "Cooked Chicken", "Wooden Pickaxe").

- An **icon**  
  A small pixel art representation used in the inventory, hotbar and crafting book. Icons must be recognizable at a glance and distinguishable from other items, especially when multiple item types share similar colors (for example several different ores).

- A **category**  
  A classification that groups items by their role. Typical categories include:
  - Weapon
  - Armor
  - Tool
  - Consumable (food, potions)
  - Resource (wood, stone, ore, memory fragments)
  - Building (bed, chest, walls, floors)
  - Other / Miscellaneous  
  The category determines where the item can be used (for example in equipment slots or in the building system) and which systems care about it (for example the hunger system cares about food, the crafting system cares about resources).

- **Stacking rules**  
  Each item defines:
  - A maximum stack size (1 for equipment that cannot stack, higher values for resources and consumables).
  - Whether the item can be stacked at all.  
  These rules influence how much of each resource the player can carry in a single inventory and how often they need to manage space.

### 5.2 Player inventory and hotbar

The player has a personal inventory that consists of:

- A **backpack grid** for general storage.
- A **hotbar** that exposes a subset of items for quick access.

The backpack is used for carrying all the items the player picks up or crafts. It is shown as a grid of slots. Each slot can contain one stack of items or be empty. The exact size of the backpack (number of slots) can be adjusted during balancing, but it should be large enough to allow exploration without constant backtracking, while still forcing some decisions about what to keep and what to leave behind.

The hotbar is a row of slots, usually displayed at the bottom of the screen as part of the HUD. It is directly connected to the inventory:

- Each hotbar slot corresponds to a specific slot in the backpack.
- Selecting a hotbar slot changes which item is currently "active".
- The active item is used for context actions:
  - Mining (if the active item is a mining tool).
  - Attacking (if the active item is a weapon).
  - Placing structures (if the active item is a building piece).
  - Using consumables like food or potions.

The interaction patterns for moving items between slots, splitting stacks and quickly transferring items should follow the conventions players know from games like Minecraft:

- Left click, right click, shift-click, double-click and dragging behavior are designed to feel familiar.
- Players should be able to:
  - Quickly move whole stacks between inventory and chests.
  - Split stacks into smaller parts.
  - Combine stacks of the same item.
  - Rearrange the hotbar without friction.

The goal is for inventory management to feel smooth and efficient. The player should not feel like they are fighting the UI to move items where they want; instead, they should spend most of their time exploring and playing, with inventory interactions happening quickly when needed.

### 5.3 Equipment panel and character slots

In addition to the general inventory, the player has an equipment panel that represents what they are currently wearing or holding. This panel is typically shown as part of the inventory screen and contains dedicated slots for different equipment types.

- Head slot (helmets, hoods, hats).
- Chest slot (armor).
- Legs slot (pants).
- Boots slot (boots).
- Accessory slots (rings, amulets, necklaces).

Only compatible items can be placed into each slot. For example:

- A helmet item can be placed into the head slot but not into the legs slot.
- A wand or staff can go into the main hand slot, but not into the chest slot.

Equipping an item from the inventory into an equipment slot:

- Updates the players stats (for example increases armor, health, damage or mana).
- May change the players visual appearance if there are sprites or overlays for that piece of equipment.

Removing an item from a slot places it back into the inventory, assuming there is space. If the inventory is full, removing equipment can be blocked or might require the player to free space first, depending on the chosen rules.

The equipment panel should:

- Give a clear overview of what the player is currently wearing.
- Make it easy to compare equipment by showing simple stats or differences in a tooltip.
- Integrate with the crafting system so that newly crafted gear can be equipped quickly.

### 5.4 Chests: storage and loot

Chests provide additional storage in the world and are used both for player made bases and for rewarding exploration in dungeons and special scenes. There are two main types of chests in the current design.

#### Regular storage chests

Regular chests are crafted by the player from materials such as wood and other basic resources. They are part of the Building category in the crafting book and, once crafted, can be placed in the world like other building items.

Behavior:

- After placement, a chest appears as an interactable object in the world.
- The player can open a chest by right clicking it.
- Opening a chest brings up a chest panel that shows:
  - The chest’s internal storage slots.
  - The player’s inventory on the side, to make moving items between them easy.
- The player can move items between chest and inventory using the same interaction patterns as inside the inventory.
- The chest panel can be closed by pressing `E` or `Esc`.
- If the player walks away from the chest beyond a certain radius, the chest panel closes automatically, and interaction stops.

Regular chests are used to:

- Store surplus resources and items that the player does not want to carry.
- Organize items by type across multiple chests (for example one chest for ores, one for food).
- Build a sense of a growing base with storage.

#### Dungeon and loot chests

Dungeon or loot chests are placed by world generation in specific structures and scenes, such as shrines, dungeons or boss arenas. They are not crafted by the player. Instead, they act as one-time rewards for reaching specific locations.

Characteristics:

- Dungeon chests contain pre spawned items that are generally stronger or rarer than what the player could craft at that moment.
- The contents can be fixed per chest type or randomized within a defined loot table (for example "snow shrine chest" has a set of possible items appropriate for that area).
- Once a dungeon chest has been looted, it may remain empty if opened again, or visually change to indicate it has been used.

Interactions with dungeon chests are identical to regular chests (right click to open, `E`/`Esc` to close, auto close when moving away), so the player does not need to learn a new UI. The difference is purely in how the chest appears (world placement) and what items it contains.

In the future, additional chest tiers (for example silver, gold, rainbow) can be added to signal different loot quality or storage capacity.

### 5.5 Relationship to other systems

The item and inventory system described here is closely connected to several other parts of the game:

- The **crafting book** creates items and building pieces and puts them into the inventory. It relies on item categories and stack rules to work correctly.
- The **building system** uses placeable items from the Building category and the hotbar to place structures like beds, chests, doors, walls and floors into the world.
- The **mining system** consumes and produces items:
  - Tools (pickaxes and similar items) are held in the hotbar and used to mine nodes.
  - Mining drops resource items that go into the inventory and later become ingredients for crafting.
- The **hunger and survival system** uses food items as consumables, stored and moved through inventory and chests.

Because of this, the item and inventory system must remain stable and predictable. Once its basic behavior is implemented and feels smooth, all higher level systems can be built on top of it without needing to change how players move and manage items.

---

## 6. Crafting system

Crafting lets the player turn gathered resources into tools, equipment, potions, food and building pieces. In this game, crafting is centered around a special object: the **crafting book**. The book acts as an in universe explanation for how the wizard remembers recipes and, at the same time, as the main user interface for all crafting operations.

The crafting system should feel powerful and convenient, but not abusable. The player should be able to open the book almost anywhere and turn raw materials into useful items, but only if they have created a safe moment and are not in immediate danger.

### 6.1 Crafting book concept

The crafting book is a unique item associated with the player. It is not just another object that can be dropped or lost; it is more like a permanent tool or menu:

- The book is always available to the player. It may be shown as an item in a special slot, or as a separate button or hotkey, but the key point is that it cannot be dropped or destroyed.
- Opening the book reveals the crafting interface. This is the single, unified place where the player can see what can be crafted and with which ingredients.
- The book reinforces the fantasy that the wizard is slowly rebuilding their knowledge: recipes represent things they have “remembered” or learned, rather than random combinations.

The expectation is that once the player understands that all crafting happens through this book, they no longer need to search the world for special crafting stations for basic tasks. Additional specialized stations can still exist later, but the standard way to craft is always through the book.

### 6.2 Categories and recipe display

Inside the crafting book, recipes are organized into clear categories so the player can quickly find what they are looking for. Planned categories include:

- **Equipment** – weapons, armor, tools and other items that change stats or combat power.
- **Building** – structures and placeable objects such as beds, doors, chests, walls and floors.
- **Food** – raw and cooked food items that restore hunger and sometimes health.
- **Potions** – consumable items that restore mana, grant temporary buffs or have other magical effects.
- **Other / Miscellaneous** – items that do not fit neatly into the previous categories (for example special components, keys, or one off quest items).

When the player selects a category, the book shows all known recipes in that category as a grid or list of icons. For each recipe:

- Hovering over the recipe brings up a detail panel with:
  - Name and icon of the resulting item.
  - Short description.
  - List of required ingredients and amounts, using the same icons and names as in the inventory.
- Recipes for which the player currently has all the required ingredients are displayed normally and are interactable.
- Recipes for which the player is missing some ingredients are displayed in a dimmed or slightly transparent style, indicating that they are currently unavailable.

The design goal is that the player can open the book and instantly answer two questions:

1. “What can I craft right now with what I have?”
2. “What would I need to craft item X?”

This means the book doubles as both a crafting interface and a visual recipe encyclopedia.

### 6.3 Crafting rules and constraints

Crafting with the book is intentionally restricted by a few rules so it does not trivialize the survival aspects:

- The player can only start crafting when there are **no enemies nearby**.  
  The game checks for hostile entities within a certain radius around the player. If any are detected, the “Craft” action is blocked or disabled, and the player must first move to safety or deal with the threat.

- Crafting takes **time** rather than being instantaneous.  
  When the player chooses to craft an item:
  - A short progress indication is shown.
  - The player character is considered busy. They cannot start another craft until the current one finishes.
  - Higher level or more complex items can take longer than simple ones.

- The game is **not paused** while crafting.  
  Enemies and other world events continue to update. If an enemy approaches while the player is crafting, they might be in danger once the craft finishes. This creates a similar tension to Ornn’s in field crafting in League of Legends: crafting in the open is convenient but risky.

- Crafting consumes ingredients from the player’s inventory.  
  If an item requires, for example, 5 pieces of wood and 2 pieces of ore, those exact items must be present. After crafting, the ingredients are removed and the resulting item is added to the inventory, assuming there is space.

- If the inventory has no free slot for the crafted item, crafting may be prevented or must clearly warn the player.  
  Dropping crafted items on the ground is possible but should not happen silently.

These rules should make crafting feel like a powerful tool that the player can use almost anywhere, but only when they have earned a safe moment and gathered enough resources.

### 6.4 Crafting and progression

The crafting book is tightly connected to progression:

- New recipes can be unlocked by:
  - Discovering new resources in distant biomes.
  - Completing certain scenes or minigames in the parallel world.
  - Meeting NPCs who teach recipes.
- Higher tier equipment, potions and building pieces require materials from deeper parts of the world or the parallel world.

The player should feel a clear arc:

- At the start, the book only contains a small set of simple recipes (basic tools, basic food, a starter chest and bed).
- Over time, the book fills with more complex options, but everything remains organized in the same interface, so it never becomes confusing.

The **Building** category is especially important because it bridges directly into the building system described next.

---

## 7. Building system

Building allows the player to place functional structures and simple architectural elements in the world. The goal is not to recreate a full voxel building game, but to give the player enough tools to create a recognizable home base with a bed, storage, doors and a bit of structure around it.

### 7.1 Scope and philosophy

The building system is intentionally limited in scope:

- The focus is on **functional structures** and **simple walls and floors**.
- There are no trenches, tall mountains or complex vertical constructions.
- There are no dozens of block shapes and micro building parts.

This limitation keeps the implementation realistic for the thesis while still supporting:

- A sense of “this is my place” when the player looks at their base.
- Basic protection from enemies, if doors and walls block movement as designed.
- A physical location for important objects like beds and chests.

The player should feel that building is useful and satisfying, but not be overwhelmed by a massive building catalog.

### 7.2 Building items and the Building category

All building items are obtained through the **Building** category in the crafting book. Typical building items include:

- **Bed** – lets the player sleep and enter the parallel world.
- **Door** – allows passage through walls and can potentially block enemies.
- **Chest** – provides storage for items (as described in the inventory section).
- **Walls** – simple segments used to form rooms or enclosures.
- **Floors** – tiles used to visually mark interior areas or walkable surfaces.

Each of these items behaves like a normal item when in the inventory:

- It has an icon and stack rules.
- It can be placed into the hotbar.
- It can be moved between inventory and chests.

The difference is that using a building item in the world does not consume it like a potion; instead, it places a persistent structure in the game world.

### 7.3 Placement and use

The general rule for building is:

- To place a building item, the player selects it in the hotbar and **right clicks** on a valid position in the world.

When the player right clicks:

- If the position is valid and empty, the corresponding structure appears:
  - Placing a bed creates a bed object that the player can later interact with to sleep.
  - Placing a chest creates a chest that can be opened and used for storage.
  - Placing a wall segment creates a piece of wall.
  - Placing a floor tile changes the ground under the player to a floor.

- If the position is invalid, nothing happens and some feedback (for example a small sound or visual) indicates that placement is not allowed.

Examples of invalid positions:

- Another solid object (like an existing wall or chest) already occupies that space.
- The position is out of bounds, too far away from the player, or on an unsuitable ground type if such constraints are defined.

Removal and modification:

- The system should allow placed structures (except possibly certain special ones) to be removed or mined later, returning either the item or some of the materials. The exact rules can be adjusted, but players generally expect to be able to fix mistakes when building.
- For the thesis, even a simple “destroy and drop item” behavior is enough, as long as it is consistent.

The building experience should be simple:

- Players craft building items from resources.
- They put them on the hotbar.
- They right click to place them and shape a small base over time.

No separate building mode is necessary; building is just another action performed via items.

---

## 8. Mining and resource gathering

Mining connects the world to the crafting and building systems by providing the raw materials needed for tools, equipment and structures. At the same time, it should reinforce the wizard fantasy rather than feeling like ordinary manual labor.

### 8.1 Mining fantasy

In many survival games, the player swings a pickaxe manually to mine. Here, the character is a wizard, so the mining animation and feeling should be different:

- When the player mines with a tool, the tool should appear to **float** and strike the target under magical control.
- The player character stands near the resource node and channels their magic through the tool rather than physically swinging it.

This subtle change helps maintain the idea that everything the player does, even mundane tasks like mining, is mediated by magic.

### 8.2 Tools, hands and mining input

Mining can be performed in two ways:

- With a **tool** (for example a magical pickaxe or another mining implement).
- With **bare hands**, for very weak or soft materials.

The interaction rule is:

- To mine, the player holds a mining capable item in the hotbar and **holds left click** while aiming at a mineable object in the world.

While left click is held:

- A mining action is performed repeatedly or as a continuous progress until the resource node is broken and drops items.
- Using a proper tool makes mining significantly faster and allows mining of harder materials.
- Using bare hands is permitted, but:
  - It is much slower.
  - It may be limited to basic blocks like very soft stone or dirt.

The expectation is that mining without tools is only used in emergencies or at the very beginning of the game. As soon as the player can craft or find a basic mining tool, they should prefer it.

### 8.3 Resource nodes and drops

Resource nodes are world objects or tiles that can be mined. Typical examples:

- Stone nodes that drop stone or rock items.
- Ore nodes that drop metal ore of various types.
- Special nodes in the parallel world that drop Memory related materials.

Each node defines:

- What tool or minimum tool “strength” is needed to mine it efficiently (for example some nodes might be extremely slow with bare hands).
- How many hits or how much time is required to break it with a given tool type.
- What items it drops on destruction and in what quantities.

The relationship between mining and crafting is straightforward:

- The player explores the world and finds nodes.
- Mining nodes produces resource items that go into the inventory.
- These items become ingredients in the crafting book for tools, equipment, building items, potions and so on.

Even with a small number of node types and resources, this creates a basic progression: better tools and equipment require rarer materials from deeper or more dangerous parts of the world.

### 8.4 Mining, combat and safety

Mining does not pause the game, so it carries similar risks to crafting:

- While the player is focused on mining, enemies may approach from off screen.
- Mining in exposed locations is dangerous; mining in caves or near the base might be safer but still not perfectly safe.

The intended behavior is:

- The player must occasionally look around and make sure the area is clear before committing to longer mining sessions.
- If enemies appear, the player should interrupt mining, deal with the threat, and then continue.

This keeps mining integrated into the survival loop instead of being a completely separate, risk free activity.

By connecting mining visually to magic (floating tools), logically to crafting (resource flow) and mechanically to risk management (time spent in dangerous areas), the game maintains coherence between the wizard fantasy and the standard survival sandbox mechanics.

---

## 9. Topics to be defined later

The following systems are acknowledged but not yet fully specified. They will each receive their own chapters or sections later:

- Battle system:
  - Basic attacks.
  - Spell casting using Words and Phrases.
  - Damage, armor and death rules.

- Enemies:
  - Enemy types, behaviors and AI (using the FSM architecture).
  - Spawn rules and difficulty scaling.

- Parallel world and minigames:
  - Detailed rules for entering and leaving.
  - Memory progression.
  - Minigames that reward Words.

- Audio:
  - Music and ambient sound design.
  - Sound effects for actions such as mining, crafting, building, eating and spellcasting.

- Saving and loading:
  - What is stored in save files.
  - When saving occurs (manual, autosave).
  - How world state, player state and progression are persisted.

---

