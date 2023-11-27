using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.GameContent.Mechanics;

namespace RustyShell {
    public class WheelRenderer : OrientableRenderer {

        /** <summary> Wheel mesh offset from block center </summary> **/  private readonly Vec3f offset;
        /** <summary> Wheel rotation direction </summary> **/             private readonly EnumRotDirection? direction;


        public WheelRenderer(
            ICoreClientAPI    coreClientAPI,
            MeshData          mesh,
            BlockEntity       blockEntity,
            BlockPos          pos,
            Vec3f             offset,
            EnumRotDirection? direction
        ) : base(coreClientAPI, mesh, blockEntity, pos) {

            this.offset     = offset;
            this.direction  = direction;

        } // WheelRenderer ..


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
                .Translate(this.offset.X - 0.5f, this.offset.Y, this.offset.Z - 0.5f)
                .Translate(0, 0, this.orientable.Offset)
                .RotateX(this.orientable.Orientation * this.direction.Sign() + this.orientable.Offset)
                .Translate(-4.25f / 16f, -14 / 16f, -0.5f)
                .Values;

            prog.ViewMatrix       = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshRef);
            prog.Stop();

        } // void ..
    } // class ..
} // namespace ..
