using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RustyShell {
    public abstract class EntityAmmunition : Entity {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Reference to the source entity </summary> **/ public Entity FiredBy;

            /** <summary> Projectile velocity before collision </summary> **/                 protected readonly Vec3d motionBeforeCollide = new();
            /** <summary> Indicates whether or not the projectile has collided </summary> **/ protected          bool  stuck;

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

                        this.IsColliding();
                        return;

                    } // if ..


                    if (this.CheckEntityCollision()) return;

                    this.motionBeforeCollide.Set(this.SidedPos.Motion);

                } // void ..


            //---------------
            // P H Y S I C S
            //---------------

                public override bool ShouldReceiveDamage(DamageSource damageSource, float damage) => false;
                public override void OnCollided() {
                    this.IsColliding();
                    this.motionBeforeCollide.Set(this.SidedPos.Motion);
                } // void ..



                public override void OnCollideWithLiquid() {
                    base.OnCollideWithLiquid();
                    this.Die();
                } // void ..

                public abstract void ImpactOnEntity(Entity entity);
                public abstract void IsColliding();


                /// <summary>
                /// Checks collision with entities
                /// </summary>
                /// <returns></returns>
                protected bool CheckEntityCollision() {

                    if (this.Api.Side.IsClient() || this.ServerPos.Motion == Vec3d.Zero) return false;

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
    } // class ..
} // namespace ..
