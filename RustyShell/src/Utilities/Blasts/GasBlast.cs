using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace RustyShell.Utilities.Blasts;
public static partial class BlastExtensions {
    public static void GasBlast(
        this IWorldAccessor self,
        Entity byEntity,
        Vec3f pos,
        int blastRadius,
        int millisecondDuration
    ) {

        long gasRef = self.RegisterGameTickListener(
            millisecondInterval : 10,
            onGameTick          : (_) => {

                if (self is IServerWorldAccessor serverWorld) {

                    Room room = serverWorld.Api.ModLoader
                        .GetModSystem<RoomRegistry>()
                        .GetRoomForPosition(pos.AsVec3i.AsBlockPos);


                    foreach (Entity entity in serverWorld.GetEntitiesAround(
                        pos.ToVec3d(), blastRadius, blastRadius,
                        (e) => !(e.Attributes?.GetBool("isMechanical", false) ?? false)
                        && serverWorld.CanDamageEntity(byEntity, e, out bool _)
                        && room.Contains(e.Pos.AsBlockPos)
                    )) {

                        ItemSlot itemslot    = (entity as EntityAgent)?.GearInventory?[(int)EnumCharacterDressType.ArmorHead];
                        ItemWearable gasmask = itemslot?.Itemstack?.Item as ItemWearable;
                        float gasStrength    = GameMath.Clamp(
                            RustyShellModSystem.ModConfig.GasDamage * (room.CoolingWallCount + room.NonCoolingWallCount) / GameMath.Max(room.ExitCount, 1f),
                            0f,
                            RustyShellModSystem.ModConfig.GasDamage
                        ); // ..


                        if (gasmask?.GetRemainingDurability(itemslot?.Itemstack) == 0 || gasmask == null)
                            entity.ReceiveDamage(new DamageSource {
                                Source       = EnumDamageSource.Internal, 
                                Type         = EnumDamageType.Suffocation,
                                CauseEntity  = byEntity,
                                SourceEntity = byEntity,
                            }, gasStrength);

                        else if (!((byEntity as IPlayer)?.WorldData?.CurrentGameMode == EnumGameMode.Creative))
                            gasmask.DamageItem(
                                world    : serverWorld,
                                byEntity : entity,
                                itemslot : itemslot,
                                amount   : GameMath.Max(GameMath.RoundRandom(serverWorld.Rand, gasStrength), 0)
                            ); // ..
                    } // foreach ..        
                } // if ..

                self.SpawnParticles(new SimpleParticleProperties(
                    1, 8,
                    ColorUtil.ToRgba(84, 204, 185, 0),
                    pos.ToVec3d() - new Vec3d(blastRadius >> 1, blastRadius >> 1, blastRadius >> 1),
                    pos.ToVec3d() + new Vec3d(blastRadius >> 1, blastRadius >> 1, blastRadius >> 1),
                    new Vec3f(-1f, 0.5f, -1f),
                    new Vec3f( 1f, 0.5f,  1f),
                    8f,
                    0.05f,
                    1f, 4f,
                    EnumParticleModel.Quad
                ) {
                    ShouldDieInLiquid    = true,
                    WindAffected         = true,
                    SelfPropelled        = true,
                    OpacityEvolve        = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255),
                    SizeEvolve           = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, 2),
                    ParentVelocityWeight = 3f,
                    ParentVelocity       = Vintagestory.API.Config.GlobalConstants.CurrentWindSpeedClient,
                }); // ..

                self.SpawnParticles(new SimpleParticleProperties(
                    1, 2,
                    ColorUtil.ToRgba(84, 204, 185, 0),
                    pos.ToVec3d(),
                    pos.ToVec3d(),
                    new Vec3f(-0.5f, 0.5f, -0.5f),
                    new Vec3f( 0.5f, 1.0f,  0.5f),
                    8f,
                    0f,
                    1f, 4f,
                    EnumParticleModel.Quad
                ) {
                    ShouldDieInLiquid    = true,
                    WindAffected         = true,
                    SelfPropelled        = true,
                    OpacityEvolve        = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255),
                    SizeEvolve           = new EvolvingNatFloat(EnumTransformFunction.QUADRATIC, 2),
                    ParentVelocityWeight = 2f,
                    ParentVelocity       = Vintagestory.API.Config.GlobalConstants.CurrentWindSpeedClient,
                }); // ..
            } // ..
        ); // ..

        self.RegisterCallback((_) => self.UnregisterGameTickListener(gasRef), millisecondDuration);
        (self as IServerWorldAccessor)?.CreateExplosion(
            pos               : pos.AsVec3i.AsBlockPos,
            blastType         : EnumBlastType.EntityBlast,
            destructionRadius : blastRadius * 0.1f,
            injureRadius      : blastRadius * 0.4f
        ); // ..
    } // void ..
} // class ..
