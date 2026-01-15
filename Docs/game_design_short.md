# Project Vision

This file is the source of truth for the project vision and gameplay rules.
It is a condensed, implementation-oriented summary of the full game design document.

## Elevator pitch

A 2D top-down pixel art survival sandbox game set in a fantasy world.
You explore a procedurally generated map, mine and loot resources, craft equipment and consumables using a universal crafting book, and fight enemies using spells assembled from discovered Words into Phrases.
A long-term objective is to regain spellcasting knowledge and eventually defeat the ultimate boss.

## Pillars (non-negotiables)

1) Exploration and survival in a procedurally generated world  
- Different seeds should meaningfully change the world so players cannot memorize optimal routes.
- Biomes must have clear identity (tiles + decorations).
- Some handcrafted scenes, NPCs, or bosses are tied to biomes, but their positions vary between worlds.

2) Progression through spell rediscovery  
- Spells come from combining permanent Word unlocks into temporary Phrases during combat.
- New Words expand player options and enable access to harder regions and encounters.

3) Parallel world as Word source  
- Parallel world provides Words via short minigames.
- Difficulty and rewards scale with Memory levels used.

4) Crafting is readable and convenient, but not free  
- Crafting uses a universal crafting book UI.
- Crafting takes time and does not pause the game.

## Thesis scope vs future scope

### In scope (thesis prototype)
- Single world (no world management UI).
- World generation at new run (seed-based determinism).
- Core HUD and controls.
- Inventory + equipment + reward chests (loot-only).
- Mining loop with tool tiers and drops.
- Crafting book (categories, constraints, output rules).
- Combat with a limited Word set (target ~14-16 total Words).
- Basic friendly NPCs (dialogue hints), basic enemy set, and a phase-based boss.

### Out of scope (future work)
- Save/load system.
- Building system (placeable walls/floors, player-made storage).
- World management UI (multiple worlds, rename/delete).
- More content (biomes, enemies, items).
- Parallel world expansion (more minigames, more complex Word rewards).
- Tutorial/intro flow.

## Core game loop

1. Explore the world to reveal new areas and locate resources, threats, and points of interest.
2. Gather materials by mining, collecting, and looting while managing survival constraints (health, mana, hunger).
3. Craft and prepare using the crafting book (tools, equipment, consumables, special progression items).
4. Fight enemies using weapons and spells assembled from Words and Phrases.
5. Progress by improving character capabilities and unlocking new Words, enabling access to more challenging regions and encounters.

## World generation

### Goals and constraints
- Determinism: same seed produces the same world layout.
- Exploration value: different seeds change biome positions, terrain variation, and structure placement.
- Biome identity: distinct ground tiles and decorations per biome.
- Biome-bound scenes: structures and special scenes are tied to biomes but positioned per seed.

### Generation process (high level)
1. Seed initialization: initialize a deterministic random generator from the world seed.
2. Biome layout: assign each world position to a biome.
3. Terrain refinement: choose concrete ground tile variants inside each biome.
4. Decoration placement: place trees/rocks/plants/props based on biome rules.
5. Structure placement: stamp handcrafted templates (shrines, camps, arenas, villages, dungeons).

Notes:
- Biomes use a Voronoi-style layout (closest biome center wins), producing large irregular regions.
- Optional constraints can prevent abrupt biome transitions (allowed neighbor rules).
- Spawn is placed inside the center of the map.

## Player, stats, and progression

### Runtime state (player)
- Health / max health
- Mana / max mana
- Regular XP and level
- Memory XP and Memory level
- Inventory state (pages, unlocked slots, equipment, hotbar assignment)
- Unlocked Words (permanent unlocks)

### Defeat and respawn
- Player is defeated at 0 health and respawns.
- Respawn is tied to the Mage Bedroll:
  - Respawn at last sleep location.
  - If never slept, respawn at world start position.
- Defeat applies a partial loss of backpack items (random selection + fixed percentage).
  - Future tuning: adjust percentage, exclude rare categories if needed.

## UI, HUD, controls, and panel rules

