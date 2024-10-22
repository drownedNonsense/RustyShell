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

namespace RustyShell;
public class ItemGrenade : Item, IExplosive {

    //=======================
    // D E F I N I T I O N S
    //=======================

        public EnumExplosiveType Type            { get; protected set; }
        public bool              IsFragmentation { get; protected set; } = false;
        public bool              IsSubmunition   { get; protected set; } = false;
        public float             Damage          { get; protected set; }
        public int?              BlastRadius     { get; protected set; }
        public int?              InjureRadius    { get; protected set; }
        public bool              CanBounce       { get; protected set; }

        public float? FlightExpectancy { get; protected set; }

        public AssetLocation SubExplosive         { get; protected set; } = null;
        public int?          SubExplosiveCount    { get; protected set; } = null;
        public int?          FragmentationConeDeg { get; protected set; } = null;


    //===============================
    // I N I T I A L I Z A T I O N S
    //===============================

        public override void OnLoaded(ICoreAPI api) {

            base.OnLoaded(api);
            this.Type = this.Variant["type"] switch {
                "common"        => EnumExplosiveType.Common,
                "explosive"     => EnumExplosiveType.Explosive,
                "antipersonnel" => EnumExplosiveType.AntiPersonnel,
                "gas"           => EnumExplosiveType.Gas,
                "incendiary"    => EnumExplosiveType.Incendiary,
                _               => EnumExplosiveType.Common,
            }; // switch ..

            this.Damage           = this.Attributes["damage"].AsInt();
            this.BlastRadius      = this.Attributes["blastRadius"].AsInt();
            this.InjureRadius     = this.Attributes["injureRadius"].AsInt();
            this.FlightExpectancy = this.Attributes["flightExpectancy"].Exists ? this.Attributes["flightExpectancy"].AsFloat() : null;
            this.CanBounce        = this.Attributes["canBounce"].AsBool();

        } // void ..


    //===============================
    // I M P L E M E N T A T I O N S
    //===============================

        //-------------------------
        // I N T E R A C T I O N S
        //-------------------------

            public override void GetHeldItemInfo(
                ItemSlot inSlot,
                StringBuilder dsc,
                IWorldAccessor world,
                bool withDebugInfo
            ) {

                if (this.Damage       > 0f) dsc.AppendLine(Lang.Get("explosive-damage",       this.Damage));
                if (this.BlastRadius  > 0)  dsc.AppendLine(Lang.Get("explosive-blastradius",  this.BlastRadius));
                if (this.InjureRadius > 0)  dsc.AppendLine(Lang.Get("explosive-injureradius", this.InjureRadius));
                
                if (this.FlightExpectancy > 0f) dsc.AppendLine(Lang.Get("explosive-flightexpectancy", this.FlightExpectancy));

                base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            } // void ..

