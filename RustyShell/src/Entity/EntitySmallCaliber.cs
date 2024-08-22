using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace RustyShell {
    public class EntitySmallCaliber : EntityAmmunition {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Blast size at impact </summary> **/ public int ImpactSize;


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //---------------
            // P H Y S I C S
            //---------------

                public override void ImpactOnEntity(Entity entity) {
                    if ((this.World as IServerWorldAccessor)?.CanDamageEntity(this.FiredBy, entity, out bool isFromPlayer) ?? false) {
                        bool didDamage = entity.ReceiveDamage(new DamageSource() {
                            Source       = isFromPlayer ? EnumDamageSource.Player : EnumDamageSource.Entity,
                            SourceEntity = this,
                            CauseEntity  = this.FiredBy,
                            Type         = EnumDamageType.PiercingAttack
                        }, 50);

                        entity.SidedPos.Motion.Add(entity.Properties.KnockbackResistance * 0.1f * this.SidedPos.Motion.ToVec3f());

                        this.Die();

                        if (isFromPlayer && didDamage)
                            this.World.PlaySoundFor(new AssetLocation("sounds/player/projectilehit"), (this.FiredBy as EntityPlayer).Player, false, 24);

                    } // if ..
                } // void ..


                public override void IsColliding() {
                    if (!this.stuck) {

                        this.CheckEntityCollision();
                        this.stuck = true;

                    } // if ..

                    if (this.ImpactSize != 0 && this.Api.Side.IsServer())
                        (this.World as IServerWorldAccessor)?.CreateExplosion(
                            this.ServerPos.AsBlockPos,
                            EnumBlastType.EntityBlast,
                            GameMath.RoundRandom(this.World.Rand, this.ImpactSize * 0.01f),
                            this.World.Rand.Next(0, this.ImpactSize),
                            0f
                        ); // ..

                    this.Die();
                } // void ..
    } // class ..
} // namespace ..
