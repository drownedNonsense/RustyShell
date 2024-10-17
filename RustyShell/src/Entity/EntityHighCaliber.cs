using Vintagestory.API.Common;
using Vintagestory.API.Server;
using RustyShell.Utilities.Blasts;


namespace RustyShell {
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
                        !this.stuck                                                                                  &&
                        this.Pos.Motion.LengthSq() >= 1                                                              &&
                        this.msSinceLaunch         >= (int)((this.ExplosiveData?.FlightExpectancy ?? 0f) * 1000) - 1500 &&
                        this.Properties.Sounds.TryGetValue("flying", out AssetLocation sound)
                    ) this.World.PlaySoundAt(sound, this, null, true, 64, 0.8f );

                } // void ..


                public override void HandleCommonBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        serverWorld.CommonBlast(
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

                        this.Die();

                    } // if ..
                } // void ..


                public override void HandleExplosiveBlast() => this.HandleCommonBlast();
                public override void HandleAntiPersonnelBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        serverWorld.CreateExplosion(
                            pos                       : this.ServerPos.AsBlockPos,
                            blastType                 : EnumBlastType.EntityBlast,
                            destructionRadius         : this.ExplosiveData.BlastRadius  ?? 0,
                            injureRadius              : this.ExplosiveData.InjureRadius ?? 0,
                            blockDropChanceMultiplier : 0f
                        ); // ..

                    } // if ..
                    this.Die();
                } // void ..

                public override void HandleGasBlast() {
                    this.World.GasBlast(
                        byEntity            : this.FiredBy,
                        pos                 : this.SidedPos.XYZFloat,
                        blastRadius         : this.ExplosiveData.BlastRadius ?? 0,
                        millisecondDuration : this.ExplosiveData.IsSubmunition ? 10000 : 20000
                    ); // ..

                    this.Die();
                } // void ..


                public override void HandleIncendiaryBlast() {
                    if (this.World is IServerWorldAccessor serverWorld) {

                        serverWorld.IncendiaryBlast(
                            byEntity     : this.FiredBy,
                            pos          : this.ServerPos.XYZFloat,
                            blastRadius  : this.ExplosiveData.BlastRadius  ?? 0,
                            injureRadius : this.ExplosiveData.InjureRadius ?? 0
                        ); // ..

                        this.Die();

                    } // if ..
                } // void ..
    } // class ..
} // namespace ..
