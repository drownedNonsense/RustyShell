using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;

namespace RustyShell {
    public class GearedGunRenderer : OrientableRenderer {

        /** <summary> Reference to the gear behavior </summary> **/ protected readonly BlockEntityBehaviorGearedGun gearedGun;
        /** <summary> Barrel mesh rotation anchor </summary> **/    protected readonly Vec3f gearAnchor;


        public GearedGunRenderer(
            ICoreClientAPI coreClientAPI,
            MeshData       mesh,
            BlockEntity    blockEntity,
            BlockPos       pos,
            Vec3f          gearAnchor
        ) : base(coreClientAPI, mesh, blockEntity, pos) {

            this.gearedGun  = blockEntity.GetBehavior<BlockEntityBehaviorGearedGun>();
            this.gearAnchor = gearAnchor;

        } // GearedGunRenderer ..


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
                .Translate(0, 0, this.orientable.Offset)
                .RotateX(MathF.Atan(this.gearedGun.Elevation))
                .Translate(-0.5f, 0, -0.5f)
                .Values;

            prog.ViewMatrix       = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshRef);
            prog.Stop();

        } // void ..
    } // class ..
} // namespace ..
