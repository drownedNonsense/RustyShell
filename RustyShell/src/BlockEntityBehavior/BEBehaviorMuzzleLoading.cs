using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;


namespace RustyShell {
    public class BlockEntityBehaviorMuzzleLoading : BlockEntityBehavior {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Current barrel state </summary> **/          public EnumGunState GunState;
            /** <summary> Reference to the ramrod sound </summary> **/ internal ILoadedSound RamrodSound;
            /** <summary> Seconds since load start </summary> **/      internal float SecondsLoaded;

            
        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public BlockEntityBehaviorMuzzleLoading(BlockEntity blockEntity) : base(blockEntity) {}

            public override void Initialize(
                ICoreAPI api,
                JsonObject properties
            ) {

                base.Initialize(api, properties);
                if (this.Api.Side.IsClient())
                    this.RamrodSound ??= ((IClientWorldAccessor)api.World).LoadSound(new SoundParams() {
                        Location        = new AssetLocation("rustyshell:sounds/ramrod"),
                        ShouldLoop      = false,
                        Position        = this.Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume          = 1f,
                        Range           = 16,
                    }); // ..
            } // void ..


            //-------------------------------
            // T R E E   A T T R I B U T E S
            //-------------------------------

                public override void FromTreeAttributes(
                    ITreeAttribute tree,
                    IWorldAccessor worldForResolving
                ) {

                    this.GunState = (EnumGunState)tree.GetAsInt("gunState", (int)this.GunState);
                    base.FromTreeAttributes(tree, worldForResolving);

                } // void ..


                public override void ToTreeAttributes(ITreeAttribute tree) {

                    if (this.Block != null) tree.SetString("forBlockCode", this.Block.Code.ToShortString());

                    tree.SetInt("gunState", (int)this.GunState);
                    base.ToTreeAttributes(tree);

                } // void ..
    } // class ..
} // namespace ..
