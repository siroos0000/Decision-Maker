using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Text;
using Android.Animation;
using Android.Graphics;
using Android.Views.Animations;
using System.Collections.Generic;

namespace Decision_Maker
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private Button addMoreButton;
        private Button spinButton;
        private TextView resultText;
        private WheelView wheelView;
        private RelativeLayout rootLayout;
        private ValueAnimator backgroundAnimator;
        private readonly List<string> segmentTexts = new() { "Yes", "No", "Maybe" };

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            wheelView = FindViewById<WheelView>(Resource.Id.myWheelView);
            addMoreButton = FindViewById<Button>(Resource.Id.AddMore);
            spinButton = FindViewById<Button>(Resource.Id.spin_button);
            resultText = FindViewById<TextView>(Resource.Id.result_text);
            rootLayout = FindViewById<RelativeLayout>(Resource.Id.root_layout);

            StartBackgroundAnimation();

            wheelView.SetSegments(segmentTexts);
            wheelView.DecisionSelected += (_, decision) => resultText.Text = $"Decision: {decision}";
            wheelView.SegmentClicked += (_, index) => ShowEditSegmentDialog(index);

            addMoreButton.Click += (_, _) => ShowAddSegmentDialog();
            spinButton.Click += (_, _) => wheelView.Spin();
        }

        private void StartBackgroundAnimation()
        {
            backgroundAnimator = ValueAnimator.OfFloat(0f, 1f);
            backgroundAnimator.SetDuration(4500);
            backgroundAnimator.RepeatCount = ValueAnimator.Infinite;
            backgroundAnimator.RepeatMode = ValueAnimatorRepeatMode.Reverse;
            backgroundAnimator.SetInterpolator(new AccelerateDecelerateInterpolator());
            backgroundAnimator.Update += (_, e) =>
            {
                if (e.Animation.AnimatedValue is Java.Lang.Float value)
                {
                    var progress = value.FloatValue();
                    var red = (int)(9 + (45 * progress));
                    var green = (int)(10 + (3 * progress));
                    var blue = (int)(12 + (12 * progress));
                    rootLayout.SetBackgroundColor(Color.Rgb(red, green, blue));
                }
            };
            backgroundAnimator.Start();
        }

        private void ShowEditSegmentDialog(int index)
        {
            if (index < 0 || index >= segmentTexts.Count)
                return;

            ShowSegmentDialog("Edit segment", segmentTexts[index], text =>
            {
                segmentTexts[index] = text;
                wheelView.SetSegments(segmentTexts);
                resultText.Text = "Choose a decision";
            });
        }

        private void ShowAddSegmentDialog()
        {
            ShowSegmentDialog("Add segment", string.Empty, text =>
            {
                segmentTexts.Add(text);
                wheelView.SetSegments(segmentTexts);
                resultText.Text = "Choose a decision";
            });
        }

        private void ShowSegmentDialog(string title, string initialText, Action<string> saveText)
        {
            var dialog = new Dialog(this);
            var layout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };
            layout.SetPadding(48, 24, 48, 24);

            var titleView = new TextView(this)
            {
                Text = title,
                TextSize = 20
            };
            layout.AddView(titleView, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent));

            var editor = CreateSegmentEditor(initialText);
            layout.AddView(editor, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent));

            var submitButton = new Button(this)
            {
                Text = "Submit"
            };
            layout.AddView(submitButton, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent));

            var cancelButton = new Button(this)
            {
                Text = "Cancel"
            };
            layout.AddView(cancelButton, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent));

            submitButton.Click += (_, _) =>
            {
                var text = editor.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    saveText(text);
                    dialog.Dismiss();
                }
            };
            cancelButton.Click += (_, _) => dialog.Dismiss();

            dialog.SetContentView(layout);
            dialog.SetTitle(title);
            dialog.Show();
            dialog.Window?.SetLayout(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);
        }

        private EditText CreateSegmentEditor(string text)
        {
            var editor = new EditText(this)
            {
                Text = text,
                Hint = "Segment text"
            };
            editor.InputType = InputTypes.ClassText | InputTypes.TextFlagCapSentences;
            editor.SetSingleLine(true);
            editor.SetSelectAllOnFocus(true);
            editor.SetPadding(48, 12, 48, 12);
            return editor;
        }
    }
}