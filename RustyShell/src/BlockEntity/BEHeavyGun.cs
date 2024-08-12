using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace RustyShell {
    public class BlockEntityHeavyGun : BlockEntityOrientable {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> A reference to the heavy gun block </summary> **/     public BlockHeavyGun BlockHeavyGun { get; private set; }
            /** <summary> A reference to muzzle loading behavior </summary> **/ protected BlockEntityBehaviorMuzzleLoading muzzleLoading;
            
            /** <summary> Remaining time until the gun can be fired </summary> **/ public float Cooldown { get; private set; }
            /** <summary> A reference to the cooldown listener </summary> **/      private long? cooldownUpdateRef;

            /// <summary>
            /// Projectile velocity in block per second at spawn
            /// </summary>
            private float blastStrength =>
                this.BlockHeavyGun.BarrelType switch {
                    EnumBarrelType.Smoothbore => this.BlockHeavyGun.FirePower * GameMath.Sqrt(this.FusedAmmunition switch { true => 8, false => this.DetonatorSlot.StackSize } * 0.2f),
                    EnumBarrelType.Rifled     => this.BlockHeavyGun.FirePower,
                    _                         => 1f,
                }; // swtich ..

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
            /** <summary> A reference to the detonator dedicated slot  </summary> **/ internal ItemSlot         DetonatorSlot  => this.Inventory[0];
            /** <summary> A reference to the ammunition dedicated slot </summary> **/ internal ItemSlot         AmmunitionSlot => this.Inventory[1];

            /** <summary> Indicates whether or the gun is waiting to be cleaned </summary> **/ public bool CanClean => this.gunState == EnumGunState.Dirty;
            /** <summary> Indicates whether or the gun is waiting to be filled </summary> **/  public bool CanFill  => this.gunState == EnumGunState.Clean || !this.BlockHeavyGun.MuzzleLoading;
            /** <summary> Indicates whether or the gun is waiting to be loaded </summary> **/  public bool CanLoad  => this.gunState == EnumGunState.Clean && !this.AmmunitionSlot.Empty && (this.FusedAmmunition || !this.DetonatorSlot.Empty);
            /** <summary> Indicates whether or the gun is waiting to be fired </summary> **/   public bool CanFire  => this.gunState == EnumGunState.Ready;

            /** <summary> Indicates whether or the loaded ammunition needs a detonator </summary> **/ public bool FusedAmmunition => this.AmmunitionSlot?.Itemstack?.ItemAttributes["hasFuse"]?.AsBool(false) ?? false;

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

                    string entityCode = this.AmmunitionSlot?
                        .Itemstack?
                        .ItemAttributes["entityCode"]?
                        .AsString();

                    EntityProperties type = this.Api.World.GetEntityType(entityCode == null ? this.AmmunitionSlot.Itemstack.Item.Code : new AssetLocation(entityCode));
                    Entity projectile     = this.Api.World.ClassRegistry.CreateEntity(type);

                    ThreadSafeRandom random = new();
                    float randPitch = (random.NextSingle() - 0.5f) * (1f - this.BlockHeavyGun.Accuracy) * 0.5f;
                    float randYaw   = (random.NextSingle() - 0.5f) * (1f - this.BlockHeavyGun.Accuracy) * 0.5f;

                    float elevation = this.GetBehavior<BlockEntityBehaviorGearedGun>()?.Elevation ?? 0f;

                    float cosPitch = GameMath.FastCos(MathF.Atan(elevation) + randPitch);
                    float sinPitch = GameMath.FastSin(MathF.Atan(elevation) + randPitch);
                    float cosYaw   = GameMath.FastCos(this.Orientation + randYaw);
                    float sinYaw   = GameMath.FastSin(this.Orientation + randYaw);

                    Vec3f direction     = new Vec3f(-cosPitch * sinYaw, sinPitch, -cosPitch * cosYaw).Normalize();
                    Vec3d projectilePos = (new Vec3f(this.Pos.X + 0.5f, this.Pos.Y + 0.5f, this.Pos.Z + 0.5f) + direction * this.BlockHeavyGun.BarrelLength).ToVec3d();
                    Vec3f velocity      = direction * this.blastStrength;

                    this.gunState = this.BlockHeavyGun.MuzzleLoading
                        ? EnumGunState.Dirty
                        : this.AmmunitionSlot.Empty
                            ? EnumGunState.Clean
                            : EnumGunState.Ready;

                    projectile.ServerPos.SetPos(projectilePos);
                    projectile.ServerPos.Motion.Set(velocity);
                    projectile.Pos.SetFrom(projectile.ServerPos);


                    if (projectile is EntityHighCaliber highCaliber) {

                        highCaliber.FiredBy      = byEntity;
                        highCaliber.FuseDuration = (int)(1000 * (((this.BlockHeavyGun.BarrelType == EnumBarrelType.Rifled)? 32f : 24f) / this.BlockHeavyGun.FirePower)
                        * (this.AmmunitionSlot
                                .Itemstack
                                .ItemAttributes["hasFuse"]?
                                .AsBool(false) ?? false ? 1f : 0.75f)
                        * highCaliber.BlastType switch {
                            EnumExplosionType.HighExplosive => 4f,
                            EnumExplosionType.Canister      => 2f,
                            EnumExplosionType.Simple        => 2f,
                            _                               => 1f
                        });

                        this.cooldownUpdateRef = this.RegisterGameTickListener(this.CooldownUpdate, ModContent.HEAVY_GUN_UPDATE_RATE);

                    } else if (projectile is EntitySmallCaliber smallCaliber) {

                        smallCaliber.FiredBy    = byEntity;
                        smallCaliber.ImpactSize = 0;

                    } // if ..


                    if (this.Api.Side.IsClient()) {

                        int smokeAmount = 0;
                        if (this.DetonatorSlot.Itemstack != null) smokeAmount = this.DetonatorSlot.StackSize;
                        if (this.AmmunitionSlot.Itemstack.ItemAttributes["hasFuse"].AsBool()) smokeAmount += 2;

                        for (int i = 0; i < smokeAmount >> (projectile is EntitySmallCaliber ? 1 : 0); i ++)
                            this.Api.World.SpawnParticles(new ExplosionSmokeParticles() {
                                basePos              = projectilePos,
                                ParentVelocityWeight = 1f,
                                ParentVelocity       = GlobalConstants.CurrentWindSpeedClient,
                            }); // ..
                    } // if ..

                    this.AmmunitionSlot?.TakeOut(1);
                    if (this.DetonatorSlot.Itemstack != null) this.DetonatorSlot?.TakeOutWhole();

                    this.Api.World.SpawnEntity(projectile);
                    this.Api.World.PlaySoundAt(new AssetLocation("sounds/effect/mediumexplosion"), this.Pos.X, this.Pos.Y, this.Pos.Z, null, false, 120);

                    this.MarkDirty();

                } // void ..


                /// <summary>
                /// Automatically sets the laying of the gun to hit a given target if conditions are met
                /// </summary>
                /// <param name="target"></param>
                /// <returns></returns>
                public bool TryLay(BlockPos target) {

                    if (this.GetBehavior<BlockEntityBehaviorGearedGun>() is BlockEntityBehaviorGearedGun gearedGun) {

                        float elevation = MathF.Atan(MathF.Asin(this.Pos.DistanceTo(target) / (100f * this.blastStrength * this.blastStrength)));
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

                    if (this.gunState == EnumGunState.Clean || this.gunState == EnumGunState.Ready)
                        dsc.AppendLine((!this.AmmunitionSlot.Empty, !this.DetonatorSlot.Empty) switch {
                            (true, false) => string.Format(
                                "Loaded with {0}x {1}",
                                this.AmmunitionSlot.StackSize,
                                this.AmmunitionSlot.Itemstack.GetName()
                            ), // ..
                            (false, true) => string.Format(
                                "Loaded with {0}x {1}",
                                this.DetonatorSlot.StackSize,
                                this.DetonatorSlot.Itemstack.GetName()
                            ), // ..
                            (true, true) => string.Format(
                                "Loaded with {0}x {1} and {2}x {3}",
                                this.AmmunitionSlot.StackSize,
                                this.AmmunitionSlot.Itemstack.GetName(),
                                this.DetonatorSlot.StackSize,
                                this.DetonatorSlot.Itemstack.GetName()
                            ), // ..
                            _ => "Empty",
                        }); // switch ..


                    base.GetBlockInfo(forPlayer, dsc);

                } // void ..


                /// <summary>
                /// Called to update the gun's fire cooldown and client-side offset rendering
                /// </summary>
                /// <param name="deltaTime"></param>
                private void CooldownUpdate(float deltaTime) {

                    this.Cooldown += deltaTime;

                    if (this.Api.Side.IsClient()
                        && this.BlockHeavyGun.Wheeled
                        && this.Cooldown <= GameMath.PIHALF
                    ) this.Offset = -2f * (MathF.Pow(GameMath.FastSin(this.Cooldown), this.Cooldown) - 1f);

                    if (this.cooldownUpdateRef is long updateRef)
                        if (this.Cooldown >= this.BlockHeavyGun.CooldownDuration) {
                            this.UnregisterGameTickListener(updateRef);
                            this.cooldownUpdateRef = null;
                            this.Cooldown          = 0f;
                            this.Offset            = 0f;
                        } // if ..
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
