using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent.Mechanics;


namespace RustyShell {
    public class BlockBehaviorRotating : BlockBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Rotation in radian per second </summary> **/        internal float TurnSpeed;
            /** <summary> A reference to the block's base mesh </summary> **/ internal MeshData BaseMesh;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockBehaviorRotating(Block block) : base(block) {}

            public override void Initialize(JsonObject properties) {

                base.Initialize(properties);
                this.TurnSpeed = properties["turnSpeed"].AsFloat(0.2f);

            } // void ..


            public override void OnLoaded(ICoreAPI api) {
                base.OnLoaded(api);
                if (api is ICoreClientAPI client)
                    client.Tesselator.TesselateShape(this.block, client.TesselatorManager.GetCachedShape(this.block.Shape.Base), out this.BaseMesh);
                    
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                /// <summary>
                /// Indicates if a given player can interact with the rotation behavior
                /// </summary>
                /// <param name="byPlayer"></param>
                /// <returns></returns>
                private bool CanInteract(IPlayer byPlayer) => byPlayer.Entity.Controls.Sneak
                    && byPlayer.Entity.ActiveHandItemSlot.Empty;


                public override WorldInteraction[] GetPlacedBlockInteractionHelp(
                    IWorldAccessor world, 
                    BlockSelection selection,
                    IPlayer forPlayer,
                    ref EnumHandling handled
                ) { return new WorldInteraction[] {

                        new () {
                            ActionLangCode  = "blockhelp-wheeled-turnleft",
                            MouseButton     = EnumMouseButton.Right,
                            HotKeyCode      = "shift",
                        }, // new ..
                        new () {
                            ActionLangCode  = "blockhelp-wheeled-turnright",
                            MouseButton     = EnumMouseButton.Right,
                            HotKeyCodes     = new string[2] { "ctrl", "shift" },
                        }, // new ..

                    }; // return ..
                } // WorldInteraction[] ..


                public override bool OnBlockInteractStart(
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;
                    if (this.CanInteract(byPlayer))
                        world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorRotating>()?
                            .TryStartUpdate();

                    return true;

                } // bool ..


                public override bool OnBlockInteractStep(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    if (this.CanInteract(byPlayer)) {

                        handling = EnumHandling.PreventSubsequent;
                        if (world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorRotating>() is BlockEntityBehaviorRotating behavior
                        ) behavior.Movement = byPlayer.Entity.Controls.CtrlKey ? EnumRotDirection.Clockwise : EnumRotDirection.Counterclockwise;

                    } else {

                        handling = EnumHandling.PreventDefault;
                        world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorRotating>()?
                            .TryEndUpdate();

                    } // if ..


                    return true;

                } // bool ..


                public override void OnBlockInteractStop(
                    float secondsUsed,
                    IWorldAccessor world, 
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;
                    world.BlockAccessor
                        .GetBlockEntity(blockSel.Position)?
                        .GetBehavior<BlockEntityBehaviorRotating>()?
                        .TryEndUpdate();

                } // void ..


                public override bool OnBlockInteractCancel(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;
                    world.BlockAccessor
                        .GetBlockEntity(blockSel.Position)?
                        .GetBehavior<BlockEntityBehaviorRotating>()?
                        .TryEndUpdate();

                    return true;

                } // bool ..
    } // class ..
} // namespace ..
