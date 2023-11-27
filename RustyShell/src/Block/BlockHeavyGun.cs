using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;


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

            /** <summary> Indicates whether or not the gun requires muzzle loading and cleaning </summary> **/ public bool MuzzleLoading        { get; private set; }
            /** <summary> Indicates whether or not the gun elevation can be adjusted </summary> **/            public bool GearedGun            { get; private set; }
            /** <summary> Indicates whether or not the gun has wheels </summary> **/                           public bool Wheeled              { get; private set; }

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

                    base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

                    if (inSlot.Itemstack.Collectible.Attributes == null) return;

                    int   accuracy  = (int)(inSlot.Itemstack.Collectible.Attributes["accuracy"].AsFloat() * 100);
                    int   firePower = (int)(inSlot.Itemstack.Collectible.Attributes["firePower"].AsFloat() * 40);
                    float cooldown  = inSlot.Itemstack.Collectible.Attributes["cooldown"].AsFloat();

                    BlockBehaviorRepeatingFire repeatingFire = this.GetBehavior<BlockBehaviorRepeatingFire>();
                    float? fireIntervalRaw = 1f / repeatingFire?.FireInterval;

                    BlockBehaviorRotating rotating = this.GetBehavior<BlockBehaviorRotating>();
                    float? turnSpeedRaw = rotating?.TurnSpeed / GameMath.PI;

                    BlockBehaviorGearedGun gearedGun = this.GetBehavior<BlockBehaviorGearedGun>();
                    (float, float)? elevationRaw = (gearedGun != null) ? (gearedGun.MinElevation, gearedGun.MaxElevation) : null;

                    BlockBehaviorMuzzleLoading muzzleLoading = this.GetBehavior<BlockBehaviorMuzzleLoading>();
                    float? cleanDurationRaw = muzzleLoading?.CleanDuraction;
                    float? loadDurationRaw  = muzzleLoading?.LoadDuration;
                    
                    dsc.AppendLine();
                    if (fireIntervalRaw is float fireInterval)    dsc.AppendLine(Lang.Get("heavygun-fireinterval",  fireInterval));
                    if (accuracy != 0)                            dsc.AppendLine(Lang.Get("heavygun-accuracy",      accuracy));
                    if (firePower != 0)                           dsc.AppendLine(Lang.Get("heavygun-firepower",     firePower));
                    if (cooldown != 0)                            dsc.AppendLine(Lang.Get("heavygun-cooldown",      cooldown));
                    if (turnSpeedRaw is float turnSpeed)          dsc.AppendLine(Lang.Get("heavygun-turnspeed",     turnSpeed));
                    if (elevationRaw is (float, float) elevation) dsc.AppendLine(Lang.Get("heavygun-elevation",     elevation.Item1, elevation.Item2));
                    if (cleanDurationRaw is float cleanDuration)  dsc.AppendLine(Lang.Get("heavygun-cleanduration", cleanDuration));
                    if (loadDurationRaw  is float loadDuration)   dsc.AppendLine(Lang.Get("heavygun-loadduration",  loadDuration));
                    
                } // void ..
    } // class ..
} // namespace ..
