using System;
using System.Collections.Generic;
using System.Text;
using TextCopy;

/// <summary>
/// This program will output lines that need to be inserted into the AHK file found in this codebase.
/// Then, run that resulting AHK (autohotkey V2) file and use the f8 key in your bisect command terminal 
/// to input these commands and build your RAD2 minecraft starting base.
/// </summary>
class Program
{
    // ---------- Core helpers (keep fill order: x z y) ----------
    static void GetFillCommand(
        int x1, int z1, int y1,
        int x2, int z2, int y2,
        string materialName,
        List<string> createCommands,
        List<string> clearCommands,
        bool isHollow = false)
    {
        // Normalize corners
        int minX = Math.Min(x1, x2), maxX = Math.Max(x1, x2);
        int minZ = Math.Min(z1, z2), maxZ = Math.Max(z1, z2);
        int minY = Math.Min(y1, y2), maxY = Math.Max(y1, y2);

        if (isHollow)
            createCommands.Add($"/fill {minX} {minZ} {minY} {maxX} {maxZ} {maxY} {materialName} hollow");
        else
            createCommands.Add($"/fill {minX} {minZ} {minY} {maxX} {maxZ} {maxY} {materialName}");

        clearCommands.Add($"/fill {minX} {minZ} {minY} {maxX} {maxZ} {maxY} air");
    }

    static void SetBlock(int x, int z, int y, string material, List<string> createCommands, List<string> clearCommands)
    {
        createCommands.Add($"/setblock {x} {z} {y} {material}");
        clearCommands.Add($"/fill {x} {z} {y} {x} {z} {y} air");
    }

    // Describe an item to put in a chest
    public record ItemSpec(string Id, int Count = 1, int Slot = -1);
    // Slot: -1 means "auto-assign next slot 0..26". Use 0..53 for double chests.

    static void FillChestItems(
        int x, int z, int y,   // NOTE: your coordinate order is x z y
        List<ItemSpec> items,
        List<string> create)
    {
        if (items == null || items.Count == 0) return;

        int next = 0; // auto slots
        foreach (var it in items)
        {
            int slot = it.Slot >= 0 ? it.Slot : next++;
            // In 1.16.5: /replaceitem block <x> <z> <y> container.<slot> <item> <count>
            create.Add($"/replaceitem block {x} {z} {y} container.{slot} {it.Id} {it.Count}");
        }
    }
    static void PlaceDoorAt(int x, int z, int y, string facing,
                        List<string> create, List<string> clear)
    {
        // Carve a 2-block-tall opening at (x, z..z+1, y)
        GetFillCommand(x, z, y, x, z + 1, y, "air", create, clear);

        // Place door parts — your coord order is (x z y)
        SetBlock(x, z, y, $"minecraft:oak_door[half=lower,facing={facing}]", create, clear);
        SetBlock(x, z + 1, y, $"minecraft:oak_door[half=upper,facing={facing}]", create, clear);
    }

    // Color-code each starting location (one entry per 4-chest set).
    // Replace with your own list of block IDs.
    static readonly string[] StartingMarkerBlocks = new[]
    {
        "minecraft:red_wool",
        "minecraft:lime_wool",
        "minecraft:blue_wool",
        "minecraft:yellow_wool",
        "minecraft:purple_wool",
        "minecraft:orange_wool",
        "minecraft:cyan_wool",
        "minecraft:magenta_wool",
        // ...add as many as you want; cycles if fewer than needed
    };

