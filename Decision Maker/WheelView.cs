using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Animation;
using Android.Views.Animations;
using Android.Widget;

namespace Decision_Maker
{
    [Register("Decision_Maker.WheelView")]
    public class WheelView : View
    {
        private Paint paint;
        private Paint textPaint;
        private RectF rect;
        private readonly Random random = new Random();
        private string[] segments = { "Yes", "No" };
        private float rotationDegrees;
        private ValueAnimator? spinAnimator;
        private ValueAnimator? selectionAnimator;
        private int selectedIndex = -1;
        private float selectionPulse;

        private readonly Color[] baseColors =
        {
            Color.ParseColor("#D7263D"), Color.ParseColor("#161A1D"),
            Color.ParseColor("#F4B942"), Color.ParseColor("#8D99AE"),
            Color.ParseColor("#A4161A"), Color.ParseColor("#2B2D42")
        };

        public IReadOnlyList<string> Segments => segments;

        public event EventHandler<string>? DecisionSelected;
        public event EventHandler<int>? SegmentClicked;

        public int ItemCount => segments.Length;

        public void SetSegments(IList<string> values)
        {
            if (values == null || values.Count == 0)
                return;

            var cleanedValues = new List<string>();
            foreach (var value in values)
            {
                var text = value?.Trim();
                if (!string.IsNullOrEmpty(text))
                    cleanedValues.Add(text);
            }

            if (cleanedValues.Count == 0)
                return;

            segments = cleanedValues.ToArray();
            Invalidate();
        }

        public void Spin()
        {
            if (segments.Length == 0 || spinAnimator?.IsRunning == true)
                return;

            var selectedIndex = random.Next(segments.Length);
            var anglePerItem = 360f / segments.Length;
            const float pointerAngle = -90f;
            var selectedSegmentCenter = selectedIndex * anglePerItem + anglePerItem / 2f;
            var targetRotation = rotationDegrees + (360f * 8f) + pointerAngle - selectedSegmentCenter;

            spinAnimator = ValueAnimator.OfFloat(rotationDegrees, targetRotation);
            spinAnimator.SetDuration(4600);
            spinAnimator.SetInterpolator(new DecelerateInterpolator(2.8f));
            spinAnimator.Update += OnSpinUpdated;
            spinAnimator.AnimationEnd += OnSpinFinished;
            spinAnimator.Start();
        }

