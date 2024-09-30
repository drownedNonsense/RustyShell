using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;

namespace RustyShell {
    public class RotatingBarrelRenderer : GearedGunRenderer {

        /** <summary> Reference to the repeating fire behavior </summary> **/ protected readonly BlockEntityBehaviorRepeatingFire repeatingFire;
        /** <summary> Rotating barrel mesh rotation anchor </summary> **/     protected readonly Vec3f barrelAnchor;
        
        public RotatingBarrelRenderer(
            ICoreClientAPI coreClientAPI,
            MeshData       mesh,
            BlockEntity    blockEntity,
            BlockPos       pos,
            Vec3f          gearAnchor,
            Vec3f          barrelAnchor
        ) : base(coreClientAPI, mesh, blockEntity, pos, gearAnchor) {

            this.repeatingFire = blockEntity.GetBehavior<BlockEntityBehaviorRepeatingFire>();
            this.barrelAnchor  = barrelAnchor;

        } // RotatingBarrelRenderer ..


        public override void OnRenderFrame(
            float deltaTime,
            EnumRenderStage stage
        ) {

            if (this.meshRef == null) return;

            IRenderAPI rpi = this.api.Render;
            Vec3d camPos   = this.api.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(this.pos.X, this.pos.Y, this.pos.Z);
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;

            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(this.pos.X - camPos.X, this.pos.Y - camPos.Y, this.pos.Z - camPos.Z)
                .Translate(0.5f, 0, 0.5f)
                .RotateY(this.orientable.Orientation)
                .Translate(this.gearAnchor.X - 0.5f, this.gearAnchor.Y, this.gearAnchor.Z - 0.5f)
                .RotateXDeg(this.gearedGun.Elevation)
                .Translate(this.barrelAnchor.X - 0.5f, this.barrelAnchor.Y, this.barrelAnchor.Z - 0.5f)
                .RotateZ(this.repeatingFire.Angle)
                .Translate(-this.barrelAnchor.X, -this.barrelAnchor.Y, -this.barrelAnchor.Z)
                .Values;

            prog.ViewMatrix       = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshRef);
            prog.Stop();

        } // void ..
    } // class ..
} // namespace ..
