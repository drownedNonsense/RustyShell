using Vintagestory.API.Common;

namespace RustyShell {
    public class RustyShellModSystem : ModSystem {

        public override bool ShouldLoad(EnumAppSide forSide) => true;
        public override void Start(ICoreAPI api) {

            base.Start(api);

            api.RegisterBlockClass("BlockOrientable", typeof(BlockOrientable));
            api.RegisterBlockClass("BlockHeavyGun", typeof(BlockHeavyGun));

            api.RegisterBlockBehaviorClass("Loadable", typeof(BlockBehaviorLoadableGun));
            api.RegisterBlockBehaviorClass("MuzzleLoading", typeof(BlockBehaviorMuzzleLoading));
            api.RegisterBlockBehaviorClass("RepeatingFire", typeof(BlockBehaviorRepeatingFire));
            api.RegisterBlockBehaviorClass("Geared", typeof(BlockBehaviorGearedGun));
            api.RegisterBlockBehaviorClass("Wheeled", typeof(BlockBehaviorWheeled));
            api.RegisterBlockBehaviorClass("Rotating", typeof(BlockBehaviorRotating));
            api.RegisterBlockBehaviorClass("Limberable", typeof(BlockBehaviorLimberable));

            api.RegisterBlockEntityClass("Orientable", typeof(BlockEntityOrientable));
            api.RegisterBlockEntityClass("HeavyGun", typeof(BlockEntityHeavyGun));

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
            api.RegisterItemClass("ItemCharge",     typeof(ItemCharge));

            RustyShellModSystem.GlobalConstants = api.LoadModConfig<RustyShellModConfig>("RustyShellModConfig.json");
            if (RustyShellModSystem.GlobalConstants == null) {

                RustyShellModSystem.GlobalConstants = new ();
                api.StoreModConfig(RustyShellModSystem.GlobalConstants, "RustyShellModConfig.json");

            } // if ..
        } // void ..        

        public static RustyShellModConfig GlobalConstants { get; private set; }

    } // class ..
} // namespace ..
