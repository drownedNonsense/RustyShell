using Vintagestory.API.Common;

namespace RustyShell;
public interface IExplosive {
    EnumExplosiveType Type                  { get; }
    bool               IsFragmentation      { get; }
    bool               IsSubmunition        { get; }
    bool               CanBounce            { get; }
    float              Damage               { get; }
    int?               BlastRadius          { get; }
    int?               InjureRadius         { get; }
    AssetLocation      SubExplosive         { get; }
    int?               SubExplosiveCount    { get; }
    int?               FragmentationConeDeg { get; }
    float?             FlightExpectancy     { get; }
} // interface ..