    static void LineWallsWithChests(
        int x, int z, int y,
        int width, int length,
        List<string> create, List<string> clear,
        int doorX, int hy)
    {
        int hx = width / 2;
        int chestZ = z - 1;

        int innerYPos = y + hy - 1; // +Y wall coordinate (facing north)
        int innerYNeg = y - hy + 1; // -Y wall coordinate (facing south)
        int innerXPos = x + hx - 1; // +X wall coordinate (facing west)
        int innerXNeg = x - hx + 1; // -X wall coordinate (facing east)

        // Round-robin index across *all* placements (does not reset per wall).
        int rr = 0;

        // ---- helpers ---------------------------------------------------------
        List<ItemSpec> BuildCherryLoot() => new()
        {
            new("pickletweaks:stone_paxel{Unbreakable:1b}", 1),
            new("pickletweaks:iron_paxel{Unbreakable:1b}", 1),
            new("pickletweaks:diamond_paxel{Unbreakable:1b}", 1),
            new("dungeons_gear:spelunker_boots{Unbreakable:1b}", 1),
            new("dungeons_gear:spelunker_leggings{Unbreakable:1b}", 1),
            new("dungeons_gear:spelunker_helmet{Unbreakable:1b}", 1),
            new("dungeons_gear:spelunker_chestplate{Unbreakable:1b}", 1),
            new("minecraft:anvil", 64),
            new("minecraft:coal", 64),
            new("minecraft:iron_ingot", 64),
            new("sophisticatedbackpacks:diamond_backpack", 1),
            new("sophisticatedbackpacks:stack_upgrade_tier_4", 1),
            new("sophisticatedbackpacks:xp_pump_upgrade", 1),
            new("sophisticatedbackpacks:advanced_pickup_upgrade", 1),
            new("sophisticatedbackpacks:magnet_upgrade", 1),
            new("sophisticatedbackpacks:tank_upgrade", 1),
            new("toms_storage:ts.adv_wireless_terminal", 1),
            new("toms_storage:ts.crafting_terminal", 2),
            new("toms_storage:ts.inventory_cable", 64),
            new("toms_storage:ts.inventory_connector", 8),
            new("minecraft:hopper", 64),
            new("minecraft:comparator", 64),
            new("minecraft:repeater", 64),
            new("minecraft:redstone", 64),
            new("minecraft:redstone_torch", 64),
            new("minecraft:furnace", 8),
            new("minecraft:torch", 64)
        };

        List<ItemSpec> BuildMapleLoot()
        {
            var mapleLoot = new List<ItemSpec>
            {
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new(GetRandomLog(), 64),
                new("minecraft:cobblestone", 64),
                new("minecraft:cobblestone", 64),
                new("minecraft:cobblestone", 64),
                new("minecraft:stone_bricks", 64),
                new("minecraft:stone_bricks", 64),
                new("minecraft:stone_bricks", 64),
                new("minecraft:chiseled_sandstone", 64),
                new("minecraft:sandstone", 64),
                new("minecraft:cut_sandstone", 64),
                new("minecraft:glass", 64),
                new("minecraft:dirt", 64),
                new("minecraft:dirt", 64),
                new("minecraft:grass_block", 64),
                new("minecraft:ladder", 64),
                new("minecraft:oak_door", 16),
                new("minecraft:torch", 64),
                new("minecraft:lantern", 64)
            };
            return mapleLoot;
        }

        List<ItemSpec> BuildDuskLoot()
        {
            var duskLoot = new List<ItemSpec>
            {
                // 10 builders:
                new("minecraft:egg", 16),
                new("minecraft:water_bucket", 1),
                new("minecraft:stone_hoe{Unbreakable:1b}", 1),
                new("cookingforblockheads:cow_jar", 4),
                new("cookingforblockheads:counter", 64),
                new("cookingforblockheads:corner", 64),
                new("cookingforblockheads:oven", 1),
                new("cookingforblockheads:cooking_table", 1),
                new(GetRandomKitchenFloor(), 64),
                new(GetRandomKitchenFloor(), 64),
                new("mysticalworld:aubergine_seeds", 64),
                new(GetRandomSeed(), 64),
                new(GetRandomSeed(), 64),
                new(GetRandomSeed(), 64),
                new(GetRandomSeed(), 64),
                new(GetRandomSeed(), 64),
                new(GetRandomSeed(), 64),
                new(GetRandomSeed(), 64),
                new(GetRandomSeed(), 64),
                new("minecraft:bone_meal", 64),
                new("minecraft:bone_meal", 64),
                new("ars_nouveau:novice_spell_book", 1),
                new("ars_nouveau:glyph_leap", 64),
                new("ars_nouveau:glyph_amplify", 64),
                new("ars_nouveau:glyph_slowfall", 64),
                new("ars_nouveau:mana_condenser", 64),
                new("ars_nouveau:mana_jar", 64),
            };

            return duskLoot;
        }

        List<ItemSpec> BuildLunarLoot()
        {
            // Loot for Lunar chests (–X wall)
            var lunarLoot = new List<ItemSpec>
            {
                new("minecraft:iron_helmet{Unbreakable:1b}", 1),
                new("minecraft:iron_chestplate{Unbreakable:1b}", 1),
                new("minecraft:iron_boots{Unbreakable:1b}", 1),
                new("minecraft:iron_leggings{Unbreakable:1b}", 1),
                new("spartanweaponry:parrying_dagger_stone{Unbreakable:1b}", 1),
                new("bountifulbaubles:wormhole_mirror", 1),
                new("minecraft:bow{Unbreakable:1b}", 1),
                new("minecraft:arrow", 64),
                new("spartanweaponry:bolt", 64),
                new("minecraft:cooked_beef", 64),
                new("minecraft:torch", 64),
                new(GetRandomSpartanWeapon(), 1),
                new(GetRandomSpartanWeapon(), 1),
                new(GetRandomSpartanWeapon(), 1),
                new(GetRandomSpartanWeapon(), 1),
                new(GetRandomSpartanWeapon(), 1),
                new(GetRandomSpartanWeapon(), 1),
                new(GetRandomSpartanWeapon(), 1),
                new(GetRandomSpartanWeapon(), 1),
            };
            AddDungeonGearSet(lunarLoot);
            AddDungeonGearSet(lunarLoot);
            return lunarLoot;
        }

        // Given the round-robin slot (0..3), build loot & pick block id for the given wall-facing.
        // Given the round-robin slot (0..3), build loot & pick block id for the given wall-facing.
        // Also places a marker one block UP and one block BEHIND the chest.
        // "Behind" means opposite the block's facing.
        void PlaceChest(int bx, int bz, int by, int rrIndex, string facing)
        {
            // Determine group color: every 4 chests share the same marker.
            int groupIndex = rrIndex / 4; // 0 for first 4, 1 for next 4, etc.
            string markerBlock = StartingMarkerBlocks[groupIndex % StartingMarkerBlocks.Length];

            // Place chest + loot by round-robin type
            switch (rrIndex % 4)
            {
                case 0:
                    SetBlock(bx, bz, by, $"blue_skies:cherry_chest[facing={facing}]", create, clear);
                    FillChestItems(bx, bz, by, BuildCherryLoot(), create);
                    break;
                case 1:
                    SetBlock(bx, bz, by, $"blue_skies:maple_chest[facing={facing}]", create, clear);
                    FillChestItems(bx, bz, by, BuildMapleLoot(), create);
                    break;
                case 2:
                    SetBlock(bx, bz, by, $"blue_skies:dusk_chest[facing={facing}]", create, clear);
                    FillChestItems(bx, bz, by, BuildDuskLoot(), create);
                    break;
                default: // 3
                    SetBlock(bx, bz, by, $"blue_skies:lunar_chest[facing={facing}]", create, clear);
                    FillChestItems(bx, bz, by, BuildLunarLoot(), create);
                    break;
            }

            // Compute "behind" offset in YOUR coordinate order (x, z, y):
            // z = vertical (up is +1), y = north/south, x = east/west.
            int dx = 0, dy = 0;
            switch (facing)
            {
                case "north": dy = +1; break; // behind north = south (+y)
                case "south": dy = -1; break; // behind south = north (-y)
                case "east": dx = -1; break; // behind east  = west (-x)
                case "west": dx = +1; break; // behind west  = east (+x)
            }

            // One up (bz + 1) and one behind (bx+dx, by+dy)
            int mx = bx + dx;
            int mz = bz + 2;   // up two
            int my = by + dy;

            SetBlock(mx, mz, my, markerBlock, create, clear);
        }
        // ----------------------------------------------------------------------

        // ---- +Y wall (faces north) ----
        for (int xi = x - hx + 3; xi <= x + hx - 3; xi++)
        {
            if (xi == x) continue; // keep center clear for door
            PlaceChest(xi, chestZ, innerYPos, rr++, "north");
        }

        // ---- -Y wall (faces south) ----
        for (int xi = x - hx + 3; xi <= x + hx - 3; xi++)
        {
            if (xi == x) continue; // keep center clear for door
            PlaceChest(xi, chestZ, innerYNeg, rr++, "south");
        }

        // ---- +X wall (faces west) ----
        for (int yi = y - hy + 3; yi <= y + hy - 3; yi++)
        {
            if (yi == y) continue; // keep center clear for door
            PlaceChest(innerXPos, chestZ, yi, rr++, "west");
        }

        // ---- -X wall (faces east) ----
        for (int yi = y - hy + 3; yi <= y + hy - 3; yi++)
        {
            if (yi == y) continue; // keep center clear for door
            PlaceChest(innerXNeg, chestZ, yi, rr++, "east");
        }
    }


