using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;


namespace RustyShell {
    public class BlockBehaviorRepeatingFire : BlockBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Interval between each fire in second </summary> **/ internal float FireInterval;

            /** <summary> Loaded rotating barrel mesh </summary> **/          internal MeshData RotatingBarrelMesh;
            /** <summary> Rotating barrel mesh origin point </summary> **/    internal Vec3f    BarrelOrigin;
            /** <summary> Rotating barrel mesh rotation anchor </summary> **/ internal Vec3f    BarrelAnchor;

            /** <summary> Mod path toward the rotating barrel shape file </summary> **/ private string rotatingBarrelShapePath;
            /** <summary> Name of the rotating barrel shape root element </summary> **/ private string rotatingBarrelOriginElementCode;
            /** <summary> Name of the barrel shape root element </summary> **/          private string barrelOriginElementCode;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockBehaviorRepeatingFire(Block block) : base(block) {}


            public override void Initialize(JsonObject properties) {

                base.Initialize(properties);
                this.FireInterval = properties["fireInterval"].AsFloat(0.1f);
                this.rotatingBarrelShapePath         = properties["rotatingBarrelShapePath"].AsString();
                this.rotatingBarrelOriginElementCode = properties["rotatingBarrelOriginElementCode"].AsString("RotatingGun");
                this.barrelOriginElementCode         = properties["barrelOriginElementCode"].AsString("Gun");

            } // void ..


            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);
                this.InitRenderer(
                    api.World,
                    this.rotatingBarrelShapePath,
                    this.rotatingBarrelOriginElementCode,
                    this.barrelOriginElementCode
                ); // ..
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                /// <summary>
                /// Indicates if a given player can interact with the repeated fire behavior
                /// </summary>
                /// <param name="byPlayer"></param>
                /// <returns></returns>
                private static bool CanInteract(IPlayer byPlayer) => byPlayer.Entity.Controls.Sprint && byPlayer.Entity.Controls.Sneak;


                public override WorldInteraction[] GetPlacedBlockInteractionHelp(
                    IWorldAccessor world, 
                    BlockSelection selection,
                    IPlayer forPlayer,
                    ref EnumHandling handled
                ) => new WorldInteraction[] {
                        new () {
                            ActionLangCode  = "blockhelp-repeatingfire-fire",
                            MouseButton     = EnumMouseButton.Right,
                            HotKeyCodes     = new string[] {"ctrl", "shift"},
                        }, // WorldInteraction ..
                    }; // WorldInteraction[] ..


                public override bool OnBlockInteractStart(
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;
                    if (CanInteract(byPlayer))
                        world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorRepeatingFire>()?
                            .TryStartFire(byPlayer.Entity);

                    return true;

                } // bool ..


                public override bool OnBlockInteractStep(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    if (CanInteract(byPlayer)) handling = EnumHandling.PreventSubsequent;
                    else {
                        
                        handling = EnumHandling.PreventDefault;
                        world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorRepeatingFire>()?
                            .TryEndFire();

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
                        .GetBehavior<BlockEntityBehaviorRepeatingFire>()?
                        .TryEndFire();

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
                        .GetBehavior<BlockEntityBehaviorRepeatingFire>()?
                        .TryEndFire();
                        
                    return true;

                } // bool ..


            //-------------------
            // R E N D E R I N G
            //-------------------

                /// <summary>
                /// Initialize a rotating barrel renderer
                /// </summary>
                /// <param name="world"></param>
                /// <param name="rotatingBarrelShapePath"></param>
                /// <param name="rotatingBarrelOriginElementCode"></param>
                /// <param name="barrelOriginElementCode"></param>
                private void InitRenderer(
                    IWorldAccessor world,
                    string rotatingBarrelShapePath,
                    string rotatingBarrelOriginElementCode,
                    string barrelOriginElementCode
                ) {

                    if (world.Side.IsClient()) {

                        Shape shape = (world.Api as ICoreClientAPI)
                            .TesselatorManager
                            .GetCachedShape(this.block.Shape.Base);

                        ITesselatorAPI mesher            = ((ICoreClientAPI)world.Api).Tesselator;
                        ShapeElement barrelElement       = shape.GetElementByName(barrelOriginElementCode);
                        Shape        rotatingBarrel      = Shape.TryGet(world.Api, rotatingBarrelShapePath + ".json");
                        ShapeElement barrelOriginElement = rotatingBarrel.GetElementByName(rotatingBarrelOriginElementCode);

                        mesher.TesselateShape(this.block, rotatingBarrel, out this.RotatingBarrelMesh);

                        this.BarrelOrigin = new Vec3f(
                            (float)barrelOriginElement.RotationOrigin[0],
                            (float)barrelOriginElement.RotationOrigin[1],
                            (float)barrelOriginElement.RotationOrigin[2]
                        ) * 0.0625f;

                        this.BarrelAnchor = new Vec3f(
                            (float)barrelElement.RotationOrigin[0],
                            (float)barrelElement.RotationOrigin[1],
                            (float)barrelElement.RotationOrigin[2]
                        ) * 0.0625f;
                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
