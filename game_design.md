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

## 6. Items, inventory, equipment and chests

### 6.1 Item model and expectations

Items are central to the game. Each item should at least define:

- **Name**  
  Descriptive, used in UI.

- **Icon**  
  Distinctive and readable at small sizes.

- **Category**  
  Examples: weapon, armor, tool, consumable, resource, building, crafting material.

- **Stacking rules**  
  - Maximum stack size (for example 1 for equipment, higher for resources).
  - Whether it can be stacked at all.

- **Basic behavior flags**  
  For example:
  - Consumable: can be used to apply an effect.
  - Placeable: creates a structure when used.
  - Tool: can mine or interact with specific world elements.

Expectations:

- The item system should be generic and data driven so that new items are mostly added by creating new data entries rather than new code.
- Items should be self explanatory in the UI: the player should understand what an item does from its icon, name and tooltip.

### 6.2 Player inventory and hotbar

The player has:

- **Backpack inventory**  
  A grid of slots that can store items.

- **Hotbar**  
  A row of slots linked to the backpack, representing quick access items.

Behavior:

- Items can be moved between slots, equipped to the character panel, or moved between inventory and chests.
- The hotbar displays which items are usable quickly. One of the hotbar slots is always selected and used for context actions such as mining or placing blocks.
- The interaction behavior for moving items (left click, right click, shift click, etc.) follows the same conventions as Minecraft:
  - Players can split stacks, move full stacks quickly, and combine stacks efficiently.
  - The intention is that experienced players feel at home and new players can learn from existing knowledge of other games.

Expectations:

- Inventory operations should feel smooth and responsive, without surprising restrictions.
- Players should be able to manage inventory and move items between chests and backpack efficiently, without fighting the UI.

Implementation notes:

- Inventory UI does not pause the game, so interactions must be possible while the player character is standing in the world.
- The system should support more inventory space in the future if needed.

### 6.3 Equipment panel

The player has a character panel with equipment slots for:

- Head.
- Chest.
- Legs.
- Weapon or main hand.
- Off hand (if used later).
- Possible accessory slots.

Behavior:

- Only compatible items can be placed into a slot (for example only helmets into the head slot).
- Equipping an item updates the players stats and, if possible, their visual appearance.
- Removing an item returns it to the inventory if there is space.

Expectations:

- The equipment panel should clearly show which slots exist, what is equipped where, and any obvious stat changes.
- For the thesis, a small number of equipment pieces is enough to demonstrate the system.

### 6.4 Chests

There are two main chest types:

1. **Regular storage chest**
   - Crafted by the player using materials.
   - Used to store items persistently in the world.
   - Can be placed as a structure from the Building category of the crafting book.
   - Opened with right click, closed with `E` or `Esc`, or automatically when the player walks away.

2. **Dungeon or loot chest**
   - Spawned by world generation in specific scenes (for example near shrines, in dungeons).
   - Contains pre defined or randomly selected loot that is better than what the player can craft at that point.
   - May be a one time reward; once looted, it may remain empty.

Future extension:

- Additional chest tiers (for example silver, gold, rainbow) could indicate better loot quality and larger storage, but this is not required for the thesis.

Expectations:

- Chest interactions should mirror inventory behavior to avoid confusion.
- Auto closing chests when leaving a radius prevent the player from forgetting open windows and losing context.

---

## 7. Crafting system - Crafting book

### 7.1 Concept

Crafting is handled through a special **crafting book** item. This book functions as a universal crafting interface that the player always has access to.

Key characteristics:

- The crafting book can be opened to show a crafting UI.
- Inside, recipes are grouped into categories such as:
  - Equipment.
  - Building.
  - Food.
  - Potions.
  - Other or Miscellaneous.
- The player browses categories and sees all known recipes in that category.

The crafting book is intentionally strong, because it allows the player to craft complex items anywhere in the world. To balance this:

- The book cannot be dropped from the inventory.
- Crafting is restricted based on nearby enemies and time.

Expectations:

- The crafting system is easy to understand and does not require remembering hidden patterns.
- The crafting book acts as an in universe explanation of how the wizard remembers recipes.

Implementation notes:

- The book may live in a special non droppable slot or exist as an always available function, but the UI should make it clear how to access it.
- Recipes are data driven and can be extended after thesis work.

### 7.2 Recipe presentation and feedback

When the crafting book is open:

- The player selects a category.
- The UI shows all recipes in that category as icons or rows.
- When the player hovers over a recipe, a detail panel shows:
  - The name and description of the resulting item.
  - The required ingredients, including amounts and icons.

Visual feedback:

- Recipes that are currently craftable (all ingredients available) are fully lit and interactable.
- Recipes that are not craftable are shown darker or with reduced opacity to signal that they are unavailable right now.
- This gives a quick overview of what is possible with the resources in the inventory and chests.

Expectations:

- The player does not have to guess unknown recipes. Everything they know is visible.
- The UI should be fast to read and not require navigating deep hierarchies.

### 7.3 Crafting rules and constraints

Crafting through the book follows several rules to keep it balanced and connected to gameplay:

