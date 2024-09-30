using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace RustyShell {
    public class ItemAmmunition : Item, IPropellant {

        //=======================
        // D E F I N I T I O N S
        //=======================

            public EnumAmmunitionType Type                    { get; protected set; }
            public bool               IsFragmentation         { get; protected set; }
            public bool               IsSubmunition           { get; protected set; }
            public float              Damage                  { get; protected set; }
            public int?               BlastRadius             { get; protected set; }
            public int?               InjureRadius            { get; protected set; }

            public float? FlightExpectancy { get; protected set; }

            public Item   Casing       { get; protected set; }
            public float? RecoveryRate { get; protected set; }

            public float? PropellantBlastStrength { get; protected set; }
            public bool?  PropellantIsSmokeless   { get; protected set; }


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);
                this.Type = this.Variant["type"] switch {
                    "common"        => EnumAmmunitionType.Common,
                    "explosive"     => EnumAmmunitionType.Explosive,
                    "antipersonnel" => EnumAmmunitionType.AntiPersonnel,
                    "gas"           => EnumAmmunitionType.Gas,
                    "incendiary"    => EnumAmmunitionType.Incendiary,
                    _               => EnumAmmunitionType.Common,
                }; // switch ..

                switch (this.Variant["shape"]) {
                    case "blunt"       : { this.IsFragmentation = true; break; }
                    case "submunition" : { this.IsSubmunition   = true; break; }
                } // switch ..

                this.Damage           = this.Attributes["damage"].AsInt();
                this.BlastRadius      = this.Attributes["blastRadius"].AsInt();
                this.InjureRadius     = this.Attributes["injureRadius"].AsInt();
                this.FlightExpectancy = this.Attributes["flightExpectancy"].Exists ? this.Attributes["flightExpectancy"].AsFloat() : null;

                if (this.Attributes["casingCode"].AsString() is string casingCode) {
                    this.Casing       = api.World.GetItem(new AssetLocation(casingCode));
                    this.RecoveryRate = this.Casing?.Attributes["recoveryRate"].AsFloat(1f);
                } // if ..

                if (this.Attributes["propellant"].Exists) {
                    this.PropellantBlastStrength = this.Attributes["propellant"]["strength"].AsInt();
                    this.PropellantIsSmokeless   = this.Attributes["propellant"]["isSmokeless"].AsBool();
                } // if ..
            } // void ..

        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                public override void GetHeldItemInfo(
                    ItemSlot inSlot,
                    StringBuilder dsc,
                    IWorldAccessor world,
                    bool withDebugInfo
                ) {

                    if (this.Damage       > 0f) dsc.AppendLine(Lang.Get("ammunition-damage",       this.Damage));
                    if (this.BlastRadius  > 0)  dsc.AppendLine(Lang.Get("ammunition-blastradius",  this.BlastRadius));
                    if (this.InjureRadius > 0)  dsc.AppendLine(Lang.Get("ammunition-injureradius", this.InjureRadius));
                    
                    if (this.FlightExpectancy > 0f) dsc.AppendLine(Lang.Get("ammunition-flightexpectancy", this.FlightExpectancy));
                    
                    if (this.RecoveryRate > 0f) {
                        dsc.AppendLine();
                        dsc.AppendLine(Lang.Get("ammunition-recoveryrate", (int)(this.RecoveryRate * 100)));
                    } // if ..
                    
                    if (this.PropellantBlastStrength > 0f) {
                        dsc.AppendLine();
                        dsc.AppendLine(Lang.Get("ammunition-propellantblaststrength", this.PropellantBlastStrength));
                    } // if ..

                    base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

                } // void ..
    } // class ..
} // namespace ..
