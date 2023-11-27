using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace RustyShell {
    public class EntitySmallCaliber : Entity {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Reference to the source entity </summary> **/ public Entity FiredBy;
            /** <summary> Blast size at impact </summary> **/           public int    ImpactSize;

            /** <summary> Projectile velocity before collision </summary> **/                 private readonly Vec3d motionBeforeCollide = new();
            /** <summary> Indicates whether or not the projectile has collided </summary> **/ private          bool  stuck;

            /** <summary> Milliseconds at collision </summary> **/    private long msCollide;
            /** <summary> Milliseconds since collision </summary> **/ private int  msSinceCollide => (int)(this.World.ElapsedMilliseconds - this.msCollide);

            private readonly CollisionTester    collTester = new();
            private          Cuboidf            collisionTestBox;
            private          EntityPartitioning entityPartitioning;

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
                this.GetBehavior<EntityBehaviorPassivePhysics>().collisionYExtra = 0f;

                this.collisionTestBox   = this.CollisionBox.Clone().OmniGrowBy(0.05f);
                this.entityPartitioning = api.ModLoader.GetModSystem<EntityPartitioning>();

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
                        || (this.Api.Side.IsClient() && this.WatchedAttributes.GetBool("stuck"));
                    
                    if (this.Api.Side.IsServer() && newStuck != this.stuck) {
                        this.stuck = newStuck;
                        this.WatchedAttributes.SetBool("stuck", this.stuck);
                    } // if ..


                    if (this.stuck) {

                        if (this.Api.Side.IsClient())
                            this.ServerPos.SetFrom(this.Pos);

                        this.IsColliding();
                        return;

                    } // if ..


                    if (this.CheckEntityCollision()) return;
                    this.motionBeforeCollide.Set(this.SidedPos.Motion.X, this.SidedPos.Motion.Y, this.SidedPos.Motion.Z);

                } // void ..


                private void ImpactOnEntity(Entity entity) {
                    if ((this.World as IServerWorldAccessor)?.CanDamageEntity(this.FiredBy, entity, out bool isFromPlayer) ?? false) {

                        this.msCollide = this.World.ElapsedMilliseconds;
                        this.SidedPos.Motion.Set(Vec3d.Zero);

                        bool didDamage = entity.ReceiveDamage(new DamageSource() {
                            Source       = isFromPlayer ? EnumDamageSource.Player : EnumDamageSource.Entity,
                            SourceEntity = this,
                            CauseEntity  = this.FiredBy,
                            Type         = EnumDamageType.PiercingAttack
                        }, 40);

                        entity.SidedPos.Motion.Add(entity.Properties.KnockbackResistance * 0.1f * this.SidedPos.Motion.ToVec3f());

                        this.Die();

                        if (isFromPlayer && didDamage)
                            this.World.PlaySoundFor(new AssetLocation("sounds/player/projectilehit"), (this.FiredBy as EntityPlayer).Player, false, 24);

                    } // if ..
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
                    this.Die();
                } // void ..


                private void IsColliding() {

                    this.SidedPos.Motion.Set(0, 0, 0);
                    if (!this.stuck
                        && this.Api.Side.IsServer()
                        && this.msSinceCollide > 500
                    ) {

                        this.CheckEntityCollision();

                        this.msCollide = World.ElapsedMilliseconds;
                        this.stuck     = true;

                        if (this.ImpactSize != 0)
                            (this.World as IServerWorldAccessor)?.CreateExplosion(
                                this.ServerPos.AsBlockPos,
                                EnumBlastType.EntityBlast,
                                GameMath.RoundRandom(this.World.Rand, this.ImpactSize * 0.01f),
                                this.World.Rand.Next(0, this.ImpactSize),
                                0f
                            ); // ..

                        this.Die();

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

                        if (e.EntityId == this.EntityId) return false;

                        Cuboidf translatedBox = e.SelectionBox.Translate(e.ServerPos.XYZ);
                        if (   translatedBox.X2 >= projectileBox.X1
                            && translatedBox.X1 <= projectileBox.X2
                            && translatedBox.Y2 >= projectileBox.Y1
                            && translatedBox.Y1 <= projectileBox.Y2
                            && translatedBox.Z2 >= projectileBox.Z1
                            && translatedBox.Z1 <= projectileBox.Z2
                        ) {

                            this.ImpactOnEntity(e);
                            return true;

                        } else return false;
                    }) != null;
                } // bool ..
    } // class ..
} // namespace ..