- The player can only craft if **no enemies are nearby**. If an enemy comes within a defined radius, crafting actions are blocked or canceled.
- Crafting takes **time**, not instantaneous:
  - When the player chooses to craft an item, a short crafting animation or progress is played.
  - High value or complex items can take slightly longer.
- The player remains vulnerable during crafting, because the game is not paused.
- Crafted items are placed into the inventory if there is space; otherwise crafting may be blocked or must be confirmed if it results in dropping items.

Expectations:

- Crafting feels like a deliberate action that must be done in relatively safe conditions, similar to Ornn's in field crafting from League of Legends.
- Crafting times must be short enough to avoid feeling tedious, especially for basic items.

Implementation notes:

- The radius for "no enemies nearby" can be tuned later.
- Recipes can be unlocked over time based on world progression, parallel world progression or NPC interactions.

### 7.4 Building category

The Building category of the crafting book is special and is described in more detail in the next section. It contains only items that result in placeable structures when used, such as:

- Beds.
- Doors.
- Chests.
- Simple walls and floors.

---

## 8. Building system

### 8.1 Scope and expectations

Building in this game is intentionally limited compared to fully voxel based sandboxes. The focus is on:

- Placing functional structures that support survival and progression.
- Creating simple shelters and rooms using walls and floors.

There are no trenches, vertical mountains or complex block architecture. Full block style terrain building with many block types is out of scope for the thesis.

### 8.2 Building items

All building related items are obtained via the Building category in the crafting book. Examples include:

- **Bed**  
  Used for sleeping and entering the parallel world.

- **Door**  
  Allows entrances to rooms; potentially blocks enemies if desired.

- **Chest**  
  Persistent storage, as described earlier.

- **Walls and floors**  
  Simple tiles that allow the player to create enclosed spaces.

Expectations:

- The set of building items for the thesis is small but representative.
- Building items clearly indicate their function through icon and description.

### 8.3 Placement and interaction

Building works as follows:

- The player crafts a building item from the crafting book.
- The item appears in the inventory and can be placed in the hotbar.
- While holding a building item in the hotbar, the player can **right click** on the world to place it at the targeted valid position.

Rules:

- Certain building items may only be placed on specific ground types or within a certain radius of the player.
- Placement fails (with feedback) if:
  - Another blocking object is present.
  - The location is invalid (for example outside the playable area).

Expectations:

- Building placement must feel responsive and predictable.
- The system should support removing or mining placed structures later, at least for simple tiles, but detailed destruction rules can be decided later.

Implementation notes:

- Since only walls and floors are planned, the world remains mostly 2D topdown without vertical building.
- Beds, chests and doors are more complex objects with interaction scripts.

---

## 9. Mining and resources

### 9.1 Concept and fantasy

Mining allows the player to extract resources from the environment, such as stone, ore and special materials. It serves two purposes:

- Provides resources for crafting tools, equipment and building items.
- Supports the wizard fantasy by showing how the wizard manipulates tools magically rather than using them by hand.

### 9.2 Tools and mining behavior

The player can mine using:

- **Tools**  
  Special items such as pickaxes or similar mining tools.

- **Bare hands**  
  Allowed, but slower and possibly limited to weaker materials.

To preserve the wizard theme:

- Tools are not swung manually by the character. Instead, when mining, the tool is visualized as floating next to the player and hitting the target, controlled by magic.
- Mining is performed by **holding left click** while a tool type item is active in the hotbar.

Expectations:

- Mining should be straightforward: aim at a mineable object and hold the mining input until it breaks and drops resources.
- Using proper tools should feel significantly more efficient than bare hands, so there is a clear incentive to craft and upgrade tools.

Implementation notes:

- Different materials can have different hardness and drop tables, but for the thesis a small set of materials is enough.
- Mining may also be used in the parallel world to gather Memory related resources, but those details are specified later.

---

## 10. Hunger and food

### 10.1 Motivation and expectations

Hunger adds a lightweight survival pressure to the game. It should:

- Encourage the player to obtain food regularly.
- Create interesting decisions about when to explore, when to return to base and when to seek food.
- Interact with other systems without overshadowing them.

Hunger is not intended to be a punishing micromanagement system. It should be simple enough to understand and handle, especially with the help of NPCs that provide food.

### 10.2 Basic behavior

- The player has a **hunger bar** visible in the HUD.
- Hunger gradually decreases over time as the player performs actions.
- When hunger is above a certain threshold, the player behaves normally.
- When hunger falls too low:
  - The player may suffer penalties such as slower movement, reduced regeneration, or periodic health loss.
  - The exact effect can be tuned so that it is noticeable but not immediately fatal.

The player can refill hunger by:

- Eating food items obtained from:
  - NPCs that provide food or drop food when defeated.
  - Loot from chests.
  - Later possibly crafted recipes.

Expectations:

- The player should not be forced to eat every few seconds. Hunger depletion speed must be balanced to allow meaningful exploration.
- Food variety can be minimal at first, focusing on a few basic items with clear effects.

Implementation notes:

- NPC design and food item definitions will be elaborated in later chapters.
- The crafting book will contain a Food category once there are enough recipes.

---

## 11. Topics to be defined later

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

