using Inv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace InvDefault
{
  public static class Shell
  {
    public static Folder cacheFolder { get; set; }

    public static void Install(Application Application)
    {
      Application.Title = "My Project";
      var Surface = Application.Window.NewSurface();
      Surface.Background.Colour = Colour.WhiteSmoke;
      cacheFolder = Application.Directory.NewFolder("cache");

      var main = new MainPage(Surface);
      Surface.Content = main;
      Application.Window.Transition(Surface);
    }
  }
  internal sealed class MainPage : Mimic<Dock>
  {
    private int imageId;

    public MainPage(Surface surface)
    {
      Base = surface.NewVerticalDock();
      ContentFrame = surface.NewFrame();
      Base.AddClient(ContentFrame);
      imageId = 0;

      Cells = new List<WebGraphic>(99);

      surface.ArrangeEvent += UpdateUI;

      // content pool
      for (int i = 0; i < 11; i++)  // add 1 full page
      {
        AddRow();
      }

      void AddRow()
      {
        for (int i = 0; i < 3; i++)
        {
          imageId += 1;
          var graphic = new WebGraphic(surface, $"https://unsplash.it/300/200?image={imageId}");
          graphic.Alignment.Stretch();
          graphic.Margin.Set(4);
          Cells.Insert(0, graphic);
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

        // add 33 more photos (1 full page) 
        for (int i = 0; i < 11; i++)
        {
          AddRow();
        }

        // aggressive attempts at memory reclamaion
        // dispose, deref and reclaim

        // when adding 1 row (3 graphics at a time)
        // 600x400 50+ rotations
        // 
        // when adding 11 rows (1 pages worth)
        // 300x200 23 rotations with no dispose, deref or reclaim
        // 300x200 23 rotations with dispose, deref, no reclaim
        // 300x200 23 rotations with reclaim no dispose, deref
        // 300x200 75+ (I gave up trying to crash it) with dispose, deref and reclaim
        // 600x400 6 rotations with dispose, deref and reclaim (for high dpi screens)

        if (Cells.Count >= 66)
        {
          //dispose
          for (int i = 33; i < Cells.Count - 33; i++)
          {
            (Cells.ElementAt(i) as IDisposable)?.Dispose(); // same as Base.Image = null
          }
          //deref
          Cells.RemoveRange(33, Cells.Count - 33); 
        }
        // reclaim
        surface.Window.Application.Process.MemoryReclamation();

        for (int r = 0; r < 11; r++)
        {
          for (int c = 0; c < 3; c++)
          {
            var index = r * 3 + c;
            var gcell = Grid[c, r];
            var cell = Cells[index];
            gcell.Transition(cell).Fade();
          }
        }

        if (imageId > 99) imageId = 0;

        Debug.WriteLine($"{surface.Window.Application.Process.GetMemoryUsage().TotalMegabytes}mb {imageId}");
      }
    }

    public List<WebGraphic> Cells { get; set; }
    public Frame ContentFrame { get; set; }
    public Frame[,] Grid { get; set; }
    public List<Stack> Rows { get; set; }
    public Scroll Scroll { get; private set; }
    public Stack Stack { get; set; }
  }

  public class WebGraphic : Mimic<Graphic>, IDisposable

  {
    public WebGraphic(Surface surface, string uri)

    {
      var application = surface.Window.Application;
      Base = surface.NewGraphic();
      var cacheFileName = GetMD5Hash(uri) + ".jpg";
      var cache = Shell.cacheFolder.NewFile(cacheFileName);

      if (cache.Exists())

      {
        var thebytes = cache.ReadAllBytes();
        var image = new Image(thebytes, ".jpg");
        Base.Image = image;
        var dimensions = application.Graphics.GetDimension(Base.Image);
        aspectRatio = dimensions.Height / (float)dimensions.Width;
        var FadeInAnimation = surface.NewAnimation();
        var target = FadeInAnimation.AddTarget(Base);
        target.FadeOpacityIn(TimeSpan.FromSeconds(1));
        FadeInAnimation.Start();
        return;
      }

      surface.Window.RunTask(Thread =>

      {
        using (var download = application.Web.GetDownload(new Uri(uri)))
        using (var memoryStream = new MemoryStream((int)download.Length))
        {
          download.Stream.CopyTo(memoryStream);
          memoryStream.Flush();
          var image = new Image(memoryStream.ToArray(), ".png");
          var newCacheFile = Shell.cacheFolder.NewFile(cacheFileName);
          newCacheFile.WriteAllBytes(memoryStream.ToArray());
          Thread.Post(() =>
          {
            Base.Image = image;
            var dimensions = application.Graphics.GetDimension(Base.Image);
            aspectRatio = dimensions.Height / dimensions.Width;
            Base.Readjust();
          });
        }
      });
    }

    public Alignment Alignment => Base.Alignment;
    public double? aspectRatio { get; set; }
    public Margin Margin => Base.Margin;
    public Size Size => Base.Size;

    public static String GetMD5Hash(String TextToHash)

    {
      MD5 md5 = new MD5CryptoServiceProvider();
      byte[] textToHash = Encoding.Default.GetBytes(TextToHash);
      byte[] result = md5.ComputeHash(textToHash);
      return BitConverter.ToString(result);
    }

    public int aspectHeight(int width) => (int)(aspectRatio * width);

    public void Dispose()
    {
      Base.Surface.Window.Call(() => { Base.Image = null; });
      GC.SuppressFinalize(this);
    }
  }
}
