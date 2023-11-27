using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using Vintagestory.API.Util;


namespace RustyShell {
    public class BlockBehaviorLoadableGun : BlockBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================
            
            /** <summary> An array of each available ammunition for this gun </summary> **/       internal Item[] Ammunitions;
            /** <summary> An array of each available ammunition code for this gun </summary> **/  private string[] ammunitionCodes;

            private ItemStack[] ammunitionStacks;

            private static ItemStack[] BlastingPowderStack;
            private static ItemStack[] LanyardStacks;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockBehaviorLoadableGun(Block block) : base(block) {}


            public override void Initialize(JsonObject properties) {

                base.Initialize(properties);

                JsonObject ammunitions = properties["ammunitionCodes"];
                this.ammunitionCodes   = ((this.block.Variant["barrel"] is string barrel && ammunitions[barrel].Exists) ? ammunitions[barrel] : ammunitions).AsArray<string>();

            } // void ..


            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);

                List<Item> ammunitions = new();
                
                if (this.Ammunitions == null && this.ammunitionCodes != null)
                    foreach (string code in this.ammunitionCodes)
                        ammunitions.AddRange(api.World.SearchItems(new AssetLocation(code)));

                this.Ammunitions ??= ammunitions.ToArray();
                this.ammunitionCodes = null;

                if (api is ICoreClientAPI client) {
                    BlockBehaviorLoadableGun.LanyardStacks = ObjectCacheUtil.GetOrCreate(client, "lanyardStacks", delegate {

                        List<ItemStack> lanyardStacks = new ();
                        foreach (Item item in api.World.SearchItems(new AssetLocation("rustyshell:lanyard-*")))
                            lanyardStacks.AddRange(item.GetHandBookStacks(client));

                        return lanyardStacks.ToArray();
                    }); // ..
                    BlockBehaviorLoadableGun.BlastingPowderStack = ObjectCacheUtil.GetOrCreate(client, "blastingPowderStack", delegate {
                        return new ItemStack[1] { new(client.World.GetItem(new AssetLocation("game:blastingpowder"))) };
                    }); // ..

                    this.ammunitionStacks = new ItemStack[this.Ammunitions.Length];
                    for (int i = 0; i < this.Ammunitions.Length; i++)
                        this.ammunitionStacks[i] = new ItemStack(this.Ammunitions[i]);

                } // if ..
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                /// <summary>
                /// Indicates whether or not a given player has a matching ammunition
                /// </summary>
                /// <param name="byPlayer"></param>
                /// <returns></returns>
                private bool HasAmmunition(IPlayer byPlayer) => this.Ammunitions.Contains(byPlayer.Entity.ActiveHandItemSlot.Itemstack?.Item);

                /// <summary>
                /// Indicates whether or not a given player has a matching detonator
                /// </summary>
                /// <param name="byPlayer"></param>
                /// <returns></returns>
                private static bool HasDetonator(IPlayer byPlayer)  => byPlayer.Entity.ActiveHandItemSlot.Itemstack?.Collectible.Code.Path == "blastingpowder";

                /// <summary>
                /// Indicates whether or not a given gun requires a detonator to fire
                /// </summary>
                /// <param name="blockEntity"></param>
                /// <returns></returns>
                private static bool GunRequiresDetonator(BlockEntityHeavyGun blockEntity) => blockEntity?.BlockHeavyGun.BarrelType == EnumBarrelType.Smoothbore && !blockEntity.FusedAmmunition;


                public override WorldInteraction[] GetPlacedBlockInteractionHelp(
                    IWorldAccessor world, 
                    BlockSelection selection,
                    IPlayer forPlayer,
                    ref EnumHandling handled
                ) {
                    return new WorldInteraction[] {
                        new WorldInteraction() {
                            ActionLangCode    = "blockhelp-loadable-filldetonator",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = BlockBehaviorLoadableGun.BlastingPowderStack,
                            GetMatchingStacks = (wi, bs, es) => {

                                BlockEntityHeavyGun blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(bs.Position);

                                if (!BlockBehaviorLoadableGun.GunRequiresDetonator(blockEntity)) return null;
                                if (blockEntity?.DetonatorSlot?.StackSize >= 16) return null;
                                if (blockEntity?.CanFill ?? false) return wi.Itemstacks;
                                return null;
                                
                            } // ..
                        }, // new ..
                        new WorldInteraction() {
                            ActionLangCode    = "blockhelp-loadable-fillammunition",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = this.ammunitionStacks,
                            GetMatchingStacks = (wi, bs, es) => {

                                BlockEntityHeavyGun blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(bs.Position);

                                if (this.Ammunitions.Length == 0 || this.ammunitionStacks.Length == 0) return null;
                                if (blockEntity?.AmmunitionSlot?.StackSize >= blockEntity?.AmmunitionSlot?.Itemstack?.Item.MaxStackSize) return null;
                                if (blockEntity?.CanFill ?? false) return wi.Itemstacks;
                                return null;
                                
                            } // ..
                        }, // new ..
                        new WorldInteraction() {
                            ActionLangCode    = "blockhelp-loadable-fire",
                            MouseButton       = EnumMouseButton.Right,
                            HotKeyCode        = "shift",
                            Itemstacks        = BlockBehaviorLoadableGun.LanyardStacks,
                            GetMatchingStacks = (wi, bs, es) => {

                                BlockEntityHeavyGun blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(bs.Position);

                                if (this.block.HasBehavior<BlockBehaviorRepeatingFire>()) return null;
                                if (blockEntity?.AmmunitionSlot?.Empty ?? true)           return null;
                                if (world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(bs.Position)?.CanFire ?? false) return wi.Itemstacks;
                                return null;
                                
                            } // ..
                        }, // new ..
                    }; // WorldInteraction[] ..
                } // WorldInteraction[] ..
                

                public override bool OnBlockInteractStart(
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;

                    ItemSlot slot                 = byPlayer.InventoryManager.ActiveHotbarSlot;
                    CollectibleObject collectible = slot.Itemstack?.Collectible;

                    BlockEntityHeavyGun blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(blockSel.Position);

                    if (blockEntity?.Cooldown != 0f) {
                        if (world.Api is ICoreClientAPI client)
                            client.TriggerIngameError(this, "barrel-toohot", Lang.Get("barrel-toohot"));

                        return true;

                    } // if ..


                    if (blockEntity?.CanFill ?? false) {


                        blockEntity.Inventory ??= new InventoryGeneric(2, "heavygun-" + blockSel.Position, blockEntity.Api);
                        if (BlockBehaviorLoadableGun.GunRequiresDetonator(blockEntity) && HasDetonator(byPlayer)) {

                            int moveableQuantity = GameMath.Min(16 - blockEntity.DetonatorSlot.StackSize, 1);

                            slot.TryPutInto(blockEntity.Api.World, blockEntity.DetonatorSlot, moveableQuantity);
                            blockEntity.MarkDirty();

                           handling = EnumHandling.PreventSubsequent;

                        } else if (this.HasAmmunition(byPlayer)) {

                            int moveableQuantity = GameMath.Min(slot.Itemstack.Item.MaxStackSize - blockEntity.AmmunitionSlot.StackSize, 1);

                            slot.TryPutInto(blockEntity.Api.World, blockEntity.AmmunitionSlot, moveableQuantity);
                            blockEntity.MarkDirty();

                            handling = EnumHandling.PreventSubsequent;

                        } // if ..
                    } // if ..

                    
                    if (blockEntity.CanFire
                        && byPlayer.Entity.Controls.Sneak
                        && (collectible?.Code.Path.StartsWith("lanyard") ?? false)
                    ) {

                        handling = EnumHandling.PreventSubsequent;
                        blockEntity.Fire(byPlayer.Entity);
                        if (byPlayer?.WorldData?.CurrentGameMode != EnumGameMode.Creative)
                            slot.Itemstack?.Item.DamageItem(world, byPlayer.Entity, slot, 1);

                    } // if ..


                    return true;

                } // bool ..
    } // class ..
} // namespace ..
