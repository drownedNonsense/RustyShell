using Vintagestory.API.Common;
using Vintagestory.API.Server;
using RustyShell.Utilities.Blasts;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;


namespace RustyShell;
public class EntityHighCaliber : EntityExplosive {

    //===============================
    // I M P L E M E N T A T I O N S
    //===============================

        //---------
        // M A I N
        //---------

            public override void OnGameTick(float deltaTime) {

                base.OnGameTick(deltaTime);
                if (
                    this.World is IClientWorldAccessor clientWorld                                                  &&
                    !this.stuck                                                                                     &&
                    this.Pos.Motion.LengthSq() >= 1                                                                 &&
                    this.msSinceLaunch         >= (int)((this.ExplosiveData?.FlightExpectancy ?? 0f) * 1000) - 1500 &&
                    this.Properties.Sounds.TryGetValue("flying", out AssetLocation sound)
                ) clientWorld.PlaySoundAt(sound, this, null, true, 64, 0.8f );

            } // void ..


            protected override void HandleCommonBlast() =>
                (this.World as IServerWorldAccessor)?.CommonBlast(
                    byEntity     : this.FiredBy,
                    pos          : this.ServerPos.XYZFloat,
                    blastRadius  : this.ExplosiveData.BlastRadius  ?? 0,
                    injureRadius : this.ExplosiveData.InjureRadius ?? 0,
                    strength     : this.ExplosiveData.Type switch {
                        EnumExplosiveType.Common    => RustyShellModSystem.ModConfig.CommonHighcaliberReinforcmentImpact,
                        EnumExplosiveType.Explosive => RustyShellModSystem.ModConfig.ExplosiveHighcaliberReinforcmentImpact,
                        _                            => 1,
                    } // switch ..
                ); // ..


            protected override void HandleExplosiveBlast()     => this.HandleCommonBlast();
            protected override void HandleAntiPersonnelBlast() =>
                (this.World as IServerWorldAccessor)?.CreateExplosion(
                    pos                       : this.ServerPos.AsBlockPos,
                    blastType                 : EnumBlastType.EntityBlast,
                    destructionRadius         : this.ExplosiveData.BlastRadius  ?? 0,
                    injureRadius              : this.ExplosiveData.InjureRadius ?? 0,
                    blockDropChanceMultiplier : 0f
                ); // ..

            protected override void HandleGasBlast() =>
                this.World.GasBlast(
                    byEntity            : this.FiredBy,
                    pos                 : this.SidedPos.XYZFloat,
                    blastRadius         : this.ExplosiveData.BlastRadius ?? 0,
                    millisecondDuration : this.ExplosiveData.IsSubmunition ? 10000 : 20000
                ); // ..


            protected override void HandleIncendiaryBlast() =>
                (this.World as IServerWorldAccessor)?.IncendiaryBlast(
                    byEntity     : this.FiredBy,
                    pos          : this.ServerPos.XYZFloat,
                    blastRadius  : this.ExplosiveData.BlastRadius  ?? 0,
                    injureRadius : this.ExplosiveData.InjureRadius ?? 0
                ); // ..
} // class ..
