﻿using System;
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
    public enum EnumExplosiveType { Common, Explosive, AntiPersonnel, Gas, Incendiary }

    public static class ModContent {

        /// <summary>
        /// 60 frames per seconds.
        /// </summary>
        public const int  HEAVY_GUN_UPDATE_RATE = 1000 / 60;

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
                ParentVelocity       = Vintagestory.API.Config.GlobalConstants.CurrentWindSpeedClient,
            }); // ..
        } // void ..


        public static void ReleaseGas(
            this IWorldAccessor self,
            BlockPos pos,
            int      radius,
            Entity   byEntity
        ) {
            if (self is IServerWorldAccessor serverWorld) {

                Room room = serverWorld.Api.ModLoader
                    .GetModSystem<RoomRegistry>()
                    .GetRoomForPosition(pos);


                foreach (Entity entity in serverWorld.GetEntitiesAround(
                    pos.ToVec3d(), radius, radius,
                    (e) => !(e.Attributes?.GetBool("isMechanical", false) ?? false) && serverWorld.CanDamageEntity(byEntity, e, out bool _) && room.Contains(e.Pos.AsBlockPos))
                ) {

                    ItemSlot itemSlot    = (entity as EntityAgent)?.GearInventory?[(int)EnumCharacterDressType.ArmorHead];
                    ItemWearable gasmask = itemSlot?.Itemstack?.Item as ItemWearable;
                    float gasStrength    = GameMath.Clamp(RustyShellModSystem.ModConfig.GasDamage * (room.CoolingWallCount + room.NonCoolingWallCount) / GameMath.Max(room.ExitCount, 1f), 0f, RustyShellModSystem.ModConfig.GasDamage);


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
            } // if ..

            self.SpawnParticles(new SimpleParticleProperties(
                1, 8,
                ColorUtil.ToRgba(84, 204, 185, 0),
                pos.ToVec3d() - new Vec3d(radius >> 1, radius >> 1, radius >> 1),
                pos.ToVec3d() + new Vec3d(radius >> 1, radius >> 1, radius >> 1),
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
