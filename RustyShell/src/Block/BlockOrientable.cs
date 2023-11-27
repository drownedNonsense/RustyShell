using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;


namespace RustyShell {
    public class BlockOrientable : Block {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Indicates whether or not the block can be rotated after being placed </summary> **/ public bool Rotating { get; private set; }


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);
                this.Rotating = this.HasBehavior<BlockBehaviorRotating>();

            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                public override bool DoPlaceBlock(
                    IWorldAccessor world,
                    IPlayer        byPlayer,
                    BlockSelection blockSel,
                    ItemStack      byItemStack
                ) {

                    bool flag = base.DoPlaceBlock(
                        world,
                        byPlayer,
                        blockSel,
                        byItemStack
                    ); // DoPlaceBlock()


                    if (flag && world.BlockAccessor.GetBlockEntity(blockSel.Position) is IOrientable blockEntity) {

                        Vec3f targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position).ToVec3f();
                        float dx = (float)byPlayer.Entity.Pos.X - (targetPos.X + (float)blockSel.HitPosition.X);
                        float dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + (float)blockSel.HitPosition.Z);
                        blockEntity.ChangeOrientation(MathF.Atan2(dx, dz));

                    } // if ..


                    return flag;

                } // bool ..
    } // class ..
} // namespace ..
