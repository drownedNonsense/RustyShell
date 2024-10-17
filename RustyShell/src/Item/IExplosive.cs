using Vintagestory.API.Common;

namespace RustyShell;
public interface IExplosive {
    EnumExplosiveType Type              { get; }
    bool               IsFragmentation  { get; }
    bool               IsSubmunition    { get; }
    float              Damage           { get; }
    int?               BlastRadius      { get; }
    int?               InjureRadius     { get; }
    AssetLocation      SubExplosive     { get; }
    float?             FlightExpectancy { get; }
} // interface ..
