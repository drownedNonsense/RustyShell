using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.API.Server;

namespace RustyShell;
public abstract class EntityExplosive : Entity {

    //=======================
    // D E F I N I T I O N S
    //=======================

        /** <summary> Reference to the source entity </summary> **/ public Entity FiredBy;
        
        public IExplosive ExplosiveData;
        public bool AffectedByWind = true;

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

                this.stuck = this.Collided;

                base.OnGameTick(deltaTime);

                if (this.stuck) {

                    this.HandleCollision();
                    return;

                } else {
                    if (this.AffectedByWind && this.Api.Side == EnumAppSide.Server) {

                        Vec3d windspeed = this.World.BlockAccessor.GetWindSpeedAt(this.SidedPos.AsBlockPos) * 0.25;
                        this.ServerPos.Add(windspeed.X, windspeed.Y, windspeed.Z);

                    } // if ..

                    this.SetRotation();

                } // if ..

                if (
                    this.ExplosiveData?.FlightExpectancy is float flightExpectancy
                    && (int)(flightExpectancy / MathF.Max(MathF.Pow((float)this.ServerPos.Motion.Length(), 3f), 1f) * 1000)
                    < this.msSinceLaunch
                ) {
                    this.HandleBlast();
                    return;
                } // if ..


                this.motionBeforeCollide.Set(this.SidedPos.Motion);
                this.CheckEntityCollision();

            } // void ..


            public void HandleBlast() {
                if (!this.stuck && this.ExplosiveData?.IsFragmentation == true) {
                    if (this.World.Side.IsServer()) {

                        Item subExplosiveItem       = this.World.GetItem(this.ExplosiveData.SubExplosive);
                        IExplosive subExplosiveData = subExplosiveItem as IExplosive;
                        string entityCode           = subExplosiveItem
                            .Attributes["entityCode"]?
                            .AsString();

                        EntityProperties type   = this.World.GetEntityType(entityCode == null ? subExplosiveItem.Code : new AssetLocation(entityCode));
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

                                EntityExplosive projectile = this.World.ClassRegistry.CreateEntity(type) as EntityExplosive;

                                projectile.FiredBy       = this.FiredBy;
                                projectile.ExplosiveData = subExplosiveData;
                                projectile.ServerPos.SetPos(this.ServerPos);
                                projectile.ServerPos.Motion = Mat4f.MulWithVec3(alignmentMatrix, direction.X, direction.Y, direction.Z).ToVec3d() * random.NextSingle();
                                projectile.Pos.SetFrom(projectile.ServerPos);
                                this.World.SpawnEntity(projectile);
                                if (subExplosiveData.Type == EnumExplosiveType.Incendiary) projectile.IsOnFire = true;

                                projectile = this.World.ClassRegistry.CreateEntity(type) as EntityExplosive;
                                projectile.FiredBy       = this.FiredBy;
                                projectile.ExplosiveData = subExplosiveData;
                                projectile.ServerPos.SetPos(this.ServerPos);
                                projectile.ServerPos.Motion = Mat4f.MulWithVec3(alignmentMatrix, direction.X, -direction.Y, direction.Z).ToVec3d() * random.NextSingle();
                                projectile.Pos.SetFrom(projectile.ServerPos);
                                this.World.SpawnEntity(projectile);
                                if (subExplosiveData.Type == EnumExplosiveType.Incendiary) projectile.IsOnFire = true;

                                projectile = this.World.ClassRegistry.CreateEntity(type) as EntityExplosive;
                                projectile.FiredBy       = this.FiredBy;
                                projectile.ExplosiveData = subExplosiveData;
                                projectile.ServerPos.SetPos(this.ServerPos);
                                projectile.ServerPos.Motion = Mat4f.MulWithVec3(alignmentMatrix, direction.X, direction.Y, -direction.Z).ToVec3d() * random.NextSingle();
                                projectile.Pos.SetFrom(projectile.ServerPos);
                                this.World.SpawnEntity(projectile);
                                if (subExplosiveData.Type == EnumExplosiveType.Incendiary) projectile.IsOnFire = true;

                                projectile = this.World.ClassRegistry.CreateEntity(type) as EntityExplosive;
                                projectile.FiredBy       = this.FiredBy;
                                projectile.ExplosiveData = subExplosiveData;
                                projectile.ServerPos.SetPos(this.ServerPos);
                                projectile.ServerPos.Motion = Mat4f.MulWithVec3(alignmentMatrix, direction.X, -direction.Y, -direction.Z).ToVec3d() * random.NextSingle();
                                projectile.Pos.SetFrom(projectile.ServerPos);
                                this.World.SpawnEntity(projectile);
                                if (subExplosiveData.Type == EnumExplosiveType.Incendiary) projectile.IsOnFire = true;

                            } // for ..
                        } // for ..

                        this.Die();
                    } // if ..
                } else switch(this.ExplosiveData?.Type) {
                    case EnumExplosiveType.Common        : { this.HandleCommonBlast();                   break; }
                    case EnumExplosiveType.Explosive     : { this.HandleExplosiveBlast();                break; }
                    case EnumExplosiveType.AntiPersonnel : { this.HandleAntiPersonnelBlast();            break; }
                    case EnumExplosiveType.Gas           : { this.HandleGasBlast();                      break; }
                    case EnumExplosiveType.Incendiary    : { this.HandleIncendiaryBlast();               break; }
                    default                              : { if (this.World.Side.IsServer()) this.Die(); break; }
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
                if ((this.World as IServerWorldAccessor)?.CanDamageEntity(this.FiredBy, entity, out bool isFromPlayer) ?? false) {

                    bool didDamage = entity.ReceiveDamage(new DamageSource() {
                        Source       = isFromPlayer ? EnumDamageSource.Player : EnumDamageSource.Entity,
                        SourceEntity = this,
                        CauseEntity  = this.FiredBy,
                        Type         = EnumDamageType.PiercingAttack
                    }, this.ExplosiveData.Damage * (this.FiredBy?.Stats.GetBlended("rangedWeaponsDamage") ?? 1f));

                    float factor = GameMath.Clamp((1 - entity.Properties.KnockbackResistance) / 10f, 0, 1);
                    entity.SidedPos.Motion.Add(factor * this.SidedPos.Motion.X, factor * this.SidedPos.Motion.Y, factor * this.SidedPos.Motion.Z);

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
            protected void CheckEntityCollision() {

                if (this.Api.Side.IsClient() || this.ServerPos.Motion == Vec3d.Zero) return;

                if (this.World.GetNearestEntity(ServerPos.XYZ, 5f, 5f, (e) => {
                    if (e.EntityId == this.EntityId || !e.IsInteractable)
                        return false;

                    double dist = e.SelectionBox.Clone().Translate(e.ServerPos.XYZFloat).ShortestDistanceFrom(ServerPos.XYZFloat);
                    return dist < 2f;
                }) is Entity target) {
                    this.ImpactOnEntity(target);
                    this.HandleCollision();
                } // if ..
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
