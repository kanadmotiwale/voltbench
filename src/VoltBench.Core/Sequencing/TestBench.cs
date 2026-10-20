using VoltBench.Core.Instruments;

namespace VoltBench.Core.Sequencing;

public sealed record TestBench(IPowerSupply PowerSupply, IMultimeter Multimeter);