    static readonly string[] LogBlocks = new[]
    {
        "minecraft:oak_log",
        "minecraft:spruce_log",
        "minecraft:birch_log",
        "minecraft:jungle_log",
        "minecraft:acacia_log",
        "minecraft:dark_oak_log",
        "blue_skies:bluebright_log",
        "blue_skies:starlit_log",
        "blue_skies:frostbright_log",
        "blue_skies:maple_log",
        "blue_skies:lunar_log",
        "blue_skies:dusk_log",
        "blue_skies:crystallized_log",
        "blue_skies:cherry_log", // fixed missing _log suffix
        "betterendforge:mossy_glowshroom_log",
        "betterendforge:lacugrove_log",
        "betterendforge:end_lotus_log",
        "betterendforge:pythadendron_log",
        "betterendforge:dragon_tree_log", // fixed case
        "betterendforge:tenanea_log",
        "betterendforge:helix_tree_log", // fixed spelling: "heliix" → "helix"
        "betterendforge:jellyshroom_log",
        "betterendforge:lucernia_log"
    };

    static string GetRandomLog()
    {
        return LogBlocks[Rng.Next(LogBlocks.Length)];
    }

    static readonly string[] FloorBlocks = new[]
    {
        "cookingforblockheads:black_kitchen_floor",
        "cookingforblockheads:blue_kitchen_floor",
        "cookingforblockheads:brown_kitchen_floor",
        "cookingforblockheads:cyan_kitchen_floor",
        "cookingforblockheads:gray_kitchen_floor",
        "cookingforblockheads:green_kitchen_floor",
        "cookingforblockheads:light_blue_kitchen_floor",
        "cookingforblockheads:light_gray_kitchen_floor",
        "cookingforblockheads:magenta_kitchen_floor",
        "cookingforblockheads:orange_kitchen_floor",
        "cookingforblockheads:pink_kitchen_floor",
        "cookingforblockheads:purple_kitchen_floor",
        "cookingforblockheads:red_kitchen_floor",
        "cookingforblockheads:white_kitchen_floor",
        "cookingforblockheads:yellow_kitchen_floor"
    };

