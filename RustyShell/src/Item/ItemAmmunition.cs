using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace RustyShell {
    public class ItemAmuunition : Item {
        public override void GetHeldItemInfo(
            ItemSlot inSlot,
            StringBuilder dsc,
            IWorldAccessor world,
            bool withDebugInfo
        ) {

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (inSlot.Itemstack.Collectible.Attributes == null) return;

            JsonObject detonation = inSlot.Itemstack.Collectible.Attributes["detonation"];
            float damage           = detonation["amount"].AsFloat();
            float altDamage        = detonation["altDamage"].AsFloat();
            int blastRadius        = detonation["blastRadius"].AsInt();
            int injureRadius       = detonation["injureRadius"].AsInt();
            float fuseDuration     = detonation["fuseDuration"].AsInt() * 0.001f;
            float duration         = detonation["duration"].AsInt() * 0.001f;
            EnumExplosionType type = detonation["type"].AsString() switch {
                "Simple"        => EnumExplosionType.Simple,
                "Piercing"      => EnumExplosionType.Piercing,
                "HighExplosive" => EnumExplosionType.HighExplosive,
                "Grape"         => EnumExplosionType.Grape,
                "Canister"      => EnumExplosionType.Canister,
                "Gas"           => EnumExplosionType.Gas,
                _               => EnumExplosionType.Simple,
            }; // switch ..

            if (altDamage == 0 && type == EnumExplosionType.Gas) altDamage = RustyShellModSystem.GlobalConstants.GasBaseDamage;

            dsc.AppendLine();
            if (damage != 0)       dsc.AppendLine(Lang.Get("ammunition-damage",( damage > 0 ? "+" : "") + damage));
            if (duration != 0 && type == EnumExplosionType.Gas) dsc.AppendLine(Lang.Get("ammunition-duration", (altDamage > 0 ? "+" : "") + altDamage, duration));
            if (blastRadius != 0)  dsc.AppendLine(Lang.Get("ammunition-blastradius", blastRadius));
            if (injureRadius != 0) dsc.AppendLine(Lang.Get("ammunition-injureradius", injureRadius));
            if (fuseDuration != 0) dsc.AppendLine(Lang.Get("ammunition-fuseduration", fuseDuration));

        } // void ..
    } // class ..
} // namespace ..