### HUD (minimum)
- Health bar
- Mana bar
- Hunger bar
- Regular XP/level indicator
- Memory XP/level indicator
- Hotbar
- Minimap (drives exploration reveal)
- In-game time indicator (day/night)

### Default controls (initial)
- Move: W A S D
- Mine/attack (context): Left mouse button (hold for mining)
- Interact/place (context): Right mouse button
- Inventory: E (toggle), Esc (close)
- Map: M (toggle), Esc (close)
- Crafting book: B (toggle), Esc (close)
- Pause/settings: Esc (only when no other panel is open)

### Panel and pausing rules
- Only one major in-game panel open at a time (inventory, chest UI, map, crafting book, settings).
- If a major panel is open, pressing its hotkey or Esc closes it.
- Non-pausing panels: inventory, chest UI, map, crafting book (world continues).
- Pausing panel: settings menu (stops time and enemy AI).

### Map reveal
- Map starts unexplored.
- Minimap reveals a local region around the player.
- Full map displays all areas revealed on the minimap.
- Future: zooming and dragging.

## Items and inventory

### Item definition (core properties)
- Name (tooltips, recipes, UI labels)
- Icon (inventory/hotbar/crafting book)
- Category (weapon, armor, tool, consumable, resource, key/special)
- Stacking rules (max stack size, stackable or not)

### Item categories (planned)
- Weapons (wands/orbs, tiered by materials/upgrades)
- Armor (helmet, chest, pants, boots)
- Tools (mining tools are most important)
- Consumables (food) - restores hunger (optionally health)
- Consumables (potions) - restores mana or temporary effects
- Resources (wood, stone, ores)
- Key and special items (Mage Bedroll, inventory upgrades)

### Player inventory + hotbar
- Backpack grid for general storage + hotbar for quick access.
- Active hotbar item performs context action:
  - Mining if tool
  - Attacking if weapon
  - Placing/deploying if placeable item (example: Mage Bedroll)
  - Consuming if food/potion
- Inventory interactions follow typical conventions (move stacks, split stacks, quick transfers).

### Inventory size and upgrades
- Inventory is page-based.
- Each page contains a fixed grid (example: 24 slots).
- Only first page available by default; later pages are visible but locked (disabled/dimmed).
- Additional capacity is permanently unlocked via a special upgrade item
  (example: Inventory Expansion Scroll crafted in the crafting book).
- Upgrade step increases usable slots by a fixed amount (example: +10) until next page unlocks.

### Equipment slots
- Head, chest, legs, boots
- Accessory slots (rings, amulets, necklaces)
- Only compatible items can be equipped.
- Equipping modifies player stats (armor, health, mana capacity, damage, etc).

### Reward chests (loot-only)
Because building and housing are postponed, there are no player-made storage containers.
Chests are rewards for exploration:
- World generator places them in handcrafted structures, and they can also be rare enemy drops.
- Chest UI opens on right click, shown alongside player inventory.
- Items can be taken from chest into player inventory.
- Chest is not long-term storage (no depositing items).
- After looting, chest becomes empty and can be removed or marked as opened.
- Chest UI closes with E or Esc, and closes if the player moves too far away.

## Mining system

### Input and tools
- Mining can be done with a tool or bare hands:
  - Tool mining is faster and enables mining harder materials.
  - Hand mining is allowed early-game but slow and limited.
- Rule: hold a mining-capable item in the hotbar and hold LMB while aiming at a mineable node.

### Resource nodes and drops
- Nodes are tiles/objects that can be mined (stone nodes, ore nodes, biome resources).
- Each node defines:
  - tool requirements (minimum strength or tool type)
  - time required per tool tier
  - drop table (items and quantities)
- Mining yields resources used as crafting ingredients.
- Progression: better tools require rarer materials found deeper or in more dangerous biomes.

## Crafting system (Crafting Book)

### Crafting book
- Universal crafting interface, always available and visible in UI.
- Serves as both crafting UI and recipe encyclopedia (what exists + required ingredients).

