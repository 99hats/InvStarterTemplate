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

      var pageDock = Surface.NewVerticalDock();
      




      Surface.Content = pageDock;
      Application.Window.Transition(Surface);
    }
  }


}