    static string GetRandomKitchenFloor()
    {
        return FloorBlocks[Rng.Next(FloorBlocks.Length)];
    }

    static readonly string[] SeedItems = new[]
    {
        "minecraft:wheat_seeds",
        "minecraft:melon_seeds",
        "minecraft:pumpkin_seeds",
        "minecraft:beetroot_seeds",
        "simplefarming:barley_seeds",
        "simplefarming:broccoli_seeds",
        "simplefarming:cantaloupe_seeds", // double-check spelling? might be "cantaloupe"
        "simplefarming:cassava_seeds",
        "simplefarming:carrot_seeds",
        "simplefarming:corn_seeds",
        "simplefarming:cotton_seeds",
        "simplefarming:cucumber_seeds",
        "simplefarming:cumin_seeds",
        "simplefarming:eggplant_seeds",
        "simplefarming:ginger_seeds",
        "simplefarming:grape_seeds",
        "simplefarming:honeydew_seeds",
        "simplefarming:lettuce_seeds",
        "simplefarming:oat_seeds",
        "simplefarming:onion_seeds",
        "simplefarming:pea_seeds",
        "simplefarming:rye_seeds",
        "simplefarming:sorghum_seeds",
        "simplefarming:spinach_seeds",
        "simplefarming:squash_seeds",
        "simplefarming:sweet_potato_seeds",
        "simplefarming:tomato_seeds",
        "simplefarming:turnip_seeds",
        "simplefarming:yam_seeds",
        "simplefarming:zucchini_seeds",
        "simplefarming:pepper_seeds"
    };

    static string GetRandomSeed()
    {
        return SeedItems[Rng.Next(SeedItems.Length)];
    }

