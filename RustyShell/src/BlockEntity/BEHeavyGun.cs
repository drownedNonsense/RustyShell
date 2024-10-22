using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;

namespace RustyShell {
    public class BlockEntityHeavyGun : BlockEntityOrientable {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> A reference to the heavy gun block </summary> **/     public BlockHeavyGun BlockHeavyGun { get; private set; }
            /** <summary> A reference to muzzle loading behavior </summary> **/ protected BlockEntityBehaviorMuzzleLoading muzzleLoading;
            
            /** <summary> Remaining time until the gun can be fired </summary> **/     public float Cooldown { get; private set; }
            /** <summary> A reference to the cooldown listener </summary> **/          private long? cooldownUpdateRef;
            /** <summary> A reference to the offset rendering listener </summary> **/  private long? offsetRenderingRef;

            /// <summary>
            /// Projectile velocity in block per second at spawn
            /// </summary>
            private float expectedVelocity => this.BlockHeavyGun.FirePower * GameMath.Sqrt(this.blastStrength * 0.3f);
            
            /// <summary>
            /// Strength of the charge or ammunition's propellant
            /// </summary>
            private float blastStrength => GameMath.Max(
                this.AmmunitionSlot?.Itemstack?.ItemAttributes["propellant"]?["strength"].AsInt() ?? 0,
                (this.ChargeSlot?.Itemstack?.ItemAttributes["propellant"]?["strength"].AsInt() ?? 0) * this.ChargeSlot.StackSize
            ); // ..

            /// <summary>
            /// Current gun state
            /// </summary>
            private EnumGunState gunState {
                get => this.muzzleLoading?.GunState ?? EnumGunState.Ready;
                set {
                    if (this.muzzleLoading is BlockEntityBehaviorMuzzleLoading muzzleLoading)
                        muzzleLoading.GunState = value;
                } // set ..
            } // EnumGunState ..


            /** <summary> Gun's barrel content </summary> **/                         internal InventoryGeneric Inventory      =  null;
            /** <summary> A reference to the charge dedicated slot  </summary> **/    internal ItemSlot         ChargeSlot  => this.Inventory[0];
            /** <summary> A reference to the ammunition dedicated slot </summary> **/ internal ItemSlot         AmmunitionSlot => this.Inventory[1];

            /** <summary> Indicates whether or the gun is waiting to be cleaned </summary> **/ public bool CanClean => this.gunState == EnumGunState.Dirty;
            /** <summary> Indicates whether or the gun is waiting to be filled </summary> **/  public bool CanFill  => this.gunState == EnumGunState.Clean || !this.BlockHeavyGun.MuzzleLoading;
            /** <summary> Indicates whether or the gun is waiting to be loaded </summary> **/  public bool CanLoad  => this.gunState == EnumGunState.Clean && !this.AmmunitionSlot.Empty && (this.FusedAmmunition || !this.ChargeSlot.Empty);
            /** <summary> Indicates whether or the gun is waiting to be fired </summary> **/   public bool CanFire  => this.gunState == EnumGunState.Ready;

            /** <summary> Indicates whether or not the loaded ammunition contains a propellant </summary> **/ public bool FusedAmmunition => this.AmmunitionSlot?.Itemstack?.ItemAttributes["propellant"]?["strength"].AsInt() > 0;

            /** <summary> Gun's offset from initial position </summary> **/ public override float Offset { get; set; }
            

        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockEntityHeavyGun() {
                this.Inventory = new InventoryGeneric(2, null, null);
            } // ..


