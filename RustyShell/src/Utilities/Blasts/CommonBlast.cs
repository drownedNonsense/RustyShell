using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace RustyShell.Utilities.Blasts;
public static partial class BlastExtensions {
    public static void CommonBlast(
        this IServerWorldAccessor self,
        Entity byEntity,
        Vec3f pos,
        int blastRadius,
        int injureRadius,
        int strength
    ) {

        Vec3i center = pos.AsVec3i;
        self.Api.ModLoader.GetModSystem<ScreenshakeToClientModSystem>().ShakeScreen(
            pos      : pos.ToVec3d(),
            strength : blastRadius * 0.4f,
            range    : blastRadius * 0.2f
        ); // ..

        int roundRadius       = blastRadius;
        Cuboidi explosionArea = new (center - new Vec3i(roundRadius, roundRadius, roundRadius), center + new Vec3i(roundRadius, roundRadius, roundRadius));

        foreach (LandClaim landClaim in (self.Api as ICoreServerAPI).WorldManager.SaveGame.LandClaims)
            if (landClaim.Intersects(explosionArea))
                return;


        bool canBreakReinforced = !((byEntity as IPlayer)?.HasPrivilege("denybreakreinforced") ?? false) && strength != 0;
        int  blastRadiusSq      = blastRadius * blastRadius;

        ModSystemBlockReinforcement reinforcement = null;
        if (canBreakReinforced) reinforcement = self.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();

        self.BlockAccessor.WalkBlocks(
            minPos      : explosionArea.Start.ToBlockPos(),
            maxPos      : explosionArea.End.ToBlockPos(),
            centerOrder : true,
            onBlock     : (block, xWorld, yWorld, zWorld) => {

                if (block.Id == 0 || block.Id == RustyShellModSystem.LookUps.FireBlock.Id) return;

                Vec3i searchPos = new (xWorld, yWorld, zWorld);
                int x = searchPos.X - center.X;
                int y = searchPos.Y - center.Y;
                int z = searchPos.Z - center.Z;

                int negDistanceSq = -(x * x + y * y + z * z);
                if (int.IsPositive(negDistanceSq + blastRadiusSq)) {
                    if (canBreakReinforced)
                        reinforcement.ConsumeStrength(searchPos.AsBlockPos, strength);

                    if (RustyShellModSystem.ModConfig.EnableLandWasting
                        && block is BlockSoil
                        && block.Variant["fertility"] is string fertility
                        && block.Variant["grasscoverage"] is string grassCoverage
                    ) {

                        string endGrassCoverage = (int?)((-negDistanceSq + (blastRadiusSq >> 2)) / (float)(blastRadiusSq + 1) * (grassCoverage switch {
                            "none"       => 0,
                            "verysparse" => 1,
                            "sparse"     => 2,
                            "normal"     => 3,
                            _            => null,
                        })) switch {
                            0 => "none",
                            1 => "verysparse",
                            2 => "sparse",
                            3 => "normal",
                            _ => grassCoverage
                        }; // switch ..

                        Block newBlock = RustyShellModSystem.LookUps.WastedSoilLookUp[fertility + endGrassCoverage];
                        self.BlockAccessor.ExchangeBlock(newBlock.Id, searchPos.AsBlockPos);

                    } // if ..     

                    self.BlockAccessor.MarkBlockEntityDirty(searchPos.AsBlockPos);
                } // if ..
            } // ..
        ); // ..


        self.CreateExplosion(
            pos                       : pos.AsVec3i.AsBlockPos,
            blastType                 : EnumBlastType.RockBlast,
            destructionRadius         : blastRadius,
            injureRadius              : injureRadius,
            blockDropChanceMultiplier : 0.2f
        ); // ..
    } // void ..
} // class ..
