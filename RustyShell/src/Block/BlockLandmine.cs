using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RustyShell.Utilities.Blasts;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RustyShell {
    public class BlockLandmine : Block {

        //=======================
        // D E F I N I T I O N S
        //=======================

            private static ItemStack[] ShovelStacks;
            private static ItemStack[] WrenchStacks;
            private static ItemStack[] PressureFuzeStack;
            private static ItemStack[] SignStack;

            public int BlastRadius  { get; protected set; }
            public int InjureRadius { get; protected set; }

            public float DisablingDuration { get; protected set; }
            public float BuryingDuration   { get; protected set; }


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);

                this.BlastRadius       = this.Attributes["blastRadius"].AsInt(4);
                this.InjureRadius      = this.Attributes["injureRadius"].AsInt(6);
                this.DisablingDuration = this.Attributes["disablingDuration"].AsFloat(2f);
                this.BuryingDuration   = this.Attributes["buryingDuration"].AsFloat(4f);

                this.PlacedPriorityInteract = this.Variant["state"] != "excavated-disabled";

                if (api is ICoreClientAPI client) {
                    BlockLandmine.ShovelStacks = ObjectCacheUtil.GetOrCreate(client, "shovelStacks", delegate {

                        List<ItemStack> shovelStacks = new ();
                        foreach (Item item in client.World.Items)
                            if (item?.Tool == EnumTool.Shovel && item.Code != null)
                                shovelStacks.AddRange(item.GetHandBookStacks(client));

                        return shovelStacks.ToArray();

                    }); // ..
                    BlockLandmine.WrenchStacks = ObjectCacheUtil.GetOrCreate(client, "wrenchStacks", delegate {

                        List<ItemStack> wrenchStacks = new ();
                        foreach (Item item in api.World.Items)
                            if (item?.Tool == EnumTool.Wrench && item.Code != null)
                                wrenchStacks.AddRange(item.GetHandBookStacks(client));

                        return wrenchStacks.ToArray();
                    }); // ..
                    BlockLandmine.PressureFuzeStack = ObjectCacheUtil.GetOrCreate(client, "pressureFuzeStack", delegate {
                        return new ItemStack[1] { new (client.World.GetItem(new AssetLocation("rustyshell:fuze-pressure"))) };
                    }); // ..
                    BlockLandmine.SignStack = ObjectCacheUtil.GetOrCreate(client, "signStack", delegate {
                        return new ItemStack[1] { new (client.World.GetBlock(new AssetLocation("game:sign-ground-north"))) };
                    }); // ..
                } // if ..
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //==============
            // INTERACTIONS
            //==============

                public override void GetHeldItemInfo(
                    ItemSlot inSlot,
                    StringBuilder dsc,
                    IWorldAccessor world,
                    bool withDebugInfo
                ) {

                    if (this.BlastRadius  > 0)  dsc.AppendLine(Lang.Get("explosive-blastradius",  this.BlastRadius));
                    if (this.InjureRadius > 0)  dsc.AppendLine(Lang.Get("explosive-injureradius", this.InjureRadius));
                    
                    base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

                } // void ..
                

                public override WorldInteraction[] GetPlacedBlockInteractionHelp(
                    IWorldAccessor world,
                    BlockSelection selection,
                    IPlayer forPlayer
                ) {
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction[] {
                        new () {
                            ActionLangCode = "blockhelp-landmine-fuze",
                            MouseButton    = EnumMouseButton.Right,
                            HotKeyCode     = "shift",
                            Itemstacks     = BlockLandmine.PressureFuzeStack,
                            GetMatchingStacks = (wi, bs, es) => {
                                if (this.Variant["state"] == "excavated-disabled") return wi.Itemstacks;
                                return null;
                            } // ..
                        }, // ..
                        new () {
                            ActionLangCode = "blockhelp-landmine-bury",
                            MouseButton    = EnumMouseButton.Right,
                            HotKeyCode     = "shift",
                            Itemstacks     = BlockLandmine.ShovelStacks,
                            GetMatchingStacks = (wi, bs, es) => {
                                if (this.Variant["state"] == "excavated-fuzed") return wi.Itemstacks;
                                return null;
                            } // ..
                        }, // ..
                        new () {
                            ActionLangCode = "blockhelp-landmine-disable",
                            MouseButton    = EnumMouseButton.Right,
                            HotKeyCode     = "shift",
                            Itemstacks     = BlockLandmine.WrenchStacks,
                            GetMatchingStacks = (wi, bs, es) => {
                                if (this.Variant["state"] == "excavated-fuzed") return wi.Itemstacks;
                                return null;
                            } // ..
                        }, // ..
                        new () {
                            ActionLangCode = "blockhelp-landmine-excavate",
                            MouseButton    = EnumMouseButton.Left,
                            HotKeyCode     = "shift",
                            Itemstacks     = BlockLandmine.ShovelStacks,
                            GetMatchingStacks = (wi, bs, es) => {
                                if (this.Variant["state"] is "buried-sign" or "buried-hidden") return wi.Itemstacks;
                                return null;
                            } // ..
                        }, // ..
                        new () {
                            ActionLangCode = "blockhelp-landmine-putsign",
                            MouseButton    = EnumMouseButton.Right,
                            HotKeyCode     = "shift",
                            Itemstacks     = BlockLandmine.SignStack,
                            GetMatchingStacks = (wi, bs, es) => {
                                if (this.Variant["state"] == "buried-hidden") return wi.Itemstacks;
                                return null;
                            } // ..
                        }, // ..
                    }); // ..
                } // ..


                public override bool TryPlaceBlock(
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    ItemStack itemstack,
                    BlockSelection blockSel,
                    ref string failureCode
                ) {
                    if (world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).Fertility > 0) return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
                    failureCode = "unsuitableground";
                    return false;
                } // bool ..


                public override bool OnBlockInteractStart(
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel
                ) {
                    this.TryConstruct(0f, byPlayer, blockSel.Position, false);
                    base.OnBlockInteractStart(world, byPlayer, blockSel);
                    return true;
                } // bool ..


                public override bool OnBlockInteractStep(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel
                ) {
                    if (this.TryConstruct(secondsUsed, byPlayer, blockSel.Position, false)) return false;
                    return true;
                } // bool ..


                public override void OnBlockInteractStop(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel
                ) {
                    byPlayer.Entity.StopAnimation("shoveldig");
                    base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
                } // void ..


                public override bool OnBlockInteractCancel(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    EnumItemUseCancelReason cancelReason
                ) {
                    byPlayer.Entity.StopAnimation("shoveldig");
                    return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
                } // bool ..


                public override void OnBlockBroken(
                    IWorldAccessor world,
                    BlockPos pos,
                    IPlayer byPlayer,
                    float dropQuantityMultiplier = 1
                ) {

                    if (this.TryConstruct(0f, byPlayer, pos, true)) return;
                    
                    if (this.Variant["state"] != "excavated-disabled") this.HandleBlast(byPlayer?.Entity, pos);
                    else base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                } // void ..


                public override void OnEntityCollide(
                    IWorldAccessor world,
                    Entity entity,
                    BlockPos pos,
                    BlockFacing facing,
                    Vec3d collideSpeed,
                    bool isImpact
                ) {
                    if (this.Variant["state"] != "excavated-disabled") this.HandleBlast(entity, pos);
                    base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
                } // void ..


                public override void OnBlockExploded(
                    IWorldAccessor world,
                    BlockPos pos,
                    BlockPos explosionCenter,
                    EnumBlastType blastType
                ) => this.HandleBlast(null, pos);


        //======
        // MAIN
        //======


            private void HandleBlast(
                Entity byEntity,
                BlockPos pos
            ) => this.api.World.BlockAccessor.GetBlockEntity<BlockEntityLandmine>(pos)?.OnBlockExploded(byEntity);


            private bool TryConstruct(
                float secondsUsed,
                IPlayer byPlayer,
                BlockPos pos,
                bool onBlockBroken
            ) {

                if (byPlayer == null) return false;
                if (!byPlayer.Entity.Controls.Sneak) return false;

                string nextState;
                switch (this.Variant["state"]) {
                    case "excavated-disabled": {
                        if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Code.Path == "fuze-pressure") {

                            nextState = "excavated-fuzed";
                            byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                            break;

                        } return false;
                    } // case ..
                    case "excavated-fuzed": {

                        if (byPlayer.InventoryManager.ActiveTool == EnumTool.Shovel) {
                            if (secondsUsed < this.BuryingDuration) {
                                if (secondsUsed == 0f)
                                    byPlayer.Entity.StartAnimation("shoveldig");

                                long totalMsBreaking = api.ObjectCache.TryGetValue("totalMsBlockBreaking", out object val)
                                    ? (long)val
                                    : 0;

                                long nowMs = api.World.ElapsedMilliseconds;

                                if (nowMs - totalMsBreaking > 1000) {
                                    
                                    api.ObjectCache["totalMsBlockBreaking"] = nowMs;
                                    RustyShellModSystem.LookUps.LowFertilitySoil.SpawnBlockBrokenParticles(pos);
                                    this.api.World.PlaySoundAt(
                                        location : this.Sounds.Place,
                                        atPlayer : byPlayer
                                    ); // ..

                                } // if ..

                                return false;

                            } else {

                                nextState = $"buried-{(RustyShellModSystem.ModConfig.ForceLandmineSign ? "sign" : "hidden")}";
                                RustyShellModSystem.LookUps.LowFertilitySoil.SpawnBlockBrokenParticles(pos);
                                this.api.World.PlaySoundAt(
                                    location : this.Sounds.Place,
                                    atPlayer : byPlayer
                                ); // ..
                            } // if ..
                        } else if (byPlayer.InventoryManager.ActiveTool == EnumTool.Wrench && secondsUsed >= this.DisablingDuration) {

                            nextState = "excavated-disabled";

                            ItemStack fuzeItemStack = new (this.api.World.GetItem(new AssetLocation("rustyshell:fuze-pressure")), 1);
                            if (!byPlayer.InventoryManager.TryGiveItemstack(fuzeItemStack))
                                this.api.World.SpawnItemEntity(
                                    itemstack : fuzeItemStack,
                                    position  : byPlayer.Entity.SidedPos.XYZ
                                ); // ..

                            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                                byPlayer.Entity
                                    .ActiveHandItemSlot
                                    .Itemstack?
                                    .Item
                                    .DamageItem(
                                        world    : this.api.World,
                                        byEntity : byPlayer.Entity,
                                        itemslot : byPlayer.Entity.ActiveHandItemSlot,
                                        amount   : 10
                                    ); // ..
                        } else return false;

                        break;

                    } // case ..
                    case "buried-hidden": {

                        if (onBlockBroken && byPlayer.InventoryManager.ActiveTool == EnumTool.Shovel)
                            nextState = "excavated-fuzed";
                            
                        else if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Code.Path == "sign-ground-north") {

                            nextState = "buried-sign";
                            this.api.World.PlaySoundAt(
                                location : byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Block?.Sounds.Place,
                                atPlayer : byPlayer
                            ); // ..

                            byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);

                        } else return false;

                        break;

                    } // case ..
                    case "buried-sign": {

                        if (onBlockBroken && byPlayer.InventoryManager.ActiveTool == EnumTool.Shovel) {

                            nextState = "excavated-fuzed";

                            if (!RustyShellModSystem.ModConfig.ForceLandmineSign) {

                                ItemStack signItemStack = new (this.api.World.GetBlock(new AssetLocation("game:sign-ground-north")), 1);
                                if (!byPlayer.InventoryManager.TryGiveItemstack(signItemStack))
                                    this.api.World.SpawnItemEntity(
                                        itemstack : signItemStack,
                                        position  : byPlayer.Entity.SidedPos.XYZ
                                    ); // ..
                            } // if ..

                            break;
                        } return false;
                    } // case ..
                    default: { return false; }
                } // switch ..

                Block block = this.api.World.GetBlock(this.CodeWithVariant("state", nextState));

                this.api.World.BlockAccessor.ExchangeBlock(block.BlockId, pos);
                this.api.World.BlockAccessor.MarkBlockEntityDirty(pos);
                this.api.World.BlockAccessor.MarkBlockDirty(pos);

                return true;

            } // bool ..


        //===========
        // RENDERING
        //===========

    } // class ..
} // namespace ..