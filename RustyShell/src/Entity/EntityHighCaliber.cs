using System;
using System.IO;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using Vintagestory.API.Util;


namespace RustyShell {
    public class EntityHighCaliber : EntityAmmunition {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Look up table for block wasting </summary> **/ protected static Dictionary<string, Block> WastedSoilLookUp;
            /** <summary> Reference to the fire block </summary> **/     protected static Block FireBlock;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void Initialize(
                EntityProperties properties,
                ICoreAPI         api,
                long             InChunkIndex3d
            ) {

                base.Initialize(properties, api, InChunkIndex3d);

                EntityHighCaliber.FireBlock        ??= this.World.GetBlock(new AssetLocation("fire"));
                EntityHighCaliber.WastedSoilLookUp   = ObjectCacheUtil.GetOrCreate(api, "wastedSoilLookup", delegate {
                    return new Dictionary<string, Block>() {
                        {"verylownone",       api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"verylowverysparse", api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"verylowsparse",     api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"verylownormal",     api.World.GetBlock(new AssetLocation("game:soil-verylow-verysparse"))},
                        {"lownone",       api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"lowverysparse", api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"lowsparse",     api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"lownormal",     api.World.GetBlock(new AssetLocation("game:soil-verylow-verysparse"))},
                        {"mediumnone",       api.World.GetBlock(new AssetLocation("game:soil-low-none"))},
                        {"mediumverysparse", api.World.GetBlock(new AssetLocation("game:soil-low-none"))},
                        {"mediumsparse",     api.World.GetBlock(new AssetLocation("game:soil-low-none"))},
                        {"mediumnormal",     api.World.GetBlock(new AssetLocation("game:soil-low-verysparse"))},
                        {"compostnone",       api.World.GetBlock(new AssetLocation("game:soil-compost-none"))},
                        {"compostverysparse", api.World.GetBlock(new AssetLocation("game:soil-compost-none"))},
                        {"compostsparse",     api.World.GetBlock(new AssetLocation("game:soil-compost-none"))},
                        {"compostnormal",     api.World.GetBlock(new AssetLocation("game:soil-compost-verysparse"))},
                        {"highnone",       api.World.GetBlock(new AssetLocation("game:soil-medium-none"))},
                        {"highverysparse", api.World.GetBlock(new AssetLocation("game:soil-medium-none"))},
                        {"highsparse",     api.World.GetBlock(new AssetLocation("game:soil-medium-none"))},
                        {"highnormal",     api.World.GetBlock(new AssetLocation("game:soil-medium-verysparse"))},
                    }; // ..
                }); // ..
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //---------
            // M A I N
            //---------

                public override void OnGameTick(float deltaTime) {

                    base.OnGameTick(deltaTime);
                    if (
                        !this.stuck                                                                                  &&
                        this.Pos.Motion.LengthSq() >= 1                                                              &&
                        this.msSinceLaunch         >= (int)((this.Ammunition?.FlightExpectancy ?? 0f) * 1000) - 1500 &&
                        this.Properties.Sounds.TryGetValue("flying", out AssetLocation sound)
                    ) this.World.PlaySoundAt(sound, this, null, true, 64, 0.8f );

                } // void ..


                public override void HandleCommonBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        Vec3i center = this.ServerPos.XYZInt;
                        this.Api.ModLoader.GetModSystem<ScreenshakeToClientModSystem>().ShakeScreen(this.ServerPos.XYZ, (this.Ammunition.BlastRadius ?? 0) >> 2, (this.Ammunition.BlastRadius ?? 0) << 3);

                        int roundRadius        = this.Ammunition.BlastRadius ?? 0;
                        Cuboidi explosionArea  = new (this.ServerPos.XYZInt - new Vec3i(roundRadius, roundRadius, roundRadius), center + new Vec3i(roundRadius, roundRadius, roundRadius));
                        List<LandClaim> claims = (this.Api as ICoreServerAPI)?.WorldManager.SaveGame.LandClaims;


                        foreach (LandClaim landClaim in claims)
                            if (landClaim.Intersects(explosionArea)) {
                                this.Die();
                                return;

                            } // if ..


                        int strength = this.Ammunition.Type switch {
                            EnumAmmunitionType.Common    => RustyShellModSystem.GlobalConstants.CommonHighcaliberReinforcmentImpact,
                            EnumAmmunitionType.Explosive => RustyShellModSystem.GlobalConstants.ExplosiveHighcaliberReinforcmentImpact,
                            _                            => 0,
                        }; // ..

                        bool canBreakReinforced = !((this.FiredBy as IPlayer)?.HasPrivilege("denybreakreinforced") ?? false) && strength != 0;
                        int  blastRadiusSq      = (this.Ammunition.BlastRadius * this.Ammunition.BlastRadius) ?? 0;

                        ModSystemBlockReinforcement reinforcement = null;
                        if (canBreakReinforced)
                            reinforcement = this.World.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

                        this.World.BlockAccessor.WalkBlocks(
                            minPos      : explosionArea.Start.ToBlockPos(),
                            maxPos      : explosionArea.End.ToBlockPos(),
                            centerOrder : true,
                            onBlock     : (block, xWorld, yWorld, zWorld) => {

                                if (block.Id == 0 || block.Id == EntityHighCaliber.FireBlock.Id) return;

                                Vec3i searchPos = new (xWorld, yWorld, zWorld);
                                int x = searchPos.X - center.X;
                                int y = searchPos.Y - center.Y;
                                int z = searchPos.Z - center.Z;

                                int negDistanceSq = -(x * x + y * y + z * z);
                                if (int.IsPositive(negDistanceSq + blastRadiusSq)) {
                                    if (canBreakReinforced) {

                                        BlockPos pos = searchPos.AsBlockPos;
                                        reinforcement.ConsumeStrength(pos, strength);

                                        if (RustyShellModSystem.GlobalConstants.EnableLandWasting
                                            && block is BlockSoil
                                            && block.Variant["fertility"] is string fertility
                                            && block.Variant["grasscoverage"] is string grassCoverage
                                        ) {

                                            Block newBlock = EntityHighCaliber.WastedSoilLookUp[fertility + grassCoverage];
                                            this.World.BlockAccessor.ExchangeBlock(newBlock.Id, pos);

                                        } // if ..     

                                        this.World.BlockAccessor.MarkBlockEntityDirty(pos);

                                    } // if ..
                                } // if ..
                            } // ..
                        ); // ..


                        if (this.World.GetEntitiesInsideCuboid(
                            explosionArea.Start.ToBlockPos(),
                            explosionArea.End.ToBlockPos(),
                            (e) => !serverWorld.CanDamageEntity(this.FiredBy, e, out _)).Length != 0
                        ) {
                            this.Die();
                            return;
                        } // if ..


                        serverWorld.CreateExplosion(
                            this.ServerPos.AsBlockPos,
                            EnumBlastType.RockBlast,
                            this.Ammunition.BlastRadius  ?? 0,
                            this.Ammunition.InjureRadius ?? 0,
                            0f
                        ); // ..


                        this.Die();

                    } // if ..
                } // void ..


