using System;
using System.IO;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using Vintagestory.API.Util;
using RustyShell.Utilities.Blasts;


namespace RustyShell {
    public class EntityHighCaliber : EntityAmmunition {

        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //---------
            // M A I N
            //---------

                public override void OnGameTick(float deltaTime) {

                    base.OnGameTick(deltaTime);
                    if (
                        !this.stuck                                                                                  &&
                        this.Pos.Motion.LengthSq() >= 1                                                              &&
                        this.msSinceLaunch         >= (int)((this.Ammunition?.FlightExpectancy ?? 0f) * 1000) - 1500 &&
                        this.Properties.Sounds.TryGetValue("flying", out AssetLocation sound)
                    ) this.World.PlaySoundAt(sound, this, null, true, 64, 0.8f );

                } // void ..


                public override void HandleCommonBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        serverWorld.CommonBlast(
                            byEntity     : this.FiredBy,
                            pos          : this.ServerPos.XYZFloat,
                            blastRadius  : this.Ammunition.BlastRadius  ?? 0,
                            injureRadius : this.Ammunition.InjureRadius ?? 0,
                            strength     : this.Ammunition.Type switch {
                                EnumAmmunitionType.Common    => RustyShellModSystem.ModConfig.CommonHighcaliberReinforcmentImpact,
                                EnumAmmunitionType.Explosive => RustyShellModSystem.ModConfig.ExplosiveHighcaliberReinforcmentImpact,
                                _                            => 1,
                            } // switch ..
                        ); // ..

                        this.Die();

                    } // if ..
                } // void ..


                public override void HandleExplosiveBlast() => this.HandleCommonBlast();
                public override void HandleAntiPersonnelBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        serverWorld.CreateExplosion(
                            pos                       : this.ServerPos.AsBlockPos,
                            blastType                 : EnumBlastType.EntityBlast,
                            destructionRadius         : this.Ammunition.BlastRadius  ?? 0,
                            injureRadius              : this.Ammunition.InjureRadius ?? 0,
                            blockDropChanceMultiplier : 0f
                        ); // ..

                        this.Die();
                    } // if ..
                } // void ..

                public override void HandleGasBlast() {
                    this.World.GasBlast(
                        byEntity            : this.FiredBy,
                        pos                 : this.SidedPos.XYZFloat,
                        blastRadius         : this.Ammunition.BlastRadius ?? 0,
                        millisecondDuration : this.Ammunition.IsSubmunition ? 10000 : 20000
                    ); // ..

                    this.Die();
                } // void ..


                public override void HandleIncendiaryBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        serverWorld.IncendiaryBlast(
                            byEntity     : this.FiredBy,
                            pos          : this.ServerPos.XYZFloat,
                            blastRadius  : this.Ammunition.BlastRadius  ?? 0,
                            injureRadius : this.Ammunition.InjureRadius ?? 0
                        ); // ..

                        this.Die();

                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
