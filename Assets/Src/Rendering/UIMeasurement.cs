using System.Diagnostics;
using Rendering;

namespace Src {

    [DebuggerDisplay("{unit}({value})")]
    public struct UIMeasurement {

        public readonly float value;
        public readonly UIUnit unit;

        public UIMeasurement(float value, UIUnit unit = UIUnit.Pixel) {
            this.value = value;
            this.unit = unit;
        }

        public UIMeasurement(int value, UIUnit unit = UIUnit.Pixel) {
            this.value = value;
            this.unit = unit;
        }

        public UIMeasurement(double value, UIUnit unit = UIUnit.Pixel) {
            this.value = (float) value;
            this.unit = unit;
        }

        [DebuggerStepThrough]
        public bool IsDefined() {
            return FloatUtil.IsDefined(value);
        }
        
        public bool isFixed => (unit & (UIUnit.Pixel | UIUnit.ParentSize | UIUnit.View | UIUnit.Em)) != 0;
        
        public bool isParentRelative => (unit & (UIUnit.ParentSize | UIUnit.ParentContentArea)) != 0;
        
        public bool isContentRelative => (unit & (UIUnit.Content | UIUnit.FitContent | UIUnit.MaxContent | UIUnit.MinContent)) != 0;
        
        public static UIMeasurement Auto => new UIMeasurement(1f, UIUnit.ParentContentArea);
        public static UIMeasurement Parent100 => new UIMeasurement(1f, UIUnit.ParentSize);
        public static UIMeasurement Content100 => new UIMeasurement(1f, UIUnit.Content);
        public static UIMeasurement Unset => new UIMeasurement(FloatUtil.UnsetFloatValue);

        public bool Equals(UIMeasurement other) {
            return value.Equals(other.value) && unit == other.unit;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is UIMeasurement && Equals((UIMeasurement) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (value.GetHashCode() * 397) ^ (int) unit;
            }
        }

        public static bool operator ==(UIMeasurement self, UIMeasurement other) {
            if (float.IsNaN(self.value) && float.IsNaN(other.value)) {
                return self.unit == other.unit;
            }
            return self.value == other.value && self.unit == other.unit;
        }

        public static bool operator !=(UIMeasurement self, UIMeasurement other) {
            return !(self == other);
        }

        public static implicit operator UIMeasurement(int value) {
            return new UIMeasurement(value, UIUnit.Pixel);
        }

        public static implicit operator UIMeasurement(float value) {
            return new UIMeasurement(value, UIUnit.Pixel);
        }

        public static implicit operator UIMeasurement(double value) {
            return new UIMeasurement((float) value, UIUnit.Pixel);
        }

    }


}