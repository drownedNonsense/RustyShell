using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;


namespace RustyShell {
    public class BlockBehaviorMuzzleLoading : BlockBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            private static ItemStack[] RamrodStack;

            /** <summary> How long it takes to clean the barrel </summary> **/ public float CleanDuraction { get; private set; }
            /** <summary> How long it takes to load the barrel </summary> **/  public float LoadDuration   { get; private set; }


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockBehaviorMuzzleLoading(Block block) : base(block) {}


            public override void Initialize(JsonObject properties) {

                base.Initialize(properties);
                this.CleanDuraction = properties["cleanDuration"].AsFloat(0f);
                this.LoadDuration   = properties["loadDuration"].AsFloat(0f);

            } // void ..


            public override void OnLoaded(ICoreAPI api) {
                base.OnLoaded(api);
                if (api is ICoreClientAPI client)
                    BlockBehaviorMuzzleLoading.RamrodStack = ObjectCacheUtil.GetOrCreate(client, "ramrodStack", delegate {
                        return new ItemStack[1] { new (client.World.GetItem(new AssetLocation("rustyshell:ramrod"))) };
                    }); // ..
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                /// <summary>
                /// Indicates if a given player can interact with the muzzle loading behavior
                /// </summary>
                /// <param name="byPlayer"></param>
                /// <returns></returns>
                private static bool CanInteract(IPlayer byPlayer) => byPlayer.Entity.ActiveHandItemSlot.Itemstack?.Collectible.Code.Path == "ramrod";


                public override WorldInteraction[] GetPlacedBlockInteractionHelp(
                    IWorldAccessor world, 
                    BlockSelection selection,
                    IPlayer forPlayer,
                    ref EnumHandling handled
                ) {
                    return new WorldInteraction[] {
                        new () {
                            ActionLangCode    = "blockhelp-muzzleloading-clean",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = BlockBehaviorMuzzleLoading.RamrodStack,
                            GetMatchingStacks = (wi, bs, es) => {

                                if (world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(bs.Position)?.CanClean ?? false) return wi.Itemstacks;
                                return null;
                                
                            } // ..
                        }, // new ..
                        new () {
                            ActionLangCode    = "blockhelp-muzzleloading-load",
                            MouseButton       = EnumMouseButton.Right,
                            Itemstacks        = BlockBehaviorMuzzleLoading.RamrodStack,
                            GetMatchingStacks = (wi, bs, es) => {

                                if (world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(bs.Position)?.CanLoad ?? false) return wi.Itemstacks;
                                return null;

                            } // ..
                        } // new ..
                    }; // WorldInteraction[] ..
                } // WorldInteraction[] ..
                

                public override bool OnBlockInteractStart(
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;
                    if (CanInteract(byPlayer))
                        world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorMuzzleLoading>()?
                            .RamrodSound?
                            .Start();
                            
                    return true;

                } // bool ..


                public override bool OnBlockInteractStep(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    if (CanInteract(byPlayer)) {

                        handling = EnumHandling.PreventSubsequent;

                        if (byPlayer?.WorldData?.CurrentGameMode != EnumGameMode.Creative)
                            byPlayer.Entity
                                .ActiveHandItemSlot
                                .Itemstack?
                                .Item
                                .DamageItem(world, byPlayer.Entity, byPlayer.Entity.ActiveHandItemSlot, GameMath.RoundRandom(world.Rand, 0.1f));


                        BlockEntityHeavyGun blockEntity           = world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(blockSel.Position);
                        BlockEntityBehaviorMuzzleLoading behavior = blockEntity?.GetBehavior<BlockEntityBehaviorMuzzleLoading>();

                        if (behavior != null) {

                            behavior.SecondsLoaded += secondsUsed - behavior.SecondsLoaded;

                            bool handled = false;
                            if      (blockEntity.CanClean && behavior.SecondsLoaded >= this.CleanDuraction) { behavior.GunState = EnumGunState.Clean; handled = true; }
                            else if (blockEntity.CanLoad  && behavior.SecondsLoaded >= this.LoadDuration)   { behavior.GunState = EnumGunState.Ready; handled = true; }

                            if (handled) {
                                blockEntity.MarkDirty();
                                behavior.SecondsLoaded = 0f;
                            } // if ..
                        } // if ..
                    } else handling = EnumHandling.PreventDefault;


                    return true;

                } // bool ..


                public override void OnBlockInteractStop(
                    float secondsUsed,
                    IWorldAccessor world, 
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) => handling = EnumHandling.PreventDefault;


                public override bool OnBlockInteractCancel(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;
                    return true;

                } // bool ..
    } // class ..
} // namespace ..
