using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent.Mechanics;

namespace RustyShell {
    public class BlockEntityBehaviorRotating : BlockEntityBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Rotation direction </summary> **/ internal EnumRotDirection? Movement;

            /** <summary> Reference to the rotation update listener </summary> **/ private long? updateRef;

            /** <summary> Reference to the orientable renderer </summary> **/ private OrientableRenderer renderer;

            /** <summary> Reference to the wheel behavior </summary> **/               protected readonly BlockBehaviorWheeled behavior;
            /** <summary> Reference to the block's orientable interface </summary> **/ protected readonly IOrientable orientable;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockEntityBehaviorRotating(BlockEntity blockEntity) : base(blockEntity) {

                this.behavior   = this.Block.GetBehavior<BlockBehaviorWheeled>();
                this.orientable = blockEntity as IOrientable;

            } // BlockEntityBehaviorRotating ..


            public override void Initialize(
                ICoreAPI api,
                JsonObject properties
            ) {

                base.Initialize(api, properties);
                if (this.Api is ICoreClientAPI client) {

                    this.renderer = new OrientableRenderer(client, this.behavior.BaseMesh, this.Blockentity, this.Pos);
                    client.Event.RegisterRenderer(this.renderer, EnumRenderStage.Opaque, "orientable");

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
                /// Called every `HEAVY_GUN_UPDATE_RATE` milliseconds to update the block's rotation
                /// </summary>
                /// <param name="deltaTime"></param>
                private void Update(float deltaTime) =>
                    this.orientable.ChangeOrientation(this.orientable.Orientation + (float)this.Movement.Sign() * this.behavior.TurnSpeed * deltaTime);

                /// <summary>
                /// Called to start the block's rotation listener if it doesn't already exist
                /// </summary>
                public void TryStartUpdate() =>
                    this.updateRef ??= this.Blockentity.RegisterGameTickListener(this.Update, ModContent.HEAVY_GUN_UPDATE_RATE);


                /// <summary>
                /// Called to end the block's rotation if it was already started
                /// </summary>
                public void TryEndUpdate() {
                    this.Movement = null;
                    if (this.updateRef.HasValue) {
                        this.Blockentity.UnregisterGameTickListener(this.updateRef.Value);
                        this.updateRef = null;
                        this.Blockentity.MarkDirty();
                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
