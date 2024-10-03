using RustyShell.Utilities.Blasts;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace RustyShell;
public class BlockEntityLandmine : BlockEntity {

    //===============================
    // I M P L E M E N T A T I O N S
    //===============================

        public void OnBlockExploded(Entity byEntity) {

            if (this.Api.World is IServerWorldAccessor serverWorld) {
                serverWorld.BlockAccessor.SetBlock(0, this.Pos);
                switch (this.Block.Variant["type"]) {
                    case "explosive"  : { serverWorld.CommonBlast(byEntity,     this.Pos.ToVec3f(), (this.Block as BlockLandmine)?.BlastRadius ?? 0, (this.Block as BlockLandmine)?.InjureRadius ?? 0, 1); break; }
                    case "incendiary" : { serverWorld.IncendiaryBlast(byEntity, this.Pos.ToVec3f(), (this.Block as BlockLandmine)?.BlastRadius ?? 0, (this.Block as BlockLandmine)?.InjureRadius ?? 0);    break; }
                }; // ..
            } // if ..
        } // void ..
} // class ..