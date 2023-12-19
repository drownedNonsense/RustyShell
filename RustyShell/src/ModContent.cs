using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.GameContent.Mechanics;
using System.Linq;


namespace RustyShell {

    public enum EnumGunState      { Dirty, Clean, Ready  }
    public enum EnumBarrelType    { Smoothbore, Rifled }
    public enum EnumExplosionType { Simple, Piercing, HighExplosive, Grape, Canister, Fire, Gas, NonExplosive }

    public static class ModContent {

        /// <summary>
        /// 30 frames per seconds.
        /// </summary>
        public const int  HEAVY_GUN_UPDATE_RATE = 1000 / 20;

        /// <summary>
        /// 1 frame per 60 miliseconds.
        /// </summary>
        public const long LOW_UPDATE_RATE = 0b11;

        public static float RadLerp(this float t, float x, float y) =>
            GameMath.Lerp(x, (MathF.Abs(GameMath.TWOPI - x) < MathF.Abs(x - y)) switch {
                true  => y + GameMath.TWOPI,
                false => y,
            }, t);


        public static EntityProperties[] SearchEntities(
            this IWorldAccessor self,
            AssetLocation[] wildcards
        ) {

            self.Logger.Chat(wildcards.Length.ToString());

            if (wildcards.Any(wildcard => wildcard.Path.IndexOf('*') != -1))
                return self.EntityTypes.ToArray();

            List<string> allBeginsWith = new ();
            foreach (AssetLocation assetLocation in wildcards)
                allBeginsWith.Add(
                    assetLocation.EndsWithWildCard
                    ? assetLocation.Path[0..^assetLocation.Path.IndexOf('*')]
                    : assetLocation.Path
                ); // ..

            return self.EntityTypes
                .Where(entityType => allBeginsWith.Any(beginsWith => entityType.Code.Path.StartsWithFast(beginsWith)))
                .ToArray();
        } // void ..


        /// <summary>
        /// Returns true when an entity can attack another one.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="byEntity"></param>
        /// <param name="targetEntity"></param>
        /// <param name="isFromPlayer"></param>
        /// <returns></returns>
        public static bool CanDamageEntity(
            this IServerWorldAccessor self,
            Entity byEntity,
            Entity targetEntity,
            out bool isFromPlayer
        ) {

            IServerPlayer fromPlayer = (byEntity as EntityPlayer)?.Player as IServerPlayer;
            isFromPlayer = fromPlayer != null;

            if (byEntity == null) return false;

            bool targetIsPlayer   = targetEntity is EntityPlayer;
            bool targetIsCreature = targetEntity is EntityAgent;
            bool canDamage        = true;

            ICoreServerAPI sapi = self.Api as ICoreServerAPI;

            if (isFromPlayer) {
                if (targetIsPlayer   && (!sapi.Server.Config.AllowPvP || !fromPlayer.HasPrivilege("attackplayers"))) canDamage = false;
                if (targetIsCreature && !fromPlayer.HasPrivilege("attackcreatures")) canDamage = false;
            } // if ..

            return canDamage;

        } // bool ..


        public static void SpawnSmokeParticles(
            this IClientWorldAccessor self,
            BlockPos pos
        ) {
            self.SpawnParticles(new SimpleParticleProperties(
                1, 2,
                ColorUtil.ToRgba(84, 84, 84, 0),
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
                ParentVelocity       = GlobalConstants.CurrentWindSpeedClient,
            }); // ..
        } // void ..


        public static void ReleaseGas(
            this IWorldAccessor self,
            BlockPos pos,
            int      radius,
            float    damage,
            Entity   byEntity
        ) {
            if (self is IServerWorldAccessor serverWorld) {

                Room room = serverWorld.Api.ModLoader
                    .GetModSystem<RoomRegistry>()
                    .GetRoomForPosition(pos);


                foreach (Entity entity in serverWorld.GetEntitiesAround(
                    pos.ToVec3d(), radius, radius >> 1,
                    (e) => !(e.Attributes?.GetBool("isMechanical", false) ?? false) && serverWorld.CanDamageEntity(byEntity, e, out bool _) && room.Contains(e.Pos.AsBlockPos))
                ) {

                    ItemSlot itemSlot    = (entity as EntityAgent)?.GearInventory?[(int)EnumCharacterDressType.ArmorHead];
                    ItemWearable gasmask = itemSlot?.Itemstack?.Item as ItemWearable;
                    float gasStrength    = GameMath.Clamp((room.CoolingWallCount + room.NonCoolingWallCount) / GameMath.Max(room.ExitCount, 1f), 0f, damage);


                    if (gasmask?.GetRemainingDurability(itemSlot?.Itemstack) == 0 || gasmask == null)
                        entity.ReceiveDamage(new DamageSource {
                            Source       = EnumDamageSource.Internal, 
                            Type         = EnumDamageType.Suffocation,
                            CauseEntity  = byEntity,
                            SourceEntity = byEntity,
                        }, gasStrength);

                    else if (!((byEntity as IPlayer)?.WorldData?.CurrentGameMode == EnumGameMode.Creative))
                        gasmask.DamageItem(serverWorld, entity, itemSlot, GameMath.Max(GameMath.RoundRandom(serverWorld.Rand, gasStrength), 0));
                    
                } // foreach ..
                    
            } else if ((self.ElapsedMilliseconds & ModContent.LOW_UPDATE_RATE) == ModContent.LOW_UPDATE_RATE) {

                self.SpawnParticles(new SimpleParticleProperties(
                    4, 32,
                    ColorUtil.ToRgba(84, 204, 185, 0),
                    pos.ToVec3d() - new Vec3d(radius >> 1, radius >> 2, radius >> 1),
                    pos.ToVec3d() + new Vec3d(radius >> 1, radius >> 2, radius >> 1),
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
                    ParentVelocity       = GlobalConstants.CurrentWindSpeedClient,
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
                    ParentVelocity       = GlobalConstants.CurrentWindSpeedClient,
                }); // ..
            } // if ..
        } // void ..