    static readonly string[] SpartanWeapons = new[]
    {
        "battleaxe",
        "halberd",
        "greatsword",
        "rapier",
        "saber",
        "katana",
        "warhammer",
        "dagger",
        "longbow",
        "glaive",
        "flanged_mace",
        "quarterstaff",
        "parrying_dagger",
        "lance",
        "hammer",
        "heavy_crossbow",
        "longsword",
        "pike",
        "scythe",
        "spear",
    };

    static readonly string[] SpartanMaterials = new[]
    {
        "bronze",
        "copper",
        "tin",
        "diamond",
        "electrum",
        "invar",
        "iron",
        "lead",
        "netherite",
        "nickel",
        "silver",
        "gold",
        "platinum",
        "steel"
    };

    static readonly Random Rng = new Random();

    static string GetRandomSpartanWeapon()
    {
        var weapon = SpartanWeapons[Rng.Next(SpartanWeapons.Length)];
        var material = SpartanMaterials[Rng.Next(SpartanMaterials.Length)];
        return $"spartanweaponry:{weapon}_{material}{{Unbreakable:1b}}";
    }

    static readonly string[] DungeonGearSets = new[]
    {
        "arctic_fox",
        "battle_robes",
        "archers",
        "beehive",
        "beenest",
        "cave_crawler",
        "champions",
        "climbing",
        "curious",
        "dark",
        "ember_robes",
        "emerald",
        "evocation_robes",
        "frost_bite",
        "frost",
        "full_metal",
        "gilded_glory",
        "goat",
        "grim",
        "guards",
        "heros",
        "highland",
        "hungry_horror",
        "hunters",
        "mercenary",
        "ocelot",
        "opulent",
        "phantom",
        "plate",
        "reinforced_mail",
        "renegade",
        "rugged_climbing",
        "scale_mail",
        "shadow_walker",
        "snow",
        "soul",
        "souldancer",
        "spider",
        "stalwart",
        "thief",
        "titans_shroud",
        "wither",
        "wolf"
    };

    static void AddDungeonGearSet(List<ItemSpec> loot)
    {
        // Pick a random set
        var set = DungeonGearSets[Rng.Next(DungeonGearSets.Length)];

        // Armor pieces
        string[] pieces = { "boots", "chestplate", "helmet", "leggings" };

        foreach (var piece in pieces)
        {
            loot.Add(new ItemSpec($"dungeons_gear:{set}_{piece}{{Unbreakable:1b}}", 1));
        }
    }

