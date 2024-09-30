namespace RustyShell {
    public class RustyShellModConfig {
        /** <summary> Additional block reinforcement damage for common high caliber </summary> **/    public int CommonHighcaliberReinforcmentImpact    =  0;
        /** <summary> Additional block reinforcement damage for explosive high caliber </summary> **/ public int ExplosiveHighcaliberReinforcmentImpact = 40;
        
        /** <summary> Default gas induced damage </summary> **/ public float GasBaseDamage = 10;

        /** <summary> Minimum draft entity taming generation </summary> **/ public int DraftEntityMinGeneration = 2;

        /** <summary> Indicates to allow artillery fire to lower soil quality </summary> **/ public bool EnableLandWasting       = true;
    } // class ..
} // namespace ..
