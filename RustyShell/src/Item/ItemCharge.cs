using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace RustyShell {
    public class ItemCharge : Item, IPropellant {

        //=======================
        // D E F I N I T I O N S
        //=======================

            public float? PropellantBlastStrength { get; protected set; }
            public bool?  PropellantIsSmokeless   { get; protected set; }


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void OnLoaded(ICoreAPI api) {
                if (this.Attributes["propellant"].Exists) {
                    this.PropellantBlastStrength    = this.Attributes["propellant"]["strength"].AsInt();
                    this.PropellantIsSmokeless = this.Attributes["propellant"]["isSmokeless"].AsBool();
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

                    if (this.PropellantBlastStrength != 0) dsc.AppendLine(Lang.Get("charge-strength", this.PropellantBlastStrength));
                    
                    base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

                } // void ..
    } // class ..
} // namespace ..