                public override void HandleExplosiveBlast() => this.HandleCommonBlast();
                public override void HandleAntiPersonnelBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        serverWorld.CreateExplosion(
                            this.ServerPos.AsBlockPos,
                            EnumBlastType.EntityBlast,
                            this.Ammunition.BlastRadius  ?? 0,
                            this.Ammunition.InjureRadius ?? 0,
                            0f
                        ); // ..

                        this.Die();
                    } // if ..
                } // void ..

                public override void HandleGasBlast() {
                    this.World.ReleaseGas(this.SidedPos.AsBlockPos, this.Ammunition.BlastRadius ?? 0, this.FiredBy);
                    if (this.World.Side.IsServer() && this.msSinceCollide > (this.Ammunition.IsSubmunition ? 10000 : 20000)) this.Die();
                } // void ..

                public override void HandleIncendiaryBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        Vec3i center           = this.ServerPos.XYZInt;
                        int incendiaryRadius   = this.Ammunition.BlastRadius ?? 0;
                        int falloutRadius      = this.Ammunition.InjureRadius ?? 0;
                        Cuboidi incendiaryArea = new (this.ServerPos.XYZInt - new Vec3i(incendiaryRadius, incendiaryRadius, incendiaryRadius), center + new Vec3i(incendiaryRadius, incendiaryRadius, incendiaryRadius));
                        Cuboidi falloutArea    = new (this.ServerPos.XYZInt - new Vec3i(falloutRadius, falloutRadius, falloutRadius),          center + new Vec3i(falloutRadius, falloutRadius, falloutRadius));

                        List<LandClaim> claims = (this.Api as ICoreServerAPI)?.WorldManager.SaveGame.LandClaims;


                        foreach (LandClaim landClaim in claims)
                            if (landClaim.Intersects(incendiaryArea)) {
                                this.Die();
                                return;

                            } // if ..

                        int blastRadiusSq = (this.Ammunition.BlastRadius * this.Ammunition.BlastRadius) ?? 0;
                        ThreadSafeRandom random = new();

                        this.World.BlockAccessor.WalkBlocks(
                            minPos      : incendiaryArea.Start.ToBlockPos(),
                            maxPos      : incendiaryArea.End.ToBlockPos(),
                            centerOrder : true,
                            onBlock     : (block, xWorld, yWorld, zWorld) => {

                                if (block.Id == 0 || block.Id == EntityHighCaliber.FireBlock.Id) return;

                                Vec3i searchPos = new (xWorld, yWorld, zWorld);
                                int x = searchPos.X - center.X;
                                int y = searchPos.Y - center.Y;
                                int z = searchPos.Z - center.Z;

                                int distanceSq = x * x + y * y + z * z;
                                if (int.IsPositive(-distanceSq + blastRadiusSq)) {

                                    BlockPos pos = searchPos.AsBlockPos;
                                    if (this.World.BlockAccessor.GetBlock(pos.UpCopy()).Id == 0 && random.Next(0, distanceSq >> 1) == 0)
                                        this.World.BlockAccessor.SetBlock(EntityHighCaliber.FireBlock.Id, pos.UpCopy());

                                    this.World.BlockAccessor.GetBlockEntity(pos.UpCopy())
                                        ?.GetBehavior<BEBehaviorBurning>()
                                        ?.OnFirePlaced(pos.UpCopy(), pos, (this.FiredBy as EntityPlayer).PlayerUID);
                                        
                                    this.World.BlockAccessor.MarkBlockDirty(pos);
                                } // if ..
                            } // ..
                        ); // ..


                        foreach (Entity entity in this.World.GetEntitiesInsideCuboid(
                            falloutArea.Start.ToBlockPos(),
                            falloutArea.End.ToBlockPos(),
                            (e) => serverWorld.CanDamageEntity(this.FiredBy, e, out _)
                        )) entity.IsOnFire = true;

                        this.Die();

                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
