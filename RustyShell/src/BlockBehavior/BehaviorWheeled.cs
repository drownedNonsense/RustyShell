using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;


namespace RustyShell {
    public class BlockBehaviorWheeled : BlockBehaviorRotating {

        //=======================
        // D E F I N I T I O N S
        //=======================

            internal MeshData       WheelMesh;
            internal Vec3f          WheelOrigin;
            internal (Vec3f, Vec3f) WheelAnchors;

            private string wheelShapePath;
            private string wheelOriginElementCode;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockBehaviorWheeled(Block block) : base(block) {}

            public override void Initialize(JsonObject properties) {

                base.Initialize(properties);
                this.wheelShapePath         = properties["wheelShapePath"].AsString("shapes/block/wood/wheel/spoked");
                this.wheelOriginElementCode = properties["wheelOriginElementCode"].AsString("RimMover");

            } // void ..


            public override void OnLoaded(ICoreAPI api) {
                base.OnLoaded(api);
                this.InitRenderer(api.World, this.wheelShapePath, this.wheelOriginElementCode);
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------
            // R E N D E R I N G
            //-------------------

                private void InitRenderer(
                    IWorldAccessor world,
                    string wheelShapePath,
                    string wheelOriginElementCode,
                    string variant1 = "Left",
                    string variant2 = "Right"
                ) {

                    if (world.Api.Side.IsClient()) {

                        Shape shape = (world.Api as ICoreClientAPI)
                            .TesselatorManager
                            .GetCachedShape(this.block.Shape.Base);

                        ITesselatorAPI mesher        = ((ICoreClientAPI)world.Api).Tesselator;
                        ShapeElement[] wheelElements = new ShapeElement[2] {
                            shape.GetElementByName(wheelOriginElementCode + variant1),
                            shape.GetElementByName(wheelOriginElementCode + variant2)
                        }; // ..

                        Shape        wheel              = Shape.TryGet(world.Api, wheelShapePath + ".json");
                        ShapeElement wheelOriginElement = wheel.GetElementByName(wheelOriginElementCode);

                        mesher.TesselateShape(this.block, wheel, out this.WheelMesh);

                        this.WheelOrigin = new Vec3f(
                            (float)wheelOriginElement.RotationOrigin[0],
                            (float)wheelOriginElement.RotationOrigin[1],
                            (float)wheelOriginElement.RotationOrigin[2]
                        ) * 0.0625f;

                        this.WheelAnchors = (
                            new Vec3f(
                                (float)wheelElements[0].RotationOrigin[0],
                                (float)wheelElements[0].RotationOrigin[1],
                                (float)wheelElements[0].RotationOrigin[2]
                            ) * 0.0625f,
                            new Vec3f(
                                (float)wheelElements[1].RotationOrigin[0],
                                (float)wheelElements[1].RotationOrigin[1],
                                (float)wheelElements[1].RotationOrigin[2]
                            ) * 0.0625f
                        ); // ..
                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
