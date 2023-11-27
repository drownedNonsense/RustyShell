using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace RustyShell {
    public class BlockEntityBehaviorTurntable : BEBehaviorMPLargeGear3m {

        public BlockEntityBehaviorTurntable(BlockEntity blockentity) : base(blockentity) {}


        public override void Initialize(ICoreAPI api, JsonObject properties) {
            base.Initialize(api, properties);

            this.AxisSign = new int[3] { 0, 1, 0 };

            if (api.Side == EnumAppSide.Client)
                Blockentity.RegisterGameTickListener(OnEverySecond, 1000);
        } // void ..


        public override bool isInvertedNetworkFor(BlockPos pos) => propagationDir == BlockFacing.DOWN;

        private void OnEverySecond(float dt) {
            float speed = this.network == null
                ? 0
                : this.network.Speed;

            if (this.Api.World.Rand.NextDouble() < speed / 4f)
                this.Api.World.PlaySoundAt(
                    new AssetLocation("sounds/block/metaldoor-place"),
                    this.Position.X + 0.5,
                    this.Position.Y + 0.5,
                    this.Position.Z + 0.5,
                    null,
                    0.85f + speed
                ); // ..
        } // void ..

        public override float GetResistance() => 0.008f;

    } // class ..
} // namespace ..
