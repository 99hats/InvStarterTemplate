using Inv;
using System.Collections.Generic;
using System.Diagnostics;

namespace InvDefault
{
  public static class Shell
  {
    public static void Install(Application Application)
    {
      Application.Title = "My Project";
      var Surface = Application.Window.NewSurface();
      Surface.Background.Colour = Colour.WhiteSmoke;

      var main = new MainPage(Surface);
      Surface.Content = main;
      Application.Window.Transition(Surface);
    }
  }

  internal sealed class MainPage : Mimic<Dock>
  {
    public MainPage(Surface surface)
    {
      Base = surface.NewVerticalDock();
      ContentFrame = surface.NewFrame();
      Base.AddClient(ContentFrame);

      Cells = new List<Button>(36);

      surface.ArrangeEvent += UpdateUI;

      // content pool
      for (int i = 0; i < 11; i++)
      {
        AddRow(Colour.DodgerBlue, 300);
      }


      void AddRow(Colour colour, int height)
      {
        for (int i = 0; i < 3; i++)
        {
          var button = surface.NewButton();
          button.Size.SetHeight(height);
          button.Alignment.Stretch();
          button.Background.Colour = colour;
          button.Margin.Set(4);
          Cells.Insert(0, button);
          if (Cells.Count >= 36)
            Cells.RemoveRange(33,3);
        }
       
      }

      // layout pool
      Stack = surface.NewVerticalStack();
      Rows = new List<Stack>();

      Grid = new Frame[3, 11];

      for (int r = 0; r < 11; r++)
      {
        var row = surface.NewHorizontalDock();
        for (int c = 0; c < 3; c++)
        {
          var frame = surface.NewFrame();
          Grid[c, r] = frame;
          row.AddClient(frame);
        }
        Stack.AddPanel(row);
      }
      Scroll = surface.NewVerticalScroll();
      Scroll.Content = Stack;
      ContentFrame.Transition(Scroll);

      void UpdateUI()
      {
        if (surface.Window.Width < 1) return;
        
        for (int r = 0; r < 11; r++)
        {
          for (int c = 0; c < 3; c++)
          {
            var index = r * 3 + c;
            var gcell = Grid[c, r];
            var cell = Cells[index];
            Debug.WriteLine($"index {index}");
            gcell.Transition(cell).Fade();
          }
        }
        AddRow(Colour.Goldenrod, 300);
      }
    }

    public Stack Stack { get; set; }

    public List<Stack> Rows { get; set; }

    public Frame[,] Grid { get; set; }

    public List<Button> Cells { get; set; }

    public Frame ContentFrame { get; set; }

    public Scroll Scroll { get; private set; }
  }
}
