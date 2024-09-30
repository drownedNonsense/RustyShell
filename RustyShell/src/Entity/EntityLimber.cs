using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using System.Linq;

namespace RustyShell {
    public class EntityLimber: EntityAgent {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Reference to the leading draft entity </summary> **/  internal Entity DraftEntityLeader;
            /** <summary> Reference to the side draft entity </summary> **/     internal Entity DraftEntitySide;
            /** <summary> Reference to the drafted limber entity </summary> **/ internal EntityLimber DraftingLimber;

            /** <summary> Indicates whether or not the limber requires two draft entity </summary> **/ protected bool  requiresSideEntity;
            /** <summary> Offset of the limber behind the draft entities </summary> **/                protected float offset;

            /** <summary> Draft entities limber search radius </summary> **/ protected const int SEARCH_RADIUS = 6;

            /// <summary>
            /// Limber entity processed move speed
            /// </summary>
            public float MoveSpeed => (this.DraftingLimber is EntityLimber e)
                ? GameMath.Min(e.MoveSpeed,                             (float)e.GetWalkSpeedMultiplier())
                : GameMath.Min(this.SidedPos.Motion.ToVec3f().Length(), (float)this.GetWalkSpeedMultiplier());



        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void Initialize(
                EntityProperties properties,
                ICoreAPI api,
                long InChunkIndex3d
            ) {

                base.Initialize(properties, api, InChunkIndex3d);
                
                this.offset             = properties.Attributes["limber"]["offset"].AsFloat(-1);
                this.requiresSideEntity = properties.Attributes["limber"]["requiresSideEntity"].AsBool(false);

                long? leaderId = this.WatchedAttributes.TryGetLong("draftEntityLeaderId");
                long? sideId   = this.WatchedAttributes.TryGetLong("draftEntitySideId");


                if (leaderId.HasValue && !(this.requiresSideEntity ^ sideId.HasValue)) {

                    this.DraftEntityLeader = api.World.GetEntityById(leaderId.Value);
                    this.DraftEntitySide   = this.requiresSideEntity ? api.World.GetEntityById(sideId.Value) : null;

                } else {

                    int entityGeneration = properties.Attributes["limber"]["minGeneration"].AsInt();
                    string[] entityCodes = api.World.SearchEntities(
                        properties.Attributes["limber"]["entityCodes"]
                            .AsArray<string>()
                            .Select(code => new AssetLocation(code))
                            .ToArray()
                    ).Select(entityType => entityType.Code.Path)
                    .ToArray();


                    Entity[] entities = this.World.GetEntitiesAround(
                        this.SidedPos.XYZ,
                        EntityLimber.SEARCH_RADIUS, EntityLimber.SEARCH_RADIUS,
                        (e) => e != this
                            && !e.WatchedAttributes.GetBool("isDraftingLimber", false)
                            && e.WatchedAttributes.GetInt("generation", entityGeneration) >= entityGeneration
                            && entityCodes.Contains(e.Code.Path)
                    ); // ..


                    if (entities.Length >= (this.requiresSideEntity ? 2 : 1)) {
                        
                        this.DraftEntityLeader = entities[0];
                        this.DraftEntityLeader.WatchedAttributes.SetBool("isDraftingLimber", true);
                        this.WatchedAttributes.SetLong("draftEntityLeaderId", this.DraftEntityLeader.EntityId);

                        if (this.requiresSideEntity) {

                            this.DraftEntitySide   = this.requiresSideEntity ? entities[1] : null;
                            this.DraftEntitySide.WatchedAttributes.SetBool("isDraftingLimber", true);
                            this.WatchedAttributes.SetLong("draftEntitySideId", this.DraftEntitySide.EntityId);

                        } // if ..

                        if (this.DraftEntityLeader is EntityLimber limber)
                            limber.DraftingLimber = this;

                    } // if ..
                } // if ..
            } // void ..


            //===============================
            // I M P L E M E N T A T I O N S
            //===============================

