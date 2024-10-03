using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using System.IO;
using Vintagestory.API.Client;

namespace RustyShell {
    public abstract class EntityAmmunition : Entity {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Reference to the source entity </summary> **/          public Entity         FiredBy;
            /** <summary> Reference to the ammunition collectible </summary> **/ public ItemAmmunition Ammunition;

            /** <summary> Projectile velocity before collision </summary> **/                 protected readonly Vec3d motionBeforeCollide = new();
            /** <summary> Indicates whether or not the projectile has collided </summary> **/ protected          bool  stuck;
            /** <summary> Milliseconds at launch </summary> **/                               protected          long  msLaunch;
            /** <summary> Milliseconds at collision </summary> **/                            protected          long  msCollide;
            /** <summary> Milliseconds since launch </summary> **/                            protected          int   msSinceLaunch  => (int)(this.World.ElapsedMilliseconds - this.msLaunch);
            /** <summary> Milliseconds since collision </summary> **/                         protected          int   msSinceCollide => (int)(this.World.ElapsedMilliseconds - this.msCollide);


            protected readonly CollisionTester    collTester = new();
            protected          Cuboidf            collisionTestBox;
            protected          EntityPartitioning entityPartitioning;

            public override bool ApplyGravity                => true;
            public override bool IsInteractable              => false;
            public override bool CanCollect(Entity byEntity) => false;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void Initialize(
                EntityProperties properties,
                ICoreAPI         api,
                long             InChunkIndex3d
            ) {

                base.Initialize(properties, api, InChunkIndex3d);

                this.entityPartitioning = api.ModLoader.GetModSystem<EntityPartitioning>();
                this.msLaunch           = this.World.ElapsedMilliseconds;
                this.collisionTestBox   = this.SelectionBox.Clone().OmniGrowBy(0.05f);

                this.GetBehavior<EntityBehaviorPassivePhysics>().collisionYExtra = 0f;

            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //---------
            // M A I N
            //---------

                public override void OnGameTick(float deltaTime) {

                    if (this.ShouldDespawn) return;

                    bool newStuck = this.Collided
                        || this.collTester.IsColliding(this.World.BlockAccessor, this.collisionTestBox, this.SidedPos.XYZ)
                        || WatchedAttributes.GetBool("stuck");

                    base.OnGameTick(deltaTime);
                    
                    if (this.Api.Side.IsServer() && newStuck != this.stuck) {
                        this.stuck = newStuck;
                        this.WatchedAttributes.SetBool("stuck", this.stuck);
                    } // if ..

                    if (this.stuck) {

                        this.HandleCollision();
                        return;

                    } else {

                        Vec3d windspeed = this.World.BlockAccessor.GetWindSpeedAt(this.SidedPos.AsBlockPos) * 0.25;
                        this.SidedPos.Add(windspeed.X, windspeed.Y, windspeed.Z);
                        this.SetRotation();

                    } // if ..

                    if (
                        this.Ammunition?.FlightExpectancy is float flightExpectancy
                        && (int)(flightExpectancy / MathF.Pow((float)this.ServerPos.Motion.Length(), 3f) * 1000)
                        < this.msSinceLaunch
                    ) {
                        this.HandleBlast();
                        return;
                    } // if ..


                    if (this.CheckEntityCollision(deltaTime)) return;

                    this.motionBeforeCollide.Set(this.SidedPos.Motion);

                } // void ..


                public void HandleBlast() {
                    if (!this.stuck && this.Ammunition?.IsFragmentation == true) {
                        if (this.World.Side.IsServer()) {

                            ItemAmmunition subMunition = this.World.GetItem(this.Ammunition.CodeWithVariants(new () {{ "shape", "submunition" }, { "casingmaterial", "none" }})) as ItemAmmunition;
                            string entityCode          = subMunition
                                .Attributes["entityCode"]?
                                .AsString();

                            EntityProperties type   = this.World.GetEntityType(entityCode == null ? subMunition.Code : new AssetLocation(entityCode));
                            ThreadSafeRandom random = new();
                            
                            float[] alignmentMatrix = Utilities.Matrix.AlignmentMatrix(this.ServerPos.Motion.ToVec3f() * 2f);

                            for (int i = 1; i < 5; i++) {

                                float pitch    = GameMath.PIHALF * 0.5f * random.NextSingle();
                                float cosPitch = GameMath.FastCos(pitch);
                                float sinPitch = GameMath.FastSin(pitch);

                                int amount = 5 / (i + 1);
                                for (int j = 0; j <= amount; j++) {

                                    float yaw       = (GameMath.PIHALF + j / (float)amount * GameMath.PIHALF) * 0.5f;
                                    float cosYaw    = GameMath.FastCos(yaw);
                                    float sinYaw    = GameMath.FastSin(yaw);
                                    Vec3f direction = new (cosPitch * sinYaw, sinPitch, -cosPitch * cosYaw);

                                    EntityAmmunition projectile = this.World.ClassRegistry.CreateEntity(type) as EntityAmmunition;

                                    projectile.FiredBy    = this.FiredBy;
                                    projectile.Ammunition = subMunition;
                                    projectile.ServerPos.SetPos(this.ServerPos);
                                    projectile.ServerPos.Motion = Mat4f.MulWithVec3(alignmentMatrix, direction.X, direction.Y, direction.Z).ToVec3d() * random.NextSingle();
                                    projectile.Pos.SetFrom(projectile.ServerPos);
                                    this.World.SpawnEntity(projectile);
                                    if (subMunition.Type == EnumAmmunitionType.Incendiary) projectile.IsOnFire = true;

                                    projectile = this.World.ClassRegistry.CreateEntity(type) as EntityAmmunition;
                                    projectile.FiredBy    = this.FiredBy;
                                    projectile.Ammunition = subMunition;
                                    projectile.ServerPos.SetPos(this.ServerPos);
                                    projectile.ServerPos.Motion = Mat4f.MulWithVec3(alignmentMatrix, direction.X, -direction.Y, direction.Z).ToVec3d() * random.NextSingle();
                                    projectile.Pos.SetFrom(projectile.ServerPos);
                                    this.World.SpawnEntity(projectile);
                                    if (subMunition.Type == EnumAmmunitionType.Incendiary) projectile.IsOnFire = true;

                                    projectile = this.World.ClassRegistry.CreateEntity(type) as EntityAmmunition;
                                    projectile.FiredBy    = this.FiredBy;
                                    projectile.Ammunition = subMunition;
                                    projectile.ServerPos.SetPos(this.ServerPos);
                                    projectile.ServerPos.Motion = Mat4f.MulWithVec3(alignmentMatrix, direction.X, direction.Y, -direction.Z).ToVec3d() * random.NextSingle();
                                    projectile.Pos.SetFrom(projectile.ServerPos);
                                    this.World.SpawnEntity(projectile);
                                    if (subMunition.Type == EnumAmmunitionType.Incendiary) projectile.IsOnFire = true;

                                    projectile = this.World.ClassRegistry.CreateEntity(type) as EntityAmmunition;
                                    projectile.FiredBy    = this.FiredBy;
                                    projectile.Ammunition = subMunition;
                                    projectile.ServerPos.SetPos(this.ServerPos);
                                    projectile.ServerPos.Motion = Mat4f.MulWithVec3(alignmentMatrix, direction.X, -direction.Y, -direction.Z).ToVec3d() * random.NextSingle();
                                    projectile.Pos.SetFrom(projectile.ServerPos);
                                    this.World.SpawnEntity(projectile);
                                    if (subMunition.Type == EnumAmmunitionType.Incendiary) projectile.IsOnFire = true;

                                } // for ..
                            } // for ..

                            this.Die();
                        } // if ..
                    } else switch(this.Ammunition?.Type) {
                        case EnumAmmunitionType.Common        : { this.HandleCommonBlast();                   break; }
                        case EnumAmmunitionType.Explosive     : { this.HandleExplosiveBlast();                break; }
                        case EnumAmmunitionType.AntiPersonnel : { this.HandleAntiPersonnelBlast();            break; }
                        case EnumAmmunitionType.Gas           : { this.HandleGasBlast();                      break; }
                        case EnumAmmunitionType.Incendiary    : { this.HandleIncendiaryBlast();               break; }
                        default                               : { if (this.World.Side.IsServer()) this.Die(); break; }
                    } // switch ..
                } // void ..


                public virtual void HandleCommonBlast()        { if (this.World.Side.IsServer()) this.Die(); }
                public virtual void HandleExplosiveBlast()     { if (this.World.Side.IsServer()) this.Die(); }
                public virtual void HandleAntiPersonnelBlast() { if (this.World.Side.IsServer()) this.Die(); }
                public virtual void HandleGasBlast()           { if (this.World.Side.IsServer()) this.Die(); }
                public virtual void HandleIncendiaryBlast()    { if (this.World.Side.IsServer()) this.Die(); }


            //---------------
            // P H Y S I C S
            //---------------

                public override bool ShouldReceiveDamage(DamageSource damageSource, float damage) => false;
                public override void OnCollided() {
                    this.HandleCollision();
                    this.motionBeforeCollide.Set(this.SidedPos.Motion);
                } // void ..



                public override void OnCollideWithLiquid() {
                    base.OnCollideWithLiquid();
                    this.Die();
                } // void ..

                public virtual void ImpactOnEntity(Entity entity) {

                    this.World.Logger.Chat(entity.Code.Path);
                    if ((this.World as IServerWorldAccessor)?.CanDamageEntity(this.FiredBy, entity, out bool isFromPlayer) ?? false) {

                        this.msCollide = this.World.ElapsedMilliseconds;
                        this.SidedPos.Motion.Set(Vec3d.Zero);

                        bool didDamage = entity.ReceiveDamage(new DamageSource() {
                            Source       = isFromPlayer ? EnumDamageSource.Player : EnumDamageSource.Entity,
                            SourceEntity = this,
                            CauseEntity  = this.FiredBy,
                            Type         = EnumDamageType.PiercingAttack
                        }, this.Ammunition.Damage * (this.FiredBy?.Stats.GetBlended("rangedWeaponsDamage") ?? 1f));

                        float knockbackResistance = entity.Properties.KnockbackResistance;
                        entity.SidedPos.Motion.Add(knockbackResistance * this.SidedPos.Motion.X, knockbackResistance * this.SidedPos.Motion.Y, knockbackResistance * this.SidedPos.Motion.Z);

                        this.HandleBlast();

                        if (isFromPlayer && didDamage)
                            this.World.PlaySoundFor(new AssetLocation("sounds/player/projectilehit"), (this.FiredBy as EntityPlayer).Player, false, 24);

                    } // if ..
                } // void ..
                
                public virtual void HandleCollision() {
                    if (!this.stuck) {

                        this.stuck     = true;
                        this.msCollide = this.World.ElapsedMilliseconds;

                    } // if ..

                    this.HandleBlast();
                } // void ..


                /// <summary>
                /// Checks collision with entities
                /// </summary>
                protected bool CheckEntityCollision(float deltaTime) {

                    if (this.Api.Side.IsClient() || this.ServerPos.Motion == Vec3d.Zero) return false;

                    Cuboidf projectileBox = this.SelectionBox.OmniGrowBy(0.05f).Translate(this.ServerPos.XYZFloat);
                    Vec3f   motion        = this.ServerPos.Motion.ToVec3f();

                    if (float.IsNegative(motion.X)) projectileBox.X1 += 1.05f * deltaTime * motion.X;
                    else                            projectileBox.X2 += 1.05f * deltaTime * motion.X;
                    if (float.IsNegative(motion.Y)) projectileBox.Y1 += 1.05f * deltaTime * motion.Y;
                    else                            projectileBox.Y2 += 1.05f * deltaTime * motion.Y;
                    if (float.IsNegative(motion.Z)) projectileBox.Z1 += 1.05f * deltaTime * motion.Z;
                    else                            projectileBox.Z2 += 1.05f * deltaTime * motion.Z;


                    if (this.entityPartitioning.GetNearestInteractableEntity(this.SidedPos.XYZ, 5f, (e) => {

                        if (e.EntityId == this.EntityId || !e.Alive) return false;
                        return e.SelectionBox
                            .OmniGrowBy(0.5f)
                            .ToDouble()
                            .Translate(e.ServerPos.XYZ)
                            .IntersectsOrTouches(projectileBox.ToDouble());
                    }) is Entity target) {
                        this.ImpactOnEntity(target);
                        return true;
                    } // if ..

                    return false;
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
                        this.SidedPos.Roll = GameMath.PIHALF
                            - MathF.Asin(GameMath.Clamp(-(float)this.SidedPos.Motion.Y * inverseSpeed, -1, 1))
                            + GameMath.FastSin((World.ElapsedMilliseconds - msLaunch) / 200f) * 0.03f;
                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
