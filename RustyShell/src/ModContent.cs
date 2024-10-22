using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
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
