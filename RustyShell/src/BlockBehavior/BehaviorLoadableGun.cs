using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;


namespace RustyShell {
    public class BlockBehaviorLoadableGun : BlockBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================
            
            /** <summary> An array of each available ammunition for this gun </summary> **/       internal Item[] Ammunitions;
            /** <summary> An array of each available ammunition code for this gun </summary> **/  private string[] ammunitionCodes;
            /** <summary> An array of each available ammunition code for this gun </summary> **/  private int ammunitionLimit;
            /** <summary> Gun ignition delay in seconds </summary> **/                            private float fuseDuration;

            private ItemStack[] ammunitionStacks;

            private static ItemStack[] BaggedChargeStacks;
            private static ItemStack[] PrimerStacks;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockBehaviorLoadableGun(Block block) : base(block) {}


            public override void Initialize(JsonObject properties) {

                base.Initialize(properties);

                JsonObject ammunitions = properties["ammunitionCodes"];
                this.ammunitionLimit   = properties["ammunitionLimit"].AsInt(1);
                this.ammunitionCodes   = ((this.block.Variant["barrel"] is string barrel && ammunitions[barrel].Exists) ? ammunitions[barrel] : ammunitions).AsArray<string>();
                this.fuseDuration      = properties["fuseDuration"].AsFloat();

            } // void ..


            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);

                List<Item> ammunitions = new();
                
                if (this.Ammunitions == null && this.ammunitionCodes != null)
                    foreach (string code in this.ammunitionCodes)
                        ammunitions.AddRange(api.World.SearchItems(new AssetLocation(code)));

                this.Ammunitions   ??= ammunitions.ToArray();
                this.ammunitionCodes = null;

                BlockBehaviorLoadableGun.BaggedChargeStacks = ObjectCacheUtil.GetOrCreate(api, "baggedChargeStacks", delegate {
                    return api.World
                        .SearchItems(new AssetLocation("rustyshell:baggedcharge-*"))
                        .Select(charge => new ItemStack(charge, 1))
                        .ToArray();
                }); // ..


                if (api is ICoreClientAPI client) {
                    BlockBehaviorLoadableGun.PrimerStacks = ObjectCacheUtil.GetOrCreate(client, "primerStacks", delegate {

                        List<ItemStack> primerStacks = new ();
                        foreach (Item item in api.World.SearchItems(new AssetLocation("rustyshell:primer-*")))
                            primerStacks.AddRange(item.GetHandBookStacks(client));

                        return primerStacks.ToArray();
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
                private bool HasAmmunition(IPlayer byPlayer) => this.Ammunitions.Contains(byPlayer.Entity.ActiveHandItemSlot.Itemstack?.Item);

                /// <summary>
                /// Indicates whether or not a given player has a matching charge
                /// </summary>
                /// <param name="byPlayer"></param>
                private static bool HasCharge(IPlayer byPlayer) => BlockBehaviorLoadableGun.BaggedChargeStacks.Any(x => x?.Collectible.Code.Path == byPlayer.Entity.ActiveHandItemSlot.Itemstack?.Collectible.Code.Path);

                /// <summary>
                /// Indicates whether or not a given gun requires a charge to fire
                /// </summary>
                /// <param name="blockEntity"></param>
                private static bool GunRequiresCharge(BlockEntityHeavyGun blockEntity) => !blockEntity?.FusedAmmunition == true && blockEntity.BlockHeavyGun.MuzzleLoading;


                public override WorldInteraction[] GetPlacedBlockInteractionHelp(
                    IWorldAccessor world, 
                    BlockSelection selection,
                    IPlayer forPlayer,
                    ref EnumHandling handled
                ) {
                    return new WorldInteraction[] {
                        new () {
                            ActionLangCode    = "blockhelp-loadable-fillcharge",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = BlockBehaviorLoadableGun.BaggedChargeStacks,
                            GetMatchingStacks = (wi, bs, es) => {

                                BlockEntityHeavyGun blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(bs.Position);

                                if (!BlockBehaviorLoadableGun.GunRequiresCharge(blockEntity))                                           return null;
                                if (blockEntity?.ChargeSlot?.StackSize == blockEntity?.ChargeSlot?.Itemstack?.Collectible.MaxStackSize) return null;
                                if (blockEntity?.CanFill ?? false) return wi.Itemstacks;
                                return null;
                                
                            } // ..
                        }, // new ..
                        new () {
                            ActionLangCode    = "blockhelp-loadable-fillammunition",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = this.ammunitionStacks,
                            GetMatchingStacks = (wi, bs, es) => {

                                BlockEntityHeavyGun blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(bs.Position);

                                if (this.Ammunitions.Length == 0 || this.ammunitionStacks.Length == 0) return null;
                                if (blockEntity?.AmmunitionSlot?.StackSize >= this.ammunitionLimit) return null;
                                if (blockEntity?.AmmunitionSlot?.StackSize >= blockEntity?.AmmunitionSlot?.Itemstack?.Item.MaxStackSize) return null;
                                if (blockEntity?.CanFill ?? false) return wi.Itemstacks;
                                return null;
                                
                            } // ..
                        }, // new ..
                        new () {
                            ActionLangCode    = "blockhelp-loadable-fire",
                            MouseButton       = EnumMouseButton.Right,
                            HotKeyCode        = "shift",
                            Itemstacks        = BlockBehaviorLoadableGun.PrimerStacks,
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
                        if (BlockBehaviorLoadableGun.GunRequiresCharge(blockEntity) && HasCharge(byPlayer)) {

                            int moveableQuantity = GameMath.Min(16 - blockEntity.ChargeSlot.StackSize, 1);

                            slot.TryPutInto(blockEntity.Api.World, blockEntity.ChargeSlot, moveableQuantity);
                            blockEntity.MarkDirty();

                           handling = EnumHandling.PreventSubsequent;

                        } else if (this.HasAmmunition(byPlayer) && blockEntity.AmmunitionSlot.StackSize < this.ammunitionLimit) {

                            int moveableQuantity = GameMath.Min(slot.Itemstack.Item.MaxStackSize - blockEntity.AmmunitionSlot.StackSize, 1);

                            slot.TryPutInto(blockEntity.Api.World, blockEntity.AmmunitionSlot, moveableQuantity);
                            blockEntity.MarkDirty();

                            handling = EnumHandling.PreventSubsequent;

                        } // if ..
                    } // if ..

                    
                    if (blockEntity.CanFire
                        && byPlayer.Entity.Controls.Sneak
                        && (collectible?.Code.Path.StartsWith("primer") ?? false)
                    ) {

                        handling = EnumHandling.PreventSubsequent;
                        if (this.fuseDuration > 0f) {
                            ILoadedSound fuseSound = (world as IClientWorldAccessor)?.LoadSound(new SoundParams() {
                                Location        = new AssetLocation("sounds/effect/fuse"),
                                ShouldLoop      = true,
                                Position        = blockSel.FullPosition.ToVec3f(),
                                DisposeOnFinish = false,
                                Volume          = 1f,
                                Range           = 16,
                            }); // ..

                            long sparkRef = world.RegisterGameTickListener(
                                millisecondInterval : 10,
                                onGameTick          : (_) => {

                                    SimpleParticleProperties particles = BlockEntityBomb.smallSparks;
                                    particles.MinPos.Set(blockSel.Position.ToVec3d() + new Vec3d(0.5, 1, 0.5));
                                    world.SpawnParticles(
                                        particlePropertiesProvider : particles,
                                        dualCallByPlayer           : byPlayer
                                    ); // ..
                                } // ..
                            ); // ..

                            fuseSound?.Start();
                            world.RegisterCallback((_) => {
                                world.UnregisterGameTickListener(sparkRef);
                                fuseSound?.Stop();
                                fuseSound?.Dispose();
                                blockEntity.Fire(byPlayer.Entity);
                            }, (int)(this.fuseDuration * 1000f));

                        } else blockEntity.Fire(byPlayer.Entity);

                        if (byPlayer?.WorldData?.CurrentGameMode != EnumGameMode.Creative)
                            slot.Itemstack?.Item.DamageItem(world, byPlayer.Entity, slot, 1);

                    } // if ..


                    return true;

                } // bool ..
    } // class ..
} // namespace ..
