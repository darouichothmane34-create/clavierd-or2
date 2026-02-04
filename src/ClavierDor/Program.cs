using System;
using System.Windows.Forms;

namespace ClavierDor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var context = new ClavierDorContext();
        context.Database.EnsureCreated();
        SeedData.EnsureSeeded(context);
        Application.Run(new MainForm(new GameService(context)));
    }
}
