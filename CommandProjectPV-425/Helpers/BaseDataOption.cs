namespace CommandProjectPV_425.Helpers;

public abstract class BaseDataOption
{
    public int Value { get; set; }
    public string DisplayName { get; set; }
    public override string ToString() => DisplayName; // для отображения в ComboBox
}
