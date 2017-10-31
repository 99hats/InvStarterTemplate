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

    public LayoutPool LayoutPool { get; set; }

    public MainPage(Surface surface)
    {
      Base = surface.NewVerticalDock();
      LayoutPool = new LayoutPool(surface, 33, 11);

      ContentFrame = surface.NewFrame();
      Base.AddClient(ContentFrame);
      imageId = 0;

      surface.ArrangeEvent += UpdateUI;

      AddCells(33);

      void AddCells(int num)
      {
        for (int i = 0; i < num; i++)
        {
          imageId += 1;
          Debug.WriteLine($"add {imageId}");
          var graphic = new WebGraphic(surface, $"https://unsplash.it/300/200/?image={imageId}");
          graphic.Alignment.Stretch();
          graphic.Background.Colour = Colour.Red;;
          graphic.Margin.Set(4);
          LayoutPool.AddCellPanel(graphic);
        }
      }

      // layout
      //LayoutPool.Table = surface.NewTable();
     
      Scroll = surface.NewVerticalScroll();
      Scroll.Content = LayoutPool.Table;
      ContentFrame.Transition(Scroll);

      void UpdateUI()
      {
        if (surface.Window.Width < 1) return;
        AddCells(33);
        LayoutPool.Reclaim();
        LayoutPool.UpdateLayout();

        if (imageId > 99) imageId = 0;

        Debug.WriteLine($"{surface.Window.Application.Process.GetMemoryUsage().TotalMegabytes}mb {imageId}");
      }
    }

    public Frame ContentFrame { get; set; } // necessary?
    public Scroll Scroll { get; private set; }
    //public Table Table { get; set; }
    
  }

  /// <summary>
  /// Long-lived objects
  /// RowDocks contain frames which are loaded with CellPanels
  ///
  /// </summary>
  public class LayoutPool
  {
    public List<Frame> Frames { get; set; }
    public List<Panel> CellPanels { get; set; }
    public List<Dock> RowDocks { get; set; }
    public Table Table { get; set; }

    public Surface Surface { get; set; }
    private int Size;

    public LayoutPool(Surface surface, int size, int rows)
    {
      Surface = surface;
      
      Size = size;
      Frames = new List<Frame>(size);
      for (int i = 0; i < size; i++)
      {
        var frame = surface.NewFrame();
        Frames.Add(frame);
      }
      CellPanels = new List<Panel>(NextPrime(size * 2));
      RowDocks = new List<Dock>(NextPrime(rows));
      for (int i = 0; i < rows; i++)
      {
        var dock = surface.NewHorizontalDock();
        RowDocks.Add(dock);
      }
      BuildTable();
    }

    public void BuildTable()
    {
      Table = Surface.NewTable();
      var tableColumn = Table.AddColumn();
      tableColumn.Star();

      // 3 columns by 11 rows
      for (int r = 0; r < 11; r++)
      {
        var tableRow = Table.AddRow();
        tableRow.Auto();


        var row = RowDocks[r];
        for (int c = 0; c < 3; c++)
        {
          row.AddClient(Frames[r * 3 + c]);

        }
        //Table.AddPanel(row);
        Table.GetCell(0, r).Content = row;
      }
    }

    public void UpdateLayout()
    {
      for (int r = 0; r < 11; r++)
      {
        for (int c = 0; c < 3; c++)
        {
          var index = r * 3 + c;
          var frame = Frames[index];
          var cell = CellPanels[index];
          frame.Transition(cell).Fade();
        }
      }

    }

    public void AddCellPanel(WebGraphic cell)
    {
      CellPanels.Insert(0, cell);
    }

    public void Reclaim()
    {
      if (CellPanels.Count >= Size * 2)
      {
        Debug.WriteLine($"Reclaiming {CellPanels.Count}");
        //dispose
        for (int i = 33; i < CellPanels.Count - 33; i++)
          (CellPanels.ElementAt(i) as IDisposable)?.Dispose();
        //deref
        CellPanels.RemoveRange(Size, CellPanels.Count - Size);
        //reclaim
        Surface.Window.Application.Process.MemoryReclamation();
      }
    }

    private static int NextPrime(int a)
    {
      while (true) { a++; if (a < 2) continue; if (a == 2) break; if (a % 2 == 0) continue; bool flag = false; for (int i = 3; (i * i) <= a; i += 2) { if (a % i == 0) { flag = true; break; } } if (!flag) break; }
      return a;
    }
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
    public Background Background => Base.Background;

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
    }
  }
}
