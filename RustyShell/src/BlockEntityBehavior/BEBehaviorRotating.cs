using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace RustyShell {
    public class BlockEntityBehaviorRotating : BlockEntityBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            protected Entity Operator;

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
                private void Update(float deltaTime) {

                    float target             = (this.Operator.SidedPos.Yaw - GameMath.PIHALF) % GameMath.TWOPI;
                    float currentOrientation = this.orientable.Orientation % GameMath.TWOPI;

                    if (currentOrientation < 0)
                        currentOrientation += 2 * MathF.PI;
                    if (target < 0)
                        target += 2 * MathF.PI;

                    float difference = target - currentOrientation;

                    if (difference > MathF.PI)
                        difference -= 2 * MathF.PI;
                    else if (difference < -MathF.PI)
                        difference += 2 * MathF.PI;

                    this.orientable.ChangeOrientation(this.orientable.Orientation + (float.IsPositive(difference)
                        ? GameMath.Min(MathF.Sign(difference) * this.behavior.TurnSpeed * deltaTime, difference)
                        : GameMath.Max(MathF.Sign(difference) * this.behavior.TurnSpeed * deltaTime, difference)));
                } // void ..


                /// <summary>
                /// Called to start the block's rotation listener if it doesn't already exist
                /// </summary>
                public void TryStartUpdate(Entity byEntity) {
                    this.Operator  ??= byEntity;
                    this.updateRef ??= this.Blockentity.RegisterGameTickListener(this.Update, ModContent.HEAVY_GUN_UPDATE_RATE);
                } // void ..


                /// <summary>
                /// Called to end the block's rotation if it was already started
                /// </summary>
                public void TryEndUpdate() {
                    this.Operator = null;
                    if (this.updateRef.HasValue) {
                        this.Blockentity.UnregisterGameTickListener(this.updateRef.Value);
                        this.updateRef = null;
                        this.Blockentity.MarkDirty();
                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
