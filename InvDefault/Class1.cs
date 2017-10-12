using Inv;

namespace InvDefault
{
  public static class Shell
  {
    public static void Install(Inv.Application Application)
    {
      Application.Title = "My Project";
      var Surface = new NavigationSurface(Application);
      Application.Window.Transition(Surface);
    }
  }

  internal sealed class NavigationSurface : Inv.Mimic<Surface>
  {
    public Scroll DrawerScroll { get; set; }
    public Frame DefaultLayer { get; set; }
    public Frame PopUpLayer { get; set; }
    public Surface Surface => this.Base;

    public NavigationSurface(Application application)
    {
      Base = application.Window.NewSurface();
      var mainOverlay = Base.NewOverlay();
      DefaultLayer = Base.NewFrame();
      PopUpLayer = Base.NewFrame();

      mainOverlay.AddPanel(DefaultLayer);
      mainOverlay.AddPanel(PopUpLayer);
      Base.Content = mainOverlay;

      var label = Base.NewLabel();
      label.Background.Colour = Colour.DodgerBlue;
      label.Padding.Set(8);
      label.Font.Size = 24;
      label.Font.Colour = Colour.White;
      label.Alignment.Center();
      label.JustifyCenter();
      label.Text = "Click Me";

      var button = Base.NewButton();
      button.Content = label;
      button.Alignment.Stretch();
      button.Background.Colour = Colour.WhiteSmoke;
      button.SingleTapEvent += () =>
      {
        var popup = new PopUpGraphic(this, InvMedia.Resources.Images.Logo);
        PopUpLayer.Transition(popup).Fade();
      };
      DefaultLayer.Transition(button);
    }
  }

  internal sealed class PopUpGraphic : Inv.Mimic<Overlay>
  {
    public PopUpGraphic(NavigationSurface navigationSurface, Image image)
    {
      var surface = navigationSurface.Surface;
      Base = surface.NewOverlay();
      var scrim = surface.NewFrame();
      scrim.Alignment.Stretch();
      scrim.Background.Colour = Colour.Black;
      scrim.Opacity.Set(1f);
      Base.AddPanel(scrim);

      var graphic = surface.NewGraphic();
      graphic.Image = image;
      graphic.Alignment.Center();
      graphic.Margin.Set(24);
      Base.AddPanel(graphic);

      var button = surface.NewButton();
      button.Alignment.Stretch();
      button.SingleTapEvent += () =>
      {
        navigationSurface.PopUpLayer.Transition(null).Fade();
      };
      Base.AddPanel(button);
    }
  }
}
