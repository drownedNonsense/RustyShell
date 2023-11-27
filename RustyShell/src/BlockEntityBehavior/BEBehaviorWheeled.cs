using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent.Mechanics;

namespace RustyShell {
    public class BlockEntityBehaviorWheeled : BlockEntityBehaviorRotating {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Reference to both wheels' renderer </summary> **/ private (WheelRenderer, WheelRenderer) renderers;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockEntityBehaviorWheeled(BlockEntity blockEntity) : base(blockEntity) {}


            public override void Initialize(
                ICoreAPI api,
                JsonObject properties
            ) {

                base.Initialize(api, properties);
                if (this.Api is ICoreClientAPI client) {

                    this.renderers.Item1 = new WheelRenderer(client, this.behavior.WheelMesh, this.Blockentity, this.Blockentity.Pos, this.behavior.WheelAnchors.Item1, EnumRotDirection.Counterclockwise);
                    this.renderers.Item2 = new WheelRenderer(client, this.behavior.WheelMesh, this.Blockentity, this.Blockentity.Pos, this.behavior.WheelAnchors.Item2, EnumRotDirection.Clockwise);
                    
                    client.Event.RegisterRenderer(
                        this.renderers.Item1,
                        EnumRenderStage.Opaque,
                        "wheel"
                    ); // ..
                    client.Event.RegisterRenderer(
                        this.renderers.Item2,
                        EnumRenderStage.Opaque,
                        "wheel"
                    ); // ..
                } // if
            } // void ..


            public override void OnBlockUnloaded() {

                base.OnBlockUnloaded();
                this.renderers.Item1?.Dispose();
                this.renderers.Item2?.Dispose();

            } // void ..


            public override void OnBlockBroken(IPlayer byPlayer = null) {

                base.OnBlockBroken(byPlayer);
                this.renderers.Item1?.Dispose();
                this.renderers.Item2?.Dispose();

            } // void ..


            public override void OnBlockRemoved() {

                base.OnBlockRemoved();
                this.renderers.Item1?.Dispose();
                this.renderers.Item2?.Dispose();

            } // void ..
    } // class ..
} // namespace ..
