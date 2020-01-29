using UIForia.Compilers;
using UIForia.Parsing.Expressions;
using UIForia.Util;

namespace UIForia.Parsing {

    public enum SlotType {

        Define,
        Children,
        Forward,
        Template,
        Override

    }

    public class SlotNode : TemplateNode {

        public string slotName;
        public SlotType slotType;

        public CompiledSlot compiledSlot;

        public SlotNode(TemplateRootNode root, TemplateNode parent, ProcessedType processedType, StructList<AttributeDefinition> attributes, in TemplateLineInfo templateLineInfo, string slotName, SlotType slotType)
            : base(root, parent, processedType, attributes, templateLineInfo) {
            this.slotName = slotName;
            this.slotType = slotType;
        }
        
        public AttributeDefinition[] GetAttributes(AttributeType expose) {
            if (attributes == null) {
                return null;
            }

            int cnt = 0;
            for (int i = 0; i < attributes.size; i++) {
                if (attributes.array[i].type == expose) {
                    cnt++;
                }
            }

            if (cnt == 0) return null;
            int idx = 0;
            AttributeDefinition[] retn = new AttributeDefinition[cnt];
            for (int i = 0; i < attributes.size; i++) {
                if (attributes.array[i].type == expose) {
                    retn[idx++] = attributes.array[i];
                }
            }

            return retn;
        }

    }

}