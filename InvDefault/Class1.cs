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

  public static class Models
  {
    public static int last { get; set; } = 0;

    public class Asset
    {
      public string title { get; set; }
      public string uri { get; set; }

      public static List<Asset> CreateAssets(int num)
      {
        List<Asset> result = new List<Asset>();
        for (int i = 0; i < num; i++)
        {
          result.Add(new Asset { title = $"{Colour.All[i + last].ToString()} {i + last}", uri = $"http://picsum.photos/300/200/?image={i + last}" });
        }
        Debug.WriteLine($"last {last}");
        last += num;
        if (last > 99) last = 0;
        return result;
      }
    }
  }

  public class AssetPanel : Mimic<Button>, IDisposable
  {
    public Surface Surface { get; }
    public Models.Asset Asset { get; set; }

    public AssetPanel(Surface surface, Models.Asset asset)
    {
      Surface = surface;
      Base = surface.NewButton();
      Asset = asset;

      WebGraphic = new WebGraphic(surface, Asset.uri);
      WebGraphic.Alignment.Stretch();

      var title = surface.NewLabel();
      title.Text = Asset.title;

      var verticalDock = surface.NewVerticalDock();
      verticalDock.Alignment.Stretch();
      verticalDock.AddHeader(WebGraphic);
      verticalDock.AddClient(title);
      Base.Content = verticalDock;

      surface.ArrangeEvent += OnAdjustEvent;

      void OnAdjustEvent()
      {
        if (surface.Window.Width < 1) return;
        var width = surface.Window.Width / 3;
        var height = WebGraphic.aspectHeight(width);
        WebGraphic.Size.Set(surface.Window.Width / 3, height);
        verticalDock.Readjust();
      };
    }

    public WebGraphic WebGraphic { get; set; }

    public Size Size => Base.Size;
    public Size GraphicSize => WebGraphic.Size;
    public Alignment Alignment => Base.Alignment;
    public Background Background => Base.Background;
    public Margin Margin => Base.Margin;

    public void Readjust() => Base.Readjust();

    public void Dispose()
    {
      WebGraphic.Dispose();
    }
  }


  /// <summary>
  /// Caching safety harness... encapuslate a lot of steps.
  /// </summary>
  public class LayoutPoolTwo
  {
    private readonly Surface _surface;
    private readonly int _cols;
    private readonly int _rows;
    private int _matrixSize;

    private List<Dock> Rows { get; set; } = new List<Dock>(13);
    private List<Frame> Frames { get; set; } = new List<Frame>(71);
    public List<AssetPanel> Cells { get; set; } = new List<AssetPanel>(71);
    public Stack PageLayout { get; set; } = null;

    public LayoutPoolTwo(Surface surface, int cols, int rows, IEnumerable<AssetPanel> assetPanels)
    {
      _surface = surface;
      _cols = cols;
      _rows = rows;
      _matrixSize = cols * rows;

      Cells.AddRange(assetPanels);
    }

    /// <summary>
    /// Possible candidate for fluent api
    /// </summary>
    public void Init()
    {
      Debug.Assert(Cells.Count > 0); // must populate cells before init, should probably include that in the ctor

      // init frames
      for (var i = 0; i < _matrixSize; i++)
        Frames.Add(_surface.NewFrame());

      // init rows
      for (var r = 0; r < _rows; r++)
        Rows.Add(_surface.NewHorizontalDock());

      // init layout
      PageLayout = _surface.NewVerticalStack();
      for (int r = 0; r < _rows; r++)
      {
        for (int c = 0; c < _cols; c++)
        {
          var index = r * _cols + c;
          Debug.WriteLine($"index {index} / {r}");
          Frames[index].Transition(Cells[index]).Fade();
          Rows[r].AddClient(Frames[index]);
        }
        PageLayout.AddPanel(Rows[r]);
      }
    }

    public void Update()
    {
      for (int i = 0; i < Frames.Count; i++)
      {
        var cell = Cells[i];
        Frames[i].Transition(cell).Fade();
      }
    }
    /// <summary>
    /// Call after update
    /// </summary>
    public void Prune()
    {
      if (Cells.Count > _matrixSize * 2)
      {
        Debug.WriteLine($"Reclaiming at {Cells.Count}");
        //dispose
        //Cells.Skip(matrixSize* 2).ForEach(x => (x as IDisposable)?.Dispose());
        for (int i = _matrixSize; i < Cells.Count; i++)
        {
          Debug.WriteLine($"Dispose {i}");
          Cells[i].Dispose();
        }
        //deref
        Cells.RemoveRange(_matrixSize, Cells.Count - _matrixSize);
        //reclaim
        _surface.Window.Application.Process.MemoryReclamation();
      }

      //Models.Asset.CreateAssets(6)
      //  .ForEach(x => Cells.Insert(0, new AssetPanel(_surface, x)));
    }
  }

  internal sealed class MainPage : Mimic<Stack>
  {
    private int imageCounter;

    private List<Models.Asset> Assets { get; set; } = new List<Models.Asset>();
    private List<Dock> Rows { get; set; } = new List<Dock>(13);
    private List<Frame> Frames { get; set; } = new List<Frame>(71);
    private List<AssetPanel> Cells { get; set; } = new List<AssetPanel>(71);
    private Stack PageLayout { get; set; } = null;

    private LayoutPoolTwo LayoutPool { get; set; }

    public MainPage(Surface surface)
    {
      Base = surface.NewVerticalStack();
      var cols = 3;
      var rows = 11;
      var matrixSize = rows * cols;

      var cells = Models.Asset.CreateAssets(33)
        .Select(x => new AssetPanel(surface, x));

      LayoutPool = new LayoutPoolTwo(surface, cols, rows, cells);

      Header = surface.NewFrame();
      Header.Size.SetHeight(80);
      Header.Alignment.Stretch();
      Header.Background.Colour = Colour.DodgerBlue;
      Base.AddPanel(Header);

      Scroll = surface.NewVerticalScroll();
      Base.AddPanel(Scroll);

      //// init assets
      //Models.Asset.CreateAssets(33)
      //  .ForEach(x => LayoutPool.Cells.Insert(0, new AssetPanel(surface, x)));



      LayoutPool.Init();

      //// init frames
      //for (var i = 0; i < matrixSize; i++)
      //  Frames.Add(surface.NewFrame());

      //// init rows
      //for (var r = 0; r < rows; r++)
      //  Rows.Add(surface.NewHorizontalDock());

      //// init layout
      //PageLayout = surface.NewVerticalStack();
      //for (int r = 0; r < rows; r++)
      //{
      //  for (int c = 0; c < cols; c++)
      //  {
      //    var index = r * cols + c;
      //    Debug.WriteLine($"index {index} / {r}");
      //    Frames[index].Transition(Cells[index]).Fade();
      //    Rows[r].AddClient(Frames[index]);
      //  }
      //  PageLayout.AddPanel(Rows[r]);
      //}

      Scroll.Content = LayoutPool.PageLayout;

      surface.ArrangeEvent += () =>
      {
        surface.Window.Post(() =>
        {

          Models.Asset.CreateAssets(3)
            .ForEach(x => LayoutPool.Cells.Insert(0, new AssetPanel(surface, x)));
          LayoutPool.Update();
          LayoutPool.Prune();
        });


      };
    }

    public Frame Header { get; set; }

    private Scroll Scroll { get; }
  }

  public class LayoutPool
  {
    private readonly int Size;

    public LayoutPool(Surface surface, int size, int rows, int cols)
    {
      Surface = surface;

      Size = size;
      Rows = rows;
      Cols = cols;
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
    }

    private List<Panel> CellPanels { get; }
    private int Cols { get; }
    private List<Frame> Frames { get; }
    private List<Dock> RowDocks { get; }
    private int Rows { get; }
    private Surface Surface { get; }

    public void AddCellPanel(Panel cell) => CellPanels.Insert(0, cell);

    public void Reclaim()
    {
      if (CellPanels.Count >= Size * 2)
      {
        Debug.WriteLine($"Reclaiming at {CellPanels.Count}");
        //dispose
        for (int i = 33; i < CellPanels.Count - 33; i++)
          (CellPanels.ElementAt(i) as IDisposable)?.Dispose();
        //deref
        CellPanels.RemoveRange(Size, CellPanels.Count - Size);
        //reclaim
        Surface.Window.Application.Process.MemoryReclamation();
      }
    }

    public void UpdateLayout()
    {
      for (int i = 0; i < Size; i++)
      {
        var frame = Frames[i];
        var cell = CellPanels[i];
        frame.Transition(cell).Fade();
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
        Base.Readjust();
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
    public Background Background => Base.Background;
    public Margin Margin => Base.Margin;
    public Size Size => Base.Size;

    public static String GetMD5Hash(String TextToHash)

    {
      MD5 md5 = new MD5CryptoServiceProvider();
      byte[] textToHash = Encoding.Default.GetBytes(TextToHash);
      byte[] result = md5.ComputeHash(textToHash);
      return BitConverter.ToString(result);
    }

    public int aspectHeight(int width)
    {
      var d = aspectRatio * width;
      if (d != null) return (int)d;
      return 0;
    }

    public void Dispose()
    {
      // important: use post not call here
      Base.Surface.Window.Post(() => { Base.Image = null; });
    }
  }
}