using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace RustyShell.Utilities.Blasts;
public static partial class BlastExtensions {
    public static void IncendiaryBlast(
        this IServerWorldAccessor self,
        Entity byEntity,
        Vec3f pos,
        int blastRadius,
        int injureRadius
    ) {

        Vec3i center           = pos.AsVec3i;
        Cuboidi incendiaryArea = new (center - new Vec3i(blastRadius, blastRadius, blastRadius),    center + new Vec3i(blastRadius, blastRadius, blastRadius));
        Cuboidi falloutArea    = new (center - new Vec3i(injureRadius, injureRadius, injureRadius), center + new Vec3i(injureRadius, injureRadius, injureRadius));

        foreach (LandClaim landClaim in (self.Api as ICoreServerAPI).WorldManager.SaveGame.LandClaims)
            if (landClaim.Intersects(incendiaryArea))
                return;


        int blastRadiusSq       = blastRadius * blastRadius;
        ThreadSafeRandom random = new();

        self.BlockAccessor.WalkBlocks(
            minPos      : incendiaryArea.Start.ToBlockPos(),
            maxPos      : incendiaryArea.End.ToBlockPos(),
            centerOrder : true,
            onBlock     : (block, xWorld, yWorld, zWorld) => {

                if (block.Id == 0 || block.Id == RustyShellModSystem.LookUps.FireBlock.Id) return;

                Vec3i searchPos = new (xWorld, yWorld, zWorld);
                int x = searchPos.X - center.X;
                int y = searchPos.Y - center.Y;
                int z = searchPos.Z - center.Z;

                int distanceSq = x * x + y * y + z * z;
                if (int.IsPositive(-distanceSq + blastRadiusSq)) {

                    BlockPos pos = searchPos.AsBlockPos;
                    if (self.BlockAccessor.GetBlock(pos.UpCopy()).Id == 0 && random.Next(0, distanceSq >> 1) == 0)
                        self.BlockAccessor.SetBlock(RustyShellModSystem.LookUps.FireBlock.Id, pos.UpCopy());

                    self.BlockAccessor.GetBlockEntity(pos.UpCopy())
                        ?.GetBehavior<BEBehaviorBurning>()
                        ?.OnFirePlaced(pos.UpCopy(), pos, (byEntity as EntityPlayer)?.PlayerUID);
                        
                    self.BlockAccessor.MarkBlockDirty(pos.UpCopy());
                    self.BlockAccessor.MarkBlockEntityDirty(pos.UpCopy());
                } // if ..
            } // ..
        ); // ..


        foreach (Entity entity in self.GetEntitiesInsideCuboid(
            falloutArea.Start.ToBlockPos(),
            falloutArea.End.ToBlockPos(),
            (e) => self.CanDamageEntity(byEntity, e, out _)
        )) entity.IsOnFire = true;

        self.CreateExplosion(
            pos                       : pos.AsVec3i.AsBlockPos,
            blastType                 : EnumBlastType.RockBlast,
            destructionRadius         : blastRadius  * 0.6f,
            injureRadius              : injureRadius * 0.6f,
            blockDropChanceMultiplier : 0.2f
        ); // ..
    } // void ..
} // class ..
