using System;
using System.Numerics;
using Dalamud.Interface.Windowing;

namespace SkillFinder.Windows;

public class MainWindow : Window, IDisposable
{
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow()
        : base("Skill Finder###SkillFinder")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 600),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
    }
}
