using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using System.Collections.Generic;

namespace RustyShell {
    public class EntityBehaviorDeployableLimber: EntityBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            private static ItemStack[] HammerStacks;
            
            /** <summary> A reference to the limbered entity </summary> **/                      protected EntityLimber entityLimber;
            /** <summary> A reference to the deployed block version of the entity </summary> **/ protected Block deployedBlock;

            public override string PropertyName() => "deployablelimber";


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public EntityBehaviorDeployableLimber(Entity entity) : base(entity) {}

            public override void Initialize(
                EntityProperties properties,
                JsonObject attributes
            ) {

                base.Initialize(properties, attributes);
                this.entityLimber  = this.entity as EntityLimber;
                this.deployedBlock = this.entity.World.GetBlock(new AssetLocation(this.entity
                    .Properties
                    .Attributes?["limber"]["deployedBlock"]
                    .AsString(this.entity.Code.Domain + ":" + this.entity.Code.Path)
                )); // ..

                if (this.entity.World.Api is ICoreClientAPI client)
                    EntityBehaviorDeployableLimber.HammerStacks = ObjectCacheUtil.GetOrCreate(client, "hammerStacks", delegate {

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

                public override WorldInteraction[] GetInteractionHelp(
                    IClientWorldAccessor world,
                    EntitySelection      es,
                    IClientPlayer        player,
                    ref EnumHandling     handled
                ) => new WorldInteraction[] {
                    new WorldInteraction() {
                        ActionLangCode    = "blockhelp-limber-deploy",
                        MouseButton       = EnumMouseButton.Left,
                        Itemstacks        = EntityBehaviorDeployableLimber.HammerStacks,
                        GetMatchingStacks = (wi, bs, es) => (this.entityLimber.DraftingLimber == null) switch {
                            true  => wi.Itemstacks,
                            false => null,
                        } // ..
                    }, // WorldInteraction ..
                }; // WorldInteraction[] ..



                /// <summary>
                /// Called to deploy the entity into a block if conditions are met
                /// </summary>
                /// <param name="byEntity"></param>
                /// <returns></returns>
                public bool TryDeploy(Entity byEntity) {
                    if (this.entity.Alive)
                        if (this.deployedBlock is Block deployedBlock) {

                            if (this.entity.World.BlockAccessor.GetBlock(this.entity.ServerPos.AsBlockPos).Replaceable < 6000)
                                return false;
                            
                            this.entityLimber.DraftEntityLeader?.WatchedAttributes.RemoveAttribute("isDraftingLimber");
                            this.entityLimber.DraftEntitySide?.WatchedAttributes.RemoveAttribute("isDraftingLimber");
                            this.entity.WatchedAttributes.RemoveAttribute("draftEntityLeaderId");
                            this.entity.WatchedAttributes.RemoveAttribute("draftEntitySideId");

                            if (this.entityLimber.DraftEntityLeader is EntityLimber leadLimber)
                                leadLimber.DraftingLimber = null;
                            
                            this.entity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/anvilmergehit"), byEntity);

                            this.entity.World.BlockAccessor.SetBlock(deployedBlock.BlockId, this.entity.ServerPos.AsBlockPos);
                            (this.entity.World.BlockAccessor.GetBlockEntity(this.entity.ServerPos.AsBlockPos) as IOrientable).ChangeOrientation(this.entity.ServerPos.Yaw);
                            this.entity.Die(EnumDespawnReason.Removed);

                            return true;
                        
                        } // if ..

                    return false;
                } // bool ..


                public override void OnInteract(
                    EntityAgent      byEntity,
                    ItemSlot         itemslot,
                    Vec3d            hitPosition,
                    EnumInteractMode mode,
                    ref EnumHandling handled
                ) {

                    handled = EnumHandling.PreventDefault;


                    if (mode == EnumInteractMode.Attack && itemslot.Itemstack?.Item is ItemHammer && this.entityLimber.DraftingLimber == null) {

                        this.TryDeploy(byEntity);
                        if (byEntity is IPlayer byPlayer)
                            if (byPlayer?.WorldData?.CurrentGameMode != EnumGameMode.Creative)
                                byEntity.ActiveHandItemSlot
                                    .Itemstack?
                                    .Item
                                    .DamageItem(this.entity.World, byEntity, byEntity.ActiveHandItemSlot, 1);

                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
