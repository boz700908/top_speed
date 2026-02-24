using System;
using System.Collections.Generic;

namespace TopSpeed.Menu
{
    [Flags]
    internal enum QuestionButtonFlags
    {
        None = 0,
        Default = 1
    }

    internal sealed class QuestionButton
    {
        public QuestionButton(string text, Action onClick, QuestionButtonFlags flags = QuestionButtonFlags.None)
        {
            Text = text ?? string.Empty;
            OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
            Flags = flags;
        }

        public string Text { get; }
        public Action OnClick { get; }
        public QuestionButtonFlags Flags { get; }
    }

    internal sealed class Question
    {
        public Question(string title, string caption, params QuestionButton[] buttons)
        {
            Title = title ?? string.Empty;
            Caption = caption ?? string.Empty;
            Buttons = buttons ?? Array.Empty<QuestionButton>();
        }

        public string Title { get; }
        public string Caption { get; }
        public IReadOnlyList<QuestionButton> Buttons { get; }
    }

    internal sealed class QuestionDialog
    {
        private const string MenuId = "question_dialog";
        private readonly MenuManager _menu;

        public QuestionDialog(MenuManager menu)
        {
            _menu = menu ?? throw new ArgumentNullException(nameof(menu));
            _menu.Register(_menu.CreateMenu(MenuId, new[] { new MenuItem("Question", MenuAction.None) }, string.Empty));
        }

        public bool IsQuestionMenu(string? currentMenuId)
        {
            return string.Equals(currentMenuId, MenuId, StringComparison.Ordinal);
        }

        public void Show(Question question)
        {
            if (question == null)
                throw new ArgumentNullException(nameof(question));

            var items = new List<MenuItem>
            {
                new MenuItem(question.Title, MenuAction.None),
                new MenuItem(question.Caption, MenuAction.None)
            };

            var defaultIndex = 2;
            var firstDefaultFound = false;
            for (var i = 0; i < question.Buttons.Count; i++)
            {
                var button = question.Buttons[i];
                if (!firstDefaultFound && (button.Flags & QuestionButtonFlags.Default) != 0)
                {
                    defaultIndex = 2 + i;
                    firstDefaultFound = true;
                }

                items.Add(new MenuItem(button.Text, MenuAction.None, onActivate: button.OnClick));
            }

            _menu.UpdateItems(MenuId, items);
            var announcement = $"{question.Title}  dialog  {question.Caption}";
            _menu.Push(MenuId, announcement, defaultIndex);
        }
    }
}
