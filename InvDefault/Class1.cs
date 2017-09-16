using Inv;
using System.Collections.Generic;

namespace InvDefault
{
  public static class Shell
  {
    public static void Install(Inv.Application Application)
    {
      Application.Title = "My Project";

      var Surface = Application.Window.NewSurface();
      Surface.Background.Colour = Colour.WhiteSmoke;

      var pageDock = Surface.NewVerticalDock();
      

      var tabsDock = Surface.NewHorizontalDock();
      tabsDock.Alignment.CenterStretch();
      tabsDock.Background.Colour = Colour.Black;

      var horizontalScroll = Surface.NewHorizontalScroll();
      horizontalScroll.Content = tabsDock;

      

      pageDock.AddClient( horizontalScroll);
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
