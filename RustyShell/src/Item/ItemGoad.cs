using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;


namespace RustyShell {
    public class ItemGoad : Item {

        //=======================
        // D E F I N I T I O N S
        //=======================

            /** <summary> Reference to the source entity </summary> **/                        public int MinGeneration;
            /** <summary> An array of each available entity types for this goad </summary> **/ public string[] EntityPaths;


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override void OnLoaded(ICoreAPI api) {

                base.OnLoaded(api);

                this.MinGeneration = this.Attributes["leadableEntityMinGeneration"].AsInt();
                this.EntityPaths   = api.World.SearchEntities(
                    this.Attributes["leadableEntityCodes"]
                        .AsArray<string>()
                        .Select(code => new AssetLocation(code))
                        .ToArray()
                ).Select(entityType => entityType.Code.Path)
                .ToArray();
            } // void ..


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            //-------------------------
            // I N T E R A C T I O N S
            //-------------------------

                public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) {
                    return new WorldInteraction[] { new() {
                        ActionLangCode = "heldhelp-lead",
                        MouseButton    = EnumMouseButton.Right,
                    }}.Append(base.GetHeldInteractionHelp(inSlot));
                } // WorldInteraction[]


                public override void OnHeldInteractStart(
                    ItemSlot slot,
                    EntityAgent byEntity,
                    BlockSelection blockSel,
                    EntitySelection entitySel,
                    bool firstEvent,
                    ref EnumHandHandling handling
                ) {

                    handling = EnumHandHandling.PreventDefault;

                    if (entitySel?.Entity is EntityAgent agent
                        && entitySel.Entity.WatchedAttributes.GetInt("generation", this.MinGeneration) >= this.MinGeneration
                        && this.EntityPaths.Contains(entitySel.Entity.Code.Path)
                        && entitySel.Entity.GetBehavior<EntityBehaviorTaskAI>() is EntityBehaviorTaskAI taskAI
                    ) taskAI.TaskManager.ExecuteTask(new AiTaskGotoEntity(agent, byEntity), 1);
                } // void ..
    } // class ..
} // namespace ..
