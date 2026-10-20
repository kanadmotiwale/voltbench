namespace VoltBench.Core.Sequencing;

public enum StepAction
{
    SetVoltage,
    SetCurrentLimit,
    SetOutput,
    Wait,
    MeasureDcVoltage,
    MeasureDcCurrent,
    MeasureResistance,
    MeasureSupplyVoltage,
    MeasureSupplyCurrent,
}

public static class StepActionExtensions
{
    public static bool IsMeasurement(this StepAction action) => action is
        StepAction.MeasureDcVoltage or StepAction.MeasureDcCurrent or StepAction.MeasureResistance or
        StepAction.MeasureSupplyVoltage or StepAction.MeasureSupplyCurrent;

    public static string Unit(this StepAction action) => action switch
    {
        StepAction.SetVoltage or StepAction.MeasureDcVoltage or StepAction.MeasureSupplyVoltage => "V",
        StepAction.SetCurrentLimit or StepAction.MeasureDcCurrent or StepAction.MeasureSupplyCurrent => "A",
        StepAction.MeasureResistance => "Ohm",
        StepAction.Wait => "ms",
        _ => string.Empty,
    };
}
