using Inv;
using Newtonsoft.Json;
using System;
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

      var pageDock = Surface.NewVerticalDock();

      var canvas = Surface.NewCanvas();
      canvas.Background.Colour = Colour.Black;

      Animator animator = null;
      Stopwatch stopwatch = null;

      canvas.DrawEvent += DC =>
      {
        if (stopwatch == null)
          stopwatch = Stopwatch.StartNew();

        if (animator == null)
          animator = Animator
            .CreateEllipse(Colour.FromHSV(210, 0.88, 1.0), Colour.White, 2, new Point(100, 100), new Point(20, 20))
            .SetStartTime(stopwatch.Elapsed.TotalSeconds)
            .Grow(20,600, 5);

        animator
          .Update(DC, stopwatch.Elapsed.TotalSeconds);
      };
      pageDock.AddClient(canvas);
      Surface.ComposeEvent += () => canvas.Draw();

      Surface.Content = pageDock;
      Application.Window.Transition(Surface);
    }
  }

  //public class Animateable
  //{
  //  public Animator.States States { get; set; }
  //  public double StartTime { get; set; }
  //  public double EndTime { get; set; }
  //}

  public class Drawable
  {
    public Colour Fill { get; set; }
    public Colour Stroke { get; set; }
    public int StrokeWidth { get; set; }
    public Point Position { get; set; }
    public Point Size { get; set; }

    public Drawable Original { get; set; }

    public Action DrawEvent { get; set; }
  }

  public class EllipseDrawable : Mimic<Drawable>
  {
    public EllipseDrawable(Colour fill, Colour stroke, int strokeWidth, Point position, Point size)
    {
      Base = new Drawable
      {
        Fill = fill,
        Stroke = stroke,
        StrokeWidth = strokeWidth,
        Position = position,
        Size = size
      };
      Base.Original = Base;
    }

    public event Action DrawEvent
    {
      add => Base.DrawEvent += value;
      remove => Base.DrawEvent -= value;
    }
  }

  public class Animator
  {
    public static DrawContract DrawContract;

    public enum DrawType
    {
      line,
      ellipse,
      rectangle,
      text,
      image
    }

    public enum States
    {
      created,
      running,
      cancelled,
      paused,
      completed
    }

    public static States State;

    public static Drawable Drawable { get; set; }

    public static List<Animation> Animations;

    public static double StartTime { get; set; }
    public static double CurrentTime { get; set; }

    private static double _timeDelta;
    public int count;

    public static List<Action> actions;
    private static DrawType _drawType;

    public static float Lerp(float a, float b, float f)
    {
      return (a * (1.0f - f)) + (b * f);
    }

    public static Animator CreateEllipse(Colour fill, Colour stroke, int strokeWidth, Point position, Point size)
    {
      actions = new List<Action>();
      Animations = new List<Animation>();
      Drawable = new EllipseDrawable(fill, stroke, strokeWidth, position, size);
      Drawable.DrawEvent += DrawEllipseEvent;
      return new Animator();
    }

    private static void DrawEllipseEvent()
    {
      var d = Drawable;
      DrawContract.DrawEllipse(d.Fill, d.Stroke, d.StrokeWidth, d.Position, d.Size);
    }

    public class Animation
    {
      public States States { get; set; }
      public double? AnimationStartTime { get; set; }
      public double? AnimationEndTime => AnimationStartTime + Duration;
      public double Duration { get; set; }

      public Animation(States state, double duration)
      {
        States = state;
        AnimationStartTime = StartTime;
        Duration = duration;
      }

      public Action AnimateEvent;

      public bool isOverDuration => CurrentTime > AnimationEndTime;

      public double Progress()
      {
        if (AnimationStartTime == null) return 0d;
        var progress = CurrentTime - AnimationStartTime;
        if (progress < 0) return 0d;
        if (progress > Duration) return 1d;
        return (double)(progress / Duration);
      }

      public double Lerp(float a, float b)
      {
        var progress = Progress();
        //var timeDelta = (int)(DateTime.UtcNow - AnimationStates.EllipseStruct.Start).TotalSeconds;
        //var elapsed = Stopwatch.Elapsed.TotalMilliseconds;
        //var perc = (float) Stopwatch.Elapsed.TotalSeconds / seconds;
        //var t = (float)Math.Sin(perc * Math.PI * 0.5f);
        //var t = 1f - (float) Math.Cos(perc * Math.PI * 0.5f);
        //var t = perc * perc;
        var t = (float)Math.Abs(progress);
        t = t * t * (3f - 2f * t);
        //t = t * t * t * (t * (6f * t - 15f) + 10f);
        var size = Animator.Lerp(a, b, t);
        return size;
      }
    }

    // chaining functions
    public Animator SetStartTime(double startTime)
    {
      StartTime = startTime;
      return this;
    }

    //public Animator FadeOut(int seconds)
    //{
    //  actions.Add(OnFadeOut);
    //  return this;

    //  void OnFadeOut()
    //  {
    //    double diff;
    //    if (_timeDelta > seconds)
    //    {
    //      var mult = (int)(_timeDelta / seconds);
    //      diff = _timeDelta - (mult * seconds);
    //    }
    //    else
    //    {
    //      diff = _timeDelta;
    //    }
    //    var perc = diff / (double)seconds;
    //    var value = Lerp(1.0f, 0f, (float)perc);
    //    Debug.WriteLine($"value {(float)value} {(float)perc} {_timeDelta} > {diff}");
    //    AnimationStates.EllipseDrawable.Fill = Colour.FromHSV((double)210, (double)0.8, (double)value);
    //    AnimationStates.EllipseDrawable.Stroke = Colour.FromHSV(0d, 0.0d, (double) value);
    //  }

    //}

    public Animator Grow(int startsize, int maxsize, int duration)
    {
      var animation = new Animation(States.running, duration);
      Drawable.Size = new Point((int)startsize, (int)startsize);
      animation.AnimateEvent += OnGrow;
      Animations.Add(animation);
      return this;

      void OnGrow()
      {
        if (animation.AnimationStartTime == null || animation.isOverDuration) animation.AnimationStartTime = CurrentTime;

        if (Drawable.Size.X > maxsize)
        {
          // reset
          animation.AnimationStartTime = CurrentTime;
        }
        var size = animation.Lerp((float)startsize, (float)maxsize);
        Drawable.Size = new Point((int)size, (int)size);
      }
    }

    // ending functions
    public Animator Update(DrawContract dc, double timeDelta)
    {
      DrawContract = dc;
      CurrentTime = timeDelta;

      foreach (var animation in Animations)
      {
        animation.AnimateEvent.Invoke();
      }

      Drawable.DrawEvent.Invoke();
      return this;
    }

    public Animator Stop()
    {
      State = States.cancelled;
      return this;
    }
  }

  public static class Extensions
  {
    public static T Clone<T>(this T source)
    {
      var serialized = JsonConvert.SerializeObject(source);
      return JsonConvert.DeserializeObject<T>(serialized);
    }
  }
}
