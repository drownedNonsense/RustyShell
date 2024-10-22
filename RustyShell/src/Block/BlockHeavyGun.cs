using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using System.Linq;


namespace RustyShell {
    public class BlockHeavyGun : BlockOrientable {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Barrel type between smoothbore and rifled </summary> **/          public EnumBarrelType BarrelType { get; private set; }
            /** <summary> Projectile maximum velocity in block per second </summary> **/    public float FirePower           { get; private set; }
            /** <summary> Projectile accuracy ranged between 0 and 1 </summary> **/         public float Accuracy            { get; private set; }
            /** <summary> Projectile spawn distance from the gun root </summary> **/        public float BarrelLength        { get; private set; }
            /** <summary> Duration in second until the gun can be used again </summary> **/ public float CooldownDuration    { get; private set; }

            /** <summary> Indicates whether or not the gun requires muzzle loading and cleaning </summary> **/ public bool MuzzleLoading { get; private set; }
            /** <summary> Indicates whether or not the gun elevation can be adjusted </summary> **/            public bool GearedGun     { get; private set; }
            /** <summary> Indicates whether or not the gun has wheels </summary> **/                           public bool Wheeled       { get; private set; }
            /** <summary> Indicates whether or not the gun can fire multiple times </summary> **/              public bool Repeating     { get; private set; }


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);
                this.BarrelType = this.Variant["barrel"] switch {
                    "smoothbore" => EnumBarrelType.Smoothbore,
                    "rifled"     => EnumBarrelType.Rifled,
                    _            => EnumBarrelType.Smoothbore
                }; // ..

                this.FirePower        = Math.Abs(      this.Attributes["firePower"].AsFloat(1f));
                this.BarrelLength     = Math.Abs(      this.Attributes["barrelLength"].AsFloat(0f));
                this.Accuracy         = GameMath.Clamp(this.Attributes["accuracy"].AsFloat(1f), 0f, 1f);
                this.CooldownDuration = Math.Abs(      this.Attributes["cooldown"].AsFloat(4f));

                this.MuzzleLoading = this.HasBehavior<BlockBehaviorMuzzleLoading>();
                this.GearedGun     = this.HasBehavior<BlockBehaviorGearedGun>();
                this.Wheeled       = this.HasBehavior<BlockBehaviorWheeled>();
                this.Repeating     = this.HasBehavior<BlockBehaviorRepeatingFire>();

            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                public override EnumItemStorageFlags GetStorageFlags(ItemStack itemstack) => EnumItemStorageFlags.Backpack;
                public override void GetHeldItemInfo(
                    ItemSlot inSlot,
                    StringBuilder dsc,
                    IWorldAccessor world,
                    bool withDebugInfo
                ) {

                    int   accuracy  = (int)(inSlot.Itemstack.Collectible.Attributes["accuracy"].AsFloat() * 100);
                    float firePower = inSlot.Itemstack.Collectible.Attributes["firePower"].AsFloat();
                    float cooldown  = inSlot.Itemstack.Collectible.Attributes["cooldown"].AsFloat();

                    BlockBehaviorRepeatingFire repeatingFire = this.GetBehavior<BlockBehaviorRepeatingFire>();
                    float? fireInterval = 1f / repeatingFire?.FireInterval;

                    BlockBehaviorRotating rotating = this.GetBehavior<BlockBehaviorRotating>();
                    float? turnSpeed = rotating?.TurnSpeed / GameMath.PI;

                    BlockBehaviorGearedGun gearedGun = this.GetBehavior<BlockBehaviorGearedGun>();
                    (float, float)? elevation  = (gearedGun != null) ? (gearedGun.MinElevation, gearedGun.MaxElevation) : null;
                    float? averageRecoilEffect = gearedGun?.RecoilEffect.avg;

                    BlockBehaviorMuzzleLoading muzzleLoading = this.GetBehavior<BlockBehaviorMuzzleLoading>();
                    float? cleanDuration = muzzleLoading?.CleanDuraction;
                    float? loadDuration  = muzzleLoading?.LoadDuration;

                    if (this.Variant["barrel"] is string barrelType) {
                        dsc.AppendLine(Lang.Get($"heavygun-{barrelType}"));
                        dsc.AppendLine();
                    } // if ..
                    
                    if (fireInterval is float)       dsc.AppendLine(Lang.Get("heavygun-fireinterval",  fireInterval));
                    if (accuracy > 0f)               dsc.AppendLine(Lang.Get("heavygun-accuracy",      accuracy));
                    if (firePower > 0f)              dsc.AppendLine(Lang.Get("heavygun-firepower",     firePower));
                    if (cooldown > 0f)               dsc.AppendLine(Lang.Get("heavygun-cooldown",      cooldown));

                    if (
                        (fireInterval is float ||accuracy > 0f || firePower > 0f || cooldown > 0f) &&
                        (elevation is (float, float) || averageRecoilEffect > 0f)
                    ) dsc.AppendLine();

                    if (elevation is (float, float)) dsc.AppendLine(Lang.Get("heavygun-elevation",     elevation?.Item1, elevation?.Item2));
                    if (averageRecoilEffect > 0f)    dsc.AppendLine(Lang.Get("heavygun-recoileffect",  averageRecoilEffect));
                    
                    if (
                        (elevation is (float, float) || averageRecoilEffect > 0f) &&
                        (turnSpeed > 0f || cleanDuration is float || loadDuration is float)
                    ) dsc.AppendLine();

                    if (turnSpeed > 0f)          dsc.AppendLine(Lang.Get("heavygun-turnspeed",     turnSpeed));
                    if (cleanDuration is float)  dsc.AppendLine(Lang.Get("heavygun-cleanduration", cleanDuration));
                    if (loadDuration  is float)  dsc.AppendLine(Lang.Get("heavygun-loadduration",  loadDuration));
                    
                    base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
                    
                } // void ..


                public override ItemStack[] GetDrops(
                    IWorldAccessor world,
                    BlockPos pos,
                    IPlayer byPlayer,
                    float dropQuantityMultiplier = 1
                ) {
                    ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
                    if (world.BlockAccessor.GetBlockEntity<BlockEntityHeavyGun>(pos) is BlockEntityHeavyGun heavyGun) {
                        
                        if (heavyGun.AmmunitionSlot.Itemstack is not null) drops = drops.Append(heavyGun.AmmunitionSlot.Itemstack).ToArray();
                        if (heavyGun.ChargeSlot.Itemstack is not null)     drops = drops.Append(heavyGun.ChargeSlot.Itemstack).ToArray();
                        
                    } // if ..

                    return drops;
                } // ItemStack ..
    } // class ..
} // namespace ..
