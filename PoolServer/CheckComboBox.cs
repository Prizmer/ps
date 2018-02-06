using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CheckComboBox
{
    /// <summary>
    /// Класс элемента комбобокса
    /// </summary>
    public class CheckComboBoxItem
    {
        /// <param name="text">Лейбл чекбокса</param>
        /// <param name="initialCheckState">Начальное положение чекбокса (true=checked)</param>
        public CheckComboBoxItem(string text, bool initialCheckState)
        {
            _checkState = initialCheckState;
            _text = text;
        }

        private bool _checkState = false;
        /// <summary>
        /// Выбран ли чекбокс (true=checked)
        /// </summary>
        public bool CheckState
        {
            get { return _checkState; }
            set { _checkState = value; }
        }

        private string _text = "";
        /// <summary>
        /// Подпись чекбокса
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        /// <summary>
        /// Плейсхолдер значения комбобокса
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Выбранные драйвера";
        }

    }

    /// <summary>
    /// Модифицированный комбобокс
    /// </summary>
    public partial class CheckComboBox : ComboBox
    {
        public CheckComboBox()
        {
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.DrawItem += new DrawItemEventHandler(CheckComboBox_DrawItem);
            this.SelectedIndexChanged += new EventHandler(CheckComboBox_SelectedIndexChanged);
            SelectedText = "Выбранные драйвера";
        }

        void CheckComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckComboBoxItem item = (CheckComboBoxItem)SelectedItem;
            item.CheckState = !item.CheckState;
            CheckStateChanged?.Invoke(item, e);
        }

        void CheckComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
            {
                return;
            }

            if (!(Items[e.Index] is CheckComboBoxItem))
            {
                e.Graphics.DrawString(
                    Items[e.Index].ToString(),
                    this.Font,
                    Brushes.Black,
                    new Point(e.Bounds.X, e.Bounds.Y));
                return;
            }

            CheckComboBoxItem box = (CheckComboBoxItem)Items[e.Index];

            CheckBoxRenderer.RenderMatchingApplicationState = true;
            CheckBoxRenderer.DrawCheckBox(
                e.Graphics,
                new Point(e.Bounds.X, e.Bounds.Y),
                e.Bounds,
                box.Text,
                this.Font,
                (e.State & DrawItemState.Focus) == 0,
                box.CheckState ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal);
        }

        public event EventHandler CheckStateChanged;

    }
}