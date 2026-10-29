using System.Collections.Generic;
using NihongoLife.Course;
using UnityEngine;

namespace NihongoLife.UI
{
    /// <summary>One tab of the school popup: owns a rectangle, draws lists/details into it, and can swap itself for a quiz.</summary>
    public abstract class CourseTabView
    {
        protected readonly CoursePopup Host;
        protected RectTransform Area;
        protected float W, H;

        protected CourseTabView(CoursePopup host) { Host = host; }

        public CourseQuizView Quiz { get; protected set; }
        public bool InQuiz => Quiz != null;

        public void Attach(RectTransform area, float w, float h) { Area = area; W = w; H = h; }

        /// <summary>Draws the tab's normal screen (clears the area first).</summary>
        public void Show() { Quiz = null; UiKit.Clear(Area); Draw(); }

        protected abstract void Draw();

        /// <summary>Redraws whatever is showing (the language changed, or the learning language switched).</summary>
        public virtual void Redraw()
        {
            if (Quiz != null) Quiz.Draw(); else { UiKit.Clear(Area); Draw(); }
        }

        /// <summary>Esc / back: leaves a quiz or a sub-screen. True when it did something.</summary>
        public virtual bool HandleBack()
        {
            if (Quiz == null) return false;
            CloseQuiz();
            return true;
        }

        public virtual void Tick(float dt) { Quiz?.Tick(dt); }

        protected void StartQuiz(List<CourseQuestion> questions, CourseQuizOptions options)
        {
            if (questions == null || questions.Count == 0) return;
            options.OnExit = CloseQuiz;
            if (string.IsNullOrEmpty(options.Target)) options.Target = Host.Target;
            UiKit.Clear(Area);
            Quiz = new CourseQuizView(Area, W, H, questions, options);
            Quiz.Start();
        }

        protected virtual void CloseQuiz() { Quiz = null; UiKit.Clear(Area); Draw(); }
    }
}