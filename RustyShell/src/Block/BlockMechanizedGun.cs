using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace RustyShell {
    public class BlockMechanizedGun : BlockLargeGear3m {

        public override bool HasMechPowerConnectorAt(
            IWorldAccessor world,
            BlockPos pos,
            BlockFacing face
        ) {
            if (face == BlockFacing.UP)   return false;
            if (face == BlockFacing.DOWN) return true;
            return (world.BlockAccessor.GetBlockEntity(pos) is BELargeGear3m beg) && beg.HasGearAt(world.Api, pos.AddCopy(face));
        } // bool ..


        public override bool TryPlaceBlock(
            IWorldAccessor world,
            IPlayer byPlayer,
            ItemStack itemstack,
            BlockSelection blockSel,
            ref string failureCode
        ) {

            List<BlockPos> smallGears = new ();
            if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode, smallGears))
                return false;

            bool ok = base.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            if (ok) {

                int dx, dz;
                BlockEntity beOwn             = world.BlockAccessor.GetBlockEntity(blockSel.Position);
                List<BlockFacing> connections = new ();

                foreach(var smallGear in smallGears) {

                    dx = smallGear.X - blockSel.Position.X;
                    dz = smallGear.Z - blockSel.Position.Z;
                    char orient = 'n';
                    if      (dx ==  1) orient = 'e';
                    else if (dx == -1) orient = 'w';
                    else if (dz ==  1) orient = 's';

                    BlockMPBase toPlaceBlock = world.GetBlock(new AssetLocation("angledgears-" + orient + orient)) as BlockMPBase;
                    BlockFacing blockFacing  = BlockFacing.FromFirstLetter(orient);

                    world.BlockAccessor.ExchangeBlock(BlockId, smallGear);
                    BlockEntity blockEntity           = world.BlockAccessor.GetBlockEntity(smallGear);
                    BEBehaviorMPBase bEBehaviorMPBase = blockEntity?.GetBehavior<BEBehaviorMPBase>();

                    if (bEBehaviorMPBase != null) {

                        bEBehaviorMPBase.SetOrientations();
                        bEBehaviorMPBase.Shape = toPlaceBlock.Shape;
                        blockEntity.MarkDirty();

                    } // if ..

                    toPlaceBlock.DidConnectAt(world, smallGear, blockFacing.Opposite);
                    connections.Add(blockFacing);

                } // if ..

                PlaceFakeBlocks(world, blockSel.Position, smallGears);

                BEBehaviorMPBase beMechBase = beOwn?.GetBehavior<BEBehaviorMPBase>();
                BlockPos pos                = blockSel.Position.DownCopy();

                if (world.BlockAccessor.GetBlock(pos) is IMechanicalPowerBlock block && block.HasMechPowerConnectorAt(world, pos, BlockFacing.UP)) {

                    block.DidConnectAt(world, pos, BlockFacing.UP);
                    connections.Add(BlockFacing.DOWN);

                } // if ..

                foreach (BlockFacing face in connections)
                    beMechBase?.WasPlaced(face);

            } // if ..

            return ok;
        } // bool ..


        private static void PlaceFakeBlocks(
            IWorldAccessor world,
            BlockPos pos,
            List<BlockPos> skips
        ) {

            Block toPlaceBlock = world.GetBlock(new AssetLocation("mpmultiblockwood"));
            BlockPos tmpPos    = new ();

            for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++) {
                    if (dx == 0 && dz == 0) continue;
                    bool toSkip = false;

                    foreach (var skipPos in skips)
                        if (pos.X + dx == skipPos.X && pos.Z + dz == skipPos.Z) {
                            toSkip = true;
                            break;
                        } // if ..

                    if (toSkip) continue;

                    tmpPos.Set(pos.X + dx, pos.Y, pos.Z + dz);
                    world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, tmpPos);
                    if (world.BlockAccessor.GetBlockEntity(tmpPos) is BEMPMultiblock be) be.Principal = pos;

                } // for ..
        } // void ..


        private bool CanPlaceBlock(
            IWorldAccessor world,
            IPlayer byPlayer,
            BlockSelection blockSel,
            ref string failureCode,
            List<BlockPos> smallGears
        ) {

            if (!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;
            BlockPos pos = blockSel.Position;

            BlockPos tmpPos   = new ();
            BlockSelection bs = blockSel.Clone();

            for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++) {

                    if (dx == 0 && dz == 0) continue;
                    tmpPos.Set(pos.X + dx, pos.Y, pos.Z + dz);

                    if ((dx == 0 || dz == 0) && world.BlockAccessor.GetBlock(tmpPos) is BlockAngledGears _) {
                        smallGears.Add(tmpPos.Copy());
                        continue;
                    } // if ..

                    bs.Position = tmpPos;
                    if (!base.CanPlaceBlock(world, byPlayer, bs, ref failureCode)) return false;
                    
                } // for ..

            return true;
        } // bool ..
    } // class ..
} // namespace ..
