using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent.Mechanics;

namespace RustyShell {
    public class BlockEntityBehaviorGearedGun : BlockEntityBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Tangent theta elevation </summary> **/ internal float Elevation;
            /** <summary> ELevation direction </summary> **/     protected EnumRotDirection? movement = EnumRotDirection.Clockwise;

            /** <summary> Reference to the elevation update listener </summary> **/ private long? updateRef;

            /** <summary> Reference to the barrel renderer </summary> **/          private           GearedGunRenderer renderer;
            /** <summary> Reference to the gear behavior </summary> **/            internal readonly BlockBehaviorGearedGun Behavior;
            /** <summary> Reference to the heavy gun block instance </summary> **/ private  readonly BlockEntityHeavyGun blockEntityHeavyGun;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockEntityBehaviorGearedGun(BlockEntity blockEntity) : base(blockEntity) {

                this.Behavior            = this.Block.GetBehavior<BlockBehaviorGearedGun>();
                this.blockEntityHeavyGun = blockEntity as BlockEntityHeavyGun;
                this.Elevation           = this.Behavior.MinElevation;
                this.blockEntityHeavyGun.MarkDirty(true);

            } // BlockEntityBehaviorGearedGun ..


            public override void Initialize(
                ICoreAPI api,
                JsonObject properties
            ) {

                base.Initialize(api, properties);
                if (this.Api is ICoreClientAPI client) {

                    this.renderer = new GearedGunRenderer(client, this.Behavior.BarrelMesh, this.Blockentity, this.Blockentity.Pos, this.Behavior.BarrelAnchor);
                    
                    client.Event.RegisterRenderer(
                        this.renderer,
                        EnumRenderStage.Opaque,
                        "gearedgun"
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
            /// Called to update the gun elevation
            /// </summary>
            /// <param name="deltaTime"></param>
            private void Update(float deltaTime) {

                this.Elevation += this.movement.Sign() * this.Behavior.LayingSpeed * deltaTime;
                if (this.Elevation > this.Behavior.MaxElevation) this.movement = EnumRotDirection.Counterclockwise;
                if (this.Elevation < this.Behavior.MinElevation) this.movement = EnumRotDirection.Clockwise;

            } // void ..


            /// <summary>
            /// Tries to start gun elevation
            /// </summary>
            public void TryStartUpdate() {

                if (this.Elevation > this.Behavior.MinElevation) this.movement = EnumRotDirection.Clockwise;
                else                                             this.movement = EnumRotDirection.Counterclockwise;

                this.updateRef ??= this.blockEntityHeavyGun.RegisterGameTickListener(this.Update, ModContent.HEAVY_GUN_UPDATE_RATE);
                this.Blockentity.MarkDirty();

            } // void ..


            /// <summary>
            /// Stops gun elevation if already started
            /// </summary>
            public void TryEndUpdate() {

                this.movement = null;
                if (this.updateRef.HasValue) {
                    
                    this.blockEntityHeavyGun.UnregisterGameTickListener(this.updateRef.Value);
                    this.updateRef = null;
                    this.Blockentity.MarkDirty();
                    
                } // if ..
            } // void ..


            //-------------------------------
            // T R E E   A T T R I B U T E S
            //-------------------------------

                public override void FromTreeAttributes(
                    ITreeAttribute tree,
                    IWorldAccessor worldForResolving
                ) {

                    this.Elevation = tree.GetFloat("elevation", this.Elevation);
                    this.movement  = tree.GetInt("elevationDirection", this.movement.Sign()) switch {
                         1 => EnumRotDirection.Clockwise,
                        -1 => EnumRotDirection.Counterclockwise,
                         _ => null,
                    }; // switch ..
                    base.FromTreeAttributes(tree, worldForResolving);

                } // void ..


                public override void ToTreeAttributes(ITreeAttribute tree) {

                    tree.SetFloat("elevation", this.Elevation);
                    tree.SetInt("elevationDirection", this.movement.Sign());
                    base.ToTreeAttributes(tree);

                } // void ..
    } // class ..
} // namespace ..