        public static void DetonateGrape(
            this IWorldAccessor self,
            BlockPos pos,
            float    range,
            float    orientation,
            Entity   byEntity
        ) {

            if (self is IServerWorldAccessor server) {
                for (int i = 0; i < 8; i++) {

                    float pitch = -range * 0.5f + i / 8f * range;
                    float num   = MathF.Cos(pitch);
                    float num2  = MathF.Sin(pitch);


                    for (int j = 0; j < 5; j++) {

                        float yaw  = orientation - range * 0.5f + j * 0.2f * range;
                        float num3 = MathF.Cos(yaw);
                        float num4 = MathF.Sin(yaw);

                        EntityProperties type         = server.GetEntityType(new AssetLocation("rustyshell:smallcaliber"));
                        EntitySmallCaliber projectile = server.ClassRegistry.CreateEntity(type) as EntitySmallCaliber;

                        projectile.FiredBy    = byEntity;
                        projectile.ImpactSize = 1;
                        projectile.ServerPos.SetPos(pos);
                        projectile.ServerPos.Motion = new (-num * num4, num2, -num * num3);
                        projectile.Pos.SetFrom(projectile.ServerPos);

                        server.SpawnEntity(projectile);

                    } // for ..
                } // for ..
            } // if ..
        } // void ..


        public static void DetonateCanister(
            this IWorldAccessor self,
            BlockPos pos,
            int      blastRadius,
            int      injureRadius,
            Entity   byEntity
        ) {

            if (self is IServerWorldAccessor server) {

                server.CreateExplosion(
                    pos,
                    EnumBlastType.EntityBlast,
                    blastRadius >> 2,
                    injureRadius,
                    0f
                ); // ..
                

                for (int i = 1; i < blastRadius >> 1; i++) {

                    float pitch = i * 0.05f * GameMath.PIHALF;
                    float num   = MathF.Cos(pitch);
                    float num2  = MathF.Sin(pitch);

                    int amount  = 10 / (i + 1);
                    float ratio = 1f / (float)amount;
                    

                    for (int j = 0; j <= amount; j++) {

                        float yaw  = j * ratio * GameMath.PIHALF;
                        float num3 = MathF.Cos(yaw);
                        float num4 = MathF.Sin(yaw);

                        EntityProperties type = server.GetEntityType(new AssetLocation("rustyshell:smallcaliber"));

                        EntitySmallCaliber projectile = server.ClassRegistry.CreateEntity(type) as EntitySmallCaliber;
                        projectile.FiredBy    = byEntity;
                        projectile.ImpactSize = 4;
                        projectile.ServerPos.SetPos(pos);
                        projectile.ServerPos.Motion = new Vec3d(-num * num4, num2, -num * num3) * blastRadius * 0.05f;
                        projectile.Pos.SetFrom(projectile.ServerPos);
                        server.SpawnEntity(projectile);

                        projectile = server.ClassRegistry.CreateEntity(type) as EntitySmallCaliber;
                        projectile.FiredBy    = byEntity;
                        projectile.ImpactSize = 4;
                        projectile.ServerPos.SetPos(pos);
                        projectile.ServerPos.Motion = new Vec3d(num * num4, num2, -num * num3) * blastRadius * 0.05f;
                        projectile.Pos.SetFrom(projectile.ServerPos);
                        server.SpawnEntity(projectile);

                        projectile = server.ClassRegistry.CreateEntity(type) as EntitySmallCaliber;
                        projectile.FiredBy    = byEntity;
                        projectile.ImpactSize = 4;
                        projectile.ServerPos.SetPos(pos);
                        projectile.ServerPos.Motion = new Vec3d(num * num4, num2, num * num3) * blastRadius * 0.05f;
                        projectile.Pos.SetFrom(projectile.ServerPos);
                        server.SpawnEntity(projectile);

                        projectile = server.ClassRegistry.CreateEntity(type) as EntitySmallCaliber;
                        projectile.FiredBy    = byEntity;
                        projectile.ImpactSize = 4;
                        projectile.ServerPos.SetPos(pos);
                        projectile.ServerPos.Motion = new Vec3d(-num * num4, num2, num * num3) * blastRadius * 0.05f;
                        projectile.Pos.SetFrom(projectile.ServerPos);
                        server.SpawnEntity(projectile);

                    } // for ..
                } // for ..
            } // if ..
        } // void ..


        /// <summary>
        /// Returns the rotation sign
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static int Sign(this EnumRotDirection? self) =>
            self switch {
                EnumRotDirection.Clockwise        =>  1,
                EnumRotDirection.Counterclockwise => -1,
                _                                 =>  0
            }; // switch ..
    } // class ..
} // namespace ..