        public WheelView(Context context) : base(context) => Init();
        public WheelView(Context context, IAttributeSet attrs) : base(context, attrs) => Init();
        public WheelView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) => Init();
        public WheelView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) => Init();

        private void Init()
        {
            paint = new Paint();
            paint.AntiAlias = true;
            paint.Dither = true;
            paint.SetStyle(Paint.Style.Fill);

            textPaint = new Paint(PaintFlags.AntiAlias)
            {
                Color = Color.White,
                TextAlign = Paint.Align.Center,
                FakeBoldText = true
            };

            Clickable = true;
        }

        public override bool OnTouchEvent(Android.Views.MotionEvent? e)
        {
            if (e == null)
                return false;

            if (e.Action == MotionEventActions.Up && spinAnimator?.IsRunning != true && rect != null)
            {
                var dx = e.GetX() - rect.CenterX();
                var dy = e.GetY() - rect.CenterY();
                var distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= rect.Width() / 2f)
                {
                    var angle = (float)(Math.Atan2(dy, dx) * 180d / Math.PI) - rotationDegrees;
                    angle = ((angle % 360f) + 360f) % 360f;
                    var segmentIndex = Math.Min(segments.Length - 1, (int)(angle / (360f / segments.Length)));
                    SegmentClicked?.Invoke(this, segmentIndex);
                }

                PerformClick();
                return true;
            }

            return true;
        }

        private void OnSpinUpdated(object? sender, ValueAnimator.AnimatorUpdateEventArgs e)
        {
            if (e.Animation.AnimatedValue is Java.Lang.Float value)
                rotationDegrees = value.FloatValue();

            Invalidate();
        }

        private void OnSpinFinished(object? sender, EventArgs e)
        {
            var anglePerItem = 360f / segments.Length;
            const float pointerAngle = -90f;
            var pointerWheelAngle = pointerAngle - rotationDegrees;
            var normalizedPointerAngle = ((pointerWheelAngle % 360f) + 360f) % 360f;
            var selectedIndex = Math.Min(
                segments.Length - 1,
                (int)(normalizedPointerAngle / anglePerItem));
            DecisionSelected?.Invoke(this, segments[selectedIndex]);

            this.selectedIndex = selectedIndex;
            selectionAnimator = ValueAnimator.OfFloat(0f, 1f);
            selectionAnimator.SetDuration(900);
            selectionAnimator.RepeatCount = 2;
            selectionAnimator.RepeatMode = ValueAnimatorRepeatMode.Reverse;
            selectionAnimator.Update += OnSelectionUpdated;
            selectionAnimator.Start();

            if (spinAnimator != null)
            {
                spinAnimator.Update -= OnSpinUpdated;
                spinAnimator.AnimationEnd -= OnSpinFinished;
                spinAnimator.Dispose();
                spinAnimator = null;
            }
        }

        private void OnSelectionUpdated(object? sender, ValueAnimator.AnimatorUpdateEventArgs e)
        {
            if (e.Animation.AnimatedValue is Java.Lang.Float value)
                selectionPulse = value.FloatValue();

            Invalidate();
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            float inset = Math.Min(w, h) * 0.1f;
            float wheelSize = Math.Min(w, h) - (inset * 2f);
            float left = (w - wheelSize) / 2f;
            float top = inset;
            rect = new RectF(left, top, left + wheelSize, top + wheelSize);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            if (rect == null) return;

            float anglePerItem = 360f / segments.Length;

            for (int i = 0; i < segments.Length; i++)
            {
                float startAngle = rotationDegrees + (i * anglePerItem);
                paint.Color = GetColorForIndex(i);
                canvas.DrawArc(rect, startAngle, anglePerItem, true, paint);

                DrawSegmentText(canvas, segments[i], startAngle + anglePerItem / 2f);
            }

            DrawSelectionHighlight(canvas, anglePerItem);
            DrawPointer(canvas);
        }

        private void DrawSelectionHighlight(Canvas canvas, float anglePerItem)
        {
            if (selectedIndex < 0 || selectedIndex >= segments.Length)
                return;

            paint.SetStyle(Paint.Style.Stroke);
            paint.Color = Color.ParseColor("#FFD166");
            paint.StrokeWidth = 7f + (selectionPulse * 8f);
            canvas.DrawArc(
                rect,
                rotationDegrees + selectedIndex * anglePerItem,
                anglePerItem,
                false,
                paint);
            paint.SetStyle(Paint.Style.Fill);
        }

        private void DrawPointer(Canvas canvas)
        {
            var centerX = rect.CenterX();
            var pointerTop = rect.Top - 28f;
            var pointerTip = rect.Top + 28f;
            var pointerWidth = Math.Max(32f, rect.Width() * 0.14f);
            var path = new Android.Graphics.Path();
            path.MoveTo(centerX - pointerWidth, pointerTop);
            path.LineTo(centerX + pointerWidth, pointerTop);
            path.LineTo(centerX, pointerTip);
            path.Close();

            paint.SetStyle(Paint.Style.Fill);
            paint.Color = Color.Black;
            canvas.DrawPath(path, paint);

            var innerPath = new Android.Graphics.Path();
            innerPath.MoveTo(centerX - pointerWidth * 0.68f, pointerTop + 6f);
            innerPath.LineTo(centerX + pointerWidth * 0.68f, pointerTop + 6f);
            innerPath.LineTo(centerX, pointerTip - 9f);
            innerPath.Close();
            paint.Color = Color.ParseColor("#D7263D");
            canvas.DrawPath(innerPath, paint);
        }

        private void DrawSegmentText(Canvas canvas, string text, float angle)
        {
            var centerX = rect.CenterX();
            var centerY = rect.CenterY();
            var radius = rect.Width() * 0.38f;
            var radians = angle * MathF.PI / 180f;
            var x = centerX + MathF.Cos(radians) * radius;
            var y = centerY + MathF.Sin(radians) * radius;
            var maxWidth = Math.Max(32f, rect.Width() * 0.34f);
            textPaint.TextSize = Math.Max(12f, rect.Width() * 0.045f);
            var fittedText = FitText(text, maxWidth);
            canvas.Save();
            canvas.Rotate(angle, x, y);
            textPaint.SetStyle(Paint.Style.Stroke);
            textPaint.StrokeWidth = Math.Max(3f, textPaint.TextSize * 0.18f);
            textPaint.Color = Color.Black;
            canvas.DrawText(fittedText, x, y + textPaint.TextSize / 3f, textPaint);
            textPaint.SetStyle(Paint.Style.Fill);
            textPaint.Color = Color.White;
            canvas.DrawText(fittedText, x, y + textPaint.TextSize / 3f, textPaint);
            canvas.Restore();
        }

        private string FitText(string text, float maxWidth)
        {
            if (textPaint.MeasureText(text) <= maxWidth)
                return text;

            var shortenedText = text;
            while (shortenedText.Length > 1 && textPaint.MeasureText(shortenedText + "…") > maxWidth)
                shortenedText = shortenedText[..^1];

            return shortenedText == text ? text : shortenedText + "…";
        }
        private Color GetColorForIndex(int index)
        {
            if (index < baseColors.Length)
                return baseColors[index];
            float hue = (index * 137.5f) % 360f;
            return Color.HSVToColor(new float[] { hue, 0.8f, 0.9f });
        }
    }
}