using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent.Mechanics;


namespace RustyShell {
    public class BlockEntityBehaviorRepeatingFire : BlockEntityBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Rotating barrel angle </summary> **/             internal float Angle;
            /** <summary> Rotating barrel rotation direction</summary> **/ protected EnumRotDirection? movement = EnumRotDirection.Clockwise;

            /** <summary> Reference to the rotation update listener </summary> **/ private long? updateRef;
            /** <summary> Reference to the firing listener </summary> **/          private long? fireRef;

            /** <summary> Reference to the source entity </summary> **/ protected Entity firingEntity;

            /** <summary> Reference to the rotating barrel renderer <summmary> **/ private            RotatingBarrelRenderer renderer;
            /** <summary> Reference to the repeating fire behavior <summmary> **/  protected readonly BlockBehaviorRepeatingFire behavior;
            /** <summary> Reference to the heavy gun block instance <summmary> **/ protected readonly BlockEntityHeavyGun blockEntityHeavyGun;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockEntityBehaviorRepeatingFire(BlockEntity blockEntity) : base(blockEntity) {

                this.behavior            = this.Block.GetBehavior<BlockBehaviorRepeatingFire>();
                this.blockEntityHeavyGun = blockEntity as BlockEntityHeavyGun;
                this.blockEntityHeavyGun.MarkDirty(true);

            } // BlockEntityBehaviorRepeatingFire ..


            public override void Initialize(ICoreAPI api, JsonObject properties) {
                base.Initialize(api, properties);
                if (this.Api is ICoreClientAPI client) {

                    this.renderer = new RotatingBarrelRenderer(
                        coreClientAPI : client,
                        mesh          : this.behavior.RotatingBarrelMesh,
                        blockEntity   : this.Blockentity,
                        pos           : this.Blockentity.Pos,
                        gearAnchor    : this.behavior.BarrelAnchor,
                        barrelAnchor  : this.behavior.BarrelOrigin
                    ); // ..
                    
                    client.Event.RegisterRenderer(
                        this.renderer,
                        EnumRenderStage.Opaque,
                        "rotatingGun"
                    ); // ..
                } // if
            } // void ..



            public override void OnBlockUnloaded() {

                base.OnBlockUnloaded();
                this.renderer?.Dispose();

            } // void ..


            public override void OnBlockBroken(IPlayer byPlayer = null) {

                base.OnBlockBroken(byPlayer);
                this.renderer?.Dispose();

            } // void ..


            public override void OnBlockRemoved() {

                base.OnBlockRemoved();
                this.renderer?.Dispose();

            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            /// <summary>
            /// Called to update the rotating barrel angle
            /// </summary>
            /// <param name="deltaTime"></param>
            private void Update(float deltaTime) =>
                this.Angle += this.movement.Sign() * GameMath.TWOPI * this.behavior.FireInterval * deltaTime;

            /// <summary>
            /// Called to fire the gun based on the block behavior's fire interval
            /// </summary>
            /// <param name="deltaTime"></param>
            private void Fire(float deltaTime) =>
                this.blockEntityHeavyGun.Fire(this.firingEntity);

            /// <summary>
            /// Tries to start firing
            /// </summary>
            /// <param name="byEntity"></param>
            public void TryStartFire(Entity byEntity) {
                this.firingEntity = byEntity;
                this.movement     = EnumRotDirection.Clockwise;
                this.updateRef ??= this.Blockentity.RegisterGameTickListener(this.Update, ModContent.HEAVY_GUN_UPDATE_RATE);
                this.fireRef   ??= this.Blockentity.RegisterGameTickListener(this.Fire, (int)(this.behavior.FireInterval * 1000));
                this.Blockentity.MarkDirty();
            } // void ..

            /// <summary>
            /// Stops firing if the firing already started
            /// </summary>
            public void TryEndFire() {
                this.firingEntity = null;
                this.movement     = null;
                if (this.updateRef.HasValue) { this.Blockentity.UnregisterGameTickListener(this.updateRef.Value); this.updateRef = null; }
                if (this.fireRef.HasValue)   { this.Blockentity.UnregisterGameTickListener(this.fireRef.Value);   this.fireRef = null; }
            } // void ..


            //-------------------------------
            // T R E E   A T T R I B U T E S
            //-------------------------------

                public override void FromTreeAttributes(
                    ITreeAttribute tree,
                    IWorldAccessor worldForResolving
                ) {

                    this.movement = tree.GetInt("rotatingGunDirection", this.movement.Sign()) switch {
                         1 => EnumRotDirection.Clockwise,
                         _ => null,
                    }; // switch ..
                    base.FromTreeAttributes(tree, worldForResolving);

                } // void ..


                public override void ToTreeAttributes(ITreeAttribute tree) {

                    tree.SetInt("elevationDirection", this.movement.Sign());
                    base.ToTreeAttributes(tree);

                } // void ..
    } // class ..
} // namespace ..
