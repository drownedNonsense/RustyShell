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
    public class EntityHighCaliber : Entity {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Reference to the source entity </summary> **/                                internal Entity            FiredBy;
            /** <summary> Type of blast on detonation </summary> **/                                   internal EnumExplosionType BlastType;
            /** <summary> Abstract value of how unlikely a projectile is to exploded </summary> **/    internal int               Stability;
            /** <summary> Blast radius in block </summary> **/                                         internal int               BlastRadius;
            /** <summary> Injure radius in block </summary> **/                                        internal int               InjureRadius;
            /** <summary> Amount of damage caused by the projectile itself on impact </summary> **/    internal float             Damage;
            /** <summary> Alternative damage like gas </summary> **/                                   internal float             AltDamage;
            /** <summary> How long some projectiles will last in second after collision </summary> **/ internal int               Duration;
            /** <summary> How long before the projectile explodes before collision </summary> **/      internal int               FuseDuration;

            /** <summary> Projectile velocity before collision </summary> **/             private readonly Vec3d motionBeforeCollide = new();
            /** <summary> Indicates whether or not the projectile is stuck </summary> **/ private bool stuck;
            /** <summary> Indicates whether or not the projectile is stuck </summary> **/ private bool beforeCollided;

            /** <summary> Milliseconds at launch </summary> **/       private long msLaunch;
            /** <summary> Milliseconds at collision </summary> **/    private long msCollide;
            /** <summary> Milliseconds since launch </summary> **/    private int  msSinceLaunch  => (int)(this.World.ElapsedMilliseconds - this.msLaunch);
            /** <summary> Milliseconds since collision </summary> **/ private int  msSinceCollide => (int)(this.World.ElapsedMilliseconds - this.msCollide);

            private readonly CollisionTester    collTester = new();
            private          Cuboidf            collisionTestBox;
            private          EntityPartitioning entityPartitioning;

            public override bool ApplyGravity                => true;
            public override bool IsInteractable              => false;
            public override bool CanCollect(Entity byEntity) => false;

            /** <summary> Look up table for block wasting </summary> **/ private static Dictionary<string, Block> WastedSoilLookUp;
            /** <summary> Reference to the fire block </summary> **/     private static Block FireBlock;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void Initialize(
                EntityProperties properties,
                ICoreAPI         api,
                long             InChunkIndex3d
            ) {

                base.Initialize(properties, api, InChunkIndex3d);

                this.msLaunch         = this.World.ElapsedMilliseconds;
                this.collisionTestBox = this.SelectionBox.Clone().OmniGrowBy(0.05f);

                this.GetBehavior<EntityBehaviorPassivePhysics>().collisionYExtra = 0f;

                this.entityPartitioning = api.ModLoader.GetModSystem<EntityPartitioning>();

                JsonObject detonation = api.World.GetItem(this.Code)?.Attributes["detonation"];
                this.BlastType = detonation["type"].AsString() switch {
                    "Simple"        => EnumExplosionType.Simple,
                    "Piercing"      => EnumExplosionType.Piercing,
                    "HighExplosive" => EnumExplosionType.HighExplosive,
                    "Grape"         => EnumExplosionType.Grape,
                    "Canister"      => EnumExplosionType.Canister,
                    "Gas"           => EnumExplosionType.Gas,
                    _               => EnumExplosionType.NonExplosive,
                }; // switch ..

                this.BlastRadius  =  detonation["blastRadius"].AsInt();
                this.InjureRadius =  detonation["injureRadius"].AsInt();
                this.Damage       =  detonation["amount"].AsFloat();
                this.AltDamage    = (detonation["altDamage"].AsFloat() is float altDamage && altDamage != 0) ? altDamage : RustyShellModSystem.GlobalConstants.GasBaseDamage;
                this.Duration     =  detonation["duration"].AsInt();

                if (this.FuseDuration is int fuseDuration && fuseDuration == 0) this.FuseDuration = detonation["fuseDuration"].AsInt();

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
                    
                    if (this.ShouldDespawn) return;

                    bool newStuck = this.Collided
                        || this.collTester.IsColliding(this.World.BlockAccessor, this.collisionTestBox, this.SidedPos.XYZ)
                        || WatchedAttributes.GetBool("stuck");
                    
                    if (this.Api.Side.IsServer() && newStuck != this.stuck) {
                        this.stuck = newStuck;
                        this.WatchedAttributes.SetBool("stuck", this.stuck);
                    } // if ..

                    if (this.stuck) {

                        this.IsColliding();
                        this.Detonate();
                        return;

                    } else {

                        this.SetRotation();

                        if (this.msSinceLaunch >= this.FuseDuration - 1500 && this.Properties.Sounds.TryGetValue("flying", out AssetLocation sound))
                            this.World.PlaySoundAt(sound, this, null, true, 64, 0.8f );

                    } // if ..


                    if (this.FuseDuration < this.msSinceLaunch) {
                        this.Detonate();
                        return;
                    } // if ..


                    if (this.CheckEntityCollision()) return;

                    this.beforeCollided = false;
                    this.motionBeforeCollide.Set(this.SidedPos.Motion);

                } // void ..


                /// <summary>
                /// Called on collision with an entity
                /// </summary>
                /// <param name="entity"></param>
                private void ImpactOnEntity(Entity entity) {
                    if ((this.World as IServerWorldAccessor)?.CanDamageEntity(this.FiredBy, entity, out bool isFromPlayer) ?? false) {

                        this.msCollide = this.World.ElapsedMilliseconds;
                        this.SidedPos.Motion.Set(Vec3d.Zero);

                        if (FiredBy != null) this.Damage *= this.FiredBy.Stats.GetBlended("rangedWeaponsDamage");

                        bool didDamage = entity.ReceiveDamage(new DamageSource() {
                            Source       = isFromPlayer ? EnumDamageSource.Player : EnumDamageSource.Entity,
                            SourceEntity = this,
                            CauseEntity  = this.FiredBy,
                            Type         = EnumDamageType.PiercingAttack
                        }, this.Damage);

                        float knockbackResistance = entity.Properties.KnockbackResistance;
                        entity.SidedPos.Motion.Add(knockbackResistance * this.SidedPos.Motion.X, knockbackResistance * this.SidedPos.Motion.Y, knockbackResistance * this.SidedPos.Motion.Z);

                        this.Detonate();

                        if (isFromPlayer && didDamage)
                            this.World.PlaySoundFor(new AssetLocation("sounds/player/projectilehit"), (this.FiredBy as EntityPlayer).Player, false, 24);

                    } // if ..
                } // void ..


                /// <summary>
                /// This method is called on the ammunition's detonation.
                /// </summary>
                private void Detonate() {
                    switch (this.BlastType) {
                        case EnumExplosionType.NonExplosive: {

                            (this.World as IServerWorldAccessor)?.CreateExplosion(
                                this.ServerPos.AsBlockPos,
                                EnumBlastType.EntityBlast,
                                GameMath.RoundRandom(this.World.Rand, 0.02f),
                                this.World.Rand.Next(0, 1),
                                0f
                            ); // ..

                            BlockPos blockPos = (this.ServerPos.XYZ + this.motionBeforeCollide.Normalize()).AsBlockPos;

                            this.World
                                .BlockAccessor
                                .GetBlock(blockPos)
                                .OnBlockExploded(this.World, blockPos, blockPos, EnumBlastType.OreBlast);
                            
                            if (this.World.Side.IsServer()) this.Die();
                            break;

                        } case EnumExplosionType.Grape : {

                            this.World.DetonateGrape(this.SidedPos.AsBlockPos, 0.4f, this.SidedPos.Yaw, this.FiredBy);
                            if (this.World.Side.IsServer()) this.Die();
                            break;

                        } case EnumExplosionType.Canister : {

                            this.World.DetonateCanister(this.SidedPos.AsBlockPos, this.BlastRadius, this.InjureRadius, this.FiredBy);
                            if (this.World.Side.IsServer()) this.Die();
                            break;
                            
                        } case EnumExplosionType.Gas : {

                            this.World.ReleaseGas(this.SidedPos.AsBlockPos, this.InjureRadius, this.AltDamage, this.FiredBy);
                            if (this.World.Side.IsServer() && this.msSinceCollide > this.Duration) this.Die();

                            break;
                        } default : {
                            if (this.World is IServerWorldAccessor serverWorld) {

                                Vec3i center = this.ServerPos.XYZInt;
                                this.Api.ModLoader.GetModSystem<ScreenshakeToClientModSystem>().ShakeScreen(this.ServerPos.XYZ, this.BlastRadius >> 2, this.BlastRadius << 4);

                                int roundRadius        = (int)MathF.Ceiling(this.BlastRadius);
                                Cuboidi explosionArea  = new (this.ServerPos.XYZInt - new Vec3i(roundRadius, roundRadius, roundRadius), center + new Vec3i(roundRadius, roundRadius, roundRadius));
                                List<LandClaim> claims = (this.Api as ICoreServerAPI)?.WorldManager.SaveGame.LandClaims;


                                foreach (LandClaim landClaim in claims)
                                    if (landClaim.Intersects(explosionArea)) {
                                        this.Die();
                                        return;

                                    } // if ..


                                int strength = this.BlastType switch {
                                    EnumExplosionType.Piercing      => RustyShellModSystem.GlobalConstants.PiercingProjectileReinforcmentImpact,
                                    EnumExplosionType.HighExplosive => RustyShellModSystem.GlobalConstants.HighExplosiveProjectileReinforcementImpact,
                                    EnumExplosionType.Simple        => RustyShellModSystem.GlobalConstants.SimpleProjectileReinforcmentImpact,
                                    _                               => 0,
                                }; // ..

                                bool canBreakReinforced = !((this.FiredBy as IPlayer)?.HasPrivilege("denybreakreinforced") ?? false) && strength != 0;
                                int blastRadiusSq       = this.BlastRadius * this.BlastRadius;

                                ModSystemBlockReinforcement reinforcement = null;
                                if (canBreakReinforced)
                                    reinforcement = this.World.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

                                this.World.BlockAccessor.SearchBlocks(
                                    explosionArea.Start.ToBlockPos(),
                                    explosionArea.End.ToBlockPos(),
                                    (b, p) => {
                                    
                                        Vec3i searchPos = p.AsVec3i;
                                        int x = searchPos.X - center.X;
                                        int y = searchPos.Y - center.Y;
                                        int z = searchPos.Z - center.Z;

                                        int negDistanceSq = -(x * x + y * y + z * z);
                                        if (int.IsPositive(negDistanceSq + blastRadiusSq)) {
                                            if (canBreakReinforced) {
                                                
                                                reinforcement.ConsumeStrength(p, strength);
                                            
                                                if (RustyShellModSystem.GlobalConstants.EnableLandWasting
                                                    && b is BlockSoil
                                                    && b.Variant["fertility"] is string fertility
                                                    && b.Variant["grasscoverage"] is string grassCoverage
                                                ) {

                                                        Block newBlock = EntityHighCaliber.WastedSoilLookUp[fertility + grassCoverage];
                                                        this.World.BlockAccessor.ExchangeBlock(newBlock.Id, p);

                                                } // if ..                                                
                                            } // if ..

                                            if (int.IsPositive(negDistanceSq + blastRadiusSq >> 1)) {

                                                EnumHandling handled = EnumHandling.PassThrough;
                                                IIgnitable ignitable = b.GetInterface<IIgnitable>(this.World, p);
                                                ignitable?.OnTryIgniteBlockOver(this.FiredBy as EntityAgent, p, this.BlastRadius, ref handled);

                                                BlockPos overBlockPos = p + new BlockPos(0, 1, 0);
                                                Block overBlock       = this.World.BlockAccessor.GetBlock(overBlockPos);
                                                if (overBlock.BlockId == 0) {

                                                    this.World.BlockAccessor.SetBlock(EntityHighCaliber.FireBlock.BlockId, overBlockPos);
                                                    BlockEntity blockEntityFire = this.World.BlockAccessor.GetBlockEntity(overBlockPos);
                                                    blockEntityFire?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(BlockFacing.UP, (this.FiredBy as EntityPlayer)?.PlayerUID);

                                                } // if ..
                                            } // if ..

                                            this.World.BlockAccessor.MarkBlockDirty(p);
                                        } // if ..

                                        return true;
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
                                    this.BlastRadius,
                                    this.InjureRadius,
                                    0f
                                ); // ..


                                this.Die();

                            } // if ..

                            break;

                        } // case ..
                    } // switch ..
                } // void ..


            //---------------
            // P H Y S I C S
            //---------------

                public override void OnCollided() {
                    this.IsColliding();
                    this.motionBeforeCollide.Set(this.SidedPos.Motion);
                } // void ..



                public override void OnCollideWithLiquid() {
                    base.OnCollideWithLiquid();
                    this.Detonate();
                } // void ..


                /// <summary>
                /// Called on collision
                /// </summary>
                private void IsColliding() {

                    // this.SidedPos.Motion.Set(Vec3d.Zero);
                    if (!this.beforeCollided
                        && this.Api.Side.IsServer()
                        && this.msSinceCollide > 500
                    ) {

                        this.CheckEntityCollision();
                        this.beforeCollided = true;
                        this.msCollide      = this.World.ElapsedMilliseconds;

                    } // if ..
                } // void ..


                /// <summary>
                /// Checks collision with entities
                /// </summary>
                /// <returns></returns>
                private bool CheckEntityCollision() {

                    if (this.Api.Side.IsClient()
                        || this.msSinceCollide   <  250
                        || this.ServerPos.Motion == Vec3d.Zero
                    ) return false;

                    Cuboidf projectileBox = this.SelectionBox.Translate(this.ServerPos.XYZFloat);
                    Vec3f   motion        = this.ServerPos.Motion.ToVec3f();

                    if (float.IsNegative(motion.X)) projectileBox.X1 += 1.5f * motion.X;
                    else                            projectileBox.X2 += 1.5f * motion.X;
                    if (float.IsNegative(motion.Y)) projectileBox.Y1 += 1.5f * motion.Y;
                    else                            projectileBox.Y2 += 1.5f * motion.Y;
                    if (float.IsNegative(motion.Z)) projectileBox.Z1 += 1.5f * motion.Z;
                    else                            projectileBox.Z2 += 1.5f * motion.Z;


                    return this.entityPartitioning.GetNearestInteractableEntity(this.SidedPos.XYZ, 5f, (e) => {

                        if (e.EntityId == this.EntityId) return true;
                        if (e.SelectionBox.ToDouble().Translate(e.ServerPos.XYZ).IntersectsOrTouches(projectileBox.ToDouble())) {

                            this.ImpactOnEntity(e);
                            return false;

                        } else return true;
                    }) != null; // ..
                } // bool ..


                /// <summary>
                /// Updates projecttile's rotation based on motion
                /// </summary>
                public virtual void SetRotation() {

                    float speed        = this.SidedPos.Motion.ToVec3f().Length();
                    float inverseSpeed = 1f / speed;

                    if (speed > 0.01f) {

                        this.SidedPos.Pitch = 0f;
                        this.SidedPos.Yaw   = 
                            GameMath.PI + MathF.Atan2((float)this.SidedPos.Motion.X * inverseSpeed, (float)this.SidedPos.Motion.Z * inverseSpeed)
                            + GameMath.FastCos((World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
                        this.SidedPos.Roll =
                            - MathF.Asin(GameMath.Clamp(-(float)this.SidedPos.Motion.Y * inverseSpeed, -1, 1))
                            + GameMath.FastSin((World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
                    } // if ..
                } // void ..


                public override void ToBytes(BinaryWriter writer, bool forClient) {
                    base.ToBytes(writer, forClient);
                    writer.Write(this.beforeCollided);
                } // ..
                

                public override void FromBytes(BinaryReader reader, bool fromServer) {
                    base.FromBytes(reader, fromServer);
                    this.beforeCollided = reader.ReadBoolean();
                } // ..
    } // class ..
} // namespace ..
