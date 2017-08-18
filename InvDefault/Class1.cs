﻿using Inv;

namespace InvDefault
{
    public static class Shell
    {
        public static void Install(Inv.Application Application)
        {
            Application.Title = "My Project";

            var Surface = Application.Window.NewSurface();
            Surface.Background.Colour = Colour.WhiteSmoke;
            var Label = Surface.NewLabel();
            Surface.Content = Label;
            Label.Alignment.Center();
            Label.Font.Size = 20;
            Label.Text = $"Howdy";

            Application.Window.Transition(Surface);
        }
    }
}
