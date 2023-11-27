using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RustyShell {
    public class BlockEntityOrientable : BlockEntity, IOrientable {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Gun's offset from initial position </summary> **/ private MeshData ownMesh;

            /** <summary> Block's orientation in radian </summary> **/        public         float Orientation { get; set; }
            /** <summary> Block's offset from initial position </summary> **/ public virtual float Offset      { get; set; } = 0f;

            /** <summary> Indicates whether or not the block should be rendered </summary> **/ protected bool shouldRender;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void Initialize(ICoreAPI api) {

                base.Initialize(api);
                if (api.Side == EnumAppSide.Client) this.LoadOrCreateMesh();

                this.shouldRender = !this.Behaviors.Any(x => x is BlockEntityBehaviorRotating);

            } // void ..
            

        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------------
            // T R E E   A T T R I B U T E S
            //-------------------------------

                public override void FromTreeAttributes(
                    ITreeAttribute tree,
                    IWorldAccessor worldForResolving
                ) {

                    this.Orientation = tree.GetFloat("meshAngle", this.Orientation);
                    if (this.Api != null && this.Api.Side == EnumAppSide.Client) {
                        this.LoadOrCreateMesh();
                        this.MarkDirty(true);
                    } // if ..

                    base.FromTreeAttributes(tree, worldForResolving);

                } // void ..


                public override void ToTreeAttributes(ITreeAttribute tree) {

                    if (this.Block != null) tree.SetString("forBlockCode", this.Block.Code.ToShortString());

                    tree.SetFloat("meshAngle", this.Orientation);
                    base.ToTreeAttributes(tree);

                } // void ..


            //-------------------
            // R E N D E R I N G
            //-------------------

                /// <summary>
                /// Called to change the block's orientation and update its mesh
                /// </summary>
                /// <param name="orientation"></param>
                public void ChangeOrientation(float orientation) {

                    this.Orientation = orientation;
                    this.LoadOrCreateMesh();
                    
                } // void ..


                /// <summary>
                /// This method updates the block's mesh.
                /// </summary>
                public void LoadOrCreateMesh() {
                    if (this.Api.Side.IsClient()) {

                        Shape shape = (Api as ICoreClientAPI).TesselatorManager.GetCachedShape(this.Block.Shape.Base);
                        (Api as ICoreClientAPI).Tesselator.TesselateShape(this.Block, shape, out MeshData mesh);
                        this.Block.Shape.Overlays = System.Array.Empty<CompositeShape>();
                        this.ownMesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0f, 0.5f), 0f, this.Orientation, 0f);

                    } // if ..
                } // void ..


                public override bool OnTesselation(
                    ITerrainMeshPool mesher,
                    ITesselatorAPI   tesselator
                ) {

                    foreach (BlockEntityBehavior behavior in this.Behaviors)
                        if (behavior.OnTesselation(mesher, tesselator)) return true;

                    if (this.ownMesh == null || !this.shouldRender) return true;
                   
                    mesher.AddMeshData(this.ownMesh);

                    return true;

                } // bool ..
    } // class ..
} // namespace ..