                public override void OnGameTick(float deltaTime) {
                    if (this.DraftEntityLeader != null && !(this.requiresSideEntity ^ this.DraftEntitySide != null)) {
                        if (this.DraftEntitySide != null) {

                            this.DraftEntitySide.SidedPos.SetPos(this.DraftEntityLeader.SidedPos.HorizontalAheadCopy(1));
                            this.DraftEntitySide.SidedPos.SetAngles(this.DraftEntityLeader.SidedPos);
                            this.DraftEntitySide.SidedPos.Motion = this.DraftEntityLeader.SidedPos.Motion;

                            if (!this.DraftEntitySide.Alive) this.DraftEntitySide = null;

                        } // if ..


                        this.SidedPos.Motion = this.DraftEntityLeader.SidedPos.XYZ + new Vec3d(
                              GameMath.Cos(this.DraftEntityLeader.SidedPos.Yaw) * (this.requiresSideEntity ? 0.5 : 0)
                            - GameMath.Cos(this.DraftEntityLeader.SidedPos.Yaw + GameMath.PIHALF) * this.offset,
                             0,
                            - GameMath.Sin(this.DraftEntityLeader.SidedPos.Yaw) * (this.requiresSideEntity ? 0.5 : 0)
                            + GameMath.Sin(this.DraftEntityLeader.SidedPos.Yaw + GameMath.PIHALF) * this.offset
                        ) - this.SidedPos.XYZ; // ..


                        float speed = this.MoveSpeed;
                        if (this.DraftEntityLeader.SidedPos.Motion.LengthSq() > speed)
                            if (this.DraftEntityLeader is EntityAgent agent)
                                agent.ServerControls.WalkVector = agent.ServerControls.WalkVector.Normalize() * speed;

                        this.SidedPos.Yaw = this.DraftEntityLeader.SidedPos.Yaw;
                        

                        if (this.World.Side.IsClient()) {

                            this.Pos.Roll = -GameMath.PIHALF * 0.3f;
                            float motion  =  GameMath.Min(this.Pos.Motion.ToVec3f().Length() / deltaTime, 1f);

                            if (!this.AnimManager
                                .ActiveAnimationsByAnimCode
                                .TryGetValue("move", out AnimationMetaData animMeta)
                            ) {

                                animMeta = new AnimationMetaData() {
                                    Code           = "move",
                                    Animation      = "move",
                                    AnimationSpeed = motion,
                                }; // ..

                                this.AnimManager.ActiveAnimationsByAnimCode.Clear();
                                this.AnimManager.ActiveAnimationsByAnimCode[animMeta.Animation] = animMeta;

                            } else animMeta.AnimationSpeed = motion;
                        } // if ..
                    } else if (this.GetBehavior<EntityBehaviorDeployableLimber>() is EntityBehaviorDeployableLimber limber) {
                        if (!limber.TryDeploy(this)) this.Pos.Roll = 0;
                    } else this.Die(EnumDespawnReason.Removed);


                    base.OnGameTick(deltaTime);

                } // void ..



                /// <summary>
                /// Indicates whether or not the limber entity has matching draft entities
                /// </summary>
                /// <param name="world"></param>
                /// <param name="properties"></param>
                /// <param name="position"></param>
                /// <returns></returns>
                public static bool HasNearbyDraftEntities(
                    IWorldAccessor world,
                    EntityProperties properties,
                    Vec3d position
                ) {

                    bool requiresSideEntity = properties.Attributes?["limber"]?["requiresSideEntity"].AsBool(false) ?? false;
                    int  minGeneration      = properties.Attributes?["limber"]?["minGeneration"].AsInt() ?? 0;

                    string[] entityCodes = world.SearchEntities(
                        properties.Attributes["limber"]["entityCodes"]
                            .AsArray<string>()
                            .Select(code => new AssetLocation(code))
                            .ToArray()
                    ).Select(entityType => entityType.Code.Path)
                    .ToArray();

                    return world.GetEntitiesAround(
                        position, EntityLimber.SEARCH_RADIUS, EntityLimber.SEARCH_RADIUS,
                        (e) => !e.WatchedAttributes.GetBool("isDraftingLimber", false)
                            && e.WatchedAttributes.GetInt("generation", minGeneration) >= minGeneration
                            && entityCodes.Contains(e.Code.Path)
                    ).Length >= (requiresSideEntity ? 2 : 1);
                } // bool ..


                public override double GetWalkSpeedMultiplier(double groundDragFactor = 0.3) {

                    int y1 = (int)(this.SidedPos.Y - 0.05f);
                    int y2 = (int)(this.SidedPos.Y + 0.01f);

                    Block belowBlock = this.World.BlockAccessor.GetBlock((int)this.SidedPos.X, y1, (int)this.SidedPos.Z);

                    this.insidePos.Set((int)this.SidedPos.X, y2, (int)this.SidedPos.Z);
                    this.insideBlock = this.World.BlockAccessor.GetBlock(this.insidePos);

                    float multiplier = 1f / (this.Properties.Weight * 0.002f);

                    if (this.FeetInLiquid)
                        multiplier *= 0.4f;

                    multiplier *= belowBlock.WalkSpeedMultiplier * (y1 == y2 ? 1 : this.insideBlock.WalkSpeedMultiplier);
                    multiplier *= multiplier;

                    return multiplier;

                } // double ..
    } // class ..
} // namespace ..
