using System.Collections.Generic;
using RustyShell.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace RustyShell {
    public class RustyShellModSystem : ModSystem {

        public override bool ShouldLoad(EnumAppSide forSide) => true;
        public override void Start(ICoreAPI api) {

            base.Start(api);

            api.RegisterBlockClass("BlockOrientable", typeof(BlockOrientable));
            api.RegisterBlockClass("BlockHeavyGun", typeof(BlockHeavyGun));
            api.RegisterBlockClass("BlockLandmine", typeof(BlockLandmine));

            api.RegisterBlockBehaviorClass("Loadable", typeof(BlockBehaviorLoadableGun));
            api.RegisterBlockBehaviorClass("MuzzleLoading", typeof(BlockBehaviorMuzzleLoading));
            api.RegisterBlockBehaviorClass("RepeatingFire", typeof(BlockBehaviorRepeatingFire));
            api.RegisterBlockBehaviorClass("Geared", typeof(BlockBehaviorGearedGun));
            api.RegisterBlockBehaviorClass("Wheeled", typeof(BlockBehaviorWheeled));
            api.RegisterBlockBehaviorClass("Rotating", typeof(BlockBehaviorRotating));
            api.RegisterBlockBehaviorClass("Limberable", typeof(BlockBehaviorLimberable));

            api.RegisterBlockEntityClass("Orientable", typeof(BlockEntityOrientable));
            api.RegisterBlockEntityClass("HeavyGun", typeof(BlockEntityHeavyGun));
            api.RegisterBlockEntityClass("Landmine", typeof(BlockEntityLandmine));

            api.RegisterBlockEntityBehaviorClass("MuzzleLoadingGun", typeof(BlockEntityBehaviorMuzzleLoading));
            api.RegisterBlockEntityBehaviorClass("RepeatingFireGun", typeof(BlockEntityBehaviorRepeatingFire));
            api.RegisterBlockEntityBehaviorClass("WheeledObject", typeof(BlockEntityBehaviorWheeled));
            api.RegisterBlockEntityBehaviorClass("RotatingObject", typeof(BlockEntityBehaviorRotating));
            api.RegisterBlockEntityBehaviorClass("GearedGun", typeof(BlockEntityBehaviorGearedGun));

            api.RegisterEntity("EntityHighCaliber", typeof(EntityHighCaliber));
            api.RegisterEntity("EntitySmallCaliber", typeof(EntitySmallCaliber));
            api.RegisterEntity("EntityLimber", typeof(EntityLimber));

            api.RegisterEntityBehaviorClass("deployablelimber", typeof(EntityBehaviorDeployableLimber));

            api.RegisterItemClass("ItemCommander",  typeof(ItemGoad));
            api.RegisterItemClass("ItemAmmunition", typeof(ItemAmmunition));
            api.RegisterItemClass("ItemGrenade",    typeof(ItemGrenade));
            api.RegisterItemClass("ItemCharge",     typeof(ItemCharge));

            RustyShellModSystem.ModConfig = api.LoadModConfig<ModConfig>("RustyShellModConfig.json");
            if (RustyShellModSystem.ModConfig == null) {

                RustyShellModSystem.ModConfig = new ();
                api.StoreModConfig(RustyShellModSystem.ModConfig, "RustyShellModConfig.json");

            } // if ..
        } // void ..        

        public override void AssetsFinalize(ICoreAPI api) {
            
            base.AssetsFinalize(api);
            RustyShellModSystem.LookUps = new() {
                FireBlock        = api.World.GetBlock(new AssetLocation("fire")),
                LowFertilitySoil = api.World.GetBlock(new AssetLocation("soil-low-none")),
                WastedSoilLookUp = ObjectCacheUtil.GetOrCreate(api, "wastedSoilLookup", delegate {
                    return new Dictionary<string, Block>() {
                        {"verylownone",       api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"verylowverysparse", api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"verylowsparse",     api.World.GetBlock(new AssetLocation("game:soil-verylow-verysparse"))},
                        {"verylownormal",     api.World.GetBlock(new AssetLocation("game:soil-verylow-verysparse"))},
                        {"lownone",       api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"lowverysparse", api.World.GetBlock(new AssetLocation("game:soil-verylow-none"))},
                        {"lowsparse",     api.World.GetBlock(new AssetLocation("game:soil-verylow-verysparse"))},
                        {"lownormal",     api.World.GetBlock(new AssetLocation("game:soil-verylow-verysparse"))},
                        {"mediumnone",       api.World.GetBlock(new AssetLocation("game:soil-low-none"))},
                        {"mediumverysparse", api.World.GetBlock(new AssetLocation("game:soil-low-none"))},
                        {"mediumsparse",     api.World.GetBlock(new AssetLocation("game:soil-low-verysparse"))},
                        {"mediumnormal",     api.World.GetBlock(new AssetLocation("game:soil-low-verysparse"))},
                        {"compostnone",       api.World.GetBlock(new AssetLocation("game:soil-compost-none"))},
                        {"compostverysparse", api.World.GetBlock(new AssetLocation("game:soil-compost-none"))},
                        {"compostsparse",     api.World.GetBlock(new AssetLocation("game:soil-compost-verysparse"))},
                        {"compostnormal",     api.World.GetBlock(new AssetLocation("game:soil-compost-verysparse"))},
                        {"highnone",       api.World.GetBlock(new AssetLocation("game:soil-medium-none"))},
                        {"highverysparse", api.World.GetBlock(new AssetLocation("game:soil-medium-none"))},
                        {"highsparse",     api.World.GetBlock(new AssetLocation("game:soil-medium-verysparse"))},
                        {"highnormal",     api.World.GetBlock(new AssetLocation("game:soil-medium-verysparse"))},
                    }; // ..
                }), // ..
            }; // ..
        } // void ..

        public static ModConfig ModConfig { get; private set; }
        public static LookUps   LookUps   { get; private set; }

    } // class ..
} // namespace ..
