using Inv;
using System.Collections.Generic;
using System.Diagnostics;

namespace InvDefault
{
  public static class Shell
  {
    public static void Install(Inv.Application Application)
    {
      Application.Title = "My Project";

      var Surface = Application.Window.NewSurface();
      Surface.Background.Colour = Colour.WhiteSmoke;

      Application.StartEvent += () =>
      {
        Application.Audio.Play(InvMedia.Resources.audio.CoolSmsTone, 1.00f, 1.00f);
      };
      var pageDock = Surface.NewVerticalDock();
      

      var tabsDock = Surface.NewHorizontalDock();
      tabsDock.Alignment.CenterStretch();
      tabsDock.Background.Colour = Colour.Black;

      var horizontalScroll = Surface.NewHorizontalScroll();
      horizontalScroll.Content = tabsDock;

      Colour colour = Colour.FromArgb(0, 100, 100, 100);

      pageDock.AddClient( horizontalScroll);
      var mpoint = new Point(100,100);
      var msize = new Point(20,20);
      var canvas = Surface.NewCanvas();
      canvas.Background.Colour = Colour.Black;
      canvas.DrawEvent += (DC) =>
      {
        colour = Colour.FromArgb(0, (byte)mpoint.X, (byte)mpoint.Y, 100);
        DC.DrawEllipse(Colour.FromArgb((byte)255, (byte)mpoint.X, (byte)mpoint.Y, 100), Colour.White, 2,mpoint, msize );
      };
      canvas.Draw();
      canvas.MoveEvent += point =>
      {
        mpoint = point;
        msize = new Point(30,30);
        
        canvas.Draw();
        Debug.WriteLine($" {point.X}x{point.Y}");
      };
      canvas.PressEvent += point => { mpoint = point; canvas.Draw(); };
      canvas.ReleaseEvent += point => { mpoint = new Point(100, 100); msize = new Point(20,20); canvas.Draw(); };
      pageDock.AddClient(canvas);

      var tabTexts = new List<string> { "Recent Posts", "Current Issue", "Events Calendar" };

      Label current = null;

      for (var index = 0; index < tabTexts.Count; index++)
      {

        var sep = Surface.NewFrame();
        sep.Border.Set(1, 0, 0, 0);
        sep.Margin.Set(0, 4);
        sep.Border.Colour = Colour.Gray;

        var tabText = tabTexts[index];
        var tabLabel = Surface.NewLabel();
        tabLabel.Text = tabText;
        tabLabel.Font.Colour = Colour.White;
        tabLabel.Font.Size = 24;
        tabLabel.Padding.Set(16, 4);
       
        if (index ==0 )
        {
          tabLabel.Border.Colour = Colour.Red;
          tabLabel.Border.Set(0, 0, 0, 3);
          current = tabLabel;
        }

        var tabButton = Surface.NewButton();
        tabButton.SingleTapEvent += () =>
        {
          var thisLabel = tabButton.Content as Label;
          thisLabel.Border.Colour = Colour.Red;
          tabLabel.Border.Set(0,0,0,3);
          current.Border.Set(0);
          current = thisLabel;
        };
        tabButton.Content = tabLabel;

        tabsDock.AddHeader(tabButton);
        if (index < tabTexts.Count) tabsDock.AddHeader(sep);

      }





      Surface.Content = pageDock;
      Application.Window.Transition(Surface);
    }
  }


}
