using Vintagestory.API.MathTools;

namespace RustyShell;
public class EntitySmallCaliber : EntityExplosive {
    protected override void HandleCollision() {

        base.HandleCollision();
        this.Api.World.SpawnParticles(
            quantity         : 2,
            color            : ColorUtil.ToRgba(40, 180, 180, 180),
            minPos           : this.SidedPos.XYZ,
            maxPos           : this.SidedPos.XYZ,
            minVelocity      : new Vec3f(-0.5f, 0.5f, -0.5f),
            maxVelocity      : new Vec3f( 0.5f, 1.0f,  0.5f),
            lifeLength       : 2f,
            gravityEffect    : 0.001f,
            scale            : 1f
        ); // ..
    } // void ..
} // class ..