### Categories
Planned categories:
- Equipment (weapons, armor, tools)
- Food
- Potions
- Other (special components, Mage Bedroll, inventory upgrade scrolls)
- Future: Building (walls/floors/storage) if base building is implemented

### Recipe display rules
- Recipes shown as icons in a grid/list.
- Craftable recipes are normal and interactable.
- Unavailable recipes are dimmed to show missing ingredients.
- Future: hovering a recipe shows full details (output, description, ingredient list).

### Crafting constraints
Crafting is intentionally restricted so it does not trivialize survival:
- Crafting takes time (shows progress, cannot be spammed instantly).
- Game is not paused while crafting is in progress.
- Inventory capacity matters:
  - crafting requires a free slot for non-stackable outputs
  - if no space, prevent crafting and show explicit reason

### Crafting output rule
- When crafting starts, ingredients are consumed from player inventory.
- Crafted item is added to the inventory.

## Combat system (Words, Phrases, Spells)

### Definitions
- Word: permanent unlock, a single building block.
- Phrase: temporary combination of Words selected during combat.
- Spell: the actual cast action produced by a completed Phrase.

### Word categories
Each Word belongs to exactly one category:
- Modifier: adds one behavior rule (piercing, exploding, etc)
- Form: delivery behavior (beam, ball, etc)
- Element: damage type + visual theme (palette, impact effects)

Thesis scope uses a limited set (target around 14-16 total Words) for feasibility and balance.

### Acquiring Words
- Starting selection: at world start, player chooses a small starter set
  (example: one Form, one Element, one Modifier).
- Parallel world rewards: minigames reward Words.
- Exploration rewards: rare Words can come from special discoveries (shrines, etc).
- No duplicates:
  - once a Word is unlocked, it is removed from reward pools
  - pools contain only Words not yet obtained

### Phrase construction rule
- A Phrase consists of exactly:
  - 1 Form
  - 1 Element
  - 1 Modifier
- After selecting these 3 Words, the Phrase is complete and the spell is cast.
- Player stores Words permanently, not full Phrases as permanent bindings.

### Spell layering (design intent)
A spell is formed from three layers:
- Modifier adds behavior rule (pierce, explode, etc)
- Form controls delivery (beam movement, projectile, area burst)
- Element controls damage type and visuals
This allows shared visuals across multiple spells while keeping different behavior.

### Casting UI
- Casting uses a dedicated spellcasting panel in the HUD.
- Panel contains three stable groups of slots:
  - Form slots
  - Element slots
  - Modifier slots
- Stable positions: a Word always appears in the same slot position when visible.

## Actors (living entities)

### Player representation
- SpriteRenderer + Animator
- Colliders for movement collision and interaction triggers
- Rigidbody-based top-down movement
- Combat component (damage, attacking, spellcasting)
- Player connects to inventory/items, crafting, mining, and parallel world.

### Friendly NPCs (thesis scope)
Purpose: atmosphere, dialogue, exploration hints.
- Do not participate in combat (thesis scope).
Planned types:
- Fairy (short hints near points of interest)
- Villagers (variants, ambience and small messages)
Behavior states (simple):
- Idle
- Patrol
- Talk (when interacting and dialogue is active)
Future: schedules, trading, simple quests.

### Enemies and boss
- Enemies provide combat challenge and loot.
- Boss is chase-focused and phase-based, designed to be readable and feasible:
  - Phase 1 (100% to 70%): chase + standard attacks
  - Phase 2 (70% to 30%): special mechanic (inverted movement controls for a duration)
  - Phase 3 (30% to 0%): stronger aggression (speed, damage, shorter cooldowns) + extra pattern
  - Phase 4 (at 5%): boss tries to use the memory-erasing spell (TBA)

## Parallel world (high level)

- Parallel world is a distinct loop used to earn Words and drive Memory progression.
- Contains short minigames with difficulty scaling and Memory costs.
- Example minigame types (planned):
  - Sequence repetition (repeat highlighted symbol sequence; scaling via symbol count, target length, playback speed)
  - Timed collection (collect X fragments in Y seconds; scaling via X, Y, spawn behavior, optional decoys)
