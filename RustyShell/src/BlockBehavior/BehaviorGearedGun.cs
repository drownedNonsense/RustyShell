using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RustyShell {
    public class BlockBehaviorGearedGun : BlockBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            private static ItemStack[] WrenchStacks;

            /** <summary> Recoil effect </summary> **/             public NatFloat RecoilEffect { get; private set; }
            /** <summary> Minimum tangent elevation </summary> **/ public float MinElevation    { get; private set; }
            /** <summary> Maximum tangent elevation </summary> **/ public float MaxElevation    { get; private set; }

            /** <summary> Loaded barrel mesh </summary> **/          internal MeshData BarrelMesh;
            /** <summary> Barrel mesh origin point </summary> **/    internal Vec3f    BarrelOrigin;
            /** <summary> Barrel mesh rotation anchor </summary> **/ internal Vec3f    BarrelAnchor;

            /** <summary> Mod path toward the barrel shape file </summary> **/ private string barrelShapePath;
            /** <summary> Name of the barrel shape root element </summary> **/ private string barrelOriginElementCode;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockBehaviorGearedGun(Block block) : base(block) {}

            public override void Initialize(JsonObject properties) {

                base.Initialize(properties);
                this.RecoilEffect = new NatFloat(properties["recoilEffect"]["avg"].AsFloat(), properties["recoilEffect"]["var"].AsFloat(), EnumDistribution.UNIFORM); 
                this.MinElevation = properties["minElevation"].AsFloat(0f);
                this.MaxElevation = properties["maxElevation"].AsFloat(1f);
                this.barrelShapePath         = properties["barrelShapePath"].AsString();
                this.barrelOriginElementCode = properties["barrelOriginElementCode"].AsString("Gun");

            } // void ..


            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);
                this.InitRenderer(
                    api.World,
                    this.barrelShapePath,
                    this.barrelOriginElementCode
                ); // ..

                if (api is ICoreClientAPI client)
                    BlockBehaviorGearedGun.WrenchStacks = ObjectCacheUtil.GetOrCreate(client, "wrenchStacks", delegate {

                        List<ItemStack> wrenchStacks = new ();
                        foreach (Item item in api.World.Items)
                            if (item is ItemWrench && item.Code != null)
                                wrenchStacks.AddRange(item.GetHandBookStacks(client));

                        return wrenchStacks.ToArray();
                    }); // ..
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                /// <summary>
                /// Indicates if a given player can interact with the gear behavior
                /// </summary>
                /// <param name="byPlayer"></param>
                /// <returns></returns>
                private static bool CanInteract(IPlayer byPlayer) => byPlayer.Entity.ActiveHandItemSlot.Itemstack?.Item is ItemWrench;


                public override WorldInteraction[] GetPlacedBlockInteractionHelp(
                    IWorldAccessor world, 
                    BlockSelection selection,
                    IPlayer forPlayer,
                    ref EnumHandling handled
                ) => new WorldInteraction[] {

                        new () {
                            ActionLangCode = "blockhelp-gearedgun-lay-up",
                            MouseButton    = EnumMouseButton.Right,
                            Itemstacks     = BlockBehaviorGearedGun.WrenchStacks
                        }, // WorldInteraction ..
                        new () {
                            ActionLangCode = "blockhelp-gearedgun-lay-down",
                            MouseButton    = EnumMouseButton.Right,
                            Itemstacks     = BlockBehaviorGearedGun.WrenchStacks,
                            HotKeyCodes    = new string[1] { "ctrl" }
                        }, // WorldInteraction ..

                    }; // WorldInteraction[] ..


                public override bool OnBlockInteractStart(
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    handling = EnumHandling.PreventDefault;
                    if (CanInteract(byPlayer) && world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorGearedGun>() is BlockEntityBehaviorGearedGun behavior
                    ) {

                        behavior.Movement = Vintagestory.GameContent.Mechanics.EnumRotDirection.Clockwise;
                        behavior.TryStartUpdate();

                    } // if ..

                    return true;

                } // bool ..


                public override bool OnBlockInteractStep(
                    float secondsUsed,
                    IWorldAccessor world,
                    IPlayer byPlayer,
                    BlockSelection blockSel,
                    ref EnumHandling handling
                ) {

                    if (CanInteract(byPlayer)) {

                        handling = EnumHandling.PreventSubsequent;
                        if (world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorGearedGun>() is BlockEntityBehaviorGearedGun behavior
                        ) behavior.Movement = byPlayer.Entity.Controls.CtrlKey
                            ? Vintagestory.GameContent.Mechanics.EnumRotDirection.Counterclockwise
                            : Vintagestory.GameContent.Mechanics.EnumRotDirection.Clockwise;
                        
                        if (byPlayer?.WorldData?.CurrentGameMode != EnumGameMode.Creative)
                            byPlayer.Entity
                                .ActiveHandItemSlot
                                .Itemstack?
                                .Item
                                .DamageItem(world, byPlayer.Entity, byPlayer.Entity.ActiveHandItemSlot, GameMath.RoundRandom(world.Rand, 0.1f));

                    } else {
                        
                        handling = EnumHandling.PreventDefault;
                        world.BlockAccessor
                            .GetBlockEntity(blockSel.Position)?
                            .GetBehavior<BlockEntityBehaviorGearedGun>()?
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
                        .GetBehavior<BlockEntityBehaviorGearedGun>()?
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
                        .GetBehavior<BlockEntityBehaviorGearedGun>()?
                        .TryEndUpdate();
                        
                    return true;

                } // bool ..


            //-------------------
            // R E N D E R I N G
            //-------------------

                /// <summary>
                /// Initializes a barrel renderer
                /// </summary>
                /// <param name="world"></param>
                /// <param name="barrelShapePath"></param>
                /// <param name="barrelOriginElementCode"></param>
                private void InitRenderer(
                    IWorldAccessor world,
                    string barrelShapePath,
                    string barrelOriginElementCode
                ) {

                    if (world.Side.IsClient()) {

                        Shape shape = (world.Api as ICoreClientAPI)
                            .TesselatorManager
                            .GetCachedShape(this.block.Shape.Base);

                        ITesselatorAPI mesher              = ((ICoreClientAPI)world.Api).Tesselator;
                        ShapeElement   barrelElement       = shape.GetElementByName(barrelOriginElementCode);
                        Shape          barrel              = Shape.TryGet(world.Api, barrelShapePath + ".json");
                        ShapeElement   barrelOriginElement = barrel.GetElementByName(barrelOriginElementCode);

                        mesher.TesselateShape(this.block, barrel, out this.BarrelMesh);

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