    // ---------- House generator ----------
    /// <summary>
    /// Builds a simple cobblestone house:
    /// - Floor at (z - 1)
    /// - Hollow cobblestone walls
    /// - Flat cobblestone roof
    /// - Center door on the +Y side
    /// - Small windows (glass_pane)
    /// - Torches inside
    /// </summary>
    static CommandGenerationResults GenerateCobbleHouse(
        int x, int z, int y,
        int width = 7,    // must be >= 5 (odd looks nicer)
        int length = 9,   // must be >= 5 (odd looks nicer)
        int height = 5)   // interior height (z to z+height-1)
    {
        var create = new List<string>();
        var clear = new List<string>();

        // Clamp mins
        width = Math.Max(width, 5);
        length = Math.Max(length, 5);
        height = Math.Max(height, 4);

        // Half extents around the player's x,y (centered on player)
        int hx = width / 2;
        int hy = length / 2;

        // Floor z level is one below the player's z
        int floorZ = z - 1;

        // Clear a working volume (no buffer on all sides)
        GetFillCommand(x - hx, floorZ - 10, y - hy,
                       x + hx, z + height + 1, y + hy,
                       "minecraft:cobblestone", create, clear);

        GetFillCommand(x - (hx - 1), z + height, y - (hy - 1),
                       x + (hx - 1), z + height, y + (hy - 1),
                       "minecraft:air", create, clear);

        GetFillCommand(x - hx + 1, floorZ + 1, y - hy + 1,
                       x + hx - 1, z + height, y + hy - 1,
                       "air", create, clear);

        int floorExtension = 5;
        GetFillCommand(x - (hx + floorExtension), floorZ, y - (hy + floorExtension),
                       x + (hx + floorExtension), floorZ, y + (hy + floorExtension),
                       "minecraft:stone_bricks", create, clear);

        floorExtension = 4;
        GetFillCommand(x - (hx + floorExtension), floorZ, y - (hy + floorExtension),
                       x + (hx + floorExtension), floorZ, y + (hy + floorExtension),
                       "minecraft:dark_oak_planks", create, clear);

        floorExtension = 1;
        GetFillCommand(x - (hx + floorExtension), floorZ, y - (hy + floorExtension),
                       x + (hx + floorExtension), floorZ, y + (hy + floorExtension),
                       "minecraft:stone_bricks", create, clear);

        floorExtension = 0;
        GetFillCommand(x - (hx + floorExtension), floorZ, y - (hy + floorExtension),
                       x + (hx + floorExtension), floorZ, y + (hy + floorExtension),
                       "minecraft:dark_oak_planks", create, clear);

        floorExtension = -1;
        GetFillCommand(x - (hx + floorExtension), floorZ + height + 1, y - (hy + floorExtension),
                       x + (hx + floorExtension), floorZ + height + 1, y + (hy + floorExtension),
                       "minecraft:dark_oak_planks", create, clear);

        // Door centered on +Y wall (south-ish in your axis)
        int doorY = y + hy;     // outside/front edge
        int doorX = x;          // centered
        // Make 2-block tall opening
        GetFillCommand(doorX, z, doorY,
                       doorX, z + 1, doorY,
                       "air", create, clear);

        // Place the door (lower + upper)
        // Facing "south" (toward increasing Y in your layout)
        //SetBlock(doorX, z, doorY, "minecraft:oak_door[half=lower,facing=south]", create, clear);
        //SetBlock(doorX, z + 1, doorY, "minecraft:oak_door[half=upper,facing=south]", create, clear);

        // Wall centers
        int centerX = x;
        int centerY = y;

        // Door positions (on wall faces)
        int yPlus = y + hy; // +Y wall (front)
        int yMinus = y - hy; // -Y wall (back)
        int xPlus = x + hx; // +X wall (right)
        int xMinus = x - hx; // -X wall (left)

        // Place doors facing OUTWARDS
        PlaceDoorAt(centerX, z, yPlus, "south", create, clear); // +Y wall -> outward = south
        PlaceDoorAt(centerX, z, yMinus, "north", create, clear); // -Y wall -> outward = north
        PlaceDoorAt(xPlus, z, centerY, "east", create, clear); // +X wall -> outward = east
        PlaceDoorAt(xMinus, z, centerY, "west", create, clear); // -X wall -> outward = west

        // Simple windows: small 2x1 panes on each side wall at eye level (z+2)
        int windowZ = z + 2;
        // On -Y wall
        SetBlock(x - hx + 1, windowZ, y - hy, "minecraft:glass_pane", create, clear);
        SetBlock(x + hx - 1, windowZ, y - hy, "minecraft:glass_pane", create, clear);
        // On +Y wall (avoid door center)
        SetBlock(x - hx + 1, windowZ, y + hy, "minecraft:glass_pane", create, clear);
        SetBlock(x + hx - 1, windowZ, y + hy, "minecraft:glass_pane", create, clear);
        // On -X and +X walls (two small windows each)
        SetBlock(x - hx, windowZ, y - hy + 1, "minecraft:glass_pane", create, clear);
        SetBlock(x - hx, windowZ, y + hy - 1, "minecraft:glass_pane", create, clear);
        SetBlock(x + hx, windowZ, y - hy + 1, "minecraft:glass_pane", create, clear);
        SetBlock(x + hx, windowZ, y + hy - 1, "minecraft:glass_pane", create, clear);

        // Roof: flat cobblestone at z+height
        GetFillCommand(x - hx, z + height, y - hy,
                       x + hx, z + height, y + hy,
                       "minecraft:stone_bricks",
                       create, clear);

        // Interior light: torches on the walls at z+1
        int torchZ = z + 2;
        SetBlock(x, torchZ, y - hy + 1, "minecraft:torch", create, clear);
        SetBlock(x, torchZ, y + hy - 1, "minecraft:torch", create, clear);
        SetBlock(x - hx + 1, torchZ, y, "minecraft:torch", create, clear);
        SetBlock(x + hx - 1, torchZ, y, "minecraft:torch", create, clear);

        SetBlock(x, torchZ, y - hy - 1, "minecraft:stone_bricks", create, clear);
        SetBlock(x, torchZ, y + hy + 1, "minecraft:stone_bricks", create, clear);
        SetBlock(x - hx - 1, torchZ, y, "minecraft:stone_bricks", create, clear);
        SetBlock(x + hx + 1, torchZ, y, "minecraft:stone_bricks", create, clear);

        torchZ++;
        SetBlock(x, torchZ, y - hy - 1, "minecraft:lantern", create, clear);
        SetBlock(x, torchZ, y + hy + 1, "minecraft:lantern", create, clear);
        SetBlock(x - hx - 1, torchZ, y, "minecraft:lantern", create, clear);
        SetBlock(x + hx + 1, torchZ, y, "minecraft:lantern", create, clear);

        torchZ++;
        torchZ++;
        torchZ++;
        SetBlock(x, torchZ, y - hy, "minecraft:lantern", create, clear);
        SetBlock(x, torchZ, y + hy, "minecraft:lantern", create, clear);
        SetBlock(x - hx, torchZ, y, "minecraft:lantern", create, clear);
        SetBlock(x + hx, torchZ, y, "minecraft:lantern", create, clear);
        SetBlock(x - hy, torchZ, y - hy, "minecraft:lantern", create, clear);
        SetBlock(x - hy, torchZ, y + hy, "minecraft:lantern", create, clear);
        SetBlock(x + hx, torchZ, y - hy, "minecraft:lantern", create, clear);
        SetBlock(x + hx, torchZ, y + hy, "minecraft:lantern", create, clear);
        SetBlock(x, torchZ, y, "minecraft:lantern", create, clear);

        LineWallsWithChests(x, z, y, width, length, create, clear, doorX, hy);

        return new CommandGenerationResults
        {
            Create = create,
            Clear = clear
        };
    }

