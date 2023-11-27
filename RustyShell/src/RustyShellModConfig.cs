namespace RustyShell {
    public class RustyShellModConfig {
        /** <summary> Additional block reinforcement damage for simple projectiles </summary> **/         public int SimpleProjectileReinforcmentImpact         = 0;
        /** <summary> Additional block reinforcement damage for piercing projectiles </summary> **/       public int PiercingProjectileReinforcmentImpact       = 40;
        /** <summary> Additional block reinforcement damage for high explosive projectiles </summary> **/ public int HighExplosiveProjectileReinforcementImpact = 20;
        
        /** <summary> Default gas induced damage </summary> **/ public float GasBaseDamage = 10;

        /** <summary> Minimum draft entity taming generation </summary> **/ public int DraftEntityMinGeneration = 2;

        /** <summary> Indicates to allow artillery fire to lower soil quality </summary> **/ public bool EnableLandWasting       = true;
    } // class ..
} // namespace ..