            public override void Initialize(ICoreAPI api) {
                base.Initialize(api);

                if (api.Side == EnumAppSide.Client) this.LoadOrCreateMesh();

                this.Inventory.LateInitialize("heavygun" + "-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
                this.Inventory.Pos = this.Pos;
                this.Inventory.ResolveBlocksOrItems();

                this.BlockHeavyGun = this.Block as BlockHeavyGun;
                this.muzzleLoading = this.GetBehavior<BlockEntityBehaviorMuzzleLoading>();

            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                /// <summary>
                /// Called to fire the gun if the conditions are met
                /// </summary>
                /// <param name="byEntity"></param>
                public void Fire(Entity byEntity) {

                    if (this.AmmunitionSlot.Itemstack == null) return;
                    if (this.Cooldown > ((this.Block.GetBehavior<BlockBehaviorRepeatingFire>()?.FireInterval + 0.5f) ?? 0f)) return;

                    string entityCode = this.AmmunitionSlot
                        .Itemstack
                        .ItemAttributes["entityCode"]?
                        .AsString();

                    EntityProperties type      = this.Api.World.GetEntityType(entityCode == null ? this.AmmunitionSlot.Itemstack.Item.Code : new AssetLocation(entityCode));
                    EntityExplosive projectile = this.Api.World.ClassRegistry.CreateEntity(type) as EntityExplosive;
                    ItemAmmunition ammunition  = this.AmmunitionSlot.Itemstack.Item as ItemAmmunition;

                    ThreadSafeRandom random = new();
                    float randPitch = (random.NextSingle() - 0.5f) * (1f - this.BlockHeavyGun.Accuracy) * 0.5f;
                    float randYaw   = (random.NextSingle() - 0.5f) * (1f - this.BlockHeavyGun.Accuracy) * 0.5f;

                    BlockEntityBehaviorGearedGun gearedGun = this.GetBehavior<BlockEntityBehaviorGearedGun>();
                    float elevation = gearedGun?.Elevation ?? 0f;

                    float cosPitch = GameMath.FastCos(GameMath.DEG2RAD * elevation + randPitch);
                    float sinPitch = GameMath.FastSin(GameMath.DEG2RAD * elevation + randPitch);
                    float cosYaw   = GameMath.FastCos(this.Orientation + randYaw);
                    float sinYaw   = GameMath.FastSin(this.Orientation + randYaw);

                    Vec3f direction     = new Vec3f(-cosPitch * sinYaw, sinPitch, -cosPitch * cosYaw).Normalize();
                    Vec3d projectilePos = (new Vec3f(this.Pos.X + 0.5f, this.Pos.Y + 1f, this.Pos.Z + 0.5f) + direction * this.BlockHeavyGun.BarrelLength).ToVec3d();
                    Vec3f velocity      = direction * this.expectedVelocity;

                    this.gunState = this.BlockHeavyGun.MuzzleLoading
                        ? EnumGunState.Dirty
                        : this.AmmunitionSlot.Empty
                            ? EnumGunState.Clean
                            : EnumGunState.Ready;

                    projectile.FiredBy       = byEntity;
                    projectile.ExplosiveData = ammunition;
                    projectile.ServerPos.SetPos(projectilePos);
                    projectile.ServerPos.Motion.Set(velocity);
                    projectile.Pos.SetFrom(projectile.ServerPos);

                    if (projectile is EntityHighCaliber highCaliber)
                        this.offsetRenderingRef ??= this.RegisterGameTickListener(this.OffsetRenderUpdate, ModContent.HEAVY_GUN_UPDATE_RATE);

                    this.Cooldown            = 0f;
                    this.cooldownUpdateRef ??= this.RegisterGameTickListener(this.CooldownUpdate, ModContent.HEAVY_GUN_UPDATE_RATE);

                    if ((this.ChargeSlot.Itemstack?.Item ?? ammunition) is IPropellant propellant) {
                        
                        Vec3f wind      = this.Api.World.BlockAccessor.GetWindSpeedAt(projectilePos.AsBlockPos).ToVec3f();
                        int smokeAmount = (projectile, propellant.PropellantIsSmokeless) switch {
                            (EntityHighCaliber, false)  => 8,
                            (EntityHighCaliber, true)   => 7,
                            (EntitySmallCaliber, false) => 4,
                            (EntitySmallCaliber, true)  => 2,
                            _                           => 1
                        }; // switch ..

                        int minSmokeOpacity = propellant.PropellantIsSmokeless == true ? 10 : 20;
                        int maxSmokeOpacity = propellant.PropellantIsSmokeless == true ? 40 : 60;
                        
                        for (int i = 0; i < smokeAmount; i ++) {

                            Vec3d position = projectilePos + (direction * i).ToVec3d();
                            float ratio    = (float)i / smokeAmount;

                            this.Api.World.SpawnParticles(
                                quantity         : smokeAmount * 0.5f * (i + 1),
                                color            : ColorUtil.ToRgba((int)(maxSmokeOpacity * (1f - ratio) + minSmokeOpacity), 180, 180, 180),
                                minPos           : position - new Vec3d(i, i, i) * 0.25,
                                maxPos           : position + new Vec3d(i, i, i) * 0.25,
                                minVelocity      : direction * (1f - ratio * 0.5f) * 0.01f + wind * ratio,
                                maxVelocity      : direction * (1f - ratio * 0.5f) * 0.25f + wind * ratio,
                                lifeLength       : 4f * (2f - ratio),
                                gravityEffect    : 0.001f * ratio,
                                scale            : i,
                                dualCallByPlayer : (byEntity as EntityPlayer)?.Player
                            ); // ..
                        } // for ..
                    } // if ..

                    if (ammunition.Casing is Item casing)
                        this.Api.World.SpawnItemEntity(
                            itemstack : new ItemStack(this.Api.World.GetItem(casing.CodeWithVariant("state", random.NextSingle() < ammunition.RecoveryRate ? "fine" : "damaged"))),
                            position  : this.Pos.UpCopy().ToVec3d() + new Vec3d(0.5, 0.5, 0.5),
                            velocity  : new Vec3d(
                                x: GameMath.FastSin(this.Orientation + GameMath.PIHALF * (0.25f + 0.5f * random.NextSingle())),
                                y: 0,
                                z: GameMath.FastCos(this.Orientation + GameMath.PIHALF * (0.25f + 0.5f * random.NextSingle()))
                            ) * 0.1
                        ); // ..

                    this.AmmunitionSlot?.TakeOut(1);
                    if (this.ChargeSlot.Itemstack != null) this.ChargeSlot?.TakeOutWhole();

                    this.Api.World.SpawnEntity(projectile);
                    this.Api.World.PlaySoundAt(
                        location         : new AssetLocation("sounds/effect/mediumexplosion"),
                        posx             : this.Pos.X,
                        posy             : this.Pos.Y,
                        posz             : this.Pos.Z,
                        dualCallByPlayer : (byEntity as EntityPlayer)?.Player,
                        range            : 120
                    ); // ..

                    if (gearedGun != null)
                        gearedGun.Elevation = GameMath.Clamp(
                            gearedGun.Elevation + ((gearedGun?.Behavior.RecoilEffect.nextFloat() - gearedGun?.Behavior.RecoilEffect.avg * 0.95f) * 3f) ?? 0f,
                            gearedGun.Behavior.MinElevation,
                            gearedGun.Behavior.MaxElevation
                        ); // ..

                    this.MarkDirty();

                } // void ..


                /// <summary>
                /// Automatically sets the laying of the gun to hit a given target if conditions are met
                /// </summary>
                /// <param name="target"></param>
                /// <returns></returns>
                public bool TryLay(BlockPos target) {

                    if (this.GetBehavior<BlockEntityBehaviorGearedGun>() is BlockEntityBehaviorGearedGun gearedGun) {

                        float elevation = MathF.Atan(MathF.Asin(this.Pos.DistanceTo(target) / (100f * this.expectedVelocity * this.expectedVelocity)));
                        if (float.IsNaN(elevation)) return false;

                        elevation = MathF.Min(gearedGun.Behavior.MaxElevation,
                            (gearedGun.Behavior.MinElevation > elevation)
                                ? GameMath.PIHALF - elevation
                                : MathF.Max(0, elevation - (1f - elevation) * 0.3f));

                        if (elevation - gearedGun.Behavior.MaxElevation > 0.2f) return false;

                        gearedGun.Elevation = elevation;

                        float dx = this.Pos.X - target.X;
                        float dz = this.Pos.Z - target.Z;
                        this.ChangeOrientation(MathF.Atan2(dx, dz));

                        return true;

                    } else return false;
                } // void ..


                public override void GetBlockInfo(
                    IPlayer       forPlayer,
                    StringBuilder dsc
                ) {

                    if (this.gunState == EnumGunState.Clean || this.gunState == EnumGunState.Ready) {
                        if (!this.AmmunitionSlot.Empty)
                            dsc.AppendLine(Lang.Get(
                                "heavyguninfo-loaded",
                                this.AmmunitionSlot.StackSize,
                                this.AmmunitionSlot.Itemstack.GetName()
                            ));

                        if (!this.ChargeSlot.Empty)
                            dsc.AppendLine(Lang.Get(
                                "heavyguninfo-charged",
                                this.ChargeSlot.StackSize,
                                this.ChargeSlot.Itemstack.GetName()
                            ));

                        if (!this.AmmunitionSlot.Empty || !this.ChargeSlot.Empty)
                            dsc.AppendLine();

                        if (this.blastStrength > 0)
                             dsc.AppendLine(Lang.Get("heavyguninfo-blaststrength", this.blastStrength));
                    } // if ..

                    if (this.GetBehavior<BlockEntityBehaviorGearedGun>().Elevation is float elevation)
                        dsc.AppendLine(Lang.Get("heavyguninfo-elevation", MathF.Round(elevation)));


                    base.GetBlockInfo(forPlayer, dsc);

                } // void ..


                /// <summary>
                /// Called to update the gun's fire cooldown
                /// </summary>
                /// <param name="deltaTime"></param>
                private void CooldownUpdate(float deltaTime) {

                    this.Cooldown += deltaTime;
                    if (this.cooldownUpdateRef is long updateRef)
                        if (this.Cooldown >= this.BlockHeavyGun.CooldownDuration) {

                            this.UnregisterGameTickListener(updateRef);
                            this.cooldownUpdateRef = null;
                            this.Cooldown          = 0f;

                            if (this.offsetRenderingRef is long offsetRenderingRef) {
                                this.offsetRenderingRef = null;
                                this.Offset             = 0f;
                                this.UnregisterGameTickListener(offsetRenderingRef);
                            } // if ..
                        } // if ..
                } // void ..


                /// <summary>
                /// Called to update the gun's client-side offset rendering
                /// </summary>
                /// <param name="deltaTime"></param>
                private void OffsetRenderUpdate(float deltaTime) {
                    if (this.Api.Side.IsClient()
                        && this.BlockHeavyGun.Wheeled
                        && this.Cooldown <= GameMath.PIHALF
                    ) this.Offset = -2f * (MathF.Pow(GameMath.FastSin(this.Cooldown), this.Cooldown) - 1f);
                } // void ..


            //-------------------------------
            // T R E E   A T T R I B U T E S
            //-------------------------------

                public override void FromTreeAttributes(
                    ITreeAttribute tree,
                    IWorldAccessor worldForResolving
                ) {
                    base.FromTreeAttributes(tree, worldForResolving);
                    this.Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
                } // void ..


                public override void ToTreeAttributes(ITreeAttribute tree) {

                    base.ToTreeAttributes(tree);
                    if (this.Inventory != null) {

                        ITreeAttribute inventoryTree = new TreeAttribute();
                        this.Inventory.ToTreeAttributes(inventoryTree);
                        tree["inventory"] = inventoryTree;
                        
                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