    // ---------- Printing ----------
    class CommandGenerationResults
    {
        public List<string> Create { get; set; }
        public List<string> Clear { get; set; }
    }

    static void PrintCommands(CommandGenerationResults r, bool includeClears = false)
    {
        var console = new StringBuilder();
        var clipboard = new StringBuilder();

        if (r.Create != null)
        {
            foreach (var c in r.Create)
            {
                console.AppendLine("\"" + c + "\",");
                clipboard.AppendLine("\"" + c + "\",");
            }
        }
        if (includeClears && r.Clear != null)
        {
            console.AppendLine();
            foreach (var c in r.Clear)
                console.AppendLine(c);
        }

        Console.WriteLine(console.ToString());
        ClipboardService.SetText(clipboard.ToString());
    }

    // ---------- CLI ----------
    static void ShowHelp()
    {
        Console.WriteLine("House Builder");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  house x z y [width length height]");
        Console.WriteLine("     Builds a basic cobblestone house centered on (x,y) with floor at (z-1).");
        Console.WriteLine("     Defaults: width=15 length=15 height=4");
        Console.WriteLine("     Example:");
        Console.WriteLine("       house 100 83 246");
        Console.WriteLine("       house 100 83 246 15 15 4");
        Console.WriteLine();
        Console.WriteLine("  help  - show this help");
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Minecraft Cobblestone House Generator");
        Console.WriteLine("Type 'help' for usage.");
        while (true)
        {
            Console.Write("\n> ");
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();

            if (cmd == "help")
            {
                ShowHelp();
                continue;
            }

            if (cmd == "house")
            {
                if (parts.Length < 4)
                {
                    Console.WriteLine("Usage: house x z y [width length height]");
                    continue;
                }

                if (!int.TryParse(parts[1], out int x) ||
                    !int.TryParse(parts[2], out int z) ||
                    !int.TryParse(parts[3], out int y))
                {
                    Console.WriteLine("x z y must be integers.");
                    continue;
                }

                int width = 15, length = 15, height = 4;
                if (parts.Length >= 7)
                {
                    int.TryParse(parts[4], out width);
                    int.TryParse(parts[5], out length);
                    int.TryParse(parts[6], out height);
                }

                var results = GenerateCobbleHouse(x, z, y, width, length, height);
                PrintCommands(results, includeClears: false);

                Console.WriteLine("\nCommands copied to clipboard (create only).");
                continue;
            }

            Console.WriteLine("Unknown command. Type 'help' for usage.");
        }
    }
}
