namespace TaskPin.Models;

public sealed class AppSettings
{
    public string StartPosition { get; set; } = "TopRight";
    public double Opacity { get; set; } = 0.96;
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
}