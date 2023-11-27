using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;

namespace RustyShell {
    public class OrientableRenderer : IRenderer {

        /** <summary> Reference to the client API </summary> **/                 protected readonly ICoreClientAPI api;
        /** <summary> Reference to the block mesh </summary> **/                 protected readonly MeshRef meshRef;
        /** <summary> Reference to the block orientable interface </summary> **/ protected readonly IOrientable orientable;
        /** <summary> Block position </summary> **/                              protected readonly BlockPos pos;

        /** <summary> Orientable model matrix </summary> **/ public Matrixf ModelMat = new();
        

        public OrientableRenderer(
            ICoreClientAPI coreClientAPI,
            MeshData       mesh,
            BlockEntity    blockEntity,
            BlockPos       pos
        ) {

            this.api        = coreClientAPI;
            this.meshRef    = coreClientAPI.Render.UploadMesh(mesh);
            this.orientable = blockEntity as IOrientable;
            this.pos        = pos;

        } // OrientableRenderer ..


        public double RenderOrder => 0.5;
        public int    RenderRange => 24;


        public virtual void OnRenderFrame(
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
                .Translate(0, 0, this.orientable.Offset)
                .Translate(-0.5f, 0f, -0.5f)
                .Values;

            prog.ViewMatrix       = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshRef);
            prog.Stop();

        } // void ..



        public void Dispose() {

            this.api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            this.meshRef.Dispose();

        } // void ..
    } // class ..
} // namespace ..