            public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity) => null;

            public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot) {
                base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
                attackedEntity.ReceiveDamage(new DamageSource() {
                    Source      = EnumDamageSource.Explosion,
                    Type        = EnumDamageType.PiercingAttack,
                    CauseEntity = byEntity
                }, this.Damage); // ..


                switch (this.Type) {
                    case EnumExplosiveType.Common or EnumExplosiveType.Explosive: {
                        if (world is IServerWorldAccessor serverWorld)
                            serverWorld.CommonBlast(
                                byEntity     : byEntity,
                                pos          : attackedEntity.ServerPos.XYZFloat,
                                blastRadius  : this.BlastRadius  ?? 0,
                                injureRadius : this.InjureRadius ?? 0,
                                strength     : this.Type switch {
                                    EnumExplosiveType.Common    => RustyShellModSystem.ModConfig.CommonHighcaliberReinforcmentImpact,
                                    EnumExplosiveType.Explosive => RustyShellModSystem.ModConfig.ExplosiveHighcaliberReinforcmentImpact,
                                    _                           => 1,
                                } // switch ..
                            ); // ..

                        break;
                    } // case ..
                    case EnumExplosiveType.AntiPersonnel: {
                        if (world is IServerWorldAccessor serverWorld)
                            serverWorld.CreateExplosion(
                                pos                       : attackedEntity.ServerPos.AsBlockPos,
                                blastType                 : EnumBlastType.EntityBlast,
                                destructionRadius         : this.BlastRadius  ?? 0,
                                injureRadius              : this.InjureRadius ?? 0,
                                blockDropChanceMultiplier : 0f
                            ); // ..

                        break;
                    } // case ..
                    case EnumExplosiveType.Gas: {
                        world.GasBlast(
                            byEntity            : byEntity,
                            pos                 : attackedEntity.SidedPos.XYZFloat,
                            blastRadius         : this.BlastRadius ?? 0,
                            millisecondDuration : this.IsSubmunition ? 10000 : 20000
                        ); // ..
                        break;
                    } // case ..
                    case EnumExplosiveType.Incendiary: {
                        if (world is IServerWorldAccessor serverWorld)
                            serverWorld.IncendiaryBlast(
                                byEntity     : byEntity,
                                pos          : attackedEntity.ServerPos.XYZFloat,
                                blastRadius  : this.BlastRadius  ?? 0,
                                injureRadius : this.InjureRadius ?? 0
                            ); // ..

                        break;
                    } // case ..
                } // switch ..
                itemslot.TakeOut(1);
                itemslot.MarkDirty();
            } // void ..

            public override void OnHeldInteractStart(
                ItemSlot itemslot,
                EntityAgent byEntity,
                BlockSelection blockSel,
                EntitySelection entitySel,
                bool firstEvent,
                ref EnumHandHandling handling
            ) {

                byEntity.Attributes.SetInt("aiming", 1);
                byEntity.Attributes.SetInt("aimingCancel", 0);
                byEntity.AnimManager.StartAnimation("slingaimgreek");

                handling = EnumHandHandling.PreventDefault;

            } // void ..

            public override bool OnHeldInteractStep(
                float secondsUsed,
                ItemSlot slot,
                EntityAgent byEntity,
                BlockSelection blockSel,
                EntitySelection entitySel
            ) => true;


            public override bool OnHeldInteractCancel(
                float secondsUsed,
                ItemSlot slot,
                EntityAgent byEntity,
                BlockSelection blockSel,
                EntitySelection entitySel,
                EnumItemUseCancelReason cancelReason
            ) {

                byEntity.Attributes.SetInt("aiming", 0);
                if (cancelReason != EnumItemUseCancelReason.ReleasedMouse)
                    byEntity.Attributes.SetInt("aimingCancel", 1);

                byEntity.StopAnimation("slingaimgreek");
                return true;

            } // bool ..


            public override void OnHeldInteractStop(
                float secondsUsed,
                ItemSlot slot,
                EntityAgent byEntity,
                BlockSelection blockSel,
                EntitySelection entitySel
            ) {
                
                if (byEntity.Attributes.GetInt("aimingCancel") == 1) return;

                byEntity.Attributes.SetInt("aiming", 0);
                byEntity.AnimManager.StopAnimation("slingaimgreek");

                if (secondsUsed < 0.35f) return;

                slot.TakeOut(1);
                slot.MarkDirty();

                IPlayer byPlayer = null;
                if (byEntity is EntityPlayer player) byPlayer = byEntity.World.PlayerByUid(player.PlayerUID);
                byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/throw"), byEntity, byPlayer, false, 8);

                string entityCode = this.Attributes["entityCode"]?.AsString();

                EntityProperties type      = byEntity.World.GetEntityType(entityCode == null ? this.Code : new AssetLocation(entityCode));
                EntityExplosive projectile = byEntity.World.ClassRegistry.CreateEntity(type) as EntityExplosive;
                
                float acc       = 1 - byEntity.Attributes.GetFloat("aimingAccuracy", 0);
                double rndpitch = byEntity.WatchedAttributes.GetDouble("aimingRandPitch", 1) * acc * 0.75;
                double rndyaw   = byEntity.WatchedAttributes.GetDouble("aimingRandYaw", 1) * acc * 0.75;

                Vec3d pos      = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y - 0.2, 0);
                Vec3d aheadPos = pos.AheadCopy(1, byEntity.ServerPos.Pitch + rndpitch, byEntity.ServerPos.Yaw + rndyaw);
                Vec3d velocity = (aheadPos - pos) * 0.5;

                projectile.FiredBy       = byEntity;
                projectile.ExplosiveData = this;
                projectile.AffectedByWind = false;
                projectile.ServerPos.SetPos(byEntity.ServerPos.AheadCopy(4).XYZ.Add(0, byEntity.LocalEyePos.Y - 0.2, 0).Ahead(0.5, 0, byEntity.ServerPos.Yaw + GameMath.PIHALF));
                projectile.ServerPos.Motion.Set(velocity);
                projectile.Pos.SetFrom(projectile.ServerPos);

                byEntity.World.SpawnEntity(projectile);

                byEntity.AnimManager.StartAnimation("slingthrowgreek");
                byEntity.World.RegisterCallback((dt) => byEntity.AnimManager.StopAnimation("slingthrowgreek"), 400);

                if (byEntity is EntityPlayer) RefillSlotIfEmpty(slot, byEntity, (itemstack) => itemstack.Collectible.Code == Code);

            } // void ..


            public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) =>
            new WorldInteraction[] {
                new () {
                    ActionLangCode = "heldhelp-throw",
                    MouseButton    = EnumMouseButton.Right,
                } // ..
            }.Append(base.GetHeldInteractionHelp(inSlot));
} // class ..
