using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RustyShell {
    public class BlockBehaviorLimberable : BlockBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            private static ItemStack[] HammerStacks;

            /** <summary> How long it takes to limber the block </summary> **/ private const float DURATION = 2f;
            /** <summary> Limbered entity code </summary> **/                  private string entityCode;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockBehaviorLimberable(Block block) : base(block) {}


            public override void Initialize(JsonObject properties) {

                base.Initialize(properties);
                this.entityCode = properties["entityCode"].AsString();

            } // void ..


            public override void OnLoaded(ICoreAPI api) {
                base.OnLoaded(api);
                if (api is ICoreClientAPI client)
                    BlockBehaviorLimberable.HammerStacks = ObjectCacheUtil.GetOrCreate(client, "hammerStacks", delegate {

                        List<ItemStack> hammerStacks = new ();
                        foreach (Item item in client.World.Items)
                            if (item is ItemHammer && item.Code != null)
                                hammerStacks.AddRange(item.GetHandBookStacks(client));

                        return hammerStacks.ToArray();
                    }); // ..
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                /// <summary>
                /// Indicates if a given player can interact with the limber behavior
                /// </summary>
                /// <param name="byPlayer"></param>
                /// <returns></returns>
                private static bool CanInteract(IPlayer byPlayer) => byPlayer.Entity.Controls.Sneak
                    && byPlayer.Entity.ActiveHandItemSlot.Itemstack?.Item is ItemHammer;


                public override WorldInteraction[] GetPlacedBlockInteractionHelp(
                    IWorldAccessor world, 
                    BlockSelection selection,
                    IPlayer forPlayer,
                    ref EnumHandling handled
                ) => new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode = "blockhelp-limber-set",
                        MouseButton    = EnumMouseButton.Right,
                        Itemstacks     = BlockBehaviorLimberable.HammerStacks,
                        HotKeyCode     = "shift",
                    }, // new ..
                }; // WorldInteraction[] ..



                public override bool OnBlockInteractStart(
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;
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


                        if (secondsUsed >= BlockBehaviorLimberable.DURATION) {

                            IOrientable orientable = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IOrientable;
                            EntityProperties type  = world.GetEntityType(new AssetLocation(this.entityCode));

                            if (type == null) return true;

                            EntityLimber entity = world.ClassRegistry.CreateEntity(type) as EntityLimber;
                            Vec3d limberPos     = blockSel.Position.ToVec3d() + new Vec3d(0.5, 0, 0.5);
                            

                            if (EntityLimber.HasNearbyDraftEntities(world, type, limberPos)) {

                                entity.ServerPos.SetPos(limberPos);
                                entity.ServerPos.Yaw = orientable?.Orientation ?? 0f;
                                entity.Pos.SetFrom(entity.ServerPos);

                                world.SpawnEntity(entity);
                                world.BlockAccessor.SetBlock(0, blockSel.Position);

                                return false;
 
                            } else if (world.Api is ICoreClientAPI client)
                                client.TriggerIngameError(this, "missing-near-draftentity", Lang.Get("missing-near-draftentity"));
                                
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
